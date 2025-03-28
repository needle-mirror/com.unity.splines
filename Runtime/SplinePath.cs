using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    // IMPORTANT:
    // SplinePathRef is the serializable equivalent of SplinePath. It is intentionally not public due to
    // questions around tooling and user interface. Left here because it is used for tests and debugging.
    // ...and if it needs to be stated, the name is terrible and should obviously be replaced before becoming public.
    [Serializable]
    class SplinePathRef
    {
        /// <summary>
        /// SliceRef represents a partial or complete range of curves from another <see cref="Spline"/>. This is a
        /// serializable type, intended to be used with <see cref="SplinePathRef"/>. To create an evaluable
        /// <see cref="ISpline"/> from a <see cref="Spline"/> and <see cref="SplineRange"/>, use <see cref="SplineSlice{T}"/>.
        /// A <see cref="SliceRef"/> by itself does not store any <see cref="BezierKnot"/>s. It stores a reference to
        /// a separate <see cref="Spline"/> index within a <see cref="SplineContainer"/>, then retrieves knots by iterating
        /// the <see cref="SplineRange"/>.
        /// Use <see cref="SliceRef"/> in conjunction with <see cref="SplinePathRef"/> to create seamless paths from
        /// discrete <see cref="Spline"/> segments.
        /// </summary>
        [Serializable]
        public class SliceRef
        {
            /// <summary>
            /// The index in the <see cref="SplineContainer.Branches"/> array of the referenced <see cref="Spline"/>.
            /// </summary>
            [SerializeField]
            public int Index;

            /// <summary>
            /// An inclusive start index, number of indices, and direction to iterate.
            /// </summary>
            [SerializeField]
            public SplineRange Range;

            /// <summary>
            /// Constructor for a new <see cref="SliceRef"/>.
            /// </summary>
            /// <param name="splineIndex">The index in the <see cref="SplineContainer.Branches"/> array of the referenced <see cref="Spline"/>.</param>
            /// <param name="range">An inclusive start index, number of indices, and direction to iterate.</param>
            public SliceRef(int splineIndex, SplineRange range)
            {
                Index = splineIndex;
                Range = range;
            }
        }

        [SerializeField]
        public SliceRef[] Splines;

        public SplinePathRef()
        {
        }

        public SplinePathRef(IEnumerable<SliceRef> slices)
        {
            Splines = slices.ToArray();
        }
    }

    /// <summary>
    /// The SplinePath type is an implementation of <see cref="ISpline"/> that is composed of multiple sections of
    /// other splines (see <see cref="SplineSlice{T}"/>). This is useful when you want to evaluate a path that follows
    /// multiple splines, typically in the case where splines share linked knots.
    ///
    /// This class is a data structure that defines the range of curves to associate together. This class is not meant to be
    /// used intensively for runtime evaluation because it is not performant. Data is not meant to be
    /// stored in that struct and that struct is not reactive to spline changes. The GameObject that contains this
    /// slice can be scaled and the knots of the targeted spline that can moved around the curve length cannot be stored
    /// here so evaluating positions, tangents and up vectors is expensive.
    ///
    /// If performance is a critical requirement, create a new <see cref="Spline"/> or
    /// <see cref="NativeSpline"/> from your <see cref="SplinePath{T}"/>. Note that you might pass a <see cref="SplinePath{T}"/>
    /// to constructors for both <see cref="Spline"/> and <see cref="NativeSpline"/>.
    /// </summary>
    /// <seealso cref="SplineRange"/>
    /// <seealso cref="KnotLinkCollection"/>
    /// <seealso cref="SplineKnotIndex"/>
    public class SplinePath : SplinePath<SplineSlice<Spline>>
    {
        /// <summary>
        /// Creates a new <see cref="SplinePath"/> from a collection of <see cref="SplineSlice{T}"/>.
        /// </summary>
        /// <param name="slices">The splines to create this path with.</param>
        public SplinePath(IEnumerable<SplineSlice<Spline>> slices) : base(slices)
        {
        }
    }

    /// <summary>
    /// The SplinePath type is an implementation of <see cref="ISpline"/> that is composed of multiple sections of
    /// other splines (see <see cref="SplineSlice{T}"/>). This is useful when you want to evaluate a path that follows
    /// multiple splines, typically in the case where splines share linked knots.
    ///
    /// If performance is a critical requirement, create a new <see cref="Spline"/> or
    /// <see cref="NativeSpline"/> from your <see cref="SplinePath{T}"/>. Note that you might pass a <see cref="SplinePath{T}"/>
    /// to constructors for both <see cref="Spline"/> and <see cref="NativeSpline"/>.
    /// </summary>
    /// <seealso cref="SplineRange"/>
    /// <seealso cref="KnotLinkCollection"/>
    /// <seealso cref="SplineKnotIndex"/>
    /// <typeparam name="T">The type of spline to create a path with.</typeparam>
    public class SplinePath<T> : ISpline, IHasEmptyCurves where T : ISpline
    {
        T[] m_Splines;

        int[] m_Splits;

        /// <summary>
        /// The <see cref="ISpline"/> splines that make up this path.
        /// </summary>
        public IReadOnlyList<T> Slices
        {
            get => m_Splines;

            set
            {
                m_Splines = value.ToArray();
                BuildSplitData();
            }
        }

        /// <summary>
        /// Create a new <see cref="SplinePath{T}"/> from a collection of <see cref="ISpline"/>.
        /// </summary>
        /// <param name="slices">A collection of <see cref="ISpline"/>.</param>
        public SplinePath(IEnumerable<T> slices)
        {
            m_Splines = slices.ToArray();
            BuildSplitData();
        }

        void BuildSplitData()
        {
            m_Splits = new int[m_Splines.Length];
            for (int i = 0, c = m_Splits.Length, k = 0; i < c; ++i)
                m_Splits[i] = (k += (m_Splines[i].Count + (m_Splines[i].Closed ? 1 : 0))) - 1;
        }

        /// <summary>
        /// Gets an enumerator that iterates through the <see cref="BezierKnot"/> collection.
        /// </summary>
        /// <returns>An IEnumerator that is used to iterate the <see cref="BezierKnot"/> collection.</returns>
        public IEnumerator<BezierKnot> GetEnumerator()
        {
            foreach (var branch in m_Splines)
                foreach (var knot in branch)
                    yield return knot;
        }

        /// <summary>
        /// Gets an enumerator that iterates through the <see cref="BezierKnot"/> collection.
        /// </summary>
        /// <returns>An IEnumerator that is used to iterate the <see cref="BezierKnot"/> collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Returns the number of knots.
        /// Note that there are duplicate knots where two <see cref="ISpline"/> meet.
        /// In addition, each closed <see cref="ISpline"/> have their first knot duplicated.
        /// Use <see cref="GetCurve"/> to access curves rather than construct the curve yourself.
        /// </summary>
        public int Count
        {
            get
            {
                var count = 0;
                foreach (var spline in m_Splines)
                    count += (spline.Count + (spline.Closed ? 1 : 0));

                return count;
            }
        }

        /// <summary>
        /// Gets the knot at <paramref name="index"/>. If the <see cref="ISpline"/> section that contains this
        /// knot has a <see cref="SplineRange"/> with <see cref="SliceDirection.Backward"/>, the in and out tangents
        /// are reversed.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        public BezierKnot this[int index] => this[GetBranchKnotIndex(index)];

        /// <summary>
        /// Gets the knot at <paramref name="index"/>. If the <see cref="ISpline"/> segment that contains this
        /// knot has a <see cref="SplineRange"/> with <see cref="SliceDirection.Backward"/>, the in and out tangents
        /// are reversed.
        /// </summary>
        /// <param name="index">The zero-based index of the slice and knot to get.</param>
        public BezierKnot this[SplineKnotIndex index]
        {
            get
            {
                var spline = m_Splines[index.Spline];
                var knotIndex = spline.Closed ? index.Knot % spline.Count : index.Knot;
                return spline[knotIndex];
            }
        }

        // used by tests
        internal SplineKnotIndex GetBranchKnotIndex(int knot)
        {
            knot = Closed ? knot % Count : math.clamp(knot, 0, Count);

            for (int i = 0, offset = 0; i < m_Splines.Length; i++)
            {
                var slice = m_Splines[i];
                var sliceCount = slice.Count + (slice.Closed ? 1 : 0);
                if (knot < offset + sliceCount)
                    return new SplineKnotIndex(i, math.max(0, knot - offset));
                offset += sliceCount;
            }

            return new SplineKnotIndex(m_Splines.Length - 1, m_Splines[^1].Count - 1);
        }

        /// <summary>
        /// <see cref="SplinePathRef"/> does not support Closed splines.
        /// </summary>
        public bool Closed => false;

        /// <summary>
        /// Return the sum of all curve lengths, accounting for <see cref="Closed"/> state.
        /// </summary>
        /// <returns>
        /// Returns the sum length of all curves composing this spline, accounting for closed state.
        /// </returns>
        public float GetLength()
        {
            var length = 0f;
            for (int i = 0, c = Closed ? Count : Count - 1; i < c; ++i)
                length += GetCurveLength(i);
            return length;
        }

        /// <summary>
        /// A collection of knot indices that should be considered degenerate curves for the purpose of creating a
        /// non-interpolated gap between curves.
        /// </summary>
        public IReadOnlyList<int> EmptyCurves => m_Splits;

        bool IsDegenerate(int index)
        {
            // because splits are set up by this class, we know that indices are sorted
            int split = Array.BinarySearch(m_Splits, index);
            if (split < 0)
                return false;
            return true;
        }

        /// <summary>
        /// Gets a <see cref="BezierCurve"/> from a knot index. This function returns
        /// degenerate (0 length) curves at the overlap points between each <see cref="ISpline"/>.
        /// </summary>
        /// <param name="knot">The knot index that is the first control point for this curve.</param>
        /// <returns>
        /// A <see cref="BezierCurve"/> formed by the knot at index and the next knot.
        /// </returns>
        public BezierCurve GetCurve(int knot)
        {
            var index = GetBranchKnotIndex(knot);

            if (IsDegenerate(knot))
            {
                var point = new BezierKnot(this[index].Position);
                return new BezierCurve(point, point);
            }

            BezierKnot a = this[index], b = this.Next(knot);
            return new BezierCurve(a, b);
        }

        /// <summary>
        /// Returns the length of a curve. This function returns 0 length for knot indices where
        /// <see cref="ISpline"/> segments overlap.
        /// </summary>
        /// <param name="index">The index of the curve that the length is retrieved from.</param>
        /// <seealso cref="GetLength"/>
        /// <returns>
        /// Returns the length of the curve of index 'index' in the spline.
        /// </returns>
        public float GetCurveLength(int index)
        {
            if(IsDegenerate(index))
                return 0f;
            var knot = GetBranchKnotIndex(index);
            var slice = m_Splines[knot.Spline];
            if (knot.Spline >= m_Splines.Length - 1 && knot.Knot >= slice.Count - 1)
                return CurveUtility.CalculateLength(GetCurve(index));
            return slice.GetCurveLength(knot.Knot);
        }

        /// <summary>
        /// Return the up vector for a t ratio on the curve.
        /// </summary>
        /// <param name="index">The index of the curve for which the length needs to be retrieved.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>
        /// Returns the up vector at the t ratio of the curve of index 'index'.
        /// </returns>
        public float3 GetCurveUpVector(int index, float t)
        {
            if(IsDegenerate(index))
                return 0f;

            var knot = GetBranchKnotIndex(index);
            var slice = m_Splines[knot.Spline];

            // Closing curve
            if (knot.Spline >= m_Splines.Length - 1 && knot.Knot >= slice.Count - 1)
            {
                BezierKnot a = this[knot], b = this.Next(index);
                var curve = new BezierCurve(a, b);

                var curveStartUp = math.rotate(a.Rotation, math.up());
                var curveEndUp = math.rotate(b.Rotation, math.up());

                return CurveUtility.EvaluateUpVector(curve, t, curveStartUp, curveEndUp);
            }

            return slice.GetCurveUpVector(knot.Knot, t);
        }

        /// <summary>
        /// Returns the interpolation ratio (0 to 1) that corresponds to a distance on a <see cref="BezierCurve"/>. The
        /// distance is relative to the curve.
        /// </summary>
        /// <param name="curveIndex"> The zero-based index of the curve.</param>
        /// <param name="curveDistance"> The distance measured from the knot at curveIndex to convert to a normalized interpolation ratio.</param>
        /// <returns>The normalized interpolation ratio that matches the distance on the designated curve. </returns>
        public float GetCurveInterpolation(int curveIndex, float curveDistance)
        {
            var knot = GetBranchKnotIndex(curveIndex);
            var slice = m_Splines[knot.Spline];
            return slice.GetCurveInterpolation(knot.Knot, curveDistance);
        }
    }
}
