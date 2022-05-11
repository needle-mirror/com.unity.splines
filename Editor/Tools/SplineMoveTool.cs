using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomEditor(typeof(SplineMoveTool))]
    class SplineMoveToolSettings : SplineToolSettings { }

#if UNITY_2021_2_OR_NEWER
    [EditorTool("Spline Move Tool", typeof(ISplineContainer), typeof(SplineToolContext))]
#else
    [EditorTool("Spline Move Tool", typeof(ISplineContainer))]
#endif
    sealed class SplineMoveTool : SplineTool
    {
        public override bool gridSnapEnabled
        {
            get => handleOrientation == HandleOrientation.Global;
        }

        public override GUIContent toolbarIcon => PathIcons.splineMoveTool;

        public override void OnToolGUI(EditorWindow window)
        {
            switch (Event.current.type)
            {
                case EventType.Layout:
                    TransformOperation.UpdatePivotPosition();
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
                    TransformOperation.UpdatePivotPosition();
                    TransformOperation.UpdateHandleRotation();
                    break;
            }

            if (TransformOperation.canManipulate)
            {
                EditorGUI.BeginChangeCheck();

                var newPos = Handles.DoPositionHandle(TransformOperation.pivotPosition, TransformOperation.handleRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorSplineUtility.RecordSelection($"Move Spline Elements ({SplineSelection.Count})");
                    TransformOperation.ApplyTranslation(newPos - TransformOperation.pivotPosition);

                    if (Tools.pivotMode == PivotMode.Center)
                        TransformOperation.ForcePivotPosition(newPos);
                }
            }
        }
    }
}