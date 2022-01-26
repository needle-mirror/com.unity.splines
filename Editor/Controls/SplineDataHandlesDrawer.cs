using System;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class SplineDataHandlesDrawer
    {
        /// <summary>
        /// LabelType used to define if label must be displayed along the handle and how it should be formatted
        /// </summary>
        internal enum LabelType
        {
            None,
            Index,
            Time
        };

        const float k_HandleSize = 0.15f;

        const int k_PickRes = 2;

        internal static void InitCustomHandles<T>(
            SplineData<T> splineData,
            ISplineDataHandle drawerInstance)
        {
            var ids = new int[splineData.Count];
            for(int kfIndex = 0; kfIndex < splineData.Count; kfIndex++)
                ids[kfIndex] = GUIUtility.GetControlID(FocusType.Passive);

            ( (SplineDataHandle<T>)drawerInstance ).m_ControlIDs = ids;
        }

        internal static void DrawCustomHandles<T>(
            SplineData<T> splineData,
            Component component,
            Spline spline,
            Matrix4x4 localToWorld,
            Color color,
            ISplineDataHandle drawerInstance,
            MethodInfo splineDataDrawMethodInfo,
            MethodInfo keyframeDrawMethodInfo)
        {
            if(splineData.Count == 0)
                return;
            
            Undo.RecordObject(component, "Modifying Spline Data Points");
            
            //Invoke if existing the custom drawer for the whole spline
            splineDataDrawMethodInfo?.Invoke(drawerInstance,
                new object[]
                {
                    splineData,
                    spline,
                    localToWorld,
                    color
                });
            
            if (keyframeDrawMethodInfo == null)
                return;
            
            var native = new NativeSpline(spline, localToWorld);
            var ids = ( (SplineDataHandle<T>)drawerInstance ).controlIDs;
            
            for(int splineDataIndex = 0; splineDataIndex < splineData.Count; splineDataIndex++)
            {
                var keyframe = splineData[splineDataIndex];
                var normalizedT = SplineUtility.GetNormalizedInterpolation(native, keyframe.Index, splineData.PathIndexUnit);
            
                native.Evaluate(normalizedT, out float3 dataPosition, out float3 dataDirection, out float3 dataUp);
                using(new Handles.DrawingScope(color))
                {
                    //Invoke if existing the custom drawer for each keyframe
                    keyframeDrawMethodInfo?.Invoke(drawerInstance,
                        new object[]
                        {
                            ids[splineDataIndex],
                            (Vector3)dataPosition,
                            (Vector3)dataDirection,
                            (Vector3)dataUp,
                            splineData,
                            splineDataIndex
                        });
                }
            }
            
            native.Dispose();
        }

        internal static void DrawSplineDataHandles<T>(
            SplineData<T> splineData,
            Component component,
            Spline spline,
            Matrix4x4 localToWorld,
            Color color,
            LabelType labelType)
        {
            Undo.RecordObject(component, "Modifying Spline Data Points");
            using var native = new NativeSpline(spline, localToWorld);
            
            using(new Handles.DrawingScope(color))
            {
                for(int keyframeIndex = 0; keyframeIndex < splineData.Count; keyframeIndex++)
                {
                    var keyframe = splineData[keyframeIndex];
                    var inUse = SplineDataHandle(splineData, keyframe, native, labelType, keyframeIndex, out float time);
            
                    if(inUse)
                    {
                        keyframe.Index = time;
                        splineData.SetDataPointNoSort(keyframeIndex, keyframe);
            
                        //OnMouseUp event
                        if(GUIUtility.hotControl == 0)
                            splineData.SortIfNecessary();
                    }
                }
            }
        }

        internal static bool SplineDataHandle<T>(
            SplineData<T> splineData,
            IDataPoint dataPoint,
            NativeSpline spline,
            LabelType labelType,
            int keyframeIndex,
            out float newTime)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(id);

            var normalizedT = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, splineData.PathIndexUnit);
            var dataPosition = SplineUtility.EvaluatePosition(spline, normalizedT);

            switch (eventType)
            {
                case EventType.Layout:
                {
                    if(!Tools.viewToolActive)
                    {
                        var dist = HandleUtility.DistanceToCircle(dataPosition, k_HandleSize * HandleUtility.GetHandleSize(dataPosition));
                        HandleUtility.AddControl(id, dist);
                    }
                    break;
                }

                case EventType.Repaint:
                    DrawSplineDataHandle(dataPosition, id);
                    DrawSplineDataLabel(dataPosition, labelType, dataPoint, keyframeIndex);
                    break;

                case EventType.MouseDown:
                    if (evt.button == 0
                        && HandleUtility.nearestControl == id
                        && GUIUtility.hotControl == 0)
                    {
                        GUIUtility.hotControl = id;

                        evt.Use();
                        newTime = GetClosestSplineDataT(spline, splineData);
                        return true;
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        evt.Use();
                        newTime = GetClosestSplineDataT(spline, splineData);
                        return true;
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        evt.Use();
                        if(evt.button == 0)
                        {
                            GUIUtility.hotControl = 0;
                            newTime = GetClosestSplineDataT(spline, splineData);
                            return true;
                        }
                    }
                    break;

                case EventType.MouseMove:
                        HandleUtility.Repaint();

                    break;
            }

            newTime = dataPoint.Index;
            return false;
        }

        static void DrawSplineDataHandle(Vector3 position, int controlID)
        {
            var handleColor = Handles.color;
            if(controlID == GUIUtility.hotControl)
                handleColor = Handles.selectedColor;
            else if(GUIUtility.hotControl == 0 && controlID == HandleUtility.nearestControl)
                handleColor = Handles.preselectionColor;

            // to avoid affecting the sphere dimensions with the handles matrix, we'll just use the position and reset
            // the matrix to identity when drawing.
            position = Handles.matrix * position;

            using(new Handles.DrawingScope(handleColor, Matrix4x4.identity))
            {
                Handles.SphereHandleCap(
                    controlID,
                    position,
                    Quaternion.identity,
                    k_HandleSize * HandleUtility.GetHandleSize(position),
                    EventType.Repaint
                );
            }
        }

        static void DrawSplineDataLabel(Vector3 position, LabelType labelType, IDataPoint dataPoint, int keyframeIndex)
        {
            if(labelType == LabelType.None)
                return;

            float labelVal = dataPoint.Index;
            if(labelType == LabelType.Index && keyframeIndex >= 0)
                labelVal = keyframeIndex;

            var label = ( Mathf.RoundToInt(labelVal * 100) / 100f ).ToString();
            label = labelType == LabelType.Index ? "[" + label + "]" : "t: "+label;
            Handles.Label(position - 0.1f * Vector3.up, label);
        }

        // Spline must be in world space
        static float GetClosestSplineDataT<T>(NativeSpline spline, SplineData<T> splineData)
        {
            var evt = Event.current;
            var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);

            SplineUtility.GetNearestPoint(spline,
                ray,
                out float3 _,
                out float t,
                k_PickRes);

            var time = SplineUtility.ConvertIndexUnit(
                spline,
                t,
                PathIndexUnit.Normalized,
                splineData.PathIndexUnit);

            return time;
        }

    }
}
