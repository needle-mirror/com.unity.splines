using System.Collections.Generic;
using UnityEditor.SettingsManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// SplineGizmoUtility provides methods for drawing in-scene representations of Splines.
    /// </summary>
    public static class SplineGizmoUtility
    {
        [UserSetting]
        internal static UserSetting<Color> s_GizmosLineColor = new UserSetting<Color>(PathSettings.instance, "Gizmos.SplineColor", Color.blue, SettingsScope.User);

        [UserSettingBlock("Gizmos")]
        static void GizmosColorPreferences(string searchContext)
        {
            s_GizmosLineColor.value = SettingsGUILayout.SettingsColorField("Splines Color", s_GizmosLineColor, searchContext);
        }
        
        const int k_SegmentsCount = 32;
        static Vector3[] s_Points;

        static Color s_OutlineColor = new Color(0f, 0f, 0f, .5f);

        static Dictionary<Spline, List<Vector3>> s_SplineCache = new Dictionary<Spline, List<Vector3>>();
        static Dictionary<Spline, Vector3[]> s_SplineCacheTable = new Dictionary<Spline, Vector3[]>();
        static Dictionary<Spline, List<BezierCurve>> s_CurveCache = new Dictionary<Spline, List<BezierCurve>>();

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorSplineUtility.afterSplineWasModified += delegate(Spline spline)
            {
                if(s_SplineCache.ContainsKey(spline))
                    s_SplineCache[spline].Clear();
                if(s_CurveCache.ContainsKey(spline))
                {
                    s_CurveCache[spline].Clear();
                    CacheSplineCurves(spline);
                }
            };
        }
        
        /// <summary>
        /// Draw a line gizmo for a <see cref="ISplineProvider"/>.
        /// </summary>
        /// <param name="provider">An object implementing the ISplineProvider interface. Usually this will be a MonoBehaviour.</param>
        public static void DrawGizmos(ISplineProvider provider)
        {
            var splines = provider.Splines;

            if (splines == null)
                return;

            var cameraUp = SceneView.lastActiveSceneView.camera.transform.up;
            var localToWorld = ((MonoBehaviour)provider).transform.localToWorldMatrix;
            foreach(var spline in splines)
            {
                if(spline == null || spline.Count < 2)
                    continue;

                if(!s_SplineCacheTable.ContainsKey(spline))
                    s_SplineCacheTable.Add(spline, null);

                int c = spline.Closed ? spline.Count : spline.Count - 1;

                if(s_SplineCacheTable[spline] == null)
                {
                    s_SplineCacheTable[spline] = new Vector3[c * k_SegmentsCount];
                    CacheCurvePositionsTable(spline, localToWorld, c);
                }

                var color = Gizmos.color;
                var from = localToWorld.MultiplyPoint(s_SplineCacheTable[spline][0]);
                for(int i = 1; i < c * k_SegmentsCount; ++i)
                {
                    var to = localToWorld.MultiplyPoint(s_SplineCacheTable[spline][i]);
                
                    var center = ( from + to ) / 2f;
                    var size = .1f * HandleUtility.GetHandleSize(center);
                
                    var dir = to - from;
                    if(dir.magnitude > size)
                    {
                        Gizmos.DrawLine(from, to);
                        Gizmos.color = s_OutlineColor;
                        //make the gizmo a little thicker
                        var offset = size * cameraUp / 7.5f;
                        Gizmos.DrawLine(from - offset, to - offset);
                
                        from = to;
                        Gizmos.color = color;
                    }
                }
                Gizmos.matrix = Matrix4x4.identity; 
            }
        }

        static void CacheSplineCurves(Spline spline)
        {
            for(int i = 0, c = spline.Closed ? spline.Count : spline.Count - 1; i < c; ++i)
                s_CurveCache[spline].Add(spline.GetCurve(i));
        }
        
        static void CacheCurvePositionsTable(Spline spline, Matrix4x4 localToWorld, int curveCount, int segments = k_SegmentsCount)
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
