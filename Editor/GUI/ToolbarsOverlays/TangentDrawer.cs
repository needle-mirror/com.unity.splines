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

        static readonly List<float> s_LengthBuffer = new List<float>(0);
        static readonly SplineGUIUtility.EqualityComparer<float> s_MagnitudeComparer = (a, b) => a.Equals(b);

        readonly TangentModePropertyField<SelectableTangent> m_Mode;
        readonly BezierTangentPropertyField<SelectableTangent> m_BezierMode;

        FloatField m_Magnitude;
        Float3PropertyField<SelectableTangent> m_Direction;

        public TangentDrawer()
        {
            AddToClassList(k_TangentDrawerStyle);

            Add(m_Mode = new TangentModePropertyField<SelectableTangent>());
            m_Mode.changed += () =>
            {
                m_BezierMode.Update(targets);
                EnableElements();
            };
            Add(m_BezierMode = new BezierTangentPropertyField<SelectableTangent>());
            m_BezierMode.changed += () =>
            {
                m_Mode.Update(targets);
                EnableElements();
            };

            CreateTangentFields();

            m_Magnitude.RegisterValueChangedCallback((evt) =>
            {
                var value = evt.newValue;
                if (evt.newValue < 0f)
                {
                    m_Magnitude.SetValueWithoutNotify(0f);
                    value = 0f;
                }

                Undo.RecordObject(target.SplineInfo.Object, SplineInspectorOverlay.SplineChangeUndoMessage);
                UpdateTangentMagnitude(value);
                var tangent = target;
                m_Direction.SetValueWithoutNotify(tangent.LocalPosition);
            });
        }

        public override string GetLabelForTargets()
        {
            if (targets.Count > 1)
                return $"<b>({targets.Count}) Tangents</b> selected";

            var inOutLabel = target.TangentIndex == 0 ? "In" : "Out";
            return $"Tangent <b>{inOutLabel}</b> selected (<b>Knot {target.KnotIndex}</b>, <b>Spline {target.SplineInfo.Index}</b>)";
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
            Add(m_Magnitude = new FloatField(L10n.Tr("Length"), 6));

            Add(m_Direction = new Float3PropertyField<SelectableTangent>(L10n.Tr("Direction"),
                    (tangent) => tangent.LocalDirection,
                    (tangent, value) => tangent.LocalDirection = value)
                { name = "direction" });
            m_Direction.changed += () => { UpdateMagnitudeField(targets); };
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
                tangentsModifiable &= SplineUtility.AreTangentsModifiable(mode);
                tangentsBroken &= mode == TangentMode.Broken;
            }

            m_Direction.SetEnabled(tangentsModifiable && tangentsBroken);
            m_Magnitude.SetEnabled(tangentsModifiable);
        }
    }
}