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

                var tangentOut = math.rotate(
                    math.inverse(rotation), 
                    SplineUtility.GetAutoSmoothTangent(positions[p], positions[i], positions[n], SplineUtility.CatmullRomTension));
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
        /// Creates a <see cref="Spline"/> in the shape of a helix with a single revolution.
        /// </summary>
        /// <param name="radius">The distance from the center to the helix's curve.</param>
        /// <param name="height">The height of the helix shape.</param>
        /// <param name="revolutions">The number of revolutions the helix should have.</param> 
        /// <returns>A new Spline.</returns>
        public static Spline CreateHelix(float radius, float height, int revolutions)
        {
            revolutions = math.max(1, revolutions);
            var revHeight = height / revolutions;
            var alpha = 0.5f * math.PI;
            var p = revHeight / (2f * math.PI);
            var ax = radius * math.cos(alpha);
            var az = radius * math.sin(alpha);
            var b = p * alpha * (radius - ax) * (3f * radius - ax) / (az * (4f * radius - ax) * math.tan(alpha));
            
            var yOffset = revHeight * 0.25f;
            var p0 = new float3(ax,  -alpha * p + yOffset, -az);
            var p1 = new float3((4f * radius - ax) / 3f, -b + yOffset, -(radius - ax) * (3f * radius - ax) / (3f * az));
            var p2 = new float3((4f * radius - ax) / 3f, b + yOffset, (radius - ax) * (3f * radius - ax) / (3f * az)); 
            var p3 = new float3(ax, alpha * p + yOffset, az);

            Spline spline = new Spline();

            // Create the first two points and tangents forming the first half of the helix.
            var tangent = p1 - p0;
            var tangentLength = math.length(tangent);
            var tangentNorm = math.normalize(tangent);
            var normal = math.cross(math.cross(tangentNorm, math.up()), tangentNorm);
            spline.Add(new BezierKnot(p0, new float3(0f, 0f, -tangentLength),  new float3(0f, 0f, tangentLength), quaternion.LookRotation(tangentNorm, normal)));
            tangent = p3 - p2;
            tangentNorm = math.normalize(tangent);
            normal = math.cross(math.cross(tangentNorm, math.up()), tangentNorm);
            spline.Add(new BezierKnot(p3, new float3(0f, 0f, -tangentLength),  new float3(0f, 0f, tangentLength), quaternion.LookRotation(tangentNorm, normal)));

            // Rotate and offset the first half to form a full single revolution helix.
            var rotation = quaternion.AxisAngle(math.up(), math.radians(180f));
            yOffset = revHeight * 0.5f;
            p3 = math.rotate(rotation, p3);
            p3.y += yOffset;
            tangent = p1 - p0;
            tangentNorm = math.normalize(tangent);
            normal = math.cross(math.cross(tangentNorm, math.up()), tangentNorm);
            spline.Add(new BezierKnot(p3, new float3(0f, 0f, -tangentLength),  new float3(0f, 0f, tangentLength), quaternion.LookRotation(tangentNorm, normal)));
            
            // Create knots for remaining revolutions
            var revYOffset = new float3(0f, revHeight, 0f);
            for (int i = 1; i < revolutions; ++i)
            {
                var knotA = spline[^1];
                knotA.Position += revYOffset;
                var knotB = spline[^2];
                knotB.Position += revYOffset;
                
                spline.Add(knotB);
                spline.Add(knotA);
            }
            
            return spline;
        }
        
        /// <summary>
        /// Creates a <see cref="Spline"/> in the shape of a square with circular arcs at its corners.
        /// </summary>
        /// <param name="size">The size of the square's edges.</param>
        /// <param name="cornerRadius">The radius of the circular arcs at the corners of the shape.
        /// A value of 0 creates a square with no rounding. A value that is half of <paramref name="size"/> creates a circle.</param>
        /// <remarks>The range for <paramref name="cornerRadius"/> is 0 and half of <paramref name="size"/>.</remarks>
        /// <returns>A new Spline.</returns>
        public static Spline CreateRoundedCornerSquare(float size, float cornerRadius)
        {
            float radius = size * 0.5f;
            cornerRadius = math.clamp(cornerRadius, 0f, radius);
            if (cornerRadius == 0f)
                return CreateSquare(size);

            float3 cornerP0 = new float3(-radius, 0f, radius - cornerRadius);
            float3 cornerP1 = new float3(-radius + cornerRadius, 0f, radius);
            float cornerTangentLen = SplineMath.GetUnitCircleTangentLength() * cornerRadius;
            float cornerAngle = 0f;
            
            var spline = new Spline();
            for (int i = 0; i < 4; i++)
            {
                var rotation = Quaternion.Euler(0f, cornerAngle, 0f);
                if (cornerRadius < 1f)
                {
                    spline.Add(new BezierKnot(rotation * cornerP0, new float3(0f, 0f, -math.min(cornerTangentLen, 0f)), new float3(0f, 0f, cornerTangentLen), Quaternion.identity * rotation));
                    spline.Add(new BezierKnot(rotation * cornerP1, new float3(0f, 0f, -cornerTangentLen), new float3(0f, 0f, math.min(cornerTangentLen, 0f)), Quaternion.Euler(0f, 90f, 0f) * rotation));
                }
                else
                    spline.Add(new BezierKnot(rotation * cornerP0, new float3(0f, 0f, -cornerTangentLen), new float3(0f, 0f, cornerTangentLen), Quaternion.identity * rotation));

                cornerAngle += 90f;
            }

            spline.Closed = true;
            return spline;
        }

        /// <summary>
        /// Creates a <see cref="Spline"/> in the shape of a square with sharp corners. 
        /// </summary>
        /// <param name="size">The size of the square's edges.</param>
        /// <returns>A new Spline.</returns>
        public static Spline CreateSquare(float size)
        {
            float3 p0 = new float3(-.5f, 0f, -.5f) * size;
            float3 p1 = new float3(-.5f, 0f,  .5f) * size;
            float3 p2 = new float3( .5f, 0f,  .5f) * size;
            float3 p3 = new float3( .5f, 0f, -.5f) * size;
            return CreateLinear(new float3[] { p0, p1, p2, p3 }, true);
        }
        
        /// <summary>
        /// Creates a <see cref="Spline"/> in the shape of a circle.
        /// </summary>
        /// <param name="radius">The radius of the circle.</param>
        /// <returns>A new Spline.</returns>
        public static Spline CreateCircle(float radius)
        {
            float3 point = new float3(-radius, 0f, 0);
            float3 tangent = new float3(0f, 0f, SplineMath.GetUnitCircleTangentLength() * radius);

            var spline = new Spline();
            quaternion rotation = quaternion.identity;
            for (int i = 0; i < 4; i++)
            {
                spline.Add(new BezierKnot(math.rotate(rotation, point), -tangent, tangent, rotation));
                rotation = math.mul(rotation, quaternion.AxisAngle(math.up(), math.PI * 0.5f));
            }
            spline.Closed = true;

            return spline;
        }

        /// <summary>
        /// Creates a <see cref="Spline"/> in the shape of a polygon with a specific number of sides.
        /// </summary>
        /// <param name="edgeSize">The size of the polygon's edges.</param>
        /// <param name="sides">The amount of sides the polygon has.</param>
        /// <returns>A new Spline.</returns>
        public static Spline CreatePolygon(float edgeSize, int sides)
        {
            sides = math.max(3, sides);
            
            var points = new float3[sides];
            var angleStep = 2f * math.PI / sides;
            var radius = edgeSize * 0.5f / math.sin(angleStep * 0.5f);
            var point = new float3(0f, 0f, radius);
            var rotation = quaternion.identity;

            for (int i = 0; i < sides; ++i)
            {
                points[i] = math.rotate(rotation, point);
                rotation = math.mul(rotation, quaternion.AxisAngle(math.up(), angleStep));
            }

            return CreateLinear(points, true);
        }
        
        /// <summary>
        /// Creates a <see cref="Spline"/> in in the shape of a star with a specified number of corners.
        /// </summary>
        /// <param name="edgeSize">The distance between the corners of the star.</param>
        /// <param name="corners">The amount of corners the star has.</param>
        /// <param name="concavity">The sharpness of the corners. The range is 0 through 1. </param>
        /// <returns>A new Spline.</returns>
        public static Spline CreateStarPolygon(float edgeSize, int corners, float concavity)
        {
            concavity = math.clamp(concavity, 0f, 1f);
            if (concavity == 0f)
                CreatePolygon(edgeSize, corners);
            
            corners = math.max(3, corners);

            var sidesDouble = corners * 2;
            var points = new float3[sidesDouble];
            var angleStep = 2f * math.PI / corners;
            var radius = edgeSize * 0.5f / math.sin(angleStep * 0.5f);
            var point = new float3(0f, 0f, radius);
            var rotation = quaternion.identity;

            for (int i = 0; i < sidesDouble; i+= 2)
            {
                points[i] = math.rotate(rotation, point);
                rotation = math.mul(rotation, quaternion.AxisAngle(math.up(), angleStep));

                if (i != 0)
                    points[i - 1] = (points[i - 2] + points[i]) * 0.5f * (1f - concavity);
                if (i == sidesDouble - 2)
                    points[i + 1] = (points[0] + points[i]) * 0.5f * (1f - concavity);
            }

            return CreateLinear(points, true);
        }
    }
}
