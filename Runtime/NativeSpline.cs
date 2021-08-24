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
        NativeArray<float> m_SegmentLength;
        bool m_Closed;
        float m_Length;

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
            m_SegmentLength = new NativeArray<float>(knots.Count, allocator);
            m_Length = 0f;

            for (int i = 0; i < curveCount; i++)
            {
                float length = CurveUtility.CalculateLength(GetCurve(i));
                m_Length += length;
                m_SegmentLength[i] = length;
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
        public float GetCurveLength(int curveIndex) => m_SegmentLength[curveIndex];

        /// <summary>
        /// Release allocated resources.
        /// </summary>
        public void Dispose()
        {
            m_Knots.Dispose();
            m_SegmentLength.Dispose();
        }
    }
}
