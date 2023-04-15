using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A read-only representation of <see cref="Spline"/> that is optimized for efficient access and queries.
    /// NativeSpline can be constructed with a spline and Transform. If a transform is applied, all values will be
    /// relative to the transformed knot positions.
    /// </summary>
    /// <remarks>
    /// NativeSpline is compatible with the job system.
    /// </remarks>
    public struct NativeSpline : ISpline, IDisposable
    {
        [ReadOnly]
        NativeArray<BezierKnot> m_Knots;

        [ReadOnly]
        NativeArray<BezierCurve> m_Curves;

        // As we cannot make a NativeArray of NativeArray all segments lookup tables are stored in a single array
        // each lookup table as a length of k_SegmentResolution and starts at index i = curveIndex * k_SegmentResolution
        [ReadOnly]
        NativeArray<DistanceToInterpolation> m_SegmentLengthsLookupTable;
        
        // As we cannot make a NativeArray of NativeArray all segments lookup tables are stored in a single array
        // each lookup table as a length of k_SegmentResolution and starts at index i = curveIndex * k_SegmentResolution
        [ReadOnly]
        NativeArray<float3> m_UpVectorsLookupTable;
        
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
        /// A NativeArray of <see cref="BezierCurve"/> that form this Spline.
        /// </summary>
        /// <returns>
        /// Returns a reference to the curves array.
        /// </returns>
        public NativeArray<BezierCurve> Curves => m_Curves;

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

        /// <summary>
        /// Gets an enumerator that iterates through the <see cref="BezierKnot"/> collection.
        /// </summary>
        /// <returns>An IEnumerator that is used to iterate the <see cref="BezierKnot"/> collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Create a new NativeSpline from a set of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="spline">The <see cref="ISpline"/> object to convert to a <see cref="NativeSpline"/>.</param>
        /// <param name="allocator">The memory allocation method to use when reserving space for native arrays.</param>
        public NativeSpline(ISpline spline, Allocator allocator = Allocator.Temp)
            : this(spline, float4x4.identity, false, allocator)
        {
        }

        /// <summary>
        /// Create a new NativeSpline from a set of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="spline">The <see cref="ISpline"/> object to convert to a <see cref="NativeSpline"/>.</param>
        /// <param name="cacheUpVectors"> Whether to cache the values of the Up vectors along the entire spline to reduce
        /// the time it takes to access those Up vectors. If you set this to true, the creation of native splines might
        /// be less performant because all the Up vectors along the spline are computed. Consider how often you need to
        /// access the values of Up vectors along the spline before you cache them. </param>
        /// <param name="allocator">The memory allocation method to use when reserving space for native arrays.</param>
        public NativeSpline(ISpline spline, bool cacheUpVectors, Allocator allocator = Allocator.Temp)
            : this(spline, float4x4.identity, cacheUpVectors, allocator)
        {
            
        }


        /// <summary>
        /// Create a new NativeSpline from a set of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="spline">The <see cref="ISpline"/> object to convert to a <see cref="NativeSpline"/>.</param>
        /// <param name="transform">A transform matrix to be applied to the spline knots and tangents.</param>
        /// <param name="allocator">The memory allocation method to use when reserving space for native arrays.</param>
        public NativeSpline(ISpline spline, float4x4 transform, Allocator allocator = Allocator.Temp)
            : this(spline,
                spline is IHasEmptyCurves disconnect ? disconnect.EmptyCurves : null,
                spline.Closed,
                transform,
                false,
                allocator)
        {
        }

        /// <summary>
        /// Create a new NativeSpline from a set of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="spline">The <see cref="ISpline"/> object to convert to a <see cref="NativeSpline"/>.</param>
        /// <param name="transform">A transform matrix to be applied to the spline knots and tangents.</param>
        /// <param name="cacheUpVectors"> Whether to cache the values of the Up vectors along the entire spline to reduce
        /// the time it takes to access those Up vectors. If you set this to true, the creation of native splines might
        /// be less performant because all the Up vectors along the spline are computed. Consider how often you need to
        /// access the values of Up vectors along the spline before you cache them. </param>
        /// <param name="allocator">The memory allocation method to use when reserving space for native arrays.</param>
        public NativeSpline(ISpline spline, float4x4 transform, bool cacheUpVectors, Allocator allocator = Allocator.Temp)
            : this(spline,
                spline is IHasEmptyCurves disconnect ? disconnect.EmptyCurves : null,
                spline.Closed,
                transform,
                cacheUpVectors,
                allocator)
        {
        }
        
        /// <summary>
        /// Create a new NativeSpline from a set of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="knots">A collection of sequential <see cref="BezierKnot"/> forming the spline path.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        /// <param name="transform">Apply a transformation matrix to the control <see cref="Knots"/>.</param>
        /// <param name="allocator">The memory allocation method to use when reserving space for native arrays.</param>
        public NativeSpline(
            IReadOnlyList<BezierKnot> knots,
            bool closed,
            float4x4 transform,
            Allocator allocator = Allocator.Temp) : this(knots, null, closed, transform, false, allocator)
        {
        }
        
        /// <summary>
        /// Create a new NativeSpline from a set of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="knots">A collection of sequential <see cref="BezierKnot"/> forming the spline path.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        /// <param name="transform">Apply a transformation matrix to the control <see cref="Knots"/>.</param>
        /// <param name="cacheUpVectors"> Whether to cache the values of the Up vectors along the entire spline to reduce
        /// the time it takes to access those Up vectors. If you set this to true, the creation of native splines might
        /// be less performant because all the Up vectors along the spline are computed. Consider how often you need to
        /// access the values of Up vectors along the spline before you cache them. </param>
        /// <param name="allocator">The memory allocation method to use when reserving space for native arrays.</param>
        public NativeSpline(
            IReadOnlyList<BezierKnot> knots,
            bool closed,
            float4x4 transform,
            bool cacheUpVectors,
            Allocator allocator = Allocator.Temp) : this(knots, null, closed, transform, cacheUpVectors, allocator)
        {
        }
        
        /// <summary>
        /// Create a new NativeSpline from a set of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="knots">A collection of sequential <see cref="BezierKnot"/> forming the spline path.</param>
        /// <param name="splits">A collection of knot indices that should be considered degenerate curves for the
        /// purpose of creating a non-interpolated gap between curves.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        /// <param name="transform">Apply a transformation matrix to the control <see cref="Knots"/>.</param>
        /// <param name="allocator">The memory allocation method to use when reserving space for native arrays.</param>
        public NativeSpline(IReadOnlyList<BezierKnot> knots, IReadOnlyList<int> splits, bool closed, float4x4 transform, Allocator allocator = Allocator.Temp)
            : this(knots, splits, closed, transform, false, allocator)
        { }
        
        /// <summary>
        /// Create a new NativeSpline from a set of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="knots">A collection of sequential <see cref="BezierKnot"/> forming the spline path.</param>
        /// <param name="splits">A collection of knot indices that should be considered degenerate curves for the
        /// purpose of creating a non-interpolated gap between curves.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        /// <param name="transform">Apply a transformation matrix to the control <see cref="Knots"/>.</param>
        /// <param name="cacheUpVectors"> Whether to cache the values of the Up vectors along the entire spline to reduce
        /// the time it takes to access those Up vectors. If you set this to true, the creation of native splines might
        /// be less performant because all the Up vectors along the spline are computed. Consider how often you need to
        /// access the values of Up vectors along the spline before you cache them. </param>
        /// <param name="allocator">The memory allocation method to use when reserving space for native arrays.</param>
        public NativeSpline(IReadOnlyList<BezierKnot> knots, IReadOnlyList<int> splits, bool closed, float4x4 transform, bool cacheUpVectors, Allocator allocator = Allocator.Temp)
        {
            int knotCount = knots.Count;
            m_Knots = new NativeArray<BezierKnot>(knotCount, allocator);
            m_Curves = new NativeArray<BezierCurve>(knotCount, allocator);
            m_SegmentLengthsLookupTable = new NativeArray<DistanceToInterpolation>(knotCount * k_SegmentResolution, allocator);
            m_Closed = closed;
            m_Length = 0f;

            //Costly to do this for temporary NativeSpline that does not require to access/compute up vectors
            m_UpVectorsLookupTable = new NativeArray<float3>(cacheUpVectors ? knotCount * k_SegmentResolution : 0, allocator);

            // As we cannot make a NativeArray of NativeArray all segments lookup tables are stored in a single array
            // each lookup table as a length of k_SegmentResolution and starts at index i = curveIndex * k_SegmentResolution

            var distanceToTimes = new NativeArray<DistanceToInterpolation>(k_SegmentResolution, Allocator.Temp);
            var upVectors = cacheUpVectors ? new NativeArray<float3>(k_SegmentResolution, Allocator.Temp) : default;

            if (knotCount > 0)
            {
                BezierKnot cur = knots[0].Transform(transform);
                for (int i = 0; i < knotCount; ++i)
                {
                    BezierKnot next = knots[(i + 1) % knotCount].Transform(transform);
                    m_Knots[i] = cur;

                    if (splits != null && splits.Contains(i))
                    {
                        m_Curves[i] = new BezierCurve(new BezierKnot(cur.Position), new BezierKnot(cur.Position));
                        var up = cacheUpVectors ? math.rotate(cur.Rotation, math.up()) : float3.zero;
                        for (int n = 0; n < k_SegmentResolution; ++n)
                        {
                            //Cache Distance in case of a split is empty
                            distanceToTimes[n] = new DistanceToInterpolation();
                            //up Vectors in case of a split is the knot up vector
                            if(cacheUpVectors)
                                upVectors[n] = up;
                        }
                    }
                    else
                    {
                        m_Curves[i] = new BezierCurve(cur, next);
                        CurveUtility.CalculateCurveLengths(m_Curves[i], distanceToTimes);

                        if (cacheUpVectors)
                        {
                            var curveStartUp = math.rotate(cur.Rotation, math.up());
                            var curveEndUp = math.rotate(next.Rotation, math.up());
                            CurveUtility.EvaluateUpVectors(m_Curves[i], curveStartUp, curveEndUp, upVectors);
                        }
                    }

                    if (m_Closed || i < knotCount - 1)
                        m_Length += distanceToTimes[k_SegmentResolution - 1].Distance;

                    for (int index = 0; index < k_SegmentResolution; index++)
                    {
                        m_SegmentLengthsLookupTable[i * k_SegmentResolution + index] = distanceToTimes[index];
                        
                        if(cacheUpVectors)
                            m_UpVectorsLookupTable[i * k_SegmentResolution + index] = upVectors[index];
                    }

                    cur = next;
                }
            }
        }

        /// <summary>
        /// Get a <see cref="BezierCurve"/> from a knot index.
        /// </summary>
        /// <param name="index">The knot index that serves as the first control point for this curve.</param>
        /// <returns>
        /// A <see cref="BezierCurve"/> formed by the knot at index and the next knot.
        /// </returns>
        public BezierCurve GetCurve(int index) => m_Curves[index];


        /// <summary>
        /// Get the length of a <see cref="BezierCurve"/>.
        /// </summary>
        /// <param name="curveIndex">The 0 based index of the curve to find length for.</param>
        /// <returns>The length of the bezier curve at index.</returns>
        public float GetCurveLength(int curveIndex)
        {
            return m_SegmentLengthsLookupTable[curveIndex * k_SegmentResolution + k_SegmentResolution - 1].Distance;    
        }
        
        /// <summary>
        /// Return the up vector for a t ratio on the curve.
        /// </summary>
        /// <param name="index">The index of the curve for which the length needs to be retrieved.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the spline.</param>
        /// <returns>
        /// Returns the up vector at the t ratio of the curve of index 'index'.
        /// </returns>
        public float3 GetCurveUpVector(int index, float t)
        {
            // Value  is not cached, compute the value directly on demand
            if (m_UpVectorsLookupTable.Length == 0)
                return this.CalculateUpVector(index, t);
            
            var curveIndex = index * k_SegmentResolution;
            var offset = 1f / (float)(k_SegmentResolution - 1);
            var curveT = 0f;
            for (int i = 0; i < k_SegmentResolution; i++)
            {
                if (t <= curveT + offset)
                {
                    var value = math.lerp(m_UpVectorsLookupTable[curveIndex + i], 
                                        m_UpVectorsLookupTable[curveIndex + i + 1], 
                                        (t - curveT) / offset);
                    
                    return value;
                }
                curveT += offset;
            }

            //Otherwise, no value has been found, return the one at the end of the segment
            return m_UpVectorsLookupTable[curveIndex + k_SegmentResolution - 1];
        }

        /// <summary>
        /// Release allocated resources.
        /// </summary>
        public void Dispose()
        {
            m_Knots.Dispose();
            m_Curves.Dispose();
            m_SegmentLengthsLookupTable.Dispose();
            m_UpVectorsLookupTable.Dispose();
        }

        // Wrapper around NativeSlice<T> because the native type does not implement IReadOnlyList<T>.
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
