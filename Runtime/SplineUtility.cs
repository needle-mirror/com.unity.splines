using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A collection of methods for extracting information about <see cref="Spline"/> types.
    /// </summary>
    /// <remarks>
    /// `SplineUtility` methods do not consider Transform values except where explicitly requested. To perform operations in world space, you can use the <see cref="SplineContainer"/> evaluate methods or build a <see cref="NativeSpline"/> with a constructor that accepts a matrix and evaluate that spline.
    /// </remarks>
    public static class SplineUtility
    {
        const int k_SubdivisionCountMin = 6;
        const int k_SubdivisionCountMax = 1024;

        /// <summary>
        /// The default tension value used for <see cref="TangentMode.AutoSmooth"/> knots.
        /// Use with <see cref="Spline.SetTangentMode(UnityEngine.Splines.TangentMode)"/> and
        /// <see cref="Spline.SetAutoSmoothTension(int,float)"/> to control the curvature of the spline at control
        /// points.
        /// </summary>
        public const float DefaultTension = 1 / 3f;

        /// <summary>
        /// The tension value for a Catmull-Rom type spline.
        /// Use with <see cref="Spline.SetTangentMode(UnityEngine.Splines.TangentMode)"/> and
        /// <see cref="Spline.SetAutoSmoothTension(int,float)"/> to control the curvature of the spline at control
        /// points.
        /// </summary>
        //todo Deprecate in 3.0.
        public const float CatmullRomTension = 1 / 2f;

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
        /// <returns>True if successful.</returns>
        public static bool Evaluate<T>(this T spline,
            float t,
            out float3 position,
            out float3 tangent,
            out float3 upVector
        ) where T : ISpline
        {
            if (spline.Count < 1)
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
            upVector = spline.GetCurveUpVector(curveIndex, curveT);

            return true;
        }

        /// <summary>
        /// Computes the interpolated position for NURBS defined by order, controlPoints, and knotVector at ratio t.
        /// </summary>
        /// <param name="t">The value between knotVector[0] and knotVector[-1] that represents the ratio along the curve.</param>
        /// <param name="controlPoints">The control points for the NURBS.</param>
        /// <param name="knotVector">The knot vector for the NURBS. There must be at least order + controlPoints.Length - 1 knots.</param>
        /// <param name="order">The order of the curve. For example, 4 for a cubic curve or 3 for quadratic.</param>
        /// <param name="position">The output variable for the float3 position at t.</param>
        /// <returns>True if successful.</returns>
        public static bool EvaluateNurbs(
            float t,
            List<float3> controlPoints,
            List<double> knotVector,
            int order,
            out float3 position
        )
        {
            position = float3.zero;
            if (knotVector.Count < controlPoints.Count + order - 1 || controlPoints.Count < order || t < 0 || 1 < t)
            {
                return false;
            }

            knotVector = new List<double>(knotVector);

            var originalFirstKnot = knotVector[0];
            var fullKnotSpan = knotVector[knotVector.Count - 1] - knotVector[0];

            //normalize knots
            if (knotVector[0] != 0 || knotVector[knotVector.Count - 1] != 1)
            {
                for (int i = 0; i < knotVector.Count; ++i)
                {
                    knotVector[i] = (knotVector[i] - originalFirstKnot) / fullKnotSpan;
                }
            }

            var span = order;
            while (span < controlPoints.Count && knotVector[span] <= t)
            {
                span++;
            }
            span--;

            var basis = SplineUtility.GetNurbsBasisFunctions(order, t, knotVector, span);

            for (int i = 0; i < order; ++i)
            {
                position += basis[i] * controlPoints[span - order + 1 + i];
            }

            return true;
        }

        static float[] GetNurbsBasisFunctions(int degree, float t, List<double> knotVector, int span)
        {
            //Constructs the Basis functions at t for the nurbs curve.
            //The nurbs basis function form can be found at this link under the section
            //"Construction of the basis functions": https://en.wikipedia.org/wiki/Non-uniform_rational_B-spline
            //This is an iterative way of computing the same thing.
            var left = new float[degree];
            var right = new float[degree];
            var N = new float[degree];

            for (int j = 0; j < degree; ++j)
            {
                left[j] = 0f;
                right[j] = 0f;
                N[j] = 1f;
            }

            for (int j = 1; j < degree; ++j)
            {

                left[j] = (float)(t - knotVector[span + 1 - j]);
                right[j] = (float)(knotVector[span + j] - t);
                var saved = 0f;
                for (int k = 0; k < j; k++)
                {
                    float temp = N[k] / (right[k + 1] + left[j - k]);
                    N[k] = saved + right[k + 1] * temp;
                    saved = left[j - k] * temp;
                }

                N[j] = saved;

            }

            return N;
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
        /// <param name="t">A value between 0 and 1 representing a ratio of the curve.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>An up vector</returns>
        public static float3 EvaluateUpVector<T>(this T spline, float t) where T : ISpline
        {
            if (spline.Count < 1)
                return float3.zero;

            var curveIndex = SplineToCurveT(spline, t, out var curveT);
            return spline.GetCurveUpVector(curveIndex, curveT);
        }
        
        /// <summary>
        /// Calculate the normal (up) vector of a spline. This is a more accurate but more expensive operation
        /// than <seealso cref="EvaluateUpVector{T}"/>.
        /// </summary>
        /// <param name="spline">The <seealso cref="NativeSpline"/> to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a ratio of the curve.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>An up vector</returns>
        public static float3 CalculateUpVector<T>(this T spline, float t) where T : ISpline
        {
            if (spline.Count < 1)
                return float3.zero;
        
            var curveIndex = SplineToCurveT(spline, t, out var curveT);
            return spline.CalculateUpVector(curveIndex, curveT);
        }

        internal static float3 CalculateUpVector<T>(this T spline, int curveIndex, float curveT) where T : ISpline
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

            var up = CurveUtility.EvaluateUpVector(curve, curveT, curveStartUp, curveEndUp);
                
            return up;
        }
        
        internal static void EvaluateUpVectorsForCurve<T>(this T spline, int curveIndex, Vector3[] upVectors) where T : ISpline
        {
            if (spline.Count < 1 || upVectors == null)
                return;
            
            var curveStartRotation = spline[curveIndex].Rotation;
            var curveStartUp = math.rotate(curveStartRotation, math.up());

            var endKnotIndex = spline.NextIndex(curveIndex);
            var curveEndRotation = spline[endKnotIndex].Rotation;
            var curveEndUp = math.rotate(curveEndRotation, math.up());
            
            CurveUtility.EvaluateUpVectors(spline.GetCurve(curveIndex), curveStartUp, curveEndUp, upVectors);
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

            if (curvature != 0)
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
            if (knotCount <= 1)
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
                    curveT = useLUT ?
                        spline.GetCurveInterpolation(index, tLength - start) :
                        (tLength - start) / curveLength;
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
            // Clamp negative curve index to 0
            if (spline.Count <= 1 || curve < 0f)
                return 0f;

            // Clamp postive curve index beyond last knot to 1
            if (curve >= (spline.Closed ? spline.Count : spline.Count - 1))
                return 1f;

            var curveIndex = (int)math.floor(curve);

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
            return GetBounds(spline, float4x4.identity);
        }

        /// <summary>
        /// Creates a bounding box for a spline.
        /// </summary>
        /// <param name="spline">The spline to calculate bounds for.</param>
        /// <param name="transform">The matrix to transform the spline's elements with.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>The bounds of a spline.</returns>
        public static Bounds GetBounds<T>(this T spline, float4x4 transform) where T : ISpline
        {
            if (spline.Count < 1)
                return default;

            var knot = spline[0];
            Bounds bounds = new Bounds(math.transform(transform, knot.Position), Vector3.zero);

            // Only encapsulate first tangentIn if the spline is closed - otherwise it's not contributing to the spline's shape.
            if (spline.Closed)
                bounds.Encapsulate(math.transform(transform, knot.Position + math.rotate(knot.Rotation, knot.TangentIn)));
            bounds.Encapsulate(math.transform(transform, knot.Position + math.rotate(knot.Rotation, knot.TangentOut)));

            for (int i = 1, c = spline.Count; i < c; ++i)
            {
                knot = spline[i];
                bounds.Encapsulate(math.transform(transform, knot.Position));
                bounds.Encapsulate(math.transform(transform, knot.Position + math.rotate(knot.Rotation, knot.TangentIn)));

                // Encapsulate last tangentOut if the spline is closed - otherwise it's not contributing to the spline's shape.
                if (spline.Closed || (!spline.Closed && i < c - 1))
                    bounds.Encapsulate(math.transform(transform, knot.Position + math.rotate(knot.Rotation, knot.TangentOut)));
            }

            return bounds;
        }

        /// <summary>
        /// Gets the number of segments for a specified spline length and resolution.
        /// </summary>
        /// <param name="length">The length of the spline to consider.</param>
        /// <param name="resolution">The value used to calculate the number of segments for a length. This is calculated
        /// as max(MIN_SEGMENTS, min(MAX_SEGMENTS, sqrt(length) * resolution)).
        /// </param>
        /// <returns>
        /// The number of segments for a length and resolution.
        /// </returns>
        [Obsolete("Use " + nameof(GetSubdivisionCount) + " instead.", false)]
        public static int GetSegmentCount(float length, int resolution) => GetSubdivisionCount(length, resolution);

        /// <summary>
        /// Gets the number of subdivisions for a spline length and resolution.
        /// </summary>
        /// <param name="length">The length of the spline to consider.</param>
        /// <param name="resolution">The resolution to consider. Higher resolutions result in more
        /// precise representations. However, higher resolutions have higher performance requirements.
        /// </param>
        /// <returns>
        /// The number of subdivisions as calculated for given length and resolution.
        /// </returns>
        public static int GetSubdivisionCount(float length, int resolution)
        {
            return (int)math.max(k_SubdivisionCountMin, math.min(k_SubdivisionCountMax, math.sqrt(length) * resolution));
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
                var (rayPoint, linePoint) = SplineMath.RayLineNearestPoint(ro, rd, a, b, out _, out var lineParam);
                float dsqr = math.lengthsq(linePoint - rayPoint);

                if (dsqr < distance)
                {
                    segment.start = t0;
                    segment.length = t1 - t0;
                    time = segment.start + segment.length * lineParam;
                    distance = dsqr;
                    nearest = linePoint;
                }

                t0 = t1;
                a = b;
            }

            distance = math.sqrt(distance);
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


            for (int i = 1; i < segments; i++)
            {
                float t1 = range.start + (range.length * (i / (segments - 1f)));
                float3 b = EvaluatePosition(spline, t1);
                var p = SplineMath.PointLineNearestPoint(point, a, b, out var lineParam);
                float dsqr = math.distancesq(p, point);

                if (dsqr < distance)
                {
                    segment.start = t0;
                    segment.length = t1 - t0;
                    time = segment.start + segment.length * lineParam;
                    distance = dsqr;

                    nearest = p;
                }

                t0 = t1;
                a = b;
            }

            distance = math.sqrt(distance);
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
        /// In most cases, the default resolution is appropriate. Use with <paramref name="iterations"/> to fine tune
        /// point accuracy.
        /// </param>
        /// <param name="iterations">
        /// The nearest point is calculated by finding the nearest point on the entire length
        /// of the spline using <paramref name="resolution"/> to divide into equally spaced line segments. Successive
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
        /// In most cases, the default resolution is appropriate. Use with <paramref name="iterations"/> to fine tune
        /// point accuracy.
        /// </param>
        /// <param name="iterations">
        /// The nearest point is calculated by finding the nearest point on the entire length
        /// of the spline using <paramref name="resolution"/> to divide into equally spaced line segments. Successive
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
        /// <param name="fromT">The Spline interpolation ratio `t` (normalized) from which the next position need to be computed.</param>
        /// <param name="relativeDistance">
        /// The relative distance at which the new point should be placed. A negative value will compute a point at a
        /// `resultPointTime` previous to `fromT` (backward search).
        /// </param>
        /// <param name="resultPointT">The normalized interpolation ratio of the resulting point.</param>
        /// <returns>The 3d point from the spline located at a linear distance from the point at t.</returns>
        public static float3 GetPointAtLinearDistance<T>(this T spline,
            float fromT,
            float relativeDistance,
            out float resultPointT) where T : ISpline
        {
            const float epsilon = 0.001f;
            if (fromT < 0)
            {
                resultPointT = 0f;
                return spline.EvaluatePosition(0f);
            }

            var length = spline.GetLength();
            var lengthAtT = fromT * length;
            float currentLength = lengthAtT;
            if (currentLength + relativeDistance >= length) //relativeDistance >= 0 -> Forward search
            {
                resultPointT = 1f;
                return spline.EvaluatePosition(1f);
            }
            else if (currentLength + relativeDistance <= 0) //relativeDistance < 0 -> Forward search
            {
                resultPointT = 0f;
                return spline.EvaluatePosition(0f);
            }

            var currentPos = spline.EvaluatePosition(fromT);
            resultPointT = fromT;

            var forwardSearch = relativeDistance >= 0;
            var residual = math.abs(relativeDistance);
            float linearDistance = 0;
            float3 point = currentPos;

            while (residual > epsilon && (forwardSearch ? resultPointT < 1f : resultPointT > 0))
            {
                currentLength += forwardSearch ? residual : -residual;
                resultPointT = currentLength / length;

                if (resultPointT > 1f) //forward search
                    resultPointT = 1f;
                else if (resultPointT < 0f) //backward search
                    resultPointT = 0f;

                point = spline.EvaluatePosition(resultPointT);
                linearDistance = math.distance(currentPos, point);
                residual = math.abs(relativeDistance) - linearDistance;
            }

            return point;
        }

        /// <summary>
        /// Given a normalized interpolation ratio, calculate the associated interpolation value in another <see cref="PathIndexUnit"/> regarding a specific spline.
        /// </summary>
        /// <param name="spline">The Spline to use for the conversion.</param>
        /// <param name="t">Normalized interpolation ratio (0 to 1).</param>
        /// <param name="targetPathUnit">The <see cref="PathIndexUnit"/> to which `t` should be converted.</param>
        /// <typeparam name="T">A type implementing <see cref="ISpline"/>.</typeparam>
        /// <returns>The interpolation value converted to targetPathUnit.</returns>
        public static float ConvertIndexUnit<T>(this T spline, float t, PathIndexUnit targetPathUnit)
            where T : ISpline
        {
            if (targetPathUnit == PathIndexUnit.Normalized)
                return WrapInterpolation(t, spline.Closed);

            return ConvertNormalizedIndexUnit(spline, t, targetPathUnit);
        }

        /// <summary>
        /// Given an interpolation value using one of the various <see cref="PathIndexUnit"/> types, calculate the associated interpolation value in another <see cref="PathIndexUnit"/> regarding a specific spline.
        /// </summary>
        /// <param name="spline">The spline to use for the conversion.</param>
        /// <param name="value">Interpolation value in the original <see cref="PathIndexUnit"/> `fromPathUnit`.</param>
        /// <param name="fromPathUnit">The <see cref="PathIndexUnit"/> for the original interpolation value type.</param>
        /// <param name="targetPathUnit">The <see cref="PathIndexUnit"/> to which `value` should be converted.</param>
        /// <typeparam name="T">A type implementing <see cref="ISpline"/>.</typeparam>
        /// <returns>The interpolation value converted to targetPathUnit.</returns>
        public static float ConvertIndexUnit<T>(this T spline, float value, PathIndexUnit fromPathUnit, PathIndexUnit targetPathUnit)
            where T : ISpline
        {
            if (fromPathUnit == targetPathUnit)
            {
                if (targetPathUnit == PathIndexUnit.Normalized)
                    value = WrapInterpolation(value, spline.Closed);

                return value;
            }

            return ConvertNormalizedIndexUnit(spline, GetNormalizedInterpolation(spline, value, fromPathUnit), targetPathUnit);
        }

        static float ConvertNormalizedIndexUnit<T>(T spline, float t, PathIndexUnit targetPathUnit) where T : ISpline
        {
            switch (targetPathUnit)
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

        static float WrapInterpolation(float t, bool closed)
        {
            if (!closed)
                return math.clamp(t, 0f, 1f);

            return t % 1f == 0f ? math.clamp(t, 0f, 1f) : t - math.floor(t);
        }

        /// <summary>
        /// Given an interpolation value in any PathIndexUnit type, calculate the normalized interpolation ratio value
        /// relative to a <see cref="Spline"/>.
        /// </summary>
        /// <param name="spline">The Spline to use for the conversion, this is necessary to compute Normalized and Distance PathIndexUnits.</param>
        /// <param name="t">The `t` value to normalize in the original PathIndexUnit.</param>
        /// <param name="originalPathUnit">The PathIndexUnit from the original `t`.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <returns>The normalized interpolation ratio (0 to 1).</returns>
        public static float GetNormalizedInterpolation<T>(T spline, float t, PathIndexUnit originalPathUnit) where T : ISpline
        {
            switch (originalPathUnit)
            {
                case PathIndexUnit.Knot:
                    return WrapInterpolation(CurveToSplineT(spline, t), spline.Closed);
                case PathIndexUnit.Distance:
                    var length = spline.GetLength();
                    return WrapInterpolation(length > 0 ? t / length : 0f, spline.Closed);
                default:
                    return WrapInterpolation(t, spline.Closed);
            }
        }

        /// <summary>
        /// Gets the index of a knot that precedes a spline index. This method uses the <see cref="Spline.Count"/>
        /// and <see cref="Spline.Closed"/> properties to ensure that it returns the correct index of the knot.
        /// </summary>
        /// <param name="spline">The spline to consider.</param>
        /// <param name="index">The current index to consider.</param>
        /// <typeparam name="T">A type that implements ISpline.</typeparam>
        /// <returns>Returns a knot index that precedes the `index` on the considered spline.</returns>
        public static int PreviousIndex<T>(this T spline, int index) where T : ISpline
            => PreviousIndex(index, spline.Count, spline.Closed);

        /// <summary>
        /// Gets the index of a knot that follows a spline index. This method uses the <see cref="Spline.Count"/> and
        /// <see cref="Spline.Closed"/> properties to ensure that it returns the correct index of the knot.
        /// </summary>
        /// <param name="spline">The spline to consider.</param>
        /// <param name="index">The current index to consider.</param>
        /// <typeparam name="T">A type that implements ISpline.</typeparam>
        /// <returns>The knot index after `index` on the considered spline.</returns>
        public static int NextIndex<T>(this T spline, int index) where T : ISpline
            => NextIndex(index, spline.Count, spline.Closed);

        /// <summary>
        /// Gets the <see cref="BezierKnot"/> before spline[index]. This method uses the <see cref="Spline.Count"/>
        /// and <see cref="Spline.Closed"/> properties to ensure that it returns the correct knot.
        /// </summary>
        /// <param name="spline">The spline to consider.</param>
        /// <param name="index">The current index to consider.</param>
        /// <typeparam name="T">A type that implements ISpline.</typeparam>
        /// <returns>The knot before the knot at spline[index].</returns>
        public static BezierKnot Previous<T>(this T spline, int index) where T : ISpline
            => spline[PreviousIndex(spline, index)];

        /// <summary>
        /// Gets the <see cref="BezierKnot"/> after spline[index]. This method uses the <see cref="Spline.Count"/>
        /// and <see cref="Spline.Closed"/> properties to ensure that it returns the correct knot.
        /// </summary>
        /// <param name="spline">The spline to consider.</param>
        /// <param name="index">The current index to consider.</param>
        /// <typeparam name="T">A type that implements ISpline.</typeparam>
        /// <returns>The knot after the knot at spline[index].</returns>
        public static BezierKnot Next<T>(this T spline, int index) where T : ISpline
            => spline[NextIndex(spline, index)];

        internal static int PreviousIndex(int index, int count, bool wrap)
        {
            return wrap ? (index + (count - 1)) % count : math.max(index - 1, 0);
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
        // todo Deprecate in 3.0 - this is not a correct uniform catmull rom parameterization.
        public static float3 GetCatmullRomTangent(float3 previous, float3 next) =>
            GetAutoSmoothTangent(previous, next, CatmullRomTension);

        /// <summary>
        /// Calculates a tangent from the previous and next knot positions.
        /// </summary>
        /// <param name="previous">The position of the previous <see cref="BezierKnot"/>.</param>
        /// <param name="next">The position of the next <see cref="BezierKnot"/>.</param>
        /// <param name="tension">Set the length of the tangent vectors.</param>
        /// <returns>Returns a tangent calculated from the previous and next knot positions.</returns>
        public static float3 GetAutoSmoothTangent(float3 previous, float3 next, float tension = DefaultTension)
        {
            if (next.Equals(previous))
                return 0f;

            return (next - previous) / math.sqrt(math.length(next - previous)) * tension;
        }

        /// <summary>
        /// Gets a tangent from the previous, current, and next knot positions.
        /// </summary>
        /// <param name="previous">The position of the previous <see cref="BezierKnot"/>.</param>
        /// <param name="current">The position of the current <see cref="BezierKnot"/>.</param>
        /// <param name="next">The position of the next <see cref="BezierKnot"/>.</param>
        /// <param name="tension">The length of the tangent vectors.</param>
        /// <returns>Returns a tangent calculated from the previous, current, and next knot positions.</returns>
        public static float3 GetAutoSmoothTangent(float3 previous, float3 current, float3 next, float tension = DefaultTension)
        {
            var d1 = math.length(current - previous);
            var d2 = math.length(next - current);
            
            if (d1 == 0f) // If we're only working with 2 valid points, then calculate roughly Uniform parametrization tangent and scale it down so we can atleast build rotation.
                return (next - current) * 0.1f; 
            else if (d2 == 0f)
                return (current - previous) * 0.1f;

            // Calculations below are based on (pp. 5-6): http://www.cemyuksel.com/research/catmullrom_param/catmullrom_cad.pdf
            // Catmull-Rom parameterization: a = 0 - Uniform, a = 0.5 - Centripetal, a = 1.0 - Chordal.
            var a = tension;
            var twoA = 2f * tension;
            
            var d1PowA = math.pow(d1, a);
            var d1Pow2A = math.pow(d1, twoA); 
            var d2PowA = math.pow(d2, a);
            var d2Pow2A = math.pow(d2, twoA);
            
            return (d1Pow2A * next - d2Pow2A * previous + (d2Pow2A - d1Pow2A) * current) / (3f * d1PowA * (d1PowA +  d2PowA));
        }

        static float3 GetUniformAutoSmoothTangent(float3 previous, float3 next, float tension)
        {
            return (next - previous) * tension;
        }

        /// <summary>
        /// Gets a <see cref="BezierKnot"/> with its tangents and rotation calculated using the previous and next knot positions.
        /// </summary>
        /// <param name="position">The position of the knot.</param>
        /// <param name="previous">The knot that immediately precedes the requested knot.</param>
        /// <param name="next">The knot that immediately follows the requested knot.</param>
        /// <returns>A <see cref="BezierKnot"/> with tangent and rotation values calculated from the previous and next knot positions.</returns>
        public static BezierKnot GetAutoSmoothKnot(float3 position, float3 previous, float3 next) =>
            GetAutoSmoothKnot(position, previous, next, math.up());

        /// <summary>
        /// Gets a <see cref="BezierKnot"/> with its tangents and rotation calculated using the previous and next knot positions.
        /// </summary>
        /// <param name="position">The position of the knot.</param>
        /// <param name="previous">The knot that immediately precedes the requested knot.</param>
        /// <param name="next">The knot that immediately follows the requested knot.</param>
        /// <param name="normal">The normal vector of the knot.</param>
        /// <returns>A <see cref="BezierKnot"/> with tangent and rotation values calculated from the previous and next knot positions.</returns>
        public static BezierKnot GetAutoSmoothKnot(float3 position, float3 previous, float3 next, float3 normal) =>
            GetAutoSmoothKnot(position, previous, next, normal, CatmullRomTension);

        /// <summary>
        /// Gets a <see cref="BezierKnot"/> with its tangents and rotation calculated using the previous and next knot positions.
        /// </summary>
        /// <param name="position">The position of the knot.</param>
        /// <param name="previous">The knot that immediately precedes the requested knot.</param>
        /// <param name="next">The knot that immediately follows the requested knot.</param>
        /// <param name="normal">The normal vector of the knot.</param>
        /// <param name="tension">Set the length of the tangent vectors.</param>
        /// <returns>A <see cref="BezierKnot"/> with tangent and rotation values calculated from the previous and next knot positions.</returns>
        public static BezierKnot GetAutoSmoothKnot(float3 position, float3 previous, float3 next, float3 normal, float tension = DefaultTension)
        {
            var tanIn = GetAutoSmoothTangent(next, position, previous, tension);
            var tanOut = GetAutoSmoothTangent(previous, position, next, tension);
            var dirIn = new float3(0, 0, math.length(tanIn));
            var dirOut = new float3(0, 0, math.length(tanOut));
            var dirRot = tanOut;

            if (dirIn.z == 0f)
                dirIn.z = dirOut.z;
            if (dirOut.z == 0f)
            {
                dirOut.z = dirIn.z;
                dirRot = -tanIn;
            }

            return new BezierKnot(position, -dirIn, dirOut, GetKnotRotation(dirRot, normal));
        }

        internal static quaternion GetKnotRotation(float3 tangent, float3 normal)
        {
            if (math.lengthsq(tangent) == 0f)
                tangent = math.rotate(Quaternion.FromToRotation(math.up(), normal), math.forward());

            float3 up = Mathf.Approximately(math.abs(math.dot(math.normalizesafe(tangent), math.normalizesafe(normal))), 1f)
                ? math.cross(math.normalizesafe(tangent), math.right())
                : Vector3.ProjectOnPlane(normal, tangent).normalized;

            return quaternion.LookRotationSafe(math.normalizesafe(tangent), up);
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
        /// Computes a <see cref="Spline"/> to approximate the curve formed by the list of provided points
        /// within the given error threshold.
        /// </summary>
        /// <param name="points">The list of <see cref="float3"/> points that define the curve to approximate.</param>
        /// <param name="errorThreshold">The error threshold to use. Represents the largest distance between any of the
        /// provided points and the curve.</param>
        /// <param name="closed">Whether to close the <see cref="Spline"/>.</param>
        /// <param name="spline">The output <see cref="Spline"/> fitted to the points, or an empty spline if could not be fitted.</param>
        /// <returns>Whether a curve could be fitted according to the provided error threshold.</returns>
        public static bool FitSplineToPoints(List<float3> points, float errorThreshold, bool closed,
            out Spline spline)
        {
            /*
                Implementation based on:
                "Algorithm for Automatically Fitting Digitized Curves"
                by Philip J. Schneider
                "Graphics Gems", Academic Press, 1990
            */

            spline = new Spline();
            var maxIterations = 4;

            var leftTangent = math.normalize(points[1] - points[0]);
            var rightTangent = math.normalize(points[points.Count - 1] - points[points.Count - 2]);

            if (points.Count == 2)
            {
                var difference = points[0] - points[1];
                var diffLength = math.sqrt(math.pow(difference.x, 2) + math.pow(difference.y, 2) +
                                              math.pow(difference.z, 2));

                var quat1 = GetKnotRotation(leftTangent, math.normalize(math.cross(leftTangent, math.right())));
                //we use 1/3 of the distance between each of the points as described in the graphics gems paper.
                var firstKnot = new BezierKnot(points[0], new float3(0, 0, -diffLength / 3f),
                    new float3(0, 0, diffLength / 3f), quat1);

                var quat2 = GetKnotRotation(rightTangent, math.normalize(math.cross(rightTangent, math.right())));
                var secondKnot = new BezierKnot(points[1], new float3(0, 0, -diffLength / 3f),
                    new float3(0, 0, diffLength / 3f), quat2);

                spline = new Spline(new BezierKnot[2] { firstKnot, secondKnot }, closed);
                return true;
            }

            //find corresponding t values for each point so we can compute errors (chord-length parameterization)
            var correspondingTValues = new float[points.Count];

            var firstPassTValues = new float[points.Count];

            firstPassTValues[0] = 0f;

            var cumulativeChordLengths = new float[points.Count - 1];

            var chordLengthSum = 0f;
            float3 chord;
            for (int i = 1; i < points.Count; i++)
            {
                chord = points[i] - points[i - 1];
                chordLengthSum += math.sqrt(
                    chord.x * chord.x +
                    chord.y * chord.y +
                    chord.z * chord.z
                );
                cumulativeChordLengths[i - 1] = chordLengthSum;
            }

            for (int i = 0; i < cumulativeChordLengths.Length; i++)
            {
                firstPassTValues[i + 1] = cumulativeChordLengths[i] / chordLengthSum;
            }

            correspondingTValues = firstPassTValues;

            spline = GenerateSplineFromTValues(points, closed, correspondingTValues);

            var tPositions = new float3[points.Count];
            for (int i = 0; i < correspondingTValues.Length; i++)
            {
                float t = correspondingTValues[i];
                float3 position = spline.EvaluatePosition(t);
                tPositions[i] = position;
            }

            (float, int) errorResult = ComputeMaxError(points, tPositions);

            var errorBeforeReparameterization = errorResult.Item1;
            var splitPoint = errorResult.Item2;

            if (errorBeforeReparameterization < errorThreshold)
            {
                return true;
            }
            //if error is small enough, try reparameterizing and fitting again
            else if (errorBeforeReparameterization < 4 * errorThreshold)
            {
                var numIterations = 0;
                while (numIterations < maxIterations)
                {
                    var curveKnots = new BezierKnot[spline.Count];
                    int i = 0;
                    foreach (BezierKnot bezierKnot in spline.Knots)
                    {
                        curveKnots[i] = bezierKnot;
                        ++i;
                    }

                    var P0 = curveKnots[0].Position;
                    var P1 = curveKnots[0].Position + math.rotate(curveKnots[0].Rotation, curveKnots[0].TangentOut);
                    var P2 = curveKnots[1].Position + math.rotate(curveKnots[1].Rotation, curveKnots[1].TangentIn);
                    var P3 = curveKnots[1].Position;

                    var P = new float3[] { P0, P1, P2, P3 };

                    var q1 = new float3[3];
                    for (int j = 0; j <= 2; ++j)
                    {
                        q1[j] = (P[j + 1] - P[j]) * 3f;
                    }

                    var q2 = new float3[2];
                    for (int j = 0; j <= 1; ++j)
                    {
                        q2[j] = (q1[j + 1] - q1[j]) * 2f;
                    }

                    for (int k = 0; k < points.Count; k++)
                    {
                        var t = correspondingTValues[k];
                        var q = Bernstein(t, P, 3);

                        var q1_t = Bernstein(t, q1, 2);
                        var q2_t = Bernstein(t, q2, 1);

                        var numerator = math.dot(q - tPositions[k], q1_t);
                        var denominator = math.dot(q1_t, q1_t) + math.dot(q - tPositions[k], q2_t);
                        if (denominator != 0)
                            correspondingTValues[k] -= (numerator / denominator);
                    }

                    spline = GenerateSplineFromTValues(points, closed, correspondingTValues);

                    tPositions = new float3[points.Count];
                    for (int k = 0; k < correspondingTValues.Length; k++)
                    {
                        var t = correspondingTValues[k];
                        var position = spline.EvaluatePosition(t);
                        tPositions[k] = position;
                    }

                    errorResult = ComputeMaxError(points, tPositions);
                    var errorAfterReparameterization = errorResult.Item1;
                    splitPoint = errorResult.Item2;
                    if (errorAfterReparameterization < errorThreshold)
                    {
                        return true;
                    }
                    numIterations++;
                }
            }

            //still not good enough. Try splitting at point of max error.
            if (points.Count == 3) splitPoint = 1;
            else
            {
                if (splitPoint == 0)
                    splitPoint++;
                else if (splitPoint == points.Count - 1)
                    splitPoint--;
            }
            Spline firstSpline, secondSpline;
            if (!FitSplineToPoints(points.GetRange(0, splitPoint + 1), errorThreshold, false, out firstSpline) ||
                !FitSplineToPoints(points.GetRange(splitPoint, points.Count - splitPoint), errorThreshold, false,
                    out secondSpline))
            {
                spline = new Spline();
                return false;
            }

            var firstKnots = new BezierKnot[firstSpline.Count];
            var secondKnots = new BezierKnot[secondSpline.Count];
            firstSpline.CopyTo(firstKnots, 0);
            secondSpline.CopyTo(secondKnots, 0);

            var allKnots = new BezierKnot[firstKnots.Length + secondKnots.Length - 1];

            var splitPointTangentIn = math.rotate(firstKnots[firstKnots.Length - 1].Rotation, firstKnots[firstKnots.Length - 1].TangentIn);
            firstKnots.CopyTo(allKnots, 0);
            secondKnots.CopyTo(allKnots, firstKnots.Length - 1);
            allKnots[firstKnots.Length - 1].TangentIn = math.rotate(math.inverse(allKnots[firstKnots.Length - 1].Rotation), splitPointTangentIn);

            float3 firstKnotPos = allKnots[0].Position, lastKnotPos = allKnots[allKnots.Length - 1].Position;
            bool firstIsLast = (
                new float3(
                    (float)Math.Round(firstKnotPos.x, 2),
                    (float)Math.Round(firstKnotPos.y, 2),
                    (float)Math.Round(firstKnotPos.z, 2)
                ).Equals(new float3(
                    (float)Math.Round(lastKnotPos.x, 2),
                    (float)Math.Round(lastKnotPos.y, 2),
                    (float)Math.Round(lastKnotPos.z, 2))
                )
            );

            if (closed && firstIsLast)
            {
                var firstPointTangentOut = math.rotate(allKnots[0].Rotation, allKnots[0].TangentOut);
                var prevAllKnots = allKnots;
                allKnots = new BezierKnot[allKnots.Length - 1];
                for (int i = 0; i < allKnots.Length; ++i)
                {
                    allKnots[i] = prevAllKnots[i + 1];
                }

                allKnots[allKnots.Length - 1].TangentOut = math.rotate(math.inverse(allKnots[allKnots.Length - 1].Rotation), firstPointTangentOut);
            }
            spline = new Spline(allKnots, closed);
            return true;
        }

        //A helper method for FitSplineToPoints that computes bernstein basis functions
        static float3 Bernstein(float t, float3[] bezier, int degree)
        {

            var copy = new float3[bezier.Length];
            bezier.CopyTo(copy, 0);

            for (int i = 1; i <= degree; ++i)
            {
                for (int j = 0; j <= degree - i; ++j)
                {
                    copy[j] = (1 - t) * copy[j] + t * copy[j + 1];
                }
            }

            return copy[0];
        }

        //A helper method for FitSplineToPoints that generates a spline from a provided array of
        // interpolation values.
        static Spline GenerateSplineFromTValues(List<float3> points, bool closed, float[] tValues)
        {
            var leftTangent = math.normalize(points[1] - points[0]);
            var rightTangent = math.normalize(points[points.Count - 2] - points[points.Count - 1]);

            var aLeft = new float3[points.Count];
            var aRight = new float3[points.Count];
            for (int i = 0; i < points.Count; ++i)
            {
                var t = tValues[i];
                aLeft[i] = leftTangent * 3 * t * (1 - t) * (1 - t);
                aRight[i] = rightTangent * 3 * t * t * (1 - t);
            }

            var c = new float[2, 2];

            float x1 = 0f, x2 = 0f;

            for (int i = 0; i < points.Count; ++i)
            {
                var t = tValues[i];
                c[0, 0] += math.dot(aLeft[i], aLeft[i]);
                c[0, 1] += math.dot(aLeft[i], aRight[i]);
                c[1, 0] += math.dot(aLeft[i], aRight[i]);
                c[1, 1] += math.dot(aRight[i], aRight[i]);

                var tmp = points[i] - (
                    points[0] * (1 - t) * (1 - t) * (1 - t)
                    + points[0] * 3 * (1 - t) * (1 - t) * t
                    + points[points.Count - 1] * 3 * (1 - t) * t * t
                    + points[points.Count - 1] * t * t * t
                );

                x1 += math.dot(aLeft[i], tmp);
                x2 += math.dot(aRight[i], tmp);
            }

            var c0_c1_determinant = c[0, 0] * c[1, 1] - c[1, 0] * c[0, 1];
            var c0_x_determinant = c[0, 0] * x2 - c[1, 0] * x1;
            var x_c1_determinant = x1 * c[1, 1] - x2 * c[0, 1];

            //now we compute the coefficients for our tangents
            var alpha1 = c0_c1_determinant == 0 ? 0 : x_c1_determinant / c0_c1_determinant;
            var alpha2 = c0_c1_determinant == 0 ? 0 : c0_x_determinant / c0_c1_determinant;

            quaternion quat1 = GetKnotRotation(leftTangent, math.normalize(math.cross(leftTangent, math.right())));
            var first = new BezierKnot(points[0], new float3(0, 0, -alpha1), new float3(0, 0, alpha1), quat1);

            quaternion quat2 = GetKnotRotation(-rightTangent, math.normalize(math.cross(-rightTangent, math.right())));
            var second = new BezierKnot(points[points.Count - 1], new float3(0, 0, -alpha2),
                new float3(0, 0, alpha2), quat2);

            return new Spline(new BezierKnot[2] { first, second }, closed);
        }

        //Helper function for FitSplineToPoints that computes the maximum least squares distance error
        // and the index of the point for which the error is maximum.
        static (float maxError, int maxErrorIndex) ComputeMaxError(List<float3> points, float3[] positions)
        {
            //use least squares difference to evaluate error.
            var maxError = 0f;
            var splitPoint = 0;
            for (int i = 0; i < points.Count; ++i)
            {
                var difference = points[i] - positions[i];
                var error = math.sqrt(
                    difference.x * difference.x +
                    difference.y * difference.y +
                    difference.z * difference.z
                );

                if (error > maxError)
                {
                    maxError = error;
                    splitPoint = i;
                }
            }

            return (maxError, splitPoint);
        }

        /// <summary>
        /// Creates a new spline and adds it to the <see cref="ISplineContainer"/>.
        /// </summary>
        /// <param name="container">The target container.</param>
        /// <typeparam name="T">A type that implements <see cref="ISplineContainer"/>.</typeparam>
        /// <returns>Returns the spline that was created and added to the container.</returns>
        public static Spline AddSpline<T>(this T container) where T : ISplineContainer
        {
            var spline = new Spline();
            AddSpline(container, spline);
            return spline;
        }

        /// <summary>
        /// Add a new <see cref="Spline"/> to the <see cref="ISplineContainer"/>.
        /// </summary>
        /// <param name="container">The target container.</param>
        /// <param name="spline">The spline to append to this container.</param>
        /// <typeparam name="T">A type that implements <see cref="ISplineContainer"/>.</typeparam>
        public static void AddSpline<T>(this T container, Spline spline) where T : ISplineContainer
        {
            var splines = new List<Spline>(container.Splines);
            splines.Add(spline);
            container.Splines = splines;
        }

        /// <summary>
        /// Removes a spline from a <see cref="ISplineContainer"/>.
        /// </summary>
        /// <param name="container">The target container.</param>
        /// <param name="splineIndex">The index of the spline to remove from the SplineContainer.</param>
        /// <typeparam name="T">A type that implements <see cref="ISplineContainer"/>.</typeparam>
        /// <returns>Returns true if the spline was removed from the container.</returns>
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
        /// Removes a spline from a <see cref="ISplineContainer"/>.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="spline">The spline to remove from the SplineContainer.</param>
        /// <typeparam name="T">A type that implements <see cref="ISplineContainer"/>.</typeparam>
        /// <returns>Returns true if the spline was removed from the container.</returns>
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

        /// <summary>
        /// Reorders a spline in a <see cref="ISplineContainer"/>.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="previousSplineIndex">The previous index of the spline to reorder in the SplineContainer.</param>
        /// <param name="newSplineIndex">The new index of the spline to reorder in the SplineContainer.</param>
        /// <typeparam name="T">A type that implements <see cref="ISplineContainer"/>.</typeparam>
        /// <returns>Returns true if the spline was reordered in the container.</returns>
        public static bool ReorderSpline<T>(this T container, int previousSplineIndex, int newSplineIndex) where T : ISplineContainer
        {
            if (previousSplineIndex < 0 || previousSplineIndex >= container.Splines.Count ||
                newSplineIndex < 0 || newSplineIndex >= container.Splines.Count)
                return false;

            var splines = new List<Spline>(container.Splines);
            var spline = splines[previousSplineIndex];
            splines.RemoveAt(previousSplineIndex);
            splines.Insert(newSplineIndex, spline);

            container.KnotLinkCollection.SplineIndexChanged(previousSplineIndex, newSplineIndex);
            container.Splines = splines;

            return true;
        }

        internal static bool IsIndexValid<T>(T container, SplineKnotIndex index) where T : ISplineContainer
        {
            return index.Knot >= 0 && index.Knot < container.Splines[index.Spline].Count &&
                index.Spline < container.Splines.Count && index.Knot < container.Splines[index.Spline].Count;
        }

        /// <summary>
        /// Sets the position of all knots linked to the knot at `index` in an <see cref="ISplineContainer"/> to the same position.
        /// </summary>
        /// <param name="container">The target container.</param>
        /// <param name="index">The `SplineKnotIndex` of the knot to use to synchronize the positions.</param>
        /// <typeparam name="T">A type that implements <see cref="ISplineContainer"/>.</typeparam>
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
                var originalPosition = knot.Position; 
                knot.Position = position;
                splines[i.Spline].SetKnotNoNotify(i.Knot, knot);

                // If the knot has been moved, notifies a knotModified event
                if (!Mathf.Approximately(Vector3.Distance(position, originalPosition), 0f))
                    splines[i.Spline].SetDirty(SplineModification.KnotModified, i.Knot);
            }
        }

        /// <summary>
        /// Links two knots in an <see cref="ISplineContainer"/>. The two knots can be on different splines, but both must be in the referenced SplineContainer.
        /// If these knots are linked to other knots, all existing links are kept and updated.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="knotA">The first knot to link.</param>
        /// <typeparam name="T">A type that implements <see cref="ISplineContainer"/>.</typeparam>
        /// <param name="knotB">The second knot to link.</param>
        public static void LinkKnots<T>(this T container, SplineKnotIndex knotA, SplineKnotIndex knotB) where T : ISplineContainer
        {
            bool similarPositions = Mathf.Approximately(math.length(container.Splines[knotA.Spline][knotA.Knot].Position - container.Splines[knotB.Spline][knotB.Knot].Position), 0f);
            var knotsToNotify = similarPositions ? null : container.KnotLinkCollection.GetKnotLinks(knotB);   
            
            container.KnotLinkCollection.Link(knotA, knotB);

            if (knotsToNotify != null)
            {
                foreach (var ski in knotsToNotify)
                    container.Splines[ski.Spline].SetDirty(SplineModification.KnotModified, ski.Knot);
            }
        }

        /// <summary>
        /// Unlinks several knots from an <see cref="ISplineContainer"/>. A knot in `knots` disconnects from other knots it was linked to.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="knots">The knot to unlink.</param>
        /// <typeparam name="T">A type implementing <see cref="ISplineContainer"/>.</typeparam>
        public static void UnlinkKnots<T>(this T container, IReadOnlyList<SplineKnotIndex> knots) where T : ISplineContainer
        {
            foreach (var knot in knots)
                container.KnotLinkCollection.Unlink(knot);
        }

        /// <summary>
        /// Checks if two knots from an <see cref="ISplineContainer"/> are linked together.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="knotA">The first knot to check.</param>
        /// <param name="knotB">The second knot to check against.</param>
        public static bool AreKnotLinked(this ISplineContainer container, SplineKnotIndex knotA, SplineKnotIndex knotB)
        {
            if (!container.KnotLinkCollection.TryGetKnotLinks(knotA, out var linkedKnots))
                return false;

            for (int i = 0; i < linkedKnots.Count; ++i)
                if (linkedKnots[i] == knotB)
                    return true;

            return false;
        }
        
        /// <summary>
        /// Copies knot links between two splines of the same <see cref="ISplineContainer"/>.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="srcSplineIndex">The index of the source spline to copy from.</param>
        /// <param name="destSplineIndex">The index of the destination spline to copy to.</param>
        /// <typeparam name="T">A type implementing <see cref="ISplineContainer"/>.</typeparam>
        /// <remarks>
        /// The knot links will only be copied if both of the spline indices are valid and both splines have the same amount of knots.
        /// </remarks>
        public static void CopyKnotLinks<T>(this T container, int srcSplineIndex, int destSplineIndex) where T : ISplineContainer
        {
            if ((srcSplineIndex < 0 || srcSplineIndex >= container.Splines.Count) ||
                (destSplineIndex < 0 || destSplineIndex >= container.Splines.Count))
                return;

            var srcSpline = container.Splines[srcSplineIndex];
            var dstSpline = container.Splines[destSplineIndex];

            if (srcSpline.Count == 0 || srcSpline.Count != dstSpline.Count)
                return;

            for (int i = 0, c = srcSpline.Count; i < c; ++i)
            {
                if (container.KnotLinkCollection.TryGetKnotLinks(new SplineKnotIndex(srcSplineIndex, i), out _))
                    container.KnotLinkCollection.Link(new SplineKnotIndex(srcSplineIndex, i), new SplineKnotIndex(destSplineIndex, i));
            }
        }

        /// <summary>
        /// Removes redundant points in a poly line to form a similar shape with fewer points.
        /// </summary>
        /// <param name="line">The poly line to act on.</param>
        /// <param name="epsilon">The maximum distance from the reduced poly line shape for a point to be discarded.</param>
        /// <typeparam name="T">The collection type. Usually this will be list or array of float3.</typeparam>
        /// <returns>Returns a new list with a poly line matching the shape of the original line with fewer points.</returns>
        public static List<float3> ReducePoints<T>(T line, float epsilon = .15f) where T : IList<float3>
        {
            var ret = new List<float3>();
            var rdp = new RamerDouglasPeucker<T>(line);
            rdp.Reduce(ret, epsilon);
            return ret;
        }

        /// <summary>
        /// Removes redundant points in a poly line to form a similar shape with fewer points.
        /// </summary>
        /// <param name="line">The poly line to act on.</param>
        /// <param name="results">A pre-allocated list to be filled with the new reduced line points.</param>
        /// <param name="epsilon">The maximum distance from the reduced poly line shape for a point to be discarded.</param>
        /// <typeparam name="T">The collection type. Usually this will be list or array of float3.</typeparam>
        public static void ReducePoints<T>(T line, List<float3> results, float epsilon = .15f) where T : IList<float3>
        {
            var rdp = new RamerDouglasPeucker<T>(line);
            rdp.Reduce(results, epsilon);
        }
        
        internal static bool AreTangentsModifiable(TangentMode mode)
        {
            return mode == TangentMode.Broken || mode == TangentMode.Continuous || mode == TangentMode.Mirrored;
        }

        /// <summary>
        /// Reverses the flow direction of a spline.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="splineIndex">The index of the spline to reverse.</param>
        public static void ReverseFlow(this ISplineContainer container, int splineIndex)
        {
            ReverseFlow(new SplineInfo(container, splineIndex));
        }

        /// <summary>
        /// Reverses the flow direction of a spline.
        /// </summary>
        /// <param name="splineInfo">The spline to reverse.</param>
        public static void ReverseFlow(SplineInfo splineInfo)
        {
            var spline = splineInfo.Spline;

            var knots = splineInfo.Spline.ToArray();
            var tangentModes = new TangentMode[spline.Count];

            for (int i = 0; i < tangentModes.Length; ++i)
                tangentModes[i] = spline.GetTangentMode(i);

            var splineLinks = new List<List<SplineKnotIndex>>();

            // GetAll LinkedKnots on the spline
            for(int previousKnotIndex = 0; previousKnotIndex < spline.Count; ++previousKnotIndex)
            {
                var knot = new SplineKnotIndex(splineInfo.Index, previousKnotIndex);
                var collection = splineInfo.Container.KnotLinkCollection.GetKnotLinks(knot).ToList();
                splineLinks.Add(collection);
            }

            //Unlink all knots in the spline
            foreach(var linkedKnots in splineLinks)
                splineInfo.Container.UnlinkKnots(linkedKnots);

            // Reverse order and tangents
            for (int previousKnotIndex = 0; previousKnotIndex < spline.Count; ++previousKnotIndex)
            {
                var knot = knots[previousKnotIndex];
                var worldKnot = knot.Transform(splineInfo.LocalToWorld);
                var tangentIn = worldKnot.TangentIn;
                var tangentOut = worldKnot.TangentOut;

                var reverseRotation = quaternion.AxisAngle(math.mul(knot.Rotation, math.up()), math.radians(180));
                reverseRotation = math.normalizesafe(reverseRotation);

                // Reverse the tangents to keep the same shape while reversing the order
                knot.Rotation = math.mul(reverseRotation, knot.Rotation);
                if(tangentModes[previousKnotIndex] is TangentMode.Broken)
                {
                    var localRot = quaternion.AxisAngle(math.up(), math.radians(180));
                    knot.TangentIn = math.rotate(localRot, tangentOut);
                    knot.TangentOut = math.rotate(localRot, tangentIn);
                }
                else if(tangentModes[previousKnotIndex] is TangentMode.Continuous)
                {
                    knot.TangentIn = -tangentOut;
                    knot.TangentOut = -tangentIn;
                }

                var newKnotIndex = spline.Count - 1 - previousKnotIndex;
                spline.SetTangentMode(newKnotIndex, tangentModes[previousKnotIndex]);
                spline[newKnotIndex] = knot;
            }

            //Redo all links
            foreach (var linkedKnots in splineLinks)
            {
                if(linkedKnots.Count == 1)
                    continue;

                var originalKnot = linkedKnots[0];
                originalKnot = new SplineKnotIndex(originalKnot.Spline, 
                    originalKnot.Spline.Equals(splineInfo.Index) ? spline.Count - 1 - originalKnot.Knot : originalKnot.Knot);
                for(int i = 1; i < linkedKnots.Count; ++i)
                {
                    var knotInfo = linkedKnots[i];
                    if (knotInfo.Spline.Equals(splineInfo.Index))
                        linkedKnots[i] = new SplineKnotIndex(splineInfo.Index, spline.Count - 1 - knotInfo.Knot);
                    
                    splineInfo.Container.LinkKnots(originalKnot, linkedKnots[i]);
                }
            }
        }

        /// <summary>
        /// Reverses the flow direction of a spline. Should only be used with Splines that aren't inside any container.
        /// </summary>
        /// <param name="spline">The spline to reverse.</param>
        public static void ReverseFlow(Spline spline)
        {
            var knots = spline.ToArray();
            var tangentModes = new TangentMode[spline.Count];

            for (int i = 0; i < tangentModes.Length; ++i)
                tangentModes[i] = spline.GetTangentMode(i);

            // Reverse order and tangents
            for (int previousKnotIndex = 0; previousKnotIndex < spline.Count; ++previousKnotIndex)
            {
                var knot = knots[previousKnotIndex];
                var tangentIn = knot.TangentIn;
                var tangentOut = knot.TangentOut;

                var reverseRotation = quaternion.AxisAngle(math.mul(knot.Rotation, math.up()), math.radians(180));
                reverseRotation = math.normalizesafe(reverseRotation);

                // Reverse the tangents to keep the same shape while reversing the order
                knot.Rotation = math.mul(reverseRotation, knot.Rotation);
                if (tangentModes[previousKnotIndex] is TangentMode.Broken)
                {
                    var localRot = quaternion.AxisAngle(math.up(), math.radians(180));
                    knot.TangentIn = math.rotate(localRot, tangentOut);
                    knot.TangentOut = math.rotate(localRot, tangentIn);
                }
                else if (tangentModes[previousKnotIndex] is TangentMode.Continuous)
                {
                    knot.TangentIn = -tangentOut;
                    knot.TangentOut = -tangentIn;
                }

                var newKnotIndex = spline.Count - 1 - previousKnotIndex;
                spline.SetTangentMode(newKnotIndex, tangentModes[previousKnotIndex]);
                spline[newKnotIndex] = knot;
            }
        }
        
        /// <summary>
        /// Joins two splines together at the specified knots. The two splines must belong to the same container and
        /// the knots must be at an extremity of their respective splines.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="mainKnot">The first spline extremity to join.</param>
        /// <param name="otherKnot">The second spline extremity to join.</param>
        /// <returns> The `SplineKnotIndex` of the junction knot.</returns>
        /// <remarks>
        /// In case a spline needs to be reversed to join the two extremities, the mainKnot defines which spline will be kept.
        /// Hence, the second one will be the reversed one.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// An exception is thrown on impossible join request (out of bounds
        /// parameters, knots on the same spline, non-extremity knots)
        /// </exception>
        public static SplineKnotIndex JoinSplinesOnKnots(this ISplineContainer container, SplineKnotIndex mainKnot, SplineKnotIndex otherKnot)
        {
            if (mainKnot.Spline == otherKnot.Spline)
            {
                Debug.LogError("Trying to join Knots already belonging to the same spline.");
                return new SplineKnotIndex();
            }

            if (mainKnot.Spline < 0 || mainKnot.Spline > container.Splines.Count)
            {
                Debug.LogError($"Spline index {mainKnot.Spline} does not exist for the current container.");
                return new SplineKnotIndex();
            }
            if(otherKnot.Spline < 0 || otherKnot.Spline > container.Splines.Count)
            {
                Debug.LogError($"Spline index {otherKnot.Spline} does not exist for the current container.");
                return new SplineKnotIndex();
            }
            
            if(mainKnot.Knot < 0 || mainKnot.Knot > container.Splines[mainKnot.Spline].Count)
            {
                Debug.LogError($"Knot index {mainKnot.Knot} does not exist for the current container for Spline[{mainKnot.Spline}].");
                return new SplineKnotIndex();
            }
            if(otherKnot.Knot < 0 || otherKnot.Knot > container.Splines[otherKnot.Spline].Count)
            {
                Debug.LogError($"Knot index {otherKnot.Knot} does not exist for the current container for Spline[{otherKnot.Spline}].");
                return new SplineKnotIndex();
            }
            
            if(mainKnot.Knot != 0 && mainKnot.Knot != container.Splines[mainKnot.Spline].Count - 1)
            {
                Debug.LogError($"Knot index {mainKnot.Knot} is not an extremity knot for the current container for Spline[{mainKnot.Spline}]." +
                    "Only extremity knots can be joined.");
                return new SplineKnotIndex();
            }
            if(otherKnot.Knot != 0 && otherKnot.Knot != container.Splines[otherKnot.Spline].Count - 1)
            {
                Debug.LogError($"Knot index {otherKnot.Knot} is not an extremity knot for the current container for Spline[{otherKnot.Spline}]." +
                    "Only extremity knots can be joined.");
                return new SplineKnotIndex();
            }
            
            var isActiveKnotAtStart = mainKnot.Knot == 0;
            var isOtherKnotAtStart = otherKnot.Knot == 0;

            //Reverse spline if needed, this is needed when the 2 knots are both starts or ends of their respective spline
            if(isActiveKnotAtStart == isOtherKnotAtStart)
                //We give more importance to the main knot, so we reverse the spline associated to otherKnot
                container.ReverseFlow(otherKnot.Spline);

            //Save Links
            var links = new List<List<SplineKnotIndex>>();

            //Save relevant data before joining the splines
            var activeSplineIndex = mainKnot.Spline;
            var activeSpline = container.Splines[activeSplineIndex];
            var activeSplineCount = activeSpline.Count;
            var otherSplineIndex = otherKnot.Spline;
            var otherSpline = container.Splines[otherSplineIndex];
            var otherSplineCount = otherSpline.Count;
            
            // Get all LinkedKnots on the splines
            for(int i = 0; i < activeSplineCount; ++i)
            {
                var knot = new SplineKnotIndex(mainKnot.Spline, i);
                links.Add(container.KnotLinkCollection.GetKnotLinks(knot).ToList());
            }
            for(int i = 0; i < otherSplineCount; ++i)
            {
                var knot = new SplineKnotIndex(otherKnot.Spline, i);
                links.Add(container.KnotLinkCollection.GetKnotLinks(knot).ToList());
            }
            
            //Unlink all knots in the spline
            foreach(var linkedKnots in links)
                container.UnlinkKnots(linkedKnots);

            if(otherSplineCount > 1)
            {
                //Join Splines
                if(isActiveKnotAtStart)
                {
                    //All position from the other spline must be added before the knot A
                    //Don't copy the last knot of the other spline as this is the one to join
                    for(int i = otherSplineCount - 2; i >= 0 ; i--)
                        activeSpline.Insert(0,otherSpline[i], otherSpline.GetTangentMode(i));
                }
                else
                {
                    //All position from the other spline must be added after the knot A
                    //Don't copy the first knot of the other spline as this is the one to join
                    for(int i = 1; i < otherSplineCount; i++)
                        activeSpline.Add(otherSpline[i], otherSpline.GetTangentMode(i));
                }
            }

            container.RemoveSplineAt(otherSplineIndex);
            var newActiveSplineIndex = otherSplineIndex > activeSplineIndex ? activeSplineIndex : mainKnot.Spline - 1;
            
            //Restore links
            foreach (var linkedKnots in links)
            {
                if(linkedKnots.Count == 1)
                    continue;

                for(int i = 0; i < linkedKnots.Count; ++i)
                {
                    var knotInfo = linkedKnots[i];
                    if(knotInfo.Spline == activeSplineIndex || knotInfo.Spline == otherSplineIndex)
                    {
                        var newIndex = knotInfo.Knot;

                        if(knotInfo.Spline == activeSplineIndex && isActiveKnotAtStart)
                            newIndex += otherSplineCount - 1;

                        if(knotInfo.Spline == otherSplineIndex && !isActiveKnotAtStart)
                            newIndex += activeSplineCount - 1;

                        linkedKnots[i] = new SplineKnotIndex(activeSplineIndex, newIndex);
                    }
                    else
                    {
                        if(knotInfo.Spline > otherSplineIndex)
                            linkedKnots[i] = new SplineKnotIndex(knotInfo.Spline - 1,knotInfo.Knot);
                    }
                }
                
                var originalKnot = linkedKnots[0];
                for(int i = 1; i < linkedKnots.Count; ++i)
                    container.LinkKnots(originalKnot, linkedKnots[i]);
            }
            
            return new SplineKnotIndex(newActiveSplineIndex, isActiveKnotAtStart ? otherSplineCount - 1 : mainKnot.Knot);
        }
        
        internal static SplineKnotIndex DuplicateKnot(this ISplineContainer container, SplineKnotIndex originalKnot, int targetIndex)
        {
            var spline = container.Splines[originalKnot.Spline];
            var knot = spline[originalKnot.Knot];
            spline.Insert(targetIndex, knot);
            spline.SetTangentMode(targetIndex, spline.GetTangentMode(originalKnot.Knot));
            return new SplineKnotIndex(originalKnot.Spline, targetIndex);
        }
        
        /// <summary>
        /// Duplicate a spline between 2 knots of a source spline.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="fromKnot">The start knot to use to duplicate the spline.</param>
        /// <param name="toKnot">The end knot to use to duplicate the spline.</param>
        /// <param name="newSplineIndex">The index of the new created spline in the container.</param>
        /// <exception cref="ArgumentException">Thrown when the provided knots aren't valid or aren't on the same spline.</exception>
        public static void DuplicateSpline(
            this ISplineContainer container, 
            SplineKnotIndex fromKnot, 
            SplineKnotIndex toKnot, 
            out int newSplineIndex)
        {
            newSplineIndex = -1;

            if (!(fromKnot.IsValid() && toKnot.IsValid()))
                throw new ArgumentException("Duplicate failed: The 2 provided knots must be valid knots.");

            if(fromKnot.Spline != toKnot.Spline)
                throw new ArgumentException("Duplicate failed: The 2 provided knots must be on the same Spline.");

            var duplicate = container.AddSpline();

            //Copy knots to the new spline
            int startIndex = Math.Min(fromKnot.Knot, toKnot.Knot);
            int toIndex = Math.Max(fromKnot.Knot, toKnot.Knot);

            var originalSplineIndex = fromKnot.Spline;
            var originalSpline = container.Splines[originalSplineIndex];
            newSplineIndex = container.Splines.Count - 1;
            for (int i = startIndex; i <= toIndex; ++i)
            {
                duplicate.Add(originalSpline[i], originalSpline.GetTangentMode(i));

                // If the old knot had any links we link both old and new knot.
                // This will result in the new knot linking to what the old knot was linked to.
                // The old knot being removed right after that takes care of cleaning the old knot from the link.
                if (container.KnotLinkCollection.TryGetKnotLinks(new SplineKnotIndex(originalSplineIndex, i), out _))
                    container.KnotLinkCollection.Link(new SplineKnotIndex(originalSplineIndex, i), new SplineKnotIndex(newSplineIndex, i - startIndex));
            }
        }

        /// <summary>
        /// Splits a spline in a SplineContainer into two splines at a specified knot.
        /// </summary>
        /// <param name="container">The target SplineContainer.</param>
        /// <param name="knotInfo">The SplineKnotIndex of the spline to split. </param>
        /// <returns>The `SplineKnotIndex` of the first knot of the spline that was created from the split.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// An exception is thrown when the knot belongs to a spline not contained by
        /// the provided container or when the knot index is out of range.
        /// </exception>
        public static SplineKnotIndex SplitSplineOnKnot(this ISplineContainer container, SplineKnotIndex knotInfo)
        {
            if (knotInfo.Spline < 0 || knotInfo.Spline > container.Splines.Count)
            {
                throw new IndexOutOfRangeException($"Spline index {knotInfo.Spline} does not exist for the current container.");
            }

            if (knotInfo.Knot < 0 || knotInfo.Knot > container.Splines[knotInfo.Spline].Count)
            {
                throw new IndexOutOfRangeException($"Knot index {knotInfo.Knot} does not exist for the current container for Spline[{knotInfo.Spline}].");
            }

            var originalSpline = container.Splines[knotInfo.Spline];
            if (originalSpline.Closed)
            {
                // Unclose and add a knot to the end with the same data
                originalSpline.Closed = false;
                var firstKnot = new SplineKnotIndex(knotInfo.Spline, 0);
                var lastKnot = container.DuplicateKnot(firstKnot, originalSpline.Count);

                // If the knot was the first one of the spline nothing else needs to be done to split the knot
                if (knotInfo.Knot == 0)
                    return firstKnot;

                // If the knot wasn't the first one we also need need to link both ends of the spline to keep the same spline we had before
                // Link knots is recording the changes to prefab instances so we don't need to add a call to RecordPrefabInstancePropertyModifications 
                container.LinkKnots(firstKnot, lastKnot);
            }
            // If not closed, split does nothing on the extremity of the spline
            else if (knotInfo.Knot == 0 || knotInfo.Knot == originalSpline.Count - 1)
                return knotInfo;

            container.DuplicateSpline(knotInfo, new SplineKnotIndex(knotInfo.Spline, originalSpline.Count - 1),
                out int newSplineIndex);
            originalSpline.Resize(knotInfo.Knot + 1);
            return new SplineKnotIndex(newSplineIndex, 0);
        }
    }
}
