using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomPropertyDrawer(typeof(SplineDataDictionary<>))]
    class SplineDataDictionaryPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_Data")) + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property.FindPropertyRelative("m_Data"), label);
        }
    }
}