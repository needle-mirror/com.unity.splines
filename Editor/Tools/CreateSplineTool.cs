using UnityEngine;
using UnityEditor.EditorTools;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
#if UNITY_2023_1_OR_NEWER
    [EditorTool("Create Spline", toolPriority = 10)]
#else
    [EditorTool("Create Spline")]
#endif
    class CreateSplineTool : KnotPlacementTool
    {
        public override GUIContent toolbarIcon => PathIcons.createSplineTool;

        [NonSerialized]
        List<Object> m_Targets = new List<Object>(1);

        protected override void AddKnotOnSurface(float3 position, float3 normal, float3 tangentOut)
        {
            if (MainTarget == null)
            {
                var gameObject = SplineMenu.CreateSplineGameObject(new MenuCommand(null));

                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localRotation = Quaternion.identity;

                MainTarget = gameObject.GetComponent<SplineContainer>();
                Selection.activeGameObject = gameObject;
                EditorSplineGizmos.showSelectedGizmo = false;
                // Set hasChanged to false as we don't want to override a custom transform set by the user.
                gameObject.transform.hasChanged = false;
            }

            base.AddKnotOnSurface(position, normal, tangentOut);
        }

        public override void OnActivated()
        {
            base.OnActivated();
            // Enable the gizmo drawing of the selected object because we aren't drawing using handles
            EditorSplineGizmos.showSelectedGizmo = true;
        }

        public override void OnWillBeDeactivated()
        {
            EditorSplineGizmos.showSelectedGizmo = false;
            base.OnWillBeDeactivated();
        }

        void UpdateTargets()
        {
            m_Targets.Clear();

            if (ToolManager.activeContextType == typeof(SplineToolContext))
            {
                m_Targets.AddRange(SplineToolContext.GetTargets());
                MainTarget = m_Targets[0] as Component;
            }
            else if (MainTarget != null)
                m_Targets.Add(MainTarget);
        }

        protected override IEnumerable<Object> GetTargets()
        {
            UpdateTargets();
            return m_Targets;
        }

        protected override IReadOnlyList<Object> GetSortedTargets(out Object mainTarget)
        {
            UpdateTargets();
            mainTarget = MainTarget;
            return m_Targets;
        }
    }
}
