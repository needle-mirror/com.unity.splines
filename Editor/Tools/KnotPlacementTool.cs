using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Splines;
#if !UNITY_2020_2_OR_NEWER
using ToolManager = UnityEditor.EditorTools.EditorTools;
#endif

namespace UnityEditor.Splines
{
    [EditorTool("Place Spline Knots", typeof(ISplineProvider))]
    sealed class KnotPlacementTool : SplineTool
    {
        const string k_DistanceAboveSurfacePrefKey = "KnotPlacementTool_DistanceAboveSurface";
        static float? s_DistanceAboveSurface;
        
        public override GUIContent toolbarIcon => PathIcons.knotPlacementTool;

        static float distanceAboveSurface
        {
            get
            {
                if (s_DistanceAboveSurface == null)
                    s_DistanceAboveSurface = EditorPrefs.GetFloat(k_DistanceAboveSurfacePrefKey, 0f);

                return s_DistanceAboveSurface.Value;
            }
            set
            {
                s_DistanceAboveSurface = value;
                EditorPrefs.SetFloat(k_DistanceAboveSurfacePrefKey, value);
            }
        }

        internal override SplineHandlesOptions handlesOptions => SplineHandlesOptions.KnotInsert|SplineHandlesOptions.ShowTangents;

        readonly List<IEditableSpline> m_Splines = new List<IEditableSpline>();
        int m_ActiveSplineIndex;
        
        float m_CurveT;
        bool m_HasClosestCurvePoint;

        Vector3 m_PointOnSurface;
        Vector3 m_NormalOfSurface;
        bool m_HasPointOnSurface;
        
        public override void OnActivated()
        {
            base.OnActivated();
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

        int m_AddControlId;
        
        public override void OnToolGUI(EditorWindow window)
        {
            Event evt = Event.current;

            GetSelectedSplines(m_Splines);
            var spline = GetActiveSpline();
            
            for (int i = 0; i < m_Splines.Count; ++i)
                SplineHandles.DrawSplineHandles(m_Splines[i], handlesOptions, i == m_ActiveSplineIndex && HandleUtility.nearestControl == m_AddControlId);
            
            if (new Rect(Vector2.zero, window.position.size).Contains(Event.current.mousePosition) && !spline.closed)
                m_AddControlId = SplineHandles.KnotSurfaceAddHandle(spline, distanceAboveSurface);

            SplineConversionUtility.ApplyEditableSplinesIfDirty(targets);

            if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Escape || evt.keyCode == KeyCode.Return))
            {
                ToolManager.SetActiveTool<SplineMoveTool>();
            }
        }
        
        bool CanBeClosed(IEditableSpline spline)
        {
            return spline.canBeClosed && !spline.closed && spline.knotCount > 1;
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

        [Shortcut("Splines/Cycle Active Spline", typeof(InternalEditorBridge.ShortcutContext), KeyCode.S)]
        static void ShortcutCycleActiveSpline(ShortcutArguments args)
        {
            if(args.context == m_ShortcutContext && m_ShortcutContext.context is KnotPlacementTool)
            {
                ( m_ShortcutContext.context as KnotPlacementTool ).CycleActiveSpline();
            }
        }
    }
}
