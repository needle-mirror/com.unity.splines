#if UNITY_BURST_ENABLED
using Unity.Burst;
#endif
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Provides methods to calculate, in parallel, the positions along <see cref="NativeSpline"/>.
    /// </summary>
    #if UNITY_BURST_ENABLED
    [BurstCompile]
    #endif
    public struct GetPosition : IJobParallelFor
    {
        /// <summary>
        /// The <see cref="NativeSpline"/> to be evaluated.
        /// </summary>
        /// <remarks>
        /// Must be allocated with a Allocator.Persistent or Allocator.TempJob.
        /// </remarks>
        [ReadOnly]
        public NativeSpline Spline;

        /// <summary>
        /// A NativeArray of float3 to be written. The size of this array determines how many positions are
        /// evaluated.
        /// </summary>
        [WriteOnly]
        public NativeArray<float3> Positions;

        /// <summary>
        /// Called by the job system to evaluate a position at an index. The interpolation value is calculated as
        /// `index / positions.Length - 1`.
        /// </summary>
        /// <param name="index">The index of the positions array to evaluate.</param>
        public void Execute(int index)
        {
            Positions[index] = Spline.EvaluatePosition(index / (Positions.Length-1f));
        }
    }

    /// <summary>
    /// A job struct for calculating in parallel the position, tangent, and normal (up) vectors along a
    /// <see cref="NativeSpline"/>.
    /// </summary>
    public struct GetPositionTangentNormal : IJobParallelFor
    {
        /// <summary>
        /// The <see cref="NativeSpline"/> to be evaluated.
        /// </summary>
        /// <remarks>
        /// Must be allocated with a Allocator.Persistent or Allocator.TempJob.
        /// </remarks>
        [ReadOnly]
        public NativeSpline Spline;

        /// <summary>
        /// A NativeArray of float3 to be written. The size of this array determines how many positions are
        /// evaluated.
        /// </summary>
        [WriteOnly]
        public NativeArray<float3> Positions;

        /// <summary>
        /// A NativeArray of float3 to be written. The size of this array must match the length of <see cref="Positions"/>.
        /// </summary>
        [WriteOnly]
        public NativeArray<float3> Tangents;

        /// <summary>
        /// A NativeArray of float3 to be written. The size of this array must match the length of <see cref="Positions"/>.
        /// </summary>
        [WriteOnly]
        public NativeArray<float3> Normals;

        /// <summary>
        /// Called by the job system to evaluate position, tangent, and normal at an index. The interpolation value is
        /// calculated as `index / positions.Length - 1`.
        /// </summary>
        /// <param name="index">The index of the positions array to evaluate.</param>
        public void Execute(int index)
        {
            Spline.Evaluate(index / (Positions.Length - 1f), out var p, out var t, out var n);
            Positions[index] = p;
            Tangents[index] = t;
            Normals[index] = n;
        }
    }

    /// <summary>
    /// The SplineJobs class contains utility methods for evaluating spline data using the Jobs system.
    /// </summary>
    public static class SplineJobs
    {
        /// <summary>
        /// Populate a preallocated NativeArray with position data from a spline.
        /// </summary>
        /// <param name="spline">The spline to evaluate. If you pass a NativeSpline, it must be allocated
        /// with the Persistent or TempJob allocator. Temp is invalid for use with the Jobs system.</param>
        /// <param name="positions">A preallocated array of float3 to be populated with evenly interpolated positions
        /// from a spline.</param>
        /// <typeparam name="T">The type of ISpline.</typeparam>
        public static void EvaluatePosition<T>(T spline, NativeArray<float3> positions) where T : ISpline
        {
            using var native = new NativeSpline(spline, Allocator.TempJob);
            EvaluatePosition(native, positions);
        }

        /// <summary>
        /// Populate a preallocated NativeArray with position data from a spline.
        /// </summary>
        /// <param name="spline">The spline to evaluate. The NativeSpline must be allocated with a Persistent
        /// or TempJob allocator. Temp is invalid for use in the Jobs system.</param>
        /// <param name="positions">A preallocated array of float3 to be populated with evenly interpolated positions
        /// from a spline.</param>
        public static void EvaluatePosition(NativeSpline spline, NativeArray<float3> positions)
        {
            var job = new GetPosition()
            {
                Spline = spline,
                Positions = positions
            };

            var handle = job.Schedule(positions.Length, 1);
            handle.Complete();
        }

        /// <summary>
        /// Populate a set of pre-allocated NativeArray with position, tangent, and normal data from a spline.
        /// </summary>
        /// <remarks>
        /// To apply a transform to the results of this method, pass a new NativeSpline constructed with the desired
        /// transformation matrix.
        ///
        /// This method creates a temporary NativeSpline copy of the spline to be evaluated. In some cases, this can
        /// be more resource intensive than iterating and evaluating a spline on a single thread. For the best performance,
        /// pass an existing NativeSpline instance to the <paramref name="spline"/>
        /// parameter.
        /// </remarks>
        /// <param name="spline">The spline to evaluate. If you pass a NativeSpline, it must be allocated
        /// with the Persistent or TempJob allocator. Temp is invalid for use with the Jobs system.</param>
        /// <param name="positions">A preallocated array of float3 to be populated with evenly interpolated positions
        /// from a spline.</param>
        /// <param name="tangents">A preallocated array of float3 to be populated with evenly interpolated tangents
        /// from a spline. Must be the same size as the positions array.</param>
        /// <param name="normals">A preallocated array of float3 to be populated with evenly interpolated normals
        /// from a spline. Must be the same size as the positions array.</param>
        /// <typeparam name="T">The type of ISpline.</typeparam>
        public static void EvaluatePositionTangentNormal<T>(
            T spline,
            NativeArray<float3> positions,
            NativeArray<float3> tangents,
            NativeArray<float3> normals) where T : ISpline
        {
            using var native = new NativeSpline(spline, Allocator.TempJob);
            EvaluatePositionTangentNormal(native, positions, tangents, normals);
        }

        /// <summary>
        /// Populate a set of preallocated NativeArray with position, tangent, and normal data from a spline.
        /// </summary>
        /// <remarks>
        /// To apply a transform to the results of this method, pass a new NativeSpline constructed with the desired
        /// transformation matrix.
        /// </remarks>
        /// <param name="spline">The spline to evaluate. The NativeSpline must be allocated with a Persistent
        /// or TempJob allocator. Temp is invalid for use in the Jobs system.</param>
        /// <param name="positions">A preallocated array of float3 to be populated with evenly interpolated positions
        /// from a spline.</param>
        /// <param name="tangents">A preallocated array of float3 to be populated with evenly interpolated tangents
        /// from a spline. Must be the same size as the positions array.</param>
        /// <param name="normals">A preallocated array of float3 to be populated with evenly interpolated normals
        /// from a spline. Must be the same size as the positions array.</param>
        public static void EvaluatePositionTangentNormal(NativeSpline spline,
            NativeArray<float3> positions,
            NativeArray<float3> tangents,
            NativeArray<float3> normals)
        {
            var job = new GetPositionTangentNormal()
            {
                Spline = spline,
                Positions = positions,
                Tangents = tangents,
                Normals = normals
            };

            var handle = job.Schedule(positions.Length, 1);
            handle.Complete();
        }
    }
}
