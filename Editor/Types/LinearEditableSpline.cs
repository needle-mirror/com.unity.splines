using System;
using System.Collections.Generic;
using Unity.Mathematics;
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
            localTangentIn = float3.zero;
            localTangentOut = float3.zero;
        }
        
        public override CurveData GetPreviewCurveForEndKnot(float3 point, float3 normal, float3 tangentOut)
        {
            CreatePreviewKnotsIfNeeded();
            
            m_PreviewKnotB.position = point;
            if (knotCount > 0)
            {
                var lastKnot = GetKnot(knotCount - 1);
                m_PreviewKnotA.Copy(lastKnot);
            }

            return new CurveData(m_PreviewKnotA, m_PreviewKnotB);
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
