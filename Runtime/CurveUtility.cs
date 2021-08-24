using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A collection of methods for extracting information about <see cref="BezierCurve"/> types.
    /// </summary>
	public static class CurveUtility
    {
        /// <summary>
        /// Given a bezier curve, return an interpolated position at ratio t.
        /// </summary>
        /// <param name="curve">A cubic bezier curve.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>A position on the curve.</returns>
        public static float3 EvaluatePosition(BezierCurve curve,  float t)
        {
            t = math.clamp(t, 0, 1);
            float oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * oneMinusT * curve.P0 +
                   3f * oneMinusT * oneMinusT * t * curve.P1 +
                   3f * oneMinusT * t * t * curve.P2 +
                   t * t * t * curve.P3;
        }

        /// <summary>
        /// Given a bezier curve, return an interpolated position at ratio t.
        /// </summary>
        /// <param name="curve">A cubic bezier curve.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>A position on the curve.</returns>
        static float3 DeCasteljau(BezierCurve curve, float t)
        {
            float3 p0 = curve.P0, p1 = curve.P1;
            float3 p2 = curve.P2, p3 = curve.P3;

            float3 a0 = math.lerp(p0, p1, t);
            float3 a1 = math.lerp(p1, p2, t);
            float3 a2 = math.lerp(p2, p3, t);
            float3 b0 = math.lerp(a0, a1, t);
            float3 b1 = math.lerp(a1, a2, t);

            return math.lerp(b0, b1, t);
        }
        
        /// <summary>
        /// Given a bezier curve, return an interpolated tangent at ratio t.
        /// </summary>
        /// <param name="curve">A cubic bezier curve.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>A tangent on the curve.</returns>
        public static float3 EvaluateTangent(BezierCurve curve, float t)
        {
            t = math.clamp(t, 0, 1);
            float oneMinusT = 1 - t;
            float oneMinusT2 = oneMinusT * oneMinusT;
            float t2 = t * t;
            return -3 * curve.P0 * oneMinusT2
                + 3 * curve.P1 * (oneMinusT2 - 2 * t * oneMinusT)
                + 3 * curve.P2 * (-t2 + oneMinusT * 2 * t)
                + 3 * curve.P3 * t2;
        }

        /// <summary>
        /// Calculate the length of a <see cref="BezierCurve"/> by unrolling the curve into linear segments and summing
        /// the lengths of the lines. This is equivalent to accessing <see cref="Spline.GetCurveLength"/>.
        /// </summary>
        /// <param name="curve">The <see cref="BezierCurve"/> to calculate length.</param>
        /// <returns>The sum length of a collection of linear segments fitting this curve.</returns>
        /// <seealso cref="ApproximateLength(BezierCurve)"/>
        public static float CalculateLength(BezierCurve curve)
        {
            const int resolution = 30;

            float magnitude = 0f;
            float3 prev = EvaluatePosition(curve, 0f);

            for (int i = 1; i < resolution; i++)
            {
                var point = EvaluatePosition(curve, i / (resolution - 1f));
                var dir = point - prev;
                magnitude += math.length(dir);
                prev = point;
            }

            return magnitude;
        }

        /// <summary>
        /// Calculate the approximate length of a <see cref="BezierCurve"/>. This is less accurate than
        /// <seealso cref="CalculateLength"/>, but can be significantly faster. Use this when accuracy is
        /// not paramount and the curve control points are changing frequently.
        /// </summary>
        /// <param name="curve">The <see cref="BezierCurve"/> to calculate length.</param>
        /// <returns>An estimate of the length of a curve.</returns>
        public static float ApproximateLength(BezierCurve curve)
        {
            float chord = math.length(curve.P3 - curve.P0);
            float net = math.length(curve.P0 - curve.P1) + math.length(curve.P2 - curve.P1) + math.length(curve.P3 - curve.P2);
            return (net + chord) / 2;
        }

        /// <summary>
        /// Decompose a curve into two smaller curves matching the source curve.
        /// </summary>
        /// <param name="curve">The source curve.</param>
        /// <param name="t">A mid-point on the source curve defining where the two smaller curves control points meet.</param>
        /// <param name="left">A curve from the source curve first control point to the mid-point, matching the curvature of the source curve.</param>
        /// <param name="right">A curve from the mid-point to the source curve fourth control point, matching the curvature of the source curve.</param>
        public static void Split(BezierCurve curve, float t, out BezierCurve left, out BezierCurve right)
        {
            t = math.clamp(t, 0f, 1f);

            // subdivide control points, first iteration
            float3 split0 = math.lerp(curve.P0, curve.P1, t);
            float3 split1 = math.lerp(curve.P1, curve.P2, t);
            float3 split2 = math.lerp(curve.P2, curve.P3, t);

            // subdivide control points, second iteration
            float3 split3 = math.lerp(split0, split1, t);
            float3 split4 = math.lerp(split1, split2, t);

            // subdivide control points, third iteration
            float3 split5 = math.lerp(split3, split4, t);

            left = new BezierCurve(curve.P0, split0, split3, split5);
            right = new BezierCurve(split5, split4, split2, curve.P3);
        }
	}
}
