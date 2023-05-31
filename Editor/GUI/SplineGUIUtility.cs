using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class SplineGUIUtility
    {
        public delegate bool EqualityComparer<in T>(T a, T b);

        internal static readonly float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        public static bool HasMultipleValues<T>(IReadOnlyList<T> elements, EqualityComparer<T> comparer)
        {
            if (elements.Count < 2)
                return false;

            var first = elements[0];
            for (int i = 1; i < elements.Count; ++i)
                if (!comparer.Invoke(first, elements[i]))
                    return true;

            return false;
        }

        public static quaternion GetQuaternionValue(SerializedProperty property)
        {
            return new quaternion(
                property.FindPropertyRelative("value.x").floatValue,
                property.FindPropertyRelative("value.y").floatValue,
                property.FindPropertyRelative("value.z").floatValue,
                property.FindPropertyRelative("value.w").floatValue);
        }

        public static void SetQuaternionValue(SerializedProperty property, Quaternion value)
        {
            property.FindPropertyRelative("value.x").floatValue = value.x;
            property.FindPropertyRelative("value.y").floatValue = value.y;
            property.FindPropertyRelative("value.z").floatValue = value.z;
            property.FindPropertyRelative("value.w").floatValue = value.w;
        }

        public static SerializedProperty GetParentSplineProperty(SerializedProperty property)
        {
            var properties = property.propertyPath.Split('.');
            if (properties.Length == 0)
                return null;

            var current = property.serializedObject.FindProperty(properties[0]);

            for (var i = 1; i < properties.Length; ++i)
            {
                var p = properties[i];
                if (current.type == typeof(Spline).Name)
                    return current;

                if (current.propertyType == SerializedPropertyType.ManagedReference
                    && current.managedReferenceFullTypename == typeof(Spline).AssemblyQualifiedName)
                    return current;

                current = current.FindPropertyRelative(p);
            }

            return null;
        }

        public static Rect ReserveSpace(float height, ref Rect total)
        {
            Rect current = total;
            current.height = height;
            total.y += height;
            return current;
        }

        public static Rect ReserveSpaceForLine(ref Rect total)
        {
            var height = EditorGUIUtility.wideMode ? lineHeight : 2f * lineHeight;
            return ReserveSpace(height, ref total);
        }
    }
}