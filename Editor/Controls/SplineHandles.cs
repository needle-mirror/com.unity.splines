using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class SplineHandles
    {
        static List<int> s_ControlIDs = new();
        static readonly List<int> k_TangentChildIDs = new(2);
        static List<int> s_CurveIDs = new();
        
        internal static List<int> tangentIDs => k_TangentChildIDs;
        
        // todo Tools.viewToolActive should be handling the modifier check, but 2022.2 broke this
        internal static bool ViewToolActive()
        {
            return Tools.viewToolActive || Tools.current == Tool.View || (Event.current.modifiers & EventModifiers.Alt) == EventModifiers.Alt;
        }

        internal static void DrawSplineHandles(IReadOnlyList<SplineInfo> splines)
        {
            var id = HandleUtility.nearestControl;

            s_CurveIDs.Clear();
            // Drawing done in two separate passes to make sure the curves are drawn behind the spline elements.
            // Draw the curves.
            for (int i = 0; i < splines.Count; ++i)
                DrawSplineCurves(splines[i]);

            KnotHandles.ClearVisibleKnots();
            // Draw the spline elements.
            for (int i = 0; i < splines.Count; ++i)
                DrawSplineElements(splines[i]);
            //Drawing knots on top of all other elements and above other splines
            KnotHandles.DrawVisibleKnots();

            var evtType = Event.current.type;
            if ((evtType == EventType.MouseMove || evtType == EventType.Layout) && HandleUtility.nearestControl == id)
                SplineHandleUtility.ResetLastHoveredElement();
        }

        internal static bool IsCurveId(int id)
        {
            return s_CurveIDs.Contains(id);
        }

        internal static void DrawSplineCurves(SplineInfo splineInfo)
        {
            var spline = splineInfo.Spline;
            var localToWorld = splineInfo.LocalToWorld;

            // If the spline isn't closed, skip the last index of the spline
            int lastIndex = spline.Closed ? spline.Count - 1 : spline.Count - 2;

            s_ControlIDs.Clear();
            for (int idIndex = 0; idIndex < lastIndex + 1; ++idIndex)
            {
                var id = GUIUtility.GetControlID(FocusType.Passive);
                s_ControlIDs.Add(id);
                s_CurveIDs.Add(id);
            }

            var drawHandlesAsActive = !SplineSelection.HasActiveSplineSelection() || SplineSelection.Contains(splineInfo);

            for (int curveIndex = 0; curveIndex < lastIndex + 1; ++curveIndex)
            {
                var curve = spline.GetCurve(curveIndex).Transform(localToWorld);
                CurveHandles.DrawWithoutHighlight(
                    s_ControlIDs[curveIndex],
                    curve,
                    splineInfo.Spline,
                    curveIndex,
                    new SelectableKnot(splineInfo, curveIndex),
                    new SelectableKnot(splineInfo, SplineUtility.NextIndex(curveIndex, spline.Count, spline.Closed)),
                    drawHandlesAsActive);
            }

            for (int curveIndex = 0; curveIndex < lastIndex + 1; ++curveIndex)
            {
                var curve = spline.GetCurve(curveIndex).Transform(localToWorld);
                CurveHandles.DrawWithHighlight(
                    s_ControlIDs[curveIndex],
                    curve,
                    splineInfo.Spline,
                    curveIndex,
                    new SelectableKnot(splineInfo, curveIndex),
                    new SelectableKnot(splineInfo, SplineUtility.NextIndex(curveIndex, spline.Count, spline.Closed)),
                    drawHandlesAsActive);
            }
        }

        static void DrawSplineElements(SplineInfo splineInfo)
        {
            var spline = splineInfo.Spline;
            var drawHandlesAsActive = !SplineSelection.HasActiveSplineSelection() || SplineSelection.Contains(splineInfo);
            if(drawHandlesAsActive)
            {
                for(int knotIndex = 0; knotIndex < spline.Count; ++knotIndex)
                {
                    k_TangentChildIDs.Clear();
                    var knot = new SelectableKnot(splineInfo, knotIndex);
                    
                    if (EditorSplineUtility.AreTangentsModifiable(splineInfo.Spline.GetTangentMode(knotIndex)))
                    {
                        var tangentIn = new SelectableTangent(splineInfo, knotIndex, BezierTangent.In);
                        var tangentOut = new SelectableTangent(splineInfo, knotIndex, BezierTangent.Out);

                        // Tangent In
                        if (SplineHandleUtility.ShouldShowTangent(tangentIn) && (spline.Closed || knotIndex != 0))
                        {
                            var controlId = GUIUtility.GetControlID(FocusType.Passive);
                            SelectionHandle(controlId, tangentIn);
                            k_TangentChildIDs.Add(controlId);
                            s_ControlIDs.Add(controlId);
                            TangentHandles.Draw(controlId, tangentIn);
                        }

                        // Tangent Out
                        if (SplineHandleUtility.ShouldShowTangent(tangentOut) && (spline.Closed || knotIndex + 1 != spline.Count))
                        {
                            var controlId = GUIUtility.GetControlID(FocusType.Passive);
                            SelectionHandle(controlId, tangentOut);
                            k_TangentChildIDs.Add(controlId);
                            s_ControlIDs.Add(controlId);
                            TangentHandles.Draw(controlId, tangentOut);
                        }
                    }

                    var id = GUIUtility.GetControlID(FocusType.Passive);
                    s_ControlIDs.Add(id);
                    SelectionHandle(id, knot);
                    KnotHandles.Draw(id, knot);
                }
            }
            else
            {
                for (int knotIndex = 0; knotIndex < spline.Count; ++knotIndex)
                {
                    var knot = new SelectableKnot(splineInfo, knotIndex);
                    KnotHandles.DrawInformativeKnot(knot);
                }
            }
        }

        static void SelectionHandle<T>(int id, T element)
            where T : struct, ISplineElement
        {
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(id);

            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    if (!ViewToolActive())
                    {
                        HandleUtility.AddControl(id, SplineHandleUtility.DistanceToCircle(element.Position, SplineHandleUtility.pickingDistance));
                        if(HandleUtility.nearestControl == id)
                        {
                            SplineHandleUtility.SetLastHoveredElement(element, id);
                        }
                    }
                    break;

                case EventType.MouseDown:
                    if (!ViewToolActive() && HandleUtility.nearestControl == id)
                    {
                        // Clicking a knot selects it
                        if (evt.button != 0)
                            break;

                        GUIUtility.hotControl = id;
                        evt.Use();

                        SplineSelectionUtility.HandleSelection(element);
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;
            }
        }
    }
}