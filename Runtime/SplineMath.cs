using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Assorted utility functions for math equations commonly used when working with Splines.
    /// </summary>
    public static class SplineMath
    {
        /// <summary>
        /// Returns the parameterization of a ray line projection. The parameter will be negative if the nearest point
        /// between the ray/line is negative to 'lineOrigin', and greater than 1 if nearest intersection is past the end
        /// off the line segment (lineOrigin + lineDir).
        /// </summary>
        /// <param name="ro">The ray origin point.</param>
        /// <param name="rd">The ray direction (normalized vector).</param>
        /// <param name="lineOrigin">Line segment first point.</param>
        /// <param name="lineDir">Line segment direction (with magnitude).</param>
        /// <returns>The parameter of a ray line projection.</returns>
        public static float RayLineParameter(float3 ro, float3 rd, float3 lineOrigin, float3 lineDir)
        {
            var v0 = ro - lineOrigin;
            var v1 = math.cross(rd, math.cross(rd, lineDir));
            // the parameter of a ray to line projection will be negative if the intersection is negative to line
            // direction from 'a', and greater than 1 if intersection is past the line segment end 'b'
            return math.dot(v0, v1) / math.dot(lineDir, v1);
        }

        /// <summary>
        /// Returns the shortest distance between a ray and line segment as a direction and magnitude.
        /// </summary>
        /// <param name="ro">The ray origin point.</param>
        /// <param name="rd">The ray direction (normalized vector).</param>
        /// <param name="a">The line start point.</param>
        /// <param name="b">The line end point.</param>
        /// <returns>Returns the shortest distance between a ray and line segment as a direction and magnitude.</returns>
        public static float3 RayLineDistance(float3 ro, float3 rd, float3 a, float3 b)
        {
            var points = RayLineNearestPoint(ro, rd, a, b);
            return points.linePoint - points.rayPoint;
        }

        /// <summary>
        /// Returns the nearest points between a ray and line segment.
        /// </summary>
        /// <param name="ro">The ray origin point.</param>
        /// <param name="rd">The ray direction (normalized vector).</param>
        /// <param name="a">The line start point.</param>
        /// <param name="b">The line end point.</param>
        /// <returns>Returns the nearest points between a ray and line segment.</returns>
        public static (float3 rayPoint, float3 linePoint) RayLineNearestPoint(
            float3 ro,
            float3 rd,
            float3 a,
            float3 b)
        {
            return RayLineNearestPoint(ro, rd, a, b, out _, out _);
        }

        /// <summary>
        /// Returns the nearest points on a ray and a line segment to one another.
        /// </summary>
        /// <param name="ro">The ray origin point.</param>
        /// <param name="rd">The ray direction (normalized vector).</param>
        /// <param name="a">The line start point.</param>
        /// <param name="b">The line end point.</param>
        /// <param name="rayParam">The signed distance between point 'ro' and the projection of the line segment along the ray.</param>
        /// <param name="lineParam">The signed distance between point 'a' and the projection of point 'p' on the line segment.</param>
        /// <returns>Returns the nearest points on a ray and a line segment to one another.</returns>
        public static (float3 rayPoint, float3 linePoint) RayLineNearestPoint(
            float3 ro,
            float3 rd,
            float3 a,
            float3 b,
            out float rayParam,
            out float lineParam)
        {
            var lineDir = b - a;
            lineParam = RayLineParameter(ro, rd, a, lineDir);
            var linePoint = a + lineDir * math.saturate(lineParam);
            rayParam = math.dot(rd, linePoint - ro);
            var rayPoint = ro + rd * rayParam;
            return (rayPoint, linePoint);
        }

        /// <summary>
        /// Returns the nearest point on a finite line segment to a point.
        /// </summary>
        /// <param name="p">The point to compare to.</param>
        /// <param name="a">The line start point.</param>
        /// <param name="b">The line end point.</param>
        /// <param name="lineParam">The signed distance between point 'a' and the projection of point 'p' on the line segment.</param>
        /// <returns>The nearest point on a line segment to another point.</returns>
        public static float3 PointLineNearestPoint(float3 p, float3 a, float3 b, out float lineParam)
        {
            var dir = b - a;
            var len = math.length(dir);
            var nrm = math.select(0f, dir * (1f / len), len > math.FLT_MIN_NORMAL);
            lineParam = math.dot(nrm, p - a);
            return a + nrm * math.clamp(lineParam, 0f, len);
        }

        /// <summary>
        /// Gets the distance from a point to a line segment.
        /// </summary>
        /// <param name="p">The point to compare against.</param>
        /// <param name="a">The start point of the line segment.</param>
        /// <param name="b">The end point of the line segment.</param>
        /// <returns>Returns the distance of the closest line from a point to a line segment.</returns>
        public static float DistancePointLine(float3 p, float3 a, float3 b)
        {
            return math.length(PointLineNearestPoint(p, a, b, out _) - p);
        }

        internal static float GetUnitCircleTangentLength()
        {
            // https://mechanicalexpressions.com/explore/geometric-modeling/circle-spline-approximation.pdf
            return (4f * (math.sqrt(2f) - 1f)) / 3f;
        }
    }
}
