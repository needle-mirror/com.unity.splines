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

            internal static TransformData GetData(ISplineElement element)
            {
                var tData = new TransformData();
                tData.position = new float3(element.position);
                if (element is BezierEditableKnot knot)
                {
                    tData.inTangentDirection = knot.tangentIn.direction;
                    tData.outTangentDirection = knot.tangentOut.direction;
                }

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

        static List<ISplineElement> s_ElementSelection = new List<ISplineElement>(32);
        
        public static IReadOnlyList<ISplineElement> elementSelection => s_ElementSelection;

        static int s_ElementSelectionCount = 0;

        public static bool canManipulate => s_ElementSelectionCount > 0;

        public static ISplineElement currentElementSelected
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
        static HashSet<EditableKnot> s_RotatedKnotCache = new HashSet<EditableKnot>();
        static RotationSyncData s_RotationSyncData = new RotationSyncData();

        internal static void UpdateSelection(IEnumerable<Object> selection)
        {
            SplineSelection.GetSelectedElements(selection, s_ElementSelection);
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
                    s_PivotPosition = EditableSplineUtility.GetBounds(s_ElementSelection, useKnotPositionForTangents).center;
                    break;

                case PivotMode.Pivot:
                    if (s_ElementSelectionCount == 0)
                        goto default;

                    var element = s_ElementSelection[0];
                    if (useKnotPositionForTangents && element is EditableTangent tangent)
                        s_PivotPosition = tangent.owner.position;
                    else
                        s_PivotPosition = element.position;
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
                    handleRotation = CalculateElementSpaceHandleRotation(curElement);
                else if (curElement is EditableTangent editableTangent)
                    handleRotation = CalculateElementSpaceHandleRotation(editableTangent.owner);
            }

            s_HandleRotation = handleRotation;
            s_HandleRotationInv = math.inverse(s_HandleRotation);
        }

        public static void ApplyTranslation(Vector3 delta)
        {
            s_RotatedKnotCache.Clear();

            foreach (var element in s_ElementSelection)
            {
                if (element is EditableKnot knot) 
                {
                    knot.position += (float3)delta;
                    if (!s_RotationSyncData.initialized)
                        s_RotationSyncData.Initialize(quaternion.identity, 0f, 1f);
                }
                else if (element is EditableTangent tangent)
                {
                    //Do nothing on the tangent if the knot is also in the selection
                    if (s_ElementSelection.Contains(tangent.owner))
                        continue;

                    if (tangent.owner is BezierEditableKnot owner)
                    {
                        if (OppositeTangentSelected(tangent))
                            owner.SetMode(BezierEditableKnot.Mode.Broken);

                        if (owner.mode == BezierEditableKnot.Mode.Broken)
                            tangent.position = tangent.owner.position + tangent.direction + (float3) delta;
                        else
                        {
                            if (s_RotatedKnotCache.Contains(tangent.owner))
                                continue;

                            if (tangent.owner is BezierEditableKnot tangentOwner)
                            {
                                var targetDirection = tangent.direction + (float3) delta;

                                // Build rotation sync data based on active selection's transformation
                                if (!s_RotationSyncData.initialized)
                                {
                                    var rotationDelta = Quaternion.FromToRotation(tangent.direction, targetDirection);
                                    var magnitudeDelta = math.length(targetDirection) - math.length(tangent.direction);

                                    s_RotationSyncData.Initialize(rotationDelta, magnitudeDelta, 1f);
                                }

                                ApplyTangentRotationSyncTransform(tangent);
                            }
                        }
                    }
                }
            }

            s_RotationSyncData.Clear();
        }
        
        public static void ApplyRotation(Quaternion deltaRotation, Vector3 rotationCenter)
        {
            s_RotatedKnotCache.Clear();
            
            foreach (var element in s_ElementSelection)
            {
                if (element is EditableKnot knot)
                {
                    var knotRotation = knot.rotation;
                    RotateKnot(knot, deltaRotation, rotationCenter);
                    if (!s_RotationSyncData.initialized)
                        s_RotationSyncData.Initialize(math.mul(math.inverse(knotRotation), knot.rotation), 0f, 1f);                    
                }
                else if (element is EditableTangent tangent && !s_ElementSelection.Contains(tangent.owner))
                {
                    if (tangent.owner is BezierEditableKnot tangentOwner)
                    {
                        if (tangentOwner.mode == BezierEditableKnot.Mode.Broken)
                        {
                            if (Tools.pivotMode == PivotMode.Pivot)
                                rotationCenter = tangent.owner.position;
            
                            var mode = tangentOwner.mode;

                            var deltaPos = math.rotate(deltaRotation, tangent.position - (float3)rotationCenter);
                            tangent.position = deltaPos + (float3)rotationCenter;

                            tangentOwner.TangentChanged(tangent, mode);
                        }
                        else
                        {
                            if (s_RotatedKnotCache.Contains(tangent.owner))
                                continue;
                            
                            deltaRotation.ToAngleAxis(out var deltaRotationAngle, out var deltaRotationAxis);

                            if (math.abs(deltaRotationAngle) > 0f)
                            {
                                if (tangentOwner.mode != BezierEditableKnot.Mode.Broken)
                                {
                                    // If we're in center pivotMode and both tangents of the same knot are in selection, enter Broken mode under these conditions:
                                    if (Tools.pivotMode == PivotMode.Center && OppositeTangentSelected(tangent))
                                    {
                                        var knotToCenter = (float3) rotationCenter - tangentOwner.position;
                                        // 1) Rotation center does not match owner knot's position
                                        if (!Mathf.Approximately(math.length(knotToCenter), 0f))
                                        {
                                            var similarity = Math.Abs(Vector3.Dot(math.normalize(deltaRotationAxis), math.normalize(knotToCenter)));
                                            // 2) Both rotation center and knot, are not on rotation delta's axis
                                            if (!Mathf.Approximately(similarity, 1f))
                                                tangentOwner.SetMode(BezierEditableKnot.Mode.Broken);
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
                                        var deltaPos = math.rotate(deltaRotation, tangent.position - (float3) rotationCenter);
                                        var knotToRotationCenter = (float3) rotationCenter - tangent.owner.position;
                                        var targetDirection = knotToRotationCenter + deltaPos;
                                        var tangentNorm = math.normalize(tangent.direction);
                                        var axisDotTangent = math.dot(math.normalize(deltaRotationAxis), tangentNorm);
                                        var toRotCenterDotTangent = math.length(knotToRotationCenter) > 0f ? math.dot(math.normalize(knotToRotationCenter), tangentNorm) : 1f;
                                        quaternion knotRotationDelta;
                                        // In center pivotMode, use handle delta only if our handle delta rotation's axis
                                        // matches knot's active selection tangent direction and rotation center is on the tangent's axis.
                                        // This makes knot roll possible when element selection list only contains one or both tangents of a single knot.
                                        if (Mathf.Approximately(math.abs(axisDotTangent), 1f) && Mathf.Approximately(math.abs(toRotCenterDotTangent), 1f))
                                            knotRotationDelta = deltaRotation;
                                        else
                                            knotRotationDelta = Quaternion.FromToRotation(tangent.direction, targetDirection);

                                        var scaleMultiplier = math.length(targetDirection) / math.length(tangent.direction);

                                        s_RotationSyncData.Initialize(knotRotationDelta, 0f, scaleMultiplier);
                                    }
                                }

                                ApplyTangentRotationSyncTransform(tangent, false);
                            }
                        }
                    }
                }
            }
            
            s_RotationSyncData.Clear();
        }

        static bool OppositeTangentSelected(EditableTangent tangent)
        {
            if (tangent.owner is BezierEditableKnot tangentOwner && tangentOwner.mode != BezierEditableKnot.Mode.Broken)
                if (tangentOwner.TryGetOppositeTangent(tangent, out var oppositeTangent) && s_ElementSelection.Contains(oppositeTangent))
                    return true;

            return false;
        }

        static void RotateKnot(EditableKnot knot, quaternion deltaRotation, float3 rotationCenter, bool allowTranslation = true)
        {
            var knotInBrokenMode = (knot is BezierEditableKnot bezierKnot && bezierKnot.mode == BezierEditableKnot.Mode.Broken);
            if (!knotInBrokenMode && s_RotatedKnotCache.Contains(knot))
                return;

            if (allowTranslation && Tools.pivotMode == PivotMode.Center)
            {
                var dir = knot.position - rotationCenter;

                if (SplineTool.handleOrientation == HandleOrientation.Element || SplineTool.handleOrientation == HandleOrientation.Parent)
                    knot.position = math.rotate(deltaRotation, dir) + rotationCenter;
                else
                    knot.position = math.rotate(s_HandleRotation, math.rotate(deltaRotation, math.rotate(s_HandleRotationInv, dir))) + rotationCenter;
            }
            
            if (SplineTool.handleOrientation == HandleOrientation.Element || SplineTool.handleOrientation == HandleOrientation.Parent)
            {
                if (Tools.pivotMode == PivotMode.Center)
                    knot.rotation = math.mul(deltaRotation, knot.rotation);
                else
                {
                    var handlePivotModeRot = math.mul(GetCurrentSelectionKnot().rotation, math.inverse(knot.rotation));
                    knot.rotation = math.mul(math.inverse(handlePivotModeRot), math.mul(deltaRotation, math.mul(handlePivotModeRot, knot.rotation)));
                }
            }
            else
                knot.rotation = math.mul(s_HandleRotation, math.mul(deltaRotation, math.mul(s_HandleRotationInv, knot.rotation)));

            s_RotatedKnotCache.Add(knot);
        }

        public static void ApplyScale(float3 scale)
        {
            s_RotatedKnotCache.Clear();
            ISplineElement[] scaledElements = new ISplineElement[s_ElementSelectionCount];
            
            for(int elementIndex = 0; elementIndex<s_ElementSelectionCount; elementIndex++)
            {
                var element = s_ElementSelection[elementIndex];
                if (element is EditableKnot knot)
                {
                    ScaleKnot(knot, elementIndex, scale);
                    
                    if (!s_RotationSyncData.initialized)
                        s_RotationSyncData.Initialize(quaternion.identity, 0f, 1f);
                } 
                else if(element is EditableTangent tangent && !s_ElementSelection.Contains(tangent.owner))
                {
                    if(tangent.owner is BezierEditableKnot tangentOwner)
                    {
                        var restoreMode = false;
                        var mode = tangentOwner.mode;
                        var scaleDelta = scale - new float3(1f, 1f, 1f);
                        if (tangentOwner.mode != BezierEditableKnot.Mode.Broken && math.length(scaleDelta) > 0f)
                        {
                            // If we're in center pivotMode and both tangents of the same knot are in selection
                            if (Tools.pivotMode == PivotMode.Center && OppositeTangentSelected(tangent))
                            {
                                var knotToCenter = (float3)pivotPosition - tangentOwner.position;
                                //  Enter broken mode if scale operation center does not match owner knot's position
                                if (!Mathf.Approximately(math.length(knotToCenter), 0f))
                                {
                                    tangentOwner.SetMode(BezierEditableKnot.Mode.Broken);
                                    var similarity = Math.Abs(Vector3.Dot(math.normalize(scaleDelta), math.normalize(knotToCenter)));
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
                            if (tangentOwner.mode == BezierEditableKnot.Mode.Broken)
                                tangent.position = ScaleTangent(tangent, s_MouseDownData[elementIndex].position, scale);
                            else
                            {
                                // Build rotation sync data based on active selection's transformation
                                if (!s_RotationSyncData.initialized)
                                {
                                    var targetDirection = ScaleTangent(tangent, s_MouseDownData[elementIndex].position, scale) - tangent.owner.position;
                                    var rotationDelta = Quaternion.FromToRotation(tangent.direction, targetDirection);
                                    var scaleMultiplier = math.length(targetDirection) / math.length(tangent.direction);

                                    s_RotationSyncData.Initialize(rotationDelta, 0f, scaleMultiplier);
                                }

                                if (tangentOwner.mode == BezierEditableKnot.Mode.Mirrored && s_RotatedKnotCache.Contains(tangentOwner))
                                    continue;

                                ApplyTangentRotationSyncTransform(tangent, false);
                            }

                            if (restoreMode)
                                tangentOwner.SetMode(mode);
                        }
                    }
                }
                scaledElements[elementIndex] = element;
            }

            s_RotationSyncData.Clear();
        }

        static void ScaleKnot(EditableKnot knot, int dataIndex, float3 scale)
        {
            if(Tools.pivotMode == PivotMode.Center)
            {
                var deltaPos = math.rotate(s_HandleRotationInv ,s_MouseDownData[dataIndex].position - (float3) pivotPosition);
                var deltaPosKnot =  deltaPos * scale;
                knot.position = math.rotate(s_HandleRotation, deltaPosKnot) + (float3)pivotPosition;
            }

            using(new BezierEditableKnot.TangentSafeEditScope(knot))
            {
                if(knot is BezierEditableKnot bezierKnot)
                {
                    var tangent = bezierKnot.tangentIn;
                    tangent.direction = math.rotate(s_HandleRotation, math.rotate(s_HandleRotationInv,s_MouseDownData[dataIndex].inTangentDirection) * scale);
                    tangent = bezierKnot.tangentOut;
                    tangent.direction = math.rotate(s_HandleRotation, math.rotate(s_HandleRotationInv,s_MouseDownData[dataIndex].outTangentDirection) * scale);
                }
            }
        }

        static float3 ScaleTangent(EditableTangent tangent, float3 originalPosition, float3 scale)
        {
            var scaleCenter = Tools.pivotMode == PivotMode.Center ? (float3) pivotPosition : tangent.owner.position;
            
            var deltaPos = math.rotate(s_HandleRotationInv, originalPosition - scaleCenter) * scale;
            return math.rotate(s_HandleRotation, deltaPos) + scaleCenter;
        }

        static void ApplyTangentRotationSyncTransform(EditableTangent tangent, bool absoluteScale = true)
        {
            if (tangent.owner is BezierEditableKnot tangentOwner)
            {
                // Apply scale only if tangent is active selection or it's part of multi select and its knot is mirrored
                if (tangent == currentElementSelected || 
                    tangentOwner.mode == BezierEditableKnot.Mode.Mirrored || 
                    (!absoluteScale && tangentOwner.mode == BezierEditableKnot.Mode.Continuous))
                {
                    if (absoluteScale)
                        tangent.direction += math.normalize(tangent.direction) * s_RotationSyncData.magnitudeDelta;
                    else 
                        tangent.direction *= s_RotationSyncData.scaleMultiplier;
                }
            }

            RotateKnot(tangent.owner, s_RotationSyncData.rotationDelta, tangent.owner.position, false);
        }

        internal static quaternion CalculateElementSpaceHandleRotation(ISplineElement element)
        {
            quaternion handleRotation = quaternion.identity;
            if (element is EditableTangent editableTangent && editableTangent.owner is BezierEditableKnot tangentKnot)
            {
                float3 forward;
                var knotUp = math.rotate(tangentKnot.rotation, math.up());
                    
                if (math.length(editableTangent.direction) > 0)
                    forward = math.normalize(editableTangent.direction);
                else // Treat zero length tangent same way as when it's parallel to knot's up vector
                    forward = knotUp;

                float3 right;
                var dotForwardKnotUp = math.dot(forward, knotUp);
                if (Mathf.Approximately(math.abs(dotForwardKnotUp), 1f))
                    right = math.rotate(tangentKnot.rotation, math.right()) * math.sign(dotForwardKnotUp);
                else
                    right = math.cross(forward, knotUp);

                handleRotation = quaternion.LookRotationSafe(forward, math.cross(right, forward));
            }
            else if (element is EditableKnot editableKnot)
                handleRotation = editableKnot.rotation;

            return handleRotation;
        }

        static EditableKnot GetCurrentSelectionKnot()
        {
            if (currentElementSelected == null)
                return null;

            if (currentElementSelected is EditableTangent tangent)
                return tangent.owner;

            if (currentElementSelected is EditableKnot knot)
                return knot;

            return null;
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
            return EditableSplineUtility.GetBounds(s_ElementSelection, useKnotPositionForTangents);
        }
    }
}
