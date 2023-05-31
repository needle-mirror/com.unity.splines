using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    class RamerDouglasPeucker<T> where T : IList<float3>
    {
        T m_Points;
        bool[] m_Keep;
        float m_Epsilon;
        int m_KeepCount;

        // not using C# Range type because we need to operate on lists and iterate without making copies of arrays
        struct Range
        {
            public int Start;
            public int Count;
            // inclusive end
            public int End => Start + Count - 1;

            public Range(int start, int count)
            {
                Start = start;
                Count = count;
            }

            public override string ToString() => $"[{Start}, {End}]";
        }

        public RamerDouglasPeucker(T points)
        {
            m_Points = points;
        }

        public void Reduce(List<float3> results, float epsilon)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            m_Epsilon = math.max(float.Epsilon, epsilon);
            m_KeepCount = m_Points.Count;
            m_Keep = new bool[m_KeepCount];
            for(int i = 0; i < m_KeepCount; i++) Keep(i);
            Reduce(new Range(0, m_KeepCount));

            results.Clear();

            if (results.Capacity < m_KeepCount)
                results.Capacity = m_KeepCount;

            for(int i = 0; i < m_Keep.Length; ++i)
                if (m_Keep[i])
                    results.Add(m_Points[i]);
        }

        void Keep(int index) => m_Keep[index] = true;

        void Discard(Range range)
        {
            m_KeepCount -= range.Count;
            for (int i = range.Start; i <= range.End; ++i)
                m_Keep[i] = false;
        }

        void Reduce(Range range)
        {
            if (range.Count < 3)
                return;

            var farthest = FindFarthest(range);

            if (farthest.distance < m_Epsilon)
            {
                Discard(new Range(range.Start + 1, range.Count - 2));
                return;
            }

            Reduce(new Range(range.Start, farthest.index - range.Start + 1));
            Reduce(new Range(farthest.index, (range.End - farthest.index) + 1));
        }

        (int index, float distance) FindFarthest(Range range)
        {
            float distance = 0f;
            int index = -1;

            for (int i = range.Start+1; i < range.End; ++i)
            {
                var d = SplineMath.DistancePointLine(m_Points[i], m_Points[range.Start], m_Points[range.End]);
                if (d > distance)
                {
                    distance = d;
                    index = i;
                }
            }

            return (index, distance);
        }
    }
}
