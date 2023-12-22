using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomEditor(typeof(SplineScaleTool))]
    class SplineScaleToolSettings : SplineToolSettings { }

    /// <summary>
    /// Provides methods to scale knots and tangents in the Scene view. This tool is only available when you use SplineToolContext.
    /// When you scale a knot, you also scale both its tangents and change the curvature of the segment around the knot. 
    /// `SplineToolContext` manages the selection of knots and tangents. You can manipulate the selection of knots and tangents with `SplineRotateTool`. 
    /// </summary>
#if UNITY_2021_2_OR_NEWER
    [EditorTool("Spline Scale Tool", typeof(ISplineContainer), typeof(SplineToolContext))]
#else
    [EditorTool("Spline Scale Tool", typeof(ISplineContainer))]
#endif
    public sealed class SplineScaleTool : SplineTool
    {
        /// <inheritdoc />
        public override GUIContent toolbarIcon => PathIcons.splineScaleTool;

        Vector3 m_currentScale = Vector3.one;

        /// <inheritdoc />
        public override void OnToolGUI(EditorWindow window)
        {
            if (Event.current.type == EventType.MouseDrag)
            {
                if (handleOrientation == HandleOrientation.Element || handleOrientation == HandleOrientation.Parent)
                    TransformOperation.pivotFreeze |= TransformOperation.PivotFreeze.Rotation;
            }

            if (Event.current.type == EventType.Layout)
                UpdatePivotPosition(true);

            if (Event.current.type == EventType.MouseDown)
            {
                TransformOperation.RecordMouseDownState();
                TransformOperation.pivotFreeze = TransformOperation.PivotFreeze.Position;
            }
            if(Event.current.type == EventType.MouseUp)
            {
                m_currentScale = Vector3.one;
                TransformOperation.ClearMouseDownState();
                TransformOperation.pivotFreeze = TransformOperation.PivotFreeze.None;
                UpdateHandleRotation();
            }

            if(TransformOperation.canManipulate && !DirectManipulation.IsDragging)
            {
                EditorGUI.BeginChangeCheck();
                m_currentScale = Handles.DoScaleHandle(m_currentScale, pivotPosition, handleRotation, HandleUtility.GetHandleSize(pivotPosition));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorSplineUtility.RecordSelection($"Scale Spline Elements ({SplineSelection.Count})");
                    TransformOperation.ApplyScale(m_currentScale);
                }
            }
        }
    }
}
