using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

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
        static readonly HashSet<ISelectableElement> s_SplineElementsCompareSet = new HashSet<ISelectableElement>();
        static readonly List<ISelectableElement> s_SplineElementsBuffer = new List<ISelectableElement>();
        static readonly HashSet<ISelectableElement> s_PreRectSelectionElements = new HashSet<ISelectableElement>();

        public void OnGUI(IReadOnlyList<SplineInfo> splines)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                case EventType.MouseMove:
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
                            UpdateSelection(m_Rect, splines);
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
                    if (SplineHandles.ViewToolActive())
                        return;

                    if (HandleUtility.nearestControl == id && evt.button == 0)
                    {
                        m_StartPos = evt.mousePosition;
                        m_Rect = new Rect(Vector3.zero, Vector2.zero);

                        BeginSelection(splines);
                        GUIUtility.hotControl = id;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        m_Rect = GetRectFromPoints(m_StartPos, evt.mousePosition);
                        evt.Use();

                        UpdateSelection(m_Rect, splines);
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        EndSelection(m_Rect, splines);
                    }
                    break;
            }
        }

        void BeginSelection(IReadOnlyList<SplineInfo> splines)
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
                SplineSelection.GetElements(splines, s_PreRectSelectionElements);
        }

        void UpdateSelection(Rect rect, IReadOnlyList<SplineInfo> splines)
        {
            //Get all elements in rect
            s_SplineElementsBuffer.Clear();
            for (int i = 0; i < splines.Count; ++i)
            {
                var splineData = splines[i];
                for (int j = 0; j < splineData.Spline.Count; ++j)
                    if(!SplineSelection.HasActiveSplineSelection() || SplineSelection.Contains(splineData))
                        GetElementSelection(rect, splineData, j, s_SplineElementsBuffer);
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

        void GetElementSelection(Rect rect, SplineInfo splineInfo, int index, List<ISelectableElement> results)
        {
            var knot = splineInfo.Spline[index];
            var localToWorld = splineInfo.LocalToWorld;
            var worldKnot = knot.Transform(localToWorld);
            Vector3 screenSpace = HandleUtility.WorldToGUIPointWithDepth(worldKnot.Position);

            if (screenSpace.z > 0 && rect.Contains(screenSpace))
                results.Add(new SelectableKnot(splineInfo, index));

            var tangentIn = new SelectableTangent(splineInfo, index, BezierTangent.In);
            if (SplineSelectionUtility.IsSelectable(tangentIn))
            {
                screenSpace = HandleUtility.WorldToGUIPointWithDepth(worldKnot.Position + math.rotate(worldKnot.Rotation, worldKnot.TangentIn));
                if (screenSpace.z > 0 && rect.Contains(screenSpace))
                    results.Add(tangentIn);
            }

            var tangentOut = new SelectableTangent(splineInfo, index, BezierTangent.Out);
            if (SplineSelectionUtility.IsSelectable(tangentOut))
            {
                screenSpace = HandleUtility.WorldToGUIPointWithDepth(worldKnot.Position + math.rotate(worldKnot.Rotation, worldKnot.TangentOut));
                if (screenSpace.z > 0 && rect.Contains(screenSpace))
                    results.Add(tangentOut);
            }
        }

        void EndSelection(Rect rect, IReadOnlyList<SplineInfo> splines)
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
