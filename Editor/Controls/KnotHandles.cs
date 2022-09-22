using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class KnotHandles
    {
        const float k_ColorAlphaFactor = 0.3f;
        const float k_KnotRotDiscRadius = 0.18f;
        const float k_KnotRotDiscWidthDefault = 1.5f;
        const float k_KnotRotDiscWidthHover = 3f;
        const float k_KnotHandleWidth = 2f;
        
        static readonly List<SelectableKnot> k_KnotBuffer = new List<SelectableKnot>();

        static readonly Vector3[] k_HandlePoints = new Vector3[11];
        
        static List<(SelectableKnot knot, bool selected, bool hovered, Color knotColor, Color discColor, bool linkedKnot)> s_Knots = new ();
        
        public static void Draw(int controlId, SelectableKnot knot, bool preview = false, bool activeSpline = true)
        {
            if (Event.current.GetTypeForControl(controlId) != EventType.Repaint)
                return;

            var knotHovered = GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlId;
            var hovered = knotHovered;
            var selected = SplineSelection.Contains(knot);

            EditorSplineUtility.GetKnotLinks(knot, k_KnotBuffer);
            var drawLinkedKnotHandle = k_KnotBuffer.Count != 1;
            var mainKnot = knot;
            SelectableKnot lastHovered = new SelectableKnot();
            //Retrieving the last hovered element
            var lastHoveredElementIsKnot = SplineHandleUtility.lastHoveredElement is SelectableKnot;
            if (lastHoveredElementIsKnot)
                lastHovered = (SelectableKnot)SplineHandleUtility.lastHoveredElement;

            //The curve is hovered if the last hovered element is a linked knot and that the elementId is set to -1
            var curveIsHovered = lastHoveredElementIsKnot &&
                k_KnotBuffer.Contains(lastHovered) &&
                SplineHandles.IsCurveId(SplineHandleUtility.lastHoveredElementId);

#if UNITY_2022_2_OR_NEWER
            var knotColor = Handles.elementColor;
            var highlightColor = Handles.elementPreselectionColor;
#else
            var knotColor = SplineHandleUtility.knotColor;
            var highlightColor = SplineHandleUtility.knotColor;
#endif
            
            if (preview)
                knotColor = Color.Lerp(Color.gray, Color.white, 0.5f);
#if UNITY_2022_2_OR_NEWER
            else if (hovered)
            {
                knotColor = Handles.elementPreselectionColor;
                highlightColor = Handles.elementPreselectionColor;
            }
            else if (selected)
            {
                knotColor = Handles.elementSelectionColor;
                highlightColor = Handles.elementSelectionColor; 
            }
#else
            else if (hovered)
            {
                knotColor = Handles.preselectionColor;
                highlightColor = Handles.preselectionColor;
            }
            else if (selected)
            {
                knotColor = Handles.selectedColor;
                highlightColor = Handles.selectedColor;
            }
#endif

            if (!activeSpline)
                knotColor = Handles.secondaryColor;

            // Knot rotation indicators
#if UNITY_2022_2_OR_NEWER
            var rotationDiscColor = Handles.elementPreselectionColor;
#else
            var rotationDiscColor = Handles.preselectionColor;
#endif
            
            hovered |= lastHoveredElementIsKnot && (knot.Equals(lastHovered) || k_KnotBuffer.Contains(lastHovered));
            
            if (!(controlId > 0 && GUIUtility.hotControl == 0 && hovered))
                hovered = false;
            
            if (drawLinkedKnotHandle)
            {
                if (curveIsHovered)
                {   
                    drawLinkedKnotHandle = false;
                    
                    if (!knot.Equals(lastHovered))
                    {
                        if (!SplineSelection.Contains(knot))
                            return;

                        hovered = false;
                        mainKnot = lastHovered;
                    }
                }
                else 
                {
                    foreach (var k in k_KnotBuffer)
                    {
                        if (!hovered)
                        {
                            var kSelected = SplineSelection.Contains(k);

                            // If the current knot in not selected but other linked knots are, skip rendering
                            if (!selected && kSelected)
                                return;

                            // If current knot is selected but not k, don't consider k as a potential knot
                            if (selected && !kSelected)
                            {
                                drawLinkedKnotHandle = false;
                                continue;
                            }
                        }

                        //Main knot is the older one, the one on the spline of lowest range and the knot of lowest index
                        if ((!SplineSelection.HasActiveSplineSelection() || SplineSelection.Contains(k.SplineInfo)) &&
                            (k.SplineInfo.Index < mainKnot.SplineInfo.Index ||
                                k.SplineInfo.Index == mainKnot.SplineInfo.Index && k.KnotIndex < mainKnot.KnotIndex))
                            mainKnot = k;
                    }
                }
            }

            if (hovered)
            {
#if UNITY_2022_2_OR_NEWER
                knotColor = Handles.elementPreselectionColor;
                highlightColor = Handles.elementPreselectionColor;
#else
                knotColor = Handles.preselectionColor;
                highlightColor = Handles.preselectionColor;
#endif
            }

            if (hovered || selected)
            {
                using (new Handles.DrawingScope(highlightColor))
                    CurveHandles.DoCurveHighlightCap(knot);
            }
            
            if (knot.Equals(mainKnot))
            {
                s_Knots.Add((knot, selected, hovered, knotColor, rotationDiscColor, drawLinkedKnotHandle));
            }
        }

        internal static void ClearVisibleKnots()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            s_Knots.Clear();
        }

        internal static void DrawVisibleKnots()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            foreach (var knotInfo in s_Knots)
                Draw(knotInfo.knot.Position, knotInfo.knot.Rotation, knotInfo.knotColor, knotInfo.selected, knotInfo.hovered, knotInfo.discColor, k_KnotRotDiscWidthHover, knotInfo.linkedKnot);
        }
        
        public static void DrawInformativeKnot(SelectableKnot knot)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var knotColor = Handles.secondaryColor;

            EditorSplineUtility.GetKnotLinks(knot, k_KnotBuffer);
            var drawLinkedKnotHandle = k_KnotBuffer.Count != 1;

            if(drawLinkedKnotHandle)
            {
                foreach(var k in k_KnotBuffer)
                {
                    //If the current knot in not selected but other linked knots are, skip rendering
                    if(SplineSelection.Contains(k))
                        return;
                }
            }

            var position = knot.Position;
            var rotation = knot.Rotation;
            var size = HandleUtility.GetHandleSize(position);
            using(new Handles.DrawingScope(knotColor, Matrix4x4.TRS(position, rotation, Vector3.one)))
            {
                Handles.DrawSolidDisc(Vector3.zero, Vector3.up, size * SplineHandleUtility.knotDiscRadiusFactorSelected /3f);
            }
        }

        internal static void Draw(SelectableKnot knot, Color knotColor, bool selected, bool hovered)
        {
            EditorSplineUtility.GetKnotLinks(knot, k_KnotBuffer);
            var mainKnot = knot;
            if(k_KnotBuffer.Count != 1)
            {
                foreach(var k in k_KnotBuffer)
                {
                    //Main knot is the older one, the one on the spline of lowest range and the knot of lowest index
                    if(k.SplineInfo.Index < mainKnot.SplineInfo.Index ||
                        k.SplineInfo.Index == mainKnot.SplineInfo.Index && k.KnotIndex < mainKnot.KnotIndex)
                        mainKnot = k;
                }
            }
            if(!mainKnot.Equals(knot))
                return;

            Draw(knot.Position, knot.Rotation, knotColor, selected, hovered, knotColor, k_KnotRotDiscWidthDefault, k_KnotBuffer.Count != 1);
        }

        internal static void Draw(Vector3 position, Quaternion rotation, Color knotColor, bool selected, bool hovered)
        {
            Draw(position, rotation, knotColor, selected, hovered, knotColor, k_KnotRotDiscWidthDefault);
        }

        static void UpdateHandlePoints(float size)
        {
            var startIndex = 5;
            k_HandlePoints[startIndex] = Vector3.forward * k_KnotRotDiscRadius * size;
            var r = Vector3.right * SplineHandleUtility.knotDiscRadiusFactorDefault * size;

            //The first and last element should be in the middle of the points list to get a better visual
            for(int i = 0; i < 9; i++)
            {
                var index = ( i + startIndex + 1 ) % k_HandlePoints.Length;
                var pos = Quaternion.Euler(0, ( 1f - i / 8f ) * 180f, 0) * r;
                k_HandlePoints[index] = pos;
                if(index == k_HandlePoints.Length - 1)
                {
                    startIndex += 1;
                    k_HandlePoints[0] = pos;
                }
            }
        }

        internal static void Draw(Vector3 position, Quaternion rotation, Color knotColor, bool selected, bool hovered, Color discColor, float rotationDiscWidth, bool linkedKnots = false)
        {
            var size = HandleUtility.GetHandleSize(position);

            using (new ZTestScope(CompareFunction.Less))
            {
                using (new Handles.DrawingScope(knotColor, Matrix4x4.TRS(position, rotation, Vector3.one)))
                    DrawKnotShape(size, selected, linkedKnots);

                if (hovered)
                {
                    using (new Handles.DrawingScope(discColor, Matrix4x4.TRS(position, rotation, Vector3.one)))
                        SplineHandleUtility.DrawAAWireDisc(Vector3.zero, Vector3.up, k_KnotRotDiscRadius * size, rotationDiscWidth);
                }
            }

            using (new ZTestScope(CompareFunction.Greater))
            {
                var newKnotColor = new Color(knotColor.r, knotColor.g, knotColor.b, knotColor.a * k_ColorAlphaFactor);
                using (new Handles.DrawingScope(newKnotColor, Matrix4x4.TRS(position, rotation, Vector3.one)))
                    DrawKnotShape(size, selected, linkedKnots);
                
                if (hovered)
                {
                    var newDiscColor = new Color(discColor.r, discColor.g, discColor.b, discColor.a * k_ColorAlphaFactor);
                    using (new Handles.DrawingScope(newDiscColor, Matrix4x4.TRS(position, rotation, Vector3.one)))
                        SplineHandleUtility.DrawAAWireDisc(Vector3.zero, Vector3.up, k_KnotRotDiscRadius * size, rotationDiscWidth);
                }
            }
        }

        static void DrawKnotShape(float size, bool selected, bool linkedKnots)
        {
            if (!linkedKnots)
            {
                UpdateHandlePoints(size);
                Handles.DrawAAPolyLine(SplineHandleUtility.denseLineAATex, k_KnotHandleWidth, k_HandlePoints);
                if (selected)
                    Handles.DrawAAConvexPolygon(k_HandlePoints);
            }
            else
            {
                // Knot disc
                if (selected)
                {
                    var radius = selected ? SplineHandleUtility.knotDiscRadiusFactorSelected : SplineHandleUtility.knotDiscRadiusFactorHover;
                    Handles.DrawSolidDisc(Vector3.zero, Vector3.up, radius * size);
                }
                else
                    Handles.DrawWireDisc(Vector3.zero, Vector3.up, SplineHandleUtility.knotDiscRadiusFactorDefault * size, SplineHandleUtility.handleWidth * SplineHandleUtility.aliasedLineSizeMultiplier);
            }
            
            Handles.DrawAAPolyLine(Vector3.zero, Vector3.up * 2f * SplineHandleUtility.sizeFactor * size);
        }
    }
}