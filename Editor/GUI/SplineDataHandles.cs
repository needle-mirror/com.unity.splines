using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// Provides default handles to SplineData.
    /// Call <see cref="DataPointHandles"/> in your Editor Tool to add default handles
    /// for you to add, move, and remove SplineData's DataPoints along a spline.
    /// </summary>
    public static class SplineDataHandles
    {
        const float k_HandleSize = 0.15f;
        const int k_PickRes = 2;

        static int[] s_DataPointsIDs;

        static int s_NewDataPointIndex = -1;
        static float s_AddingDataPoint = float.NaN;

        /// <summary>
        /// Creates manipulation handles in the SceneView to add, move, and remove SplineData's DataPoints along a spline.
        /// DataPoints of the targeted SplineData along a Spline. Left click on an empty location
        /// on the spline adds a new DataPoint in the SplineData. Left click on an existing DataPoint
        /// allows to move this point along the Spline while a right click on it allows to delete that DataPoint.
        /// </summary>
        /// <description>
        /// Left-click an empty location on the spline to add a new DataPoint to the SplineData.
        /// Left-click on a DataPoint to move the point along the Spline. Right-click a DataPoint to delete it.
        /// </description>
        /// <param name="spline">The Spline to use to interprete the SplineData.</param>
        /// <param name="splineData">The SplineData for which the handles are drawn.</param>
        /// <param name="useDefaultValueOnAdd">Either to use default value or closer DataPoint value when adding new DataPoint.</param>
        /// <typeparam name="TSpline">The Spline type.</typeparam>
        /// <typeparam name="TData">The type of data this data point stores.</typeparam>
        public static void DataPointHandles<TSpline, TData>(
            this TSpline spline,
            SplineData<TData> splineData,
            bool useDefaultValueOnAdd = false)
                where TSpline : ISpline
        {
            spline.DataPointHandles(splineData, useDefaultValueOnAdd, 0);
        }

        /// <summary>
        /// Creates manipulation handles in the Scene view that can be used to add, move, and remove SplineData's DataPoints along a spline.
        /// </summary>
        /// <description>
        /// Left-click an empty location on the spline to add a new DataPoint to the SplineData.
        /// Left-click and drag a DataPoint to move the point along the spline. Right-click a DataPoint to delete it.
        /// </description>
        /// <param name="spline">The spline to use to interprete the SplineData.</param>
        /// <param name="splineData">The SplineData for which the handles are drawn.</param>
        /// <param name="splineID">The ID for the spline.</param>
        /// <param name="useDefaultValueOnAdd">Whether to use the default value or a closer DataPoint value when adding new DataPoint.</param>
        /// <typeparam name="TSpline">The spline type.</typeparam>
        /// <typeparam name="TData">The type of data this data point stores.</typeparam>
        public static void DataPointHandles<TSpline, TData>(
            this TSpline spline,
            SplineData<TData> splineData,
            bool useDefaultValueOnAdd,
            int splineID = 0)
                where TSpline : ISpline
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);

            DataPointAddHandle(id, spline, splineData, useDefaultValueOnAdd, splineID);

            // Draw Default manipulation handles
            DataPointMoveHandles(spline, splineData);

            // Remove DataPoint functionality
            TryRemoveDataPoint(splineData);
        }


        static void TryRemoveDataPoint<TData>(SplineData<TData> splineData)
        {
            var evt = Event.current;
            //Remove data point only when not adding one and when using right click button
            if(float.IsNaN(s_AddingDataPoint) && GUIUtility.hotControl == 0
                && evt.type == EventType.MouseDown && evt.button == 1
                && s_DataPointsIDs.Contains(HandleUtility.nearestControl))
            {
                var dataPointIndex = splineData.Indexes.ElementAt(Array.IndexOf(s_DataPointsIDs, HandleUtility.nearestControl));
                splineData.RemoveDataPoint(dataPointIndex);
                GUI.changed = true;
                evt.Use();
            }
        }

        static bool IsHotControl(float splineID)
        {
            return !float.IsNaN(s_AddingDataPoint) && splineID.Equals(s_AddingDataPoint);
        }

        static void DataPointAddHandle<TSpline, TData>(
            int controlID,
            TSpline spline,
            SplineData<TData> splineData,
            bool useDefaultValueOnAdd,
            int splineID)
            where TSpline : ISpline
        {
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(controlID);

            switch (eventType)
            {
                case EventType.Layout:
                {
                    if (!Tools.viewToolActive)
                    {
                        var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                        SplineUtility.GetNearestPoint(spline, ray, out var pos, out _);
                        HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(pos, 0.1f));
                    }
                    break;
                }

                case EventType.Repaint:
                    if ((HandleUtility.nearestControl == controlID && GUIUtility.hotControl == 0 && float.IsNaN(s_AddingDataPoint)) || IsHotControl(splineID))
                    {
                        var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                        SplineUtility.GetNearestPoint(spline, ray, out var pos, out var t);
                        var upDir = spline.EvaluateUpVector(t);
                        Handles.CircleHandleCap(controlID, pos, Quaternion.LookRotation(upDir), 0.15f * HandleUtility.GetHandleSize(pos), EventType.Repaint);
                    }
                    break;

                case EventType.MouseDown:
                    if (evt.button == 0
                        && !Tools.viewToolActive
                        && HandleUtility.nearestControl == controlID
                        && GUIUtility.hotControl == 0)
                    {
                        s_AddingDataPoint = splineID;
                        var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                        SplineUtility.GetNearestPoint(spline, ray, out _, out var t);
                        var index = SplineUtility.ConvertIndexUnit(
                            spline, t,
                            splineData.PathIndexUnit);

                        s_NewDataPointIndex = splineData.AddDataPointWithDefaultValue(index, useDefaultValueOnAdd);
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (evt.button == 0 && IsHotControl(splineID))
                    {
                        var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                        SplineUtility.GetNearestPoint(spline, ray, out _, out var t);
                        var index = SplineUtility.ConvertIndexUnit(
                            spline, t,
                            splineData.PathIndexUnit);
                        s_NewDataPointIndex = splineData.MoveDataPoint(s_NewDataPointIndex, index);
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (evt.button == 0 && IsHotControl(splineID))
                    {
                        s_AddingDataPoint = float.NaN;
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
                    // If the current DataPoint is moved across another DataPoint, then update the hotControl ID
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
                        && !Tools.viewToolActive
                        && HandleUtility.nearestControl == controlID
                        && GUIUtility.hotControl == 0)
                    {
                        GUIUtility.hotControl = controlID;
                        newTime = GetClosestSplineDataT(spline, splineData);
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        newTime = GetClosestSplineDataT(spline, splineData);
                        GUI.changed = true;
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
