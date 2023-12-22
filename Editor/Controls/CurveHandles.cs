using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class CurveHandles
    {
        const float k_CurveLineWidth = 4f;
        const float k_PreviewCurveOpacity = 0.5f;

        static readonly Vector3[] s_CurveDrawingBuffer = new Vector3[SplineCacheUtility.CurveDrawResolution + 1];
        static readonly Vector3[] s_FlowTriangleVertices = new Vector3[3];

        /// <summary>
        /// Creates handles for a BezierCurve.
        /// </summary>
        /// <param name="controlID">The controlID of the curve to create highlights for.</param>
        /// <param name="curve">The <see cref="BezierCurve"/> to create handles for.</param>
        public static void Draw(int controlID, BezierCurve curve)
        {
            if(Event.current.type == EventType.Repaint)
                Draw(controlID, curve, false, true);
        }

        /// <summary>
        /// Creates handles for a BezierCurve.
        /// </summary>
        /// <param name="curve">The <see cref="BezierCurve"/> to create handles for.</param>
        /// <param name="activeSpline">Whether the curve is part of the active spline.</param>
        internal static void Draw(BezierCurve curve, bool activeSpline)
        {
            if(Event.current.type == EventType.Repaint)
                Draw(0, curve, false, activeSpline);
        }

        /// <summary>
        /// Creates highlights for a BezierCurve to make it easier to select.
        /// </summary>
        /// <param name="controlID">The controlID of the curve to create highlights for.</param>
        /// <param name="curve">The <see cref="BezierCurve"/> to create highlights for.</param>
        /// <param name="spline">The <see cref="ISpline"/> (if any) that the curve belongs to.</param>
        /// <param name="curveIndex">The curve's index if it belongs to a spline - otherwise -1.</param>
        /// <param name="knotA">The knot at the start of the curve.</param>
        /// <param name="knotB">The knot at the end of the curve.</param>
        /// <param name="activeSpline">Whether the curve is part of the active spline.</param>
        internal static void DrawWithHighlight(
            int controlID,
            ISpline spline,
            int curveIndex,
            float4x4 localToWorld,
            SelectableKnot knotA,
            SelectableKnot knotB,
            bool activeSpline)
        {
            var evt = Event.current;
            switch(evt.GetTypeForControl(controlID))
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    if (!SplineHandles.ViewToolActive() && activeSpline)
                    {
                        var curve = spline.GetCurve(curveIndex).Transform(localToWorld);
                        var dist = DistanceToCurve(curve);
                        HandleUtility.AddControl(controlID, Mathf.Max(0, dist - SplineHandleUtility.pickingDistance));
                        //Trigger repaint on MouseMove to update highlight visuals from SplineHandles
                        if (evt.type == EventType.MouseMove || controlID == HandleUtility.nearestControl)
                        {
                            SplineHandleUtility.GetNearestPointOnCurve(curve, out _, out var t);
                            var curveMidT = EditorSplineUtility.GetCurveMiddleInterpolation(curve, spline, curveIndex);
                            var hoveredKnot = t <= curveMidT ? knotA : knotB;

                            if (!(SplineHandleUtility.lastHoveredElement is SelectableKnot knot) || !knot.Equals(hoveredKnot))
                            {
                                if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlID)
                                {
                                    SplineHandleUtility.SetLastHoveredElement(hoveredKnot, controlID);
                                    SceneView.RepaintAll();
                                }
                            }
                        }
                    }
                    break;

                case EventType.MouseDown:
                    var curveMD = spline.GetCurve(curveIndex).Transform(localToWorld);
                    if (!SplineHandles.ViewToolActive() && HandleUtility.nearestControl == controlID)
                    {
                        //Clicking a knot selects it
                        if (evt.button != 0)
                            break;

                        GUIUtility.hotControl = controlID;
                        evt.Use();

                        SplineHandleUtility.GetNearestPointOnCurve(curveMD, out _, out var t);
                        SplineSelectionUtility.HandleSelection(t <= .5f ? knotA : knotB, false);
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;
            }
        }

        /// <summary>
        /// Draws flow on a BezierCurve to indicate the direction.
        /// </summary>
        /// <param name="curve">The <see cref="BezierCurve"/> to create highlights for.</param>
        /// <param name="spline">The <see cref="ISpline"/> (if any) that the curve belongs to.</param>
        /// <param name="curveIndex">The curve's index if it belongs to a spline - otherwise -1.</param>
        internal static void DrawFlow(BezierCurve curve, ISpline spline, int curveIndex)
        {
            if(Event.current.type != EventType.Repaint)
                return;

            var arrow = SplineCacheUtility.GetCurveArrow(spline, curveIndex, curve);
            s_FlowTriangleVertices[0] = arrow.positions[0];
            s_FlowTriangleVertices[1] = arrow.positions[1];
            s_FlowTriangleVertices[2] = arrow.positions[2];

            using (new Handles.DrawingScope(SplineHandleUtility.lineColor, arrow.trs))
            {
                using (new ZTestScope(CompareFunction.Less))
                    Handles.DrawAAConvexPolygon(s_FlowTriangleVertices);
            }

            using (new Handles.DrawingScope(SplineHandleUtility.lineBehindColor, arrow.trs))
            {
                using (new ZTestScope(CompareFunction.Greater))
                    Handles.DrawAAConvexPolygon(s_FlowTriangleVertices);
            }
        }

        static void Draw(int controlID, BezierCurve curve, bool preview, bool activeSpline)
        {
            var evt = Event.current;

            switch (evt.type)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    if (!SplineHandles.ViewToolActive() && activeSpline)
                    {
                        var dist = DistanceToCurve(curve);
                        HandleUtility.AddControl(controlID, Mathf.Max(0, dist - SplineHandleUtility.pickingDistance));
                    }
                    break;

                case EventType.Repaint:
                    var prevColor = Handles.color;
                    FillCurveDrawingBuffer(curve);

                    var color = SplineHandleUtility.lineColor;
                    if (preview)
                        color.a *= k_PreviewCurveOpacity;

                    Handles.color = color;
                    using (new ZTestScope(CompareFunction.Less))
                        Handles.DrawAAPolyLine(SplineHandleUtility.denseLineAATex, k_CurveLineWidth, s_CurveDrawingBuffer);

                    color = SplineHandleUtility.lineBehindColor;
                    if (preview)
                        color.a *= k_PreviewCurveOpacity;

                    Handles.color = color;
                    using (new ZTestScope(CompareFunction.Greater))
                        Handles.DrawAAPolyLine(SplineHandleUtility.denseLineAATex, k_CurveLineWidth, s_CurveDrawingBuffer);

                    Handles.color = prevColor;
                    break;
            }
        }

        static void FillCurveDrawingBuffer(BezierCurve curve)
        {
            SplineCacheUtility.GetCurvePositions(curve, s_CurveDrawingBuffer);
        }

        internal static float DistanceToCurve(BezierCurve curve)
        {
            FillCurveDrawingBuffer(curve);
            return DistanceToCurve();
        }

        static float DistanceToCurve()
        {
            float dist = float.MaxValue;
            for (var i = 0; i < s_CurveDrawingBuffer.Length - 1; ++i)
            {
                var a = s_CurveDrawingBuffer[i];
                var b = s_CurveDrawingBuffer[i + 1];
                dist = Mathf.Min(HandleUtility.DistanceToLine(a, b), dist);
            }

            return dist;
        }

        internal static void DoCurveHighlightCap(SelectableKnot knot)
        {
            if(Event.current.type != EventType.Repaint)
                return;

            if(knot.IsValid())
            {
                var spline = knot.SplineInfo.Spline;
                var localToWorld = knot.SplineInfo.LocalToWorld;

                if(knot.KnotIndex > 0 || spline.Closed)
                {
                    var curve = spline.GetCurve(spline.PreviousIndex(knot.KnotIndex)).Transform(localToWorld);
                    var curveMiddleT = EditorSplineUtility.GetCurveMiddleInterpolation(curve, spline, spline.PreviousIndex(knot.KnotIndex));
                    DrawCurveHighlight(curve, 1f, curveMiddleT);
                }

                if(knot.KnotIndex < spline.Count - 1  || spline.Closed)
                {
                    var curve = spline.GetCurve(knot.KnotIndex).Transform(localToWorld);
                    var curveMiddleT = EditorSplineUtility.GetCurveMiddleInterpolation(curve, spline, knot.KnotIndex);
                    DrawCurveHighlight(curve, 0f, curveMiddleT);
                }
            }
        }

        static void DrawCurveHighlight(BezierCurve curve, float startT, float endT)
        {
            FillCurveDrawingBuffer(curve);

            var growing = startT <= endT;
            var color = Handles.color;
            color.a = growing ? 1f : 0f;

            using (new ZTestScope(CompareFunction.Less))
                using (new Handles.DrawingScope(color))
                    DrawAAPolyLineForCurveHighlight(color, startT, endT, 1f, growing);

            using (new ZTestScope(CompareFunction.Greater))
                using (new Handles.DrawingScope(color))
                    DrawAAPolyLineForCurveHighlight(color, startT, endT, 0.3f, growing);
        }

        static void DrawAAPolyLineForCurveHighlight(Color color, float startT, float endT, float colorAlpha, bool growing)
        {
            for (int i = 1; i <= SplineCacheUtility.CurveDrawResolution; ++i)
            {
                Handles.DrawAAPolyLine(SplineHandleUtility.denseLineAATex, k_CurveLineWidth, new[] { s_CurveDrawingBuffer[i - 1], s_CurveDrawingBuffer[i] });

                var current = ((float)i / (float)SplineCacheUtility.CurveDrawResolution);
                if (growing)
                {
                    if (current > endT)
                        color.a = 0f;
                    else if (current > startT)
                        color.a = (1f - (current - startT) / (endT - startT)) * colorAlpha;
                }
                else
                {
                    if (current < endT)
                        color.a = 0f;
                    else if (current > endT && current < startT)
                        color.a = (current - endT) / (startT - endT) * colorAlpha;
                }

                Handles.color = color;
            }
        }

        /// <summary>
        /// Creates the set of control points that make up a curve.
        /// </summary>
        /// <param name="curve">The <see cref="BezierCurve"/> to create control points for.</param>
        public static void DrawControlNet(BezierCurve curve)
        {
            Handles.color = Color.green;
            Handles.DotHandleCap(-1, curve.P0, Quaternion.identity, HandleUtility.GetHandleSize(curve.P0) * .04f, Event.current.type);
            Handles.color = Color.red;
            Handles.DotHandleCap(-1, curve.P1, Quaternion.identity, HandleUtility.GetHandleSize(curve.P1) * .04f, Event.current.type);
            Handles.color = Color.yellow;
            Handles.DotHandleCap(-1, curve.P2, Quaternion.identity, HandleUtility.GetHandleSize(curve.P2) * .04f, Event.current.type);
            Handles.color = Color.blue;
            Handles.DotHandleCap(-1, curve.P3, Quaternion.identity, HandleUtility.GetHandleSize(curve.P3) * .04f, Event.current.type);

            Handles.color = Color.gray;
            Handles.DrawDottedLine(curve.P0, curve.P1, 2f);
            Handles.DrawDottedLine(curve.P1, curve.P2, 2f);
            Handles.DrawDottedLine(curve.P2, curve.P3, 2f);
        }
    }
}
