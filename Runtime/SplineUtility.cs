using Unity.Mathematics;
using System;
using Unity.Collections;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A collection of methods for extracting information about <see cref="Spline"/> types.
    /// </summary>
    public static class SplineUtility
    {
        const int k_ResolutionSegmentCountMin = 6;
        const int k_ResolutionSegmentCountMax = 1024;
        
        /// <summary>
        /// The minimum resolution allowable when unrolling a curve to hit test while picking (selecting a spline with a cursor).
        /// 
        /// Pick resolution is used when determining how many segments are required to unroll a curve. Unrolling is the
        /// process of calculating a series of line segments to approximate a curve. Some functions in SplineUtility
        /// allow you to specify a resolution. Lower resolution means fewer segments, while higher resolutions result
        /// in more segments. Use lower resolutions where performance is critical and accuracy is not paramount. Use
        /// higher resolution where a fine degree of accuracy is necessary and performance is less important.
        /// </summary>
        public const int PickResolutionMin = 2;
        
        /// <summary>
        /// The default resolution used when unrolling a curve to hit test while picking (selecting a spline with a cursor).
        /// 
        /// Pick resolution is used when determining how many segments are required to unroll a curve. Unrolling is the
        /// process of calculating a series of line segments to approximate a curve. Some functions in SplineUtility
        /// allow you to specify a resolution. Lower resolution means fewer segments, while higher resolutions result
        /// in more segments. Use lower resolutions where performance is critical and accuracy is not paramount. Use
        /// higher resolution where a fine degree of accuracy is necessary and performance is less important.
        /// </summary>
        public const int PickResolutionDefault = 4;

        /// <summary>
        /// The maximum resolution allowed when unrolling a curve to hit test while picking (selecting a spline with a cursor).
        /// 
        /// Pick resolution is used when determining how many segments are required to unroll a curve. Unrolling is the
        /// process of calculating a series of line segments to approximate a curve. Some functions in SplineUtility
        /// allow you to specify a resolution. Lower resolution means fewer segments, while higher resolutions result
        /// in more segments. Use lower resolutions where performance is critical and accuracy is not paramount. Use
        /// higher resolution where a fine degree of accuracy is necessary and performance is less important.
        /// </summary>
        public const int PickResolutionMax = 64;

        /// <summary>
        /// The default resolution used when unrolling a curve to draw a preview in the Scene View.
        /// 
        /// Pick resolution is used when determining how many segments are required to unroll a curve. Unrolling is the
        /// process of calculating a series of line segments to approximate a curve. Some functions in SplineUtility
        /// allow you to specify a resolution. Lower resolution means fewer segments, while higher resolutions result
        /// in more segments. Use lower resolutions where performance is critical and accuracy is not paramount. Use
        /// higher resolution where a fine degree of accuracy is necessary and performance is less important.
        /// </summary>
        public const int DrawResolutionDefault = 10;
        
        /// <summary>
        /// Return an interpolated position at ratio t.
        /// </summary>
        /// <param name="spline">The spline to interpolate.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>A position on the spline.</returns>
        public static float3 EvaluatePosition<T>(this T spline, float t) where T : ISpline
        {
            if (spline.KnotCount < 1)
                return float.PositiveInfinity;
            var curveIndex = spline.GetCurve(SplineToCurveInterpolation(spline, t, out var curveT));
            return CurveUtility.EvaluatePosition(curveIndex, curveT);
        }

        /// <summary>
        /// Return an interpolated direction at ratio t.
        /// </summary>
        /// <param name="spline">The spline to interpolate.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>A direction on the spline.</returns>
        public static float3 EvaluateDirection<T>(this T spline, float t) where T : ISpline
        {
            if (spline.KnotCount < 1)
                return float.PositiveInfinity;
            var curveIndex = SplineToCurveInterpolation(spline, t, out float segmentT);
            return CurveUtility.EvaluateTangent(spline.GetCurve(curveIndex), segmentT);
        }

        /// <summary>
        /// Evaluate an up vector of a spline at a specific t
        /// </summary>
        /// <param name="spline">The <seealso cref="NativeSpline"/> to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of the curve.</param>
        /// <returns>An up vector</returns>
        public static float3 EvaluateUpVector<T>(this T spline, float t) where T : ISpline
        {
            if (spline.KnotCount < 1)
                return 0;

            var knotIndex = math.max(0, SplineToCurveInterpolation(spline, t, out float curveT));
            var nextKnotIndex = math.min(spline.KnotCount - 1, spline.Closed ? (knotIndex + 1) % spline.KnotCount : knotIndex + 1);
            var rotationT = math.nlerp(spline[knotIndex].Rotation, spline[nextKnotIndex].Rotation, curveT);
            return math.rotate(rotationT, math.up());
        }

        internal static float3 GetContinuousTangent(float3 otherTangent, float3 tangentToAlign)
        {
            // Mirror tangent but keep the same length
            float3 dir = -math.normalize(otherTangent);

            float tangentToAlignLength = math.length(tangentToAlign);
            return dir * tangentToAlignLength;
        }

        /// <summary>
        /// Given a normalized interpolation (t) for a spline, calculate the curve index and curve-relative
        /// normalized interpolation.
        /// </summary>
        /// <param name="spline">The target spline.</param>
        /// <param name="splineT">A normalized spline interpolation value to be converted into curve space.</param>
        /// <param name="curveT">A normalized curve interpolation value.</param>
        /// <returns>The curve index.</returns>
        public static int SplineToCurveInterpolation<T>(this T spline, float splineT, out float curveT) where T : ISpline
        {
            splineT = math.clamp(splineT, 0, 1);
            var tLength = splineT * spline.GetLength();
            var start = 0f;

            var closed = spline.Closed;
            var knotCount = spline.KnotCount;

            for (int i = 0, c = closed ? knotCount : knotCount - 1; i < c; i++)
            {
                var index = i % knotCount;
                var curveLength = spline.GetCurveLength(index);

                if (tLength <= (start + curveLength))
                {
                    curveT = (tLength - start) / curveLength;
                    return index;
                }

                start += curveLength;
            }

            curveT = 1;
            return closed ? knotCount - 1 : knotCount - 2;
        }

        /// <summary>
        /// Given an interpolation value for a curve, calculate the relative normalized spline interpolation.
        /// </summary>
        /// <param name="spline">The target spline.</param>
        /// <param name="curve">A curve index and normalized interpolation. The curve index is represented by the
        /// integer part of the float, and interpolation is the fractional part. This is the format used by
        /// <seealso cref="PathIndexUnit.Knot"/>.
        /// </param>
        /// <returns>An interpolation value relative to normalized Spline length (0 to 1).</returns>
        /// <seealso cref="SplineToCurveInterpolation{T}"/>
        public static float CurveToSplineInterpolation<T>(T spline, float curve) where T : ISpline
        {
            var curveIndex = (int) math.floor(curve);

            float t = 0f;
            
            for (int i = 0; i < curveIndex; i++)
                t += spline.GetCurveLength(i);

            t += spline.GetCurveLength(curveIndex) * math.frac(curve);

            return t / spline.GetLength();
        }
        
        /// <summary>
        /// Calculate the length of a spline when transformed by a matrix.
        /// </summary>
        /// <param name="spline"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static float CalculateLength(Spline spline, float4x4 transform)
        {
            using var nativeSpline = spline.ToNativeSpline(transform);
            return nativeSpline.GetLength();
        }

        /// <summary>
        /// Calculate the bounding box of a Spline.
        /// </summary>
        /// <param name="spline">The spline for which to calculate bounds.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>The bounds of a spline.</returns>
        public static Bounds GetBounds<T>(T spline) where T : ISpline
        {
            if (spline.KnotCount < 1)
                return default;
            
            var knot = spline[0];
            Bounds bounds = new Bounds(knot.Position, Vector3.zero);
            
            for (int i = 1, c = spline.KnotCount; i < c; ++i)
            {
                knot = spline[i];
                bounds.Encapsulate(knot.Position);
            }

            return bounds;
        }

        // Get the point on a line segment at the smallest distance to intersection
        static float3 RayLineSegmentNearestPoint(float3 rayOrigin, float3 rayDir, float3 s0, float3 s1, out float t)
        {
            float3 am = s1 - s0;
            float al = math.length(am);
            float3 ad = (1f / al) * am;
            float dot = math.dot(ad, rayDir);

            if (1f - math.abs(dot) < Mathf.Epsilon)
            {
                t = 0f;
                return s0;
            }

            float3 c = rayOrigin - s0;
            float rm = math.dot(rayDir, rayDir);
            float n = -dot * math.dot(rayDir, c) + math.dot(ad, c) * rm;
            float d = math.dot(ad, ad) * rm - dot * dot;
            float mag = math.min(al, math.max(0f, n / d));
            t = mag / al;
            return s0 + ad * mag;
        }

        static float3 PointLineSegmentNearestPoint(float3 p, float3 a, float3 b, out float t)
        {
            float l2 = math.lengthsq(b - a);

            if (l2 == 0.0)
            {
                t = 0f;
                return a;
            }

            t = math.dot(p - a, b - a) / l2;

            if (t < 0.0)
                return a;
            if (t > 1.0)
                return b;

            return a + t * (b - a);
        }

        // Same as ProjectPointLine but without clamping.
        static float3 ProjectPointRay(float3 point, float3 ro, float3 rd)
        {
            float3 relativePoint = point - ro;
            float dot = math.dot(rd, relativePoint);
            return ro + rd * dot;
        }

        /// <summary>
        /// Use this function to calculate the number of segments for a given spline length and resolution.
        /// </summary>
        /// <param name="length">A distance value in <see cref="PathIndexUnit"/>.</param>
        /// <param name="resolution">A value used to calculate the number of segments for a length. This is calculated
        /// as max(MIN_SEGMENTS, min(MAX_SEGMENTS, sqrt(length) * resolution)).
        /// </param>
        /// <returns>
        /// The number of segments as calculated for given length and resolution.
        /// </returns>
        public static int GetSegmentCount(float length, int resolution)
        {
            return (int) math.max(k_ResolutionSegmentCountMin, math.min(k_ResolutionSegmentCountMax, math.sqrt(length) * resolution));
        }

        struct Segment
        {
            public float start, length;

            public Segment(float start, float length)
            {
                this.start = start;
                this.length = length;
            }
        }

        static Segment GetNearestPoint(NativeSpline spline,
            float3 ro, float3 rd,
            Segment range,
            out float distance, out float3 nearest, out float time,
            int segments)
        {
            distance = float.PositiveInfinity;
            nearest = float.PositiveInfinity;
            time = float.PositiveInfinity;
            Segment segment = new Segment(-1f, 0f);

            float t0 = range.start;
            float3 a = EvaluatePosition(spline, t0);

            for (int i = 1; i < segments; i++)
            {
                float t1 = range.start + (range.length * (i / (segments - 1f)));
                float3 b = EvaluatePosition(spline, t1);
                float3 p = RayLineSegmentNearestPoint(ro, rd, a, b, out float st);
                float d = math.length(ProjectPointRay(p, ro, rd) - p);

                if (d < distance)
                {
                    segment.start = t0;
                    segment.length = t1 - t0;
                    time = segment.start + segment.length * st;
                    distance = d;
                    nearest = p;
                }

                t0 = t1;
                a = b;
            }

            return segment;
        }

        static Segment GetNearestPoint<T>(T spline,
            float3 point,
            Segment range,
            out float distance, out float3 nearest, out float time,
            int segments) where T : ISpline
        {
            distance = float.PositiveInfinity;
            nearest = float.PositiveInfinity;
            time = float.PositiveInfinity;
            Segment segment = new Segment(-1f, 0f);

            float t0 = range.start;
            float3 a = EvaluatePosition(spline, t0);
            float dsqr = distance;

            for (int i = 1; i < segments; i++)
            {
                float t1 = range.start + (range.length * (i / (segments - 1f)));
                float3 b = EvaluatePosition(spline, t1);
                float3 p = PointLineSegmentNearestPoint(point, a, b, out float st);
                float d = math.distancesq(p, point);

                if (d < dsqr)
                {
                    segment.start = t0;
                    segment.length = t1 - t0;
                    time = segment.start + segment.length * st;
                    dsqr = d;
                    distance = math.sqrt(d);
                    nearest = p;
                }

                t0 = t1;
                a = b;
            }

            return segment;
        }

        /// <summary>
        /// Calculate the point on a spline nearest to a ray.
        /// </summary>
        /// <param name="spline">The input spline to search for nearest point.</param>
        /// <param name="ray">The input ray to search against.</param>
        /// <param name="nearest">The point on a spline nearest to the input ray. The accuracy of this value is
        /// affected by the <paramref name="resolution"/>.</param>
        /// <param name="time">The normalized time value to the nearest point.</param>
        /// <param name="resolution">Affects how many segments to split a spline into when calculating the nearest point.
        /// Higher values mean smaller and more segments, which increases accuracy at the cost of processing time.
        /// The minimum resolution is defined by <seealso cref="PickResolutionMin"/>, and the maximum is defined by
        /// <seealso cref="PickResolutionMax"/>.
        /// In most cases, the default resolution is appropriate. Use with <seealso cref="iterations"/> to fine tune
        /// point accuracy.
        /// </param>
        /// <param name="iterations">
        /// The nearest point is calculated by finding the nearest point on the entire length
        /// of the spline using <seealso cref="resolution"/> to divide into equally spaced line segments. Successive
        /// iterations will then subdivide further the nearest segment, producing more accurate results. In most cases,
        /// the default value is sufficient, but if extreme accuracy is required this value can be increased to a
        /// maximum of <see cref="PickResolutionMax"/>.
        /// </param>
        /// <returns>The distance from ray to nearest point.</returns>
        public static float GetNearestPoint(NativeSpline spline, Ray ray, out float3 nearest, out float time, int resolution = PickResolutionDefault, int iterations = 2)
        {
            float distance = float.PositiveInfinity;
            nearest = float.PositiveInfinity;
            float3 ro = ray.origin, rd = ray.direction;
            Segment segment = new Segment(0f, 1f);
            time = 0f;
            int res = math.min(math.max(PickResolutionMin, resolution), PickResolutionMax);

            for (int i = 0, c = math.min(10, iterations); i < c; i++)
            {
                int segments = GetSegmentCount(spline.GetLength() * segment.length, res);
                segment = GetNearestPoint(spline, ro, rd, segment, out distance, out nearest, out time, segments);
            }

            return distance;
        }

        /// <summary>
        /// Calculate the point on a spline nearest to a point.
        /// </summary>
        /// <param name="spline">The input spline to search for nearest point.</param>
        /// <param name="point">The input point to compare.</param>
        /// <param name="nearest">The point on a spline nearest to the input point. The accuracy of this value is
        /// affected by the <paramref name="resolution"/>.</param>
        /// <param name="time">The normalized time value to the nearest point.</param>
        /// <param name="resolution">Affects how many segments to split a spline into when calculating the nearest point.
        /// Higher values mean smaller and more segments, which increases accuracy at the cost of processing time.
        /// The minimum resolution is defined by <seealso cref="PickResolutionMin"/>, and the maximum is defined by
        /// <seealso cref="PickResolutionMax"/>.
        /// In most cases, the default resolution is appropriate. Use with <seealso cref="iterations"/> to fine tune
        /// point accuracy.
        /// </param>
        /// <param name="iterations">
        /// The nearest point is calculated by finding the nearest point on the entire length
        /// of the spline using <seealso cref="resolution"/> to divide into equally spaced line segments. Successive
        /// iterations will then subdivide further the nearest segment, producing more accurate results. In most cases,
        /// the default value is sufficient, but if extreme accuracy is required this value can be increased to a
        /// maximum of <see cref="PickResolutionMax"/>.
        /// </param>
        /// <returns>The distance from input point to nearest point on spline.</returns>
        public static float GetNearestPoint<T>(T spline,
            float3 point,
            out float3 nearest,
            out float time,
            int resolution = PickResolutionDefault,
            int iterations = 2) where T : ISpline
        {
            float distance = float.PositiveInfinity;
            nearest = float.PositiveInfinity;
            Segment segment = new Segment(0f, 1f);
            time = 0f;
            int res = math.min(math.max(PickResolutionMin, resolution), PickResolutionMax);

            for (int i = 0, c = math.min(10, iterations); i < c; i++)
            {
                int segments = GetSegmentCount(spline.GetLength() * segment.length, res);
                segment = GetNearestPoint(spline, point, segment, out distance, out nearest, out time, segments);
            }

            return distance;
        }
        
        /// <summary>
        /// Given a time value using a certain PathIndexUnit type, calculate the associated time value in another targetPathUnit regarding a specific spline.
        /// </summary>
        /// <param name="spline">The Spline to use for the conversion, this is necessary to compute Normalized and Distance PathIndexUnits.</param>
        /// <param name="time">The splineData time in the original PathIndexUnit.</param>
        /// <param name="originalTimeUnit">The PathIndexUnit from the original time.</param>
        /// <param name="targetPathUnit">The PathIndexUnit in which time should be converted.</param>
        /// <returns>The time converted in the targetPathUnit.</returns>
        public static float GetConvertedTime<T>(T spline, float time, PathIndexUnit originalTimeUnit, PathIndexUnit targetPathUnit)
            where T : ISpline
        {
            if(originalTimeUnit == targetPathUnit)
                return time;
            return GetConvertedTime(spline, GetNormalizedTime(spline, time, originalTimeUnit), targetPathUnit);
        }
        
        static float GetConvertedTime<T>(T spline, float normalizedTime, PathIndexUnit targetPathUnit) where T : ISpline
        {
            switch(targetPathUnit)
            {
                case PathIndexUnit.Knot:
                    int splineIndex = SplineToCurveInterpolation(spline, normalizedTime, out float curveTime);
                    return splineIndex + curveTime;
                case PathIndexUnit.Distance:
                    return normalizedTime * spline.GetLength();
                default:
                    return normalizedTime;
            }
        }
        
        /// <summary>
        /// Given a time value using a certain PathIndexUnit type, calculate the normalized time value regarding a specific spline.
        /// </summary>
        /// <param name="spline">The Spline to use for the conversion, this is necessary to compute Normalized and Distance PathIndexUnits.</param>
        /// <param name="time">The time to normalize in the original PathIndexUnit.</param>
        /// <param name="originalTimeUnit">The PathIndexUnit from the original time.</param>
        /// <returns>The normalized time.</returns>
        public static float GetNormalizedTime<T>(T spline, float time, PathIndexUnit originalTimeUnit) where T : ISpline
        {
            switch(originalTimeUnit)
            {
                case PathIndexUnit.Knot:
                    return CurveToSplineInterpolation(spline, time);
                case PathIndexUnit.Distance:
                    return time / spline.GetLength();
                default:
                    return time;
            }
        }

        internal static int PreviousIndex<T>(this T spline, int index) where T : ISpline 
            => PreviousIndex(index, spline.KnotCount, spline.Closed);

        internal static int NextIndex<T>(this T spline, int index) where T : ISpline
            => NextIndex(index, spline.KnotCount, spline.Closed);
        
        internal static int PreviousIndex(int index, int count, bool wrap)
        {
            return wrap ? (index + (count-1)) % count : math.max(index - 1, 0);
        }

        internal static int NextIndex(int index, int count, bool wrap)
        {
            return wrap ? (index + 1) % count : math.min(index + 1, count - 1);
        }
        
        internal static float3 GetLinearTangent(float3 point, float3 to)
        {
            return (to - point) / 3.0f;
        }

        /// <summary>
        /// Reset a transform position to a position while keeping knot positions in the same place. This modifies both
        /// knot positions and transform position.
        /// </summary>
        /// <param name="container">The target spline.</param>
        /// <param name="position">The </param>
        public static void SetPivot(SplineContainer container, Vector3 position)
        {
            var transform = container.transform;
            var delta = position - transform.position;
            transform.position = position;
            var spline = container.Spline;
            for (int i = 0, c = spline.KnotCount; i < c; i++)
                spline[i] = spline[i] - delta;
        }
    }
}
