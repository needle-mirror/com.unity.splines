using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class TangentHandles
    {
        const float k_TangentLineWidthDefault = 2f;
        const float k_TangentLineWidthHover = 3.5f;
        const float k_TangentLineWidthSelected = 4.5f;
        const float k_TangentStartOffsetFromKnot = 0.22f;
        const float k_tangentEndOffsetFromHandle = 0.11f;

        public static void DrawInformativeTangent(SelectableTangent tangent, bool active = true)
        {
            DrawInformativeTangent(tangent.Position, tangent.Owner.Position, active);
        }

        public static void DrawInformativeTangent(Vector3 position, Vector3 knotPosition, bool active = true)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var tangentColor = SplineHandleUtility.knotColor;
            if (!active)
                tangentColor = Handles.secondaryColor;

            var tangentArmColor = tangentColor == SplineHandleUtility.knotColor
                ? SplineHandleUtility.tangentColor
                : tangentColor;

            using (new ColorScope(tangentArmColor))
            {
                var toTangent = position - knotPosition;
                var toTangentNorm = math.normalize(toTangent);
                var length = math.length(toTangent);
                var knotHandleOffset = HandleUtility.GetHandleSize(knotPosition) * k_TangentStartOffsetFromKnot;

                length = Mathf.Max(0f, length - knotHandleOffset);
                knotPosition += (Vector3)toTangentNorm * knotHandleOffset;

                SplineHandleUtility.DrawLineWithWidth(knotPosition, knotPosition  + (Vector3)toTangentNorm * length, k_TangentLineWidthDefault);
            }
        }

        public static void Draw(Vector3 position, Vector3 knotPosition, bool active = true)
        {
            Draw(-1, position, Quaternion.identity,  knotPosition, false, false, false, TangentMode.Broken, active);
        }

        public static void Draw(int controlId, SelectableTangent tangent, bool active = true)
        {
            var owner = tangent.Owner;
            Draw(
                controlId,
                tangent.Position,
                TransformOperation.CalculateElementSpaceHandleRotation(math.length(tangent.LocalPosition) > 0 ? (ISplineElement)tangent : tangent.Owner),
                owner.Position,
                SplineSelection.Contains(tangent),
                SplineSelection.Contains(tangent.OppositeTangent),
                SplineHandleUtility.IsLastHoveredTangent(tangent.OppositeTangent),
                owner.Mode,
                active);
        }

        public static void Draw(int controlId, Vector3 position, Quaternion rotation, Vector3 knotPosition, bool selected, bool oppositeSelected, bool oppositeHovered, TangentMode mode, bool active)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var size = HandleUtility.GetHandleSize(position);
            var hovered = HandleUtility.nearestControl == controlId;

            var tangentColor = SplineHandleUtility.knotColor;
            if (selected)
                tangentColor = Handles.selectedColor;
            else if (hovered)
                tangentColor = Handles.preselectionColor;

            if (!active)
                tangentColor = Handles.secondaryColor;

            var tangentArmColor = tangentColor == SplineHandleUtility.knotColor
                ? SplineHandleUtility.tangentColor
                : tangentColor;
            if (tangentArmColor == SplineHandleUtility.tangentColor && oppositeSelected && mode != TangentMode.Broken )
                tangentArmColor = Handles.selectedColor;

            using (new ColorScope(tangentArmColor))
            {
                var width = k_TangentLineWidthDefault;
                if (selected || (mode == TangentMode.Mirrored && oppositeSelected))
                    width = k_TangentLineWidthSelected;
                else if (hovered || (mode == TangentMode.Mirrored && oppositeHovered))
                    width = k_TangentLineWidthHover;

                var tex = width > k_TangentLineWidthDefault ? SplineHandleUtility.thickTangentLineAATex : null;

                var startPos = knotPosition;
                var toTangent = position - knotPosition;
                var toTangentNorm = math.normalize(toTangent);
                var length = math.length(toTangent);

                var knotHandleSize = HandleUtility.GetHandleSize(startPos);
                var knotHandleOffset = knotHandleSize * k_TangentStartOffsetFromKnot;
                var tangentHandleOffset = size * k_tangentEndOffsetFromHandle;
                // Reduce the length slightly, so that there's some space between tangent line endings and handles.
                length = Mathf.Max(0f, length - knotHandleOffset - tangentHandleOffset);
                startPos += (Vector3)toTangentNorm * knotHandleOffset;
                SplineHandleUtility.DrawLineWithWidth(startPos + (Vector3)toTangentNorm * length, startPos, width, tex);
            }

            using (new Handles.DrawingScope(tangentColor, Matrix4x4.TRS(position, rotation, Vector3.one)))
            {
                if (selected || hovered)
                {
                    var radius = (selected ? SplineHandleUtility.knotDiscRadiusFactorSelected : SplineHandleUtility.knotDiscRadiusFactorHover) * size;
                    // As Handles.DrawSolidDisc has no thickness parameter, we're drawing a wire disc here so that the solid disc has thickness when viewed from a shallow angle.
                    Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius * 0.7f, SplineHandleUtility.handleWidthHover);
                    Handles.DrawSolidDisc(Vector3.zero, Vector3.up, radius);
                }
                else
                    Handles.DrawWireDisc(Vector3.zero, Vector3.up, SplineHandleUtility.knotDiscRadiusFactorDefault * size,
                        SplineHandleUtility.handleWidthHover * SplineHandleUtility.aliasedLineSizeMultiplier);
            }
        }
    }
}