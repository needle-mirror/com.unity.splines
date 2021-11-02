namespace UnityEngine.Splines
{
    /// <summary>
    /// ISpline defines the interface from which Spline types inherit.
    /// </summary>
    public interface ISpline
    {   
        public struct DistanceToTime
        {
            public float distance;
            public float time;
        }
        
        /// <summary>
        /// Return the number of knots.
        /// </summary>
        int KnotCount { get; }
        
        /// <summary>
        /// Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).
        /// </summary>
        bool Closed { get; }
        
        /// <summary>
        /// Get the knot at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the knot.</param>
        BezierKnot this[int index] { get; }

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
        /// <param name="index">The index of the curve for which the length needs to be retrieved</param>
        /// <seealso cref="GetLength"/>
        /// <returns>
        /// Returns the length of the curve of index 'index' in the spline.
        /// </returns>
        public float GetCurveLength(int index);

        /// <summary>
        /// Return the normalized time corresponding to a distance on a <see cref="BezierCurve"/>.
        /// </summary>
        /// <param name="index"> The zero-based index of the curve.</param>
        /// <param name="distance"> The distance to convert to time.</param>
        /// <returns>  The normalized time associated to distance on the designated curve. </returns>
        public float CurveDistanceToTime(int index, float distance);
    }
}