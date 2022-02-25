using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomEditor(typeof(SplineScaleTool))]
    class SplineScaleToolSettings : SplineToolSettings { }
    
#if UNITY_2021_2_OR_NEWER
    [EditorTool("Spline Scale Tool", typeof(ISplineProvider), typeof(SplineToolContext))]
#else
    [EditorTool("Spline Scale Tool", typeof(ISplineProvider))]
#endif
    sealed class SplineScaleTool : SplineTool
    {
        public override GUIContent toolbarIcon => PathIcons.splineScaleTool;

        internal override SplineHandlesOptions handlesOptions => SplineHandlesOptions.ManipulationDefault;

        Vector3 m_currentScale = Vector3.one;

        public override void OnToolGUI(EditorWindow window)
        {
            if (Event.current.type == EventType.MouseDrag)
            {
                if (handleOrientation == HandleOrientation.Element || handleOrientation == HandleOrientation.Parent)
                    TransformOperation.pivotFreeze |= TransformOperation.PivotFreeze.Rotation;
            }
            
            if (Event.current.type == EventType.Layout)
                TransformOperation.UpdatePivotPosition(true);
            
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
                TransformOperation.UpdateHandleRotation();
            }

            if(TransformOperation.canManipulate)
            {
                EditorGUI.BeginChangeCheck();
                m_currentScale = Handles.DoScaleHandle(m_currentScale, TransformOperation.pivotPosition,
                    TransformOperation.handleRotation, HandleUtility.GetHandleSize(TransformOperation.pivotPosition));
                if(EditorGUI.EndChangeCheck())
                    TransformOperation.ApplyScale(m_currentScale);
            }
            SplineConversionUtility.ApplyEditableSplinesIfDirty(targets);
        }
    }
}
