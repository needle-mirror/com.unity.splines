using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class SplineHandleSettingsWindow : EditorWindow
    {
        const float k_BorderWidth = 1;

        Toggle m_FlowDirection;
        Toggle m_AllTangents;
        Toggle m_KnotIndices;
        Toggle m_SplineMesh;

        public static void Show(Rect buttonRect)
        {
            var window = CreateInstance<SplineHandleSettingsWindow>();
            window.hideFlags = HideFlags.DontSave;

#if UNITY_2022_1_OR_NEWER
            var popupWidth = 150;
#else
            var popupWidth = 180;
#endif
            window.ShowAsDropDown(GUIUtility.GUIToScreenRect(buttonRect), new Vector2(popupWidth, 80));
        }

        void OnEnable()
        {
            Color borderColor = EditorGUIUtility.isProSkin ? new Color(0.44f, 0.44f, 0.44f, 1f) : new Color(0.51f, 0.51f, 0.51f);

            rootVisualElement.style.borderLeftWidth = k_BorderWidth;
            rootVisualElement.style.borderTopWidth = k_BorderWidth;
            rootVisualElement.style.borderRightWidth = k_BorderWidth;
            rootVisualElement.style.borderBottomWidth = k_BorderWidth;
            rootVisualElement.style.borderLeftColor = borderColor;
            rootVisualElement.style.borderTopColor = borderColor;
            rootVisualElement.style.borderRightColor = borderColor;
            rootVisualElement.style.borderBottomColor = borderColor;

            rootVisualElement.Add(m_FlowDirection = new Toggle(L10n.Tr("Flow Direction")));
            m_FlowDirection.style.flexDirection = FlexDirection.RowReverse;

            rootVisualElement.Add(m_AllTangents = new Toggle(L10n.Tr("All Tangents")));
            m_AllTangents.style.flexDirection = FlexDirection.RowReverse;

            rootVisualElement.Add(m_KnotIndices = new Toggle(L10n.Tr("Knot Indices")));
            m_KnotIndices.style.flexDirection = FlexDirection.RowReverse;

            rootVisualElement.Add(m_SplineMesh = new Toggle(L10n.Tr("Show Mesh")));
            m_SplineMesh.style.flexDirection = FlexDirection.RowReverse;


            m_FlowDirection.RegisterValueChangedCallback((evt) =>
            {
                SplineHandleSettings.FlowDirectionEnabled = evt.newValue;
                SceneView.RepaintAll();
            });
            m_AllTangents.RegisterValueChangedCallback((evt) =>
            {
                SplineHandleSettings.ShowAllTangents = evt.newValue;
                SceneView.RepaintAll();
            });
            m_KnotIndices.RegisterValueChangedCallback((evt) =>
            {
                SplineHandleSettings.ShowKnotIndices = evt.newValue;
                SceneView.RepaintAll();
            });
            m_SplineMesh.RegisterValueChangedCallback((evt) =>
            {
                SplineHandleSettings.ShowMesh = evt.newValue;
                SceneView.RepaintAll();
            });

            UpdateValues();
        }


        void UpdateValues()
        {
            m_FlowDirection.SetValueWithoutNotify(SplineHandleSettings.FlowDirectionEnabled);
            m_AllTangents.SetValueWithoutNotify(SplineHandleSettings.ShowAllTangents);
            m_KnotIndices.SetValueWithoutNotify(SplineHandleSettings.ShowKnotIndices);
            m_SplineMesh.SetValueWithoutNotify(SplineHandleSettings.ShowMesh);
            SceneView.RepaintAll();
        }
    }
}