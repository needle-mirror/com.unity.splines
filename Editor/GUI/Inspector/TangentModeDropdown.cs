using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class TangentModeDropdown<T> : ModeDropdown<T>
        where T : ISplineElement
    {
        static readonly SplineGUIUtility.EqualityComparer<T> s_Comparer = (a, b) =>
            GetModeForComparison(EditorSplineUtility.GetKnot(a).Mode) ==
            GetModeForComparison(EditorSplineUtility.GetKnot(b).Mode);

        static TangentMode GetModeForComparison(TangentMode mode)
        {
            switch (mode)
            {
                case TangentMode.Continuous:
                case TangentMode.Mirrored:
                case TangentMode.Broken:
                    return TangentMode.Broken; //Used to represent bezier

                default:
                    return mode;
            }
        }

        protected override string[] modes => k_Modes;
        static readonly string[] k_Modes = {L10n.Tr("Linear"), L10n.Tr("Auto"), L10n.Tr("Bezier") };

        protected override string modesTooltip => k_Tooltip;
        static readonly string k_Tooltip = L10n.Tr(
            "Linear Tangents:\nTangents are pointing to the previous/next spline knot.\n" +
            "Auto Smooth:\nTangents are calculated using the previous and next knot positions.\n"+
            "Bezier:\nTangents are customizable and modifiable.\n"
        );

        protected override string[] icons => s_IconStyles;
        static readonly string[] s_IconStyles = new[] { "tangent-linear","tangent-autosmooth","tangent-continuous" };

        internal TangentModeDropdown()
        {
            VisualElement row;
            Add(row = new VisualElement(){name = "TangentModes"});
            row.style.flexDirection = FlexDirection.Row;

            var label = new Label() { text = L10n.Tr("Mode") };
            row.Add(label);
            label.style.flexGrow = 1;
            label.style.height = 24;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;

            row.Add(m_ModeDropdown = new DropdownField(){name = "ModesDropdown"});
            m_ModeDropdown.choices = modes.ToList();
            m_ModeDropdown.tooltip = modesTooltip;

            m_ModeDropdown.RegisterValueChangedCallback(OnValueChange);
        }

        protected override bool HasMultipleValues(IReadOnlyList<T> targets)
        {
            return SplineGUIUtility.HasMultipleValues(targets, s_Comparer);
        }

        internal override TangentMode GetTangentModeFromIndex(int index)
        {
            if(index == 0)
                return TangentMode.Linear;
            if(index == 1)
                return TangentMode.AutoSmooth;

            return TangentMode.Broken;
        }

        internal override int GetIndexFromTangentMode(TangentMode mode)
        {
            if(mode == TangentMode.Linear)
                return 0;
            if(mode == TangentMode.AutoSmooth)
                return 1;

            return 2;
        }

        protected override void OnValueChange(ChangeEvent<string> evt)
        {
            if (ArrayUtility.IndexOf(modes, evt.newValue) < 0)
                return;

            iconElement.RemoveFromClassList(m_CurrentIconStyle);
            iconElement.AddToClassList(m_CurrentIconStyle = icons[m_ModeDropdown.index]);

            var mode = GetTangentModeFromIndex(m_ModeDropdown.index);

            SetTangentMode(mode);

            base.OnValueChange(evt);
        }
    }
}