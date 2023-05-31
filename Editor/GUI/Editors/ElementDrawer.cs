using System.Collections.Generic;
using UnityEngine.Splines;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    interface IElementDrawer
    {
        bool HasKnot(Spline spline, int index);
        void PopulateTargets(IReadOnlyList<SplineInfo> splines);
        void Update();
        string GetLabelForTargets(); 
    }

    abstract class ElementDrawer<T> : VisualElement, IElementDrawer where T : ISelectableElement
    {
        public List<T> targets { get; } = new List<T>();
        public T target => targets[0];

        public virtual void Update() {}
        public virtual string GetLabelForTargets() => string.Empty;

        public bool HasKnot(Spline spline, int index)
        {
            foreach (var t in targets)
                if (t.SplineInfo.Spline == spline && t.KnotIndex == index)
                    return true;

            return false;
        }

        public void PopulateTargets(IReadOnlyList<SplineInfo> splines)
        {
            SplineSelection.GetElements(splines, targets);
        }
    }
}