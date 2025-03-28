namespace UnityEngine.Splines
{
    /// <summary>
    /// Describes the different ways a tool might interact with a tangent handle.
    /// </summary>
    public enum TangentMode
    {
        /// <summary>
        /// Tangents are calculated using the previous and next knot positions.
        /// </summary>
        AutoSmooth = 0,

        /// <summary>
        /// Tangents are not used. A linear spline is a series of knots connected by a path with no curvature.
        /// </summary>
        Linear = 1,

        /// <summary>
        /// Tangents are kept parallel and with matching lengths. Modifying one tangent updates the opposite
        /// tangent to the inverse direction and equivalent length.
        /// </summary>
        Mirrored = 2,

        /// <summary>
        /// Tangents are kept parallel. Modifying one tangent changes the direction of the opposite tangent,
        /// but does not affect the opposite tangent's length.
        /// </summary>
        Continuous = 3,

        /// <summary>
        /// The length and direction of the tangents are independent of each other. Modifying one tangent on a knot does not affect the other.
        /// </summary>
        Broken = 4
    }
}
