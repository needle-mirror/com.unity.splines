using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomPropertyDrawer(typeof(SplineType))]
    sealed class SplineTypeDrawer : PropertyDrawer
    {
        static readonly List<IEditableSpline> s_PathsBuffer = new List<IEditableSpline>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            var index = EditorGUI.PropertyField(position, property, label);
            if (EditorGUI.EndChangeCheck())
            {
                EditorApplication.delayCall += () =>
                {
                    //When switching type we do a conversion pass to update the spline data to fit the new type
                    SplineConversionUtility.UpdateEditableSplinesForTargets(property.serializedObject.targetObjects);
                    EditableSplineUtility.GetSelectedSplines(property.serializedObject.targetObjects, s_PathsBuffer);
                    foreach (var path in s_PathsBuffer)
                        path.SetDirty();
                    SplineConversionUtility.ApplyEditableSplinesIfDirty(property.serializedObject.targetObjects);
                    SceneView.RepaintAll();
                };
            }
        }
    }
}
