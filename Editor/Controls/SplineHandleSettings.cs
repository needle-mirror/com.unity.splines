using System;
using UnityEngine;
using UnityEditor.SettingsManagement;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class SplineHandleSettings
    {
        [UserSetting]
        static readonly Pref<bool> s_FlowDirectionEnabled = new Pref<bool>("Handles.FlowDirectionEnabled", true);

        [UserSetting]
        static readonly Pref<bool> s_ShowAllTangents = new Pref<bool>("Handles.ShowAllTangents", true);

        static readonly Pref<bool> s_ShowKnotIndices = new Pref<bool>("Handles.ShowKnotIndices", false);

        [UserSetting]
        static UserSetting<bool> s_ShowMesh = new UserSetting<bool>(PathSettings.instance,"Handles.Debug.ShowMesh", false, SettingsScope.User);
        [UserSetting]
        static UserSetting<Color> s_MeshColor = new UserSetting<Color>(PathSettings.instance, "Handles.Debug.MeshColor", Color.white, SettingsScope.User);
        [UserSetting]
        static UserSetting<float> s_MeshSize = new UserSetting<float>(PathSettings.instance, "Handles.Debug.MeshSize", 0.1f, SettingsScope.User);
        [UserSetting]
        static UserSetting<int> s_MeshResolution = new UserSetting<int>(PathSettings.instance, "Handles.Debug.MeshResolution", SplineUtility.DrawResolutionDefault, SettingsScope.User);

        [UserSettingBlock("Spline Mesh")]
        static void HandleDebugPreferences(string searchContext)
        {
            EditorGUI.BeginChangeCheck();

            s_MeshColor.value = SettingsGUILayout.SettingsColorField("Color", s_MeshColor, searchContext);
            s_MeshSize.value = SettingsGUILayout.SettingsSlider("Size", s_MeshSize, 0.01f, 1f, searchContext);
            s_MeshResolution.value = SettingsGUILayout.SettingsSlider("Resolution", s_MeshResolution, 4, 100, searchContext);

            if(EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
        }

        public static bool FlowDirectionEnabled
        {
            get => s_FlowDirectionEnabled;
            set => s_FlowDirectionEnabled.SetValue(value);
        }

        public static bool ShowAllTangents
        {
            get => s_ShowAllTangents;
            set => s_ShowAllTangents.SetValue(value);
        }

        public static bool ShowKnotIndices
        {
            get => s_ShowKnotIndices;
            set => s_ShowKnotIndices.SetValue(value);
        }

        public static bool ShowMesh
        {
            get => s_ShowMesh;
            set => s_ShowMesh.SetValue(value);
        }

        public static Color SplineMeshColor => s_MeshColor;
        public static float SplineMeshSize => s_MeshSize;
        public static int SplineMeshResolution => s_MeshResolution;
    }
}
