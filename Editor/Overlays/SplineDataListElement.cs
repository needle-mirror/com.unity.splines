using System;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    internal class SplineDataListElement : VisualElement
    {
        const string k_UxmlPath = "Packages/com.unity.splines/Editor/Resources/Overlays/UXML/splinedata-element.uxml";
        static VisualTreeAsset s_TreeAsset;

        SplineDataOverlay.SplineDataElement m_Target;
        public SplineDataOverlay.SplineDataElement target
        {
            get => m_Target;
            set
            {
                m_Target = value;
                m_Toggle.value = m_Target.displayed;
                m_Foldout.text = m_Target.splineDataField.Name;
                m_ColorField.value = m_Target.color;
                m_SplineField.value = m_Target.splineContainer;
            }
        }
        
        VisualElement m_SplineDataElement;
        Toggle m_Toggle;
        Foldout m_Foldout;
        ColorField m_ColorField;
        ObjectField m_SplineField;
        DropdownField m_DropDownIndexType;
        
        public SplineDataListElement()
        {
            if(s_TreeAsset == null)
                s_TreeAsset = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(k_UxmlPath, typeof(VisualTreeAsset));

            s_TreeAsset.CloneTree(this);

            m_SplineDataElement = this.Q<VisualElement>("SplineDataElement");
            
            m_Toggle = this.Q<Toggle>("SplineDataToggle");
            m_Toggle.RegisterValueChangedCallback(OnToggleValueChanged);

            m_Foldout = this.Q<Foldout>("SplineDataFoldout");
            m_Foldout.value = false;
            
            m_ColorField = this.Q<ColorField>("SplineDataColor");
            m_ColorField.RegisterValueChangedCallback(OnColorValueChanged);
            
            m_SplineField = this.Q<ObjectField>("SplineDataSpline");
            m_SplineField.objectType = typeof(SplineContainer);
            m_SplineField.allowSceneObjects = true;
            m_SplineField.RegisterValueChangedCallback(OnSplineTargetChanged);

            m_DropDownIndexType = this.Q<DropdownField>("DataLabelType");
            m_DropDownIndexType.choices = Enum.GetNames(typeof(SplineDataHandles.LabelType)).ToList();
            m_DropDownIndexType.index = 0;
            m_DropDownIndexType.RegisterValueChangedCallback(OnDropdownIndexChanged);
        }
        
        void OnToggleValueChanged(ChangeEvent<bool> boolEvent)
        {
            target.displayed = boolEvent.newValue;
        }
        
        void OnColorValueChanged(ChangeEvent<Color> colorEvent)
        {
            target.color = colorEvent.newValue;
        }

        void OnSplineTargetChanged(ChangeEvent<Object> evt)
        {
            if(evt.newValue is SplineContainer container)
                target.splineContainer = container;
            else
                target.splineContainer = null;
        }

        void OnDropdownIndexChanged(ChangeEvent<string> strEvent)
        {
            target.labelType = (SplineDataHandles.LabelType)m_DropDownIndexType.index;
        }
    }
}
