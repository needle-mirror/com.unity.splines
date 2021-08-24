using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    internal class SplineDataList : VisualElement
    {
        const string k_UxmlPath = "Packages/com.unity.splines/Editor/Resources/Overlays/UXML/splinedata-overlay.uxml";
        static VisualTreeAsset s_TreeAsset;

        ScrollView m_ListRoot;

        List<SplineDataOverlay.SplineDataContainer> m_SplineDataContainers = new List<SplineDataOverlay.SplineDataContainer>();
        
        public SplineDataList()
        {
            if(s_TreeAsset == null)
                s_TreeAsset = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(k_UxmlPath, typeof(VisualTreeAsset));

            if(s_TreeAsset != null)
            {
                s_TreeAsset.CloneTree(this);
                m_ListRoot = this.Q<ScrollView>("ContainerList");
            }
        }
        
        public void RebuildMenu(List<SplineDataOverlay.SplineDataContainer> elements)
        {
            m_SplineDataContainers.Clear();
            m_ListRoot.Clear();

            for(int i = 0; i < elements.Count; i++)
            {
                m_SplineDataContainers.Add(elements[i]);

                var ve = new SplineDataFoldout();
                ve.target = m_SplineDataContainers[i];
                m_ListRoot.Add(ve);
            }
        }
    }
}
