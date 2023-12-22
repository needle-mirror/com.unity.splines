using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomEditor(typeof(SplineMoveTool))]
    class SplineMoveToolSettings : SplineToolSettings { }

    /// <summary>
    /// Provides methods that move knots and tangents in the Scene view. This tool is only available when you use SplineToolContext.
    /// `SplineMoveTool` works similarly to the Move tool for GameObjects, except that it has extra handle configurations according to the `handleOrientation` settings.
    /// `SplineToolContext` manages the selection of knots and tangents. You can manipulate the selection of knots and tangents with `SplineMoveTool`. 
    /// </summary>
#if UNITY_2021_2_OR_NEWER
    [EditorTool("Spline Move Tool", typeof(ISplineContainer), typeof(SplineToolContext))]
#else
    [EditorTool("Spline Move Tool", typeof(ISplineContainer))]
#endif
    public sealed class SplineMoveTool : SplineTool
    {
        /// <inheritdoc />
        public override bool gridSnapEnabled
        {
            get => handleOrientation == HandleOrientation.Global;
        }
        
        /// <inheritdoc />
        public override GUIContent toolbarIcon => PathIcons.splineMoveTool;

        /// <inheritdoc />
        public override void OnToolGUI(EditorWindow window)
        {
            switch (Event.current.type)
            {
                case EventType.Layout:
                    UpdatePivotPosition();
                    break;

                case EventType.MouseDrag:
                    if (handleOrientation == HandleOrientation.Element || handleOrientation == HandleOrientation.Parent)
                        TransformOperation.pivotFreeze |= TransformOperation.PivotFreeze.Rotation;

                    // In rotation sync center mode, pivot has to be allowed to move away
                    // from the selection center. Therefore we freeze pivot's position
                    // and force the position later on based on handle's translation delta.
                    if (Tools.pivotMode == PivotMode.Center)
                        TransformOperation.pivotFreeze |= TransformOperation.PivotFreeze.Position;
                    break;

                case EventType.MouseUp:
                    TransformOperation.pivotFreeze = TransformOperation.PivotFreeze.None;
                    UpdatePivotPosition();
                    UpdateHandleRotation();
                    break;
            }

            if (TransformOperation.canManipulate && !DirectManipulation.IsDragging)
            {
                EditorGUI.BeginChangeCheck();

                var newPos = Handles.DoPositionHandle(pivotPosition, handleRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorSplineUtility.RecordSelection($"Move Spline Elements ({SplineSelection.Count})");
                    TransformOperation.ApplyTranslation(newPos - pivotPosition);

                    if (Tools.pivotMode == PivotMode.Center)
                        TransformOperation.ForcePivotPosition(newPos);
                }
            }
        }
    }
}
