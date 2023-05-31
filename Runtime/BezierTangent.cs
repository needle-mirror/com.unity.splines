
namespace UnityEngine.Splines
{
    /// <summary>
    /// Describes the direction of a <see cref="BezierKnot"/> tangent. A spline is composed of a list of
    /// <see cref="BezierKnot"/>, where every knot can be either the start or end of a <see cref="BezierCurve"/>. The
    /// <see cref="BezierTangent"/> enum indicates which tangent should be used to construct a curve.
    /// </summary>
    public enum BezierTangent
    {
        /// <summary>
        /// The "In" tangent is the second tangent in a curve composed of two knots.
        /// </summary>
        In = 0,

        /// <summary>
        /// The "Out" tangent is the first tangent in a curve composed of two knots.
        /// </summary>
        Out = 1
    }
}
