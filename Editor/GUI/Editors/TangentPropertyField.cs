using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;
#if !UNITY_2022_1_OR_NEWER
using UnityEditor.UIElements;
#endif

namespace UnityEditor.Splines
{
    class TangentPropertyField : VisualElement
    {
        static readonly List<float> s_LengthBuffer = new List<float>(0);
        static readonly SplineGUIUtility.EqualityComparer<float> s_MagnitudeComparer = (a, b) => a.Equals(b);

        const string k_TangentFoldoutStyle = "tangent-drawer";
        const string k_TangentMagnitudeFloatFieldStyle = "tangent-magnitude-floatfield";

        readonly BezierTangent m_Direction;
        readonly FloatField m_Magnitude;
        public readonly Float3PropertyField<SelectableKnot> vector3field;

        IReadOnlyList<SelectableKnot> m_Elements = new List<SelectableKnot>(0);

        public event Action changed;

        public TangentPropertyField(string text, string vect3name, BezierTangent direction)
        {
            m_Direction = direction;

            //Create Elements
            AddToClassList(k_TangentFoldoutStyle);
            AddToClassList("unity-base-field");

            style.marginBottom = style.marginLeft = style.marginRight = style.marginTop = 0;

            var foldout = new Foldout() { value = SessionState.GetBool("Splines." + vect3name + ".Foldout", false)};
            var foldoutToggle = foldout.Q<Toggle>();

            m_Magnitude = new FloatField(L10n.Tr(text), 6);
            m_Magnitude.style.flexDirection = FlexDirection.Row;
            m_Magnitude.RemoveFromClassList("unity-base-field");
            vector3field = new Float3PropertyField<SelectableKnot>("", GetTangentPosition, ApplyPosition) { name = vect3name };

            //Build UI Hierarchy
            Add(foldout);
            foldoutToggle.Add(m_Magnitude);
            foldout.Add(vector3field);
            foldout.Q<VisualElement>("unity-content").style.marginBottom = 0;
            foldout.RegisterValueChangedCallback((evt) =>
            {
                SessionState.SetBool("Splines." + vect3name + ".Foldout", evt.newValue);
                #if UNITY_6000_0_OR_NEWER
                SplineInspectorOverlay.instance?.RefreshPopup();
                #endif
            });

            var field = m_Magnitude.Q<VisualElement>("unity-text-input");
            field.AddToClassList(k_TangentMagnitudeFloatFieldStyle);

            vector3field.changed += () =>
            {
                Update(m_Elements);
                changed?.Invoke();
            };

            m_Magnitude.RegisterValueChangedCallback((evt) =>
            {
                var value = evt.newValue;
                if (evt.newValue < 0f)
                {
                    m_Magnitude.SetValueWithoutNotify(0f);
                    value = 0f;
                }

                EditorSplineUtility.RecordObjects(m_Elements, SplineInspectorOverlay.SplineChangeUndoMessage);
                for (var i = 0; i < m_Elements.Count; ++i)
                {
                    var knot = m_Elements[i];
                    UpdateTangentMagnitude(new SelectableTangent(knot.SplineInfo, knot.KnotIndex, m_Direction), value, m_Direction == BezierTangent.In ? -1f : 1f);
                }

                m_Magnitude.showMixedValue = false;
                Update(m_Elements);
                changed?.Invoke();
            });
        }

        public void Update(IReadOnlyList<SelectableKnot> elements)
        {
            m_Elements = elements;

            s_LengthBuffer.Clear();
            for (int i = 0; i < elements.Count; ++i)
                s_LengthBuffer.Add(math.length((m_Direction == BezierTangent.In ? elements[i].TangentIn : elements[i].TangentOut).LocalPosition));

            m_Magnitude.showMixedValue = SplineGUIUtility.HasMultipleValues(s_LengthBuffer, s_MagnitudeComparer);
            if (!m_Magnitude.showMixedValue)
                m_Magnitude.SetValueWithoutNotify(s_LengthBuffer[0]);

            vector3field.Update(elements);
        }

        float3 GetTangentPosition(SelectableKnot knot)
        {
            return new SelectableTangent(knot.SplineInfo, knot.KnotIndex, m_Direction).LocalPosition;
        }

        void ApplyPosition(SelectableKnot knot, float3 position)
        {
            new SelectableTangent(knot.SplineInfo, knot.KnotIndex, m_Direction) { LocalPosition = position };
        }

        void UpdateTangentMagnitude(SelectableTangent tangent, float value, float directionSign)
        {
            var direction = new float3(0, 0, directionSign);
            if (math.length(tangent.LocalDirection) > 0)
                direction = math.normalize(tangent.LocalDirection);

            ElementInspector.ignoreKnotCallbacks = true;
            tangent.LocalPosition = value * direction;
            ElementInspector.ignoreKnotCallbacks = false;
        }
    }
}