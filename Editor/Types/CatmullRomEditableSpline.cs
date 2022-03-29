using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [Serializable]
    sealed class CatmullRomEditableKnot : EditableKnot
    {
        public override void OnKnotInsertedOnCurve(EditableKnot previous, EditableKnot next, float t)
        {
            if (spline is CatmullRomEditableSpline splineCR)
            {
                var tangentOut = splineCR.GetTangentOut(index);
                var normalRotation = math.nlerp(previous.rotation, next.rotation, t);
                rotation = quaternion.LookRotationSafe(tangentOut, math.rotate(normalRotation, math.up()));
                
                tangentOut = splineCR.GetTangentOut(previous.index);
                previous.rotation = quaternion.LookRotationSafe(tangentOut, math.rotate(previous.rotation, math.up()));
                
                tangentOut = splineCR.GetTangentOut(next.index);
                next.rotation = quaternion.LookRotationSafe(tangentOut, math.rotate(next.rotation, math.up()));
            }
        }
    }
    
    [Serializable]
    sealed class CatmullRomEditableSpline : EditableSpline<CatmullRomEditableKnot>
    {
        struct CatmullRomSegment
        {
            public float3 p0;
            public float3 t0;
            public quaternion r0;

            public float3 p1;
            public float3 t1;
            public quaternion r1;
        }
        
        public override void OnKnotAddedAtEnd(EditableKnot knot, float3 normal, float3 _)
        {
            if (knot.spline is CatmullRomEditableSpline splineCR)
            {
                var tangentOut = Vector3.ProjectOnPlane(Quaternion.FromToRotation(math.up(), normal) * splineCR.GetTangentOut(knot.index), normal);
                knot.rotation = quaternion.LookRotationSafe(math.normalize(tangentOut), normal);
                
                if (knot.index != 0)
                {
                    knot.spline.GetPreviousKnot(knot.index, out var prevKnot);
                    var prevNormal = math.rotate(prevKnot.rotation, math.up());
                    tangentOut = Vector3.ProjectOnPlane(Quaternion.FromToRotation(math.up(), prevNormal) * splineCR.GetTangentOut(prevKnot.index), prevNormal);
                    prevKnot.rotation = quaternion.LookRotationSafe(math.normalize(tangentOut), prevNormal);
                }
            }
        }
        
        CatmullRomSegment GetSegment(int index)
        {
            if (!closed && index > knotCount - 1)
                throw new IndexOutOfRangeException();
            
            return GetSegment(GetKnot(index), GetKnot((index + 1) % knotCount));
        }

        CatmullRomSegment GetSegment(EditableKnot a, EditableKnot b)
        {
            var p0 = a.localPosition;
            var t0 = GetTangentOut(a, a.GetPrevious(), b);
            var r0 = a.localRotation;

            var p1 = b.localPosition;
            var t1 = -GetTangentOut(b, a, b.GetNext());
            var r1 = b.localRotation;

            return new CatmullRomSegment { p0 = p0, t0 = t0, r0 = r0, p1 = p1, t1 = t1, r1 = r1 };
        }

        public override void GetLocalTangents(EditableKnot knot, out float3 localTangentIn, out float3 localTangentOut)
        {
            localTangentOut = GetTangentOut(knot.index) / 3.0f;
            localTangentIn = -localTangentOut / 3.0f;
        }

        public Vector3 GetTangentOut(int index)
        {
            var currentKnot = GetKnot(index);
            GetPreviousKnot(index, out EditableKnot previousKnot);
            GetNextKnot(index, out EditableKnot nextKnot);

            return GetTangentOut(currentKnot, previousKnot, nextKnot);
        }

        public static Vector3 GetTangentOut(EditableKnot knot, EditableKnot previousKnot, EditableKnot nextKnot)
        {
            if (previousKnot == null && nextKnot == null)
                return Vector3.forward;

            if (previousKnot == null)
                return nextKnot.localPosition - knot.localPosition;

            if (nextKnot == null)
                return knot.localPosition - previousKnot.localPosition;

            return 0.5f * (nextKnot.localPosition - previousKnot.localPosition);
        }
        
        public override CurveData GetPreviewCurveForEndKnot(float3 point, float3 normal, float3 _)
        {
            CreatePreviewKnotsIfNeeded();
            
            if (knotCount == 0)
            { 
                m_PreviewKnotB.position = point;
                m_PreviewKnotB.rotation = quaternion.LookRotationSafe(Quaternion.FromToRotation(math.up(), normal) * math.forward(), normal);

                m_PreviewKnotA.Copy(m_PreviewKnotB);
            }
            else
            {
                var lastKnot = GetKnot(knotCount - 1);
                m_PreviewKnotA.Copy(lastKnot);
                m_PreviewKnotB.Copy(m_PreviewKnotA);
                m_PreviewKnotB.position = point;
                
                var prevNormal = math.rotate(lastKnot.rotation, math.up());
                var tangentOut = Vector3.ProjectOnPlane(Quaternion.FromToRotation(math.up(), normal) * GetTangentOut(m_PreviewKnotB, lastKnot, null), normal);
                m_PreviewKnotB.rotation = quaternion.LookRotationSafe(math.normalize(tangentOut), normal);
                
                tangentOut = Vector3.ProjectOnPlane(Quaternion.FromToRotation(math.up(), prevNormal) * GetTangentOut(lastKnot, lastKnot.GetPrevious(), m_PreviewKnotB), prevNormal);
                m_PreviewKnotA.rotation = quaternion.LookRotationSafe(math.normalize(tangentOut), prevNormal);
            }

            return new CurveData(m_PreviewKnotA, m_PreviewKnotB);
        }

        public override float3 GetPointOnCurve(CurveData curve, float t)
        {
            var localToWorld = localToWorldMatrix;
            var segment = GetSegment(curve.a, curve.b);

            segment.p0 = localToWorld.MultiplyPoint3x4(segment.p0);
            segment.t0 = localToWorld.MultiplyVector(segment.t0);
            segment.p1 = localToWorld.MultiplyPoint3x4(segment.p1);
            segment.t1 = localToWorld.MultiplyVector(-segment.t1);
            var t2 = t * t;
            var t3 = t * t * t;
            
            return (2.0f * t3 - 3.0f * t2 + 1.0f) * segment.p0
                   + (t3 - 2.0f * t2 + t) * segment.t0
                   + (-2.0f * t3 + 3.0f * t2) * segment.p1
                   + (t3 - t2) * segment.t1;
        }

        public override void ToBezier(List<BezierKnot> results)
        {
            var builder = new BezierBuilder(results, closed, knotCount);
            if (builder.segmentCount == 0)
            {
                var knot = GetKnot(0);
                var tangentIn = -math.forward() / 3.0f;
                builder.SetKnot(0, knot.position, tangentIn, -tangentIn, knot.rotation);
            }
            else
            {
                for (int i = 0; i < builder.segmentCount; ++i)
                {
                    var segment = GetSegment(i);
                    var tangentOut = math.rotate(math.inverse(segment.r0), segment.t0 / 3.0f);
                    var tangentIn =  math.rotate(math.inverse(segment.r1), segment.t1 / 3.0f);
                    builder.SetSegment(i, segment.p0, tangentOut, segment.r0, segment.p1, tangentIn, segment.r1);
                }
            }
        }

        public override void FromBezier(IReadOnlyList<BezierKnot> knots)
        {
            Resize(knots.Count);
            for (int i = 0; i < knotCount; ++i)
            {
                var knot = GetKnot(i);
                knot.localPosition = knots[i].Position;
                knot.localRotation = knots[i].Rotation;
            }
        }
    }
}
