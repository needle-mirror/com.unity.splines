using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    // Multi-object selection is not supported
    [CustomEditor(typeof(SplineContainer))]
    class SplineContainerEditor : UnityEditor.Editor
    {
        SerializedProperty m_SplineProperty;
        SerializedProperty splinesProperty => m_SplineProperty ??= serializedObject.FindProperty("m_Splines");

        static GUIStyle s_HelpLabelStyle;
        static GUIStyle HelpLabelStyle
        {
            get
            {            
                if (s_HelpLabelStyle == null)
                {
                    s_HelpLabelStyle = new GUIStyle(EditorStyles.helpBox);
                    s_HelpLabelStyle.padding = new RectOffset(2, 2, 2, 2);
                }

                return s_HelpLabelStyle;
            }
        }
        
        static GUIContent m_HelpLabelContent;
        
        const string k_HelpBoxIconPath = "SplineEditMode-Info";
        static GUIContent m_HelpLabelContentIcon;

        const string k_ComponentMessage = "Use the Spline Edit Mode in the Scene Tools Overlay to edit this Spline.";


        public void OnEnable()
        {
            m_HelpLabelContent = EditorGUIUtility.TrTextContent(k_ComponentMessage);
            m_HelpLabelContentIcon = new GUIContent(PathIcons.GetIcon(k_HelpBoxIconPath));
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }
        
        void UndoRedoPerformed()
        {
            foreach (var t in targets)
            {
                var container = t as SplineContainer;
                if (container != null)
                {
                    foreach (var spline in container.Splines)
                        spline.SetDirty(SplineModification.Default);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // [SPLB-132] Reverting to custom helpbox as the default helpbox style as a trouble to handle custom icons
            // when using a screen with PixelPerPoints different than 1. This is done in trunk by setting the
            // Texture2d.pixelsPerPoints which is an internal property than cannot be access from here.
            EditorGUILayout.BeginHorizontal(HelpLabelStyle);
            EditorGUIUtility.SetIconSize(new Vector2(32f, 32f));
            EditorGUILayout.LabelField(m_HelpLabelContentIcon, 
                GUILayout.Width(34), GUILayout.MinHeight(34), GUILayout.ExpandHeight(true));
            EditorGUIUtility.SetIconSize(Vector2.zero);
            EditorGUILayout.LabelField(m_HelpLabelContent, 
                new GUIStyle(EditorStyles.label){wordWrap = HelpLabelStyle.wordWrap, fontSize = HelpLabelStyle.fontSize, padding = new RectOffset(-2, 0, 0, 0)}, 
                GUILayout.ExpandHeight(true));
            EditorGUILayout.EndHorizontal();
            
            SplineReorderableList.Get(splinesProperty).DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        bool HasFrameBounds()
        {
            foreach (var o in targets)
            {
                var target = (SplineContainer) o;
                foreach (var spline in target.Splines)
                    if (spline.Count > 0)
                        return true;
            }

            return false;
        }

        Bounds OnGetFrameBounds()
        {
            List<SplineInfo> splines = new List<SplineInfo>();
            EditorSplineUtility.GetSplinesFromTargets(targets, splines);
            return EditorSplineUtility.GetBounds(splines);
        }
    }
}
