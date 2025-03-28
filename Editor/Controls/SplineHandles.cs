using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// This class provides the ability to draw a handle for a spline.
    /// </summary>
    public static class SplineHandles
    {
        /// <summary>
        /// The scope used to draw a spline. This is managing several purposes when using SplineHandles.DrawSomething().
        /// This ensure selection is working properly, and that hovering an element is highlighting the correct related
        /// elements (for instance hovering a tangent highlights the opposite one when needed and the knot as well).
        /// </summary>
        public class SplineHandleScope : IDisposable
        {
            int m_NearestControl;

            /// <summary>
            /// Defines a new scope to draw spline elements in.
            /// </summary>
            public SplineHandleScope()
            {
                m_NearestControl = HandleUtility.nearestControl;
                Clear();
                SplineHandleUtility.minElementId = GUIUtility.GetControlID(FocusType.Passive);
            }

            /// <summary>
            /// Called automatically when the `SplineHandleScope` is disposed.
            /// </summary>
            public void Dispose()
            {
                SplineHandleUtility.maxElementId = GUIUtility.GetControlID(FocusType.Passive);

                var evtType = Event.current.type;
                if ( (evtType == EventType.MouseMove || evtType == EventType.Layout)
                    && HandleUtility.nearestControl == m_NearestControl)
                    SplineHandleUtility.ResetLastHoveredElement();
            }
        }

        /// <summary>
        /// The color of sections of spline curve handles that are behind objects in the Scene view.
        /// </summary>
        public static Color lineBehindColor => SplineHandleUtility.lineBehindColor;

        /// <summary>
        /// The color of sections of spline curves handles that are in front of objects in the Scene view.
        /// </summary>
        public static Color lineColor => SplineHandleUtility.lineColor;

        /// <summary>
        /// The color of tangent handles for a spline.
        /// </summary>
        public static Color tangentColor => SplineHandleUtility.tangentColor;

        /// <summary>
        /// The distance to pick a spline knot, tangent, or curve handle at.
        /// </summary>
        public static float pickingDistance => SplineHandleUtility.pickingDistance;

        static List<int> s_ControlIDs = new();
        static List<int> s_CurveIDs = new();

        static readonly List<SelectableKnot> k_KnotBuffer = new ();
        static Dictionary<SelectableKnot, int> s_KnotsIDs = new ();

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

        /// <summary>
        /// Creates handles for a set of splines. These handles display the knots, tangents, and segments of a spline.
        /// These handles support selection and the direct manipulation of spline elements.
        /// </summary>
        /// <param name="splines">The set of splines to draw handles for.</param>
        public static void DoHandles(IReadOnlyList<SplineInfo> splines)
        {
            Profiler.BeginSample("SplineHandles.DoHandles");
            using (new SplineHandleScope())
            {
                // Drawing done in two separate passes to make sure the curves are drawn behind the spline elements.
                // Draw the curves.
                for (int i = 0; i < splines.Count; ++i)
                {
                    DoSegmentsHandles(splines[i]);
                }

                DoKnotsAndTangentsHandles(splines);
            }
            Profiler.EndSample();
        }

        internal static bool IsCurveId(int id)
        {
            return s_CurveIDs.Contains(id);
        }

        /// <summary>
        /// Creates knot and tangent handles for a spline. Call `DoKnotsAndTangentsHandles` in a `SplineHandleScope`.
        /// This method is used internally by `DoHandles`.
        /// </summary>
        /// <param name="spline">The spline to create knot and tangent handles for.</param>
        public static void DoKnotsAndTangentsHandles(SplineInfo spline)
        {
            SplineHandleUtility.UpdateElementColors();
            KnotHandles.ClearVisibleKnots();
            // Draw the spline elements.
            DrawSplineElements(spline);
            //Drawing knots on top of all other elements and above other splines
            KnotHandles.DrawVisibleKnots();
        }

        /// <summary>
        /// Creates knot and tangent handles for multiple splines. Call `DoKnotsAndTangentsHandles` in a `SplineHandleScope`.
        /// This method is used internally by `DoHandles`.
        /// </summary>
        /// <param name="splines">The splines to create knot and tangent handles for.</param>
        public static void DoKnotsAndTangentsHandles(IReadOnlyList<SplineInfo> splines)
        {
            SplineHandleUtility.UpdateElementColors();
            KnotHandles.ClearVisibleKnots();
            // Draw the spline elements.
            for (int i = 0; i < splines.Count; ++i)
                DrawSplineElements(splines[i]);
            //Drawing knots on top of all other elements and above other splines
            KnotHandles.DrawVisibleKnots();
        }

        /// <summary>
        /// Creates segment handles for a spline. Call `DoCurvesHandles` in a `SplineHandleScope`.
        /// This method is used internally by `DrawHandles`.
        /// </summary>
        /// <param name="splineInfo">The splineInfo of the spline to draw knots and tangents for.</param>
        public static void DoSegmentsHandles(SplineInfo splineInfo)
        {
            var spline = splineInfo.Spline;
            if (spline == null || spline.Count < 2)
                return;

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

            //Draw all the curves at once
            SplineCacheUtility.GetCachedPositions(spline, out var positions);

            using (new Handles.DrawingScope(SplineHandleUtility.lineColor, localToWorld))
            {
                using (new ZTestScope(CompareFunction.Less))
                    Handles.DrawAAPolyLine(SplineHandleUtility.denseLineAATex, 4f, positions);
            }

            using (new Handles.DrawingScope(SplineHandleUtility.lineBehindColor, localToWorld))
            {
                using (new ZTestScope(CompareFunction.Greater))
                    Handles.DrawAAPolyLine(SplineHandleUtility.denseLineAATex, 4f, positions);
            }

            if (drawHandlesAsActive)
            {
                for (int curveIndex = 0; curveIndex < lastIndex + 1; ++curveIndex)
                {
                    if (SplineHandleSettings.FlowDirectionEnabled && Event.current.type == EventType.Repaint)
                    {
                        var curve = spline.GetCurve(curveIndex).Transform(localToWorld);
                        CurveHandles.DrawFlow(curve, spline, curveIndex);
                    }
                }
            }

            for (int curveIndex = 0; curveIndex < lastIndex + 1; ++curveIndex)
            {
                CurveHandles.DrawWithHighlight(
                    s_ControlIDs[curveIndex],
                    spline,
                    curveIndex,
                    localToWorld,
                    new SelectableKnot(splineInfo, curveIndex),
                    new SelectableKnot(splineInfo, SplineUtility.NextIndex(curveIndex, spline.Count, spline.Closed)),
                    drawHandlesAsActive);
            }

            SplineHandleUtility.canDrawOnCurves = true;
        }

        static void DrawSplineElements(SplineInfo splineInfo)
        {
            var spline = splineInfo.Spline;
            var drawHandlesAsActive = !SplineSelection.HasActiveSplineSelection() || SplineSelection.Contains(splineInfo);

            if (drawHandlesAsActive)
            {
                for (int knotIndex = 0; knotIndex < spline.Count; ++knotIndex)
                    DrawKnotWithTangentsHandles_Internal(new SelectableKnot(splineInfo, knotIndex));
            }
            else
            {
                for (int knotIndex = 0; knotIndex < spline.Count; ++knotIndex)
                    KnotHandles.DrawInformativeKnot(new SelectableKnot(splineInfo, knotIndex));
            }
        }

        /// <summary>
        /// Creates handles for a knot and its tangents if those tangents are modifiable.
        /// These handles support the selection and direct manipulation of spline elements.
        /// Call `DoKnotWithTangentsHandles` in a `SplineHandleScope`.
        /// </summary>
        /// <param name="knot">The knot to draw handles for.</param>
        public static void DoKnotWithTangentsHandles(SelectableKnot knot)
        {
            KnotHandles.ClearVisibleKnots();
            DrawKnotWithTangentsHandles_Internal(knot);
            KnotHandles.DrawVisibleKnots();
        }

        static void DrawKnotWithTangentsHandles_Internal(SelectableKnot knot)
        {
            var splineInfo = knot.SplineInfo;
            if (SplineUtility.AreTangentsModifiable(splineInfo.Spline.GetTangentMode(knot.KnotIndex)))
                DoTangentsHandles(knot);

            DrawKnotHandles_Internal(knot);
        }

        /// <summary>
        /// Create handles for a knot. These handles the support selection and direct manipulation of spline elements.
        /// Call `DoKnotHandles` in a `SplineHandleScope`.
        /// </summary>
        /// <param name="knot">The knot to draw handles for.</param>
        public static void DoKnotHandles(SelectableKnot knot)
        {
            KnotHandles.ClearVisibleKnots();
            DrawKnotHandles_Internal(knot);
            KnotHandles.DrawVisibleKnots();
        }

        static void DrawKnotHandles_Internal(SelectableKnot knot)
        {
            var id = GetKnotID(knot);
            SelectionHandle(id, knot);
            KnotHandles.Draw(id, knot);
        }
        /// <summary>
        /// Create handles for a knot's tangents if those tangents are modifiable. `DoTangentsHandles` does not create handles for the knot.
        /// These handles support the selection and direct manipulation of the spline elements.
        /// Call `DoTangentsHandles` in a `SplineHandleScope`.
        /// </summary>
        /// <param name="knot">The knot to draw tangent handles for.</param>
        public static void DoTangentsHandles(SelectableKnot knot)
        {
            if(!knot.IsValid())
                return;

            var splineInfo = knot.SplineInfo;
            var spline = splineInfo.Spline;
            var knotIndex = knot.KnotIndex;

            var tangentIn = new SelectableTangent(splineInfo, knotIndex, BezierTangent.In);
            var tangentOut = new SelectableTangent(splineInfo, knotIndex, BezierTangent.Out);

            var controlIdIn = GUIUtility.GetControlID(FocusType.Passive);
            var controlIdOut = GUIUtility.GetControlID(FocusType.Passive);
            // Tangent In
            if (GUIUtility.hotControl == controlIdIn || SplineHandleUtility.ShouldShowTangent(tangentIn) && (spline.Closed || knotIndex != 0))
            {
                SelectionHandle(controlIdIn, tangentIn);
                TangentHandles.Draw(controlIdIn, tangentIn);
            }
            // Tangent Out
            if (GUIUtility.hotControl == controlIdOut || SplineHandleUtility.ShouldShowTangent(tangentOut) && (spline.Closed || knotIndex + 1 != spline.Count))
            {
                SelectionHandle(controlIdOut, tangentOut);
                TangentHandles.Draw(controlIdOut, tangentOut);
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
            where T : struct, ISelectableElement
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

                        GUI.changed = true;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && evt.button == 0)
                    {
                        if (!DirectManipulation.IsDragging)
                            SplineSelectionUtility.HandleSelection(element);

                        DirectManipulation.EndDrag();
                        GUI.changed = true;

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


        /// <summary>
        /// Draws handles for a knot. These handles are drawn only during repaint events and not on selection.
        /// </summary>
        /// <param name="knot">The <see cref="SelectableKnot"/> to create handles for.</param>
        /// <param name="selected">Set to true to draw the knot handle as a selected element.</param>
        /// <param name="hovered">Set to true to draw the knot handle as a hovered element.</param>
        public static void DrawKnot(SelectableKnot knot, bool selected = false, bool hovered = false)
            => DrawKnot(-1, knot, selected, hovered);

        /// <summary>
        /// Draws handles for a knot. These handles are drawn only during repaint events and not on selection.
        /// </summary>
        /// <param name="controlID">The controlID of the tangent to create handles for.</param>
        /// <param name="knot">The <see cref="SelectableKnot"/> to create handles for.</param>
        /// <param name="selected">Set to true to draw the knot handle as a selected element.</param>
        /// <param name="hovered">Set to true to draw the knot handle as a hovered element.</param>
        public static void DrawKnot(int controlID, SelectableKnot knot, bool selected = false, bool hovered = false)
        {
            KnotHandles.Do(controlID, knot, selected, hovered);
        }

        /// <summary>
        /// Draws handles for a tangent. These handles are drawn only during repaint events and not on selection.
        /// </summary>
        /// <param name="tangent">The <see cref="SelectableTangent"/> to create handles for.</param>
        /// <param name="selected">Set to true to draw the tangent handle as a selected element.</param>
        /// <param name="hovered">Set to true to draw the tangent handle as a hovered element.</param>
        public static void DrawTangent(SelectableTangent tangent, bool selected = false, bool hovered = false) => DrawTangent(-1, tangent, selected, hovered);

        /// <summary>
        /// Draws handles for a tangent. These handles are drawn only during repaint events and not on selection.
        /// </summary>
        /// <param name="controlID">The controlID of the tangent to create handles for.</param>
        /// <param name="tangent">The <see cref="SelectableTangent"/> to create handles for.</param>
        /// <param name="selected">Set to true to draw the tangent handle as a selected element.</param>
        /// <param name="hovered">Set to true to draw the tangent handle as a hovered element.</param>
        public static void DrawTangent(int controlID, SelectableTangent tangent, bool selected = false, bool hovered = false)
        {
            TangentHandles.Do(controlID, tangent, selected, hovered);
        }
    }
}
