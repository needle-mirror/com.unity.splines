using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomPropertyDrawer(typeof(BezierKnot))]
    sealed class KnotPropertyDrawer : PropertyDrawer
    {
        static readonly GUIContent k_Position = EditorGUIUtility.TrTextContent("Position");
        static readonly GUIContent k_Rotation = EditorGUIUtility.TrTextContent("Rotation");
        static readonly GUIContent k_TangentIn = EditorGUIUtility.TrTextContent("Tangent In");
        static readonly GUIContent k_TangentOut = EditorGUIUtility.TrTextContent("Tangent Out");

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = SplineGUIUtility.lineHeight * 2; //Position + Rotation
            height += SplineGUIUtility.lineHeight * 2;

            if(!EditorGUIUtility.wideMode)
                height *= 2f;

            height += SplineGUIUtility.lineHeight; //Knot Title added at the end as it'll be always on 1 line

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var pos = property.FindPropertyRelative("Position");
            var rotation = property.FindPropertyRelative("Rotation");

            EditorGUI.PropertyField(SplineGUIUtility.ReserveSpaceForLine(ref position), pos, k_Position);
            SplineGUI.QuaternionField(SplineGUIUtility.ReserveSpaceForLine(ref position), k_Rotation, rotation);
            
            var tangentIn = property.FindPropertyRelative("TangentIn");
            var tangentOut = property.FindPropertyRelative("TangentOut");

            EditorGUI.PropertyField(SplineGUIUtility.ReserveSpaceForLine(ref position), tangentIn, k_TangentIn);
            EditorGUI.PropertyField(SplineGUIUtility.ReserveSpaceForLine(ref position), tangentOut, k_TangentOut);
        }
    }
}