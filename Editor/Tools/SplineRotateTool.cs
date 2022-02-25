using UnityEngine;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomEditor(typeof(SplineRotateTool))]
    class SplineRotateToolSettings : SplineToolSettings { }
    
#if UNITY_2021_2_OR_NEWER
    [EditorTool("Spline Rotate", typeof(ISplineProvider), typeof(SplineToolContext))]
#else
    [EditorTool("Spline Rotate", typeof(ISplineProvider))]
#endif
    sealed class SplineRotateTool : SplineTool
    {
        public override GUIContent toolbarIcon => PathIcons.splineRotateTool;

        internal override SplineHandlesOptions handlesOptions => SplineHandlesOptions.ManipulationDefault;

        Quaternion m_CurrentRotation = Quaternion.identity;
        Vector3 m_RotationCenter = Vector3.zero;
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
                TransformOperation.UpdateHandleRotation();
            }
            
            if (Event.current.type == EventType.Layout)
                TransformOperation.UpdatePivotPosition(true);
            
            if(TransformOperation.canManipulate)
            {
                EditorGUI.BeginChangeCheck();
                var rotation = Handles.DoRotationHandle(m_CurrentRotation, m_RotationCenter);
                if(EditorGUI.EndChangeCheck())
                {
                    TransformOperation.ApplyRotation(rotation * Quaternion.Inverse(m_CurrentRotation), m_RotationCenter);
                    m_CurrentRotation = rotation;
                }

                if(GUIUtility.hotControl == 0)
                {
                    TransformOperation.UpdateHandleRotation();
                    m_CurrentRotation = TransformOperation.handleRotation;
                    m_RotationCenter = TransformOperation.pivotPosition;
                }
            }

            SplineConversionUtility.ApplyEditableSplinesIfDirty(targets);
        }
    }
}
