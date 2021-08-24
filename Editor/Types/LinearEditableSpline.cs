using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [Serializable]
    sealed class LinearEditableSpline : EditableSpline<EditableKnot>
    {
        public override float3 GetPointOnCurve(CurveData curve, float t)
        {
            return curve.a.position + (curve.b.position - curve.a.position) * t;
        }
        
        public override void GetLocalTangents(EditableKnot knot, out float3 localTangentIn, out float3 localTangentOut)
        {
            var previousKnot = knot.GetPrevious();
            var nextKnot = knot.GetNext();

            if (previousKnot == null && nextKnot == null)
            {
                localTangentIn = knot.ToSplineSpaceTangent(-math.forward() / 3.0f);
                localTangentOut = -localTangentIn;
            }
            else
            {
                localTangentIn = previousKnot != null
                    ? SplineUtility.GetLinearTangent(knot.position, previousKnot.localPosition)
                    : float3.zero;
                localTangentOut = nextKnot != null
                    ? SplineUtility.GetLinearTangent(knot.position, nextKnot.localPosition)
                    : float3.zero;
            }

            if (!closed)
            {
                if (knot.index == 0 && math.length(localTangentIn) == 0f)
                    localTangentIn = -localTangentOut;
                else if (knot.index == knotCount - 1 && math.length(localTangentOut) == 0f)
                    localTangentOut = -localTangentIn;
            }
        }

        public override void ToBezier(List<BezierKnot> results)
        {
            for (int i = 0; i < knotCount; ++i)
            {
                var editKnot = GetKnot(i);
                var position = editKnot.localPosition;
                GetLocalTangents(editKnot, out var tangentIn, out var tangentOut);

                var knot = new BezierKnot(position, tangentIn, tangentOut, editKnot.localRotation);
                results.Add(knot);
            }
        }

        public override void FromBezier(IReadOnlyList<BezierKnot> knots)
        {
            Resize(knots.Count);
            for (int i = 0; i < knots.Count; ++i)
            {
                var knot = GetKnot(i);
                knot.localPosition = knots[i].Position;
                knot.localRotation = knots[i].Rotation;
            }
        }
    }
}
