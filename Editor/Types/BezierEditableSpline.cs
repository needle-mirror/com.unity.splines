using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// Defines whether a selected tangent is preceding or following associated knot. A knot can be used to form two
    /// curves, serving either as the first or last point on a curve. When used as the first point, the 'Out' tangent
    /// is used to calculate <see cref="BezierCurve.P1"/>. When used as the last point, the 'In' tangent is used to
    /// calculate <see cref="BezierCurve.P2"/>.
    /// </summary>
    enum BezierTangent
    {
        In,
        Out,
    }

    /// <summary>
    /// Representation of a <see cref="BezierKnot"/> that may be selected and manipulated in the Editor.
    /// </summary>
    [Serializable]
    sealed class BezierEditableKnot : EditableKnot
    {
        //Enable scope that breaks tangents dependency when needed
        //and rebuild it after dispose
        internal struct TangentSafeEditScope : IDisposable
        {
            EditableKnot m_Knot;

            public TangentSafeEditScope(EditableKnot knot)
            {
                m_Knot = knot;
                if(m_Knot is BezierEditableKnot bezierKnot)
                    bezierKnot.m_Mode = Mode.Broken;
            }

            public void Dispose()
            {
                if(m_Knot is BezierEditableKnot bezierKnot)
                    bezierKnot.SetMode(bezierKnot.CalculateMode());
            }
        }

        /// <summary>
        /// Describes the different ways a tool may interact with a tangent handle.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Tangents are not used. A linear spline is a series of connected points with no curve applied when
            /// interpolating between knots.
            /// </summary>
            Linear,

            /// <summary>
            /// Tangents are kept parallel and with matching length. Modifying one tangent will update the opposite
            /// tangent to the inverse direction and equivalent length.
            /// </summary>
            Mirrored,

            /// <summary>
            /// Tangents are kept in parallel. Modifying one tangent will change the direction of the opposite tangent,
            /// but does not affect the length.
            /// </summary>
            Continuous,
    
            /// <summary>
            /// Tangents are manipulated in isolation. Modifying one tangent on a knot does not affect the other.
            /// </summary>
            Broken,
        }

        [SerializeField] EditableTangent m_TangentIn;
        [SerializeField] EditableTangent m_TangentOut;
        Mode m_Mode = Mode.Broken;

        /// <summary>
        /// Defines how tangents behave when manipulated.
        /// </summary>
        public Mode mode => m_Mode;

        /// <summary>
        /// The tangent preceding this knot.
        /// </summary>
        public EditableTangent tangentIn => m_TangentIn;
        
        /// <summary>
        /// The tangent following this knot.
        /// </summary>
        public EditableTangent tangentOut => m_TangentOut;

        /// <summary>
        /// Create a new BezierEditableKnot.
        /// </summary>
        public BezierEditableKnot()
        {
            m_TangentIn = new EditableTangent(this, (int)BezierTangent.In);
            m_TangentOut = new EditableTangent(this, (int)BezierTangent.Out);
            m_TangentIn.directionChanged += () => TangentChanged(m_TangentIn, m_TangentOut);
            m_TangentOut.directionChanged += () => TangentChanged(m_TangentOut, m_TangentIn);
        }

        internal void TangentChanged(EditableTangent changed, Mode desiredMode)
        {
            if(TryGetOppositeTangent(changed, out EditableTangent opposite))
            {
                m_Mode = desiredMode;
                TangentChanged(changed, opposite);
            }
        }

        void TangentChanged(EditableTangent changed, EditableTangent opposite)
        {
            if (float.IsNaN(changed.localPosition.x) || float.IsNaN(changed.localPosition.y) || float.IsNaN(changed.localPosition.z))
                changed.localPosition = new float3(0, 0, 0);
            
            switch (mode)
            {
                case Mode.Continuous:
                    opposite.SetLocalPositionNoNotify(SplineUtility.GetContinuousTangent(changed.localPosition, opposite.localPosition));
                    break;

                case Mode.Mirrored:
                    opposite.SetLocalPositionNoNotify(-changed.localPosition);
                    break;
 
                case Mode.Linear:
                    ValidateMode();
                    break;
            }

            SetDirty();
        }

        internal override EditableTangent GetTangent(int index)
        {
            switch ((BezierTangent)index)
            {
                case BezierTangent.In: return m_TangentIn;
                case BezierTangent.Out: return m_TangentOut;
                default: return null;
            }
        }

        internal bool TryGetOppositeTangent(EditableTangent tangent, out EditableTangent oppositeTangent)
        {
            if(tangent == tangentIn)
            {
                oppositeTangent = tangentOut;
                return true;
            }
            if(tangent == tangentOut)
            {
                oppositeTangent = tangentIn;
                return true;
            }

            oppositeTangent = null;
            return false;
        }

        /// <summary>
        /// Sets knot's local tangents.
        /// </summary>
        /// <param name="localTangentIn">Knot's in tangent in local (knot) space.</param>
        /// <param name="localTangentOut">Knot's out tangent in local (knot) space.</param>
        public void SetLocalTangents(float3 localTangentIn, float3 localTangentOut)
        {
            using(new TangentSafeEditScope(this))
            {
                m_TangentIn.localPosition = localTangentIn;
                m_TangentOut.localPosition = localTangentOut;
            }
        }

        /// <summary>
        /// Given world space in and out tangents, transforms to knot space and sets as knot's tangents
        /// </summary>
        /// <param name="tangentIn"> World space in tangent.</param>
        /// <param name="tangentOut"> World space out tangent.</param>
        public void SetTangents(float3 tangentIn, float3 tangentOut)
        {
            var splineSpaceTangentIn = spline.worldToLocalMatrix.MultiplyVector(tangentIn);
            var splineSpaceTangentOut = spline.worldToLocalMatrix.MultiplyVector(tangentOut);
            SetLocalTangents(this.ToKnotSpaceTangent(splineSpaceTangentIn), 
                             this.ToKnotSpaceTangent(splineSpaceTangentOut));
        }

        public void ValidateMode()
        {
            switch (mode)
            {
                case Mode.Continuous:
                    if (!AreTangentsContinuous(m_TangentIn.localPosition, m_TangentOut.localPosition))
                    {
                        SetMode(Mode.Broken);
                    }
                    break;

                case Mode.Linear:
                    if (!AreTangentsLinear())
                    {
                        SetMode(Mode.Broken);
                    }
                    break;
            }
        }

        public void SetMode(Mode mode)
        {
            if (this.mode == mode)
                return;

            var previousMode = m_Mode;
            m_Mode = mode;
            ForceUpdateTangentsFromMode(previousMode);
            SetDirty();
        }

        public void ForceUpdateTangentsFromMode(Mode previousMode)
        {
            switch (mode)
            {
                case Mode.Mirrored:
                case Mode.Continuous:
                case Mode.Broken:
                    SyncKnotAndTangents(previousMode);
                    break;
                
                case Mode.Linear:
                    m_TangentIn.SetLocalPositionNoNotify(float3.zero);
                    m_TangentOut.SetLocalPositionNoNotify(float3.zero);
                    break;
                
                default:
                    Debug.LogError($"{mode} Knot mode is not supported!");
                    break;
            }
        }

        void SyncKnotAndTangents(Mode previousMode)
        {
            if (mode == Mode.Broken)
            {
                // When switching from Linear to Broken, tangents should just be set to 1/3 curve length "linear" tangents. Otherwise, switching to Broken requires no sync.
                if (previousMode == Mode.Linear)
                {
                    var prevKnot = GetPrevious();
                    var toPrevious = prevKnot != null ? prevKnot.position - position : float3.zero;
                    m_TangentIn.SetLocalPositionNoNotify(worldToLocalMatrix.MultiplyVector(toPrevious / 3f));

                    var nextKnot = GetNext();
                    var toNext = nextKnot != null ? nextKnot.position - position : float3.zero;
                    m_TangentOut.SetLocalPositionNoNotify(worldToLocalMatrix.MultiplyVector(toNext / 3f));
                }
                return;
            }
            
            var newDirection = float3.zero;
            if (mode == Mode.Continuous || mode == Mode.Mirrored)
            {
                // Smooth the tangent when switching from linear mode to continous or mirrored
                if (previousMode == Mode.Linear)
                    newDirection = math.mul(math.inverse(localRotation), CatmullRomEditableSpline.GetTangentOut(this, this.GetPrevious(), this.GetNext())) / 3f;
                else
                {
                    if (mode == Mode.Continuous)
                        newDirection = SplineUtility.GetContinuousTangent(m_TangentIn.localPosition, m_TangentOut.localPosition);
                    else if (mode == Mode.Mirrored)
                        newDirection = -m_TangentIn.localPosition;
                }
            } 
            
            var tangentOutNorm = math.normalize(m_TangentOut.localPosition);
            // Sync tangents to knot if we're switching out of broken mode and our tangents
            // are not opposite to each other and/or are not aligned with knot's forward
            if ((previousMode == Mode.Broken || previousMode == Mode.Linear && math.lengthsq(newDirection) > 0f) &&
                (!Mathf.Approximately(math.dot(math.normalize(m_TangentIn.localPosition), tangentOutNorm), -1f) ||
                 math.abs(math.dot(math.forward(), tangentOutNorm.z)) < 1f))
            {
                var length =  math.length(previousMode == Mode.Linear ? newDirection : m_TangentIn.localPosition);
                m_TangentIn.SetLocalPositionNoNotify(-math.forward() * length);
                
                length = math.length(previousMode == Mode.Linear ? newDirection : m_TangentOut.localPosition);
                m_TangentOut.SetLocalPositionNoNotify(math.forward() * length);
    
                var newDirectionW = localToWorldMatrix.MultiplyVector(newDirection);
                var newDirectionNorm = math.normalize(newDirectionW);
                var newUp = math.rotate(Quaternion.FromToRotation(math.forward(), newDirectionNorm), math.up());
                var newRotation = Quaternion.LookRotation(newDirectionNorm, newUp);
                
                // If the new rotation is just a roll, favor old rotation
                if (Mathf.Approximately(Vector3.Dot((Quaternion) rotation * Vector3.forward, newRotation * Vector3.forward), 1f))
                    return;
                
                rotation = Quaternion.LookRotation(newDirectionNorm, newUp);
            }
            else
                m_TangentOut.SetLocalPositionNoNotify(newDirection);
        }

        public static bool AreTangentsContinuous(float3 tangentIn, float3 tangentOut)
        {
            var tangentInDir = math.normalize(tangentIn);
            var tangentOutDir = math.normalize(tangentOut);

            if (Mathf.Approximately(math.dot(tangentInDir, tangentOutDir), -1f))
                return true;

            return false;
        }

        public static bool AreTangentsMirrored(float3 tangentIn, float3 tangentOut)
        {
            const float kEpsilon = 0.001f;
            
            var tangentInDir = math.normalize(tangentIn);
            var tangentOutDir = math.normalize(tangentOut);

            var areOpposite = Mathf.Approximately(math.dot(tangentInDir, tangentOutDir), -1f);
            var areEqualLength = Math.Abs(math.length(tangentIn) - math.length(tangentOut)) < kEpsilon;
            
            if (areOpposite && areEqualLength)
                return true;

            return false;
        }

        bool AreTangentsLinear()
        {
            return m_TangentIn.localPosition.Equals(float3.zero) && m_TangentOut.localPosition.Equals(float3.zero);
        }

        public override void OnPathUpdatedFromTarget()
        {
            SetMode(CalculateMode());
        }

        internal Mode CalculateMode()
        {
            if (AreTangentsLinear())
                return Mode.Linear;

            if (AreTangentsMirrored(m_TangentIn.localPosition, m_TangentOut.localPosition))
                return Mode.Mirrored;

            if (AreTangentsContinuous(m_TangentIn.localPosition, m_TangentOut.localPosition))
                return Mode.Continuous;

            return Mode.Broken;
        }

        public override void OnKnotInsertedOnCurve(EditableKnot previous, EditableKnot next, float t)
        {
            if (!(previous is BezierEditableKnot prevKnot && next is BezierEditableKnot nextKnot))
                return;

            var curveToSplit = BezierCurve.FromTangent(prevKnot.localPosition, prevKnot.ToSplineSpaceTangent(prevKnot.tangentOut.localPosition), 
                nextKnot.localPosition, nextKnot.ToSplineSpaceTangent(nextKnot.tangentIn.localPosition));

            CurveUtility.Split(curveToSplit, t, out var leftCurve, out var rightCurve);
            
            var tangentsMirrored = AreTangentsMirrored(prevKnot.ToSplineSpaceTangent(prevKnot.tangentIn.localPosition), leftCurve.Tangent0);
            if (prevKnot.mode == Mode.Mirrored && !tangentsMirrored)
                prevKnot.SetMode(Mode.Continuous);

            prevKnot.tangentOut.localPosition = prevKnot.ToKnotSpaceTangent(leftCurve.Tangent0);

            tangentsMirrored = AreTangentsMirrored(nextKnot.ToSplineSpaceTangent(nextKnot.tangentOut.localPosition), rightCurve.Tangent1);
            if (nextKnot.mode == Mode.Mirrored && !tangentsMirrored)
                nextKnot.SetMode(Mode.Continuous);

            nextKnot.tangentIn.localPosition = nextKnot.ToKnotSpaceTangent(rightCurve.Tangent1);

            var up = CurveUtility.EvaluateUpVector(curveToSplit, t, math.rotate(previous.localRotation, math.up()), math.rotate(next.localRotation, math.up()));
            localRotation = quaternion.LookRotationSafe(math.normalize(rightCurve.Tangent0), up);

            SetLocalTangents(this.ToKnotSpaceTangent(leftCurve.Tangent1), this.ToKnotSpaceTangent(rightCurve.Tangent0));
        }
    }

    [Serializable]
    sealed class BezierEditableSpline : EditableSpline<BezierEditableKnot>
    {
        public override int tangentsPerKnot => 2;

        public override void OnKnotAddedAtEnd(EditableKnot knot, float3 normal, float3 tangentOut)
        {
            if (knot is BezierEditableKnot bezierKnot)
            {
                var previousKnot = knot.GetPrevious();
                
                GetRotationsForNewCurve(knot.position, normal, tangentOut, knotCount, previousKnot as BezierEditableKnot, out var endKnotRotation, out var prevKnotRotation);
                knot.rotation = endKnotRotation;

                if (knotCount > 1) 
                    previousKnot.rotation = prevKnotRotation;

                bezierKnot.SetTangents( -tangentOut, tangentOut);
            }
        }
        
        public override CurveData GetPreviewCurveForEndKnot(float3 point, float3 normal, float3 tangentOut)
        {
            CreatePreviewKnotsIfNeeded();

            if (knotCount > 0)
            {
                var lastKnot = GetKnot(knotCount - 1);
                m_PreviewKnotA.Copy(lastKnot);
            }

            m_PreviewKnotB.Copy(m_PreviewKnotA);
            m_PreviewKnotB.position = point;
            
            GetRotationsForNewCurve(point, normal, tangentOut, knotCount + 1, m_PreviewKnotA as BezierEditableKnot, out var endKnotRotation, out var _);
            m_PreviewKnotB.rotation = endKnotRotation;

            for (int i = 0; i < m_PreviewKnotB.tangentCount; ++i)
            {
                var previewTangent = m_PreviewKnotB.GetTangent(i);
                previewTangent.direction = i == 0 ? -tangentOut : tangentOut;
            }
            
            return new CurveData(m_PreviewKnotA, m_PreviewKnotB);
        }
        
        void GetRotationsForNewCurve(float3 point, float3 normal, float3 tangentOut, int newKnotCount, BezierEditableKnot previousKnot, out quaternion endKnotRotation, out quaternion prevKnotRotation)
        {
            var tangentOutLen = math.length(tangentOut);
            prevKnotRotation = quaternion.identity;

            if (newKnotCount == 1 && tangentOutLen == 0f)
                endKnotRotation = Quaternion.FromToRotation(math.up(), normal);
            else
            {
                if (previousKnot != null)
                {
                    prevKnotRotation = previousKnot.rotation;
                    var toPrevious = previousKnot.position - point;
                    if (tangentOutLen == 0f)
                    {
                        
                        var toPreviousProj = Vector3.ProjectOnPlane(math.normalize(toPrevious), normal);
                        if (toPreviousProj.magnitude > 0f)
                            endKnotRotation = quaternion.LookRotationSafe(-math.normalize(toPreviousProj), normal);
                        else
                            endKnotRotation = Quaternion.FromToRotation(math.up(), normal);
                    }
                    else
                        endKnotRotation = quaternion.LookRotationSafe(math.normalize(tangentOut), normal);

                    // When placing 2nd knot and if the first knot was placed without specifying a custom world tangent out,
                    // adjust 1st knot's rotation and point it in the direction of the 2nd knot
                    if (newKnotCount == 2)
                    {
                        var previousUp = math.rotate(previousKnot.rotation, math.up());
                        var toEndKnotProj = Vector3.ProjectOnPlane(math.normalize(toPrevious), previousUp);
                        var previousTangentOutLen = math.length(previousKnot.tangentOut.localPosition);

                        if (previousTangentOutLen == 0 && toEndKnotProj.magnitude > 0f)
                            prevKnotRotation = quaternion.LookRotationSafe(math.normalize(toEndKnotProj), previousUp);
                    }
                }
                else
                {
                    if (tangentOutLen > 0f)
                        endKnotRotation = quaternion.LookRotationSafe(math.normalize(tangentOut), normal);
                    else
                        endKnotRotation = quaternion.LookRotationSafe(Quaternion.FromToRotation(math.up(), normal) * math.forward(), normal);
                }
            }
        }

        public override float3 GetPointOnCurve(CurveData curve, float t)
        {
            var a = (BezierEditableKnot) curve.a;
            var b = (BezierEditableKnot) curve.b;

            return CurveUtility.EvaluatePosition(BezierCurve.FromTangent(a.position, a.tangentOut.direction, b.position, b.tangentIn.direction), t);
        }

        public override void GetLocalTangents(EditableKnot knot, out float3 localTangentIn, out float3 localTangentOut)
        {
            var bezierEditableKnot = knot as BezierEditableKnot;
            localTangentIn = knot.ToSplineSpaceTangent(bezierEditableKnot.tangentIn.localPosition);
            localTangentOut = knot.ToSplineSpaceTangent(bezierEditableKnot.tangentOut.localPosition);
        }

        public override void ToBezier(List<BezierKnot> results)
        {
            for (int i = 0; i < knotCount; ++i)
            {
                var knot = GetKnot(i);
                results.Add(new BezierKnot(
                    knot.localPosition,
                    knot.tangentIn.localPosition,
                    knot.tangentOut.localPosition,
                    knot.localRotation));
            }
        }

        public override void FromBezier(IReadOnlyList<BezierKnot> knots)
        {
            Resize(knots.Count);
            for (int i = 0; i < knots.Count; ++i)
            {
                var editKnot = GetKnot(i);
                var knot = knots[i];
                editKnot.localPosition = knot.Position;
                editKnot.localRotation = knot.Rotation;
                editKnot.SetLocalTangents(knot.TangentIn, knot.TangentOut);
            }
        }
    }
}
