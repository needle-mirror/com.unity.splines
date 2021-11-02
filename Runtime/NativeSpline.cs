using System;
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
        NativeArray<ISpline.DistanceToTime> m_SegmentLengthsLookupTable;
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
        public int KnotCount => m_Knots.Length;
        
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
        /// Create a new NativeSpline from a set of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="knots">A collection of sequential <see cref="BezierKnot"/> forming the spline path.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        /// <param name="allocator">The memory allocation method to use when reserving space for native arrays.</param>
        public NativeSpline(IList<BezierKnot> knots, bool closed, Allocator allocator = Allocator.Temp)
            : this(knots, closed, float4x4.identity, allocator) { }

        /// <summary>
        /// Create a new NativeSpline from a set of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="knots">A collection of sequential <see cref="BezierKnot"/> forming the spline path.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        /// <param name="transform">Apply a transformation matrix to the control <see cref="Knots"/>.</param>
        /// <param name="allocator">The memory allocation method to use when reserving space for native arrays.</param>
        public NativeSpline(IList<BezierKnot> knots, bool closed, float4x4 transform, Allocator allocator = Allocator.Temp)
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
            m_SegmentLengthsLookupTable = new NativeArray<ISpline.DistanceToTime>(knots.Count * k_SegmentResolution, allocator);
            m_Length = 0f;

            List<ISpline.DistanceToTime> distanceToTimes = new List<ISpline.DistanceToTime>(k_SegmentResolution);
            for (int i = 0; i < curveCount; i++)
            {
                CurveUtility.CalculateCurveLengths(GetCurve(i), distanceToTimes);
                m_Length += distanceToTimes.Count > 0 ? distanceToTimes[k_SegmentResolution - 1].distance : 0f;
                
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
            int next = m_Closed ? (index + 1) % KnotCount : math.min(index + 1, KnotCount - 1);
            return new BezierCurve(m_Knots[index], m_Knots[next]);
        }

        /// <summary>
        /// Get the length of a <see cref="BezierCurve"/>.
        /// </summary>
        /// <param name="curveIndex">The 0 based index of the curve to find length for.</param>
        /// <returns>The length of the bezier curve at index.</returns>
        public float GetCurveLength(int curveIndex) => m_SegmentLengthsLookupTable[curveIndex * k_SegmentResolution + k_SegmentResolution - 1].distance;

        /// <summary>
        /// Release allocated resources.
        /// </summary>
        public void Dispose()
        {
            m_Knots.Dispose();
            m_SegmentLengthsLookupTable.Dispose();
        }
        
        /// <summary>
        /// Return the time normalized time t corresponding to a distance on a <see cref="BezierCurve"/>.
        /// </summary>
        /// <param name="index"> The zero-based index of the curve.</param>
        /// <returns>  The normalized time associated to distance on the designated curve. </returns>
        public float CurveDistanceToTime(int index, float dist)
        {
            if(index <0 || index >= m_SegmentLengthsLookupTable.Length || dist <= 0)
                return 0;
            
            var curveLength = GetCurveLength(index);
            if(dist >= curveLength)
                return 1f;
        
            var t = 0f;
            var startIndex = index * k_SegmentResolution;
            var prev = m_SegmentLengthsLookupTable[startIndex];
            for(int i = 1; i < k_SegmentResolution; i++)
            {
                var current = m_SegmentLengthsLookupTable[startIndex + i];
                if(dist < current.distance)
                {
                    t = math.lerp(prev.time, current.time, (dist - prev.distance) / (current.distance - prev.distance));
                    return t;
                }
        
                prev = current;
            }
            
            return 1f;
        }
    }
}
