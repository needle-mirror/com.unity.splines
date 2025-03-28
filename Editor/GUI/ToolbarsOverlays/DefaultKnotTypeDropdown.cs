using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.Splines;
#if !UNITY_2022_1_OR_NEWER
#endif

namespace UnityEditor.Splines.Editor.GUI
{
    [EditorToolbarElement("Spline Tool Settings/Default Knot Type")]
    class DefaultKnotTypeDropdown : EditorToolbarDropdown
    {
        const string k_LinearIconPath = "Packages/com.unity.splines/Editor/Editor Resources/Icons/Tangent_Linear.png";
        const string k_AutoSmoothIconPath = "Packages/com.unity.splines/Editor/Editor Resources/Icons/AutoSmoothKnot.png";
        readonly GUIContent[] m_OptionContents = new GUIContent[2];

        public DefaultKnotTypeDropdown()
        {
            name = "Default Knot Type";

            var content = EditorGUIUtility.TrTextContent("Linear",
                "Tangents are not used. A linear knot tries to connect to another by a path with no curvature.",
                k_LinearIconPath);
            m_OptionContents[0] = content;

            content = EditorGUIUtility.TrTextContent("Auto Smooth",
                "Tangents are calculated using the previous and next knot positions.",
                k_AutoSmoothIconPath);
            m_OptionContents[1] = content;

            clicked += OpenContextMenu;

            RefreshElementContent();
        }

        void OpenContextMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(m_OptionContents[0], EditorSplineUtility.DefaultTangentMode == TangentMode.Linear,
                () => SetTangentModeIfNeeded(TangentMode.Linear));

            menu.AddItem(m_OptionContents[1], EditorSplineUtility.DefaultTangentMode == TangentMode.AutoSmooth,
                () => SetTangentModeIfNeeded(TangentMode.AutoSmooth));

            menu.DropDown(worldBound);
        }

        void SetTangentModeIfNeeded(TangentMode tangentMode)
        {
            if (EditorSplineUtility.DefaultTangentMode != tangentMode)
            {
                EditorSplineUtility.s_DefaultTangentMode.SetValue(tangentMode, true);
                RefreshElementContent();
            }
        }

        void RefreshElementContent()
        {
            var content = m_OptionContents[EditorSplineUtility.DefaultTangentMode == TangentMode.Linear ? 0 : 1];
            text = content.text;
            tooltip = content.tooltip;
            icon = content.image as Texture2D;
        }
    }
}
