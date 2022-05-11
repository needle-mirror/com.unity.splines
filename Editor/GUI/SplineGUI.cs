using Unity.Mathematics;
using UnityEngine;

namespace UnityEditor.Splines
{
    static class SplineGUI
    {
        public static void QuaternionField(Rect rect, GUIContent content, SerializedProperty property)
        {
            EditorGUI.BeginChangeCheck();
            Quaternion value = SplineGUIUtility.GetQuaternionValue(property);
            var result = EditorGUI.Vector3Field(rect, content, value.eulerAngles);
            if (EditorGUI.EndChangeCheck())
                SplineGUIUtility.SetQuaternionValue(property, Quaternion.Euler(result));
        }
    }
}
