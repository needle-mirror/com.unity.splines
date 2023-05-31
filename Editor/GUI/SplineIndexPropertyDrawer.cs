using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// A PropertyDrawer used to show a popup menu with available spline indices relative to a <see cref="ISplineContainer"/>.
    /// Add <see cref="UnityEngine.Splines.SplineIndexAttribute"/> to a serialized integer type to use.
    /// </summary>
    [CustomPropertyDrawer(typeof(SplineIndexAttribute))]
    public class SplineIndexPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Returns the height of a SerializedProperty in pixels.
        /// </summary>
        /// <param name="property">The SerializedProperty to calculate height for.</param>
        /// <param name="label">The label of the SerializedProperty.</param>
        /// <returns>Returns the height of a SerializedProperty in pixels.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorStyles.popup.CalcSize(label).y;
        }

        /// <summary>
        /// Creates an interface for a SerializedProperty with an integer property type.
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI.</param>
        /// <param name="property">The SerializedProperty to make the custom GUI for.</param>
        /// <param name="label">The label of this property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer || attribute is not SplineIndexAttribute attrib)
                return;

            var path = property.propertyPath.Replace(property.name, attrib.SplineContainerProperty);
            var container = property.serializedObject.FindProperty(path);

            if (container == null || !(container.objectReferenceValue is ISplineContainer res))
                EditorGUI.HelpBox(position,
                    $"SplineIndex property attribute does not reference a valid SplineContainer: " +
                    $"\"{attrib.SplineContainerProperty}\"", MessageType.Warning);
            else
                SplineGUI.SplineIndexField(position, property, label, res.Splines.Count);
        }
    }
}
