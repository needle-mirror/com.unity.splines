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
    [EditorTool("Spline Move Tool", typeof(ISplineProvider), typeof(SplineToolContext))]
#else
    [EditorTool("Spline Move Tool", typeof(ISplineProvider))]
#endif
    sealed class SplineMoveTool : SplineTool
    {
        public override bool gridSnapEnabled
        {
            get => handleOrientation == HandleOrientation.Global; 
        }

        public override GUIContent toolbarIcon => PathIcons.splineMoveTool;
        
        internal override SplineHandlesOptions handlesOptions => SplineHandlesOptions.ManipulationDefault;
        
        public override void OnToolGUI(EditorWindow window)
        {
            if (Event.current.type == EventType.MouseDrag)
            {
                if (handleOrientation == HandleOrientation.Element || handleOrientation == HandleOrientation.Parent)
                    TransformOperation.pivotFreeze |= TransformOperation.PivotFreeze.Rotation;

                // In rotation sync center mode, pivot has to be allowed to move away
                // from the selection center. Therefore we freeze pivot's position 
                // and force the position later on based on handle's translation delta.
                if (Tools.pivotMode == PivotMode.Center)
                    TransformOperation.pivotFreeze |= TransformOperation.PivotFreeze.Position;
            }

            if (Event.current.type == EventType.MouseUp)
            {
                TransformOperation.pivotFreeze = TransformOperation.PivotFreeze.None;
                TransformOperation.UpdatePivotPosition();
                TransformOperation.UpdateHandleRotation();
            }

            if (Event.current.type == EventType.Layout)
                TransformOperation.UpdatePivotPosition();

            if (TransformOperation.canManipulate)
            {
                EditorGUI.BeginChangeCheck();
                
                var newPos = Handles.DoPositionHandle(TransformOperation.pivotPosition, TransformOperation.handleRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    TransformOperation.ApplyTranslation(newPos - TransformOperation.pivotPosition);
                    
                    if (Tools.pivotMode == PivotMode.Center)
                        TransformOperation.ForcePivotPosition(newPos);
                }
            }

            SplineConversionUtility.ApplyEditableSplinesIfDirty(targets);
        }
    }
}
