using System.Collections.Generic;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A collection of methods for extracting information about <see cref="BezierCurve"/> types.
    /// </summary>
	public static class CurveUtility
    {
        struct FrenetFrame
        {
            public float3 origin;
            public float3 tangent;
            public float3 normal;
            public float3 binormal;
        }

        const int k_NormalsPerCurve = 16;
        static readonly float3[] s_NormalBuffer = new float3[k_NormalsPerCurve];

        /// <summary>
        /// Given a bezier curve, return an interpolated position at ratio t.
        /// </summary>
        /// <param name="curve">A cubic bezier curve.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>A position on the curve.</returns>
        public static float3 EvaluatePosition(BezierCurve curve,  float t)
        {
            t = math.clamp(t, 0, 1);
            var t2 = t * t;
            var t3 = t2 * t;
            var position =
                curve.P0 * ( -1f * t3 + 3f * t2 - 3f * t + 1f ) +
                curve.P1 * (  3f * t3 - 6f * t2 + 3f * t) +
                curve.P2 * ( -3f * t3 + 3f * t2) +
                curve.P3 * (       t3 );

            return position;
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
            float t2 = t * t;

            var tangent =
                curve.P0 * ( -3f * t2 +  6f * t - 3f ) +
                curve.P1 * (  9f * t2 - 12f * t + 3f) +
                curve.P2 * ( -9f * t2 +  6f * t ) +
                curve.P3 * (  3f * t2 );

            return tangent;
        }

        /// <summary>
        /// Given a bezier curve, return an interpolated acceleration at ratio t.
        /// </summary>
        /// <param name="curve">A cubic bezier curve.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>An acceleration vector on the curve.</returns>
        public static float3 EvaluateAcceleration(BezierCurve curve,  float t)
        {
            t = math.clamp(t, 0, 1);

            var acceleration =
                curve.P0 * ( -6f * t + 6f ) +
                curve.P1 * ( 18f * t - 12f) +
                curve.P2 * (-18f * t + 6f ) +
                curve.P3 * (  6f * t );

            return acceleration;
        }

        /// <summary>
        /// Given a bezier curve, return an interpolated curvature at ratio t.
        /// </summary>
        /// <param name="curve">A cubic bezier curve.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>A curvature value on the curve.</returns>
        public static float EvaluateCurvature(BezierCurve curve, float t)
        {
            t = math.clamp(t, 0, 1);

            var firstDerivative = EvaluateTangent(curve, t);
            var secondDerivative = EvaluateAcceleration(curve, t);
            var firstDerivativeNormSq = math.lengthsq(firstDerivative);
            var secondDerivativeNormSq = math.lengthsq(secondDerivative);
            var derivativesDot = math.dot(firstDerivative, secondDerivative);

            var kappa = math.sqrt(
                    ( firstDerivativeNormSq * secondDerivativeNormSq ) - ( derivativesDot * derivativesDot ))
                / ( firstDerivativeNormSq * math.length(firstDerivative));

            return kappa;
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

        /// <summary>
        /// Calculate the length of a <see cref="BezierCurve"/> by unrolling the curve into linear segments and summing
        /// the lengths of the lines. This is equivalent to accessing <see cref="Spline.GetCurveLength"/>.
        /// </summary>
        /// <param name="curve">The <see cref="BezierCurve"/> to calculate length.</param>
        /// <param name="resolution">The number of linear segments used to calculate the curve length.</param>
        /// <returns>The sum length of a collection of linear segments fitting this curve.</returns>
        /// <seealso cref="ApproximateLength(BezierCurve)"/>
        public static float CalculateLength(BezierCurve curve, int resolution = 30)
        {
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
        /// Populate a pre-allocated lookupTable array with distance to 't' values. The number of table entries is
        /// dependent on the size of the passed lookupTable.
        /// </summary>
        /// <param name="curve">The <see cref="BezierCurve"/> to create a distance to 't' lookup table for.</param>
        /// <param name="lookupTable">A pre-allocated array to populate with distance to interpolation ratio data.</param>
        public static void CalculateCurveLengths(BezierCurve curve, DistanceToInterpolation[] lookupTable)
        {
            var resolution = lookupTable.Length;

            float magnitude = 0f;
            float3 prev = EvaluatePosition(curve, 0f);
            lookupTable[0] = new DistanceToInterpolation() { Distance = 0f , T = 0f };

            for (int i = 1; i < resolution; i++)
            {
                var t = i / ( resolution - 1f );
                var point = EvaluatePosition(curve, t);
                var dir = point - prev;
                magnitude += math.length(dir);
                lookupTable[i] = new DistanceToInterpolation() { Distance = magnitude , T = t};
                prev = point;
            }
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

        internal static float3 EvaluateUpVector(BezierCurve curve, float t, float3 startUp, float3 endUp)
        {
            // Ensure we have workable tangents by linearizing ones that are of zero length
            var linearTangentLen = math.length(SplineUtility.GetLinearTangent(curve.P0, curve.P3));
            var linearTangentOut = math.normalize(curve.P3 - curve.P0) * linearTangentLen;
            if (Mathf.Approximately(math.length(curve.P0 - curve.P1), 0f))
                curve.P1 = curve.P0 + linearTangentOut;
            if (Mathf.Approximately(math.length(curve.P2 - curve.P3), 0f))
                curve.P2 = curve.P3 - linearTangentOut;

            // Construct initial frenet frame
            FrenetFrame frame;
            frame.origin = curve.P0;
            frame.tangent = curve.P1 - curve.P0;
            frame.normal = startUp;
            frame.binormal = math.normalize(math.cross(frame.tangent, frame.normal));
            s_NormalBuffer[0] = frame.normal;

            // Continue building remaining rotation minimizing frames
            var stepSize = 1f / (k_NormalsPerCurve - 1);
            var currentT = stepSize;
            var prevT = 0f;
            var upVector = float3.zero;
            FrenetFrame prevFrame;
            for (int i = 1; i < k_NormalsPerCurve; ++i)
            {
                prevFrame = frame;
                frame = GetNextRotationMinimizingFrame(curve, prevFrame, currentT);
                s_NormalBuffer[i] = frame.normal;

                if (prevT <= t && currentT >= t)
                {
                    var lerpT = (t - prevT) / stepSize;
                    upVector = Vector3.Slerp(prevFrame.normal, frame.normal, lerpT);
                }
                prevT = currentT;
                currentT += stepSize;
            }

            if (prevT <= t && currentT >= t)
                upVector = endUp;

            var lastFrameNormal = s_NormalBuffer[k_NormalsPerCurve - 1];

            var angleBetweenNormals = math.acos(math.clamp(math.dot(lastFrameNormal, endUp), -1f, 1f));
            if (angleBetweenNormals == 0f)
                return upVector;

            // Since there's an angle difference between the end knot's normal and the last evaluated frenet frame's normal,
            // the remaining code gradually applies the angle delta across the evaluated frames' normals.
            var lastNormalTangent = math.normalize(frame.tangent);
            var positiveRotation = quaternion.AxisAngle(lastNormalTangent, angleBetweenNormals);
            var negativeRotation = quaternion.AxisAngle(lastNormalTangent, -angleBetweenNormals);
            var positiveRotationResult = math.acos(math.clamp(math.dot(math.rotate(positiveRotation, endUp), lastFrameNormal), -1f, 1f));
            var negativeRotationResult = math.acos(math.clamp(math.dot(math.rotate(negativeRotation, endUp), lastFrameNormal), -1f, 1f));

            if (positiveRotationResult > negativeRotationResult)
                angleBetweenNormals *= -1f;

            currentT = stepSize;
            prevT = 0f;
            for (int i = 1; i < s_NormalBuffer.Length; i++)
            {
                var normal = s_NormalBuffer[i];
                var adjustmentAngle = math.lerp(0f, angleBetweenNormals, currentT);
                var tangent = math.normalize(CurveUtility.EvaluateTangent(curve, currentT));
                var adjustedNormal = math.rotate(quaternion.AxisAngle(tangent, -adjustmentAngle), normal);

                s_NormalBuffer[i] = adjustedNormal;

                // Early exit if we've already adjusted the normals at offsets that curveT is in between
                if (prevT <= t && currentT >= t)
                {
                    var lerpT = (t - prevT) / stepSize;
                    upVector = Vector3.Slerp(s_NormalBuffer[i - 1], s_NormalBuffer[i], lerpT);

                    return upVector;
                }

                prevT = currentT;
                currentT += stepSize;
            }

            return endUp;
        }

        static FrenetFrame GetNextRotationMinimizingFrame(BezierCurve curve, FrenetFrame previousRMFrame, float nextRMFrameT)
        {
            FrenetFrame nextRMFrame;

            // Evaluate position and tangent for next RM frame
            nextRMFrame.origin = CurveUtility.EvaluatePosition(curve, nextRMFrameT);
            nextRMFrame.tangent = CurveUtility.EvaluateTangent(curve, nextRMFrameT);

            // Mirror the rotational axis and tangent
            float3 toCurrentFrame = nextRMFrame.origin - previousRMFrame.origin;
            float c1 = math.dot(toCurrentFrame, toCurrentFrame);
            float3 riL = previousRMFrame.binormal - toCurrentFrame * 2f / c1 * math.dot(toCurrentFrame, previousRMFrame.binormal);
            float3 tiL = previousRMFrame.tangent - toCurrentFrame * 2f / c1 * math.dot(toCurrentFrame, previousRMFrame.tangent);

            // Compute a more stable binormal
            float3 v2 = nextRMFrame.tangent - tiL;
            float c2 = math.dot(v2, v2);

            // Fix binormal's axis
            nextRMFrame.binormal = math.normalize(riL - v2 * 2f / c2 * math.dot(v2, riL));
            nextRMFrame.normal = math.normalize(math.cross(nextRMFrame.binormal, nextRMFrame.tangent));

            return nextRMFrame;
        }

        /// <summary>
        /// Return the normalized interpolation (t) corresponding to a distance on a <see cref="BezierCurve"/>. This
        /// method accepts a look-up table (referred to in code with acronym "LUT") that may be constructed using
        /// <see cref="CalculateCurveLengths"/>. The built-in Spline class implementations (<see cref="Spline"/> and
        /// <see cref="NativeSpline"/>) cache these look-up tables internally.
        /// </summary>
        /// <typeparam name="T">The collection type.</typeparam>
        /// <param name="lut">A look-up table of distance to 't' values. See <see cref="CalculateCurveLengths"/> for creating
        /// this collection.</param>
        /// <param name="distance">The curve-relative distance to convert to an interpolation ratio (also referred to as 't').</param>
        /// <returns>  The normalized interpolation ratio associated to distance on the designated curve.</returns>
        public static float GetDistanceToInterpolation<T>(T lut, float distance) where T : IReadOnlyList<DistanceToInterpolation>
        {
            if(lut == null || lut.Count < 1 || distance <= 0)
                return 0f;

            var resolution = lut.Count;
            var curveLength = lut[resolution-1].Distance;

            if(distance >= curveLength)
                return 1f;

            var prev = lut[0];

            for(int i = 1; i < resolution; i++)
            {
                var current = lut[i];
                if(distance < current.Distance)
                    return math.lerp(prev.T, current.T, (distance - prev.Distance) / (current.Distance - prev.Distance));
                prev = current;
            }

            return 1f;
        }
    }
}
