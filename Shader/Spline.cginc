struct BezierCurve
{
    float3 P0;
    float3 P1;
    float3 P2;
    float3 P3;
};

struct BezierKnot
{
    float3 Position;
    float3 TangentIn;
    float3 TangentOut;
    float3x3 Rotation;
};

typedef float4 SplineInfo;
uint GetKnotCount(SplineInfo info) { return info.x; }
bool GetSplineClosed(SplineInfo info) { return info.y > 0; }
float GetSplineLength(SplineInfo info) { return info.z; }

float SplineToCurveT(const SplineInfo info, const StructuredBuffer<float> curveLengths, const float splineT)
{
    const uint knotCount = GetKnotCount(info);
    const bool closed = GetSplineClosed(info);
    const float targetLength = saturate(splineT) * GetSplineLength(info);
    float start = 0;

    for (int i = 0, c = closed ? knotCount : knotCount - 1; i < c; ++i)
    {
        const float curveLength = curveLengths[i];

        if (targetLength <= (start + curveLength))
        {
            // knot index unit stores curve index in integer part, and curve t in fractional. that means it cannot accurately
            // represent the absolute end of a spline. so instead we check for it and return a value that's really close.
            // if we don't check, this method would happily return knotCount+1, which is fine for closed loops but not open.
            return i + clamp((targetLength - start) / curveLength, 0, .9999);
        }

        start += curveLength;
    }

    return closed ? 0 : knotCount-2 + .9999;
}

BezierCurve GetCurve(const BezierKnot a, const BezierKnot b)
{
    BezierCurve curve;
    curve.P0 = a.Position;
    curve.P1 = mul(a.Rotation, a.TangentOut);
    curve.P2 = mul(b.Rotation, b.TangentIn);
    curve.P3 = b.Position;
    return curve;
}

float3 EvaluatePosition(const BezierCurve curve, const float curve_t)
{
    const float t = saturate(curve_t);
    const float oneMinusT = 1. - t;
    return oneMinusT * oneMinusT * oneMinusT * curve.P0 +
       3. * oneMinusT * oneMinusT * t * curve.P1 +
       3. * oneMinusT * t * t * curve.P2 +
       t * t * t * curve.P3;
}

float3 EvaluatePosition(const SplineInfo info,
    const StructuredBuffer<BezierKnot> knots,
    const StructuredBuffer<float> curveLengths,
    const float splineT)
{
    const float curve = SplineToCurveT(info, curveLengths, splineT);
    const int a = floor(curve);
    return EvaluatePosition(GetCurve(knots[a], knots[(a+1)%GetKnotCount(info)]), frac(curve));
}

float3 EvaluateTangent(const BezierCurve curve, const float curve_t)
{
    const float t = saturate(curve_t);
    const float oneMinusT = 1 - t;
    const float oneMinusT2 = oneMinusT * oneMinusT;
    const float t2 = t * t;
    return -3 * curve.P0 * oneMinusT2
        + 3 * curve.P1 * (oneMinusT2 - 2 * t * oneMinusT)
        + 3 * curve.P2 * (-t2 + oneMinusT * 2 * t)
        + 3 * curve.P3 * t2;
}
