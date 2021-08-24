using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

using Interpolators = UnityEngine.Splines.Interpolators;

namespace Unity.Splines.Examples
{
    public class CustomUpVectorHandle : SplineDataDrawer<float3>
    {
        static float s_DisplaySpace = 0.5f;

        static Quaternion s_StartingRotation;

        public override void DrawSplineData(
            SplineData<float3> splineData,
            Spline spline,
            Matrix4x4 localToWorld,
            Color color)
        {
            using(var nativeSpline = spline.ToNativeSpline(localToWorld, Allocator.Temp))
            {
                if(GUIUtility.hotControl == 0 || controlIDs.Contains(GUIUtility.hotControl))
                {
                    var currentOffset = s_DisplaySpace;
                    while(currentOffset < nativeSpline.GetLength())
                    {
                        var t = currentOffset / nativeSpline.GetLength();
                        var position = nativeSpline.EvaluatePosition(t);
                        var direction = SplineUtility.EvaluateDirection(nativeSpline, t);
                        var up = SplineUtility.EvaluateUpVector(nativeSpline, t);
                        var data = splineData.Evaluate(nativeSpline, t, PathIndexUnit.Normalized,
                            new Interpolators.LerpFloat3());

                        Matrix4x4 localMatrix = Matrix4x4.identity;
                        localMatrix.SetTRS(position, Quaternion.LookRotation(direction, up), Vector3.one);
                        using(new Handles.DrawingScope(color, localMatrix))
                            Handles.DrawLine(Vector3.zero, math.normalize(data));

                        currentOffset += s_DisplaySpace;
                    }
                }
            }
        }

        public override void DrawKeyframe(
            int controlID, 
            Vector3 position, 
            Vector3 direction,
            Vector3 upDirection,
            SplineData<float3> splineData, 
            int keyframeIndex)
        {           
            var keyframe = splineData[keyframeIndex];
            
            Matrix4x4 localMatrix = Matrix4x4.identity;
            localMatrix.SetTRS(position, Quaternion.LookRotation(direction, upDirection), Vector3.one);

            var matrix = Handles.matrix * localMatrix;
            using(new Handles.DrawingScope(matrix))
            {
                var keyframeRotation = Quaternion.FromToRotation(Vector3.up, keyframe.Value);

                if(GUIUtility.hotControl == 0)
                    s_StartingRotation = keyframeRotation;

                Handles.ArrowHandleCap(-1, Vector3.zero, Quaternion.FromToRotation(Vector3.forward, keyframe.Value), 1 / 1.15f, EventType.Repaint);
                var rotation = Handles.Disc(controlID, keyframeRotation, Vector3.zero, Vector3.forward, 1, false, 0);
                
                 if(GUIUtility.hotControl == controlID)
                 {
                      var deltaRot = Quaternion.Inverse(s_StartingRotation) * rotation;
                      keyframe.Value = deltaRot * s_StartingRotation * Vector3.up;
                      splineData[keyframeIndex] = keyframe;
                 }
            }
        }
    }
}
