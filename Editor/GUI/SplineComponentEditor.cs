using System;
using UnityEngine;
using UnityEditor;

class SplineComponentEditor : Editor
{
    static GUIStyle s_HorizontalLine;
    static GUIStyle s_FoldoutStyle;
    
    protected virtual void OnEnable()
    {
        if (s_HorizontalLine == null)
        {
            s_HorizontalLine = new GUIStyle();
            s_HorizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            s_HorizontalLine.margin = new RectOffset(0, 0, 3, 3);
            s_HorizontalLine.fixedHeight = 1;
        }
    }
    
    protected void HorizontalLine(Color color)
    {
        var c = GUI.color;
        GUI.color = color;
        GUILayout.Box( GUIContent.none, s_HorizontalLine );
        GUI.color = c;
    }
    
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
