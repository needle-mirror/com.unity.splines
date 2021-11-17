using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines.Editor.GUI
{
    class SplineDataConversionWindow : EditorWindow
    {
        readonly static string k_SplineDataConversionMessage = L10n.Tr("Select a reference spline to convert your SplineData with no data loss. Otherwise data won't be converted.");

        SerializedProperty m_SplineDataProperty;
        SerializedProperty m_SplineDataUnitProperty;
        FieldInfo m_FieldInfo;
        SplineContainer m_TargetSpline;
        int m_NewValue;
        
        public static void DoConfirmWindow(SerializedProperty property, SerializedProperty unitProperty, FieldInfo fieldInfo, Component targetComponent, int newValue)
        {
            // Get existing open window or if none, make a new one:
            var window = (SplineDataConversionWindow)GetWindow(typeof(SplineDataConversionWindow));
            window.m_SplineDataProperty = property;
            window.m_SplineDataUnitProperty = unitProperty;
            window.m_FieldInfo = fieldInfo;
            window.m_TargetSpline = FindPlausibleSplineContainer(targetComponent);
            window.m_NewValue = newValue;
            window.minSize = new Vector2(400, 100);
            window.maxSize = new Vector2(400, 100);
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label(L10n.Tr("Spline Data Conversion"), EditorStyles.boldLabel);
            if(m_TargetSpline == null)
                EditorGUILayout.HelpBox(k_SplineDataConversionMessage,MessageType.Warning);
            else
                EditorGUILayout.HelpBox(L10n.Tr($"The spline {m_TargetSpline} will be used for data conversion."),MessageType.Info);

            m_TargetSpline = (SplineContainer)EditorGUILayout.ObjectField(
                "Reference Spline", 
                (Object)m_TargetSpline, 
                typeof(SplineContainer), 
                true);

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button(new GUIContent(L10n.Tr("Convert"), L10n.Tr("Convert data indexes to the new Unit."))))
            {
                if(m_TargetSpline != null)
                    ApplyConversion();
                else
                    ApplyWithNoConversion();
                
                Close();
            }
            if(GUILayout.Button(new GUIContent(L10n.Tr("Don't Convert"), L10n.Tr("Do not convert data indexes."))))
            {
                ApplyWithNoConversion();
                Close();
            }
            if(GUILayout.Button(L10n.Tr("Cancel")))
                Close();
            EditorGUILayout.EndHorizontal();
        }
        
        static SplineContainer FindPlausibleSplineContainer(Component targetComponent)
        {
            SplineContainer container = null;
            var fieldInfos = targetComponent.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            var providerFieldInfo = fieldInfos.FirstOrDefault(field => field.FieldType == typeof(SplineContainer));
            if(providerFieldInfo != null && providerFieldInfo.FieldType == typeof(SplineContainer))
                container = (SplineContainer)providerFieldInfo.GetValue(targetComponent);

            if(container == null)
                container = targetComponent.gameObject.GetComponent<SplineContainer>();
            
            return container;
        }

        void ApplyWithNoConversion()
        {
            m_SplineDataUnitProperty.intValue = m_NewValue;
            m_SplineDataUnitProperty.serializedObject.ApplyModifiedProperties();
        }
        
        void ApplyConversion()
        {
            var targetObject = m_FieldInfo.GetValue(m_SplineDataProperty.serializedObject.targetObject);
            var convertMethod = targetObject.GetType().GetMethod("ConvertPathUnit", BindingFlags.Instance | BindingFlags.Public)?
                .MakeGenericMethod(typeof(NativeSpline));

            convertMethod?.Invoke(targetObject, new object[]
            {
                new NativeSpline(m_TargetSpline.Spline, m_TargetSpline.transform.localToWorldMatrix),
                (PathIndexUnit)m_NewValue
            });
            m_SplineDataProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}
