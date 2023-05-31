using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// Create a property drawer for <see cref="SplineInfo"/> types.
    /// </summary>
    [CustomPropertyDrawer(typeof(SplineInfo))]
    public class SplineInfoPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Returns the height of a SerializedProperty in pixels.
        /// </summary>
        /// <param name="property">The SerializedProperty to calculate height for.</param>
        /// <param name="label">The label of the SerializedProperty.</param>
        /// <returns>Returns the height of a SerializedProperty in pixels.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorStyles.popup.CalcSize(label).y * 2;
        }

        /// <summary>
        /// Creates an interface for a SerializedProperty with an integer property type.
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI.</param>
        /// <param name="property">The SerializedProperty to make the custom GUI for.</param>
        /// <param name="label">The label of this property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var obj = property.FindPropertyRelative("m_Object");
            var con = property.FindPropertyRelative("m_Container");
            var ind = property.FindPropertyRelative("m_SplineIndex");

            if (con.managedReferenceValue != null && obj.objectReferenceValue == null)
                EditorGUI.LabelField(SplineGUIUtility.ReserveSpaceForLine(ref position), "ISplineContainer",
                    property.managedReferenceFieldTypename);
            else
            {
                EditorGUI.ObjectField(SplineGUIUtility.ReserveSpaceForLine(ref position), obj, typeof(ISplineContainer),
                    new GUIContent("Spline Container"));
            }

            EditorGUI.PropertyField(SplineGUIUtility.ReserveSpaceForLine(ref position), ind);
        }
    }
}
