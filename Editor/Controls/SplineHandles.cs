using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class SplineHandles
    {
        static int[] s_CurveIDs;

        static readonly List<int> s_TangentChildIDs = new List<int>(2);

        internal static void DrawSplineHandles(IReadOnlyList<SplineInfo> splines)
        {
            for (int i = 0; i < splines.Count; ++i)
            {
                DrawSplineHandles(splines[i]);
            }
        }

        internal static bool DrawSplineHandles(SplineInfo splineInfo, bool activeSpline = true)
        {
            var spline = splineInfo.Spline;
            var localToWorld = splineInfo.LocalToWorld;
            // If the spline isn't closed, skip the last index of the spline
            int lastIndex = spline.Closed ? spline.Count - 1 : spline.Count - 2;

            s_CurveIDs = new int[spline.GetCurveCount()];
            for(int idIndex = 0; idIndex < lastIndex + 1; ++idIndex)
                s_CurveIDs[idIndex] = GUIUtility.GetControlID(FocusType.Passive);

            var drawHandlesAsActive = s_CurveIDs.Contains(HandleUtility.nearestControl) || activeSpline;
            for (int curveIndex = 0; curveIndex < lastIndex + 1; ++curveIndex)
            {
                var curve = spline.GetCurve(curveIndex).Transform(localToWorld);
                CurveHandles.DrawWithHighlight(
                    s_CurveIDs[curveIndex],
                    curve,
                    new SelectableKnot(splineInfo, curveIndex),
                    new SelectableKnot(splineInfo, SplineUtility.NextIndex(curveIndex, spline.Count, spline.Closed)),
                    drawHandlesAsActive);
            }

            for (int knotIndex = 0; knotIndex < spline.Count; ++knotIndex)
            {
                s_TangentChildIDs.Clear();
                var knot = new SelectableKnot(splineInfo, knotIndex);

                int controlId = SelectionHandle(knot);
                KnotHandles.Draw(controlId, knot, s_TangentChildIDs, false, s_CurveIDs.Contains(HandleUtility.nearestControl) || drawHandlesAsActive);

                if (EditorSplineUtility.AreTangentsModifiable(splineInfo.Spline.GetTangentMode(knotIndex)))
                {
                    var tangentIn = new SelectableTangent(splineInfo, knotIndex, BezierTangent.In);
                    var tangentOut = new SelectableTangent(splineInfo, knotIndex, BezierTangent.Out);

                    //Tangent In
                    if (spline.Closed || knotIndex != 0)
                    {
                        controlId = SelectionHandle(tangentIn);
                        s_TangentChildIDs.Add(controlId);
                        TangentHandles.Draw(controlId, tangentIn, drawHandlesAsActive);
                    }

                    //Tangent Out
                    if (spline.Closed || knotIndex + 1 != spline.Count)
                    {
                        controlId = SelectionHandle(tangentOut);
                        s_TangentChildIDs.Add(controlId);
                        TangentHandles.Draw(controlId, tangentOut, drawHandlesAsActive);
                    }
                }
            }

            if (SplineHandleUtility.lastHoveredTangent.IsValid() && Event.current.GetTypeForControl(SplineHandleUtility.lastHoveredTangentID) == EventType.Repaint)
                SplineHandleUtility.SetLastHoveredTangent(default, -1);

            return activeSpline;
        }

        static int SelectionHandle<T>(T element)
            where T : struct, ISplineElement
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(id);

            switch (eventType)
            {
                case EventType.Layout:
                    if (!Tools.viewToolActive)
                    {
                        HandleUtility.AddControl(id, SplineHandleUtility.DistanceToCircle(element.Position, SplineHandleUtility.pickingDistance));
                        if (element is SelectableTangent tangent && HandleUtility.nearestControl == id)
                            SplineHandleUtility.SetLastHoveredTangent(tangent, id);
                    }
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id)
                    {
                        //Clicking a knot selects it
                        if (evt.button != 0)
                            break;

                        GUIUtility.hotControl = id;
                        evt.Use();

                        SplineSelectionUtility.HandleSelection(
                            element,
                            (EditorGUI.actionKey || evt.modifiers == EventModifiers.Shift));
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }

                    break;

                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint();
                    break;
            }

            return id;
        }
    }
}