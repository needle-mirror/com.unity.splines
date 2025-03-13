using System;
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

        static readonly Color s_OutlineColor = new Color(0f, 0f, 0f, .5f);

        /// <summary>
        /// Draw a line gizmo for a <see cref="ISplineContainer"/>.
        /// </summary>
        /// <param name="container">An object implementing the ISplineContainer interface. Usually this will be a MonoBehaviour.</param>
        public static void DrawGizmos(ISplineContainer container)
        {
            var splines = container.Splines;
            if (splines == null)
                return;

            Gizmos.matrix = ((MonoBehaviour)container).transform.localToWorldMatrix;
            foreach (var spline in splines)
            {
                if(spline == null || spline.Count < 2)
                    continue;

                Vector3[] positions;
                SplineCacheUtility.GetCachedPositions(spline, out positions);

#if UNITY_2023_1_OR_NEWER
                Gizmos.DrawLineStrip(positions, false);
#else
                for (int i = 1; i < positions.Length; ++i)
                    Gizmos.DrawLine(positions[i-1], positions[i]);
#endif
            }
            Gizmos.matrix = Matrix4x4.identity;
        }

        /// <summary>
        /// Draw a line gizmo for a <see cref="ISplineProvider"/>.
        /// </summary>
        /// <param name="provider">An object implementing the ISplineProvider interface. Usually this will be a MonoBehaviour.</param>
        [Obsolete("Use the overload that uses " + nameof(ISplineContainer))]
        public static void DrawGizmos(ISplineProvider provider)
        {
            var splines = provider.Splines;
            if (splines == null)
                return;

            Gizmos.matrix = ((MonoBehaviour)provider).transform.localToWorldMatrix;
            foreach (var spline in splines)
            {
                if (spline == null || spline.Count < 2)
                    continue;

                Vector3[] positions;
                SplineCacheUtility.GetCachedPositions(spline, out positions);

#if UNITY_2023_1_OR_NEWER
                Gizmos.DrawLineStrip(positions, false);
#else
                for (int i = 1; i < positions.Length; ++i)
                    Gizmos.DrawLine(positions[i-1], positions[i]);
#endif
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}