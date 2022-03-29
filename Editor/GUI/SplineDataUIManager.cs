using System;
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

        static void SetSplineDataDirty(FieldInfo fieldInfo, SerializedProperty dataPointProperty)
        {
            var targetObject = fieldInfo.GetValue(dataPointProperty.serializedObject.targetObject);
            var dirtyMethod = targetObject.GetType().GetMethod("SetDirty", BindingFlags.Instance | BindingFlags.NonPublic);
            dirtyMethod?.Invoke(targetObject, null);
        }
        
        public ReorderableList GetDataPointsReorderableList(SerializedProperty property, SerializedProperty dataPointProperty, FieldInfo fieldInfo, PathIndexUnit unit)
        {
            var key = dataPointProperty.propertyPath + property.serializedObject.targetObject.GetInstanceID();
            if(s_ReorderableLists.TryGetValue(key, out var list))
            {
                try
                {
                    SerializedProperty.EqualContents(list.serializedProperty, dataPointProperty);
                    return list;
                }
                catch (NullReferenceException) 
                {
                    s_ReorderableLists.Remove(key);
                }
            }

            list = new ReorderableList(dataPointProperty.serializedObject, dataPointProperty, true, false, true, true);
            s_ReorderableLists.Add(key, list);
            
            list.elementHeightCallback = (int index) =>
            {
                return dataPointProperty.arraySize > 0 && dataPointProperty.GetArrayElementAtIndex(index).isExpanded
                    ? 3 * EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing
                    : EditorGUIUtility.singleLineHeight;
            };

            list.onChangedCallback = reorderableList =>
            {
                SetSplineDataDirty(fieldInfo, dataPointProperty);
            };
            
            list.drawElementCallback =
                (Rect position, int listIndex, bool isActive, bool isFocused) =>
            {
                var ppte = dataPointProperty.GetArrayElementAtIndex(listIndex);
            
                EditorGUI.indentLevel++;
                var expended = EditorGUI.Foldout(SplineUIManager.ReserveSpace(EditorGUIUtility.singleLineHeight, ref position), ppte.isExpanded, new GUIContent($"Data Point [{listIndex}]"), true);
                if(expended != ppte.isExpanded)
                {
                    ppte.isExpanded = expended;
                    if(!isActive)
                        list.index = listIndex;
                    list.GrabKeyboardFocus();
                }
            
                if(ppte.isExpanded)
                {
                    EditorGUI.indentLevel++; 
                    SplineUIManager.ReserveSpace(EditorGUIUtility.standardVerticalSpacing, ref position);
                    EditorGUI.BeginChangeCheck();
                    var indexProperty = ppte.FindPropertyRelative("m_Index");
                    EditorGUI.DelayedFloatField(SplineUIManager.ReserveSpace(EditorGUIUtility.singleLineHeight, ref position), indexProperty, new GUIContent($"Data Index ({GetDisplayName(unit)})", L10n.Tr(k_DataIndexTooltip)));
                    if(EditorGUI.EndChangeCheck())
                    {                
                        if(!isActive)
                            return;
                        
                        dataPointProperty.serializedObject.ApplyModifiedProperties();
                        var newIndex = ppte.FindPropertyRelative("m_Index").floatValue;
                 
                        var targetObject = fieldInfo.GetValue(dataPointProperty.serializedObject.targetObject);       
                        var sortMethod = targetObject.GetType().GetMethod("ForceSort", BindingFlags.Instance | BindingFlags.NonPublic);
                        EditorApplication.delayCall += () =>
                        {
                            sortMethod?.Invoke(targetObject, null);
                            dataPointProperty.serializedObject.Update();
                            for(int i = 0; i < dataPointProperty.arraySize; i++)
                            {
                                var index = dataPointProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_Index").floatValue;
                                if(index == newIndex)
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
                        SetSplineDataDirty(fieldInfo, dataPointProperty);
                        if(!isActive)
                            list.index = listIndex;
                    }

                    EditorGUI.indentLevel--;
                }
                
                EditorGUI.indentLevel--;
            };
            
            return list;
        }
    }
}
