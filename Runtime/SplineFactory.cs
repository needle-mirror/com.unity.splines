using System.Collections.Generic;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Methods to create spline shapes.
    /// </summary>
    public static class SplineFactory
    {
        /// <summary>
        /// Create a <see cref="Spline"/> from a list of positions.
        /// </summary>
        /// <param name="positions">A collection of knot positions.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        /// <returns>A new Spline.</returns>
        public static Spline CreateLinear(IList<float3> positions, bool closed = false)
        {
            return CreateLinear(positions, null, closed);
        }

        /// <summary>
        /// Create a <see cref="Spline"/> from a list of positions.
        /// </summary>
        /// <param name="positions">A collection of knot positions.</param>
        /// <param name="rotations">A collection of knot rotations. Must be equal in length to the positions array.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        /// <returns>A new Spline.</returns>
        public static Spline CreateLinear(IList<float3> positions, IList<quaternion> rotations, bool closed = false)
        {
            var knotCount = positions.Count;
            var spline = new Spline(knotCount, closed);

            for (int i = 0; i < knotCount; ++i)
            {
                var position = positions[i];
                var rotation = rotations?[i] ?? quaternion.identity;
                var tangentIn = float3.zero;
                var tangentOut = float3.zero;

                spline.Add(new BezierKnot(position, tangentIn, tangentOut, rotation), TangentMode.Linear);
            }

            return spline;
        }

        /// <summary>
        /// Create a <see cref="Spline"/> from a list of positions and place tangents to create Catmull Rom curves.
        /// </summary>
        /// <param name="positions">A collection of knot positions.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        /// <returns>A new Spline.</returns>
        public static Spline CreateCatmullRom(IList<float3> positions, bool closed = false)
        {
            return CreateCatmullRom(positions, null, closed);
        }

        /// <summary>
        /// Create a <see cref="Spline"/> from a list of positions and place tangents to create Catmull Rom curves.
        /// </summary>
        /// <param name="positions">A collection of knot positions.</param>
        /// <param name="rotations">A collection of knot rotations. Must be equal in length to the positions array.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        /// <returns>A new Spline.</returns>
        internal static Spline CreateCatmullRom(IList<float3> positions, IList<quaternion> rotations, bool closed = false)
        {
            var knotCount = positions.Count;
            var spline = new Spline(knotCount, closed);

            for (int i = 0; i < knotCount; ++i)
            {
                var position = positions[i];
                var rotation = rotations?[i] ?? quaternion.identity;
                var n = SplineUtility.NextIndex(i, knotCount, closed);
                var p = SplineUtility.PreviousIndex(i, knotCount, closed);

                var tangentOut = math.rotate(math.inverse(rotation), SplineUtility.GetCatmullRomTangent(positions[p], positions[n]));
                var tangentIn = -tangentOut;
                spline.Add(new BezierKnot(position, tangentIn, tangentOut, rotation), TangentMode.AutoSmooth);
            }

            return spline;
        }

        /// <summary>
        /// Create a <see cref="Spline"/> in a square shape with rounding at the edges.
        /// </summary>
        /// <param name="radius">The distance from center to outermost edge.</param>
        /// <param name="rounding">The amount of rounding to apply to corners.</param>
        /// <returns>A new Spline.</returns>
        public static Spline CreateRoundedSquare(float radius, float rounding)
        {
            float3 p0 = new float3(-.5f, 0f, -.5f);
            float3 p1 = new float3(-.5f, 0f,  .5f);
            float3 p2 = new float3( .5f, 0f,  .5f);
            float3 p3 = new float3( .5f, 0f, -.5f);
            float3 tanIn  = new float3(0f, 0f, -1f);
            float3 tanOut = new float3(0f, 0f,  1f);

            var spline = new Spline(new BezierKnot[]
            {
                new BezierKnot(p0 * radius, tanIn * rounding, tanOut * rounding, Quaternion.Euler(0f, -45f, 0f)),
                new BezierKnot(p1 * radius, tanIn * rounding, tanOut * rounding, Quaternion.Euler(0f, 45f, 0f)),
                new BezierKnot(p2 * radius, tanIn * rounding, tanOut * rounding, Quaternion.Euler(0f, 135f, 0f)),
                new BezierKnot(p3 * radius, tanIn * rounding, tanOut * rounding, Quaternion.Euler(0f, -135f, 0f))
            }, true);

            for (int i = 0; i < spline.Count; ++i)
                spline.SetTangentMode(i, TangentMode.Mirrored);

            return spline;
        }

        /// <summary>
        /// Create a <see cref="Spline"/> in a square shape with sharp corners.
        /// </summary>
        /// <param name="radius">The distance from center to outermost edge.</param>
        /// <returns>A new Spline.</returns>
        public static Spline CreateSquare(float radius)
        {
            float3 p0 = new float3(-.5f, 0f, -.5f) * radius;
            float3 p1 = new float3(-.5f, 0f,  .5f) * radius;
            float3 p2 = new float3( .5f, 0f,  .5f) * radius;
            float3 p3 = new float3( .5f, 0f, -.5f) * radius;
            return CreateLinear(new float3[] { p0, p1, p2, p3 }, true);
        }
    }
}
