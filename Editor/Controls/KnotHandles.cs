using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class KnotHandles
    {
        const float k_KnotRotDiscRadius = 0.18f;
        const float k_KnotRotDiscWidthDefault = 1.5f;
        const float k_KnotRotDiscWidthHover = 3f;
        const float k_KnotRotDiscWidthSelected = 4f;

        static readonly List<SelectableKnot> s_KnotBuffer = new List<SelectableKnot>();

        public static void Draw(int controlId, SelectableKnot knot, List<int> tangentControlIDs = null, bool preview = false, bool activeSpline = true)
        {
            if (Event.current.GetTypeForControl(controlId) != EventType.Repaint)
                return;

            var mirroredTangentSelected = false;
            var mirroredTangentHovered = false;

            if (tangentControlIDs != null && (knot.Mode == TangentMode.Mirrored || knot.Mode == TangentMode.Continuous))
            {
                mirroredTangentSelected |= SplineSelection.Contains(knot.TangentIn);
                mirroredTangentSelected |= SplineSelection.Contains(knot.TangentOut);

                if (!mirroredTangentSelected)
                {
                    foreach (var tangentID in tangentControlIDs)
                    {
                        if (HandleUtility.nearestControl == tangentID)
                            mirroredTangentHovered = true;
                    }
                }
            }

            var hovered = HandleUtility.nearestControl == controlId;
            bool selected = SplineSelection.Contains(knot);

            var knotColor = SplineHandleUtility.knotColor;
            if (preview)
                knotColor = Color.Lerp(Color.gray, Color.white, 0.5f);
            else if (selected)
                knotColor = Handles.selectedColor;
            else if (controlId > 0 && GUIUtility.hotControl == 0 && hovered)
                knotColor = Handles.preselectionColor;

            if (!activeSpline)
                knotColor = Handles.secondaryColor;

            // Knot rotation indicators
            var rotationDiscColor = knotColor;
            if (!selected && mirroredTangentSelected)
                rotationDiscColor = Handles.selectedColor;

            var rotationDiscWidth = k_KnotRotDiscWidthDefault;
            if (selected || mirroredTangentSelected)
                rotationDiscWidth = k_KnotRotDiscWidthSelected;
            else if (hovered || mirroredTangentHovered)
                rotationDiscWidth = k_KnotRotDiscWidthHover;

            if(hovered)
            {
                EditorSplineUtility.GetKnotLinks(knot, s_KnotBuffer);
                foreach(var k in s_KnotBuffer)
                {
                    Draw(k.Position, k.Rotation, knotColor, selected, hovered, rotationDiscColor, rotationDiscWidth);
                    using(new Handles.DrawingScope(knotColor))
                        CurveHandles.DoCurveHighlightCap(k);
                }
            }
            else if(selected)
            {
                Draw(knot.Position, knot.Rotation, knotColor, selected, hovered, rotationDiscColor, rotationDiscWidth);
                using(new Handles.DrawingScope(knotColor))
                    CurveHandles.DoCurveHighlightCap(knot);
            }
            else
                Draw(knot.Position, knot.Rotation, knotColor, selected, hovered, rotationDiscColor, rotationDiscWidth);
        }

        internal static void Draw(Vector3 position, Quaternion rotation, Color knotColor, bool selected, bool hovered)
        {
            Draw(position, rotation, knotColor, selected, hovered, knotColor, k_KnotRotDiscWidthDefault);
        }

        internal static void Draw(Vector3 position, Quaternion rotation, Color knotColor, bool selected, bool hovered, Color rotationDiscColor, float rotationDiscWidth)
        {
            var size = HandleUtility.GetHandleSize(position);

            using (new Handles.DrawingScope(knotColor, Matrix4x4.TRS(position, rotation, Vector3.one)))
            {
                // Knot disc
                if (selected || hovered)
                {
                    var radius = selected ? SplineHandleUtility.knotDiscRadiusFactorSelected : SplineHandleUtility.knotDiscRadiusFactorHover;
                    Handles.DrawSolidDisc(Vector3.zero, Vector3.up, radius * size);
                }
                else
                    Handles.DrawWireDisc(Vector3.zero, Vector3.up, SplineHandleUtility.knotDiscRadiusFactorDefault * size, SplineHandleUtility.handleWidthHover * SplineHandleUtility.aliasedLineSizeMultiplier);
            }

            using (new Handles.DrawingScope(rotationDiscColor, Matrix4x4.TRS(position, rotation, Vector3.one)))
            {
                SplineHandleUtility.DrawAAWireDisc(Vector3.zero, Vector3.up, k_KnotRotDiscRadius * size, rotationDiscWidth);

                Handles.DrawAAPolyLine(Vector3.zero, Vector3.up * 2f * SplineHandleUtility.sizeFactor * size);
            }
        }
    }
}