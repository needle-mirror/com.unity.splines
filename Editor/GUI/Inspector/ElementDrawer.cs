using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    interface IElementDrawer
    {
        void SetTarget(ISplineElement element);
        void Update();
        void OnTargetSet();
    }

    abstract class ElementDrawer<T> : VisualElement, IElementDrawer where T : ISplineElement
    {
        const int k_FloatFieldsDigits = 3;
        
        public T target { get; private set; }

        public virtual void Update() {}

        public void SetTarget(ISplineElement element)
        {
            target = (T) element;
            OnTargetSet();
        }
        
        public static float Round(float value)
        {
            float mult = Mathf.Pow(10.0f, k_FloatFieldsDigits);
            return Mathf.Round(value * mult) / mult;
        }

        public virtual void OnTargetSet() { }

        protected void IgnoreKnotCallbacks(bool ignore)
        {
            if (parent is ElementInspector inspector)
                inspector.ignoreKnotCallbacks = ignore;
        }
    }
}
