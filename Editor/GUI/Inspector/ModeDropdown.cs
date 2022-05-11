using System;
using System.Collections.Generic;
using UnityEngine.Splines;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    abstract class ModeDropdown<T> : VisualElement
        where T : ISplineElement
    {
        protected abstract string[] modes{ get; }
        protected abstract string[] icons{ get; }

        protected abstract string modesTooltip{ get; }

        protected string m_CurrentIconStyle = String.Empty;

        protected DropdownField m_ModeDropdown;
        IReadOnlyList<T> m_Elements = new List<T>(0);

        public event Action changed;

        Image m_IconElement;
        protected Image iconElement
        {
            get
            {
                if(m_IconElement == null && m_ModeDropdown != null)
                {
                    var inputField = m_ModeDropdown.ElementAt(0);
                    inputField.Insert(0, m_IconElement = new Image() { name = "TangentIcon" });
                }

                return m_IconElement;
            }
        }

        protected virtual void OnValueChange(ChangeEvent<string> evt)
        {
            m_ModeDropdown.showMixedValue = false;

            changed?.Invoke();
        }
        protected virtual bool ShouldShow(IReadOnlyList<T> targets) => true;
        protected abstract bool HasMultipleValues(IReadOnlyList<T> targets);

        public void Update()
        {
            Update(m_Elements);
        }

        public void Update(IReadOnlyList<T> targets)
        {
            style.display = ShouldShow(targets) ? DisplayStyle.Flex : DisplayStyle.None;

            m_Elements = targets;
            SetValueNoNotify(EditorSplineUtility.GetKnot(targets[0]).Mode);

            m_ModeDropdown.showMixedValue = HasMultipleValues(targets);
            iconElement.style.display = m_ModeDropdown.showMixedValue ? DisplayStyle.None : DisplayStyle.Flex;
        }

        internal abstract TangentMode GetTangentModeFromIndex(int index);
        internal abstract int GetIndexFromTangentMode(TangentMode mode);

        protected void SetValueNoNotify(TangentMode modeValue)
        {
            var modeIndex = GetIndexFromTangentMode(modeValue);

            if(modes.Length > 0 && modeIndex >= 0 && modeIndex < modes.Length)
            {
                m_ModeDropdown.SetValueWithoutNotify(modes[modeIndex]);

                iconElement.RemoveFromClassList(m_CurrentIconStyle);
                iconElement.AddToClassList(m_CurrentIconStyle = icons[modeIndex]);
            }
        }

        internal TangentMode GetMode()
        {
            return GetTangentModeFromIndex(m_ModeDropdown.index);
        }

        protected void SetTangentMode(TangentMode mode)
        {
            EditorSplineUtility.RecordSelection(SplineInspectorOverlay.SplineChangeUndoMessage);

            for (int i = 0; i < m_Elements.Count; ++i)
            {
                var knot = EditorSplineUtility.GetKnot(m_Elements[i]);
                var previousMode = knot.Mode;
                if (previousMode == mode)
                    return;
                
                BezierTangent mainTangent = BezierTangent.Out;

                // If we were in a non bezier mode and we swap to bezier, set mirrored by default
                if ((previousMode == TangentMode.AutoSmooth || previousMode == TangentMode.Linear) && mode == TangentMode.Broken)
                    mode = TangentMode.Mirrored;

                if (mode is TangentMode.Mirrored or TangentMode.Continuous)
                {
                    // m_Target is the knot "knot", use the InTangent to resolve the new mode
                    var refTangent = knot.TangentIn;

                    // Else if target is a tangent, update the values regarding the selected tangent
                    if (m_Elements[i] is SelectableTangent target)
                        refTangent = target;

                    mainTangent = (BezierTangent)refTangent.TangentIndex;
                }

                knot.SetTangentMode(mode, mainTangent);
            }
        }
    }
}