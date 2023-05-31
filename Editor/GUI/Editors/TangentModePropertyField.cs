using System;
using System.Collections.Generic;
using UnityEngine.Splines;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class TangentModePropertyField<T> : VisualElement
        where T : ISelectableElement
    {
        const string k_ButtonStripUssClass = "button-strip";
        const string k_ButtonStripButtonUssClass = k_ButtonStripUssClass + "-button";
        const string k_ButtonStripButtonLeftUssClass = k_ButtonStripButtonUssClass + "--left";
        const string k_ButtonStripButtonMiddleUssClass = k_ButtonStripButtonUssClass + "--middle";
        const string k_ButtonStripButtonRightUssClass = k_ButtonStripButtonUssClass + "--right";
        const string k_ButtonStripButtonIconUssClass = k_ButtonStripButtonUssClass + "__icon";
        const string k_ButtonStripButtonTextUssClass = k_ButtonStripButtonUssClass + "__text";
        const string k_ButtonStripButtonCheckedUssClass = k_ButtonStripButtonUssClass + "--checked";

        static readonly SplineGUIUtility.EqualityComparer<T> s_Comparer = (a, b) =>
            GetModeForProperty(EditorSplineUtility.GetKnot(a).Mode) ==
            GetModeForProperty(EditorSplineUtility.GetKnot(b).Mode);

        public event Action changed;

        const TangentMode k_BezierMode = TangentMode.Mirrored;

        readonly Button m_LinearButton;
        readonly Button m_AutoSmoothButton;
        readonly Button m_BezierButton;
        IReadOnlyList<T> m_Elements = new List<T>(0);
        TangentMode m_CurrentMode;

        internal TangentModePropertyField()
        {
            AddToClassList(k_ButtonStripUssClass);

            Add(m_LinearButton = CreateButton("Linear", L10n.Tr("Linear"), L10n.Tr("Tangents are pointing to the previous/next spline knot.")));
            m_LinearButton.AddToClassList(k_ButtonStripButtonLeftUssClass);
            m_LinearButton.clickable.clicked += () => OnValueChange(TangentMode.Linear);

            Add(m_AutoSmoothButton = CreateButton("AutoSmooth", L10n.Tr("Auto"), L10n.Tr("Tangents are calculated using the previous and next knot positions.")));
            m_AutoSmoothButton.AddToClassList(k_ButtonStripButtonMiddleUssClass);
            m_AutoSmoothButton.clickable.clicked += () => OnValueChange(TangentMode.AutoSmooth);

            Add(m_BezierButton = CreateButton("Bezier", L10n.Tr("Bezier"), L10n.Tr("Tangents are customizable and modifiable.")));
            m_BezierButton.AddToClassList(k_ButtonStripButtonRightUssClass);
            m_BezierButton.clickable.clicked += () => OnValueChange(TangentMode.Mirrored);
        }

        static Button CreateButton(string name, string text, string tooltip)
        {
            var button = new Button{name = name};
            button.tooltip = tooltip;
            button.AddToClassList(k_ButtonStripButtonUssClass);

            var icon = new VisualElement();
            icon.AddToClassList(k_ButtonStripButtonIconUssClass);
            icon.pickingMode = PickingMode.Ignore;
            button.Add(icon);

            var label = new TextElement();
            label.AddToClassList(k_ButtonStripButtonTextUssClass);
            label.pickingMode = PickingMode.Ignore;
            label.text = text;
            button.Add(label);

            return button;
        }

        public void Update(IReadOnlyList<T> targets)
        {
            m_Elements = targets;

            if (SplineGUIUtility.HasMultipleValues(m_Elements, s_Comparer))
                SetToMixedValuesState();
            else
                SetValueWithoutNotify(EditorSplineUtility.GetKnot(targets[0]).Mode);
        }

        void SetToMixedValuesState()
        {
            SetValueWithoutNotify((TangentMode)(-1));
        }

        void SetValueWithoutNotify(TangentMode mode)
        {
            m_CurrentMode = GetModeForProperty(mode);

            SetButtonChecked(m_LinearButton, m_CurrentMode == TangentMode.Linear);
            SetButtonChecked(m_AutoSmoothButton, m_CurrentMode == TangentMode.AutoSmooth);
            SetButtonChecked(m_BezierButton, m_CurrentMode == k_BezierMode);
        }
        void SetButtonChecked(Button button, bool isChecked)
        {
            button.EnableInClassList(k_ButtonStripButtonCheckedUssClass, isChecked);
        }

        void OnValueChange(TangentMode mode)
        {
            if (GetModeForProperty(mode) == m_CurrentMode)
                return;

            SetValueWithoutNotify(mode);

            EditorSplineUtility.RecordSelection(SplineInspectorOverlay.SplineChangeUndoMessage);
            for (int i = 0; i < m_Elements.Count; ++i)
            {
                var knot = EditorSplineUtility.GetKnot(m_Elements[i]);
                if (m_Elements[i] is SelectableTangent tangent)
                    knot.SetTangentMode(mode, (BezierTangent)tangent.TangentIndex);
                else
                {
                    // Continuous mode should be prefered instead of Mirrored when switching from AutoSmooth to Bezier 
                    // as Centripetal Catmull-Rom's tangents are not always of equal length.
                    if (knot.Mode == TangentMode.AutoSmooth && mode == TangentMode.Mirrored)
                        knot.Mode = TangentMode.Continuous;
                    else 
                        knot.Mode = mode;
                }
            }

            changed?.Invoke();
        }

        static TangentMode GetModeForProperty(TangentMode mode)
        {
            switch (mode)
            {
                case TangentMode.Continuous:
                case TangentMode.Mirrored:
                case TangentMode.Broken:
                    return k_BezierMode;

                default:
                    return mode;
            }
        }
    }
}