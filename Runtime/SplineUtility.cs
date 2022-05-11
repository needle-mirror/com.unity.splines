using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A collection of methods for extracting information about <see cref="Spline"/> types.
    /// </summary>
    public static class SplineUtility
    {
        const int k_SubdivisionCountMin = 6;
        const int k_SubdivisionCountMax = 1024;

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
        /// Compute interpolated position, direction and upDirection at ratio t. Calling this method to get the
        /// 3 vectors is faster than calling independently EvaluatePosition, EvaluateDirection and EvaluateUpVector
        /// for the same time t as it reduces some redundant computation.
        /// </summary>
        /// <param name="spline">The spline to interpolate.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <param name="position">Output variable for the float3 position at t.</param>
        /// <param name="tangent">Output variable for the float3 tangent at t.</param>
        /// <param name="upVector">Output variable for the float3 up direction at t.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>Boolean value, true if a valid set of output variables as been computed.</returns>
        public static bool Evaluate<T>(this T spline,
                float t,
                out float3 position,
                out float3 tangent,
                out float3 upVector
            ) where T : ISpline
        {
            if(spline.Count < 1)
            {
                position = float3.zero;
                tangent = new float3(0, 0, 1);
                upVector = new float3(0, 1, 0);
                return false;
            }

            var curveIndex = SplineToCurveT(spline, t, out var curveT);
            var curve = spline.GetCurve(curveIndex);

            position = CurveUtility.EvaluatePosition(curve, curveT);
            tangent = CurveUtility.EvaluateTangent(curve, curveT);
            upVector = spline.EvaluateUpVector(curveIndex, curveT);

            return true;
        }

        /// <summary>
        /// Return an interpolated position at ratio t.
        /// </summary>
        /// <param name="spline">The spline to interpolate.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>A position on the spline.</returns>
        public static float3 EvaluatePosition<T>(this T spline, float t) where T : ISpline
        {
            if (spline.Count < 1)
                return float.PositiveInfinity;
            var curve = spline.GetCurve(SplineToCurveT(spline, t, out var curveT));
            return CurveUtility.EvaluatePosition(curve, curveT);
        }

        /// <summary>
        /// Return an interpolated direction at ratio t.
        /// </summary>
        /// <param name="spline">The spline to interpolate.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>A direction on the spline.</returns>
        public static float3 EvaluateTangent<T>(this T spline, float t) where T : ISpline
        {
            if (spline.Count < 1)
                return float.PositiveInfinity;
            var curve = spline.GetCurve(SplineToCurveT(spline, t, out var curveT));
            return CurveUtility.EvaluateTangent(curve, curveT);
        }

        /// <summary>
        /// Evaluate the normal (up) vector of a spline.
        /// </summary>
        /// <param name="spline">The <seealso cref="NativeSpline"/> to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of the curve.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>An up vector</returns>
        public static float3 EvaluateUpVector<T>(this T spline, float t) where T : ISpline
        {
            if (spline.Count < 1)
                return float3.zero;

            var curveIndex = SplineToCurveT(spline, t, out var curveT);
            return spline.EvaluateUpVector(curveIndex, curveT);
        }

        static float3 EvaluateUpVector<T>(this T spline, int curveIndex, float curveT) where T : ISpline
        {
            if (spline.Count < 1)
                return float3.zero;

            var curve = spline.GetCurve(curveIndex);

            var curveStartRotation = spline[curveIndex].Rotation;
            var curveStartUp = math.rotate(curveStartRotation, math.up());
            if (curveT == 0f)
                return curveStartUp;

            var endKnotIndex = spline.NextIndex(curveIndex);
            var curveEndRotation = spline[endKnotIndex].Rotation;
            var curveEndUp = math.rotate(curveEndRotation, math.up());
            if (curveT == 1f)
                return curveEndUp;

            return CurveUtility.EvaluateUpVector(curve, curveT, curveStartUp, curveEndUp);
        }

        /// <summary>
        /// Return an interpolated acceleration at ratio t.
        /// </summary>
        /// <param name="spline">The spline to interpolate.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>An acceleration on the spline.</returns>
        public static float3 EvaluateAcceleration<T>(this T spline, float t) where T : ISpline
        {
            if (spline.Count < 1)
                return float3.zero;
            var curve = spline.GetCurve(SplineToCurveT(spline, t, out var curveT));
            return CurveUtility.EvaluateAcceleration(curve, curveT);
        }

        /// <summary>
        /// Return an interpolated curvature at ratio t.
        /// </summary>
        /// <param name="spline">The spline to interpolate.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>A curvature on the spline.</returns>
        public static float EvaluateCurvature<T>(this T spline, float t) where T : ISpline
        {
            if (spline.Count < 1)
                return 0f;

            var curveIndex = SplineToCurveT(spline, t, out var curveT);
            var curve = spline.GetCurve(curveIndex);

            return CurveUtility.EvaluateCurvature(curve, curveT);
        }

        /// <summary>
        /// Return the curvature center at ratio t. The curvature center represents the center of the circle
        /// that is tangent to the curve at t. This circle is in the plane defined by the curve velocity (tangent)
        /// and the curve acceleration at that point.
        /// </summary>
        /// <param name="spline">The spline to interpolate.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>A point representing the curvature center associated to the position at t on the spline.</returns>
        public static float3 EvaluateCurvatureCenter<T>(this T spline, float t) where T : ISpline
        {
            if (spline.Count < 1)
                return 0f;

            var curveIndex = SplineToCurveT(spline, t, out var curveT);
            var curve = spline.GetCurve(curveIndex);

            var curvature = CurveUtility.EvaluateCurvature(curve, curveT);

            if(curvature != 0)
            {
                var radius = 1f / curvature;

                var position = CurveUtility.EvaluatePosition(curve, curveT);
                var velocity = CurveUtility.EvaluateTangent(curve, curveT);
                var acceleration = CurveUtility.EvaluateAcceleration(curve, curveT);
                var curvatureUp = math.normalize(math.cross(acceleration, velocity));
                var curvatureRight = math.normalize(math.cross(velocity, curvatureUp));

                return position + radius * curvatureRight;
            }

            return float3.zero;
        }

        /// <summary>
        /// Given a normalized interpolation (t) for a spline, calculate the curve index and curve-relative
        /// normalized interpolation.
        /// </summary>
        /// <param name="spline">The target spline.</param>
        /// <param name="splineT">A normalized spline interpolation value to be converted into curve space.</param>
        /// <param name="curveT">A normalized curve interpolation value.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>The curve index.</returns>
        public static int SplineToCurveT<T>(this T spline, float splineT, out float curveT) where T : ISpline
        {
            return SplineToCurveT(spline, splineT, out curveT, true);
        }

        static int SplineToCurveT<T>(this T spline, float splineT, out float curveT, bool useLUT) where T : ISpline
        {
            var knotCount = spline.Count;
            if(knotCount <= 1)
            {
                curveT = 0f;
                return 0;
            }

            splineT = math.clamp(splineT, 0, 1);
            var tLength = splineT * spline.GetLength();

            var start = 0f;
            var closed = spline.Closed;
            for (int i = 0, c = closed ? knotCount : knotCount - 1; i < c; i++)
            {
                var index = i % knotCount;
                var curveLength = spline.GetCurveLength(index);

                if (tLength <= (start + curveLength))
                {
                    curveT =  useLUT ?
                        spline.GetCurveInterpolation(index, tLength - start) :
                        (tLength - start)/ curveLength;
                    return index;
                }

                start += curveLength;
            }

            curveT = 1f;
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
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>An interpolation value relative to normalized Spline length (0 to 1).</returns>
        /// <seealso cref="SplineToCurveT{T}"/>
        public static float CurveToSplineT<T>(this T spline, float curve) where T : ISpline
        {
            if(spline.Count <= 1 || curve < 0f)
                return 0f;

            if(curve >= ( spline.Closed ? spline.Count : spline.Count - 1 ))
                return 1f;

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
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static float CalculateLength<T>(this T spline, float4x4 transform) where T : ISpline
        {
            using var nativeSpline = new NativeSpline(spline, transform);
            return nativeSpline.GetLength();
        }

        /// <summary>
        /// Calculates the number of curves in a spline.
        /// </summary>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <param name="spline"></param>
        /// <returns>The number of curves in a spline.</returns>
        public static int GetCurveCount<T>(this T spline) where T : ISpline
        {
            return math.max(0, spline.Count - (spline.Closed ? 0 : 1));
        }

        /// <summary>
        /// Calculate the bounding box of a Spline.
        /// </summary>
        /// <param name="spline">The spline for which to calculate bounds.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>The bounds of a spline.</returns>
        public static Bounds GetBounds<T>(this T spline) where T : ISpline
        {
            if (spline.Count < 1)
                return default;

            var knot = spline[0];
            Bounds bounds = new Bounds(knot.Position, Vector3.zero);

            for (int i = 1, c = spline.Count; i < c; ++i)
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

        [Obsolete("Use "+nameof(GetSubdivisionCount)+" instead.", false)]
        public static int GetSegmentCount(float length, int resolution) => GetSubdivisionCount(length,resolution);

        /// <summary>
        /// Use this function to calculate the number of subdivisions for a given spline length and resolution.
        /// </summary>
        /// <param name="length">A distance value in <see cref="PathIndexUnit"/>.</param>
        /// <param name="resolution">A value used to calculate the number of subdivisions for a length. This is calculated
        /// as max(MIN_SUBDIVISIONS, min(MAX_SUBDIVISIONS, sqrt(length) * resolution)).
        /// </param>
        /// <returns>
        /// The number of subdivisions as calculated for given length and resolution.
        /// </returns>
        public static int GetSubdivisionCount(float length, int resolution)
        {
            return (int) math.max(k_SubdivisionCountMin, math.min(k_SubdivisionCountMax, math.sqrt(length) * resolution));
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

        static Segment GetNearestPoint<T>(T spline,
            float3 ro, float3 rd,
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
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <param name="t">The normalized time value to the nearest point.</param>
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
        public static float GetNearestPoint<T>(T spline,
            Ray ray,
            out float3 nearest,
            out float t,
            int resolution = PickResolutionDefault,
            int iterations = 2) where T : ISpline
        {
            float distance = float.PositiveInfinity;
            nearest = float.PositiveInfinity;
            float3 ro = ray.origin, rd = ray.direction;
            Segment segment = new Segment(0f, 1f);
            t = 0f;
            int res = math.min(math.max(PickResolutionMin, resolution), PickResolutionMax);

            for (int i = 0, c = math.min(10, iterations); i < c; i++)
            {
                int segments = GetSubdivisionCount(spline.GetLength() * segment.length, res);
                segment = GetNearestPoint(spline, ro, rd, segment, out distance, out nearest, out t, segments);
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
        /// <param name="t">The normalized interpolation ratio corresponding to the nearest point.</param>
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
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>The distance from input point to nearest point on spline.</returns>
        public static float GetNearestPoint<T>(T spline,
            float3 point,
            out float3 nearest,
            out float t,
            int resolution = PickResolutionDefault,
            int iterations = 2) where T : ISpline
        {
            float distance = float.PositiveInfinity;
            nearest = float.PositiveInfinity;
            Segment segment = new Segment(0f, 1f);
            t = 0f;
            int res = math.min(math.max(PickResolutionMin, resolution), PickResolutionMax);

            for (int i = 0, c = math.min(10, iterations); i < c; i++)
            {
                int segments = GetSubdivisionCount(spline.GetLength() * segment.length, res);
                segment = GetNearestPoint(spline, point, segment, out distance, out nearest, out t, segments);
            }

            return distance;
        }

        /// <summary>
        /// Given a Spline and interpolation ratio, calculate the 3d point at a linear distance from point at spline.EvaluatePosition(t).
        /// Returns the corresponding time associated to this 3d position on the Spline.
        /// </summary>
        /// <param name="spline">The Spline on which to compute the point.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <param name="fromT">The Spline interpolation ratio 't' (normalized) from which the next position need to be computed.</param>
        /// <param name="relativeDistance">
        /// The relative distance at which the new point should be placed. A negative value will compute a point at a
        /// 'resultPointTime' previous to 'fromT' (backward search).
        /// </param>
        /// <param name="resultPointT">The normalized interpolation ratio of the resulting point.</param>
        /// <returns>The 3d point from the spline located at a linear distance from the point at t.</returns>
        public static float3 GetPointAtLinearDistance<T>(this T spline,
            float fromT,
            float relativeDistance,
            out float resultPointT) where T : ISpline
        {
            const float epsilon = 0.001f;
            if(fromT <0)
            {
                resultPointT = 0f;
                return spline.EvaluatePosition(0f);
            }

            var length = spline.GetLength();
            var lengthAtT = fromT * length;
            float currentLength = lengthAtT;
            if(currentLength + relativeDistance >= length) //relativeDistance >= 0 -> Forward search
            {
                resultPointT = 1f;
                return spline.EvaluatePosition(1f);
            }
            else if(currentLength + relativeDistance <= 0) //relativeDistance < 0 -> Forward search
            {
                resultPointT = 0f;
                return spline.EvaluatePosition(0f);
            }

            var currentPos = spline.EvaluatePosition(fromT);
            resultPointT = fromT;

            var forwardSearch = relativeDistance >= 0;
            var residual = math.abs(relativeDistance);
            float linearDistance = 0;
            float3 point = spline.EvaluatePosition(fromT);
            while(residual > epsilon && (forwardSearch ? resultPointT < 1f : resultPointT > 0))
            {
                currentLength += forwardSearch ? residual : -residual;
                resultPointT = currentLength / length;

                if(resultPointT > 1f) //forward search
                {
                    resultPointT = 1f;
                    point = spline.EvaluatePosition(1f);
                }else if(resultPointT < 0f) //backward search
                {
                    resultPointT = 0f;
                    point = spline.EvaluatePosition(0f);
                }

                point = spline.EvaluatePosition(resultPointT);
                linearDistance = math.distance(currentPos, point);
                residual = math.abs(relativeDistance) - linearDistance;
            }

            return point;
        }

        /// <summary>
        /// Given a normalized interpolation ratio, calculate the associated interpolation value in another targetPathUnit regarding a specific spline.
        /// </summary>
        /// <param name="spline">The Spline to use for the conversion, this is necessary to compute Normalized and Distance PathIndexUnits.</param>
        /// <param name="t">Normalized interpolation ratio (0 to 1).</param>
        /// <param name="targetPathUnit">The PathIndexUnit to which 't' should be converted.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>The interpolation value converted to targetPathUnit.</returns>
        public static float ConvertIndexUnit<T>(this T spline, float t, PathIndexUnit targetPathUnit)
            where T : ISpline
        {
            if (targetPathUnit == PathIndexUnit.Normalized)
                return WrapInterpolation(t);

            return ConvertNormalizedIndexUnit(spline, t, targetPathUnit);
        }

        /// <summary>
        /// Given an interpolation value using a certain PathIndexUnit type, calculate the associated interpolation value in another targetPathUnit regarding a specific spline.
        /// </summary>
        /// <param name="spline">The Spline to use for the conversion, this is necessary to compute Normalized and Distance PathIndexUnits.</param>
        /// <param name="t">Interpolation in the original PathIndexUnit.</param>
        /// <param name="fromPathUnit">The PathIndexUnit for the original interpolation value.</param>
        /// <param name="targetPathUnit">The PathIndexUnit to which 't' should be converted.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>The interpolation value converted to targetPathUnit.</returns>
        public static float ConvertIndexUnit<T>(this T spline, float t, PathIndexUnit fromPathUnit, PathIndexUnit targetPathUnit)
            where T : ISpline
        {
            if (fromPathUnit == targetPathUnit)
            {
                if (targetPathUnit == PathIndexUnit.Normalized)
                    t = WrapInterpolation(t);

                return t;
            }

            return ConvertNormalizedIndexUnit(spline, GetNormalizedInterpolation(spline, t, fromPathUnit), targetPathUnit);
        }

        static float ConvertNormalizedIndexUnit<T>(T spline, float t, PathIndexUnit targetPathUnit) where T : ISpline
        {
            switch(targetPathUnit)
            {
                case PathIndexUnit.Knot:
                    //LUT SHOULD NOT be used here as PathIndexUnit.KnotIndex is linear regarding the distance
                    //(and thus not be interpreted using the LUT and the interpolated T)
                    int splineIndex = spline.SplineToCurveT(t, out float curveTime, false);
                    return splineIndex + curveTime;
                case PathIndexUnit.Distance:
                    return t * spline.GetLength();
                default:
                    return t;
            }
        }

        static float WrapInterpolation(float t)
        {
            return t % 1f == 0f ? math.clamp(t, 0f, 1f) : t - math.floor(t);
        }

        /// <summary>
        /// Given an interpolation value in any PathIndexUnit type, calculate the normalized interpolation ratio value
        /// relative to a <see cref="Spline"/>.
        /// </summary>
        /// <param name="spline">The Spline to use for the conversion, this is necessary to compute Normalized and Distance PathIndexUnits.</param>
        /// <param name="t">The 't' value to normalize in the original PathIndexUnit.</param>
        /// <param name="originalPathUnit">The PathIndexUnit from the original 't'.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>The normalized interpolation ratio (0 to 1).</returns>
        public static float GetNormalizedInterpolation<T>(T spline, float t, PathIndexUnit originalPathUnit) where T : ISpline
        {
            switch(originalPathUnit)
            {
                case PathIndexUnit.Knot:
                    return WrapInterpolation(CurveToSplineT(spline, t));
                case PathIndexUnit.Distance:
                    var length = spline.GetLength();
                    return WrapInterpolation(length > 0 ? t / length : 0f);
                default:
                    return WrapInterpolation(t);
            }
        }

        public static int PreviousIndex<T>(this T spline, int index) where T : ISpline
            => PreviousIndex(index, spline.Count, spline.Closed);

        public static int NextIndex<T>(this T spline, int index) where T : ISpline
            => NextIndex(index, spline.Count, spline.Closed);

        public static BezierKnot Next<T>(this T spline, int index) where T : ISpline
            => spline[NextIndex(spline, index)];

        public static BezierKnot Previous<T>(this T spline, int index) where T : ISpline
            => spline[PreviousIndex(spline, index)];

        internal static int PreviousIndex(int index, int count, bool wrap)
        {
            return wrap ? (index + (count-1)) % count : math.max(index - 1, 0);
        }

        internal static int NextIndex(int index, int count, bool wrap)
        {
            return wrap ? (index + 1) % count : math.min(index + 1, count - 1);
        }

        internal static float3 GetExplicitLinearTangent(float3 point, float3 to)
        {
            return (to - point) / 3.0f;
        }

        // Typically a linear tangent is stored as a zero length vector. When a tangent is user controlled, we need to
        // extrude out the tangent to something a user can grab rather than leaving the tangent on top of the knot.
        internal static float3 GetExplicitLinearTangent(BezierKnot from, BezierKnot to)
        {
            var tin = to.Position - from.Position;
            return math.mul(math.inverse(from.Rotation), tin * .33f);
        }

        /// <summary>
        /// Calculates a tangent from the previous and next knot positions.
        /// </summary>
        /// <param name="previous">The position of the previous <see cref="BezierKnot"/>.</param>
        /// <param name="next">The position of the next <see cref="BezierKnot"/>.</param>
        /// <returns>Returns a tangent calculated from the previous and next knot positions.</returns>
        public static float3 GetCatmullRomTangent(float3 previous, float3 next)
        {
            return 0.5f * (next - previous) / 3.0f;
        }

        /// <summary>
        /// Gets a <see cref="BezierKnot"/> with its tangents and rotation calculated using the previous and next knot positions.
        /// </summary>
        /// <param name="position">The position of the knot.</param>
        /// <param name="previous">The knot that immediately precedes the requested knot.</param>
        /// <param name="next">The knot that immediately follows the requested knot.</param>
        /// <returns>A <see cref="BezierKnot"/> with tangent and rotation values calculated from the previous and next knot positions.</returns>
        public static BezierKnot GetAutoSmoothKnot(float3 position, float3 previous, float3 next)
        {
            return GetAutoSmoothKnot(position, previous, next, math.up());
        }

        /// <summary>
        /// Gets a <see cref="BezierKnot"/> with its tangents and rotation calculated using the previous and next knot positions.
        /// </summary>
        /// <param name="position">The position of the knot.</param>
        /// <param name="previous">The knot that immediately precedes the requested knot.</param>
        /// <param name="next">The knot that immediately follows the requested knot.</param>
        /// <param name="normal">The normal vector of the knot.</param>
        /// <returns>A <see cref="BezierKnot"/> with tangent and rotation values calculated from the previous and next knot positions.</returns>
        public static BezierKnot GetAutoSmoothKnot(float3 position, float3 previous, float3 next, float3 normal)
        {
            var tan = GetCatmullRomTangent(previous, next);
            var dir = new float3(0, 0, math.length(tan));
            return new BezierKnot(position, -dir, dir, GetKnotRotation(tan, normal));
        }

        internal static quaternion GetKnotRotation(float3 tangent, float3 normal)
        {
            if (math.lengthsq(tangent) == 0f)
                tangent = math.rotate(Quaternion.FromToRotation(math.up(), normal), math.forward());

            float3 up = Mathf.Approximately(math.abs(math.dot(tangent, normal)), 1f)
                ? math.cross(tangent, math.right())
                : Vector3.ProjectOnPlane(normal, tangent).normalized;

            return quaternion.LookRotationSafe(tangent, up);
        }

        /// <summary>
        /// Reset a transform position to a position while keeping knot positions in the same place. This modifies both
        /// knot positions and transform position.
        /// </summary>
        /// <param name="container">The target spline.</param>
        /// <param name="position">The point in world space to move the pivot to.</param>
        public static void SetPivot(SplineContainer container, Vector3 position)
        {
            var transform = container.transform;
            var delta = position - transform.position;
            transform.position = position;
            var spline = container.Spline;
            for (int i = 0, c = spline.Count; i < c; i++)
                spline[i] = spline[i] - delta;
        }

        /// <summary>
        /// Creates a new spline and adds it to the SplineContainer.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <return>Returns the spline that was created and added to the container.</return>
        public static Spline AddSpline<T>(this T container) where T : ISplineContainer
        {
            var splines = new List<Spline>(container.Splines);
            var spline = new Spline();
            splines.Add(spline);
            container.Splines = splines;
            return spline;
        }

        /// Removes a spline from a SplineContainer.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="splineIndex">The index of the spline to remove from the SplineContainer.</param>
        /// <return>Returns true if the spline was removed from the container.</return>
        public static bool RemoveSplineAt<T>(this T container, int splineIndex) where T : ISplineContainer
        {
            if (splineIndex < 0 || splineIndex >= container.Splines.Count)
                return false;

            var splines = new List<Spline>(container.Splines);
            splines.RemoveAt(splineIndex);
            container.KnotLinkCollection.SplineRemoved(splineIndex);

            container.Splines = splines;
            return true;
        }

        /// <summary>
        /// Removes a spline from a SplineContainer.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="spline">The spline to remove from the SplineContainer.</param>
        /// <return>Returns true if the spline was removed from the container.</return>
        public static bool RemoveSpline<T>(this T container, Spline spline) where T : ISplineContainer
        {
            var splines = new List<Spline>(container.Splines);
            var index = splines.IndexOf(spline);
            if (index < 0)
                return false;

            splines.RemoveAt(index);
            container.KnotLinkCollection.SplineRemoved(index);

            container.Splines = splines;
            return true;
        }

        internal static bool IsIndexValid<T>(T container, SplineKnotIndex index) where T : ISplineContainer
        {
            return index.Knot >= 0 && index.Knot < container.Splines[index.Spline].Count &&
                index.Spline < container.Splines.Count && index.Knot < container.Splines[index.Spline].Count;
        }

        /// <summary>
        /// Synchronizes the position of all knots linked to the knot at `index`.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="index">The `SplineKnotIndex` of the knot to use to synchronize the positions.</param>
        public static void SetLinkedKnotPosition<T>(this T container, SplineKnotIndex index) where T : ISplineContainer
        {
            if (!container.KnotLinkCollection.TryGetKnotLinks(index, out var knots))
                return;

            var splines = container.Splines;
            var position = splines[index.Spline][index.Knot].Position;

            foreach (var i in knots)
            {
                if (!IsIndexValid(container, i))
                    return;

                var knot = splines[i.Spline][i.Knot];
                knot.Position = position;
                splines[i.Spline].SetKnotNoNotify(i.Knot, knot);
            }
        }

        /// <summary>
        /// Links two knots in a SplineContainer. The two knots can be on different splines, but both must be in the referenced SplineContainer.
        /// If these knots are linked to other knots, all existing links are kept and updated.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="knotA">The first knot to link.</param>
        /// <param name="knotB">The second knot to link.</param>
        public static void LinkKnots<T>(this T container, SplineKnotIndex knotA,  SplineKnotIndex knotB) where T : ISplineContainer
        {
            container.KnotLinkCollection.Link(knotA, knotB);
        }

        /// <summary>
        /// Unlinks several knots from a SplineContainer. A knot in `knots` disconnects from other knots it was linked to.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="knots">The knot to unlink.</param>
        public static void UnlinkKnots<T>(this T container, IReadOnlyList<SplineKnotIndex> knots) where T : ISplineContainer
        {
            foreach (var knot in knots)
                container.KnotLinkCollection.Unlink(knot);
        }
    }
}