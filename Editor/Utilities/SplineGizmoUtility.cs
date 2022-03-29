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

        static Vector3[] s_Points;
        static Vector3 s_CameraUp;
        static Color s_OutlineColor = new Color(0f, 0f, 0f, .5f);
        
        /// <summary>
        /// Draw a line gizmo for a <see cref="ISplineProvider"/>.
        /// </summary>
        /// <param name="provider">An object implementing the ISplineProvider interface. Usually this will be a MonoBehaviour.</param>
        public static void DrawGizmos(ISplineProvider provider)
        {
            var splines = provider.Splines;

            if (splines == null)
                return;

            s_CameraUp = SceneView.lastActiveSceneView.camera.transform.up;
            var localToWorld = ((MonoBehaviour)provider).transform.localToWorldMatrix;
            foreach(var spline in splines)
            {
                if(spline == null || spline.Count < 2)
                    continue;

                Vector3[] positions;
                SplineCacheUtility.GetCachedPositions(spline, out positions);

                var color = Gizmos.color;
                var from = localToWorld.MultiplyPoint(positions[0]);
                var previousDir = Vector3.zero;
                for(int i = 1; i < positions.Length; ++i)
                {
                    var to = localToWorld.MultiplyPoint(positions[i]);
                
                    var center = ( from + to ) / 2f;
                    var size = .1f * HandleUtility.GetHandleSize(center);
                
                    var dir = to - from;
                    var delta = previousDir.magnitude == 0 ? 1f :Vector3.Dot(previousDir, dir.normalized);
                    //If the angle is too wide between 2 positions, take the previous position to draw the line
                    if(delta < 0.9f)
                    {
                        Gizmos.color = color;
                        DrawLineSegment(from, from + previousDir, size);
                        from = from + previousDir;
                        dir = to - from;
                    }
                    //Is the second position far enough to draw the segment
                    if(i == positions.Length-1 || dir.magnitude > size)
                    {
                        Gizmos.color = color;
                        DrawLineSegment(from, to, size);
                        from = to;
                        previousDir = Vector3.zero;
                    }
                    else
                        previousDir = dir;
                }
                Gizmos.matrix = Matrix4x4.identity; 
            }
        }

        static void DrawLineSegment(Vector3 from, Vector3 to, float size)
        {
            Gizmos.DrawLine(from, to);
            Gizmos.color = s_OutlineColor;
            //make the gizmo a little thicker
            var offset = size * s_CameraUp / 7.5f;
            Gizmos.DrawLine(from - offset, to - offset);
        }
    }
}
