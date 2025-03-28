using System;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Control points for a cubic Bezier curve.
    ///
    /// Points P0 through P3 are in sequential order, describing the starting point, second, third, and ending controls
    /// for a cubic Bezier curve.
    /// </summary>
    public struct BezierCurve : IEquatable<BezierCurve>
    {
        /// <summary>
        /// First control point.
        /// </summary>
        public float3 P0;

        /// <summary>
        /// Second control point.
        /// Subtract <see cref="P0"/> from <see cref="P1"/> to derive the first tangent for a curve.
        /// </summary>
        public float3 P1;

        /// <summary>
        /// Third control point.
        /// Subtract <see cref="P3"/> from <see cref="P2"/> to derive the second tangent for a curve.
        /// </summary>
        public float3 P2;

        /// <summary>
        /// Fourth control point.
        /// </summary>
        public float3 P3;

        /// <summary>
        /// The direction and magnitude of the first tangent in a cubic curve.
        /// </summary>
        public float3 Tangent0
        {
            get => P1 - P0;
            set => P1 = P0 + value;
        }

        /// <summary>
        /// The direction and magnitude of the second tangent in a cubic curve.
        /// </summary>
        public float3 Tangent1
        {
            get => P2 - P3;
            set => P2 = P3 + value;
        }

        /// <summary>
        /// Construct a cubic Bezier curve from a linear curve. A linear curve is a straight line.
        /// </summary>
        /// <param name="p0">The first control point. This is the start point of the curve.</param>
        /// <param name="p1">The second control point. This is the end point of the curve.</param>
        public BezierCurve(float3 p0, float3 p1)
        {
            P0 = P2 = p0;
            P1 = P3 = p1;
        }

        /// <summary>
        /// Construct a cubic Bezier curve by elevating a quadratic curve.
        /// </summary>
        /// <param name="p0">The first control point. This is the start point of the curve.</param>
        /// <param name="p1">The second control point.</param>
        /// <param name="p2">The third control point. This is the end point of the curve.</param>
        public BezierCurve(float3 p0, float3 p1, float3 p2)
        {
            const float k_13 = 1 / 3f;
            const float k_23 = 2 / 3f;
            float3 tan = k_23 * p1;

            P0 = p0;
            P1 = k_13 * p0 + tan;
            P2 = k_13 * p2 + tan;
            P3 = p2;
        }

        /// <summary>
        /// Construct a cubic Bezier curve from a series of control points.
        /// </summary>
        /// <param name="p0">The first control point. This is the start point of the curve.</param>
        /// <param name="p1">The second control point.</param>
        /// <param name="p2">The third control point.</param>
        /// <param name="p3">The fourth control point. This is the end point of the curve.</param>
        public BezierCurve(float3 p0, float3 p1, float3 p2, float3 p3)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        /// <summary>
        /// Construct a cubic Bezier curve from a start and end <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="a">The knot to use as the first and second control points. The first control point is equal
        /// to <see cref="BezierKnot.Position"/>, and the second control point is equal to
        /// (<see cref="BezierKnot.Position"/> + <see cref="BezierKnot.TangentOut"/> that's rotated by <see cref="BezierKnot.Rotation"/>).</param>
        /// <param name="b">The knot to use as the third and fourth control points. The third control point is equal
        /// to (<see cref="BezierKnot.Position"/> + <see cref="BezierKnot.TangentIn"/> that's rotated by <see cref="BezierKnot.Rotation"/>), and the fourth control point is
        /// equal to <see cref="BezierKnot.Position"/>.</param>
        public  BezierCurve(BezierKnot a, BezierKnot b) :
            this(a.Position, a.Position + math.rotate(a.Rotation, a.TangentOut), b.Position +  math.rotate(b.Rotation, b.TangentIn), b.Position)
        {
        }

        /// <summary>
        /// Multiply the curve positions by a matrix.
        /// </summary>
        /// <param name="matrix">The matrix to multiply.</param>
        /// <returns>A new BezierCurve multiplied by matrix.</returns>
        public BezierCurve Transform(float4x4 matrix)
        {
            return new BezierCurve(
                math.transform(matrix, P0),
                math.transform(matrix, P1),
                math.transform(matrix, P2),
                math.transform(matrix, P3));
        }

        /// <summary>
        /// Create a BezierCurve from a start and end point plus tangent directions.
        /// </summary>
        /// <param name="pointA">Starting position of the curve.</param>
        /// <param name="tangentOutA">The direction and magnitude to the second control point.</param>
        /// <param name="pointB">Ending position of the curve.</param>
        /// <param name="tangentInB">The direction and magnitude to the third control point.</param>
        /// <returns>A new BezierCurve from the derived control points.</returns>
        public static BezierCurve FromTangent(float3 pointA, float3 tangentOutA, float3 pointB, float3 tangentInB)
        {
            return new BezierCurve(pointA, pointA + tangentOutA, pointB + tangentInB, pointB);
        }

        /// <summary>
        /// Gets the same BezierCurve but in the opposite direction.
        /// </summary>
        /// <returns>Returns the BezierCurve struct in the inverse direction.</returns>
        public BezierCurve GetInvertedCurve()
        {
            return new BezierCurve(P3, P2, P1, P0);
        }

        /// <summary>
        /// Compare two curves for equality.
        /// </summary>
        /// <param name="other">The curve to compare against.</param>
        /// <returns>Returns true when the control points of each curve are identical.</returns>
        public bool Equals(BezierCurve other)
        {
            return P0.Equals(other.P0) && P1.Equals(other.P1) && P2.Equals(other.P2) && P3.Equals(other.P3);
        }

        /// <summary>
        /// Compare against an object for equality.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns>
        /// Returns true when <paramref name="obj"/> is a <see cref="BezierCurve"/> and the control points of each
        /// curve are identical.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is BezierCurve other && Equals(other);
        }

        /// <summary>
        /// Calculate a hash code for this curve.
        /// </summary>
        /// <returns>
        /// A hash code for the curve.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = P0.GetHashCode();
                hashCode = (hashCode * 397) ^ P1.GetHashCode();
                hashCode = (hashCode * 397) ^ P2.GetHashCode();
                hashCode = (hashCode * 397) ^ P3.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Compare two curves for equality.
        /// </summary>
        /// <param name="left">The first curve.</param>
        /// <param name="right">The second curve.</param>
        /// <returns>Returns true when the control points of each curve are identical.</returns>
        public static bool operator ==(BezierCurve left, BezierCurve right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare two curves for inequality.
        /// </summary>
        /// <param name="left">The first curve.</param>
        /// <param name="right">The second curve.</param>
        /// <returns>Returns false when the control points of each curve are identical.</returns>
        public static bool operator !=(BezierCurve left, BezierCurve right)
        {
            return !left.Equals(right);
        }
    }
}
