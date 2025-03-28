using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// A utility class providing methods to handle and manage spline extrusion operations.
    /// Initializes event listeners on load to monitor and respond to object changes, duplication, and paste operations specific to splines.
    /// </summary>
    [InitializeOnLoad]
    public static class SplineExtrudeUtility
    {
        static SplineExtrudeUtility()
        {
#if UNITY_2022_2_OR_NEWER
            ClipboardUtility.duplicatedGameObjects += OnPasteOrDuplicated;
            ClipboardUtility.pastedGameObjects += OnPasteOrDuplicated;
            ObjectChangeEvents.changesPublished += ObjectEventChangesPublished;
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

        static void ObjectEventChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                var type = stream.GetEventType(i);
                if (type == ObjectChangeKind.ChangeGameObjectStructure)
                {
                    stream.GetChangeGameObjectStructureEvent(i, out var changeGameObjectStructure);
                    if (EditorUtility.InstanceIDToObject(changeGameObjectStructure.instanceId) is GameObject go)
                        CheckForSplineExtrudeAdded(go);
                }
            }
        }
#else
        static void ObjectEventChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0, c = stream.length; i < c; ++i)
            {
                // SplineExtrude was created via duplicate, copy paste
                var type = stream.GetEventType(i);
                if (type == ObjectChangeKind.CreateGameObjectHierarchy)
                {
                    stream.GetCreateGameObjectHierarchyEvent(i, out CreateGameObjectHierarchyEventArgs data);
                    GameObjectCreatedOrStructureModified(data.instanceId);
                }
                else if (type == ObjectChangeKind.ChangeGameObjectStructure)
                {
                    stream.GetChangeGameObjectStructureEvent(i, out var changeGameObjectStructure);
                    if (EditorUtility.InstanceIDToObject(changeGameObjectStructure.instanceId) is GameObject go)
                        CheckForSplineExtrudeAdded(go);
                }
            }
        }

        static void GameObjectCreatedOrStructureModified(int instanceId)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is GameObject go)
                CheckForExtrudeMeshCreatedOrModified(go);
        }
#endif

        static void CheckForSplineExtrudeAdded(GameObject go)
        {
            if (go.TryGetComponent<SplineExtrude>(out var splineExtrude))
                splineExtrude.SetSplineContainerOnGO();

            var childCount = go.transform.childCount;
            if (childCount > 0)
            {
                for (int childIndex = 0; childIndex < childCount; ++childIndex)
                    CheckForSplineExtrudeAdded(go.transform.GetChild(childIndex).gameObject);
            }
        }

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
