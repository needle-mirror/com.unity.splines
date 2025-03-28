using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A key-value pair associating a distance to interpolation ratio ('t') value. This is used when evaluating Spline
    /// attributes to ensure uniform distribution of sampling points.
    /// </summary>
    /// <seealso cref="CurveUtility.CalculateCurveLengths"/>
    [Serializable]
    public struct DistanceToInterpolation
    {
        /// <summary>
        /// Distance in Unity units.
        /// </summary>
        public float Distance;

        /// <summary>
        /// A normalized interpolation ratio ('t').
        /// </summary>
        public float T;

        internal static readonly DistanceToInterpolation Invalid = new () { Distance = -1f, T = -1f };
    }

    /// <summary>
    /// This interface defines a collection of knot indices that should be considered disconnected from the following
    /// knot indices when creating a <see cref="BezierCurve"/>.
    /// </summary>
    public interface IHasEmptyCurves
    {
        /// <summary>
        /// A collection of knot indices that should be considered degenerate curves for the purpose of creating a
        /// non-interpolated gap between curves.
        /// </summary>
        public IReadOnlyList<int> EmptyCurves { get; }
    }

    /// <summary>
    /// ISpline defines the interface from which Spline types inherit.
    /// </summary>
    public interface ISpline : IReadOnlyList<BezierKnot>
    {
        /// <summary>
        /// Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).
        /// </summary>
        bool Closed { get; }

        /// <summary>
        /// Return the sum of all curve lengths, accounting for <see cref="Closed"/> state.
        /// </summary>
        /// <returns>
        /// Returns the sum length of all curves composing this spline, accounting for closed state.
        /// </returns>
        float GetLength();

        /// <summary>
        /// Get a <see cref="BezierCurve"/> from a knot index.
        /// </summary>
        /// <param name="index">The knot index that serves as the first control point for this curve.</param>
        /// <returns>
        /// A <see cref="BezierCurve"/> formed by the knot at index and the next knot.
        /// </returns>
        public BezierCurve GetCurve(int index);

        /// <summary>
        /// Return the length of a curve.
        /// </summary>
        /// <param name="index">The index of the curve for which the length needs to be retrieved.</param>
        /// <seealso cref="GetLength"/>
        /// <returns>
        /// Returns the length of the curve of index 'index' in the spline.
        /// </returns>
        public float GetCurveLength(int index);

        /// <summary>
        /// Return the up vector for a t ratio on the curve. Contrary to <see cref="SplineUtility.EvaluateUpVector"/>,
        /// this method uses cached values when possible for better performance when accessing
        /// these values regularly.
        /// </summary>
        /// <param name="index">The index of the curve for which the length needs to be retrieved.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>
        /// Returns the up vector at the t ratio of the curve of index 'index'.
        /// </returns>
        public float3 GetCurveUpVector(int index, float t);

        /// <summary>
        /// Return the interpolation ratio (0 to 1) corresponding to a distance on a <see cref="BezierCurve"/>. Distance
        /// is relative to the curve.
        /// </summary>
        /// <param name="curveIndex"> The zero-based index of the curve.</param>
        /// <param name="curveDistance"> The distance (measuring from the knot at curveIndex) to convert to a normalized interpolation ratio.</param>
        /// <returns>The normalized interpolation ratio matching distance on the designated curve. </returns>
        public float GetCurveInterpolation(int curveIndex, float curveDistance);
    }
}
