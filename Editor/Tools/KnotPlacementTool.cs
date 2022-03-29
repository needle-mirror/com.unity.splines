using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
#if !UNITY_2020_2_OR_NEWER
using ToolManager = UnityEditor.EditorTools.EditorTools;
#endif

namespace UnityEditor.Splines
{
    [EditorTool("Place Spline Knots", typeof(ISplineProvider), typeof(SplineToolContext))]
    sealed class KnotPlacementTool : SplineTool
    {
        public override bool gridSnapEnabled => true;
        
        enum State
        {
            KnotPlacement,
            TangentPlacement,
            SplineClosure
        }
        
        const string k_DistanceAboveSurfacePrefKey = "KnotPlacementTool_DistanceAboveSurface";
        static float? s_DistanceAboveSurface;

        public override GUIContent toolbarIcon => PathIcons.knotPlacementTool;

        internal override SplineHandlesOptions handlesOptions
        {
            get
            {
                switch (m_State)
                {
                    case State.SplineClosure:
                    case State.TangentPlacement:
                        return SplineHandlesOptions.ShowTangents;
                    default:
                        return SplineHandlesOptions.KnotInsert | SplineHandlesOptions.ShowTangents;
                }
            }
        }

        readonly List<IEditableSpline> m_Splines = new List<IEditableSpline>();
        int m_ActiveSplineIndex;

        State m_State;
        Vector3 m_LastSurfacePoint;
        Vector3 m_LastSurfaceNormal;
        Vector3 m_CustomTangentOut;
        Plane m_KnotPlane;

        int m_StartId;
        int m_EndId;

        int m_AddKnotId;
        int m_ClosingKnotId;

        public override void OnActivated()
        {
            base.OnActivated();
            m_State = State.KnotPlacement;
            SplineToolContext.UseCustomSplineHandles(true);
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= OnSelectionChanged;
        }

        public override void OnWillBeDeactivated()
        {
            base.OnWillBeDeactivated();
            SplineToolContext.UseCustomSplineHandles(false);
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= OnSelectionChanged;
        }

        void OnSelectionChanged()
        {
            m_ActiveSplineIndex = -1;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            Event evt = Event.current;

            GetSelectedSplines(m_Splines);
            var spline = GetActiveSpline();

            m_StartId = GUIUtility.GetControlID(FocusType.Passive);
            m_AddKnotId = GUIUtility.GetControlID(FocusType.Passive);
            m_ClosingKnotId = GUIUtility.GetControlID(FocusType.Passive);
            var nearestControlIsSpline = HandleUtility.nearestControl == m_AddKnotId
                                            || HandleUtility.nearestControl == m_ClosingKnotId
                                            //If the spline is closed and the nearest control is not one that is define after in the splines
                                            || spline.closed && (HandleUtility.nearestControl < m_StartId || HandleUtility.nearestControl > m_EndId);
            var isMouseInWindow = new Rect(Vector2.zero, window.position.size).Contains(Event.current.mousePosition);

            bool canCloseActiveSpline = false;
            for(int i = 0; i < m_Splines.Count; ++i)
            {
                var active = SplineHandles.DrawSplineHandles(
                    m_Splines[i],
                    handlesOptions,
                    i == m_ActiveSplineIndex && nearestControlIsSpline || !isMouseInWindow);

                if(active)
                    canCloseActiveSpline = m_Splines[i] == spline;
            }
            m_EndId = GUIUtility.GetControlID(FocusType.Passive);
            
            DoClosingKnot(spline, canCloseActiveSpline);

            if (!spline.closed)
                DoKnotSurfaceAddHandle(m_AddKnotId, spline, isMouseInWindow);

            if(spline.closed && evt.type == EventType.MouseMove)
                HandleUtility.Repaint();

            SplineConversionUtility.ApplyEditableSplinesIfDirty(targets);

            if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Escape || evt.keyCode == KeyCode.Return))
            {
                ClearTangentPlacementData();
                ToolManager.SetActiveTool<SplineMoveTool>();
            }
        }

        void ClearTangentPlacementData()
        {
            m_State = State.KnotPlacement;
            m_LastSurfacePoint = Vector3.zero;
            m_LastSurfaceNormal = Vector3.zero;
            m_CustomTangentOut = Vector3.zero;
        }

        void DoClosingKnot(IEditableSpline spline, bool active)
        {
            if (m_State != State.TangentPlacement && spline.knotCount >= 3 && spline.canBeClosed && !spline.closed)
            {
                if (HandleUtility.nearestControl == m_ClosingKnotId || m_State == State.SplineClosure)
                {
                    Event evt = Event.current;
                    switch (evt.GetTypeForControl(m_ClosingKnotId))
                    {
                        case EventType.Repaint:
                            if (!Tools.viewToolActive)
                            {
                                var firstKnot = spline.GetKnot(0);
                                if (firstKnot.tangentCount > 1)
                                {
                                    var tangentOut = firstKnot.GetTangent(1).direction;
                                    DrawPreviewCurveForNewEndKnot(spline, firstKnot.position, tangentOut, m_LastSurfaceNormal, m_ClosingKnotId, true);
                                }
                                else
                                {
                                    var tangentOut = float3.zero;
                                    DrawPreviewCurveForNewEndKnot(spline, firstKnot.position, tangentOut, m_LastSurfaceNormal, m_ClosingKnotId, true);
                                }
                            }
                            break;
                        
                        case EventType.MouseDown:
                            m_State = State.SplineClosure;
                            break;
                    }
                }
                
                EditableKnot knot = spline.GetKnot(0);
                if (SplineHandles.ButtonHandle(m_ClosingKnotId, knot, active))
                {
                    EditableSplineUtility.CloseSpline(spline);
                    m_State = State.KnotPlacement;
                }
            }
        }

        void  DoKnotSurfaceAddHandle(int controlID, IEditableSpline spline, bool isMouseInWindow)
        {
            if (spline == null)
                return;

            Event evt = Event.current;
            switch (evt.GetTypeForControl(controlID))
            {
                case EventType.Layout:
                    if (!Tools.viewToolActive)
                        HandleUtility.AddDefaultControl(controlID);
                    break;

                case EventType.Repaint:
                    if (HandleUtility.nearestControl == controlID && !Tools.viewToolActive)
                    {
                        // Draw curve preview if we're placing tangents. Otherwise, draw it only if the cursor's ray is intersecting with a surface.
                        if(m_State == State.TangentPlacement ||
                            ( m_State == State.KnotPlacement &&
                                isMouseInWindow &&
                                SplineHandleUtility.GetPointOnSurfaces(evt.mousePosition, out m_LastSurfacePoint, out m_LastSurfaceNormal) ))
                        {
                            //todo enable this after PR lands
//#if UNITY_2022_2_OR_NEWER
                            //if(EditorSnapSettings.incrementalSnapActive)
                            //    m_LastSurfacePoint = SplineHandleUtility.DoIncrementSnap(m_LastSurfacePoint, spline.GetKnot(spline.knotCount - 1).position);
//#endif
                            DrawPreviewCurveForNewEndKnot(spline, m_LastSurfacePoint, m_CustomTangentOut, m_LastSurfaceNormal, controlID);
                        }
                    }
        
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == controlID && GUIUtility.hotControl == 0 && evt.button == 0)
                    {
                        GUIUtility.hotControl = controlID;
                        evt.Use();

                        if (SplineHandleUtility.GetPointOnSurfaces(evt.mousePosition, out m_LastSurfacePoint, out m_LastSurfaceNormal))
                        {
                            //todo enable this after PR lands
//#if UNITY_2022_2_OR_NEWER
//                            if(EditorSnapSettings.incrementalSnapActive)
//                                m_LastSurfacePoint = SplineHandleUtility.DoIncrementSnap(m_LastSurfacePoint, spline.GetKnot(spline.knotCount - 1).position);
//#endif
                            
                            m_KnotPlane = new Plane(m_LastSurfaceNormal, m_LastSurfacePoint);
                            if (spline.tangentsPerKnot > 0)
                                m_State = State.TangentPlacement;
                        }
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        evt.Use();

                        if (evt.button == 0)
                        {
                            GUIUtility.hotControl = 0;

                            if (SplineHandleUtility.GetPointOnSurfaces(evt.mousePosition, out Vector3 _, out Vector3 _))
                            {
                                m_LastSurfacePoint = SplineHandleUtility.RoundBasedOnMinimumDifference(m_LastSurfacePoint);
                                
                                //todo enable this after PR lands
//#if UNITY_2022_2_OR_NEWER
//                                if(EditorSnapSettings.incrementalSnapActive)
//                                    m_LastSurfacePoint = SplineHandleUtility.DoIncrementSnap(m_LastSurfacePoint, spline.GetKnot(spline.knotCount - 1).position);
//#endif
                                // Check component count to ensure that we only move the transform of a newly created
                                // spline. I.e., we don't want to move a GameObject that has other components like
                                // a MeshRenderer, for example.
                                if (spline.knotCount < 1
                                    && spline.conversionTarget is Component component
                                    && component.gameObject.GetComponents<Component>().Length == 2)
                                    component.transform.position = m_LastSurfacePoint;

                                EditableSplineUtility.AddPointToEnd(spline, m_LastSurfacePoint, m_LastSurfaceNormal, m_CustomTangentOut);
                            }

                            ClearTangentPlacementData();
                        }
                    }

                    break;

                case EventType.MouseMove:
                    if (HandleUtility.nearestControl == controlID)
                        HandleUtility.Repaint();

                    break;

                case EventType.MouseDrag:
                    if (m_State == State.TangentPlacement && GUIUtility.hotControl == controlID && evt.button == 0)
                    {
                        evt.Use();
                        var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                        if (m_KnotPlane.Raycast(ray, out float distance))
                            m_CustomTangentOut = (ray.origin + ray.direction * distance) - m_LastSurfacePoint;
                    }
                    break;
            }
        }

        void DrawPreviewCurveForNewEndKnot(IEditableSpline spline, float3 point, float3 tangentOut, float3 normal, int knotHandleId, bool isClosingCurve = false)
        {
            var previewCurve = spline.GetPreviewCurveForEndKnot(point, normal, tangentOut);

            if (spline.knotCount > 0)
                SplineHandles.CurveHandleCap(previewCurve, -1, EventType.Repaint, m_State != State.TangentPlacement);

            if (!isClosingCurve)
            {
                for(int i = 0; i < previewCurve.b.tangentCount; ++i)
                {
                    var tangent = previewCurve.b.GetTangent(i);
                    if(math.length(tangent.localPosition) > 0)
                        SplineHandles.DrawTangentHandle(tangent);
                }

                // In addition, display the normally hidden tangent out of the last knot.
                // It gives an impression of an issue when it's hidden but the preview knot shows both tangents.
                if (spline.knotCount > 0 && previewCurve.a.tangentCount > 0)
                    SplineHandles.DrawTangentHandle(previewCurve.a.GetTangent(previewCurve.a.tangentCount - 1));

                SplineHandles.DrawKnotHandle(m_LastSurfacePoint, previewCurve.b.rotation, false, knotHandleId);
            }
            else
            {
                var firstKnot = spline.GetKnot(0);
                if (firstKnot.tangentCount > 1)
                {
                    SplineHandles.DrawTangentHandle(firstKnot.GetTangent(0));

                    if (spline.knotCount > 0 && previewCurve.a.tangentCount > 0)
                        SplineHandles.DrawTangentHandle(previewCurve.a.GetTangent(previewCurve.a.tangentCount - 1));
                }
            }
        }

        void GetSelectedSplines(List<IEditableSpline> results)
        {
            results.Clear();
            foreach (var t in targets)
            {
                IReadOnlyList<IEditableSpline> paths = EditableSplineManager.GetEditableSplines(t);
                if (paths == null)
                    continue;

                for (int i = 0; i < paths.Count; ++i)
                {
                    results.AddRange(paths);
                }
            }
        }

        void CycleActiveSpline()
        {
            m_ActiveSplineIndex = (m_ActiveSplineIndex + 1) % m_Splines.Count;
            SceneView.RepaintAll();
        }

        protected override IEditableSpline GetActiveSpline()
        {
            IEditableSpline spline;

            if(m_ActiveSplineIndex == -1)
            {
                spline = base.GetActiveSpline();
                m_ActiveSplineIndex = m_Splines.IndexOf(spline);
            }
            else
                spline = m_Splines[m_ActiveSplineIndex];

            return spline;
        }

        [Shortcut("Splines/Cycle Active Spline", typeof(SceneView), KeyCode.S)]
        static void ShortcutCycleActiveSpline(ShortcutArguments args)
        {
            if(m_ActiveTool is KnotPlacementTool tool)
                tool.CycleActiveSpline();
        }
    }
}
