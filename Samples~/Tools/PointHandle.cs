using UnityEditor;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace Unity.Splines.Examples
{
    [CustomSplineDataHandle(typeof(PointHandleAttribute))]
    public class PointHandle : SplineDataHandle<float2>
    {
        const float k_HandleSize = 0.2f;

        public override void DrawDataPoint(int controlID, Vector3 position, Vector3 direction, Vector3 upDirection, SplineData<float2> splineData, int dataPointIndex)
        {
            var handleColor = Handles.color;
            if(GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlID)
                handleColor = Handles.preselectionColor;
            
            var pointData = splineData[dataPointIndex];
            var pointValue = new float3(pointData.Value.x, 0f, pointData.Value.y);
            
            var size = k_HandleSize * HandleUtility.GetHandleSize(pointValue);

            using (new Handles.DrawingScope(handleColor))
            {
                EditorGUI.BeginChangeCheck();
                Handles.DrawLine(position, pointValue);
                
                var newPointValue = (float3)Handles.Slider2D(controlID, pointValue, -Vector3.up, Vector3.right, Vector3.forward, size, Handles.ConeHandleCap, Vector2.zero, true); 
                if (EditorGUI.EndChangeCheck())
                {
                    var delta = newPointValue - pointValue;
                    pointData.Value  += new float2(delta.x, delta.z);
                    splineData[dataPointIndex] = pointData;
                }
            }
        }
    }
}
