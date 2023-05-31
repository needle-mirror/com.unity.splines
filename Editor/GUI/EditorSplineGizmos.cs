using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Splines;
using UObject = UnityEngine.Object;

namespace UnityEditor.Splines
{
    static class EditorSplineGizmos
    {
        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
        // ReSharper disable once Unity.ParameterNotDerivedFromComponent
        static void DrawUnselectedSplineGizmos(ISplineContainer provider, GizmoType gizmoType)
        {
            //Skip if tool engaged is a spline tool
            if ((typeof(SplineTool).IsAssignableFrom(ToolManager.activeToolType)
                || ToolManager.activeContextType == typeof(SplineToolContext))
                && (gizmoType & GizmoType.Selected) > 0)
                return;

            var prev = Gizmos.color;
            Gizmos.color = (gizmoType & (GizmoType.Selected | GizmoType.Active)) > 0
                ? Handles.selectedColor
                : SplineGizmoUtility.s_GizmosLineColor.value;
            SplineGizmoUtility.DrawGizmos(provider);
            Gizmos.color = prev;
        }
    }
}