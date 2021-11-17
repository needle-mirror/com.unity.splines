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
            var knotCount = positions.Count;
            var spline = new Spline(knotCount, closed) { EditType = SplineType.Linear };

            for (int i = 0; i < knotCount; ++i)
            {
                var position = positions[i];
                var n = SplineUtility.NextIndex(i, knotCount, closed);
                var p = SplineUtility.PreviousIndex(i, knotCount, closed);

                var tangentIn = SplineUtility.GetLinearTangent(position, positions[p]);
                var tangentOut = SplineUtility.GetLinearTangent(position, positions[n]);

                spline.Add(new BezierKnot(position, tangentIn, tangentOut, quaternion.identity));
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

            return new Spline(new BezierKnot[]
            {
                new BezierKnot(p0 * radius, tanIn * rounding, tanOut * rounding, Quaternion.Euler(0f,  -45f, 0f)),
                new BezierKnot(p1 * radius, tanIn * rounding, tanOut * rounding, Quaternion.Euler(0f,   45f, 0f)),
                new BezierKnot(p2 * radius, tanIn * rounding, tanOut * rounding, Quaternion.Euler(0f,  135f, 0f)),
                new BezierKnot(p3 * radius, tanIn * rounding, tanOut * rounding, Quaternion.Euler(0f, -135f, 0f))
            }, true) { EditType = SplineType.Bezier };
        }

        /// <summary>
        /// Create a <see cref="Spline"/> in a square shape with sharp corners.
        /// </summary>
        /// <param name="radius">The distance from center to outermost edge.</param>
        /// <returns>A new Spline.</returns>
        public static Spline CreateSquare(float radius)
        {
            float3 p0 = new float3(-.5f, 0f, -.5f);
            float3 p1 = new float3(-.5f, 0f,  .5f);
            float3 p2 = new float3( .5f, 0f,  .5f);
            float3 p3 = new float3( .5f, 0f, -.5f);
            return CreateLinear(new float3[] { p0, p1, p2, p3 }, true);
        }
    }
}
