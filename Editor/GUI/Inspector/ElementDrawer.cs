using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    interface IElementDrawer
    {
        void SetTarget(ISplineElement element);
        void Update();
    }

    abstract class ElementDrawer<T> : VisualElement, IElementDrawer where T : ISplineElement
    {
        public T target { get; private set; }

        public virtual void Update() {}

        public void SetTarget(ISplineElement element)
        {
            target = (T) element;
        }
    }
}
