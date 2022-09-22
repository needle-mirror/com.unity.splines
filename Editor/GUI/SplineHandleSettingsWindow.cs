using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class SplineHandleSettingsWindow : EditorWindow
    {
        const float k_BorderWidth = 1;

        Toggle m_FlowDirection;
        Toggle m_AllTangents;

        public static void Show(Rect buttonRect)
        {
            var window = CreateInstance<SplineHandleSettingsWindow>();
            window.hideFlags = HideFlags.DontSave;
            window.ShowAsDropDown(GUIUtility.GUIToScreenRect(buttonRect), new Vector2(160, 40));
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
            rootVisualElement.Add(m_AllTangents = new Toggle(L10n.Tr("All Tangents")));

            m_FlowDirection.RegisterValueChangedCallback((evt) => SplineHandleSettings.FlowDirectionEnabled = evt.newValue);
            m_AllTangents.RegisterValueChangedCallback((evt) => SplineHandleSettings.ShowAllTangents = evt.newValue);

            UpdateValues();
            SplineHandleSettings.Changed += UpdateValues;
        }

        void OnDisable()
        {
            SplineHandleSettings.Changed -= UpdateValues;
        }

        void UpdateValues()
        {
            m_FlowDirection.SetValueWithoutNotify(SplineHandleSettings.FlowDirectionEnabled);
            m_AllTangents.SetValueWithoutNotify(SplineHandleSettings.ShowAllTangents);
            SceneView.RepaintAll();
        }
    }
}