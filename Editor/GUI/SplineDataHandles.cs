using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// Provides methods for drawing <see cref="SplineData"/> manipulation handles.
    /// </summary>
    public static class SplineDataHandles
    {
        const float k_HandleSize = 0.15f;
        const int k_PickRes = 2;

        static int[] s_DataPointsIDs;
        
        static int s_NewDataPointIndex = -1;
        static bool s_AddingDataPoint = false;

        static bool m_ShowAddHandle;
        static float3 m_Position;
        static float m_T;
        
        /// <summary>
        /// Draw default manipulation handles that enables adding, removing and moving 
        /// DataPoints of the targeted SplineData along a Spline. Left click on an empty location
        /// on the spline adds a new DataPoint in the SplineData. Left click on an existing DataPoint
        /// allows to move this point along the Spline while a right click on it allows to delete that DataPoint. 
        /// </summary>
        /// <param name="spline">The Spline to use to interpret the SplineData.</param>
        /// <param name="splineData">The SplineData for which the handles are drawn.</param>
        /// <typeparam name="TSpline">The Spline type.</typeparam>
        /// <typeparam name="TData">The type of data this data point stores.</typeparam>
        public static void DataPointHandles<TSpline, TData>(
            this TSpline spline, 
            SplineData<TData> splineData) 
                where TSpline : ISpline
        {
            var evt = Event.current;
            if(evt.type == EventType.MouseMove)
            {
                //Compute distance to spline and closest point
                var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                var distance = SplineUtility.GetNearestPoint(spline, ray, out m_Position, out m_T);
                m_ShowAddHandle = distance < HandleUtility.GetHandleSize(m_Position);
            }
            
            //Id has to be consistent no matter the distance test
            var id = GUIUtility.GetControlID(FocusType.Passive);

            //Only activating the tooling when close enough from the spline
            if(m_ShowAddHandle)
                DataPointAddHandle(id, spline, splineData, m_Position, m_T);

            //Remove DataPoint functionality
            TryRemoveDataPoint(splineData);

            //Draw Default manipulation handles
            DataPointMoveHandles(spline, splineData);
        }

        static void TryRemoveDataPoint<TData>(SplineData<TData> splineData)
        {
            var evt = Event.current;
            //Remove data point only when not adding one and when using right click button
            if(!s_AddingDataPoint && GUIUtility.hotControl == 0 
                && evt.type == EventType.MouseDown && evt.button == 1
                && s_DataPointsIDs.Contains(HandleUtility.nearestControl))
            {
                var dataPointIndex = splineData.Indexes.ElementAt(Array.IndexOf(s_DataPointsIDs, HandleUtility.nearestControl));
                splineData.RemoveDataPoint(dataPointIndex);
                evt.Use();
            }
        }

        static void DataPointAddHandle<TSpline, TData>(
            int controlID, 
            TSpline spline, 
            SplineData<TData> splineData, 
            float3 pos, 
            float t) 
            where TSpline : ISpline
        {
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(controlID);

            switch (eventType)
            {
                case EventType.Layout:
                {
                    if(!Tools.viewToolActive)
                        HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(pos, 0.1f));
                    break;
                }

                case EventType.Repaint:
                    if(HandleUtility.nearestControl == controlID && GUIUtility.hotControl == 0 || s_AddingDataPoint)
                    {
                        var upDir = spline.EvaluateUpVector(t);
                        Handles.CircleHandleCap(controlID, pos, Quaternion.LookRotation(upDir), 0.15f * HandleUtility.GetHandleSize(pos), EventType.Repaint);
                    }
                    break;

                case EventType.MouseDown:
                    if (evt.button == 0
                        && HandleUtility.nearestControl == controlID
                        && GUIUtility.hotControl == 0)
                    {
                        s_AddingDataPoint = true;
                        var index = SplineUtility.ConvertIndexUnit(
                            spline, t,
                            splineData.PathIndexUnit);
                        
                        s_NewDataPointIndex = splineData.AddDataPointWithDefaultValue(index);
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (evt.button == 0 && s_AddingDataPoint)
                    {
                        GUIUtility.hotControl = 0;
                        var index = SplineUtility.ConvertIndexUnit(
                            spline, t,
                            splineData.PathIndexUnit);
                        s_NewDataPointIndex = splineData.MoveDataPoint(s_NewDataPointIndex, index);
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (evt.button == 0 && s_AddingDataPoint)
                    {
                        s_AddingDataPoint = false;
                        s_NewDataPointIndex = -1;
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;

                case EventType.MouseMove:
                    HandleUtility.Repaint();
                    break;
            }
        }

        static void DataPointMoveHandles<TSpline, TData>(TSpline spline, SplineData<TData> splineData) 
                where TSpline : ISpline
        {
            if(s_DataPointsIDs == null || s_DataPointsIDs.Length != splineData.Count)
                s_DataPointsIDs = new int[splineData.Count];
            
            //Cache all data point IDs   
            for(int dataIndex = 0; dataIndex < splineData.Count; dataIndex++)
                s_DataPointsIDs[dataIndex] = GUIUtility.GetControlID(FocusType.Passive);
            
            //Draw all data points handles on the spline
            for(int dataIndex = 0; dataIndex < splineData.Count; dataIndex++)
            {
                var id = GUIUtility.GetControlID(FocusType.Passive);
                
                var index = splineData.Indexes.ElementAt(dataIndex);
                SplineDataHandle(
                    s_DataPointsIDs[dataIndex],
                    spline,
                    splineData, 
                    index, 
                    k_HandleSize,
                    out float newIndex);
        
                if(GUIUtility.hotControl == s_DataPointsIDs[dataIndex])
                {
                    var newDataIndex = splineData.MoveDataPoint(dataIndex, newIndex);
                    //If the current DataPoint is moved across another DataPoint, then update the hotControl ID
                    if(newDataIndex - index != 0)
                        GUIUtility.hotControl = s_DataPointsIDs[newDataIndex];
                }
            }
        }

        static void SplineDataHandle<TSpline, TData>(
            int controlID,
            TSpline spline,
            SplineData<TData> splineData,
            float dataPointIndex,
            float size,
            out float newTime) where TSpline : ISpline
        {
            newTime = dataPointIndex;
            
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(controlID);
    
            var normalizedT = SplineUtility.GetNormalizedInterpolation(spline, dataPointIndex, splineData.PathIndexUnit);
            var dataPosition = SplineUtility.EvaluatePosition(spline, normalizedT);
    
            switch (eventType)
            {
                case EventType.Layout:
                    var dist = HandleUtility.DistanceToCircle(dataPosition, size * HandleUtility.GetHandleSize(dataPosition));
                    HandleUtility.AddControl(controlID, dist);
                    break;
    
                case EventType.Repaint:
                    DrawSplineDataHandle(controlID, dataPosition, size);
                    break;
    
                case EventType.MouseDown:
                    if (evt.button == 0
                        && HandleUtility.nearestControl == controlID
                        && GUIUtility.hotControl == 0)
                    {
                        GUIUtility.hotControl = controlID;
                        newTime = GetClosestSplineDataT(spline, splineData);
                        evt.Use();
                    }
                    break;
    
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        newTime = GetClosestSplineDataT(spline, splineData);
                        evt.Use();
                    }
                    break;
    
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        if(evt.button == 0)
                        {
                            GUIUtility.hotControl = 0;
                            newTime = GetClosestSplineDataT(spline, splineData);
                        }
                        evt.Use();
                    }
                    break;
    
                case EventType.MouseMove:
                    HandleUtility.Repaint();
                    break;
            }
        }

        static void DrawSplineDataHandle(int controlID, Vector3 position, float size)
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
                    size * HandleUtility.GetHandleSize(position),
                    EventType.Repaint
                );
            }
        }
    
        // Spline must be in world space
        static float GetClosestSplineDataT<TSpline,TData>(TSpline spline, SplineData<TData> splineData) where TSpline : ISpline
        {
            var evt = Event.current;
            var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
    
            SplineUtility.GetNearestPoint(spline,
                ray,
                out float3 _,
                out float t,
                k_PickRes);
    
            return SplineUtility.ConvertIndexUnit(spline, t, splineData.PathIndexUnit);
        }
    }
}
