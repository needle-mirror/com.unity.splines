using System;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// This struct contains position and tangent data for a knot. The position is a scalar point and the tangents are vectors.
    /// The <see cref="Spline"/> class stores a collection of BezierKnot that form a series of connected
    /// <see cref="BezierCurve"/>. Each knot contains a Position, Tangent In, and Tangent Out. When a spline is not
    /// closed, the first and last knots will contain an extraneous tangent (in and out, respectively).
    /// </summary>
    [Serializable]
    public struct BezierKnot : ISerializationCallbackReceiver, IEquatable<BezierKnot>
    {
        /// <summary>
        /// The position of the knot in local space. On a cubic Bezier curve, this is equivalent to <see cref="BezierCurve.P0"/> or
        /// <see cref="BezierCurve.P3"/>, depending on whether this knot is forming the first or second control point
        /// of the curve.
        /// </summary>
        public float3 Position;

        /// <summary>
        /// The tangent vector that leads into this knot. On a cubic Bezier curve, this value is used to calculate
        /// <see cref="BezierCurve.P2"/> when used as the second knot in a curve.
        /// </summary>
        public float3 TangentIn;

        /// <summary>
        /// The tangent vector that follows this knot. On a cubic Bezier curve, this value is used to calculate
        /// <see cref="BezierCurve.P1"/> when used as the first knot in a curve.
        /// </summary>
        public float3 TangentOut;

        /// <summary>
        /// The rotation of the knot in local space.
        /// </summary>
        public quaternion Rotation;

        /// <summary>
        /// Create a new BezierKnot struct.
        /// </summary>
        /// <param name="position">The position of the knot relative to the spline.</param>
        public BezierKnot(float3 position): this(position, 0f, 0f, quaternion.identity)
        {
        }

        /// <summary>
        /// Creates a new <see cref="BezierKnot"/> struct.
        /// </summary>
        /// <param name="position">The position of the knot relative to the spline.</param>
        /// <param name="tangentIn">The leading tangent to this knot.</param>
        /// <param name="tangentOut">The following tangent to this knot.</param>
        public BezierKnot(float3 position, float3 tangentIn, float3 tangentOut)
            : this(position, tangentIn, tangentOut, quaternion.identity)
        {
        }

        /// <summary>
        /// Create a new BezierKnot struct.
        /// </summary>
        /// <param name="position">The position of the knot relative to the spline.</param>
        /// <param name="tangentIn">The leading tangent to this knot.</param>
        /// <param name="tangentOut">The following tangent to this knot.</param>
        /// <param name="rotation">The rotation of the knot relative to the spline.</param>
        public BezierKnot(float3 position, float3 tangentIn, float3 tangentOut, quaternion rotation)
        {
            Position = position;
            TangentIn = tangentIn;
            TangentOut = tangentOut;
            Rotation = rotation;
        }

        /// <summary>
        /// Multiply the position and tangents by a matrix.
        /// </summary>
        /// <param name="matrix">The matrix to multiply.</param>
        /// <returns>A new BezierKnot multiplied by matrix.</returns>
        public BezierKnot Transform(float4x4 matrix)
        {
            var rotation = math.mul(new quaternion(matrix), Rotation);
            var invRotation = math.inverse(rotation);
            // Tangents need to be scaled, so rotation should be applied to them.
            // No need however to use the translation as this is only a direction.
            return new BezierKnot(
                math.transform(matrix, Position),
                math.rotate(invRotation, math.rotate(matrix, math.rotate(Rotation,TangentIn))),
                math.rotate(invRotation, math.rotate(matrix, math.rotate(Rotation,TangentOut))),
                rotation);
        }

        /// <summary>
        /// Adds a knot position. This operation applies only to the position and does not modify tangents or rotation.
        /// </summary>
        /// <param name="knot">The target knot.</param>
        /// <param name="rhs">The value to add.</param>
        /// <returns>A new BezierKnot where position is the sum of knot.position and rhs.</returns>
        public static BezierKnot operator +(BezierKnot knot, float3 rhs)
        {
            return new BezierKnot(knot.Position + rhs, knot.TangentIn, knot.TangentOut, knot.Rotation);
        }

        /// <summary>
        /// Subtracts a knot position. This operation applies only to the position and does not modify tangents or rotation.
        /// </summary>
        /// <param name="knot">The target knot.</param>
        /// <param name="rhs">The value to subtract.</param>
        /// <returns>A new BezierKnot where position is the sum of knot.position minus rhs.</returns>
        public static BezierKnot operator -(BezierKnot knot, float3 rhs)
        {
            return new BezierKnot(knot.Position - rhs, knot.TangentIn, knot.TangentOut, knot.Rotation);
        }

        internal BezierKnot BakeTangentDirectionToRotation(bool mirrored, BezierTangent main = BezierTangent.Out)
        {
            if (mirrored)
            {
                float lead = math.length(main == BezierTangent.In ? TangentIn : TangentOut);
                return new BezierKnot(Position,
                    new float3(0f, 0f, -lead),
                    new float3(0f, 0f,  lead),
                    SplineUtility.GetKnotRotation(
                        math.mul(Rotation, main == BezierTangent.In ? -TangentIn : TangentOut),
                        math.mul(Rotation, math.up())));
            }

            return new BezierKnot(Position,
                new float3(0, 0, -math.length(TangentIn)),
                new float3(0, 0, math.length(TangentOut)),
                Rotation = SplineUtility.GetKnotRotation(
                    math.mul(Rotation, main == BezierTangent.In ? -TangentIn : TangentOut),
                    math.mul(Rotation, math.up())));
        }

        /// <summary>
        /// See ISerializationCallbackReceiver.
        /// </summary>
        public void OnBeforeSerialize() {}

        /// <summary>
        /// See ISerializationCallbackReceiver.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // Ensures that when adding the first knot via Unity inspector
            // or when deserializing knot that did not have the rotation field prior,
            // rotation is deserialized to identity instead of (0, 0, 0, 0) which does not represent a valid rotation.
            if (math.lengthsq(Rotation) == 0f)
                Rotation = quaternion.identity;
        }

        /// <summary>
        /// Create a string with the values of this knot.
        /// </summary>
        /// <returns>A summary of the values contained by this knot.</returns>
        public override string ToString() => $"{{{Position}, {TangentIn}, {TangentOut}, {Rotation}}}";

        /// <summary>
        /// Compare two knots for equality.
        /// </summary>
        /// <param name="other">The knot to compare against.</param>
        /// <returns>Returns true when the position, tangents, and rotation of each knot are identical.</returns>
        public bool Equals(BezierKnot other)
        {
            return Position.Equals(other.Position)
                && TangentIn.Equals(other.TangentIn)
                && TangentOut.Equals(other.TangentOut)
                && Rotation.Equals(other.Rotation);
        }

        /// <summary>
        /// Compare against an object for equality.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns>
        /// Returns true when <paramref name="obj"/> is a <see cref="BezierKnot"/> and the values of each knot are
        /// identical.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is BezierKnot other && Equals(other);
        }

        /// <summary>
        /// Calculate a hash code for this knot.
        /// </summary>
        /// <returns>
        /// A hash code for the knot.
        /// </returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Position, TangentIn, TangentOut, Rotation);
        }
    }
}
