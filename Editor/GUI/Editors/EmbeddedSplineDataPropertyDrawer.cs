using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// Creates a property drawer for <see cref="EmbeddedSplineData"/> types.
    /// </summary>
    /// <seealso cref="EmbeddedSplineDataFieldsAttribute"/>
    [CustomPropertyDrawer(typeof(EmbeddedSplineData))]
    [CustomPropertyDrawer(typeof(EmbeddedSplineDataFieldsAttribute))]
    public class EmbeddedSplineDataPropertyDrawer : PropertyDrawer
    {
        bool m_AttemptedFindSplineContainer;
        static readonly string k_SplineDataKeyContent = "Key";

        static Rect ReserveLine(ref Rect rect, int lines = 1)
        {
            var ret = SplineGUIUtility.ReserveSpace(EditorGUIUtility.singleLineHeight * lines, ref rect);
            rect.y += EditorGUIUtility.standardVerticalSpacing * lines;
            return ret;
        }

        static int GetSetBitCount(EmbeddedSplineDataField fields)
        {
            int c = 0, e = (int) fields;
            for(int i = 0; i < 4; ++i)
                if ((e & (1 << i)) != 0)
                    ++c;
            return c;
        }

        /// <summary>
        /// Gets the height of a SerializedProperty in pixels.
        /// </summary>
        /// <param name="property">The SerializedProperty to calculate height for.</param>
        /// <param name="label">The label of the SerializedProperty.</param>
        /// <returns>The height of a SerializedProperty in pixels.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var flags = attribute is EmbeddedSplineDataFieldsAttribute attrib
                ? attrib.Fields
                : EmbeddedSplineDataField.All;

            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            // reserve one line for foldout
            float height = EditorGUIUtility.singleLineHeight;
            height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * GetSetBitCount(flags);

            return height;
        }

        /// <summary>
        /// Creates an interface for a SerializedProperty with an <see cref="EmbeddedSplineData"/> type.
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI.</param>
        /// <param name="property">The SerializedProperty to make the custom GUI for.</param>
        /// <param name="label">The label of this property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var flags = attribute is EmbeddedSplineDataFieldsAttribute filter
                ? filter.Fields
                : EmbeddedSplineDataField.All;
            var fields = GetSetBitCount(flags);

            var container = property.FindPropertyRelative("m_Container");
            var index = property.FindPropertyRelative("m_SplineIndex");
            var key = property.FindPropertyRelative("m_Key");
            var type = property.FindPropertyRelative("m_Type");

            if (fields > 1)
            {
                property.isExpanded = EditorGUI.Foldout(ReserveLine(ref position),
                    property.isExpanded,
                    label?.text ?? property.displayName);

                if (!property.isExpanded)
                    return;
            }

            label = fields == 1 ? label : null;

            // don't create key in property editor
            // don't copy key values to empty targets

            EditorGUI.BeginChangeCheck();

            // only attempt to reconcile the SplineContainer value once per lifetime of drawer. otherwise you get some
            // odd behaviour when trying to delete or replace the value in the inspector.
            if (!m_AttemptedFindSplineContainer
                && container.objectReferenceValue == null
                && property.serializedObject.targetObject is Component cmp
                && cmp.TryGetComponent<SplineContainer>(out var spcnt))
            {
                container.objectReferenceValue = spcnt;
                GUI.changed = true;
            }

            m_AttemptedFindSplineContainer = true;

            if((flags & EmbeddedSplineDataField.Container) == EmbeddedSplineDataField.Container)
                EditorGUI.PropertyField(ReserveLine(ref position), container, label);

            if (!(container?.objectReferenceValue is SplineContainer component))
                component = null;

            if ((flags & EmbeddedSplineDataField.SplineIndex) == EmbeddedSplineDataField.SplineIndex)
                SplineGUI.SplineIndexField(ReserveLine(ref position), index, label, component);

            if ((flags & EmbeddedSplineDataField.Key) == EmbeddedSplineDataField.Key)
            {
                string[] keys = component == null || index.intValue < 0 || index.intValue >= component.Splines.Count
                    ? Array.Empty<string>()
                    : component[index.intValue].GetSplineDataKeys((EmbeddedSplineDataType) type.enumValueIndex).ToArray();
                var i = Array.IndexOf(keys, key.stringValue);
                EditorGUI.BeginChangeCheck();
                i = EditorGUI.Popup(ReserveLine(ref position), label?.text ?? k_SplineDataKeyContent, i, keys);
                if (EditorGUI.EndChangeCheck())
                    key.stringValue = keys[i];
            }

            if((flags & EmbeddedSplineDataField.Type) == EmbeddedSplineDataField.Type)
                EditorGUI.PropertyField(ReserveLine(ref position), type);
        }
    }
}
