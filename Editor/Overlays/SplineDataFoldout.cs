using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    internal class SplineDataFoldout : VisualElement
    {
        const string k_UxmlPath = "Packages/com.unity.splines/Editor/Resources/Overlays/UXML/splinedata-go-foldout.uxml";
        static VisualTreeAsset s_TreeAsset;

        SplineDataOverlay.SplineDataContainer m_Target;

        public SplineDataOverlay.SplineDataContainer target
        {
            get => m_Target;
            set
            {
                m_Target = value;
                m_ContainerFoldout.text = m_Target.owner.name;
                m_ContainerFoldout.value = false;
                CreateList();
            }
        }
        
        Foldout m_ContainerFoldout;

        List<SplineDataOverlay.SplineDataElement> m_SplineDataElements = new List<SplineDataOverlay.SplineDataElement>();
        
        public SplineDataFoldout()
        {
            if(s_TreeAsset == null)
                s_TreeAsset = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(k_UxmlPath, typeof(VisualTreeAsset));
            s_TreeAsset.CloneTree(this);
            
            m_ContainerFoldout = this.Q<Foldout>("GameObjectContainer");
        }

        void CreateList()
        {
            m_SplineDataElements.Clear();

            for(int i = 0; i < target.splineDataElements.Count; i++)
            {
                m_SplineDataElements.Add(target.splineDataElements[i]);
                
                var ve = new SplineDataListElement();
                ve.target = m_SplineDataElements[i];
                m_ContainerFoldout.Add(ve);
            }
        }
    }
}
