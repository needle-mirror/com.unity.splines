using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// SplineSlice represents a partial or complete range of curves from another <see cref="Spline"/>.
    /// A <see cref="SplineSlice{T}"/> by itself does not store any <see cref="BezierKnot"/>s. It stores a reference to
    /// a separate <see cref="Spline"/>, then retrieves knots by iterating the <see cref="SplineRange"/>.
    /// Use <see cref="SplineSlice{T}"/> in conjunction with <see cref="SplinePath"/> to create seamless paths from
    /// discrete <see cref="Spline"/> segments.
    ///
    /// This class is a data structure that defines the range of curves to associate together. This class is not meant to be
    /// used intensively for runtime evaluation because it is not performant. Data is not meant to be
    /// stored in that struct and that struct is not reactive to spline changes. The GameObject that contains this
    /// slice can be scaled and the knots of the targeted spline that can moved around the curve length cannot be stored
    /// here so evaluating positions, tangents and up vectors is expensive.
    ///
    /// If performance is a critical requirement, create a new <see cref="Spline"/> or
    /// <see cref="NativeSpline"/> from the relevant <see cref="SplinePath{T}"/> or <see cref="SplineSlice{T}"/>.
    /// Note that you might pass a <see cref="SplineSlice{T}"/> to constructors for both <see cref="Spline"/> and <see cref="NativeSpline"/>.
    /// </summary>
    /// <remarks>
    /// Iterating a <see cref="SplineSlice{T}"/> is not as efficient as iterating a <see cref="Spline"/> or
    /// <see cref="NativeSpline"/> because it does not cache any information. Where performance is a concern, create
    /// a new <see cref="Spline"/> or <see cref="NativeSpline"/> from the <see cref="SplineSlice{T}"/>.
    /// </remarks>
    /// <typeparam name="T">The type of spline that this slice represents.</typeparam>
    public struct SplineSlice<T> : ISpline where T : ISpline
    {
        /// <summary>
        /// The <see cref="Spline"/> that this Slice will read <see cref="BezierKnot"/> and <see cref="BezierCurve"/>
        /// data from.
        /// A <see cref="SplineSlice{T}"/> by itself does not store any <see cref="BezierKnot"/>s. Instead, it references
        /// a partial or complete range of existing <see cref="Spline"/>s.
        /// </summary>
        public T Spline;

        /// <summary>
        /// An inclusive start index, number of indices, and direction to iterate.
        /// </summary>
        public SplineRange Range;

        /// <summary>
        /// A transform matrix to be applied to the spline knots and tangents.
        /// </summary>
        public float4x4 Transform;

        /// <summary>
        /// Return the number of knots in this branch. This function clamps the <see cref="Range"/> to the Count of the
        /// the referenced <see cref="Spline"/>.
        /// </summary>
        public int Count
        {
            get
            {
                if (Spline.Closed)
                    return math.clamp(Range.Count, 0, Spline.Count + 1);

                if (Range.Direction == SliceDirection.Backward)
                    return math.clamp(Range.Count, 0, Range.Start + 1);
                else
                    return math.clamp(Range.Count, 0, Spline.Count - Range.Start);
            }
        }

        /// <summary>
        /// Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).
        /// </summary>
        public bool Closed => false;

        static BezierKnot FlipTangents(BezierKnot knot) =>
            new BezierKnot(knot.Position, knot.TangentOut, knot.TangentIn, knot.Rotation);

        /// <summary>
        /// Get a <see cref="BezierKnot"/> at the zero-based index of this <see cref="SplineSlice{T}"/>.
        /// </summary>
        /// <param name="index">The index to get.</param>
        public BezierKnot this[int index]
        {
            get
            {
                int indexFromRange = Range[index];
                indexFromRange = (indexFromRange + Spline.Count) % Spline.Count;

                return Range.Direction == SliceDirection.Backward
                   ? FlipTangents(Spline[indexFromRange]).Transform(Transform)
                   : Spline[indexFromRange].Transform(Transform);
            }
        }

        /// <summary>
        /// Get an enumerator that iterates through the <see cref="BezierKnot"/> collection. Note that this will either
        /// increment or decrement indices depending on the value of the <see cref="SplineRange.Direction"/>.
        /// </summary>
        /// <returns>An IEnumerator that is used to iterate the <see cref="BezierKnot"/> collection.</returns>
        public IEnumerator<BezierKnot> GetEnumerator()
        {
            for (int i = 0, c = Range.Count; i < c; ++i)
                yield return this[i];
        }

        /// <summary>
        /// Gets an enumerator that iterates through the <see cref="BezierKnot"/> collection. It either
        /// increments or decrements indices depending on the value of the <see cref="SplineRange.Direction"/>.
        /// </summary>
        /// <returns>Returns an IEnumerator that is used to iterate the <see cref="BezierKnot"/> collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Constructor for a new SplineSlice.
        /// </summary>
        /// <param name="spline">
        /// The <see cref="Spline"/> that this Slice will read <see cref="BezierKnot"/> and <see cref="BezierCurve"/>
        /// data from.
        /// </param>
        /// <param name="range">The start index and count of knot indices that compose this slice.</param>
        public SplineSlice(T spline, SplineRange range)
            : this(spline, range, float4x4.identity)
        {}

        /// <summary>
        /// Constructor for a new SplineSlice.
        /// </summary>
        /// <param name="spline">
        /// The <see cref="Spline"/> that this Slice will read <see cref="BezierKnot"/> and <see cref="BezierCurve"/>
        /// data from.
        /// </param>
        /// <param name="range">The start index and count of knot indices that compose this slice.</param>
        /// <param name="transform">A transform matrix to be applied to the spline knots and tangents.</param>
        public SplineSlice(T spline, SplineRange range, float4x4 transform)
        {
            Spline = spline;
            Range = range;
            Transform = transform;
        }

        /// <summary>
        /// Return the sum of all curve lengths.
        /// </summary>
        /// <remarks>
        /// It is inefficient to call this method frequently, as it will calculate the length of all curves every time
        /// it is invoked. In cases where performance is critical, create a new <see cref="Spline"/> or
        /// <see cref="NativeSpline"/> instead. Note that you may pass a <see cref="SplineSlice{T}"/> to constructors
        /// for both <see cref="Spline"/> and <see cref="NativeSpline"/>.
        /// </remarks>
        /// <seealso cref="GetCurveLength"/>
        /// <returns>
        /// Returns the sum length of all curves composing this spline.
        /// </returns>
        public float GetLength()
        {
            var len = 0f;
            for (int i = 0, c = Count; i < c; ++i)
                len += GetCurveLength(i);
            return len;
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
            int bi = math.min(math.max(index + 1, 0), Range.Count-1);
            BezierKnot a = this[index], b = this[bi];
            if (index == bi)
                return new BezierCurve(a.Position, b.Position);
            return new BezierCurve(a, b);
        }

        /// <summary>
        /// Return the length of a curve.
        /// </summary>
        /// <param name="index">The index of the curve for which the length needs to be retrieved.</param>
        /// <seealso cref="GetLength"/>
        /// <remarks>
        /// The curve length cannot be cached here because the transform matrix associated to this slice might impact that
        /// value, like using a non-uniform scale on the GameObject associated with that slice. It is inefficient
        /// to call this method frequently, because it calculates the length of the curve every time
        /// it is invoked.
        /// If performance is a critical requirement, create a new <see cref="Spline"/> or
        /// <see cref="NativeSpline"/> from the relevant <see cref="SplinePath{T}"/> or <see cref="SplineSlice{T}"/>.
        /// Note that you might pass a <see cref="SplineSlice{T}"/> to constructors for both <see cref="Spline"/> and <see cref="NativeSpline"/>.
        /// </remarks>
        /// <returns>
        /// Returns the length of the curve of index 'index' in the spline.
        /// </returns>
        public float GetCurveLength(int index)
        {
            return CurveUtility.CalculateLength(GetCurve(index));
        }

        /// <summary>
        /// Return the up vector for a t ratio on the curve.
        /// </summary>
        /// <param name="index">The index of the curve for which the length needs to be retrieved.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <remarks>
        /// It is inefficient to call this method frequently, as it will calculate the up Vector of the curve every time
        /// it is invoked. In cases where performance is critical, create a new <see cref="Spline"/> or
        /// <see cref="NativeSpline"/> instead. Note that you may pass a <see cref="SplineSlice{T}"/> to constructors
        /// for both <see cref="Spline"/> and <see cref="NativeSpline"/>.
        /// </remarks>
        /// <returns>
        /// Returns the up vector at the t ratio of the curve of index 'index'.
        /// </returns>
        public float3 GetCurveUpVector(int index, float t)
        {
            return SplineUtility.CalculateUpVector(this, index, t);
        }

        /// <summary>
        /// Return the normalized interpolation (t) corresponding to a distance on a <see cref="BezierCurve"/>.
        /// </summary>
        /// <remarks>
        /// It is inefficient to call this method frequently, as it will calculate the interpolation lookup table every
        /// time it is invoked. In cases where performance is critical, create a new <see cref="Spline"/> or
        /// <see cref="NativeSpline"/> instead. Note that you may pass a <see cref="SplineSlice{T}"/> to constructors
        /// for both <see cref="Spline"/> and <see cref="NativeSpline"/>.
        /// </remarks>
        /// <param name="curveIndex"> The zero-based index of the curve.</param>
        /// <param name="curveDistance">The curve-relative distance to convert to an interpolation ratio (also referred to as 't').</param>
        /// <returns>  The normalized interpolation ratio associated to distance on the designated curve.</returns>
        public float GetCurveInterpolation(int curveIndex, float curveDistance)
        {
            return CurveUtility.GetDistanceToInterpolation(GetCurve(curveIndex), curveDistance);
        }
    }
}
