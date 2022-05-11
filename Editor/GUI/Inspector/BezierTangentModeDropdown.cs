using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class BezierTangentModeDropdown<T> : ModeDropdown<T>
        where T : ISplineElement
    {
        static readonly SplineGUIUtility.EqualityComparer<T> s_Comparer = (a, b) =>
            EditorSplineUtility.GetKnot(a).Mode == EditorSplineUtility.GetKnot(b).Mode;

        protected override string[] modes => k_Modes;
        static readonly string[] k_Modes = {L10n.Tr("Mirrored"), L10n.Tr("Continuous"), L10n.Tr("Broken")};

        protected override string modesTooltip => k_Tooltip;

        static readonly string k_Tooltip = L10n.Tr(
            "Mirrored Tangents:\nIf Knot or InTangent is selected, OutTangent will be mirrored on InTangent. Else, InTangent will be mirrored on OutTangent.\n" +
            "Continuous Tangents:\nInTangent and OutTangent are always aligned.\n" +
            "Broken Tangents:\nInTangent and OutTangent are dissociated.\n"
        );

        protected override string[] icons => s_IconStyles;
        static readonly string[] s_IconStyles = new[] {"tangent-mirrored", "tangent-continuous", "tangent-broken"};

        internal BezierTangentModeDropdown()
        {
            VisualElement row;
            Add(row = new VisualElement() {name = "BezierModes"});
            row.style.flexDirection = FlexDirection.Row;

            var label = new Label() {text = L10n.Tr("Tangents")};
            row.Add(label);
            label.style.flexGrow = 1;
            label.style.height = 24;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;

            row.Add(m_ModeDropdown = new DropdownField() {name = "ModesDropdown"});
            m_ModeDropdown.choices = modes.ToList();
            m_ModeDropdown.tooltip = modesTooltip;

            m_ModeDropdown.RegisterValueChangedCallback(OnValueChange);
        }

        protected override bool HasMultipleValues(IReadOnlyList<T> targets)
        {
            return SplineGUIUtility.HasMultipleValues(targets, s_Comparer);
        }

        protected override bool ShouldShow(IReadOnlyList<T> targets)
        {
            // Don't show if an element in the selection isn't a bezier mode 
            for (int i = 0; i < targets.Count; ++i)
            {
                var knot = EditorSplineUtility.GetKnot(targets[i]);
                if (!EditorSplineUtility.AreTangentsModifiable(knot.Mode))
                    return false;
            }

            return true;
        }

        internal override TangentMode GetTangentModeFromIndex(int index)
        {
            if (index == 0)
                return TangentMode.Mirrored;
            if (index == 1)
                return TangentMode.Continuous;

            return TangentMode.Broken;
        }

        internal override int GetIndexFromTangentMode(TangentMode mode)
        {
            if (mode == TangentMode.Mirrored)
                return 0;

            if (mode == TangentMode.Continuous)
                return 1;

            if (mode == TangentMode.Broken)
                return 2;

            return 2;
        }

        protected override void OnValueChange(ChangeEvent<string> evt)
        {
            if (ArrayUtility.IndexOf(modes, evt.newValue) < 0)
                return;

            SetTangentValue(m_ModeDropdown.index);

            iconElement.RemoveFromClassList(m_CurrentIconStyle);
            iconElement.AddToClassList(m_CurrentIconStyle = icons[m_ModeDropdown.index]);

            base.OnValueChange(evt);
        }

        void SetTangentValue(int newIndex)
        {
            SetTangentMode(GetTangentModeFromIndex(newIndex));
        }
    }
}