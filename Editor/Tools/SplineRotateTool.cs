using UnityEngine;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomEditor(typeof(SplineRotateTool))]
    class SplineRotateToolSettings : SplineToolSettings { }

    /// <summary>
    /// Provides methods to rotate knots and tangents in the Scene view. This tool is only available when you use SplineToolContext.
    /// `SplineRotateTool` is similar to the Rotate tool for GameObjects except that it has extra handle configurations according to the `handleOrientation` settings.
    /// The rotation of tangents are usually related to the rotation of their knots, except when tangents use the Broken Bezier tangent mode. The rotation of tangents that use the Broken Bezier tangent mode are independent from the rotation of their knot. 
    /// `SplineToolContext` manages the selection of knots and tangents. You can manipulate the selection of knots and tangents with `SplineRotateTool`. 
    /// </summary>
#if UNITY_2021_2_OR_NEWER
    [EditorTool("Spline Rotate", typeof(ISplineContainer), typeof(SplineToolContext))]
#else
    [EditorTool("Spline Rotate", typeof(ISplineContainer))]
#endif
    public sealed class SplineRotateTool : SplineTool
    {
        /// <inheritdoc />
        public override GUIContent toolbarIcon => PathIcons.splineRotateTool;

        Quaternion m_CurrentRotation = Quaternion.identity;
        Vector3 m_RotationCenter = Vector3.zero;
        
        /// <inheritdoc />
        public override void OnToolGUI(EditorWindow window)
        {
            if (Event.current.type == EventType.MouseDrag)
            {
                if (handleOrientation == HandleOrientation.Element || handleOrientation == HandleOrientation.Parent)
                    TransformOperation.pivotFreeze |= TransformOperation.PivotFreeze.Rotation;
            }

            if (Event.current.type == EventType.MouseUp)
            {
                TransformOperation.pivotFreeze = TransformOperation.PivotFreeze.None;
                UpdateHandleRotation();
            }

            if (Event.current.type == EventType.Layout)
                UpdatePivotPosition(true);

            if(TransformOperation.canManipulate && !DirectManipulation.IsDragging)
            {
                EditorGUI.BeginChangeCheck();
                var rotation = Handles.DoRotationHandle(m_CurrentRotation, m_RotationCenter);
                if(EditorGUI.EndChangeCheck())
                {
                    EditorSplineUtility.RecordSelection($"Rotate Spline Elements ({SplineSelection.Count})");
                    TransformOperation.ApplyRotation(rotation * Quaternion.Inverse(m_CurrentRotation), m_RotationCenter);
                    m_CurrentRotation = rotation;
                }

                if(GUIUtility.hotControl == 0)
                {
                    UpdateHandleRotation();
                    m_CurrentRotation = handleRotation;
                    m_RotationCenter = pivotPosition;
                }
            }
        }
    }
}
