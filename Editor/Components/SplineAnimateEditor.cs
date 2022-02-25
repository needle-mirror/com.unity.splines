using System;
using UnityEngine.Splines;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    [CustomEditor(typeof(SplineAnimate))]
    [CanEditMultipleObjects]
    class SplineAnimateEditor : UnityEditor.Editor
    {
        VisualElement m_Root;
        Button m_PlayButton;
        Slider m_ProgressSlider;
        FloatField m_ElapsedTimeField;
        EnumField m_ObjectForwardField;
        EnumField m_ObjectUpField;

        SerializedProperty m_TargetProperty;
        SerializedProperty m_MethodProperty;
        SerializedProperty m_DurationProperty;
        SerializedProperty m_SpeedProperty;
        SerializedProperty m_ObjectForwardProperty;
        SerializedProperty m_ObjectUpProperty;
        SerializedObject m_TransformSO;

        SplineAnimate m_SplineAnimate;

        const string k_UxmlPath = "Packages/com.unity.splines/Editor/Resources/UI/UXML/splineanimate-inspector.uxml";
        static VisualTreeAsset s_TreeAsset;
        static StyleSheet s_ThemeStyleSheet;

        void OnEnable()
        {
            m_SplineAnimate = target as SplineAnimate;
            m_SplineAnimate.onUpdated += OnSplineAnimateUpdated;
            m_TargetProperty = serializedObject.FindProperty("m_Target");
            m_MethodProperty = serializedObject.FindProperty("m_Method");
            m_DurationProperty = serializedObject.FindProperty("m_Duration");
            m_SpeedProperty = serializedObject.FindProperty("m_MaxSpeed");
            m_ObjectForwardProperty = serializedObject.FindProperty("m_ObjectForwardAxis");
            m_ObjectUpProperty = serializedObject.FindProperty("m_ObjectUpAxis");
            m_TransformSO = new SerializedObject(m_SplineAnimate.transform);
            
            EditorApplication.update += OnEditorUpdate;
        }

        void OnDisable()
        {
            if (m_SplineAnimate != null && m_SplineAnimate.splineContainer != null)
            {
                if (!EditorApplication.isPlaying)
                    m_SplineAnimate.Restart(false);

                m_SplineAnimate.onUpdated -= OnSplineAnimateUpdated;
            }

            EditorApplication.update -= OnEditorUpdate;
        }

        void OnEditorUpdate()
        {
            if (m_SplineAnimate == null || m_SplineAnimate.splineContainer == null)
                return;
            
            if (m_SplineAnimate.isPlaying && !EditorApplication.isPlaying)
                m_SplineAnimate.Update();

            RefreshProgressFields();
        }

        public override VisualElement CreateInspectorGUI()
        {
            m_Root = new VisualElement();

            if (s_TreeAsset == null)
                s_TreeAsset = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(k_UxmlPath, typeof(VisualTreeAsset));
            s_TreeAsset.CloneTree(m_Root);
            
            if (s_ThemeStyleSheet == null)
                s_ThemeStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"Packages/com.unity.splines/Editor/Stylesheets/SplineAnimateInspector{(EditorGUIUtility.isProSkin ? "Dark" : "Light")}.uss");

            m_Root.styleSheets.Add(s_ThemeStyleSheet);
            
            var splineField = m_Root.Q<PropertyField>("spline-container");
            splineField.RegisterValueChangeCallback((_) => { m_SplineAnimate.splineContainer = m_TargetProperty.objectReferenceValue as SplineContainer; });

            var methodField = m_Root.Q<PropertyField>("method");
            methodField.RegisterValueChangeCallback((_) => { RefreshMethodParamFields((SplineAnimate.Method)m_MethodProperty.enumValueIndex); });
            RefreshMethodParamFields((SplineAnimate.Method)m_MethodProperty.enumValueIndex);
            
            var durationField = m_Root.Q<PropertyField>("duration");
            durationField.RegisterValueChangeCallback((_) => { m_SplineAnimate.duration = m_DurationProperty.floatValue; });
            
            var speedField = m_Root.Q<PropertyField>("max-speed");
            speedField.RegisterValueChangeCallback((_) => { m_SplineAnimate.maxSpeed = m_SpeedProperty.floatValue; });
            
            m_ObjectForwardField = m_Root.Q<EnumField>("object-forward");
            m_ObjectForwardField.RegisterValueChangedCallback((evt) => OnObjectAxisFieldChange(evt, m_ObjectForwardProperty, m_ObjectUpProperty));
            
            m_ObjectUpField = m_Root.Q<EnumField>("object-up");
            m_ObjectUpField.RegisterValueChangedCallback((evt) => OnObjectAxisFieldChange(evt, m_ObjectUpProperty, m_ObjectForwardProperty));

            var playButton = m_Root.Q<Button>("play");
            playButton.clicked += OnPlayClicked;

            var pauseButton = m_Root.Q<Button>("pause");
            pauseButton.clicked += OnPauseClicked;

            var resetButton = m_Root.Q<Button>("reset");
            resetButton.clicked += OnResetClicked;

            m_ProgressSlider = m_Root.Q<Slider>("normalized-progress");
            m_ProgressSlider.RegisterValueChangedCallback((evt) => OnProgressSliderChange(evt.newValue));

            m_ElapsedTimeField = m_Root.Q<FloatField>("elapsed-time");
            m_ElapsedTimeField.RegisterValueChangedCallback((evt) => OnElapsedTimeFieldChange(evt.newValue));

            return m_Root;
        }

        void RefreshMethodParamFields(SplineAnimate.Method method)
        {
            var durationField = m_Root.Q<PropertyField>("duration");
            var maxSpeedField = m_Root.Q<PropertyField>("max-speed");

            if (method == (int) SplineAnimate.Method.Time)
            {
                durationField.style.display = DisplayStyle.Flex;
                maxSpeedField.style.display = DisplayStyle.None;
            }
            else
            {
                durationField.style.display = DisplayStyle.None;
                maxSpeedField.style.display = DisplayStyle.Flex;
            }
        }

        void RefreshProgressFields()
        {
            if (m_ProgressSlider == null || m_ElapsedTimeField == null)
                return;

            m_ProgressSlider.SetValueWithoutNotify(m_SplineAnimate.GetLoopInterpolation());
            m_ElapsedTimeField.SetValueWithoutNotify(m_SplineAnimate.elapsedTime);
        }

        void OnProgressSliderChange(float progress)
        {
            m_SplineAnimate.Pause();
            m_SplineAnimate.normalizedTime = progress;

            RefreshProgressFields();
        }

        void OnElapsedTimeFieldChange(float elapsedTime)
        {
            m_SplineAnimate.Pause();
            m_SplineAnimate.elapsedTime = elapsedTime;

            RefreshProgressFields();
        }

        void OnObjectAxisFieldChange(ChangeEvent<Enum> changeEvent, SerializedProperty axisProp, SerializedProperty otherAxisProp)
        {
            if (changeEvent.newValue == null)
                return;
            
            var newValue = (SplineAnimate.AlignAxis)changeEvent.newValue;
            var previousValue = (SplineAnimate.AlignAxis)changeEvent.previousValue;

            // Swap axes if the picked value matches that of the other axis field
            if (newValue == (SplineAnimate.AlignAxis)otherAxisProp.enumValueIndex)
            {
                otherAxisProp.enumValueIndex = (int)previousValue;
                serializedObject.ApplyModifiedProperties();
            }
            // Prevent the user from configuring object's forward and up as opposite axes
            if (((int) newValue) % 3 == otherAxisProp.enumValueIndex % 3)
            {
                axisProp.enumValueIndex = (int)previousValue;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        void OnPlayClicked()
        {
            if (!m_SplineAnimate.isPlaying)
            {
                if (m_SplineAnimate.normalizedTime == 1f)
                    m_SplineAnimate.Restart(true);
                else
                    m_SplineAnimate.Play();
            }
        }

        void OnPauseClicked()
        {
            m_SplineAnimate.Pause();
        }

        void OnResetClicked()
        {
            m_SplineAnimate.Restart(false);
            RefreshProgressFields();
        }

        void OnSplineAnimateUpdated(Vector3 position, Quaternion rotation)
        {
            if (!EditorApplication.isPlaying)
            {
                m_TransformSO.Update();
                
                var localPosition = position;
                var localRotation = rotation;
                if (m_SplineAnimate.transform.parent != null)
                {
                    localPosition = m_SplineAnimate.transform.parent.worldToLocalMatrix.MultiplyPoint3x4(position);
                    localRotation = Quaternion.Inverse(m_SplineAnimate.transform.parent.rotation) * localRotation;
                }

                m_TransformSO.FindProperty("m_LocalPosition").vector3Value = localPosition;
                m_TransformSO.FindProperty("m_LocalRotation").quaternionValue = localRotation;

                m_TransformSO.ApplyModifiedProperties();
            }
        }
    }
}