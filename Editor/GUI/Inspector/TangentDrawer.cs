using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Splines;
using UnityEngine.UIElements;

#if !UNITY_2022_1_OR_NEWER
using UnityEditor.UIElements;
#endif

namespace UnityEditor.Splines
{
    sealed class TangentDrawer : ElementDrawer<SelectableTangent>
    {
        const string k_TangentDrawerStyle = "tangent-drawer";
        const string k_TangentLabelStyle = "tangent-label";
        const string k_TangentFillerStyle = "tangent-filler";
        const string k_TangentMagnitudeFloatFieldStyle = "tangent-magnitude-floatfield";

        static readonly List<float> s_LengthBuffer = new List<float>(0);
        static readonly SplineGUIUtility.EqualityComparer<float> s_MagnitudeComparer = (a, b) => a.Equals(b);

        readonly TangentModeDropdown<SelectableTangent> m_Mode;
        readonly BezierTangentModeDropdown<SelectableTangent> m_BezierMode;

        FloatField m_Magnitude;
        Label m_DirectionLabel;
        Float3PropertyField<SelectableTangent> m_Direction;

        public TangentDrawer()
        {
            AddToClassList(k_TangentDrawerStyle);

            Add(m_Mode = new TangentModeDropdown<SelectableTangent>());
            m_Mode.changed += () =>
            {
                m_BezierMode.Update(targets);
                EnableElements();
            };
            Add(m_BezierMode = new BezierTangentModeDropdown<SelectableTangent>());
            m_BezierMode.changed += () =>
            {
                m_Mode.Update(targets);
                EnableElements();
            };
            
            CreateTangentFields();

            m_Magnitude.RegisterValueChangedCallback((evt) =>
            {
                Undo.RecordObject(target.SplineInfo.Target, SplineInspectorOverlay.SplineChangeUndoMessage);
                UpdateTangentMagnitude(evt.newValue);
                var tangent = target;
                m_Direction.SetValueWithoutNotify(tangent.LocalPosition);
            });

            Add(new Separator());
        }

        public override string GetLabelForTargets()
        {
            if (targets.Count > 1)
                return $"<b>({targets.Count}) Tangents</b> selected";

            var inOutLabel = target.TangentIndex == 0 ? "In" : "Out";
            return $"Tangent <b>{inOutLabel}</b> selected (<b>Knot {target.KnotIndex}</b>)";
        }

        public override void Update()
        {
            base.Update();
            
            m_Mode.Update(targets);
            m_BezierMode.Update(targets);

            UpdateMagnitudeField(targets);
            m_Direction.Update(targets);
            
            EnableElements();
        }

        void CreateTangentFields()
        {
            m_Magnitude = new FloatField("Magnitude", 3);
            var field = m_Magnitude.Q<VisualElement>("unity-text-input");
            field.AddToClassList(k_TangentMagnitudeFloatFieldStyle);

            m_DirectionLabel = new Label("Direction");
            m_DirectionLabel.AddToClassList(k_TangentLabelStyle);

            var filler = new VisualElement();
            filler.AddToClassList(k_TangentFillerStyle);

            Add(m_Direction = new Float3PropertyField<SelectableTangent>("",
                    (tangent) => tangent.LocalDirection,
                    (tangent, value) => tangent.LocalDirection = value)
                { name = "direction" });
            m_Direction.changed += () => { UpdateMagnitudeField(targets); };

            //Build UI Hierarchy
            Add(m_Magnitude);
            Add(m_DirectionLabel);
            Add(filler);
            filler.Add(m_Direction);
        }

        void UpdateMagnitudeField(IReadOnlyList<SelectableTangent> tangents)
        {
            s_LengthBuffer.Clear();
            for (int i = 0; i < tangents.Count; ++i)
                s_LengthBuffer.Add(math.length(tangents[i].LocalPosition));

            m_Magnitude.showMixedValue = SplineGUIUtility.HasMultipleValues(s_LengthBuffer, s_MagnitudeComparer);
            if (!m_Magnitude.showMixedValue)
                m_Magnitude.SetValueWithoutNotify(s_LengthBuffer[0]);
        }

        void UpdateTangentMagnitude(float value)
        {
            ElementInspector.ignoreKnotCallbacks = true;
            for (int i = 0; i < targets.Count; ++i)
            {
                var direction = new float3(0, 0, 1);

                var tangent = targets[i];
                if (math.length(tangent.LocalPosition) > 0)
                    direction = math.normalize(tangent.LocalPosition);

                tangent.LocalPosition = value * direction;
            }
            ElementInspector.ignoreKnotCallbacks = false;
        }

        void EnableElements()
        {
            bool tangentsModifiable = true;
            bool tangentsBroken = true;
            for (int i = 0; i < targets.Count; ++i)
            {
                var mode = targets[i].Owner.Mode;
                tangentsModifiable &= EditorSplineUtility.AreTangentsModifiable(mode);
                tangentsBroken &= mode == TangentMode.Broken;
            }
            
            m_DirectionLabel.style.display = tangentsModifiable ? DisplayStyle.Flex : DisplayStyle.None;
            m_Direction.style.display = tangentsModifiable ? DisplayStyle.Flex : DisplayStyle.None;
            m_Magnitude.style.display = tangentsModifiable ? DisplayStyle.Flex : DisplayStyle.None;

            if(tangentsModifiable)
                m_Direction.SetEnabled(tangentsBroken);
        }
    }
}
