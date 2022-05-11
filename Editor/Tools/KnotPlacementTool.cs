using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEditor.ShortcutManagement;
using Object = UnityEngine.Object;
#if !UNITY_2020_2_OR_NEWER
using ToolManager = UnityEditor.EditorTools.EditorTools;
#endif

#if UNITY_2022_1_OR_NEWER
using UnityEditor.Overlays;
#else
using System.Linq;
using System.Reflection;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;
#endif

namespace UnityEditor.Splines
{
    [CustomEditor(typeof(KnotPlacementTool))]
#if UNITY_2022_1_OR_NEWER
    class KnotPlacementToolSettings : UnityEditor.Editor, ICreateToolbar
    {
        public IEnumerable<string> toolbarElements
        {
#else
    class KnotPlacementToolSettings : CreateToolbarBase
    {
        protected override IEnumerable<string> toolbarElements
        {
#endif
            get
            {
                yield return "Spline Tool Settings/Default Knot Type";
            }
        }
    }

    [EditorTool("Draw Spline", typeof(ISplineContainer), typeof(SplineToolContext))]
    sealed class KnotPlacementTool : SplineTool
    {
        sealed class DrawingOperation : IDisposable
        {
            public enum DrawingDirection
            {
                Start,
                End
            }

            public readonly SplineInfo CurrentSplineInfo;

            readonly DrawingDirection m_Direction;
            readonly bool m_AllowDeleteIfNoCurves;

            int GetLastKnotIndex()
            {
                var isFromStartAndClosed = m_Direction == DrawingDirection.Start && CurrentSplineInfo.Spline.Closed;
                var isFromEndAndOpened = m_Direction == DrawingDirection.End && !CurrentSplineInfo.Spline.Closed;
                return isFromStartAndClosed || isFromEndAndOpened  ? (CurrentSplineInfo.Spline.Count - 1) : 0 ;
            }

            SelectableKnot GetLastAddedKnot()
            {
                return new SelectableKnot(CurrentSplineInfo, GetLastKnotIndex());
            }

            public DrawingOperation(SplineInfo splineInfo, DrawingDirection direction, bool allowDeleteIfNoCurves)
            {
                CurrentSplineInfo = splineInfo;
                m_Direction = direction;
                m_AllowDeleteIfNoCurves = allowDeleteIfNoCurves;
            }

            public void OnGUI(IReadOnlyList<SplineInfo> splines)
            {
                KnotPlacementHandle(splines, CreateKnotOnKnot, CreateKnotOnSurface, DrawPreview);
            }

            void UncloseSplineIfNeeded()
            {
                // If the spline was closed, we unclose it, create a knot on the last knot and connect the first and last
                if (CurrentSplineInfo.Spline.Closed)
                {
                    CurrentSplineInfo.Spline.Closed = false;

                    switch (m_Direction)
                    {
                        case DrawingDirection.Start:
                        {
                            var lastKnot = new SelectableKnot(CurrentSplineInfo, CurrentSplineInfo.Spline.Count - 1);
                            var normal = math.mul(lastKnot.Rotation, math.up());
                            EditorSplineUtility.AddKnotToTheStart(CurrentSplineInfo, lastKnot.Position, normal, -lastKnot.TangentOut.Direction);
                            //Adding the knot before the first element is shifting indexes using a callback
                            //Using a delay called here to be certain that the indexes has been shift and that this new link won't be shifted
                            EditorApplication.delayCall += () => EditorSplineUtility.LinkKnots(new SelectableKnot(CurrentSplineInfo, 1), new SelectableKnot(CurrentSplineInfo, CurrentSplineInfo.Spline.Count -1));
                            break;
                        }

                        case DrawingDirection.End:
                        {
                            var firstKnot = new SelectableKnot(CurrentSplineInfo, 0);
                            var normal = math.mul(firstKnot.Rotation, math.up());
                            EditorSplineUtility.AddKnotToTheEnd(CurrentSplineInfo, firstKnot.Position, normal, -firstKnot.TangentIn.Direction);
                            EditorSplineUtility.LinkKnots(new SelectableKnot(CurrentSplineInfo, 0), new SelectableKnot(CurrentSplineInfo, CurrentSplineInfo.Spline.Count - 1));
                            break;
                        }
                    }
                }
            }

            void CreateKnotOnKnot(SelectableKnot knot, float3 tangentOut)
            {
                EditorSplineUtility.RecordObject(CurrentSplineInfo, "Draw Spline");

                if (knot.Equals(GetLastAddedKnot()))
                    return;

                // If the user clicks on the first knot (or a knot linked to the first knot) of the spline close the spline
                var closeKnotIndex = m_Direction == DrawingDirection.End ? 0 : knot.SplineInfo.Spline.Count-1;
                if (knot.SplineInfo.Equals(CurrentSplineInfo)
                    && (knot.KnotIndex == closeKnotIndex || EditorSplineUtility.AreKnotLinked(knot, new SelectableKnot(CurrentSplineInfo, closeKnotIndex))))
                {
                    knot.SplineInfo.Spline.Closed = true;
                    //When using a Bezier Tangent, break the mode on the first knot to keep both tangents
                    if (knot.Mode != EditorSplineUtility.DefaultTangentMode || math.lengthsq(tangentOut) > float.Epsilon)
                        knot.Mode = TangentMode.Broken;

                    SelectableTangent tangent;
                    switch (m_Direction)
                    {
                        case DrawingDirection.Start:
                            tangent = new SelectableTangent(knot.SplineInfo, closeKnotIndex, BezierTangent.Out);
                            break;

                        case DrawingDirection.End:
                            tangent = new SelectableTangent(knot.SplineInfo, closeKnotIndex, BezierTangent.In);
                            break;

                        default:
                            tangent = default;
                            break;
                    }

                    tangent.Direction = -tangentOut;
                }
                else
                {
                    UncloseSplineIfNeeded();

                    AddKnot(knot.Position, math.mul(knot.Rotation, math.up()), tangentOut);
                    //DelayCall necessary when m_Direction = DrawingDirection.Start
                    //As adding the knot before the first element is shifting indexes using a callback
                    //Using a delay called here to be certain that the indexes has been shift and that this new link won't be shifted
                    if(m_Direction == DrawingDirection.End)
                        EditorSplineUtility.LinkKnots(knot, GetLastAddedKnot());
                    else
                        EditorApplication.delayCall += () => EditorSplineUtility.LinkKnots(new SelectableKnot(knot.SplineInfo, knot.KnotIndex+1), GetLastAddedKnot());
                }
            }

            void CreateKnotOnSurface(float3 position, float3 normal, float3 tangentOut)
            {
                EditorSplineUtility.RecordObject(CurrentSplineInfo, "Draw Spline");

                var lastKnot = GetLastAddedKnot();

                if (lastKnot.IsValid())
                    position = ApplyIncrementalSnap(position, lastKnot.Position);

                UncloseSplineIfNeeded();

                AddKnot(position, normal, tangentOut);
            }

            void AddKnot(float3 position, float3 normal, float3 tangentOut)
            {
                switch (m_Direction)
                {
                    case DrawingDirection.Start:
                        EditorSplineUtility.AddKnotToTheStart(CurrentSplineInfo, position, normal, tangentOut);
                        break;

                    case DrawingDirection.End:
                        EditorSplineUtility.AddKnotToTheEnd(CurrentSplineInfo, position, normal, tangentOut);
                        break;
                }
            }


            void DrawPreview(float3 position, float3 normal, float3 tangent, SelectableKnot target)
            {
                if (!Mathf.Approximately(math.length(tangent), 0))
                {
                    TangentHandles.Draw(position - tangent, position);
                    TangentHandles.Draw(position + tangent, position);
                }

                var lastKnot = GetLastAddedKnot();
                if (target.IsValid() && target.Equals(lastKnot))
                    return;

                var mode = EditorSplineUtility.GetModeFromPlacementTangent(tangent);
                position = ApplyIncrementalSnap(position, lastKnot.Position);

                if (mode == TangentMode.AutoSmooth)
                    tangent = SplineUtility.GetCatmullRomTangent(lastKnot.Position, position);

                BezierCurve previewCurve = m_Direction == DrawingDirection.Start
                    ? EditorSplineUtility.GetPreviewCurveFromStart(CurrentSplineInfo, lastKnot.KnotIndex, position, tangent, mode)
                    : EditorSplineUtility.GetPreviewCurveFromEnd(CurrentSplineInfo, lastKnot.KnotIndex, position, tangent, mode);

                CurveHandles.DrawPreview(previewCurve);

                if (EditorSplineUtility.AreTangentsModifiable(lastKnot.Mode) && CurrentSplineInfo.Spline.Count > 0)
                    TangentHandles.DrawInformativeTangent(previewCurve.P1, previewCurve.P0);

                KnotHandles.Draw(position, SplineUtility.GetKnotRotation(tangent, normal), SplineHandleUtility.knotColor, false, false);
            }

            public void Dispose()
            {
                EditorSplineUtility.RecordObject(CurrentSplineInfo, "Finalize Spline Drawing");

                var spline = CurrentSplineInfo.Spline;
                //Remove drawing action that created no curves and were canceled after being created
                if (m_AllowDeleteIfNoCurves && spline != null && spline.Count == 1)
                    CurrentSplineInfo.Container.RemoveSplineAt(CurrentSplineInfo.Index);
            }
        }

#if UNITY_2022_2_OR_NEWER
        public override bool gridSnapEnabled => true;
#endif

        public override GUIContent toolbarIcon => PathIcons.knotPlacementTool;

        static bool IsMouseInWindow(EditorWindow window) => new Rect(Vector2.zero, window.position.size).Contains(Event.current.mousePosition);

        static PlacementData s_PlacementData;

        int m_ActiveObjectIndex = 0;
        readonly List<Object> m_SortedTargets = new List<Object>();
        readonly List<SplineInfo> m_SplineBuffer = new List<SplineInfo>(4);
        static readonly List<SelectableKnot> s_KnotsBuffer = new List<SelectableKnot>();
        DrawingOperation m_CurrentDrawingOperation;
        Object m_MainTarget;

        public override void OnActivated()
        {
            base.OnActivated();
            SplineToolContext.UseCustomSplineHandles(true);
            SplineSelection.UpdateObjectSelection(targets);
            m_ActiveObjectIndex = 0;
        }

        public override void OnWillBeDeactivated()
        {
            base.OnWillBeDeactivated();
            SplineToolContext.UseCustomSplineHandles(false);
            EndDrawingOperation();
        }

        public override void OnToolGUI(EditorWindow window)
        {
            var targets = GetSortedTargets(out m_MainTarget);
            var allSplines = EditorSplineUtility.GetSplinesFromTargetsInternal(targets);

            //If the spline being drawn on doesn't exist anymore, end the drawing operation
            if (m_CurrentDrawingOperation != null && (!allSplines.Contains(m_CurrentDrawingOperation.CurrentSplineInfo) || m_CurrentDrawingOperation.CurrentSplineInfo.Spline.Count == 0))
                EndDrawingOperation();

            DrawSplines(targets, allSplines, m_MainTarget);

            m_CurrentDrawingOperation?.OnGUI(allSplines);

            if (m_CurrentDrawingOperation == null)
                KnotPlacementHandle(allSplines, BeginDrawingOperation, BeginDrawingOperation, DrawCreationPreview);

            HandleCancellation();
        }

        void DrawSplines(IReadOnlyList<Object> targets, IReadOnlyList<SplineInfo> allSplines, Object mainTarget)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            EditorSplineUtility.TryGetNearestKnot(allSplines, Event.current.mousePosition, out SelectableKnot hoveredKnot);

            foreach (var target in targets)
            {
                EditorSplineUtility.GetSplinesFromTarget(target, m_SplineBuffer);
                bool isMainTarget = target == mainTarget;

                //Draw curves
                foreach (var splineInfo in m_SplineBuffer)
                {
                    var spline = splineInfo.Spline;
                    var localToWorld = splineInfo.LocalToWorld;

                    for (int i = 0, count = spline.GetCurveCount(); i < count; ++i)
                        CurveHandles.Draw(spline.GetCurve(i).Transform(localToWorld), isMainTarget);
                }

                //Draw knots
                foreach (var splineInfo in m_SplineBuffer)
                {
                    var spline = splineInfo.Spline;
                    var localToWorld = splineInfo.LocalToWorld;

                    for (int knotIndex = 0, count = spline.Count; knotIndex < count; ++knotIndex)
                    {
                        var knot = spline[knotIndex].Transform(localToWorld);
                        bool isHovered = hoveredKnot.SplineInfo.Equals(splineInfo) && hoveredKnot.KnotIndex == knotIndex;
                        var color = isMainTarget || isHovered ? SplineHandleUtility.knotColor : SplineHandleUtility.knotColor * .4f;
                        KnotHandles.Draw(knot.Position, knot.Rotation, color, false, isHovered);

                        if(EditorSplineUtility.AreTangentsModifiable(spline.GetTangentMode(knotIndex)))
                        {
                            //Tangent In
                            if(spline.Closed || knotIndex != 0)
                            {
                                var tangentIn = new SelectableTangent(splineInfo, knotIndex, BezierTangent.In);
                                TangentHandles.DrawInformativeTangent(tangentIn, isMainTarget);
                            }

                            //Tangent Out
                            if(spline.Closed || knotIndex + 1 != spline.Count)
                            {
                                var tangentOut = new SelectableTangent(splineInfo, knotIndex, BezierTangent.Out);
                                TangentHandles.DrawInformativeTangent(tangentOut, isMainTarget);
                            }
                        }
                    }
                }
            }
        }

        void BeginDrawingOperation(SelectableKnot startFrom, float3 tangent)
        {
            Undo.RecordObject(startFrom.SplineInfo.Target, "Draw Spline");

            EndDrawingOperation();

            // If we start from one of the ends of the spline we just append to that spline unless the spline is already closed.
            // We check all the knots to see if we can extend one of the spline instead of adding a new one
            if (!startFrom.SplineInfo.Spline.Closed)
            {
                EditorSplineUtility.GetKnotLinks(startFrom, s_KnotsBuffer);
                s_KnotsBuffer.Sort((a, b) => a.SplineInfo.Index.CompareTo(b.SplineInfo.Index)); //Prefer earlier splines in the list

                foreach (var c in s_KnotsBuffer)
                {
                    if (EditorSplineUtility.IsEndKnot(c))
                    {
                        if (math.lengthsq(tangent) > float.Epsilon)
                        {
                            var current = c;
                            var tOut = current.TangentOut;
                            current.Mode = TangentMode.Broken;
                            tOut.Direction = tangent;
                        }

                        m_CurrentDrawingOperation = new DrawingOperation(startFrom.SplineInfo, DrawingOperation.DrawingDirection.End, false);
                        return;
                    }

                    if (c.KnotIndex == 0)
                    {
                        if (math.lengthsq(tangent) > float.Epsilon)
                        {
                            var current = c;
                            var tIn = current.TangentIn;
                            current.Mode = TangentMode.Broken;
                            tIn.Direction = tangent;
                        }

                        m_CurrentDrawingOperation = new DrawingOperation(startFrom.SplineInfo, DrawingOperation.DrawingDirection.Start, false);
                        return;
                    }
                }
            }

            // Otherwise we start a new spline
            var knot = EditorSplineUtility.CreateSpline(startFrom, tangent);
            EditorSplineUtility.LinkKnots(knot, startFrom);
            m_CurrentDrawingOperation = new DrawingOperation(knot.SplineInfo, DrawingOperation.DrawingDirection.End, true);
        }

        void BeginDrawingOperation(float3 position, float3 normal, float3 tangentOut)
        {
            Undo.RecordObject(m_MainTarget, "Draw Spline");

            EndDrawingOperation();

            var container = (ISplineContainer)m_MainTarget;

            // Check component count to ensure that we only move the transform of a newly created
            // spline. I.e., we don't want to move a GameObject that has other components like
            // a MeshRenderer, for example.
            if ((container.Splines.Count == 1 && container.Splines[0].Count == 0
                || container.Splines.Count == 0)
                && ((Component)m_MainTarget).GetComponents<Component>().Length == 2)
            {
                ((Component)m_MainTarget).transform.position = position;
            }

            SplineInfo splineInfo;

            // Spline gets created with an empty spline so we add to that spline first if needed
            if (container.Splines.Count == 1 && container.Splines[0].Count == 0)
                splineInfo = new SplineInfo(container, 0);
            else
                splineInfo = EditorSplineUtility.CreateSpline(container);

            EditorSplineUtility.AddKnotToTheEnd(splineInfo, position, normal, tangentOut);
            m_CurrentDrawingOperation = new DrawingOperation(splineInfo, DrawingOperation.DrawingDirection.End, false);
        }

        void DrawCreationPreview(float3 position, float3 normal, float3 tangentOut, SelectableKnot target)
        {
            if (!Mathf.Approximately(math.length(tangentOut), 0))
                TangentHandles.Draw(position + tangentOut, position);

            KnotHandles.Draw(position, SplineUtility.GetKnotRotation(tangentOut, normal), SplineHandleUtility.knotColor, false, false);
        }

        static void KnotPlacementHandle(
            IReadOnlyList<SplineInfo> splines,
            Action<SelectableKnot, float3> createKnotOnKnot,
            Action<float3, float3, float3> createKnotOnSurface,
            Action<float3, float3, float3, SelectableKnot> drawPreview)
        {
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var evt = Event.current;

            if (s_PlacementData != null && GUIUtility.hotControl != controlId)
                s_PlacementData = null;

            switch (evt.GetTypeForControl(controlId))
            {
                case EventType.Layout:
                    if (!Tools.viewToolActive)
                        HandleUtility.AddDefaultControl(controlId);
                    break;

                case EventType.Repaint:
                {
                    var mousePosition = Event.current.mousePosition;
                    if (GUIUtility.hotControl == 0
                        && SceneView.currentDrawingSceneView != null
                        && !IsMouseInWindow(SceneView.currentDrawingSceneView))
                        break;

                    if (GUIUtility.hotControl == 0)
                    {
                        if (EditorSplineUtility.TryGetNearestKnot(splines, mousePosition, out SelectableKnot knot))
                        {
                            drawPreview.Invoke(knot.Position, math.rotate(knot.Rotation, math.up()), float3.zero, knot);
                        }
                        else if (EditorSplineUtility.TryGetNearestPositionOnCurve(splines, mousePosition, out SplineCurveHit hit))
                        {
                            drawPreview.Invoke(hit.Position, hit.Normal, float3.zero, default);
                        }
                        else if (SplineHandleUtility.GetPointOnSurfaces(mousePosition, out Vector3 position, out Vector3 normal))
                        {
                            drawPreview.Invoke(position, normal, float3.zero, default);
                        }
                    }

                    if (s_PlacementData != null)
                    {
                        var knotPosition = s_PlacementData.Position;
                        var tangentOut = s_PlacementData.TangentOut;

                        if (!Mathf.Approximately(math.length(s_PlacementData.TangentOut), 0))
                        {
                            TangentHandles.Draw(knotPosition - tangentOut, knotPosition);
                            TangentHandles.Draw(knotPosition + tangentOut, knotPosition);
                        }

                        drawPreview.Invoke(s_PlacementData.Position, s_PlacementData.Normal, s_PlacementData.TangentOut, default);
                    }

                    break;
                }

                case EventType.MouseMove:
                    if (HandleUtility.nearestControl == controlId)
                        HandleUtility.Repaint();
                    break;

                case EventType.MouseDown:
                {
                    if (evt.button != 0)
                        break;

                    if (HandleUtility.nearestControl == controlId)
                    {
                        GUIUtility.hotControl = controlId;
                        evt.Use();

                        var mousePosition = Event.current.mousePosition;
                        if (EditorSplineUtility.TryGetNearestKnot(splines, mousePosition, out SelectableKnot knot))
                        {
                            s_PlacementData = new KnotPlacementData(knot);
                        }

                        else if (EditorSplineUtility.TryGetNearestPositionOnCurve(splines, mousePosition, out SplineCurveHit hit))
                        {
                            s_PlacementData = new CurvePlacementData(hit);
                        }

                        else if (SplineHandleUtility.GetPointOnSurfaces(mousePosition, out Vector3 position, out Vector3 normal))
                        {
                            s_PlacementData = new PlacementData(position, normal);
                        }
                    }

                    break;
                }

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId && evt.button == 0)
                    {
                        evt.Use();

                        if (s_PlacementData != null)
                        {
                            var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                            if (s_PlacementData.Plane.Raycast(ray, out float distance))
                            {
                                s_PlacementData.TangentOut = (ray.origin + ray.direction * distance) - s_PlacementData.Position;
                            }
                        }
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId && evt.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        if (s_PlacementData != null)
                        {
                            var linkedKnot = s_PlacementData.GetOrCreateLinkedKnot();
                            if(linkedKnot.IsValid())
                                createKnotOnKnot.Invoke(linkedKnot, s_PlacementData.TangentOut);
                            else
                                createKnotOnSurface.Invoke(s_PlacementData.Position, s_PlacementData.Normal, s_PlacementData.TangentOut);

                            s_PlacementData = null;
                        }
                    }
                    break;

                case EventType.KeyDown:
                    if (GUIUtility.hotControl == controlId && (evt.keyCode == KeyCode.Escape || evt.keyCode == KeyCode.Return))
                    {
                        s_PlacementData = null;
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;
            }
        }

        void HandleCancellation()
        {
            var evt = Event.current;
            if (GUIUtility.hotControl == 0 && evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.Return)
                {
                    //If we are currently drawing, end the drawing operation and start a new one. If we haven't started drawing, switch to move tool instead
                    if (m_CurrentDrawingOperation != null)
                        EndDrawingOperation();
                    else
                        ToolManager.SetActiveTool<SplineMoveTool>();
                }

#if !UNITY_2022_1_OR_NEWER
                //For 2022.1 and after the ESC key is handled in the EditorToolManager
                if (evt.keyCode == KeyCode.Escape)
                    ToolManager.SetActiveTool<SplineMoveTool>();
#endif
            }
        }

        void EndDrawingOperation()
        {
            m_CurrentDrawingOperation?.Dispose();
            m_CurrentDrawingOperation = null;
        }

        IReadOnlyList<Object> GetSortedTargets(out Object mainTarget)
        {
            m_SortedTargets.Clear();
            m_SortedTargets.AddRange(targets);

            if (m_ActiveObjectIndex >= m_SortedTargets.Count)
                m_ActiveObjectIndex = 0;

            mainTarget = m_CurrentDrawingOperation != null ? m_CurrentDrawingOperation.CurrentSplineInfo.Target : m_SortedTargets[m_ActiveObjectIndex];

            // Move main target to the end for rendering/picking
            m_SortedTargets.Remove(mainTarget);
            m_SortedTargets.Add(mainTarget);

            return m_SortedTargets;
        }

        static Vector3 ApplyIncrementalSnap(Vector3 current, Vector3 origin)
        {
#if UNITY_2022_2_OR_NEWER
            if (EditorSnapSettings.incrementalSnapActive)
                return SplineHandleUtility.DoIncrementSnap(current, origin);
#endif
            return current;
        }

        void CycleActiveTarget()
        {
            m_ActiveObjectIndex = (m_ActiveObjectIndex + 1) % targets.Count();
            SceneView.RepaintAll();
        }

        [Shortcut("Splines/Cycle Active Spline", typeof(SceneView), KeyCode.S)]
        static void ShortcutCycleActiveSpline(ShortcutArguments args)
        {
            if (activeTool is KnotPlacementTool tool)
                tool.CycleActiveTarget();
        }
    }
}