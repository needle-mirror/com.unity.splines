using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomPropertyDrawer(typeof(SplineData<>))]
    class SplineDataPropertyDrawer : PropertyDrawer
    {
        static readonly string k_MultiSplineEditMessage = L10n.Tr("Multi-selection is not supported for SplineData");
        static readonly string k_DataUnitTooltip = L10n.Tr("The unit Data Points are using to be associated to the spline. 'Spline Distance' is " +
            "using the distance in Unity Units from the spline origin, 'Normalized Distance' is using a normalized value of the spline " +
            "length between [0,1] and Knot Index is using Spline Knot indices ");

        readonly static GUIContent[] k_PathUnitIndexLabels = new[]
        {
            new GUIContent(L10n.Tr("Spline Distance")),
            new GUIContent(L10n.Tr("Normalized Distance")),
            new GUIContent(L10n.Tr("Knot Index"))
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded || property.serializedObject.isEditingMultipleObjects)
                return height;

            //Adding space for the object field
            height += EditorGUIUtility.standardVerticalSpacing ;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_DefaultValue"))  + EditorGUIUtility.standardVerticalSpacing ;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_IndexUnit")) + EditorGUIUtility.standardVerticalSpacing;

            var datapointsProperty = property.FindPropertyRelative("m_DataPoints");
            height += EditorGUIUtility.singleLineHeight;
            if (datapointsProperty.isExpanded)
            {
                height += 2 * EditorGUIUtility.singleLineHeight;
                var arraySize = datapointsProperty.arraySize;
                if (arraySize == 0)
                {
                    height += EditorGUIUtility.singleLineHeight;
                }
                else
                {
                    for (int dataPointIndex = 0; dataPointIndex < arraySize; dataPointIndex++)
                    {
                        height += datapointsProperty.GetArrayElementAtIndex(dataPointIndex).isExpanded
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
            if (property.serializedObject.isEditingMultipleObjects)
            {
                EditorGUI.LabelField(position, L10n.Tr(k_MultiSplineEditMessage), EditorStyles.helpBox);
                return;
            }

            property.isExpanded = EditorGUI.Foldout(SplineGUIUtility.ReserveSpace(EditorGUIUtility.singleLineHeight, ref position), property.isExpanded, label);
            ++EditorGUI.indentLevel;
            if (property.isExpanded)
            {
                SplineGUIUtility.ReserveSpace(EditorGUIUtility.standardVerticalSpacing, ref position);

                var valueProperty = property.FindPropertyRelative("m_DefaultValue");
                EditorGUI.PropertyField(SplineGUIUtility.ReserveSpace(EditorGUI.GetPropertyHeight(valueProperty), ref position), valueProperty);
                SplineGUIUtility.ReserveSpace(EditorGUIUtility.standardVerticalSpacing, ref position);

                var indexProperty = property.FindPropertyRelative("m_IndexUnit");
                var dataPointsProperty = property.FindPropertyRelative("m_DataPoints");
                var pathUnit = (PathIndexUnit)indexProperty.intValue;
                EditorGUI.BeginChangeCheck();
                var newPathUnit = EditorGUI.Popup(SplineGUIUtility.ReserveSpace(EditorGUI.GetPropertyHeight(indexProperty), ref position),
                    new GUIContent("Data Index Unit", L10n.Tr(k_DataUnitTooltip)), (int)pathUnit, k_PathUnitIndexLabels);
                if (EditorGUI.EndChangeCheck())
                {
                    if (dataPointsProperty.arraySize == 0)
                        indexProperty.intValue = newPathUnit;
                    else
                    {
                        SplineDataConversionWindow.DoConfirmWindow(
                            position,
                            property,
                            property.serializedObject.targetObject as Component,
                            newPathUnit,
                            false);
                        GUIUtility.ExitGUI();
                    }
                }

                SplineGUIUtility.ReserveSpace(EditorGUIUtility.standardVerticalSpacing, ref position);

                dataPointsProperty.isExpanded = EditorGUI.Foldout(SplineGUIUtility.ReserveSpace(EditorGUIUtility.singleLineHeight, ref position), dataPointsProperty.isExpanded, new GUIContent("Data Points"));
                if (dataPointsProperty.isExpanded)
                {
                    SplineDataReorderableListUtility
                        .GetDataPointsReorderableList(property, dataPointsProperty, pathUnit).DoList(position);
                }
            }
            --EditorGUI.indentLevel;

            EditorGUI.EndProperty();
        }
    }
}
