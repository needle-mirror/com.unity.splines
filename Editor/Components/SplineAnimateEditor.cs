using System;
using System.Collections.Generic;
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
        List<VisualElement> m_Roots = new ();
        List<Slider> m_ProgressSliders = new ();
        List<FloatField> m_ElapsedTimeFields = new ();
        List<EnumField> m_ObjectForwardFields = new ();
        List<EnumField> m_ObjectUpFields = new ();

        SerializedProperty m_MethodProperty;
        SerializedProperty m_ObjectForwardProperty;
        SerializedProperty m_ObjectUpProperty;
        SerializedProperty m_StartOffsetProperty;
        SerializedObject m_TransformSO;

        SplineAnimate m_SplineAnimate;

        const string k_UxmlPath = "Packages/com.unity.splines/Editor/Editor Resources/UI/UXML/splineanimate-inspector.uxml";
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

            m_Roots.Clear();
            m_ObjectForwardFields.Clear();
            m_ObjectUpFields.Clear();
            m_ProgressSliders.Clear();
            m_ElapsedTimeFields.Clear();

            EditorApplication.update += OnEditorUpdate;
            Spline.Changed += OnSplineChange;
            SplineContainer.SplineAdded += OnContainerSplineSetModified;
            SplineContainer.SplineRemoved += OnContainerSplineSetModified;
        }

        void OnDisable()
        {
            if(m_SplineAnimate != null)
                m_SplineAnimate.Updated -= OnSplineAnimateUpdated;

            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (m_Components != null)
                {
                    foreach (var animate in m_Components)
                    {
                        if (animate != null && animate.Container != null)
                        {
                            animate.RecalculateAnimationParameters();
                            animate.Restart(false);
                        }
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
            else if(m_SplineAnimate.IsPlaying)
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
            var root = new VisualElement();

            if (s_TreeAsset == null)
                s_TreeAsset = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(k_UxmlPath, typeof(VisualTreeAsset));
            s_TreeAsset.CloneTree(root);

            if (s_ThemeStyleSheet == null)
                s_ThemeStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"Packages/com.unity.splines/Editor/Stylesheets/SplineAnimateInspector{(EditorGUIUtility.isProSkin ? "Dark" : "Light")}.uss");

            root.styleSheets.Add(s_ThemeStyleSheet);

            var methodField = root.Q<PropertyField>("method");
            methodField.RegisterValueChangeCallback((_) => { RefreshMethodParamFields((SplineAnimate.Method)m_MethodProperty.enumValueIndex); });
            RefreshMethodParamFields((SplineAnimate.Method)m_MethodProperty.enumValueIndex);

            var objectForwardField = root.Q<EnumField>("object-forward");
            objectForwardField.RegisterValueChangedCallback((evt) => OnObjectAxisFieldChange(evt, m_ObjectForwardProperty, m_ObjectUpProperty));

            var objectUpField = root.Q<EnumField>("object-up");
            objectUpField.RegisterValueChangedCallback((evt) => OnObjectAxisFieldChange(evt, m_ObjectUpProperty, m_ObjectForwardProperty));

            var playButton = root.Q<Button>("play");
            playButton.SetEnabled(!EditorApplication.isPlaying);
            playButton.clicked += OnPlayClicked;

            var pauseButton = root.Q<Button>("pause");
            pauseButton.SetEnabled(!EditorApplication.isPlaying);
            pauseButton.clicked += OnPauseClicked;

            var resetButton = root.Q<Button>("reset");
            resetButton.SetEnabled(!EditorApplication.isPlaying);
            resetButton.clicked += OnResetClicked;

            var progressSlider = root.Q<Slider>("normalized-progress");
            progressSlider.SetEnabled(!EditorApplication.isPlaying);
            progressSlider.RegisterValueChangedCallback((evt) => OnProgressSliderChange(evt.newValue));

            var elapsedTimeField = root.Q<FloatField>("elapsed-time");
            elapsedTimeField.SetEnabled(!EditorApplication.isPlaying);
            elapsedTimeField.RegisterValueChangedCallback((evt) => OnElapsedTimeFieldChange(evt.newValue));

            var startOffsetField = root.Q<PropertyField>("start-offset");
            startOffsetField.RegisterValueChangeCallback((evt) =>
            {
                m_SplineAnimate.StartOffset = m_StartOffsetProperty.floatValue;
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    m_SplineAnimate.Restart(false);
                    OnElapsedTimeFieldChange(elapsedTimeField.value);
                }
            });

            m_Roots.Add(root);
            m_ProgressSliders.Add(progressSlider);
            m_ElapsedTimeFields.Add(elapsedTimeField);

            m_ObjectForwardFields.Add(objectForwardField);
            m_ObjectUpFields.Add(objectUpField);

            return root;
        }

        void RefreshMethodParamFields(SplineAnimate.Method method)
        {
            foreach (var root in m_Roots)
            {

                var durationField = root.Q<PropertyField>("duration");
                var maxSpeedField = root.Q<PropertyField>("max-speed");

                if (method == (int)SplineAnimate.Method.Time)
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
        }

        void RefreshProgressFields()
        {
            for (int i = 0; i < m_ProgressSliders.Count && i < m_ElapsedTimeFields.Count; ++i)
            {
                var progressSlider = m_ProgressSliders[i];
                var elapsedTimeField = m_ElapsedTimeFields[i];
                if (progressSlider == null || elapsedTimeField == null)
                    continue;

                progressSlider.SetValueWithoutNotify(m_SplineAnimate.GetLoopInterpolation(false));
                elapsedTimeField.SetValueWithoutNotify(m_SplineAnimate.ElapsedTime);
            }
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

            foreach (var objectForwardField in m_ObjectForwardFields)
                objectForwardField.SetValueWithoutNotify((SplineComponent.AlignAxis)m_ObjectForwardProperty.enumValueIndex);
            foreach (var objectUpField in m_ObjectUpFields)
                objectUpField.SetValueWithoutNotify((SplineComponent.AlignAxis)m_ObjectUpProperty.enumValueIndex);
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
            if ((!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode) || splineAnimate.Container == null)
                return;

            const float k_OffsetGizmoSize = 0.15f;
            splineAnimate.Container.Evaluate(splineAnimate.StartOffsetT, out var offsetPos, out var forward, out var up);

#if UNITY_2022_2_OR_NEWER
            using (new Handles.DrawingScope(Handles.elementColor))
#else
            using (new Handles.DrawingScope(SplineHandleUtility.knotColor))
#endif
            if (Vector3.Magnitude(forward) <= Mathf.Epsilon)
            {
                if (splineAnimate.StartOffsetT < 1f)
                    forward = splineAnimate.Container.EvaluateTangent(Mathf.Min(1f, splineAnimate.StartOffsetT + 0.01f));
                else
                    forward = splineAnimate.Container.EvaluateTangent(splineAnimate.StartOffsetT - 0.01f);

            }
            Handles.ConeHandleCap(-1, offsetPos, Quaternion.LookRotation(Vector3.Normalize(forward), up), k_OffsetGizmoSize * HandleUtility.GetHandleSize(offsetPos), EventType.Repaint);
        }
    }
}
