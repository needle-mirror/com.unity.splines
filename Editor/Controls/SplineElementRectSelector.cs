using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Splines
{
    class SplineElementRectSelector
    {
        enum Mode
        {
            None,
            Replace,
            Add,
            Subtract
        }
        
        static class Styles
        {
            public static readonly GUIStyle selectionRect = GUI.skin.FindStyle("selectionRect");
        }

        Rect m_Rect;
        Vector2 m_StartPos;
        Mode m_Mode;
        Mode m_InitialMode;
        static readonly HashSet<ISplineElement> s_SplineElementsCompareSet = new HashSet<ISplineElement>();
        static readonly List<ISplineElement> s_SplineElementsBuffer = new List<ISplineElement>();
        static readonly HashSet<ISplineElement> s_PreRectSelectionElements = new HashSet<ISplineElement>();

        public void OnGUI(IReadOnlyList<IEditableSpline> paths)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    if (!Tools.viewToolActive)
                    {
                        HandleUtility.AddDefaultControl(id);

                        if (m_Mode != Mode.None)
                        {
                            // If we've started rect select in Add or Subtract modes, then if we were in a Replace 
                            // mode just before (i.e. the shift or action has been released temporarily), 
                            // we need to bring back the pre rect selection elements into current selection.
                            if (m_InitialMode != Mode.Replace && RefreshSelectionMode())
                            {
                                SplineSelection.Clear();
                                s_SplineElementsCompareSet.Clear();

                                if (m_Mode != Mode.Replace)
                                {
                                    foreach (var element in s_PreRectSelectionElements)
                                        SplineSelection.Add(element);
                                }
                                
                                m_Rect = GetRectFromPoints(m_StartPos, evt.mousePosition);
                                UpdateSelection(m_Rect, paths);
                            }
                        }
                    }

                    break;

                case EventType.Repaint:
                    if (GUIUtility.hotControl == id && m_Rect.size != Vector2.zero)
                    {
                        Handles.BeginGUI();
                        Styles.selectionRect.Draw(m_Rect, GUIContent.none, false, false, false, false);
                        Handles.EndGUI();
                    }
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                    {
                        m_StartPos = evt.mousePosition;
                        m_Rect = new Rect(Vector3.zero, Vector2.zero);

                        BeginSelection(paths);
                        GUIUtility.hotControl = id;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        m_Rect = GetRectFromPoints(m_StartPos, evt.mousePosition);
                        evt.Use();

                        UpdateSelection(m_Rect, paths);
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        EndSelection(m_Rect, paths);
                    }
                    break;
            }
        }

        protected virtual void BeginSelection(IReadOnlyList<IEditableSpline> paths)
        {
            RefreshSelectionMode();
            m_InitialMode = m_Mode;
            
            s_SplineElementsCompareSet.Clear();
            s_SplineElementsBuffer.Clear();
            if (m_Mode == Mode.Replace)
            {
                SplineSelection.Clear();
                s_PreRectSelectionElements.Clear();
            }
            else
                SplineSelection.GetSelectedElements(s_PreRectSelectionElements);
        }

        protected virtual void UpdateSelection(Rect rect, IReadOnlyList<IEditableSpline> paths)
        {
            //Get all elements in rect
            s_SplineElementsBuffer.Clear();
            for (int i = 0; i < paths.Count; ++i)
            {
                IEditableSpline spline = paths[i];
                for (int j = 0; j < spline.knotCount; ++j)
                    GetElementSelection(rect, spline, j, s_SplineElementsBuffer);
            }

            foreach (var splineElement in s_SplineElementsBuffer)
            {
                //Compare current frame buffer with last frame's to find new additions/removals
                var wasInRectLastFrame = s_SplineElementsCompareSet.Remove(splineElement);
                if (m_Mode == Mode.Replace || m_Mode == Mode.Add)
                {
                    var canAdd = m_Mode == Mode.Replace ? true : !s_PreRectSelectionElements.Contains(splineElement);
                    if (!wasInRectLastFrame && canAdd)
                        SplineSelection.Add(splineElement);
                } 
                else if (m_Mode == Mode.Subtract && !wasInRectLastFrame)
                {
                    SplineSelection.Remove(splineElement);
                }
            }
            
            //Remaining spline elements from last frame are removed from selection (or added if mode is subtract)
            foreach (var splineElement in s_SplineElementsCompareSet)
            {
                if (m_Mode == Mode.Replace || m_Mode == Mode.Add)
                {
                    // If we're in Add mode, don't remove elements that were in select prior to rect selection 
                    if (m_Mode == Mode.Add && s_PreRectSelectionElements.Contains(splineElement))
                        continue;
                    SplineSelection.Remove(splineElement);
                }
                else if (m_Mode == Mode.Subtract && s_PreRectSelectionElements.Contains(splineElement))
                    SplineSelection.Add(splineElement);
            }
            
            //Move current elements buffer to hash set for next frame compare
            s_SplineElementsCompareSet.Clear();
            foreach (var splineElement in s_SplineElementsBuffer)
                s_SplineElementsCompareSet.Add(splineElement);
        }

        bool RefreshSelectionMode()
        {
            var modeBefore = m_Mode;
            if (Event.current.shift)
                m_Mode = Mode.Add;
            else if (EditorGUI.actionKey)
                m_Mode = Mode.Subtract;
            else
                m_Mode = Mode.Replace;

            // Return true if the mode has changed
            return m_Mode != modeBefore;
        }

        void GetElementSelection(Rect rect, IEditableSpline spline, int index, List<ISplineElement> results)
        {
            var knot = spline.GetKnot(index);
            Vector3 screenSpace = HandleUtility.WorldToGUIPointWithDepth(knot.position);

            if (screenSpace.z > 0 && rect.Contains(screenSpace))
                results.Add(knot);

            for(int tangentIndex = 0; tangentIndex < knot.tangentCount; tangentIndex++)
            {
                var tangent = knot.GetTangent(tangentIndex);
                screenSpace = HandleUtility.WorldToGUIPointWithDepth(tangent.position);

                if (SplineSelectionUtility.IsSelectable(spline, index, tangent) && screenSpace.z > 0 && rect.Contains(screenSpace))
                    results.Add(tangent);
            }
        }

        protected virtual void EndSelection(Rect rect, IReadOnlyList<IEditableSpline> paths)
        {
            m_Mode = m_InitialMode = Mode.None;
        }

        static Rect GetRectFromPoints(Vector2 a, Vector2 b)
        {
            Vector2 min = new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
            Vector2 max = new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));

            return new Rect(min, max - min);
        }
    }
}
