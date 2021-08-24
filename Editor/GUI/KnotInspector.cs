using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Splines
{
    sealed class KnotInspector
    {
        static class Content
        {
            public static readonly GUIContent title = EditorGUIUtility.TrTextContent("Knot Inspector");
            public static readonly GUIContent noKnotSelected = EditorGUIUtility.TrTextContent("No Knot Selected");
            public static readonly GUIContent position = EditorGUIUtility.TrTextContent("Position");
        }

        public void OnGUI(IReadOnlyList<EditableKnot> targets)
        {
            var prevWideMode = EditorGUIUtility.wideMode;
            var prevLabelWidth = EditorGUIUtility.labelWidth;
            if (Screen.width > 320)
            {
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.labelWidth = Screen.width - 230;
            }

            GUILayout.Label(Content.title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                if (targets.Count > 0)
                {
                    DoPositionGUI(targets);
                }
                else
                {
                    GUILayout.Label(Content.noKnotSelected, EditorStyles.centeredGreyMiniLabel);
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUIUtility.wideMode = prevWideMode;
            EditorGUIUtility.labelWidth = prevLabelWidth;
        }

        void DoPositionGUI(IReadOnlyList<EditableKnot> targets)
        {
            PathGUIControls.pointsBuffer.Clear();
            for (int i = 0; i < targets.Count; ++i)
            {
                PathGUIControls.pointsBuffer.Add(targets[i].localPosition);
            }

            EditorGUI.BeginChangeCheck();
            PathGUIControls.MultiEditVector3Field(Content.position, PathGUIControls.pointsBuffer);
            if (EditorGUI.EndChangeCheck())
            {
                for (var i = 0; i < targets.Count; ++i)
                {
                    targets[i].localPosition = PathGUIControls.pointsBuffer[i];
                }
            }
        }
    }
}
