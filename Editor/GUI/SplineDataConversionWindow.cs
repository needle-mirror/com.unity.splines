using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    class SplineDataConversionWindow : EditorWindow
    {
        static readonly string k_SplineDataConversionMessage = L10n.Tr("Select a reference spline to convert your " +
                                                                       "SplineData with no data loss. Otherwise data " +
                                                                       "won't be converted.");

        static readonly Regex k_EmbeddedSplineDataContainerIndex =
            new Regex(@"((?<=m_Splines\.Array\.data\[)([0-9]+))(?=\])", RegexOptions.Compiled);

        SerializedProperty m_SplineDataProperty;
        SplineContainer m_TargetContainer;
        int m_TargetSpline;
        PathIndexUnit m_NewIndexUnit;
        string[] m_SplineSelectionContent;
        int[] m_SplineSelectionValues;

        public static void DoConfirmWindow(Rect rect,
            SerializedProperty splineDataProperty,
            Component targetComponent,
            int newValue,
            bool forceShowDialog)
        {
            var window = Resources.FindObjectsOfTypeAll<SplineDataConversionWindow>().FirstOrDefault();
            if (window == null)
                window = CreateInstance<SplineDataConversionWindow>();
            window.titleContent = new GUIContent("Convert Spline Data Index Unit");
            window.m_SplineDataProperty = splineDataProperty;
            var(container, index) = FindPlausibleSplineContainer(targetComponent, splineDataProperty);
            window.SetTargetSpline(container, index);
            window.m_NewIndexUnit = (PathIndexUnit) newValue;
            window.minSize = new Vector2(400, 124);
            window.maxSize = new Vector2(400, 124);

            if (forceShowDialog || container == null || index < 0)
            {
                window.ShowUtility();
            }
            else
            {
                window.ApplyConversion();
                DestroyImmediate(window);
            }
        }

        void SetTargetSpline(SplineContainer container, int index)
        {
            m_TargetContainer = container;
            var splines = container == null ? Array.Empty<Spline>() : container.Splines;
            m_TargetSpline = math.min(math.max(0, index), splines.Count-1);
            m_SplineSelectionContent = new string[splines.Count];
            m_SplineSelectionValues = new int[splines.Count];
            for (int i = 0, c = splines.Count; i < c; ++i)
            {
                m_SplineSelectionContent[i] = $"Spline {i}";
                m_SplineSelectionValues[i] = i;
            }
        }

        void OnGUI()
        {
            GUILayout.Label(L10n.Tr("Spline Data Conversion"), EditorStyles.boldLabel);
            if(m_TargetContainer == null)
                EditorGUILayout.HelpBox(k_SplineDataConversionMessage,MessageType.Warning);
            else
                EditorGUILayout.HelpBox(L10n.Tr($"The spline {m_TargetContainer} will be used for data conversion."), MessageType.Info);

            var container = m_TargetContainer;
            container = (SplineContainer)EditorGUILayout.ObjectField("Reference Spline",
                container,
                typeof(SplineContainer),
                true);
            if(container != m_TargetContainer)
                SetTargetSpline(container, m_TargetSpline);

            m_TargetSpline = SplineGUILayout.SplineIndexPopup("Spline", m_TargetSpline, m_TargetContainer);

            EditorGUILayout.BeginHorizontal();

            if(GUILayout.Button(new GUIContent(L10n.Tr("Convert"), L10n.Tr("Convert data indexes to the new Unit."))))
            {
                if(m_TargetContainer != null)
                    ApplyConversion();
                Close();
            }

            if(GUILayout.Button(L10n.Tr("Cancel")))
                Close();

            EditorGUILayout.EndHorizontal();
        }

        static (SplineContainer, int) FindPlausibleSplineContainer(Component targetComponent, SerializedProperty prop)
        {
            SplineContainer container = null;
            var fieldInfos = targetComponent.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            var providerFieldInfo = fieldInfos.FirstOrDefault(field => field.FieldType == typeof(SplineContainer));
            if(providerFieldInfo != null && providerFieldInfo.FieldType == typeof(SplineContainer))
                container = (SplineContainer)providerFieldInfo.GetValue(targetComponent);

            if(container == null)
                container = targetComponent.gameObject.GetComponent<SplineContainer>();

            int index = 0;

            // if this is embedded SplineData, try to extract the spline index (if more than one spline)
            if (container != null && container.Splines.Count > 1)
            {
                // m_Splines.Array.data[0].m_ObjectData.m_Data.Array.data[0].Value.m_DataPoints
                var match = k_EmbeddedSplineDataContainerIndex.Match(prop.propertyPath);
                if (match.Success && int.TryParse(match.Value, out var i))
                    index = i;
            }

            return (container, index);
        }

        void ApplyConversion()
        {
            m_SplineDataProperty.serializedObject.Update();
            ConvertPathUnit(m_SplineDataProperty, m_TargetContainer, m_TargetSpline, m_NewIndexUnit);
            m_SplineDataProperty.serializedObject.ApplyModifiedProperties();
        }

        static void ConvertPathUnit(SerializedProperty splineData,
            ISplineContainer container,
            int splineIndex,
            PathIndexUnit newIndexUnit)
        {
            var spline = container.Splines[splineIndex];
            var transform = container is Component component
                ? component.transform.localToWorldMatrix
                : Matrix4x4.identity;

            using var native = new NativeSpline(spline, transform);
            var array = splineData.FindPropertyRelative("m_DataPoints");
            var pathUnit = splineData.FindPropertyRelative("m_IndexUnit");
            var from = (PathIndexUnit) Enum.GetValues(typeof(PathIndexUnit)).GetValue(pathUnit.enumValueIndex);

            for (int i = 0, c = array.arraySize; i < c; ++i)
            {
                var point = array.GetArrayElementAtIndex(i);
                var index = point.FindPropertyRelative("m_Index");
                index.floatValue = native.ConvertIndexUnit(index.floatValue, from, newIndexUnit);
            }

            pathUnit.enumValueIndex = (int) newIndexUnit;
        }
    }
}
