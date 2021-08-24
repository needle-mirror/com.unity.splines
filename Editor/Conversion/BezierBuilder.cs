using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace UnityEditor.Splines
{
    struct BezierBuilder
    {
        readonly List<BezierKnot> m_ResultKnots;
        readonly bool m_Closed;

        int knotCount => m_ResultKnots.Count;
        public int segmentCount => m_Closed ? m_ResultKnots.Count : m_ResultKnots.Count - 1;

        public BezierBuilder(List<BezierKnot> result, bool closed, int targetKnotCount)
        {
            m_ResultKnots = result;
            for (int i = 0; i < targetKnotCount; ++i)
            {
                var knot = new BezierKnot();
                knot.Rotation = quaternion.identity;
                
                m_ResultKnots.Add(knot);
            }

            m_Closed = closed;
        }
        
        public void SetKnot(int index, float3 position, float3 tangentIn, float3 tangentOut, quaternion rotation)
        {
            var current = m_ResultKnots[index];
            
            current.Position = position;
            current.TangentIn = tangentIn;
            current.TangentOut = tangentOut;
            current.Rotation = rotation;
            
            m_ResultKnots[index] = current;
        }

        void GetSegmentEndIndex(int index, out int endIndex)
        {
            endIndex = m_Closed ? (index + 1) % knotCount : index + 1;
        }

        public void SetSegment(int index, float3 posA, float3 tangentOutA, quaternion rotationA, float3 posB, float3 tangentInB,  quaternion rotationB)
        {
            GetSegmentEndIndex(index, out int nextIndex);
            var current = m_ResultKnots[index];
            current.Position = posA;
            current.Rotation = rotationA;
            current.TangentOut = tangentOutA;

            var next = m_ResultKnots[nextIndex];
            next.Position = posB;
            next.Rotation = rotationB;
            next.TangentIn = tangentInB;

            if (!m_Closed)
            {
                if (index == 0)
                    current.TangentIn = -current.TangentOut;
                else if (nextIndex == knotCount - 1)
                    next.TangentOut = -next.TangentIn;
            }

            m_ResultKnots[index] = current;
            m_ResultKnots[nextIndex] = next;
        }
    }
}
