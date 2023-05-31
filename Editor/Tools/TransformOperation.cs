using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    static class TransformOperation
    {
        [Flags]
        public enum PivotFreeze
        {
            None = 0,
            Position = 1,
            Rotation = 2,
            All = Position | Rotation
        }

        struct TransformData
        {
            internal float3 position;
            internal float3 inTangentDirection;
            internal float3 outTangentDirection;

            internal static TransformData GetData(ISelectableElement element)
            {
                var tData = new TransformData();
                tData.position = new float3(element.Position);
                var knot = new SelectableKnot(element.SplineInfo, element.KnotIndex);
                tData.inTangentDirection = knot.TangentIn.Direction;
                tData.outTangentDirection = knot.TangentOut.Direction;

                return tData;
            }
        }

        struct RotationSyncData
        {
            quaternion m_RotationDelta;
            float m_MagnitudeDelta;
            float m_ScaleMultiplier; // Only used for scale operation
            bool m_Initialized;

            public bool initialized => m_Initialized;
            public quaternion rotationDelta => m_RotationDelta;
            public float magnitudeDelta => m_MagnitudeDelta;
            public float scaleMultiplier => m_ScaleMultiplier;

            public void Initialize(quaternion rotationDelta, float magnitudeDelta, float scaleMultiplier)
            {
                m_RotationDelta = rotationDelta;
                m_MagnitudeDelta = magnitudeDelta;
                m_ScaleMultiplier = scaleMultiplier;
                m_Initialized = true;
            }

            public void Clear()
            {
                m_RotationDelta = quaternion.identity;
                m_MagnitudeDelta = 0f;
                m_ScaleMultiplier = 1f;
                m_Initialized = false;
            }
        }

        static readonly List<ISelectableElement> s_ElementSelection = new List<ISelectableElement>(32);

        public static IReadOnlyList<ISelectableElement> elementSelection => s_ElementSelection;

        static int s_ElementSelectionCount = 0;

        public static bool canManipulate => s_ElementSelectionCount > 0;

        static ISelectableElement currentElementSelected
            => canManipulate ? s_ElementSelection[0] : null;

        static Vector3 s_PivotPosition;
        public static Vector3 pivotPosition => s_PivotPosition;

        static quaternion s_HandleRotation;
        public static quaternion handleRotation => s_HandleRotation;

        //Caching rotation inverse for rotate and scale operations
        static quaternion s_HandleRotationInv;

        public static PivotFreeze pivotFreeze { get; set; }

        static TransformData[] s_MouseDownData;

        // Used to prevent same knot being rotated multiple times during a transform operation in Rotation Sync mode.
        static HashSet<SelectableKnot> s_RotatedKnotCache = new HashSet<SelectableKnot>();

        // Used to prevent the translation of the same knot multiple times if a linked knot was moved
        static HashSet<SelectableKnot> s_LinkedKnotCache = new HashSet<SelectableKnot>();

        static readonly List<SelectableKnot> s_KnotBuffer = new List<SelectableKnot>();
        static RotationSyncData s_RotationSyncData = new RotationSyncData();

        internal static void UpdateSelection(IEnumerable<Object> selection)
        {
            SplineSelection.GetElements(EditorSplineUtility.GetSplinesFromTargetsInternal(selection), s_ElementSelection);
            s_ElementSelectionCount = s_ElementSelection.Count;
            if (s_ElementSelectionCount > 0)
            {
                UpdatePivotPosition();
                UpdateHandleRotation();
            }
        }

        internal static void UpdatePivotPosition(bool useKnotPositionForTangents = false)
        {
            if ((pivotFreeze & PivotFreeze.Position) != 0)
                return;

            switch (Tools.pivotMode)
            {
                case PivotMode.Center:
                    s_PivotPosition = EditorSplineUtility.GetElementBounds(s_ElementSelection, useKnotPositionForTangents).center;
                    break;

                case PivotMode.Pivot:
                    if (s_ElementSelectionCount == 0)
                        goto default;

                    var element = s_ElementSelection[0];
                    if (useKnotPositionForTangents && element is SelectableTangent tangent)
                        s_PivotPosition = tangent.Owner.Position;
                    else
                        s_PivotPosition = element.Position;
                    break;

                default:
                    s_PivotPosition = Vector3.positiveInfinity;
                    break;
            }
        }

        // A way to set pivot position for situations, when by design, pivot position does
        // not necessarily match the pivot of selected elements.
        internal static void ForcePivotPosition(float3 position)
        {
            s_PivotPosition = position;
        }

        internal static void UpdateHandleRotation()
        {
            if ((pivotFreeze & PivotFreeze.Rotation) != 0)
                return;

            var handleRotation = Tools.handleRotation;
            if (canManipulate && (SplineTool.handleOrientation == HandleOrientation.Element || SplineTool.handleOrientation == HandleOrientation.Parent))
            {
                var curElement = TransformOperation.currentElementSelected;

                if (SplineTool.handleOrientation == HandleOrientation.Element)
                    handleRotation = EditorSplineUtility.GetElementRotation(curElement);
                else if (curElement is SelectableTangent editableTangent)
                    handleRotation = EditorSplineUtility.GetElementRotation(editableTangent.Owner);
            }

            s_HandleRotation = handleRotation;
            s_HandleRotationInv = math.inverse(s_HandleRotation);
        }

        public static void ApplyTranslation(float3 delta)
        {
            s_RotatedKnotCache.Clear();
            s_LinkedKnotCache.Clear();

            foreach (var element in s_ElementSelection)
            {
                if (element is SelectableKnot knot)
                {
                    if (!s_LinkedKnotCache.Contains(knot))
                    {
                        knot.Position = ApplySmartRounding(knot.Position + delta);

                        EditorSplineUtility.GetKnotLinks(knot, s_KnotBuffer);
                        foreach (var k in s_KnotBuffer)
                            s_LinkedKnotCache.Add(k);

                        if (!s_RotationSyncData.initialized)
                            s_RotationSyncData.Initialize(quaternion.identity, 0f, 1f);
                    }
                }
                else if (element is SelectableTangent tangent)
                {
                    knot = tangent.Owner;
                    //Do nothing on the tangent if the knot is also in the selection
                    if (s_ElementSelection.Contains(knot))
                        continue;

                    if (OppositeTangentSelected(tangent))
                        knot.Mode = TangentMode.Broken;

                    if (knot.Mode == TangentMode.Broken)
                        tangent.Position = ApplySmartRounding(knot.Position + tangent.Direction + delta);
                    else
                    {
                        if (s_RotatedKnotCache.Contains(knot))
                            continue;
                        
                        // Build rotation sync data based on active selection's transformation
                        if (!s_RotationSyncData.initialized)
                        {
                            var newTangentPosWorld = knot.Position + tangent.Direction + delta;
                            var deltas = CalculateMirroredTangentTranslationDeltas(tangent, newTangentPosWorld);
                            
                            s_RotationSyncData.Initialize(deltas.knotRotationDelta, deltas.tangentLocalMagnitudeDelta, 1f);
                        }
                        ApplyTangentRotationSyncTransform(tangent);
                    }
                }
            }

            s_RotationSyncData.Clear();
        }
        
        public static void ApplyRotation(Quaternion deltaRotation, float3 rotationCenter)
        {
            s_RotatedKnotCache.Clear();

            foreach (var element in s_ElementSelection)
            {
                if (element is SelectableKnot knot)
                {
                    var knotRotation = knot.Rotation;
                    RotateKnot(knot, deltaRotation, rotationCenter);
                    if (!s_RotationSyncData.initialized)
                        s_RotationSyncData.Initialize(math.mul(math.inverse(knotRotation), knot.Rotation), 0f, 1f);
                }
                else if (element is SelectableTangent tangent && !s_ElementSelection.Contains(tangent.Owner))
                {
                    knot = tangent.Owner;
                    if (knot.Mode == TangentMode.Broken)
                    {
                        if (Tools.pivotMode == PivotMode.Pivot)
                            rotationCenter = knot.Position;

                        var mode = knot.Mode;

                        var deltaPos = math.rotate(deltaRotation, tangent.Position - rotationCenter);
                        tangent.Position = deltaPos + rotationCenter;
                    }
                    else
                    {
                        if (s_RotatedKnotCache.Contains(tangent.Owner))
                            continue;

                        deltaRotation.ToAngleAxis(out var deltaRotationAngle, out var deltaRotationAxis);

                        if (math.abs(deltaRotationAngle) > 0f)
                        {
                            if (knot.Mode != TangentMode.Broken)
                            {
                                // If we're in center pivotMode and both tangents of the same knot are in selection, enter Broken mode under these conditions:
                                if (Tools.pivotMode == PivotMode.Center && OppositeTangentSelected(tangent))
                                {
                                    var knotToCenter = (float3) rotationCenter - knot.Position;
                                    // 1) Rotation center does not match owner knot's position
                                    if (!Mathf.Approximately(math.length(knotToCenter), 0f))
                                    {
                                        var similarity = Math.Abs(Vector3.Dot(math.normalize(deltaRotationAxis),
                                            math.normalize(knotToCenter)));
                                        // 2) Both rotation center and knot, are not on rotation delta's axis
                                        if (!Mathf.Approximately(similarity, 1f))
                                            knot.Mode = TangentMode.Broken;
                                    }
                                }
                            }

                            // Build rotation sync data based on active selection's transformation
                            if (!s_RotationSyncData.initialized)
                            {
                                if (Tools.pivotMode == PivotMode.Pivot)
                                    s_RotationSyncData.Initialize(deltaRotation, 0f, 1f);
                                else
                                {
                                    var deltaPos = math.rotate(deltaRotation, tangent.Position - rotationCenter);
                                    var knotToRotationCenter = rotationCenter - tangent.Owner.Position;
                                    var targetDirection = knotToRotationCenter + deltaPos;
                                    var tangentNorm = math.normalize(tangent.Direction);
                                    var axisDotTangent = math.dot(math.normalize(deltaRotationAxis), tangentNorm);
                                    var toRotCenterDotTangent = math.length(knotToRotationCenter) > 0f
                                        ? math.dot(math.normalize(knotToRotationCenter), tangentNorm)
                                        : 1f;
                                    quaternion knotRotationDelta;
                                    // In center pivotMode, use handle delta only if our handle delta rotation's axis
                                    // matches knot's active selection tangent direction and rotation center is on the tangent's axis.
                                    // This makes knot roll possible when element selection list only contains one or both tangents of a single knot.
                                    if (Mathf.Approximately(math.abs(axisDotTangent), 1f) &&
                                        Mathf.Approximately(math.abs(toRotCenterDotTangent), 1f))
                                        knotRotationDelta = deltaRotation;
                                    else
                                        knotRotationDelta = Quaternion.FromToRotation(tangent.Direction, targetDirection);

                                    var scaleMultiplier = math.length(targetDirection) / math.length(tangent.Direction);

                                    s_RotationSyncData.Initialize(knotRotationDelta, 0f, scaleMultiplier);
                                }
                            }

                            ApplyTangentRotationSyncTransform(tangent, false);
                        }
                    }
                }
            }

            s_RotationSyncData.Clear();
        }

        static bool OppositeTangentSelected(SelectableTangent tangent)
        {
            if (tangent.Owner.Mode != TangentMode.Broken)
                if (s_ElementSelection.Contains(tangent.OppositeTangent))
                    return true;

            return false;
        }

        static void RotateKnot(SelectableKnot knot, quaternion deltaRotation, float3 rotationCenter, bool allowTranslation = true)
        {
            var knotInBrokenMode = knot.Mode == TangentMode.Broken;
            if (!knotInBrokenMode && s_RotatedKnotCache.Contains(knot))
                return;

            if (allowTranslation && Tools.pivotMode == PivotMode.Center)
            {
                var dir = knot.Position - rotationCenter;

                if (SplineTool.handleOrientation == HandleOrientation.Element || SplineTool.handleOrientation == HandleOrientation.Parent)
                    knot.Position = math.rotate(deltaRotation, dir) + rotationCenter;
                else
                    knot.Position = math.rotate(s_HandleRotation, math.rotate(deltaRotation, math.rotate(s_HandleRotationInv, dir))) + rotationCenter;
            }

            if (SplineTool.handleOrientation == HandleOrientation.Element || SplineTool.handleOrientation == HandleOrientation.Parent)
            {
                if (Tools.pivotMode == PivotMode.Center)
                    knot.Rotation = math.mul(deltaRotation, knot.Rotation);
                else
                {
                    var handlePivotModeRot = math.mul(GetCurrentSelectionKnot().Rotation, math.inverse(knot.Rotation));
                    knot.Rotation = math.mul(math.inverse(handlePivotModeRot), math.mul(deltaRotation, math.mul(handlePivotModeRot, knot.Rotation)));
                }
            }
            else
                knot.Rotation = math.mul(s_HandleRotation, math.mul(deltaRotation, math.mul(s_HandleRotationInv, knot.Rotation)));

            s_RotatedKnotCache.Add(knot);
        }

        public static void ApplyScale(float3 scale)
        {
            s_RotatedKnotCache.Clear();
            ISelectableElement[] scaledElements = new ISelectableElement[s_ElementSelectionCount];

            for (int elementIndex = 0; elementIndex < s_ElementSelectionCount; elementIndex++)
            {
                var element = s_ElementSelection[elementIndex];
                if (element is SelectableKnot knot)
                {
                    ScaleKnot(knot, elementIndex, scale);

                    if (!s_RotationSyncData.initialized)
                        s_RotationSyncData.Initialize(quaternion.identity, 0f, 1f);
                }
                else if (element is SelectableTangent tangent && !s_ElementSelection.Contains(tangent.Owner))
                {
                    var owner = tangent.Owner;
                    var restoreMode = false;
                    var mode = owner.Mode;
                    var scaleDelta = scale - new float3(1f, 1f, 1f);
                    if (mode != TangentMode.Broken && math.length(scaleDelta) > 0f)
                    {
                        // If we're in center pivotMode and both tangents of the same knot are in selection
                        if (Tools.pivotMode == PivotMode.Center && OppositeTangentSelected(tangent))
                        {
                            var knotToCenter = (float3) pivotPosition - owner.Position;
                            //  Enter broken mode if scale operation center does not match owner knot's position
                            if (!Mathf.Approximately(math.length(knotToCenter), 0f))
                            {
                                owner.Mode = TangentMode.Broken;
                                var similarity = Math.Abs(Vector3.Dot(math.normalize(scaleDelta),
                                    math.normalize(knotToCenter)));
                                // If scale center and knot are both on an axis that's orthogonal to scale operation's axis,
                                // mark knot for mode restore so that mirrored/continous modes can be restored
                                if (Mathf.Approximately(similarity, 0f))
                                    restoreMode = true;
                            }
                        }
                    }

                    var index = Array.IndexOf(scaledElements, element);
                    if (index == -1) //element not scaled yet
                    {
                        if (owner.Mode == TangentMode.Broken)
                            tangent.Position = ScaleTangent(tangent, s_MouseDownData[elementIndex].position, scale);
                        else
                        {
                            // Build rotation sync data based on active selection's transformation
                            if (!s_RotationSyncData.initialized)
                            {
                                var newTangentPosWorld = ScaleTangent(tangent, s_MouseDownData[elementIndex].position, scale);
                                var deltas = CalculateMirroredTangentTranslationDeltas(tangent, newTangentPosWorld);
                                var scaleMultiplier = 1f + deltas.tangentLocalMagnitudeDelta / math.length(tangent.LocalDirection);

                                s_RotationSyncData.Initialize(deltas.knotRotationDelta, 0f, scaleMultiplier);
                            }

                            if (owner.Mode == TangentMode.Mirrored && s_RotatedKnotCache.Contains(owner))
                                continue;

                            ApplyTangentRotationSyncTransform(tangent, false);
                        }

                        if (restoreMode)
                            owner.Mode = mode;
                    }
                }

                scaledElements[elementIndex] = element;
            }

            s_RotationSyncData.Clear();
        }

        static void ScaleKnot(SelectableKnot knot, int dataIndex, float3 scale)
        {
            if (Tools.pivotMode == PivotMode.Center)
            {
                var deltaPos = math.rotate(s_HandleRotationInv,
                    s_MouseDownData[dataIndex].position - (float3) pivotPosition);
                var deltaPosKnot = deltaPos * scale;
                knot.Position = math.rotate(s_HandleRotation, deltaPosKnot) + (float3) pivotPosition;
            }

            var tangent = knot.TangentIn;
            tangent.Direction = math.rotate(s_HandleRotation, math.rotate(s_HandleRotationInv, s_MouseDownData[dataIndex].inTangentDirection) * scale);
            tangent = knot.TangentOut;
            tangent.Direction = math.rotate(s_HandleRotation, math.rotate(s_HandleRotationInv, s_MouseDownData[dataIndex].outTangentDirection) * scale);
        }

        static float3 ScaleTangent(SelectableTangent tangent, float3 originalPosition, float3 scale)
        {
            var scaleCenter = Tools.pivotMode == PivotMode.Center ? (float3) pivotPosition : tangent.Owner.Position;

            var deltaPos = math.rotate(s_HandleRotationInv, originalPosition - scaleCenter) * scale;
            return math.rotate(s_HandleRotation, deltaPos) + scaleCenter;
        }

        static void ApplyTangentRotationSyncTransform(SelectableTangent tangent, bool absoluteScale = true)
        {
            if (tangent.Equals(currentElementSelected) ||
                tangent.Owner.Mode == TangentMode.Mirrored ||
                (!absoluteScale && tangent.Owner.Mode == TangentMode.Continuous))
            {
                if (absoluteScale)
                {
                    var localDirection = tangent.LocalDirection;
                    if (Mathf.Approximately(math.length(localDirection), 0f))
                        localDirection = new float3(0, 0, 1f);
                    
                    tangent.LocalDirection += math.normalizesafe(localDirection) * s_RotationSyncData.magnitudeDelta;
                }
                else
                    tangent.LocalDirection *=  s_RotationSyncData.scaleMultiplier;
            }

            RotateKnot(tangent.Owner, s_RotationSyncData.rotationDelta, tangent.Owner.Position, false);
        }

        /*
         Given a mirrored tangent and a target position, calculate the knot rotation delta and tangent's local magnitude change required to 
         put the tangent into target world position while fully respecting the owner spline's transformation (including non-uniform scale).
         */
        internal static (quaternion knotRotationDelta, float tangentLocalMagnitudeDelta) CalculateMirroredTangentTranslationDeltas(SelectableTangent tangent, float3 targetPosition)
        {
            var knot = tangent.Owner;
            var splineTrsInv = math.inverse(knot.SplineInfo.LocalToWorld);
            var splineTrs = knot.SplineInfo.LocalToWorld;
            var splinePos = splineTrs.c3.xyz;
            var splineRotation = new quaternion(splineTrs);

            var unscaledTargetPos = splinePos + math.rotate(splineRotation, math.transform(splineTrsInv, targetPosition));
            var unscaledCurrentPos = splinePos + math.rotate(splineRotation, math.transform(splineTrsInv, tangent.Position));
            var unscaledKnotPos = splinePos + math.rotate(splineRotation, math.transform(splineTrsInv, knot.Position));

            var knotRotationInv = math.inverse(knot.Rotation);
            var forward = (tangent.TangentIndex == 0 ? -1f : 1f) * math.normalizesafe(unscaledTargetPos - unscaledKnotPos);
            var up = math.mul(knot.Rotation, math.up());
            var knotLookAtRotation = quaternion.LookRotationSafe(forward, up);
            var knotRotationDelta = math.mul(knotLookAtRotation, knotRotationInv);

            var targetLocalDirection = math.rotate(knotRotationInv, (unscaledTargetPos - unscaledKnotPos));
            var tangentLocalMagnitudeDelta = math.length(targetLocalDirection) - math.length(tangent.LocalDirection);

            return (knotRotationDelta, tangentLocalMagnitudeDelta);
        }

        static SelectableKnot GetCurrentSelectionKnot()
        {
            if (currentElementSelected == null)
                return default;

            if (currentElementSelected is SelectableTangent tangent)
                return tangent.Owner;

            if (currentElementSelected is SelectableKnot knot)
                return knot;

            return default;
        }

        public static void RecordMouseDownState()
        {
            s_MouseDownData = new TransformData[s_ElementSelectionCount];
            for (int i = 0; i < s_ElementSelectionCount; i++)
            {
                s_MouseDownData[i] = TransformData.GetData(s_ElementSelection[i]);
            }
        }

        public static void ClearMouseDownState()
        {
            s_MouseDownData = null;
        }

        public static Bounds GetSelectionBounds(bool useKnotPositionForTangents = false)
        {
            return EditorSplineUtility.GetElementBounds(s_ElementSelection, useKnotPositionForTangents);
        }

        public static float3 ApplySmartRounding(float3 position)
        {
            //If we are snapping, disable the smart rounding. If not the case, the transform will have the wrong snap value based on distance to screen.
#if UNITY_2022_2_OR_NEWER
            if (EditorSnapSettings.incrementalSnapActive || EditorSnapSettings.gridSnapActive)
                return position;
#endif

            float3 minDifference = SplineHandleUtility.GetMinDifference(position);
            for (int i = 0; i < 3; ++i)
                position[i] = Mathf.Approximately(position[i], 0f) ? position[i] : SplineHandleUtility.RoundBasedOnMinimumDifference(position[i], minDifference[i]);

            return position;
        }
    }
}
