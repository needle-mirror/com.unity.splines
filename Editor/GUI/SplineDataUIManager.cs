using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    internal class SplineDataUIManager:ScriptableSingleton<SplineDataUIManager>
    {
        readonly static string k_DataIndexTooltip = L10n.Tr("The index of the Data Point along the spline and the unit used");
        readonly static string k_DataValueTooltip = L10n.Tr("The value of the Data Point.");
        
        static Dictionary<string, ReorderableList> s_ReorderableLists = new Dictionary<string, ReorderableList>();

        void OnEnable()
        {
            Selection.selectionChanged += ClearReorderableLists;
        }

        void OnDisable()
        {
            Selection.selectionChanged -= ClearReorderableLists;
        }

        string GetDisplayName(PathIndexUnit unit)
        {
            switch(unit)
            {
                case PathIndexUnit.Distance:
                    return "Dist";
                case PathIndexUnit.Normalized:
                    return "Path %";
                case PathIndexUnit.Knot:
                default:
                    return "Knot";
            }
        }
        
        void ClearReorderableLists()
        {
            s_ReorderableLists.Clear();
        }
        
        public ReorderableList GetKeyframesReorderableList(SerializedProperty property, SerializedProperty keyframesProperty, FieldInfo fieldInfo, PathIndexUnit unit)
        {
            var key = keyframesProperty.propertyPath + property.serializedObject.targetObject.GetInstanceID();
            if(s_ReorderableLists.TryGetValue(key, out var list))
                return list;

            list = new ReorderableList(keyframesProperty.serializedObject, keyframesProperty, true, false, true, true);
            s_ReorderableLists.Add(key, list);
            
            list.elementHeightCallback = (int index) =>
            {
                return keyframesProperty.GetArrayElementAtIndex(index).isExpanded
                    ? 3 * EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing
                    : EditorGUIUtility.singleLineHeight;
            };

            list.drawElementCallback =
                (Rect position, int index, bool isActive, bool isFocused) =>
            {
                var ppte = keyframesProperty.GetArrayElementAtIndex(index);
            
                EditorGUI.indentLevel++;
                var expended = EditorGUI.Foldout(SplineUIManager.ReserveSpace(EditorGUIUtility.singleLineHeight, ref position), ppte.isExpanded, new GUIContent($"Data Point [{index}]"), true);
                if(expended != ppte.isExpanded)
                {
                    ppte.isExpanded = expended;
                    if(!isActive)
                        list.index = index;
                    list.GrabKeyboardFocus();
                }
            
                if(ppte.isExpanded)
                {
                    EditorGUI.indentLevel++; 
                    SplineUIManager.ReserveSpace(EditorGUIUtility.standardVerticalSpacing, ref position);
                    EditorGUI.BeginChangeCheck();
                    var timeProperty = ppte.FindPropertyRelative("m_Index");
                    EditorGUI.DelayedFloatField(SplineUIManager.ReserveSpace(EditorGUIUtility.singleLineHeight, ref position), timeProperty, new GUIContent($"Data Index ({GetDisplayName(unit)})", L10n.Tr(k_DataIndexTooltip)));
                    if(EditorGUI.EndChangeCheck())
                    {                
                        if(!isActive)
                            return;
                        
                        keyframesProperty.serializedObject.ApplyModifiedProperties();
                        var newTime = ppte.FindPropertyRelative("m_Index").floatValue;
                        
                        var targetObject = fieldInfo.GetValue(keyframesProperty.serializedObject.targetObject);
                        var sortMethod = targetObject.GetType().GetMethod("ForceSort", BindingFlags.Instance | BindingFlags.NonPublic);
                    
                        EditorApplication.delayCall += () =>
                        {
                            sortMethod?.Invoke(targetObject, null);
                            keyframesProperty.serializedObject.Update();
                            for(int i = 0; i < keyframesProperty.arraySize; i++)
                            {
                                var time = keyframesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_Index").floatValue;
                                if(time == newTime)
                                {
                                    list.index = i;
                                    break;
                                }
                            }
                        };
                    }
                        
                    SplineUIManager.ReserveSpace(EditorGUIUtility.standardVerticalSpacing, ref position);
                    EditorGUI.BeginChangeCheck();
                    var valueProperty = ppte.FindPropertyRelative("m_Value");
                    EditorGUI.PropertyField(SplineUIManager.ReserveSpace(EditorGUI.GetPropertyHeight(valueProperty), ref position), valueProperty, new GUIContent("Data Value", L10n.Tr(k_DataValueTooltip)));
                    
                    if(EditorGUI.EndChangeCheck())
                    {
                        if(!isActive)
                            list.index = index;
                    }

                    EditorGUI.indentLevel--;
                }
                
                EditorGUI.indentLevel--;
            };
            
            return list;
        }
    }
}
