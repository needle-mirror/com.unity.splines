using System.Collections.Generic;
using System.Linq;
using UnityEditor.SettingsManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Splines
{
    static class SplineHandles
    {
        [UserSetting("Handles", "Curve Normal Color")]
        internal static readonly UserSetting<Color> k_LineNormalFrontColor = new UserSetting<Color>(PathSettings.instance, "Handles.CurveNormalInFrontColor", Color.white, SettingsScope.User);

        [UserSetting("Handles", "Curve Normal Color Behind Surface")]
        internal static readonly UserSetting<Color> k_LineNormalBehindColor = new UserSetting<Color>(PathSettings.instance, "Handles.CurveNormalBehindColor", new Color(0.98f, 0.62f, 0.62f, 0.4f), SettingsScope.User);

        [UserSetting("Handles", "Knot Default Color")]
        internal static readonly UserSetting<Color> k_KnotColor = new UserSetting<Color>(PathSettings.instance, "Handles.KnotDefaultColor", Color.white, SettingsScope.User);

        [UserSetting("Handles", "Tangent Default Color")]
        internal static readonly UserSetting<Color> k_TangentColor = new UserSetting<Color>(PathSettings.instance, "Handles.TangentDefaultColor", new Color(0.15f, 0.55f, 1f, 1), SettingsScope.User);


        const float k_KnotPickingDistance = 18f;
        const float k_TangentPickingDistance = 8f;
        const float k_TangentLineWidth = 3f;
        const float k_CurvePickingDistance = 8f;
        const int k_SegmentCount = 30;
        const float k_CurveLineWidth = 5f;
        const float k_CurveGuideLineWidth = 3f;
        static readonly Color k_GuideLinesColor = new Color(.7f, .7f, .7f, 1);
        
        static readonly Vector3[] s_CurveSegmentsBuffer = new Vector3[k_SegmentCount + 1];

        internal static void DrawSplineHandles(IReadOnlyList<IEditableSpline> paths, SplineHandlesOptions options)
        {
            for (int i = 0; i < paths.Count; ++i)
            {
                DrawSplineHandles(paths[i], options);
            }
        }

        internal static void DrawSplineHandles(IEditableSpline spline, SplineHandlesOptions options, bool activeSpline = true)
        {
            int lastIndex = spline.closed ? spline.knotCount - 1 : spline.knotCount - 2; //If the spline isn't closed, skip the last index of the spline
            var isInsertingKnots = HasOption(options, SplineHandlesOptions.KnotInsert);

            int[] curveIDs = new int[0];
            if(isInsertingKnots && lastIndex+1>=0)
            {
                curveIDs = new int[lastIndex+1];
                for (int idIndex = 0; idIndex < lastIndex+1; ++idIndex)
                    curveIDs[idIndex] = GUIUtility.GetControlID(FocusType.Passive);
            }
            
            for (int knotIndex = 0; knotIndex <= lastIndex; ++knotIndex)
            {
                var curve = new CurveData(spline, knotIndex);
                if (isInsertingKnots)
                    CurveHandleWithKnotInsert(curve, curveIDs[knotIndex], curveIDs.Contains(HandleUtility.nearestControl) || activeSpline);
                else
                    DrawCurve(curve);
            }

            for (int knotIndex = 0; knotIndex < spline.knotCount; ++knotIndex)
            {
                var knot = spline.GetKnot(knotIndex);

                if (HasOption(options, SplineHandlesOptions.ShowTangents))
                {
                    for (int tangentIndex = 0; tangentIndex < knot.tangentCount; ++tangentIndex)
                    {
                        //Not drawing unused tangents
                        if (!spline.closed && ((knotIndex == 0 && tangentIndex == 0) ||
                                               (knotIndex + 1 == spline.knotCount && tangentIndex + 1 == knot.tangentCount)))
                            continue;

                        var tangent = knot.GetTangent(tangentIndex);
                        if (HasOption(options, SplineHandlesOptions.SelectableTangents))
                            SelectionHandle(tangent);
                        else
                            DrawTangentHandle(tangent);
                    }
                }

                if (HasOption(options, SplineHandlesOptions.SelectableKnots))
                    SelectionHandle(knot);
                else
                    DrawKnotHandle(knot, curveIDs.Contains(HandleUtility.nearestControl) || activeSpline);
            }
        }

        static bool HasOption(SplineHandlesOptions options, SplineHandlesOptions target)
        {
            return (options & target) == target;
        }

        internal static void SelectionHandle(ISplineElement element)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(id);
            
            switch (eventType)
            {
                case EventType.Layout:
                    if (!Tools.viewToolActive)
                        HandleUtility.AddControl(id, SplineHandleUtility.DistanceToCircle(element.position, GetPickingDistance(element)));
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id)
                    {
                        //Clicking a knot selects it
                        if (evt.button != 0)
                            break;

                        GUIUtility.hotControl = id;
                        evt.Use();

                        //Add/Remove from knotSelection
                        if (evt.modifiers == EventModifiers.Command
                            || evt.modifiers == EventModifiers.Control)
                        {
                            if (SplineSelection.Contains(element))
                                SplineSelection.Remove(element);
                            else
                                SplineSelection.Add(element);
                        }
                        //Change active knot
                        else if (evt.modifiers == EventModifiers.Shift)
                        {
                            SplineSelection.SetActive(element);
                        }
                        else
                        {
                            SplineSelection.ClearNoUndo(false);
                            SplineSelection.Add(element);
                        }
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

                case EventType.Repaint:
                    switch (element)
                    {
                        case EditableKnot knot:
                            DrawKnotHandle(knot, id);
                            break;
                        case EditableTangent tangent:
                            DrawTangentHandle(tangent, id);
                            break;
                    }
                    break;
            }
        }
        
        internal static bool ButtonHandle(EditableKnot knot)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(id);

            var position = knot.position;

            switch (eventType)
            {
                case EventType.Layout:
                {
                    if(!Tools.viewToolActive)
                        HandleUtility.AddControl(id, SplineHandleUtility.DistanceToKnot(position));
                    break;
                }
                
                case EventType.Repaint:
                    DrawKnotHandle(knot, id);
                    break;
                
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id)
                    {
                        //Clicking a knot selects it
                        if (evt.button != 0)
                            break;

                        GUIUtility.hotControl = id;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        return true;
                    }
                    break;

                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint();
                    break;
            }

            return false;
        }

        static float GetPickingDistance(ISplineElement element)
        {
            switch (element)
            {
                case EditableKnot _: return k_KnotPickingDistance;
                case EditableTangent _: return k_TangentPickingDistance;
                default: return 0f;
            }
        }
        
        public static void CurveHandleWithKnotInsert(CurveData curve, int controlID, bool activeSpline)
        {
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(controlID);
            
            CurveHandleCap(curve, controlID, eventType, activeSpline);
            switch (eventType)
            {
                case EventType.Repaint:
                    if (HandleUtility.nearestControl == controlID)
                    {
                        SplineHandleUtility.GetNearestPointOnCurve(curve, out Vector3 position, out float t);
                        if(t > 0f && t < 1f)
                        {
                            var mouseRect = new Rect(evt.mousePosition - new Vector2(500, 500), new Vector2(1000, 1000));
                            EditorGUIUtility.AddCursorRect(mouseRect, MouseCursor.ArrowPlus);

                            DrawKnotHandle(position, Quaternion.identity, false, controlID, false, activeSpline);
                        }
                    }
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == controlID)
                    {
                        if (evt.button != 0)
                            break;

                        GUIUtility.hotControl = controlID;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        SplineHandleUtility.GetNearestPointOnCurve(curve, out Vector3 position, out float t);
                        
                        //Do not place a new knot on an existing one to prevent creation of singularity points with bad tangents
                        if(t > 0f && t < 1f)
                        {
                            EditableKnot knot = EditableSplineUtility.InsertKnotOnCurve(curve, position, t);

                            if(!evt.control)
                                SplineSelection.Clear();

                            SplineSelection.Add(knot);
                        }
                    }
                    break;

                case EventType.MouseMove:
                    if (HandleUtility.nearestControl == controlID)
                        HandleUtility.Repaint();
                    break;
            }
        }


        internal static void DrawKnotHandle(EditableKnot knot, bool activeSpline = true)
        {
            DrawKnotHandle(knot, -1, activeSpline);
        }
        
        internal static void DrawKnotHandle(EditableKnot knot, int controlId = -1, bool activeSpline = true)
        {
            DrawKnotHandle(knot.position, knot.rotation, SplineSelection.Contains(knot), controlId, false, activeSpline);
        }

        internal static void DrawPreviewKnot(EditableKnot knot)
        {
            DrawKnotHandle(knot.position, knot.rotation, false, -1, true);
        }

        internal static void DrawKnotHandle(Vector3 knotPosition, Quaternion knotRotation, bool selected, int controlId, bool preview = false, bool activeSpline = true)
        {
            if(Event.current.GetTypeForControl(controlId) != EventType.Repaint)
                return;
            
            var knotColor = k_KnotColor.value;
            if(preview)
                knotColor = Color.Lerp(Color.gray, Color.white, 0.5f);
            else if(selected)
                knotColor = Handles.selectedColor;
            else if(controlId > 0 && GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlId)
                knotColor = Handles.preselectionColor;

            if(!activeSpline)
                knotColor = Handles.secondaryColor;

            DrawKnotHandle(knotPosition, knotRotation, knotColor);
        }

        internal static void DrawKnotHandle(Vector3 knotPosition, Quaternion rotation, Color mainColor)
        {
            var size = HandleUtility.GetHandleSize(knotPosition) * 0.1f;
            
            using(new ColorScope(mainColor))
                Handles.CubeHandleCap(-1, knotPosition, rotation, size, EventType.Repaint);
        }


        internal static void DrawTangentHandle(EditableTangent tangent, int controlId = -1)
        {
            if(Event.current.type != EventType.Repaint)
                return;
            
            var knotPos = tangent.owner.position;
            var tangentPos = tangent.position;

            var size = HandleUtility.GetHandleSize(tangentPos) * 0.1f;
            
            var tangentColor = k_TangentColor.value;
            if(SplineSelection.Contains(tangent))
                tangentColor = Handles.selectedColor;
            else if(HandleUtility.nearestControl == controlId)
                tangentColor = Handles.preselectionColor;
            
            var tangentArmColor = tangentColor;
            var useDottedLine = false;
            if(tangentArmColor == k_TangentColor && IsOppositeTangentSelected(tangent))
            {
                tangentArmColor = Handles.selectedColor;
                useDottedLine = ( tangent.owner is BezierEditableKnot bezierKnot ) &&
                                bezierKnot.mode == BezierEditableKnot.Mode.Continuous;
            }

            using(new ColorScope(tangentArmColor))
                SplineHandleUtility.DrawLineWithWidth(tangentPos, knotPos, k_TangentLineWidth, useDottedLine);
            using(new ColorScope(tangentColor))
                Handles.SphereHandleCap(controlId, tangentPos, Quaternion.identity, size, EventType.Repaint);
        }

        static bool IsOppositeTangentSelected(EditableTangent tangent)
        {
            return tangent.owner is BezierEditableKnot knot
                   && knot.mode != BezierEditableKnot.Mode.Broken
                   && knot.TryGetOppositeTangent(tangent, out EditableTangent oppositeTangent)
                   && SplineSelection.Contains(oppositeTangent);
        }

        internal static void DrawCurve(CurveData curve)
        {
            Event evt = Event.current;
            if (evt.type == EventType.Repaint)
                CurveHandleCap(curve, -1, EventType.Repaint);
        }

        internal static void CurveHandleCap(CurveData curve, int controlID, EventType eventType, bool activeSpline = true)
        {
            switch (eventType)
            {
                case EventType.Layout:
                    {
                        SplineHandleUtility.GetCurveSegments(curve, s_CurveSegmentsBuffer);

                        float dist = float.MaxValue;
                        for (var i = 0; i < s_CurveSegmentsBuffer.Length - 1; ++i)
                        {
                            var a = s_CurveSegmentsBuffer[i];
                            var b = s_CurveSegmentsBuffer[i + 1];
                            dist = Mathf.Min(HandleUtility.DistanceToLine(a, b), dist);
                        }

                        if (!Tools.viewToolActive)
                            HandleUtility.AddControl(controlID, Mathf.Max(0, dist - k_CurvePickingDistance));
                        break;
                    }

                case EventType.Repaint:
                    {
                        SplineHandleUtility.GetCurveSegments(curve, s_CurveSegmentsBuffer);
                        //We attenuate the spline display if a spline can be controlled (id != -1) and
                        //if it's not the current active spline
                        var attenuate = controlID != -1  && !activeSpline;
                        
                        var prevColor = Handles.color;
                        
                        var color = k_LineNormalFrontColor.value;
                        if (attenuate)
                            color = Handles.secondaryColor;
                        
                        Handles.color = color;

                        using (new ZTestScope(CompareFunction.Less))
                        {
                            Handles.DrawAAPolyLine(k_CurveLineWidth, s_CurveSegmentsBuffer);
                        }

                        color = k_LineNormalBehindColor.value;
                        if (attenuate)
                            color = Handles.secondaryColor;
                        
                        Handles.color = color;

                        using (new ZTestScope(CompareFunction.Greater))
                        {
                            Handles.DrawAAPolyLine(k_CurveLineWidth, s_CurveSegmentsBuffer);
                        }

                        Handles.color = prevColor;

                        using (new ColorScope(k_GuideLinesColor))
                        {
                            //For better readability, in the case that the curve doesn't go through the point, we add a line between the start of the curve and the point
                            Vector3 firstPoint = curve.a.position;
                            Vector3 firstSegmentPoint = s_CurveSegmentsBuffer[0];
                            if (firstPoint != firstSegmentPoint)
                                Handles.DrawDottedLine(firstPoint, firstSegmentPoint, k_CurveGuideLineWidth);

                            //Only do this for last curve to not get line overlap
                            bool lastCurve = !curve.a.spline.closed && curve.a.index == curve.a.spline.knotCount - 1;
                            if (lastCurve)
                            {
                                Vector3 lastPoint = curve.b.position;
                                Vector3 lastSegmentPoint = s_CurveSegmentsBuffer[k_SegmentCount];
                                if (lastPoint != lastSegmentPoint)
                                    Handles.DrawDottedLine(lastPoint, lastSegmentPoint, k_CurveGuideLineWidth);
                            }
                        }

                        break;
                    }
            }
        }
        
        internal static int KnotSurfaceAddHandle(IEditableSpline spline, float distanceAboveSurface = 0)
        {
            if (spline == null)
                return -1;

            Event evt = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    if (!Tools.viewToolActive)
                        HandleUtility.AddDefaultControl(id);
                    break;

                case EventType.Repaint:
                    if (HandleUtility.nearestControl == id && !Tools.viewToolActive)
                    {
                        if (SplineHandleUtility.GetPointOnSurfaces(evt.mousePosition, distanceAboveSurface, out Vector3 point, out Vector3 normal))
                        {
                            if (spline.knotCount > 0)
                            {
                                Vector3 lastPoint = spline.GetKnot(spline.knotCount - 1).position;
                                Handles.DrawDottedLine(lastPoint, point, 3f);
                            }
                            DrawKnotHandle(point, Quaternion.identity,  false, id);
                        }
                    }
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id)
                    {
                        if (evt.button != 0)
                            break;

                        GUIUtility.hotControl = id;
                        evt.Use();
                    } 
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        if (SplineHandleUtility.GetPointOnSurfaces(evt.mousePosition, distanceAboveSurface, out Vector3 point, out Vector3 normal))
                        {
                            point = SplineHandleUtility.RoundBasedOnMinimumDifference(point);
                            EditableSplineUtility.AddPointToEnd(spline, point, normal);
                        }
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
