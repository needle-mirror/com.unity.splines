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
        public override void OnKnotAddedToPathEnd(float3 position, float3 normal)
        {
            if (spline is CatmullRomEditableSpline splineCR)
            {
                var tangentOut = Quaternion.FromToRotation(math.up(), normal) * splineCR.GetTangent(index);
                rotation = quaternion.LookRotation(tangentOut, normal);

                if (index != 0)
                {
                    spline.GetPreviousKnot(index, out var prevKnot);
                    tangentOut = math.rotate(prevKnot.rotation, splineCR.GetTangent(prevKnot.index));
                    prevKnot.rotation = quaternion.LookRotation(tangentOut, math.rotate(prevKnot.rotation, math.up()));
                }
            }
        }

        public override void OnKnotInsertedOnCurve(EditableKnot previous, EditableKnot next, float t)
        {
            if (spline is CatmullRomEditableSpline splineCR)
            {
                var tangentOut = splineCR.GetTangent(index);
                var normalRotation = math.nlerp(previous.rotation, next.rotation, t);
                rotation = quaternion.LookRotation(tangentOut, math.rotate(normalRotation, math.up()));
                
                tangentOut = splineCR.GetTangent(previous.index);
                previous.rotation = quaternion.LookRotation(tangentOut, math.rotate(previous.rotation, math.up()));
                
                tangentOut = splineCR.GetTangent(next.index);
                next.rotation = quaternion.LookRotation(tangentOut, math.rotate(next.rotation, math.up()));
            }
        }
    }
    
    [Serializable]
    sealed class CatmullRomEditableSpline : EditableSpline<CatmullRomEditableKnot>
    {
        (Vector3 p0, Vector3 t0, quaternion r0, Vector3 p1, Vector3 t1, quaternion r1) GetSegment(int index)
        {
            if (!closed && index > knotCount - 1)
                throw new IndexOutOfRangeException();

            var knot = GetKnot(index);
            var p0 = knot.localPosition;
            var t0 = math.forward() * math.length(GetTangent(index));
            var r0 = knot.localRotation;
            
            var next = (index + 1) % knotCount;
            knot = GetKnot(next);
            var p1 = knot.localPosition;
            var t1 = -math.forward() * math.length(GetTangent(next));
            var r1 = knot.localRotation;

            return (p0, t0, r0, p1, t1, r1);
        }

        public override void GetLocalTangents(EditableKnot knot, out float3 localTangentIn, out float3 localTangentOut)
        {
            localTangentOut = knot.ToSplineSpaceTangent((GetTangent(knot.index) / 3.0f));
            localTangentIn = knot.ToSplineSpaceTangent(-localTangentOut / 3.0f);
        }

        public Vector3 GetTangent(int index)
        {
            var currentKnot = GetKnot(index);
            var hasPrevious = GetPreviousKnot(index, out EditableKnot previousKnot);
            var hasNext = GetNextKnot(index, out EditableKnot nextKnot);

            if (!hasPrevious && !hasNext)
                return Vector3.forward;

            if (!hasPrevious)
                return nextKnot.localPosition - currentKnot.localPosition;

            if (!hasNext)
                return currentKnot.localPosition - previousKnot.localPosition;

            return 0.5f * (nextKnot.localPosition - previousKnot.localPosition);
        }

        public override float3 GetPointOnCurve(CurveData curve, float t)
        {
            var localToWorld = localToWorldMatrix;
            var (p0, t0, r0, p1, t1, r1) = GetSegment(curve.a.index);
            p0 = localToWorld.MultiplyPoint3x4(p0);
            t0 = localToWorld.MultiplyVector(math.rotate(r0, t0));
            p1 = localToWorld.MultiplyPoint3x4(p1);
            t1 = localToWorld.MultiplyVector(math.rotate(r1, -t1));
            var t2 = t * t;
            var t3 = t * t * t;
            
            return (2.0f * t3 - 3.0f * t2 + 1.0f) * p0
                   + (t3 - 2.0f * t2 + t) * t0
                   + (-2.0f * t3 + 3.0f * t2) * p1
                   + (t3 - t2) * t1;
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
                    var (p0, t0, r0, p1, t1, r1) = GetSegment(i);
                    var tangentOut = t0 / 3.0f;
                    var tangentIn = t1 / 3.0f;
                    builder.SetSegment(i, p0, tangentOut, r0, p1, tangentIn, r1);
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
