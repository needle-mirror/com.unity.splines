using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class SplineCacheUtility
    {
        const int k_SegmentsCount = 32;
        
        static Dictionary<Spline, Vector3[]> s_SplineCacheTable = new Dictionary<Spline, Vector3[]>();

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorSplineUtility.AfterSplineWasModified += delegate(Spline spline)
            {
                if(s_SplineCacheTable.ContainsKey(spline))
                    s_SplineCacheTable[spline] = null;
            };
            
            Spline.afterSplineWasModified += (Spline s) => ClearCache();
            Undo.undoRedoPerformed +=  ClearCache;
        }

        internal static void ClearCache()
        {
            s_SplineCacheTable.Clear();
        }

        public static void GetCachedPositions(Spline spline, out Vector3[] positions)
        {
            if(!s_SplineCacheTable.ContainsKey(spline))
                s_SplineCacheTable.Add(spline, null);

            int count = spline.Closed ? spline.Count : spline.Count - 1;

            if(s_SplineCacheTable[spline] == null)
            {
                s_SplineCacheTable[spline] = new Vector3[count * k_SegmentsCount];
                CacheCurvePositionsTable(spline, count);
            }
            positions = s_SplineCacheTable[spline];
        }
       
        static void CacheCurvePositionsTable(Spline spline, int curveCount, int segments = k_SegmentsCount)
        {
            float inv = 1f / (segments - 1);
            for(int i = 0; i < curveCount; ++i)
            {
                var curve = spline.GetCurve(i);
                var startIndex = i * k_SegmentsCount;
                for(int n = 0; n < segments; n++)
                    (s_SplineCacheTable[spline])[startIndex + n] = CurveUtility.EvaluatePosition(curve, n * inv);
            }
        }
    }
}
