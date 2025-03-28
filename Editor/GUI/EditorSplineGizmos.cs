using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class EditorSplineGizmos
    {
        public static bool showSelectedGizmo = false;

        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
        // ReSharper disable once Unity.ParameterNotDerivedFromComponent
        static void DrawUnselectedSplineGizmos(ISplineContainer provider, GizmoType gizmoType)
        {
            //Skip if tool engaged is a spline tool
            if (typeof(SplineTool).IsAssignableFrom(ToolManager.activeToolType) && !showSelectedGizmo && (gizmoType & GizmoType.Selected) > 0)
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