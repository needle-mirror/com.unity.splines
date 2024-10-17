using System;
using UnityEngine;
using UnityEditor;

class SplineComponentEditor : Editor
{
    static GUIStyle s_FoldoutStyle;

    internal static readonly string k_Helpbox = L10n.Tr("Instantiated Objects need a SplineContainer target to be created.");

    protected bool Foldout(bool foldout, GUIContent content)
    {
        return Foldout(foldout, content, false);
    }

    public static bool Foldout(bool foldout, GUIContent content, bool toggleOnLabelClick)
    {
        if (s_FoldoutStyle == null)
        {
            s_FoldoutStyle = new GUIStyle(EditorStyles.foldout);
            s_FoldoutStyle.fontStyle = FontStyle.Bold;
        }

        return EditorGUILayout.Foldout(foldout, content, toggleOnLabelClick, s_FoldoutStyle);
    }

    internal struct LabelWidthScope : IDisposable
    {
        float previousWidth;

        public LabelWidthScope(float width)
        {
            previousWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = width;
        }

        public void Dispose()
        {
            EditorGUIUtility.labelWidth = previousWidth;
        }
    }
}
