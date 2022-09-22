using UnityEditor.Toolbars;
using UnityEngine;

namespace UnityEditor.Splines
{
    [EditorToolbarElement("Spline Tool Settings/Handle Visuals")]
    sealed class SplineHandleSettingsDropdown : EditorToolbarDropdown
    {
        public SplineHandleSettingsDropdown()
        {
            var content = EditorGUIUtility.TrTextContent("Visuals", "Visual settings for handles");

            text = content.text;
            tooltip = content.tooltip;
            icon = (Texture2D)content.image;

            clicked += OnClick;
        }

        void OnClick()
        {
            SplineHandleSettingsWindow.Show(worldBound);
        }
    }
}