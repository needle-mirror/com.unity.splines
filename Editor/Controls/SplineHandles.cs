using System.Collections.Generic;
using System.Linq;
using UnityEditor.SettingsManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace UnityEditor.Splines
{
    static class SplineHandles
    {
        [UserSetting]
        internal static UserSetting<Color> s_LineNormalFrontColor = new UserSetting<Color>(PathSettings.instance, "Handles.CurveNormalInFrontColor", Color.white, SettingsScope.User);

        [UserSetting]
        internal static UserSetting<Color> s_LineNormalBehindColor = new UserSetting<Color>(PathSettings.instance, "Handles.CurveNormalBehindColor", new Color(0.98f, 0.62f, 0.62f, 0.4f), SettingsScope.User);
        
        [UserSetting]
        internal static UserSetting<Color> s_KnotColor = new UserSetting<Color>(PathSettings.instance, "Handles.KnotDefaultColor", new Color(.4f, 1f, .95f, 1f), SettingsScope.User);

        [UserSetting]
        internal static UserSetting<Color> s_TangentColor = new UserSetting<Color>(PathSettings.instance, "Handles.TangentDefaultColor", Color.black, SettingsScope.User);

        [UserSettingBlock("Handles")]
        static void HandleColorPreferences(string searchContext)
        {
            s_LineNormalFrontColor.value = SettingsGUILayout.SettingsColorField("Curve Color", s_LineNormalFrontColor, searchContext);
            s_LineNormalBehindColor.value = SettingsGUILayout.SettingsColorField("Curve Color Behind Surface", s_LineNormalBehindColor, searchContext);
            s_KnotColor.value = SettingsGUILayout.SettingsColorField("Knot Color", s_KnotColor, searchContext);
            s_TangentColor.value = SettingsGUILayout.SettingsColorField("Tangent Color", s_TangentColor, searchContext);
        }

        const float k_SizeFactor = 0.15f;
        const float k_PickingDistance = 8f;
        
        const float k_HandleWidthDefault = 2f;
        const float k_HandleWidthHover = 4f;
        
        const float k_KnotDiscRadiusFactorDefault = 0.06f;
        const float k_KnotDiscRadiusFactorHover = 0.07f;
        const float k_KnotDiscRadiusFactorSelected = 0.085f;
        
        const float k_KnotRotDiscRadius = 0.18f;
        const float k_KnotRotDiscWidthDefault = 1.5f;
        const float k_KnotRotDiscWidthHover = 3f;
        const float k_KnotRotDiscWidthSelected = 4f;
        
        const float k_TangentLineWidthDefault = 2f;
        const float k_TangentLineWidthHover = 3.5f;
        const float k_TangentLineWidthSelected = 4.5f;
        const float k_TangentStartOffsetFromKnot = 0.22f;
        const float k_tangentEndOffsetFromHandle = 0.11f;

        const float k_AliasedLineSizeMultiplier = 0.5f;
        
        const int k_SegmentCount = 30;
        const float k_CurveLineWidth = 5f;
        const float k_PreviewCurveOpacity = 0.5f;
        const string k_TangentLineAATexPath = "Textures/TangentLineAATex";

        static Texture2D s_ThickTangentLineAATex = Resources.Load<Texture2D>(k_TangentLineAATexPath);

        static readonly Vector3[] s_CurveSegmentsBuffer = new Vector3[k_SegmentCount + 1];
        static readonly Vector3[] s_SegmentBuffer = new Vector3[2];
        static readonly Vector3[] s_AAWireDiscBuffer = new Vector3[18];
        
        static ISplineElement s_LastHoveredTangent;
        static int s_LastHoveredTangentID;
        static List<int> s_ElementChildIDs = new List<int>();
        
        internal static void DrawSplineHandles(IReadOnlyList<IEditableSpline> paths, SplineHandlesOptions options)
        {
            for (int i = 0; i < paths.Count; ++i)
            {
                DrawSplineHandles(paths[i], options);
            }
        }

        internal static bool DrawSplineHandles(IEditableSpline spline, SplineHandlesOptions options, bool activeSpline = true)
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

            activeSpline = curveIDs.Contains(HandleUtility.nearestControl) || activeSpline;
            for (int knotIndex = 0; knotIndex <= lastIndex; ++knotIndex)
            {
                var curve = new CurveData(spline, knotIndex);
                if (isInsertingKnots)
                    CurveHandleWithKnotInsert(curve, curveIDs[knotIndex], activeSpline);
                else
                    DrawCurve(curve);
            }

            var drawHandlesAsActive = curveIDs.Contains(HandleUtility.nearestControl) || activeSpline;
            
            for (int knotIndex = 0; knotIndex < spline.knotCount; ++knotIndex)
            {
                var knot = spline.GetKnot(knotIndex);

                if (HasOption(options, SplineHandlesOptions.ShowTangents))
                {
                    for (int tangentIndex = 0; tangentIndex < knot.tangentCount; ++tangentIndex)
                    {
                        //Not drawing unused tangents
                        if (!spline.closed && ((knotIndex == 0 && tangentIndex == 0) ||
                                               (knotIndex != 0 && knotIndex + 1 == spline.knotCount && tangentIndex + 1 == knot.tangentCount)))
                            continue;

                        var tangent = knot.GetTangent(tangentIndex);
                        if (HasOption(options, SplineHandlesOptions.SelectableTangents))
                        {
                            var tangentHandlelID = SelectionHandle(tangent);
                            s_ElementChildIDs.Add(tangentHandlelID);
                        }
                        else
                            DrawTangentHandle(tangent, -1, drawHandlesAsActive);
                    }
                }
                
                if (HasOption(options, SplineHandlesOptions.SelectableKnots))
                    SelectionHandle(knot);
                else
                    DrawKnotHandle(knot, null, drawHandlesAsActive);
                
                s_ElementChildIDs.Clear();
            }

            if (s_LastHoveredTangent != null && Event.current.GetTypeForControl(s_LastHoveredTangentID) == EventType.Repaint)            
                s_LastHoveredTangent = null;
   
            return activeSpline;
        }

        static bool HasOption(SplineHandlesOptions options, SplineHandlesOptions target)
        {
            return (options & target) == target;
        }

        internal static int SelectionHandle(ISplineElement element)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(id);
            
            switch (eventType)
            {
                case EventType.Layout:
                    if (!Tools.viewToolActive)
                    {
                        HandleUtility.AddControl(id, SplineHandleUtility.DistanceToCircle(element.position, k_PickingDistance));

                        if (element is EditableTangent)
                        {
                            if (HandleUtility.nearestControl == id)
                            {
                                s_LastHoveredTangent = element;
                                s_LastHoveredTangentID = id;
                            }
                        }
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

                        //Add/Remove from knotSelection
                        if (EditorGUI.actionKey || evt.modifiers == EventModifiers.Shift)
                        {
                            if (SplineSelection.Contains(element))
                                SplineSelection.Remove(element);
                            else
                                SplineSelection.Add(element);
                        }
                        else
                        {
                            SplineSelection.Clear();
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
                            DrawKnotHandle(knot, id, s_ElementChildIDs);
                            break;
                        case EditableTangent tangent:
                            DrawTangentHandle(tangent, id);
                            break;
                    }
                    break;
            }

            return id;
        }
        
        internal static bool ButtonHandle(int controlID, EditableKnot knot, bool active)
        {
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(controlID);

            var position = knot.position;

            switch (eventType)
            {
                case EventType.Layout:
                {
                    if(!Tools.viewToolActive)
                        HandleUtility.AddControl(controlID, SplineHandleUtility.DistanceToKnot(position));
                    break;
                }
                
                case EventType.Repaint:
                    DrawKnotHandle(knot, controlID, null, active);
                    break;
                
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == controlID)
                    {
                        //Clicking a knot selects it
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

                        return true;
                    }
                    break;

                case EventType.MouseMove:
                    if (HandleUtility.nearestControl == controlID)
                        HandleUtility.Repaint();
                    break;
            }

            return false;
        }
        
        public static void CurveHandleWithKnotInsert(CurveData curve, int controlID, bool activeSpline)
        {
            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(controlID);
            
            CurveHandleCap(curve, controlID, eventType, false, activeSpline);
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

                            var previewKnotRotation = quaternion.identity;
                            if (curve.a.spline is BezierEditableSpline)
                            {
                                var bezierCurve = BezierCurve.FromTangent(curve.a.position, curve.a.GetTangent((int)BezierTangent.Out).direction, 
                                    curve.b.position, curve.b.GetTangent((int)BezierTangent.In).direction);

                                var up = CurveUtility.EvaluateUpVector(bezierCurve, t, math.rotate(curve.a.rotation, math.up()), math.rotate(curve.b.rotation, math.up()));
                                var tangentOut = CurveUtility.EvaluateTangent(bezierCurve, t);
                                previewKnotRotation = quaternion.LookRotationSafe(math.normalize(tangentOut), up);
                            }

                            DrawKnotHandle(position, previewKnotRotation, false, controlID, false, activeSpline);
                        }
                    }
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == controlID)
                    {
                        if (evt.button != 0)
                            break;

                        SplineHandleUtility.GetNearestPointOnCurve(curve, out Vector3 position, out float t);
                        
                        //Do not place a new knot on an existing one to prevent creation of singularity points with bad tangents
                        if(t > 0f && t < 1f)
                        {
                            EditableKnot knot = EditableSplineUtility.InsertKnotOnCurve(curve, position, t);

                            if(!(evt.control || evt.shift))
                                SplineSelection.Clear();

                            SplineSelection.Add(knot);
                        }
                        
                        evt.Use();
                    }
                    break;

                case EventType.MouseMove:
                    if(HandleUtility.nearestControl == controlID)
                        HandleUtility.Repaint();
                    
                    break;
            }
        }

        internal static void DrawKnotHandle(EditableKnot knot, List<int> tangentControlIDs = null, bool activeSpline = true)
        {
            DrawKnotHandle(knot, -1, tangentControlIDs, activeSpline);
        }
        
        internal static void DrawKnotHandle(EditableKnot knot, int controlId = -1, List<int> tangentControlIDs = null, bool activeSpline = true)
        {
            var mirroredTangentSelected = false;
            var mirroredTangentHovered = false;
            
            if (tangentControlIDs != null && knot is BezierEditableKnot bezierKnot &&
                (bezierKnot.mode == BezierEditableKnot.Mode.Mirrored || bezierKnot.mode == BezierEditableKnot.Mode.Continuous)) 
            {
                for (int i = 0; i < knot.tangentCount; i++)
                {
                    var tangent = knot.GetTangent(i);
                    if (SplineSelection.Contains(tangent))
                    {
                        mirroredTangentSelected = true;
                        break;
                    }
                }

                if (!mirroredTangentSelected)
                {
                    foreach (var tangentID in tangentControlIDs)
                    {
                        if (HandleUtility.nearestControl == tangentID)
                            mirroredTangentHovered = true;
                    }
                }
            }

            DrawKnotHandle(knot.position, knot.rotation, SplineSelection.Contains(knot), controlId, false, activeSpline, mirroredTangentSelected, mirroredTangentHovered);
        }

        internal static void DrawKnotHandle(Vector3 knotPosition, Quaternion knotRotation, bool selected, int controlId, 
            bool preview = false, bool activeSpline = true,  bool mirroredTangentSelected = false, bool mirroredTangentHovered = false)
        {
            if(Event.current.GetTypeForControl(controlId) != EventType.Repaint)
                return;
            
            var knotColor = s_KnotColor.value;
            if(preview)
                knotColor = Color.Lerp(Color.gray, Color.white, 0.5f);
            else if(selected)
                knotColor = Handles.selectedColor;
            else if(controlId > 0 && GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlId)
                knotColor = Handles.preselectionColor;

            if(!activeSpline)
                knotColor = Handles.secondaryColor;

            var handleSize = HandleUtility.GetHandleSize(knotPosition);
            var hovered = HandleUtility.nearestControl == controlId;
            
            using (new Handles.DrawingScope(knotColor, Matrix4x4.TRS(knotPosition, knotRotation, Vector3.one)))
            {
                // Knot disc
                if (selected || hovered)
                {
                    var radius = selected ? k_KnotDiscRadiusFactorSelected : k_KnotDiscRadiusFactorHover;
                    Handles.DrawSolidDisc(Vector3.zero, Vector3.up, radius * handleSize);
                }
                else
                    Handles.DrawWireDisc(Vector3.zero, Vector3.up, k_KnotDiscRadiusFactorDefault * handleSize, k_HandleWidthHover * k_AliasedLineSizeMultiplier);
            }

            var rotationDiscColor = knotColor;
            if (!selected && mirroredTangentSelected)
                rotationDiscColor = Handles.selectedColor;
            
            using (new Handles.DrawingScope(rotationDiscColor, Matrix4x4.TRS(knotPosition, knotRotation, Vector3.one)))
            {
                // Knot rotation indicators
                var rotationDiscWidth = k_KnotRotDiscWidthDefault;
                if (selected || mirroredTangentSelected)
                    rotationDiscWidth = k_KnotRotDiscWidthSelected;
                else if (hovered || mirroredTangentHovered)
                    rotationDiscWidth = k_KnotRotDiscWidthHover;

                DrawAAWireDisc(Vector3.zero, Vector3.up, k_KnotRotDiscRadius * handleSize, rotationDiscWidth);

                s_SegmentBuffer[0] = Vector3.zero;
                s_SegmentBuffer[1] = Vector3.up * 2f * k_SizeFactor * handleSize;
                Handles.DrawAAPolyLine(k_HandleWidthDefault, s_SegmentBuffer);
            }
        }
        
        internal static void DrawPreviewKnot(EditableKnot knot)
        {
            DrawKnotHandle(knot.position, knot.rotation, false, -1, true);
        }
        
        internal static void DrawTangentHandle(EditableTangent tangent, int controlId = -1, bool activeHandle = true)
        {
            if(Event.current.type != EventType.Repaint)
                return;

            var knotPos = tangent.owner.position;
            var tangentPos = tangent.position;

            var tangentHandleSize = HandleUtility.GetHandleSize(tangentPos);

            var tangentColor = s_KnotColor.value;
            var selected = SplineSelection.Contains(tangent);
            var hovered = HandleUtility.nearestControl == controlId;
            if (selected)
                tangentColor = Handles.selectedColor;
            else if (hovered)
                tangentColor = Handles.preselectionColor;
            
            if(!activeHandle)
                tangentColor = Handles.secondaryColor;

            var tangentArmColor = tangentColor == s_KnotColor ? s_TangentColor.value : tangentColor;
            
            var oppositeSelected = IsOppositeTangentSelected(tangent);
            if (tangentArmColor == s_TangentColor && oppositeSelected)
                tangentArmColor = Handles.selectedColor;
            
            var oppositeHovered = IsOppositeTangentHovered(tangent);
            var mirrored = (tangent.owner is BezierEditableKnot bezierKnot) &&
                           bezierKnot.mode == BezierEditableKnot.Mode.Mirrored;

            using (new ColorScope(tangentArmColor))
            {
                var width = k_TangentLineWidthDefault;
                if (selected || (mirrored && oppositeSelected))
                    width = k_TangentLineWidthSelected;
                else if (hovered || (mirrored && oppositeHovered))
                    width = k_TangentLineWidthHover;
                
                var tex = width > k_TangentLineWidthDefault ? s_ThickTangentLineAATex : null;

                var startPos = knotPos;
                var toTangent = tangentPos - knotPos;
                var toTangentNorm = math.normalize(toTangent);
                var length = math.length(toTangent);
                
                var knotHandleSize = HandleUtility.GetHandleSize(startPos);
                var knotHandleOffset = knotHandleSize * k_TangentStartOffsetFromKnot;
                var tangentHandleOffset = tangentHandleSize * k_tangentEndOffsetFromHandle;
                // Reduce the length slightly, so that there's some space between tangent line endings and handles.
                length = Mathf.Max(0f, length - knotHandleOffset - tangentHandleOffset);
                startPos += toTangentNorm * knotHandleOffset;
                SplineHandleUtility.DrawLineWithWidth(startPos + toTangentNorm * length, startPos, width, tex);
            }

            var rotation = TransformOperation.CalculateElementSpaceHandleRotation(math.length(tangent.localPosition) >0 ? tangent : tangent.owner);
            using (new Handles.DrawingScope(tangentColor, Matrix4x4.TRS(tangent.position, rotation, Vector3.one)))
            {
                if (selected || hovered)
                {
                    var radius = (selected ? k_KnotDiscRadiusFactorSelected : k_KnotDiscRadiusFactorHover) * tangentHandleSize;
                    // As Handles.DrawSolidDisc has no thickness parameter, we're drawing a wire disc here so that the solid disc has thickness when viewed from a shallow angle.
                    Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius * 0.7f, k_HandleWidthHover);
                    Handles.DrawSolidDisc(Vector3.zero, Vector3.up, radius);
                }
                else
                    Handles.DrawWireDisc(Vector3.zero, Vector3.up, k_KnotDiscRadiusFactorDefault * tangentHandleSize, k_HandleWidthHover * k_AliasedLineSizeMultiplier);
            }
        }

        static void DrawAAWireDisc(Vector3 position, Vector3 normal, float radius, float thickness)
        {
            // Right vector calculation here is identical to Handles.DrawWireDisc 
            Vector3 right = Vector3.Cross(normal, Vector3.up);
            if ((double)right.sqrMagnitude < 1.0 / 1000.0)
                right = Vector3.Cross(normal, Vector3.right);
            
            var angleStep = 360f / (s_AAWireDiscBuffer.Length - 1);
            for (int i = 0; i < s_AAWireDiscBuffer.Length - 1; i++)
            {
                s_AAWireDiscBuffer[i] = position + right * radius;
                right = Quaternion.AngleAxis(angleStep, normal) * right;
            }

            s_AAWireDiscBuffer[s_AAWireDiscBuffer.Length - 1] = s_AAWireDiscBuffer[0];

            var tex = thickness > 2f ? s_ThickTangentLineAATex : null;
            Handles.DrawAAPolyLine(tex, thickness, s_AAWireDiscBuffer);
        }

        static bool IsOppositeTangentSelected(EditableTangent tangent)
        {
            return tangent.owner is BezierEditableKnot knot
                   && knot.mode != BezierEditableKnot.Mode.Broken
                   && knot.TryGetOppositeTangent(tangent, out EditableTangent oppositeTangent)
                   && SplineSelection.Contains(oppositeTangent);
        }
        
        static bool IsOppositeTangentHovered(EditableTangent tangent)
        {
            return tangent.owner is BezierEditableKnot knot
                   && knot.TryGetOppositeTangent(tangent, out EditableTangent oppositeTangent)
                   && (s_LastHoveredTangent == oppositeTangent);
        }
        
        internal static void DrawCurve(CurveData curve)
        {
            Event evt = Event.current;
            if (evt.type == EventType.Repaint)
                CurveHandleCap(curve, -1, EventType.Repaint);
        }

        internal static void CurveHandleCap(CurveData curve, int controlID, EventType eventType, bool previewCurve = false, bool activeSpline = true)
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
                            HandleUtility.AddControl(controlID, Mathf.Max(0, dist - k_PickingDistance));
                        break;
                    }

                case EventType.Repaint:
                    {
                        SplineHandleUtility.GetCurveSegments(curve, s_CurveSegmentsBuffer);
                        //We attenuate the spline display if a spline can be controlled (id != -1) and
                        //if it's not the current active spline
                        var attenuate = controlID != -1 && !activeSpline;
                        
                        var prevColor = Handles.color;
                        
                        var color = s_LineNormalFrontColor.value;
                        if (attenuate)
                            color = Handles.secondaryColor;
                        if (previewCurve)
                            color.a *= k_PreviewCurveOpacity;
                        
                        Handles.color = color;

                        using (new ZTestScope(CompareFunction.Less))
                        {
                            Handles.DrawAAPolyLine(k_CurveLineWidth, s_CurveSegmentsBuffer);
                        }

                        color = s_LineNormalBehindColor.value;
                        if (attenuate)
                            color = Handles.secondaryColor;
                        if (previewCurve)
                            color.a *= k_PreviewCurveOpacity;
                        
                        Handles.color = color;

                        using (new ZTestScope(CompareFunction.Greater))
                        {
                            Handles.DrawAAPolyLine(k_CurveLineWidth, s_CurveSegmentsBuffer);
                        }

                        Handles.color = prevColor;
                        break;
                    }
            }
        }
    }
}
