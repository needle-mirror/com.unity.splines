using System.Linq;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Splines;
using UObject = UnityEngine.Object;

namespace UnityEditor.Splines
{
	static class EditorSplineGizmos
	{
		[DrawGizmo(GizmoType.Active | GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
		static void DrawUnselectedSplineGizmos(ISplineContainer provider, GizmoType gizmoType)
		{
			//Skip if tool engaged is a spline tool
			if (typeof(SplineTool).IsAssignableFrom(ToolManager.activeToolType)
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