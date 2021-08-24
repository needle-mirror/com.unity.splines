using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Splines;
using UObject = UnityEngine.Object;

namespace UnityEditor.Splines
{
	public static class EditorSplineGizmos
	{
		[DrawGizmo(GizmoType.Active | GizmoType.NonSelected | GizmoType.Selected)]
		static void DrawSplineContainerGizmos(ISplineProvider provider, GizmoType gizmoType)
		{
			//Skip if tool engaged is a spline tool
			if (typeof(SplineTool).IsAssignableFrom(ToolManager.activeToolType) &&
			    (provider is UObject objectProvider) && EditableSplineManager.TryGetTargetData(objectProvider, out _))
				return;

			Gizmos.color = Color.blue;
			SplineGizmoUtility.DrawGizmos(provider);
			Gizmos.color = Color.white;
		}
	}
}
