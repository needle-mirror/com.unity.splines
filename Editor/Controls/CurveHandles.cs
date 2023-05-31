using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class CurveHandles
    {
        const int k_CurveDrawResolution = 72;
        const float k_CurveLineWidth = 4f;
        const float k_PreviewCurveOpacity = 0.5f;

        static readonly Vector3[] s_CurveDrawingBuffer = new Vector3[k_CurveDrawResolution + 1];
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
            BezierCurve curve,
            ISpline spline,
            int curveIndex,
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
                        var dist = DistanceToCurve(curve);
                        HandleUtility.AddControl(controlID, Mathf.Max(0, dist - SplineHandleUtility.pickingDistance));
                        //Trigger repaint on MouseMove to update highlight visuals from SplineHandles
                        if (evt.type == EventType.MouseMove || controlID == HandleUtility.nearestControl)
                        {
                            SplineHandleUtility.GetNearestPointOnCurve(curve, out _, out var t);
                            var curveMidT = GetCurveMiddleInterpolation(curve, spline, curveIndex);
                            var hoveredKnot = t <= curveMidT ? knotA : knotB;

                            if (!(SplineHandleUtility.lastHoveredElement is SelectableKnot knot) || !knot.Equals(hoveredKnot))
                            {
                                SplineHandleUtility.SetLastHoveredElement(hoveredKnot, controlID);
                                SceneView.RepaintAll();
                            }
                        }
                    }
                    break;

                case EventType.MouseDown:
                    if (!SplineHandles.ViewToolActive() && HandleUtility.nearestControl == controlID)
                    {
                        //Clicking a knot selects it
                        if (evt.button != 0)
                            break;

                        GUIUtility.hotControl = controlID;
                        evt.Use();

                        SplineHandleUtility.GetNearestPointOnCurve(curve, out _, out var t);
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
        /// Draws curve and flow for a BezierCurve without the highlight.
        /// </summary>
        /// <param name="controlID">The controlID of the curve to create highlights for.</param>
        /// <param name="curve">The <see cref="BezierCurve"/> to create highlights for.</param>
        /// <param name="spline">The <see cref="ISpline"/> (if any) that the curve belongs to.</param>
        /// <param name="curveIndex">The curve's index if it belongs to a spline - otherwise -1.</param>
        /// <param name="knotA">The knot at the start of the curve.</param>
        /// <param name="knotB">The knot at the end of the curve.</param>
        /// <param name="activeSpline">Whether the curve is part of the active spline.</param>
        internal static void DrawWithoutHighlight(
            int controlID,
            BezierCurve curve,
            ISpline spline,
            int curveIndex,
            SelectableKnot knotA,
            SelectableKnot knotB,
            bool activeSpline)
        {
            var evt = Event.current;
            switch (evt.GetTypeForControl(controlID))
            {
                case EventType.Repaint:
                    Draw(controlID, curve, false, activeSpline);
                    if (SplineHandleSettings.FlowDirectionEnabled && activeSpline)
                        DrawFlow(curve, spline, curveIndex, math.rotate(knotA.Rotation, math.up()), math.rotate(knotB.Rotation, math.up()));
                    break;
            }
        }

        /// <summary>
        /// Draws flow on a BezierCurve to indicate the direction.
        /// </summary>
        /// <param name="curve">The <see cref="BezierCurve"/> to create highlights for.</param>
        /// <param name="spline">The <see cref="ISpline"/> (if any) that the curve belongs to.</param>
        /// <param name="curveIndex">The curve's index if it belongs to a spline - otherwise -1.</param>
        /// <param name="upAtStart">The up vector at the start of the curve.</param>
        /// <param name="upAtEnd">The up vector at the end of the curve.</param>
        internal static void DrawFlow(BezierCurve curve, ISpline spline, int curveIndex, Vector3 upAtStart, Vector3 upAtEnd)
        {
            if(Event.current.type != EventType.Repaint)
                return;

            var curveMidT = GetCurveMiddleInterpolation(curve, spline, curveIndex);
            var arrow = GetFlowArrowData(curve, curveMidT, upAtStart, upAtEnd);
            s_FlowTriangleVertices[0] = arrow.pointA;
            s_FlowTriangleVertices[1] = arrow.pointB;
            s_FlowTriangleVertices[2] = arrow.pointC;

            using (new Handles.DrawingScope(SplineHandleUtility.lineColor, arrow.transform))
            {
                using (new ZTestScope(CompareFunction.Less))
                    Handles.DrawAAConvexPolygon(s_FlowTriangleVertices);
            }

            using (new Handles.DrawingScope(SplineHandleUtility.lineBehindColor, arrow.transform))
            {
                using (new ZTestScope(CompareFunction.Greater))
                    Handles.DrawAAConvexPolygon(s_FlowTriangleVertices);
            }
        }

        public static (Vector3 pointA, Vector3 pointB, Vector3 pointC, Matrix4x4 transform) GetFlowArrowData(BezierCurve curve, float t, Vector3 upAtStart, Vector3 upAtEnd, float sizeMultiplier = 1f)
        {
            var position = (Vector3)CurveUtility.EvaluatePosition(curve, t);
            var tangent = ((Vector3)CurveUtility.EvaluateTangent(curve, t)).normalized;
            var up = CurveUtility.EvaluateUpVector(curve, t, upAtStart, upAtEnd);
            var rotation = Quaternion.LookRotation(tangent, up);

            var arrowMaxSpline = .05f * CurveUtility.ApproximateLength(curve);
            var size = HandleUtility.GetHandleSize(position) * .5f * sizeMultiplier;

            tangent = new Vector3(0, 0, .1f) * size;
            var right = new Vector3(0.075f, 0, 0) * size;
            var magnitude = tangent.magnitude;

            if(magnitude > arrowMaxSpline)
            {
                var ratio = arrowMaxSpline / magnitude;
                tangent *= ratio;
                right *= ratio;
            }

            var a = tangent;
            var b = -tangent + right;
            var c = -tangent - right;

            return (pointA: a, pointB: b, pointC: c, transform: Matrix4x4.TRS(position, rotation, Vector3.one));
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
            const float segmentPercentage = 1f / k_CurveDrawResolution;
            for (int i = 0; i <= k_CurveDrawResolution; ++i)
            {
                s_CurveDrawingBuffer[i] = CurveUtility.EvaluatePosition(curve, i * segmentPercentage);
            }
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

        /// <summary>
        /// Returns the interpolation value that corresponds to the middle (distance wise) of the curve.
        /// If spline and curveIndex are provided, the function leverages the spline's LUTs, otherwise the LUT is built on the fly.
        /// </summary>
        /// <param name="curve">The curve to evaluate.</param>
        /// <param name="spline">The ISpline that curve belongs to. Not used if curve is not part of any spline.</param>
        /// <param name="curveIndex">The index of the curve if it's part of the spine.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        internal static float GetCurveMiddleInterpolation<T>(BezierCurve curve, T spline, int curveIndex) where T: ISpline
        {
            var curveMidT = 0f;
            if (curveIndex >= 0)
                curveMidT = spline.GetCurveInterpolation(curveIndex, spline.GetCurveLength(curveIndex) * 0.5f);
            else
                curveMidT = CurveUtility.GetDistanceToInterpolation(curve, CurveUtility.ApproximateLength(curve) * 0.5f);

            return curveMidT;
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
                    var curveMiddleT = GetCurveMiddleInterpolation(curve, spline, spline.PreviousIndex(knot.KnotIndex));
                    DrawCurveHighlight(curve, 1f, curveMiddleT);
                }

                if(knot.KnotIndex < spline.Count - 1  || spline.Closed)
                {
                    var curve = spline.GetCurve(knot.KnotIndex).Transform(localToWorld);
                    var curveMiddleT = GetCurveMiddleInterpolation(curve, spline, knot.KnotIndex);
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
            for (int i = 1; i <= k_CurveDrawResolution; ++i)
            {
                Handles.DrawAAPolyLine(SplineHandleUtility.denseLineAATex, k_CurveLineWidth, new[] { s_CurveDrawingBuffer[i - 1], s_CurveDrawingBuffer[i] });

                var current = ((float)i / (float)k_CurveDrawResolution);
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