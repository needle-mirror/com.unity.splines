using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [InitializeOnLoad]
    public static class SplineExtrudeUtility
    {
        static SplineExtrudeUtility()
        {            
#if UNITY_2022_2_OR_NEWER
            ClipboardUtility.duplicatedGameObjects += OnPasteOrDuplicated;
            ClipboardUtility.pastedGameObjects += OnPasteOrDuplicated;
#else
            ObjectChangeEvents.changesPublished += ObjectEventChangesPublished;
#endif
        }

#if UNITY_2022_2_OR_NEWER
        static void OnPasteOrDuplicated(GameObject[] duplicates)
        {
            foreach (var duplicate in duplicates)
                CheckForExtrudeMeshCreatedOrModified(duplicate);
        }
#else
        static void ObjectEventChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0, c = stream.length; i < c; ++i)
            {
                // SplineExtrude was created via duplicate, copy paste
                if (stream.GetEventType(i) == ObjectChangeKind.CreateGameObjectHierarchy)
                {
                    stream.GetCreateGameObjectHierarchyEvent(i, out CreateGameObjectHierarchyEventArgs data);
                    GameObjectCreatedOrStructureModified(data.instanceId);
                }
            }
        }
        
        static void GameObjectCreatedOrStructureModified(int instanceId)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is GameObject go)
                CheckForExtrudeMeshCreatedOrModified(go);
        }
#endif
        
        static void CheckForExtrudeMeshCreatedOrModified(GameObject go)
        {
            //Check if the current GameObject has a SplineExtrude component
            if(go.TryGetComponent<SplineExtrude>(out var extrudeComponent))
                extrudeComponent.Reset();
            
            var childCount = go.transform.childCount;
            if (childCount > 0)
            {
                //Check through the children
                for(int childIndex = 0; childIndex < childCount; ++childIndex)
                    CheckForExtrudeMeshCreatedOrModified(go.transform.GetChild(childIndex).gameObject);
            }
        }
    }
}
