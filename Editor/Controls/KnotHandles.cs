using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

        internal static void Do(int controlId, SelectableKnot knot, bool selected = false, bool hovered = false)
        {
            if (Event.current.GetTypeForControl(controlId) != EventType.Repaint)
                return;

            //Hovered might not be available if a TRS tool is in use
            hovered &= SplineHandleUtility.IsHoverAvailableForSplineElement();

            var knotColor = SplineHandleUtility.elementColor;
            var rotationDiscColor = SplineHandleUtility.elementPreselectionColor;
            if (hovered)
                knotColor = SplineHandleUtility.elementPreselectionColor;
            else if (selected)
                knotColor = SplineHandleUtility.elementSelectionColor;

            Draw(knot.Position, knot.Rotation, knotColor, selected, hovered, rotationDiscColor, k_KnotRotDiscWidthHover);
            DrawKnotIndices(knot);
        }

        internal static void Draw(int controlId, SelectableKnot knot)
        {
            if (Event.current.GetTypeForControl(controlId) != EventType.Repaint)
                return;

            if(!knot.IsValid())
                return;

            var selected = SplineSelection.Contains(knot);
            var knotHovered = SplineHandleUtility.IsElementHovered(controlId);

            //Retrieving linked knots
            EditorSplineUtility.GetKnotLinks(knot, k_KnotBuffer);
            var drawLinkedKnotHandle = k_KnotBuffer.Count != 1;
            var mainKnot = knot;

            SelectableKnot lastHovered = new SelectableKnot();
            // Retrieving the last hovered element
            // SplineHandleUtility.lastHoveredElement is pointing either to:
            // - the hovered Knot and the ID is pointing to the controlID of that knot in that case
            // - if a curve is hovered, the element is the knot closest to the hovered part of the curve (start or end knot depending)
            //   and the controlID is the one of the curve
            var lastHoveredElementIsKnot = SplineHandleUtility.lastHoveredElement is SelectableKnot;
            if (lastHoveredElementIsKnot)
                lastHovered = (SelectableKnot)SplineHandleUtility.lastHoveredElement;

            var isCurveId = SplineHandles.IsCurveId(SplineHandleUtility.lastHoveredElementId);

            var curveIsHovered = lastHoveredElementIsKnot &&
                k_KnotBuffer.Contains(lastHovered) &&
                isCurveId;

            var hovered = knotHovered || (curveIsHovered && knot.Equals(lastHovered));

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
                    foreach (var linkedKnot in k_KnotBuffer)
                    {
                        if (!hovered)
                        {
                            var kSelected = SplineSelection.Contains(linkedKnot);

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
                        if ((!SplineSelection.HasActiveSplineSelection() || SplineSelection.Contains(linkedKnot.SplineInfo)) &&
                            (linkedKnot.SplineInfo.Index < mainKnot.SplineInfo.Index ||
                                linkedKnot.SplineInfo.Index == mainKnot.SplineInfo.Index && linkedKnot.KnotIndex < mainKnot.KnotIndex))
                            mainKnot = linkedKnot;
                    }
                }
            }

            //Hovered might not be available if a TRS tool is in use
            hovered &= SplineHandleUtility.IsHoverAvailableForSplineElement();

            var knotColor = SplineHandleUtility.elementColor;
            var highlightColor = SplineHandleUtility.elementPreselectionColor;
            var rotationDiscColor = SplineHandleUtility.elementPreselectionColor;
            if (hovered)
            {
                knotColor = SplineHandleUtility.elementPreselectionColor;
                highlightColor = SplineHandleUtility.elementPreselectionColor;
            }
            else if (selected)
            {
                knotColor = SplineHandleUtility.elementSelectionColor;
                highlightColor = SplineHandleUtility.elementSelectionColor;
            }

            if (SplineHandleUtility.canDrawOnCurves && (hovered || selected))
            {
                using (new Handles.DrawingScope(highlightColor))
                    CurveHandles.DoCurveHighlightCap(knot);
            }

            if (knot.Equals(mainKnot))
            {
                s_Knots.Add((knot, selected, hovered, knotColor, rotationDiscColor, drawLinkedKnotHandle));
                DrawKnotIndices(knot);
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

        static void DrawKnotIndices(SelectableKnot knot)
        {
            if (!SplineHandleSettings.ShowKnotIndices)
                return;

            var hasLinkedKnots = !(k_KnotBuffer.Count == 1 && k_KnotBuffer.Contains(knot));
            if (k_KnotBuffer != null && k_KnotBuffer.Count > 0 && hasLinkedKnots)
            {
                var stringBuilder = new System.Text.StringBuilder("[");
                for (var i = 0; i < k_KnotBuffer.Count; i++)
                {
                    stringBuilder.Append($"({k_KnotBuffer[i].SplineInfo.Index},{k_KnotBuffer[i].KnotIndex})");
                    if (i != k_KnotBuffer.Count - 1)
                        stringBuilder.Append(", ");
                }

                stringBuilder.Append("]");
                Handles.Label(knot.Position, stringBuilder.ToString());
            }
            else
            {
                Handles.Label(knot.Position, $"[{knot.KnotIndex}]");
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

        internal static void DrawInformativeKnot(SelectableKnot knot, float sizeFactor = 0.5f)
        {
            if (Event.current.type != EventType.Repaint)
                return;

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

            DrawInformativeKnotVisual(knot, SplineHandleUtility.lineColor, sizeFactor);
        }

        static void DrawInformativeKnotVisual(SelectableKnot knot, Color knotColor, float sizeFactor = 0.5f)
        {
            var position = knot.Position;
            var size = HandleUtility.GetHandleSize(position);
            using(new Handles.DrawingScope(knotColor, Matrix4x4.TRS(position, knot.Rotation, Vector3.one)))
            {
                Handles.DrawSolidDisc(Vector3.zero, Vector3.up, size * SplineHandleUtility.knotDiscRadiusFactorSelected * sizeFactor);
            }
        }

        static void Draw(Vector3 position, Quaternion rotation, Color knotColor, bool selected, bool hovered, Color discColor, float rotationDiscWidth, bool linkedKnots = false)
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
                    var newDiscColor = new Color(discColor.r, discColor.g,
                        discColor.b, discColor.a * k_ColorAlphaFactor);
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
