using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Interpolators = UnityEngine.Splines.Interpolators;

namespace Unity.Splines.Examples
{
    [CustomSplineDataHandle(typeof(BorderHandleAttribute))]
    public class BorderHandle : SplineDataHandle<float>
    {
        const float k_HandleSize = 0.15f;
        const float k_HandleOffset = 2f;
        const float k_LineLengthsSize = 4f;
        const int k_SamplesPerCurve = 15;
        
        static List<Vector3> s_LineSegments = new List<Vector3>();

        public override void DrawSplineData(SplineData<float> splineData, Spline spline, Matrix4x4 localToWorld, Color color)
        {
            if (GUIUtility.hotControl == 0 || ((IList)controlIDs).Contains(GUIUtility.hotControl))
            {
                s_LineSegments.Clear();

                var curveCount = spline.Closed ? spline.KnotCount : spline.KnotCount - 1;
                var stepSize = 1f / k_SamplesPerCurve;
                var prevBorderPos = Vector3.zero;

                for (int curveIndex = 0; curveIndex < curveCount; ++curveIndex)
                {
                    for (int step = 0; step < k_SamplesPerCurve; ++step)
                    {
                        var splineTime = spline.CurveToSplineInterpolation(curveIndex + step * stepSize);
                        spline.Evaluate(splineTime, out var position, out var tangent, out var upVector);
                        var right = math.cross(math.normalize(tangent), upVector);
                        var border = splineData.Evaluate(spline, splineTime, PathIndexUnit.Normalized, new Interpolators.LerpFloat());
                        var borderPos = position + right * border;
                        borderPos += (float3) GetBorderHandleOffset(borderPos);
                        if (curveIndex > 0 || step > 0)
                        {
                            s_LineSegments.Add(prevBorderPos);
                            s_LineSegments.Add(borderPos);
                        }

                        prevBorderPos = borderPos;
                    }
                }

                using (new Handles.DrawingScope(color, localToWorld))
                    Handles.DrawDottedLines(s_LineSegments.ToArray(), k_LineLengthsSize);
            }
        }

        public override void DrawDataPoint(int controlID, Vector3 position, Vector3 direction, Vector3 upDirection, SplineData<float> splineData, int dataPointIndex)
        {
            var handleColor = Handles.color;
            if(GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlID)
                handleColor = Handles.preselectionColor;
            
            var right = Vector3.Cross(direction.normalized, upDirection.normalized);
            var borderData = splineData[dataPointIndex];
            var borderPos = position + right * borderData.Value;
            
            var size = k_HandleSize * HandleUtility.GetHandleSize(borderPos);
            var sliderOffset = GetBorderHandleOffset(borderPos);

            var handleMatrix = Handles.matrix * Matrix4x4.TRS(sliderOffset, Quaternion.identity, Vector3.one);
            using (new Handles.DrawingScope(handleColor, handleMatrix))
            {
                EditorGUI.BeginChangeCheck();
                var newBorderPos = Handles.Slider(controlID, borderPos, right, size, Handles.RectangleHandleCap, 0f);
                if (EditorGUI.EndChangeCheck())
                {
                    var delta = newBorderPos - borderPos;

                    borderData.Value = Mathf.Max(0f, borderData.Value + (Vector3.Dot(right, delta) > 0 ? delta.magnitude : -delta.magnitude));
                    splineData[dataPointIndex] = borderData;
                }
            }
        }

        Vector3 GetBorderHandleOffset(Vector3 borderPos)
        {
            var size = k_HandleSize * HandleUtility.GetHandleSize(borderPos);
            return Vector3.up * size * k_HandleOffset;
        }
    }
}
