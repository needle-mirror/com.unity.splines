using System;
using System.Collections.Generic;
using UnityEngine.Splines;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class BezierTangentPropertyField<T> : DropdownField
        where T : ISelectableElement
    {
        static readonly SplineGUIUtility.EqualityComparer<T> s_Comparer = (a, b) =>
            EditorSplineUtility.GetKnot(a).Mode == EditorSplineUtility.GetKnot(b).Mode;

        static readonly List<string> k_Modes = new List<string>{L10n.Tr("Mirrored"), L10n.Tr("Continuous"), L10n.Tr("Broken")};

        static readonly string k_Tooltip = L10n.Tr(
            "Mirrored Tangents:\nIf Knot or InTangent is selected, OutTangent will be mirrored on InTangent. Else, InTangent will be mirrored on OutTangent.\n" +
            "Continuous Tangents:\nInTangent and OutTangent are always aligned.\n" +
            "Broken Tangents:\nInTangent and OutTangent are dissociated.\n"
        );

        IReadOnlyList<T> m_Elements = new List<T>(0);

        public event Action changed;

        internal BezierTangentPropertyField() : base(k_Modes, 0)
        {
            label = L10n.Tr("Bezier");
            name = "ModesDropdown";
            tooltip = k_Tooltip;

            this.RegisterValueChangedCallback(OnValueChange);
        }

        bool ShouldShow(IReadOnlyList<T> targets)
        {
            // Don't show if an element in the selection isn't a bezier mode
            for (int i = 0; i < targets.Count; ++i)
            {
                var knot = EditorSplineUtility.GetKnot(targets[i]);
                if (!SplineUtility.AreTangentsModifiable(knot.Mode))
                    return false;
            }

            return true;
        }

        static TangentMode GetTangentModeFromIndex(int index)
        {
            switch (index)
            {
                case 0: return TangentMode.Mirrored;
                case 1: return TangentMode.Continuous;
                case 2: return TangentMode.Broken;
                default: return TangentMode.Mirrored;
            }
        }

        static int GetIndexFromTangentMode(TangentMode mode)
        {
            switch (mode)
            {
                case TangentMode.Mirrored: return 0;
                case TangentMode.Continuous: return 1;
                case TangentMode.Broken: return 2;
                default: return 0;
            }
        }

        public void Update(IReadOnlyList<T> targets)
        {
            SetEnabled(ShouldShow(targets));

            m_Elements = targets;
            SetValueWithoutNotify(EditorSplineUtility.GetKnot(targets[0]).Mode);

            showMixedValue = SplineGUIUtility.HasMultipleValues(targets, s_Comparer);
        }

        public void SetValueWithoutNotify(TangentMode mode)
        {
            SetValueWithoutNotify(k_Modes[GetIndexFromTangentMode(mode)]);
        }

        void OnValueChange(ChangeEvent<string> evt)
        {
            var index = k_Modes.IndexOf(evt.newValue);
            if (index < 0)
                return;

            showMixedValue = false;
            var targetMode = GetTangentModeFromIndex(index);

            EditorSplineUtility.RecordSelection(SplineInspectorOverlay.SplineChangeUndoMessage);
            for (int i = 0; i < m_Elements.Count; ++i)
            {
                var knot = EditorSplineUtility.GetKnot(m_Elements[i]);
                if (m_Elements[i] is SelectableTangent tangent)
                    knot.SetTangentMode(targetMode, (BezierTangent)tangent.TangentIndex);
                else
                    knot.Mode = targetMode;
            }

            changed?.Invoke();
        }
    }
}
