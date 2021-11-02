using System;
using UnityEditor.Splines.Editor.GUI;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomPropertyDrawer(typeof(SplineData<>))]
    public class SplineDataPropertyDrawer : PropertyDrawer
    {
        readonly static string k_MultiSplineEditMessage = L10n.Tr("Multi-selection is not supported for SplineData");
        readonly static string k_DataUnitTooltip = L10n.Tr("The unit Data Points are using to be associated to the spline. 'Distance' is " +
            "using the distance in Unity Units from the spline origin, Path % is using a normalized value of the spline " +
            "length between [0,1] and Knot Index is using Spline Knot Indexes ");

        readonly static GUIContent[] k_PathUnitIndexLabels = new[]
        {
            new GUIContent(L10n.Tr("Path Distance")),
            new GUIContent(L10n.Tr("Path Percentage")),
            new GUIContent(L10n.Tr("Knot Index"))
        };
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if(!property.isExpanded || property.serializedObject.isEditingMultipleObjects)
                return height;
            
            //Adding space for the object field
            height += EditorGUIUtility.singleLineHeight  + EditorGUIUtility.standardVerticalSpacing ;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_IndexUnit"))  + EditorGUIUtility.standardVerticalSpacing ;
            
            var datapointsProperty = property.FindPropertyRelative("m_DataPoints");
            height += EditorGUIUtility.singleLineHeight;
            if(datapointsProperty.isExpanded)
            {
                height += 2 * EditorGUIUtility.singleLineHeight;
                var arraySize = datapointsProperty.arraySize;
                if(arraySize == 0)
                {
                    height += EditorGUIUtility.singleLineHeight;
                }
                else
                {
                    for(int keyframeIndex = 0; keyframeIndex < arraySize; keyframeIndex++)
                    {
                        height += datapointsProperty.GetArrayElementAtIndex(keyframeIndex).isExpanded
                            ? 3 * EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing
                            : EditorGUIUtility.singleLineHeight;
                    }
                }
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            if(property.serializedObject.isEditingMultipleObjects)
            {
                EditorGUI.LabelField(position,L10n.Tr(k_MultiSplineEditMessage), EditorStyles.helpBox);
                return;
            }

            property.isExpanded = EditorGUI.Foldout(SplineUIManager.ReserveSpace(EditorGUIUtility.singleLineHeight, ref position), property.isExpanded, label);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                SplineUIManager.ReserveSpace(EditorGUIUtility.standardVerticalSpacing, ref position);
                
                var indexProperty = property.FindPropertyRelative("m_IndexUnit");
                var keyframesProperty = property.FindPropertyRelative("m_DataPoints");
                var pathUnit = (PathIndexUnit)indexProperty.intValue;
                EditorGUI.BeginChangeCheck();
                var newPathUnit = EditorGUI.Popup(SplineUIManager.ReserveSpace(EditorGUI.GetPropertyHeight(indexProperty), ref position),
                    new GUIContent("Data Index Unit",L10n.Tr(k_DataUnitTooltip)), (int)pathUnit, k_PathUnitIndexLabels);
                if(EditorGUI.EndChangeCheck())
                {
                    if(keyframesProperty.arraySize == 0)
                        indexProperty.intValue = newPathUnit;
                    else
                        SplineDataConversionWindow.DoConfirmWindow(property, indexProperty, fieldInfo, property.serializedObject.targetObject as Component, newPathUnit);
                }

                SplineUIManager.ReserveSpace(EditorGUIUtility.standardVerticalSpacing, ref position);

                keyframesProperty.isExpanded = EditorGUI.Foldout(SplineUIManager.ReserveSpace(EditorGUIUtility.singleLineHeight, ref position), keyframesProperty.isExpanded, new GUIContent("Data Points"));
                if(keyframesProperty.isExpanded)
                {
                    SplineDataUIManager.instance.GetKeyframesReorderableList(property, keyframesProperty, fieldInfo, pathUnit).DoList(position);
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }
    }
}
