using System;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// Contains IMGUI controls for editing Spline data.
    /// </summary>
    public static class SplineGUI
    {
        static GUIContent s_TempContent = new GUIContent();
        static string[] s_SplineIndexPopupContents = new string[1] { "Spline 0" };

        internal static GUIContent TempContent(string label, string tooltip = null, Texture2D image = null)
        {
            s_TempContent.text = label;
            s_TempContent.tooltip = tooltip;
            s_TempContent.image = image;
            return s_TempContent;
        }

        /// <summary>
        /// Creates a dropdown to select an index between 0 and the count of <see cref="Splines"/> contained in the
        /// provided <param name="container"></param>.
        /// </summary>
        /// <param name="container">A <see cref="SplineContainer"/> that determines how many splines are available in
        /// the popup selector.</param>
        /// <param name="label">The label to use for this property. If null, the property display name is used.</param>
        /// <param name="rect">The rectangle on the screen to use for the field.</param>
        /// <param name="property">A SerializedProperty that stores an integer value.</param>
        /// <exception cref="ArgumentException">An exception is thrown if <param name="property"> is not an integer
        /// field.</param></exception>
        /// <typeparam name="T">The type implementing <see cref="ISplineContainer"/>.</typeparam>
        public static void SplineIndexField<T>(Rect rect, SerializedProperty property, GUIContent label, T container) where T : ISplineContainer
        {
             SplineIndexField(rect, property, label, container == null ? 0 : container.Splines.Count);
        }

        /// <summary>
        /// Creates a dropdown to select an index between 0 and <param name="splineCount"></param>.
        /// </summary>
        /// <param name="rect">The rectangle on the screen to use for the field.</param>
        /// <param name="property">A SerializedProperty that stores an integer value.</param>
        /// <param name="label">The label to use for this property. If null, the property display name is used.</param>
        /// <param name="splineCount">The number of splines available. In most cases, this is the size of
        /// <see cref="SplineContainer.Splines"/></param>
        /// <exception cref="ArgumentException">An exception is thrown if <param name="property"> is not an integer
        /// field.</param></exception>
        public static void SplineIndexField(Rect rect, SerializedProperty property, GUIContent label, int splineCount)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
                throw new ArgumentException("Spline index property must be of type `int`.", nameof(property));
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            property.intValue = SplineIndexPopup(rect, label == null ? property.displayName : label.text, property.intValue, splineCount);
            EditorGUI.showMixedValue = false;
        }

        /// <summary>
        /// Creates a dropdown to select an index between 0 and <param name="splineCount"></param>.
        /// </summary>
        /// <param name="label">An optional prefix label.</param>
        /// <param name="splineCount">The number of splines available. In most cases, this is the size of
        /// <see cref="SplineContainer.Splines"/></param>
        /// <param name="rect">The rectangle on the screen to use for the field.</param>
        /// <param name="index">The current index.</param>
        /// <returns>The selected index.</returns>
        public static int SplineIndexPopup(Rect rect, string label, int index, int splineCount)
        {
            if (splineCount != s_SplineIndexPopupContents.Length)
            {
                Array.Resize(ref s_SplineIndexPopupContents, splineCount);
                for (int i = 0; i < splineCount; ++i)
                    s_SplineIndexPopupContents[i] = $"Spline {i}";
            }

            return Math.Min(
                Math.Max(0, EditorGUI.IntPopup(rect, label, index, s_SplineIndexPopupContents, null)),
                splineCount-1);
        }

    }

    /// <summary>
    /// Provides IMGUI controls to edit Spline data.
    /// </summary>
    public static class SplineGUILayout
    {
        /// <summary>
        /// Creates a dropdown to select an index between 0 and the count of <see cref="Splines"/> contained in the
        /// provided <param name="container"></param>.
        /// </summary>
        /// <param name="container">A <see cref="SplineContainer"/> that determines how many splines are available in
        /// the popup selector.</param>
        /// <param name="property">A SerializedProperty that stores an integer value.</param>
        /// <exception cref="ArgumentException">An exception is thrown if <param name="property"> is not an integer
        /// field.</param></exception>
        /// <typeparam name="T">The type implementing <see cref="ISplineContainer"/>.</typeparam>
        public static void SplineIndexField<T>(SerializedProperty property, T container) where T : ISplineContainer
        {
             SplineIndexField(property, container == null ? 0 : container.Splines.Count);
        }

        /// <summary>
        /// Creates a dropdown to select an index between 0 and <param name="splineCount"></param>.
        /// </summary>
        /// <param name="property">A SerializedProperty that stores an integer value.</param>
        /// <param name="splineCount">The number of splines available. In most cases, this is the size of
        /// <see cref="SplineContainer.Splines"/></param>
        /// <exception cref="ArgumentException">An exception is thrown if <param name="property"> is not an integer
        /// field.</param></exception>
        public static void SplineIndexField(SerializedProperty property, int splineCount)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
                throw new ArgumentException("Spline index property must be of type `int`.", nameof(property));
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            property.intValue = SplineIndexPopup(property.displayName, property.intValue, splineCount);
            EditorGUI.showMixedValue = false;
        }

        /// <summary>
        /// Creates a dropdown to select a spline index relative to <param name="container"></param>.
        /// </summary>
        /// <param name="label">An optional prefix label.</param>
        /// <param name="index">The current index.</param>
        /// <param name="container">A <see cref="SplineContainer"/> that determines how many splines are available in
        /// the popup selector.</param>
        /// <typeparam name="T">The type of <see cref="ISplineContainer"/>.</typeparam>
        /// <returns>The selected index.</returns>
        public static int SplineIndexPopup<T>(string label, int index, T container) where T : ISplineContainer
        {
            return SplineIndexPopup(label, index, container == null ? 0 : container.Splines.Count);
        }

        /// <summary>
        /// Creates a dropdown to select an index between 0 and <param name="splineCount"></param>.
        /// </summary>
        /// <param name="label">An optional prefix label.</param>
        /// <param name="splineCount">The number of splines available. In most cases, this is the size of
        /// <see cref="SplineContainer.Splines"/></param>
        /// <param name="index">The current index.</param>
        /// <returns>The selected index.</returns>
        public static int SplineIndexPopup(string label, int index, int splineCount)
        {
            var rect = GUILayoutUtility.GetRect(SplineGUI.TempContent(label), EditorStyles.popup);
            return SplineGUI.SplineIndexPopup(rect, label, index, splineCount);
        }

        /// <summary>
        /// Creates a field for an embedded <see cref="SplineData{T}"/> property. Embedded <see cref="SplineData{T}"/>
        /// is stored in the <see cref="Spline"/> class and can be accessed through a string key value. Use this
        /// function to expose an embedded <see cref="SplineData{T}"/> through the Inspector.
        /// </summary>
        /// <param name="container">The <see cref="SplineContainer"/> that holds the <see cref="Spline"/> target.</param>
        /// <param name="index">The index of the target <see cref="Spline"/> in the <see cref="SplineContainer.Splines"/>
        /// array.</param>
        /// <param name="type">The <see cref="EmbeddedSplineDataType"/> type of data stored in the embedded
        /// <see cref="SplineData{T}"/></param>
        /// <param name="key">A string value used to identify and access embedded <see cref="SplineData{T}"/>.</param>
        /// <returns>True if the property has children, is expanded, and includeChildren was set to false. Returns false otherwise.</returns>
        public static bool EmbeddedSplineDataField(SplineContainer container, int index, EmbeddedSplineDataType type, string key)
        {
            return EmbeddedSplineDataField(null, container, index, type, key);
        }

        /// <summary>
        /// Creates a field for an embedded <see cref="SplineData{T}"/> property. Embedded <see cref="SplineData{T}"/>
        /// is stored in the <see cref="Spline"/> class and can be accessed through a string key value. Use this
        /// function to expose an embedded <see cref="SplineData{T}"/> through the Inspector.
        /// </summary>
        /// <param name="label">An optional prefix label.</param>
        /// <param name="container">The <see cref="SplineContainer"/> that holds the <see cref="Spline"/> target.</param>
        /// <param name="index">The index of the target <see cref="Spline"/> in the <see cref="SplineContainer.Splines"/>
        /// array.</param>
        /// <param name="type">The <see cref="EmbeddedSplineDataType"/> type of data stored in the embedded
        /// <see cref="SplineData{T}"/></param>
        /// <param name="key">A string value used to identify and access embedded <see cref="SplineData{T}"/>.</param>
        /// <returns>True if the property has children, is expanded, and includeChildren was set to false. Returns false otherwise.</returns>
        public static bool EmbeddedSplineDataField(GUIContent label,
            SplineContainer container,
            int index,
            EmbeddedSplineDataType type,
            string key)
        {
            if (container == null || index < 0 || index >= container.Splines.Count)
                return false;

            var property = SerializedPropertyUtility.GetEmbeddedSplineDataProperty(container, index, type, key);

            property.serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            var ret = EditorGUILayout.PropertyField(property, label);
            if(EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();
            return ret;
        }

        internal static void QuaternionField(Rect rect, GUIContent content, SerializedProperty property)
        {
            EditorGUI.BeginChangeCheck();
            Quaternion value = SplineGUIUtility.GetQuaternionValue(property);
            var result = EditorGUI.Vector3Field(rect, content, value.eulerAngles);
            if (EditorGUI.EndChangeCheck())
                SplineGUIUtility.SetQuaternionValue(property, Quaternion.Euler(result));
        }
    }
}
