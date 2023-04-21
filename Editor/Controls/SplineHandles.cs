using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// This class provides the ability to draw a handle for a spline.
    /// </summary>
    public static class SplineHandles
    {
        static List<int> s_ControlIDs = new();
        static readonly List<int> k_TangentChildIDs = new(2);
        static List<int> s_CurveIDs = new();

        static readonly List<SelectableKnot> k_KnotBuffer = new ();
        static Dictionary<SelectableKnot, int> s_KnotsIDs = new ();

        internal static List<int> tangentIDs => k_TangentChildIDs;

        // todo Tools.viewToolActive should be handling the modifier check, but 2022.2 broke this
        internal static bool ViewToolActive()
        {
            return Tools.viewToolActive || Tools.current == Tool.View || (Event.current.modifiers & EventModifiers.Alt) == EventModifiers.Alt;
        }

        static void Clear()
        {
            s_CurveIDs.Clear();
            s_KnotsIDs.Clear();
        }

        internal static void DrawSplineHandles(IReadOnlyList<SplineInfo> splines)
        {
            var id = HandleUtility.nearestControl;

            Clear();
            SplineHandleUtility.minElementId = GUIUtility.GetControlID(FocusType.Passive);

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

            SplineHandleUtility.maxElementId = GUIUtility.GetControlID(FocusType.Passive);

            var evtType = Event.current.type;
            if ( (evtType == EventType.MouseMove || evtType == EventType.Layout) && HandleUtility.nearestControl == id)
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

            if (SplineHandleSettings.ShowMesh)
            {
                using (var nativeSpline = new NativeSpline(spline, localToWorld))
                using (var mesh = new SplineMeshHandle<NativeSpline>())
                using (new ZTestScope(UnityEngine.Rendering.CompareFunction.Less))
                {
                    mesh.Do(nativeSpline, SplineHandleSettings.SplineMeshSize, SplineHandleSettings.SplineMeshColor, SplineHandleSettings.SplineMeshResolution);
                }
            }

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

            if (drawHandlesAsActive)
            {
                for (int knotIndex = 0; knotIndex < spline.Count; ++knotIndex)
                {
                    k_TangentChildIDs.Clear();
                    var knot = new SelectableKnot(splineInfo, knotIndex);

                    if (SplineUtility.AreTangentsModifiable(splineInfo.Spline.GetTangentMode(knotIndex)))
                    {
                        var tangentIn = new SelectableTangent(splineInfo, knotIndex, BezierTangent.In);
                        var tangentOut = new SelectableTangent(splineInfo, knotIndex, BezierTangent.Out);

                        var controlIdIn = GUIUtility.GetControlID(FocusType.Passive);
                        var controlIdOut = GUIUtility.GetControlID(FocusType.Passive);
                        // Tangent In
                        if (GUIUtility.hotControl == controlIdIn || SplineHandleUtility.ShouldShowTangent(tangentIn) && (spline.Closed || knotIndex != 0))
                        {
                            SelectionHandle(controlIdIn, tangentIn);
                            k_TangentChildIDs.Add(controlIdIn);
                            TangentHandles.Draw(controlIdIn, tangentIn);
                        }

                        // Tangent Out
                        if (GUIUtility.hotControl == controlIdOut || SplineHandleUtility.ShouldShowTangent(tangentOut) && (spline.Closed || knotIndex + 1 != spline.Count))
                        {
                            SelectionHandle(controlIdOut, tangentOut);
                            k_TangentChildIDs.Add(controlIdOut);
                            TangentHandles.Draw(controlIdOut, tangentOut);
                        }
                    }

                    var id = GetKnotID(knot);
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

        static int GetKnotID(SelectableKnot knot)
        {
            EditorSplineUtility.GetKnotLinks(knot, k_KnotBuffer);
            //If a linked knot as already been assigned, return the same id
            if (s_KnotsIDs.ContainsKey(k_KnotBuffer[0]))
                return s_KnotsIDs[k_KnotBuffer[0]];

            //else compute a new id and record it
            var id = GUIUtility.GetControlID(FocusType.Passive);
            s_KnotsIDs.Add(k_KnotBuffer[0], id);
            return id;
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
                        if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == id)
                            SplineHandleUtility.SetLastHoveredElement(element, id);
                    }
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                    {
                        GUIUtility.hotControl = id;
                        evt.Use();

                        DirectManipulation.BeginDrag(element.Position, EditorSplineUtility.GetElementRotation(element));
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        EditorSplineUtility.RecordObject(element.SplineInfo, "Move Knot");
                        var pos = TransformOperation.ApplySmartRounding(DirectManipulation.UpdateDrag(id));

                        if (element is SelectableTangent tangent)
                            EditorSplineUtility.ApplyPositionToTangent(tangent, pos);
                        else
                            element.Position = pos;

                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && evt.button == 0)
                    {
                        if (!DirectManipulation.IsDragging)
                            SplineSelectionUtility.HandleSelection(element);

                        DirectManipulation.EndDrag();

                        evt.Use();
                        GUIUtility.hotControl = 0;
                    }
                    break;

                case EventType.Repaint:
                    DirectManipulation.DrawHandles(id, element.Position);
                    break;
            }
        }

        /// <summary>
        /// Draws a handle for a spline.
        /// </summary>
        /// <param name="spline">The target spline.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        public static void DoSpline<T>(T spline) where T : ISpline => DoSpline(-1, spline);

        /// <summary>
        /// Draws a handle for a spline.
        /// </summary>
        /// <param name="controlID">The spline mesh controlID.</param>
        /// <param name="spline">The target spline.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        public static void DoSpline<T>(int controlID, T spline) where T : ISpline
        {
            for(int i = 0; i < spline.GetCurveCount(); ++i)
                CurveHandles.Draw(controlID, spline.GetCurve(i));
        }

        /// <summary>
        /// Draws a handle for a <see cref="BezierCurve"/>.
        /// </summary>
        /// <param name="curve">The <see cref="BezierCurve"/> to create handles for.</param>
        public static void DoCurve(BezierCurve curve) => CurveHandles.Draw(-1, curve);

        /// <summary>
        /// Draws a handle for a <see cref="BezierCurve"/>.
        /// </summary>
        /// <param name="controlID">The spline mesh controlID.</param>
        /// <param name="curve">The <see cref="BezierCurve"/> to create handles for.</param>
        public static void DoCurve(int controlID, BezierCurve curve) => CurveHandles.Draw(controlID, curve);
    }
}
