using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// Contains specialized utility functions for creating SerializedObject and SerializedProperty objects from
    /// <see cref="SplineContainer"/>, <see cref="Spline"/>, and <see cref="SplineData{T}"/>.
    /// </summary>
    public static class SerializedPropertyUtility
    {
        const string k_ArrayIndicator = ".Array.data[";
        static Dictionary<int, SerializedObject> s_SerializedObjectCache = new();
        static Dictionary<int, SerializedProperty> s_SerializedPropertyCache = new();
        static readonly Regex k_ExtractArrayPath = new Regex("(?!\\[)[0-9]+(?=\\])", RegexOptions.RightToLeft | RegexOptions.Compiled);

        static SerializedPropertyUtility()
        {
            Selection.selectionChanged += ClearCaches;
            Undo.undoRedoPerformed += ClearPropertyCache;
        }

        static void ClearCaches()
        {
            s_SerializedObjectCache.Clear();
            s_SerializedPropertyCache.Clear();
        }

        /// <summary>
        /// Clear cached SerializedProperty objects. This is automatically called on every selection change. Use this
        /// function if you need to insert or remove properties that may have been cached earlier in the frame.
        /// </summary>
        public static void ClearPropertyCache()
        {
            s_SerializedPropertyCache.Clear();
        }

        internal static object GetSerializedPropertyObject(SerializedProperty property)
        {
            var mbObject = property.serializedObject.targetObject;

            // Array fields in SerializedProperty paths have this slightly complicated pathing in the format of
            // arrayFieldName.Array.data[i] which we can simplify to just arrayFieldName[i] for easier parsing.
            var path = property.propertyPath.Replace(k_ArrayIndicator, "[");
            var splitPath = path.Split('.');

            var parentObject = (object)mbObject;
            var pathPartObject = default(object);

            for (int i = 0; i < splitPath.Length; ++i)
            {
                var propertyPathPart = splitPath[i];

                var isArray = IsPropertyIndexer(propertyPathPart, out var fieldName, out var arrayIndex);
                var fieldInfo = parentObject.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                pathPartObject = fieldInfo.GetValue(parentObject);
                if (isArray)
                {
                    if (pathPartObject is IList list)
                        pathPartObject = list[arrayIndex];
                }

                parentObject = pathPartObject;
            }

            return pathPartObject;
        }

        static bool IsPropertyIndexer(string propertyPart, out string fieldName, out int index)
        {
            var regex = new Regex(@"(.+)\[(\d+)\]");
            var match = regex.Match(propertyPart);

            if (match.Success) // Property refers to an array or list
            {
                fieldName = match.Groups[1].Value;
                index = int.Parse(match.Groups[2].Value);
                return true;
            }
            else
            {
                fieldName = propertyPart;
                index = -1;
                return false;
            }
        }

        /// <summary>
        /// Create a SerializedObject for a <see cref="SplineContainer"/>. This value is cached.
        /// </summary>
        /// <param name="container">The <see cref="SplineContainer"/> to create a SerializedObject for.</param>
        /// <returns>A SerializedObject for the requested <see cref="SplineContainer"/>, or null if container is null.
        /// </returns>
        public static SerializedObject GetSerializedObject(SplineContainer container)
        {
            var hash = container.GetInstanceID();
            if (!s_SerializedObjectCache.TryGetValue(hash, out var so))
                s_SerializedObjectCache.Add(hash, so = new SerializedObject(container));
            return so;
        }

        /// <summary>
        /// Create a SerializedProperty for a <see cref="Spline"/> at the requested index in the
        /// <see cref="SplineContainer.Splines"/>.
        /// </summary>
        /// <param name="splineContainer">The <see cref="SplineContainer"/> to reference.</param>
        /// <param name="splineIndex">The index of the Spline in the <see cref="SplineContainer.Splines"/> array.</param>
        /// <returns>A SerializedProperty for the requested <see cref="Spline"/>, or null if not found.</returns>
        public static SerializedProperty GetSplineSerializedProperty(SerializedObject splineContainer, int splineIndex)
        {
            if (splineContainer == null || splineContainer.targetObject == null)
                return null;

            var hash = HashCode.Combine(splineContainer.targetObject.GetInstanceID(), splineIndex);

            if (!s_SerializedPropertyCache.TryGetValue(hash, out var splineProperty))
            {
                var splines = splineContainer?.FindProperty("m_Splines");
                splineProperty = splines == null || splineIndex < 0 || splineIndex >= splines.arraySize
                    ? null
                    : splines.GetArrayElementAtIndex(splineIndex);
                s_SerializedPropertyCache.Add(hash, splineProperty);
            }

            return splineProperty;
        }

        static string GetEmbeddedSplineDataPropertyName(EmbeddedSplineDataType type)
        {
            return type switch
            {
                EmbeddedSplineDataType.Int => "m_IntData",
                EmbeddedSplineDataType.Float => "m_FloatData",
                EmbeddedSplineDataType.Float4 => "m_Float4Data",
                EmbeddedSplineDataType.Object => "m_ObjectData",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        /// <summary>
        /// Create a SerializedProperty for a <see cref="SplineData{T}"/> value embedded in a <see cref="Spline"/>
        /// class. These are keyed collections of <see cref="SplineData{T}"/> that are managed by the <see cref="Spline"/>
        /// instance. See <see cref="Spline.GetOrCreateIntData"/>, <see cref="Spline.GetOrCreateFloatData"/>, etc.
        /// </summary>
        /// <param name="container">The <see cref="SplineContainer"/> that contains the target <see cref="Spline"/>.</param>
        /// <param name="index">The index of the Spline in the <see cref="SplineContainer.Splines"/> array.</param>
        /// <param name="type">The <see cref="EmbeddedSplineDataType"/>.</param>
        /// <param name="key">A string value used to identify and access a <see cref="SplineData{T}"/>.</param>
        /// <returns>A SerializedProperty for the requested <see cref="SplineData{T}"/>, or null if not found.</returns>
        public static SerializedProperty GetEmbeddedSplineDataProperty(
            SplineContainer container,
            int index,
            EmbeddedSplineDataType type,
            string key)
        {
            var splineProperty = GetSplineSerializedProperty(GetSerializedObject(container), index);
            if (splineProperty == null)
                return null;
            return GetEmbeddedSplineDataProperty(splineProperty, type, key);
        }

        /// <summary>
        /// Create a SerializedProperty for a <see cref="SplineData{T}"/> value embedded in a <see cref="Spline"/>
        /// class. These are keyed collections of <see cref="SplineData{T}"/> that are managed by the <see cref="Spline"/>
        /// instance. See <see cref="Spline.GetOrCreateIntData"/>, <see cref="Spline.GetOrCreateFloatData"/>, etc.
        /// </summary>
        /// <param name="splineProperty">The SerializedProperty for the target <see cref="Spline"/>.</param>
        /// <param name="type">The <see cref="EmbeddedSplineDataType"/>.</param>
        /// <param name="key">A string value used to identify and access a <see cref="SplineData{T}"/>.</param>
        /// <returns>A SerializedProperty for the requested <see cref="SplineData{T}"/>, or null if not found.</returns>
        public static SerializedProperty GetEmbeddedSplineDataProperty(SerializedProperty splineProperty,
            EmbeddedSplineDataType type,
            string key)
        {
            var hash = HashCode.Combine(splineProperty.serializedObject.targetObject.GetInstanceID(),
                splineProperty.propertyPath.GetHashCode(),
                type.GetHashCode(),
                key.GetHashCode());

            if (s_SerializedPropertyCache.TryGetValue(hash, out var splineDataProperty) && splineDataProperty?.serializedObject != null)
                return splineDataProperty;

            var dict = splineProperty.FindPropertyRelative(GetEmbeddedSplineDataPropertyName(type));
            var data = dict?.FindPropertyRelative("m_Data");

            for (int i = 0; i < data?.arraySize; ++i)
            {
                var kvp = data.GetArrayElementAtIndex(i);
                var k = kvp.FindPropertyRelative("Key");

                if (k.stringValue == key)
                {
                    s_SerializedPropertyCache[hash] = splineDataProperty = kvp.FindPropertyRelative("Value");
                    return splineDataProperty;
                }
            }

            s_SerializedPropertyCache[hash] = null;
            return null;
        }

        internal static SerializedProperty GetEmbeddedSplineDataProperty(SerializedProperty embeddedSplineDataProperty)
        {
            var container = embeddedSplineDataProperty.FindPropertyRelative("m_Container");
            var index = embeddedSplineDataProperty.FindPropertyRelative("m_SplineIndex");
            var type = embeddedSplineDataProperty.FindPropertyRelative("m_Type");
            var key = embeddedSplineDataProperty.FindPropertyRelative("m_Key");

            if (container == null || !(container.objectReferenceValue is SplineContainer containerBehaviour))
                return null;

            var containerSerializedObject = GetSerializedObject(containerBehaviour);
            var spline = GetSplineSerializedProperty(containerSerializedObject, index.intValue);
            if (spline == null)
                return null;
            return GetEmbeddedSplineDataProperty(spline, (EmbeddedSplineDataType)type.enumValueIndex, key.stringValue);
        }

        internal static bool GetContainerAndIndex(SerializedProperty spline, out ISplineContainer container, out int index)
        {
            container = spline?.serializedObject?.targetObject as ISplineContainer;
            index = 0;
            if (container == null)
                return false;
            if(TryGetSplineIndex(spline, out index))
                return true;
            return container.Splines.Count == 1;
        }

        // Extracts the index of a Spline in the ISplineContainer array, or 0 if not part of an array.
        internal static bool TryGetSplineIndex(SerializedProperty splineProperty, out int index)
        {
            index = 0;
            var match = k_ExtractArrayPath.Match(splineProperty.propertyPath);
            return match.Success && int.TryParse(match.Value, out index);
        }

        internal static bool TryGetSpline(SerializedProperty splineProperty, out Spline spline)
        {
            if (GetContainerAndIndex(splineProperty, out var container, out var index))
            {
                spline = container.Splines[index];
                return true;
            }

            spline = null;
            return false;
        }
    }
}
