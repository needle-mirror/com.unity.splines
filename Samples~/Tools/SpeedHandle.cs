using System.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

using Interpolators = UnityEngine.Splines.Interpolators;

namespace Unity.Splines.Examples
{
    public class SpeedHandle  : SplineDataDrawer<float>
     {
         const float k_HandleSize = 0.1f;
         const float k_SpeedScaleFactor = 10f;
         
         static float s_DisplaySpace = 0.2f;
         
         public override void DrawSplineData(
             SplineData<float> splineData,
             Spline spline,
             Matrix4x4 localToWorld,
             Color color)
         {
             using(var nativeSpline = spline.ToNativeSpline(localToWorld))
             {
                 if(GUIUtility.hotControl == 0 || ( (IList)controlIDs ).Contains(GUIUtility.hotControl))
                 {
                     var data = splineData.Evaluate(nativeSpline, 0, PathIndexUnit.Distance, new Interpolators.LerpFloat());
                     var position = nativeSpline.EvaluatePosition(0);
                     var previousExtremity = (Vector3)position + ( data / k_SpeedScaleFactor ) * Vector3.up;

                     var currentOffset = s_DisplaySpace;
                     while(currentOffset < nativeSpline.Length)
                     {
                         var t = currentOffset / nativeSpline.Length;
                         position = nativeSpline.EvaluatePosition(t);
                         data = splineData.Evaluate(nativeSpline, currentOffset, PathIndexUnit.Distance, new Interpolators.LerpFloat());

                         var extremity = (Vector3)position + ( data / k_SpeedScaleFactor ) * Vector3.up;

                         using(new Handles.DrawingScope(color))
                             Handles.DrawLine(previousExtremity, extremity);

                         currentOffset += s_DisplaySpace;
                         previousExtremity = extremity;
                     }

                     position = nativeSpline.EvaluatePosition(1);
                     data = splineData.Evaluate(nativeSpline, nativeSpline.Length, PathIndexUnit.Distance, new Interpolators.LerpFloat());

                     var lastExtremity = (Vector3)position + ( data / k_SpeedScaleFactor ) * Vector3.up;

                     using(new Handles.DrawingScope(color))
                         Handles.DrawLine(previousExtremity, lastExtremity);
                 }
             }
         }
                  
         public override void DrawKeyframe(
             int controlID, 
             Vector3 position, 
             Vector3 direction,
             Vector3 upDirection,
             SplineData<float> splineData, 
             int keyframeIndex)
         {
             var handleColor = Handles.color;
             if(GUIUtility.hotControl == controlID)
                 handleColor = Handles.selectedColor;
             else if(GUIUtility.hotControl == 0 && HandleUtility.nearestControl==controlID)
                 handleColor = Handles.preselectionColor;
             
             var keyframe = splineData[keyframeIndex];
             
             var extremity = position + (keyframe.Value / k_SpeedScaleFactor) * Vector3.up;
             using(new Handles.DrawingScope(handleColor))
             {
                 var size = k_HandleSize * HandleUtility.GetHandleSize(position);
                 Handles.DrawLine(position, extremity);
                 var val = Handles.Slider(controlID, extremity, Vector3.up, size, Handles.SphereHandleCap, 0);
                 Handles.Label(extremity + 2f * size * Vector3.up, keyframe.Value.ToString());
                 
                 if(GUIUtility.hotControl == controlID)
                 {
                     if(Mathf.Abs((val - position).magnitude - keyframe.Value) > 0)
                         keyframe.Value = Mathf.Clamp(k_SpeedScaleFactor * Mathf.Abs((val - position).magnitude), 0.01f, 100f);
                     splineData[keyframeIndex] = keyframe;
                 }
             }
         }
     }
}
