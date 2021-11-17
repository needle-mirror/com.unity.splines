using System;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// This struct contains position and tangent data for a knot.
    /// The <see cref="Spline"/> class stores a collection of BezierKnot that form a series of connected
    /// <see cref="BezierCurve"/>. Each knot contains a Position, Tangent In, and Tangent Out. When a Spline is not
    /// closed, the first and last knots will contain an extraneous tangent (in and out, respectively).
    /// </summary>
    [Serializable]
    public struct BezierKnot: ISerializationCallbackReceiver
    {
        /// <summary>
        /// The position of the knot. On a cubic bezier curve, this is equivalent to <see cref="BezierCurve.P0"/> or
        /// <see cref="BezierCurve.P3"/>, depending on whether this knot is forming the first or second control point
        /// of the curve.
        /// </summary>
        public float3 Position;

        /// <summary>
        /// The tangent leading into this knot. On a cubic bezier curve, this value is used to calculate
        /// <see cref="BezierCurve.P2"/> when used as the second knot in a curve.
        /// </summary>
        public float3 TangentIn;

        /// <summary>
        /// The tangent following this knot. On a cubic bezier curve, this value is used to calculate
        /// <see cref="BezierCurve.P1"/> when used as the first knot in a curve.
        /// </summary>
        public float3 TangentOut;

        /// <summary>
        /// Rotation of the knot.
        /// </summary>
        public quaternion Rotation;

        /// <summary>
        /// Create a new BezierKnot struct.
        /// </summary>
        /// <param name="position">The position of the knot relative to the spline.</param>
        public BezierKnot(float3 position)
        {
            Position = position;
            TangentIn = float3.zero;
            TangentOut = float3.zero;
            Rotation = quaternion.identity;
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
        /// Knot position addition. This operation only applies to the position, tangents and rotation are unmodified.
        /// </summary>
        /// <param name="knot">The target knot.</param>
        /// <param name="rhs">The value to add.</param>
        /// <returns>A new BezierKnot where position is the sum of knot.position and rhs.</returns>
        public static BezierKnot operator +(BezierKnot knot, float3 rhs)
        {
            return new BezierKnot(knot.Position + rhs, knot.TangentIn, knot.TangentOut, knot.Rotation);
        }

        /// <summary>
        /// Knot position subtraction. This operation only applies to the position, tangents and rotation are unmodified.
        /// </summary>
        /// <param name="knot">The target knot.</param>
        /// <param name="rhs">The value to subtract.</param>
        /// <returns>A new BezierKnot where position is the sum of knot.position minus rhs.</returns>
        public static BezierKnot operator -(BezierKnot knot, float3 rhs)
        {
            return new BezierKnot(knot.Position - rhs, knot.TangentIn, knot.TangentOut, knot.Rotation);
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
    }
}
