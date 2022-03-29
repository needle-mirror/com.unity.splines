using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A readonly representation of <see cref="Spline"/> that is optimized for efficient access and queries.
    /// NativeSpline can be constructed with a Spline and Transform. If a transform is applied, all values will be
    /// relative to the transformed knot positions.
    /// </summary>
    /// <remarks>
    /// NativeSpline is compatible with the job system.
    /// </remarks>
    public struct NativeSpline : ISpline, IDisposable
    {
        NativeArray<BezierKnot> m_Knots;
        // As we cannot make a NativeArray of NativeArray all segments lookup tables are stored in a single array
        // each lookup table as a length of k_SegmentResolution and starts at index i = curveIndex * k_SegmentResolution
        NativeArray<DistanceToInterpolation> m_SegmentLengthsLookupTable;
        bool m_Closed;
        float m_Length;
        const int k_SegmentResolution = 30;

        /// <summary>
        /// A NativeArray of <see cref="BezierKnot"/> that form this Spline.
        /// </summary>
        /// <returns>
        /// Returns a reference to the knots array.
        /// </returns>
        public NativeArray<BezierKnot> Knots => m_Knots;

        /// <summary>
        /// Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).
        /// </summary>
        public bool Closed => m_Closed;

        /// <summary>
        /// Return the number of knots.
        /// </summary>
        public int Count => m_Knots.Length;

        /// <summary>
        /// Return the sum of all curve lengths, accounting for <see cref="Closed"/> state.
        /// Note that this value is affected by the transform used to create this NativeSpline.
        /// </summary>
        /// <returns>
        /// Returns the sum length of all curves composing this spline, accounting for closed state.
        /// </returns>
        public float GetLength() => m_Length;

        /// <summary>
        /// Get the knot at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the knot.</param>
        public BezierKnot this[int index] => m_Knots[index];

        /// <summary>
        /// Get an enumerator that iterates through the <see cref="BezierKnot"/> collection.
        /// </summary>
        /// <returns>An IEnumerator that is used to iterate the <see cref="BezierKnot"/> collection.</returns>
        public IEnumerator<BezierKnot> GetEnumerator() => m_Knots.GetEnumerator();
            
        /// <inheritdoc cref="GetEnumerator"/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Create a new NativeSpline from a set of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="spline">The <see cref="ISpline"/> object to convert to a <see cref="NativeSpline"/>.</param>
        /// <param name="allocator">The memory allocation method to use when reserving space for native arrays.</param>
        public NativeSpline(ISpline spline, Allocator allocator = Allocator.Temp)
            : this(spline, spline.Closed, float4x4.identity, allocator) { }

        /// <summary>
        /// Create a new NativeSpline from a set of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="spline">The <see cref="ISpline"/> object to convert to a <see cref="NativeSpline"/>.</param>
        /// <param name="transform">A transform matrix to be applied to the spline knots and tangents.</param>
        /// <param name="allocator">The memory allocation method to use when reserving space for native arrays.</param>
        public NativeSpline(ISpline spline, float4x4 transform, Allocator allocator = Allocator.Temp)
            : this(spline, spline.Closed, transform, allocator) { }

        /// <summary>
        /// Create a new NativeSpline from a set of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="knots">A collection of sequential <see cref="BezierKnot"/> forming the spline path.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        /// <param name="transform">Apply a transformation matrix to the control <see cref="Knots"/>.</param>
        /// <param name="allocator">The memory allocation method to use when reserving space for native arrays.</param>
        public NativeSpline(IReadOnlyList<BezierKnot> knots, bool closed, float4x4 transform, Allocator allocator = Allocator.Temp)
        {
            int kc = knots.Count;
            m_Knots = new NativeArray<BezierKnot>(knots.Count, allocator);

            for (int i = 0; i < kc; i++)
                m_Knots[i] = knots[i].Transform(transform);

            m_Closed = closed;
            m_Length = 0f;

            int curveCount = m_Closed ? kc : kc - 1;

            // As we cannot make a NativeArray of NativeArray all segments lookup tables are stored in a single array
            // each lookup table as a length of k_SegmentResolution and starts at index i = curveIndex * k_SegmentResolution
            m_SegmentLengthsLookupTable = new NativeArray<DistanceToInterpolation>(knots.Count * k_SegmentResolution, allocator);
            m_Length = 0f;

            DistanceToInterpolation[] distanceToTimes = new DistanceToInterpolation[k_SegmentResolution];

            for (int i = 0; i < curveCount; i++)
            {
                CurveUtility.CalculateCurveLengths(GetCurve(i), distanceToTimes);
                m_Length += distanceToTimes[k_SegmentResolution - 1].Distance;
                for(int distanceToTimeIndex = 0; distanceToTimeIndex < k_SegmentResolution; distanceToTimeIndex++)
                    m_SegmentLengthsLookupTable[i * k_SegmentResolution + distanceToTimeIndex] = distanceToTimes[distanceToTimeIndex];
            }
        }

        /// <summary>
        /// Get a <see cref="BezierCurve"/> from a knot index.
        /// </summary>
        /// <param name="index">The knot index that serves as the first control point for this curve.</param>
        /// <returns>
        /// A <see cref="BezierCurve"/> formed by the knot at index and the next knot.
        /// </returns>
        public BezierCurve GetCurve(int index)
        {
            int next = m_Closed ? (index + 1) % Count : math.min(index + 1, Count - 1);
            return new BezierCurve(m_Knots[index], m_Knots[next]);
        }

        /// <summary>
        /// Get the length of a <see cref="BezierCurve"/>.
        /// </summary>
        /// <param name="curveIndex">The 0 based index of the curve to find length for.</param>
        /// <returns>The length of the bezier curve at index.</returns>
        public float GetCurveLength(int curveIndex) => m_SegmentLengthsLookupTable[curveIndex * k_SegmentResolution + k_SegmentResolution - 1].Distance;

        /// <summary>
        /// Release allocated resources.
        /// </summary>
        public void Dispose()
        {
            m_Knots.Dispose();
            m_SegmentLengthsLookupTable.Dispose();
        }

        struct Slice<T> : IReadOnlyList<T> where T : struct
        {
            NativeSlice<T> m_Slice;
            public Slice(NativeArray<T> array, int start, int count) { m_Slice = new NativeSlice<T>(array, start, count); }
            public IEnumerator<T> GetEnumerator() => m_Slice.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public int Count => m_Slice.Length;
            public T this[int index] => m_Slice[index];
        }

        /// <summary>
        /// Return the normalized interpolation (t) corresponding to a distance on a <see cref="BezierCurve"/>.
        /// </summary>
        /// <param name="curveIndex"> The zero-based index of the curve.</param>
        /// <param name="curveDistance">The curve-relative distance to convert to an interpolation ratio (also referred to as 't').</param>
        /// <returns>  The normalized interpolation ratio associated to distance on the designated curve.</returns>
        public float GetCurveInterpolation(int curveIndex, float curveDistance)
        {
            if(curveIndex <0 || curveIndex >= m_SegmentLengthsLookupTable.Length || curveDistance <= 0)
                return 0f;
            var curveLength = GetCurveLength(curveIndex);
            if(curveDistance >= curveLength)
                return 1f;
            var startIndex = curveIndex * k_SegmentResolution;
            var slice = new Slice<DistanceToInterpolation>(m_SegmentLengthsLookupTable, startIndex, k_SegmentResolution);
            return CurveUtility.GetDistanceToInterpolation(slice, curveDistance);
        }
    }
}
