using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    [Icon("UnityEditor.InspectorWindow")]
    [Overlay(typeof(SceneView), "unity-spline-inspector", "Spline Inspector", "SplineInspector")]
    sealed class SplineInspectorOverlay : Overlay, ITransientOverlay
    {
        static VisualTreeAsset s_VisualTree;

        public bool visible => ToolManager.activeContextType == typeof(SplineToolContext);
        
        ElementInspector m_ElementInspector;

        public override VisualElement CreatePanelContent()
        {
            VisualElement root = new VisualElement();

            m_ElementInspector = new ElementInspector();
            UpdateInspector();

            root.Add(m_ElementInspector);
            
            return root;
        }

        public override void OnCreated()
        {
            displayedChanged += OnDisplayedChange;
            SplineSelection.changed += UpdateInspector;
            SplineConversionUtility.splineTypeChanged += UpdateInspector;
        }

        public override void OnWillBeDestroyed()
        {
            displayedChanged -= OnDisplayedChange;
            SplineSelection.changed -= UpdateInspector;
            SplineConversionUtility.splineTypeChanged -= UpdateInspector;
        }

        void OnDisplayedChange(bool displayed)
        {
            UpdateInspector();
        }

        void UpdateInspector()
        {
            m_ElementInspector?.SetElement(SplineSelection.GetActiveElement(), SplineSelection.count);
        }
    }
}
