using System;
using System.Linq;
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

        SerializedProperty m_MethodProperty;
        SerializedProperty m_ObjectForwardProperty;
        SerializedProperty m_ObjectUpProperty;
        SerializedProperty m_StartOffsetProperty;
        SerializedObject m_TransformSO;

        SplineAnimate m_SplineAnimate;

        const string k_UxmlPath = "Packages/com.unity.splines/Editor/Resources/UI/UXML/splineanimate-inspector.uxml";
        static VisualTreeAsset s_TreeAsset;
        static StyleSheet s_ThemeStyleSheet;

        SplineAnimate[] m_Components;

        void OnEnable()
        {
            m_SplineAnimate = target as SplineAnimate;
            if (m_SplineAnimate == null)
                return;

            m_SplineAnimate.Updated += OnSplineAnimateUpdated;

            try {
                m_MethodProperty = serializedObject.FindProperty("m_Method");
                m_ObjectForwardProperty = serializedObject.FindProperty("m_ObjectForwardAxis");
                m_ObjectUpProperty = serializedObject.FindProperty("m_ObjectUpAxis");
                m_StartOffsetProperty = serializedObject.FindProperty("m_StartOffset");
            }
            catch (Exception)
            {
                return;
            }

            m_TransformSO = new SerializedObject(m_SplineAnimate.transform);
            m_Components = targets.Select(x => x as SplineAnimate).Where(y => y != null).ToArray();

            foreach (var animate in m_Components)
            {
                if (animate.Container != null)
                    animate.RecalculateAnimationParameters();
            }

            EditorApplication.update += OnEditorUpdate;
            Spline.Changed += OnSplineChange;
            SplineContainer.SplineAdded += OnContainerSplineSetModified;
            SplineContainer.SplineRemoved += OnContainerSplineSetModified;
        }

        void OnDisable()
        {
            if(m_SplineAnimate != null)
                m_SplineAnimate.Updated -= OnSplineAnimateUpdated;

            if (!EditorApplication.isPlaying)
            {
                foreach (var animate in m_Components)
                {
                    if (animate.Container != null)
                    {
                        animate.RecalculateAnimationParameters();
                        animate.Restart(false);
                    }
                }
            }

            EditorApplication.update -= OnEditorUpdate;
            Spline.Changed -= OnSplineChange;
            SplineContainer.SplineAdded -= OnContainerSplineSetModified;
            SplineContainer.SplineRemoved -= OnContainerSplineSetModified;
        }

        void OnEditorUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                if (m_SplineAnimate.Container != null && m_SplineAnimate.IsPlaying)
                {
                    m_SplineAnimate.Update();
                    RefreshProgressFields();
                }
            }
            else
                RefreshProgressFields();
        }

        void OnSplineChange(Spline spline, int knotIndex, SplineModification modificationType)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            foreach (var animate in m_Components)
            {
                if (animate.Container != null && animate.Container.Splines.Contains(spline))
                    animate.RecalculateAnimationParameters();
            }
        }

        void OnContainerSplineSetModified(SplineContainer container, int spline)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            foreach (var animate in m_Components)
            {
                if (animate.Container == container)
                    animate.RecalculateAnimationParameters();
            }
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
            
            var methodField = m_Root.Q<PropertyField>("method");
            methodField.RegisterValueChangeCallback((_) => { RefreshMethodParamFields((SplineAnimate.Method)m_MethodProperty.enumValueIndex); });
            RefreshMethodParamFields((SplineAnimate.Method)m_MethodProperty.enumValueIndex);

            m_ObjectForwardField = m_Root.Q<EnumField>("object-forward");
            m_ObjectForwardField.RegisterValueChangedCallback((evt) => OnObjectAxisFieldChange(evt, m_ObjectForwardProperty, m_ObjectUpProperty));

            m_ObjectUpField = m_Root.Q<EnumField>("object-up");
            m_ObjectUpField.RegisterValueChangedCallback((evt) => OnObjectAxisFieldChange(evt, m_ObjectUpProperty, m_ObjectForwardProperty));

            var startOffsetField = m_Root.Q<PropertyField>("start-offset");
            startOffsetField.RegisterValueChangeCallback((_) =>
            {
                m_SplineAnimate.StartOffset = m_StartOffsetProperty.floatValue;
                m_SplineAnimate.Restart(false);
                OnElapsedTimeFieldChange(m_ElapsedTimeField.value);
            });

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

            m_ProgressSlider.SetValueWithoutNotify(m_SplineAnimate.GetLoopInterpolation(false));
            m_ElapsedTimeField.SetValueWithoutNotify(m_SplineAnimate.ElapsedTime);
        }

        void OnProgressSliderChange(float progress)
        {
            m_SplineAnimate.Pause();
            m_SplineAnimate.NormalizedTime = progress;

            RefreshProgressFields();
        }

        void OnElapsedTimeFieldChange(float elapsedTime)
        {
            m_SplineAnimate.Pause();
            m_SplineAnimate.ElapsedTime = elapsedTime;

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
            if (!m_SplineAnimate.IsPlaying)
            {
                m_SplineAnimate.RecalculateAnimationParameters();
                if (m_SplineAnimate.NormalizedTime == 1f)
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
            m_SplineAnimate.RecalculateAnimationParameters();
            m_SplineAnimate.Restart(false);
            RefreshProgressFields();
        }

        void OnSplineAnimateUpdated(Vector3 position, Quaternion rotation)
        {
            if (m_SplineAnimate == null)
                return;

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

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawSplineAnimateGizmos(SplineAnimate splineAnimate, GizmoType gizmoType)
        {
            if (splineAnimate.Container == null)
                return;

            const float k_OffsetGizmoSize = 0.15f;
            splineAnimate.Container.Evaluate(splineAnimate.StartOffsetT, out var offsetPos, out var forward, out var up);

#if UNITY_2022_2_OR_NEWER
            using (new Handles.DrawingScope(Handles.elementColor))
#else
            using (new Handles.DrawingScope(SplineHandleUtility.knotColor))
#endif
                Handles.ConeHandleCap(-1, offsetPos, Quaternion.LookRotation(forward, up), k_OffsetGizmoSize * HandleUtility.GetHandleSize(offsetPos), EventType.Repaint);
        }
    }
}
