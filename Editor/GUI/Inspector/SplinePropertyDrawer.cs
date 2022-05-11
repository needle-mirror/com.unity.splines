using System;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomPropertyDrawer(typeof(Spline))]
    class SplinePropertyDrawer : PropertyDrawer
    {
        int GetSplineIndex(SerializedProperty splineProperty)
        {
            var properties = splineProperty.propertyPath.Split('.');
            if(properties.Length >= 3 && properties[^2].Equals("Array"))
            {
                var dataIndex = properties[^1][^2];
                return dataIndex - '0';
            }

            return -1;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = SplineGUIUtility.lineHeight;
            if (!property.isExpanded)
                return height;

            //Closed property
            height += SplineGUIUtility.lineHeight;

            //Knots properties
            var knotProperty = property.FindPropertyRelative("m_Knots");
            return height + SplineReorderableListUtility.GetKnotReorderableList(property, knotProperty, GetSplineIndex(property)).GetHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var splineIndex = GetSplineIndex(property);
            var splineLabel = splineIndex >= 0 ? "Spline "+splineIndex : "Spline";

            EditorGUI.BeginChangeCheck();
            property.isExpanded = EditorGUI.Foldout(SplineGUIUtility.ReserveSpace(SplineGUIUtility.lineHeight, ref position), property.isExpanded, new GUIContent(splineLabel));
            if(property.isExpanded)
            {
                var closedProperty = property.FindPropertyRelative("m_Closed");
                EditorGUI.PropertyField(SplineGUIUtility.ReserveSpace(SplineGUIUtility.lineHeight, ref position), closedProperty);
                var knotProperty = property.FindPropertyRelative("m_Knots");
                SplineReorderableListUtility.GetKnotReorderableList(property, knotProperty, splineIndex).DoList(position);
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                var splines = (Spline[]) fieldInfo.GetValue(property.serializedObject.targetObject);
                foreach (var spline in splines)
                    spline?.SetDirty();
            }
        }
    }
}