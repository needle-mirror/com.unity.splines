using System;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    public static class SplineDataHandles
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

        const float k_HandleSize = 0.25f;
        //readonly static Vector3 s_ArrowDirection = 1.15f * k_HandleSize * Vector3.up;

        const int k_PickRes = 2;

        internal static void InitCustomHandles<T>(
            SplineData<T> splineData, 
            object drawerInstance)
        {
            var ids = new int[splineData.Count];
            for(int kfIndex = 0; kfIndex < splineData.Count; kfIndex++)
                ids[kfIndex] = GUIUtility.GetControlID(FocusType.Passive);
            
            ( (SplineDataDrawer<T>)drawerInstance ).controlIDs = ids;
        }
        
        internal static void DrawCustomKeyframeHandles<T>(
            SplineData<T> splineData, 
            Spline spline, 
            Matrix4x4 localToWorld, 
            Color color,
            object drawerInstance,
            MethodInfo keyframeDrawMethodInfo)
        {
            var ids = ( (SplineDataDrawer<T>)drawerInstance ).controlIDs;
            
            using(var nativeSpline = spline.ToNativeSpline())
            for(int keyframeIndex = 0; keyframeIndex < splineData.Count; keyframeIndex++)
            {
                var keyframe = splineData[keyframeIndex];
                var normalizedT = SplineUtility.GetNormalizedTime(nativeSpline, keyframe.Time, splineData.PathIndexUnit);
                var dataPosition = nativeSpline.EvaluatePosition(normalizedT);
                var dataDirection = SplineUtility.EvaluateDirection(nativeSpline, normalizedT);
                var dataUp = SplineUtility.EvaluateUpVector(nativeSpline, normalizedT);
                using(new Handles.DrawingScope(color, localToWorld))
                {
                    keyframeDrawMethodInfo?.Invoke(drawerInstance,
                        new object[]
                        {
                            ids[keyframeIndex],
                            (Vector3)dataPosition,
                            (Vector3)dataDirection,
                            (Vector3)dataUp,
                            splineData,
                            keyframeIndex
                        });
                }
            }
        }
        
        internal static void DrawSplineDataHandles<T>(
            SplineData<T> splineData, 
            Spline spline, 
            Matrix4x4 localToWorld, 
            Color color, 
            LabelType labelType)
        {
            using(var nativeSpline = spline.ToNativeSpline(localToWorld))
            using(new Handles.DrawingScope(color))
            {
                for(int keyframeIndex = 0; keyframeIndex < splineData.Count; keyframeIndex++)
                {
                    var keyframe = splineData[keyframeIndex];
                    var inUse = SplineDataHandle(splineData, keyframe, nativeSpline, labelType, keyframeIndex, out float time);
                
                    if(inUse)
                    {
                        keyframe.Time = time;
                        splineData.SetKeyframeNoSort(keyframeIndex, keyframe);
                        
                        //OnMouseUp event
                        if(GUIUtility.hotControl == 0)
                            splineData.SortIfNecessary();
                    }
                }
            }
        }
        
        internal static bool SplineDataHandle<T>(
            SplineData<T> splineData, 
            IKeyframe keyframe,
            NativeSpline nativeSpline,
            LabelType labelType,
            int keyframeIndex,
            out float newTime)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(id);
            
            var normalizedT = SplineUtility.GetNormalizedTime(nativeSpline, keyframe.Time, splineData.PathIndexUnit);
            var dataPosition = SplineUtility.EvaluatePosition(nativeSpline, normalizedT);
            var dataUp = SplineUtility.EvaluateUpVector(nativeSpline, normalizedT);

            switch (eventType)
            {
                case EventType.Layout:
                {
                    if(!Tools.viewToolActive)
                    {
                        var dist = 0.5f * HandleUtility.DistanceToLine(dataPosition, (Vector3)dataPosition + (Vector3)(1.15f * k_HandleSize * dataUp));
                        HandleUtility.AddControl(id, dist);
                    }

                    break;
                }
                
                case EventType.Repaint:
                    DrawSplineDataHandle(dataPosition, dataUp, id);
                    DrawSplineDataLabel(dataPosition, labelType, keyframe, keyframeIndex);
                    break;
                
                case EventType.MouseDown:
                    if (evt.button == 0
                        && HandleUtility.nearestControl == id 
                        && GUIUtility.hotControl == 0)
                    {
                        GUIUtility.hotControl = id;
                            
                        evt.Use();
                        newTime = GetClosestSplineDataTime(nativeSpline, splineData);
                        return true;
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        evt.Use();
                        newTime = GetClosestSplineDataTime(nativeSpline, splineData);
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
                            newTime = GetClosestSplineDataTime(nativeSpline, splineData);
                            return true;
                        }
                    }
                    break;

                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint();
                    break;
            }

            newTime = keyframe.Time;
            return false;
        }

        static void DrawSplineDataHandle(Vector3 position, Vector3 up, int controlID)
        {
            var handleColor = Handles.color;
            if(controlID == GUIUtility.hotControl)
                handleColor = Handles.selectedColor;
            else if(GUIUtility.hotControl == 0 && controlID == HandleUtility.nearestControl)
                handleColor = Handles.preselectionColor;

            using(new Handles.DrawingScope(handleColor))
            {
                Handles.ArrowHandleCap(
                    controlID,
                    position + (1.15f * k_HandleSize * up),
                    Quaternion.LookRotation(-up),
                    k_HandleSize,
                    EventType.Repaint);
            }
        }

        static void DrawSplineDataLabel(Vector3 position, LabelType labelType, IKeyframe keyframe, int keyframeIndex)
        {
            if(labelType == LabelType.None)
                return;
            
            float labelVal = keyframe.Time;
            if(labelType == LabelType.Index && keyframeIndex >= 0)
                labelVal = keyframeIndex;
            
            var label = ( Mathf.RoundToInt(labelVal * 100) / 100f ).ToString();
            label = labelType == LabelType.Index ? "[" + label + "]" : "t: "+label;
            Handles.Label(position - 0.1f * Vector3.up, label);
        }

        static float GetClosestSplineDataTime<T>(NativeSpline nativeSpline, SplineData<T> splineData)
        {
            var evt = Event.current;
            SplineUtility.GetNearestPoint(nativeSpline,
                HandleUtility.GUIPointToWorldRay(evt.mousePosition),
                out float3 _,
                out float t,
                k_PickRes);

            var time = SplineUtility.GetConvertedTime(
                nativeSpline,
                t, 
                PathIndexUnit.Normalized, 
                splineData.PathIndexUnit);
            
            return time;
        }
        
    }
}