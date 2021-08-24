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
            /// Tangents are manipulated in isolation. Modifying one tangent on a knot does not affect the other.
            /// </summary>
            Broken,
            
            /// <summary>
            /// Tangents are kept in parallel. Modifying one tangent will change the direction of the opposite tangent,
            /// but does not affect the length.
            /// </summary>
            Continuous,
    
            /// <summary>
            /// Tangents are kept parallel and with matching length. Modifying one tangent will update the opposite
            /// tangent to the inverse direction and equivalent length.
            /// </summary>
            Mirrored,
            
            /// <summary>
            /// Tangents are not used. A linear spline is a series of connected points with no curve applied when
            /// interpolating between knots.
            /// </summary>
            Linear
        }

        [SerializeField] EditableTangent m_TangentIn;
        [SerializeField] EditableTangent m_TangentOut;
        Mode m_Mode = Mode.Broken;

        /// <summary>
        /// Defines how tangents behave when manipulated.
        /// </summary>
        public Mode mode => m_Mode;

        /// <summary>
        /// How many editable tangents a knot contains. Cubic bezier splines contain 2 tangents, except at the ends of
        /// a Spline that is not closed, in which case the knot contains a single tangent. Other spline type representations
        /// may contain more or fewer tangents (ex, a Catmull-Rom spline does not expose any editable tangents). 
        /// </summary>
        public override int tangentCount => 2;

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

            spline.SetDirty();
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
        void SetTangents(float3 tangentIn, float3 tangentOut)
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

            m_Mode = mode;
            switch (mode)
            {
                case Mode.Continuous:
                    m_TangentOut.localPosition = SplineUtility.GetContinuousTangent(m_TangentIn.localPosition, m_TangentOut.localPosition);
                    break;

                case Mode.Linear:
                    UpdateLinearTangents();
                    break;
            }

            spline.SetDirty();
        }

        void UpdateLinearTangentsForThisAndAdjacents()
        {
            if (GetPrevious() is BezierEditableKnot previous)
            {
                UpdateLinearTangent(m_TangentIn, previous);
                previous.UpdateLinearTangent(previous.m_TangentOut, this);
            }

            if (GetNext() is BezierEditableKnot next)
            {
                UpdateLinearTangent(m_TangentOut, next);
                next.UpdateLinearTangent(next.m_TangentIn, this);
            }
        }

        void UpdateLinearTangents()
        {
            if (spline.GetPreviousKnot(index, out EditableKnot prevKnot))
                UpdateLinearTangent(m_TangentIn, prevKnot);

            if (spline.GetNextKnot(index, out EditableKnot nextKnot))
                UpdateLinearTangent(m_TangentOut, nextKnot);
        }

        void UpdateLinearTangent(EditableTangent tangent, EditableKnot target)
        {
            if (mode != Mode.Linear)
                return;
            
            tangent.localPosition = this.ToKnotSpaceTangent((target.localPosition - localPosition) / 3.0f);
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

        public static bool IsTangentLinear(float3 knotPos, float3 nextPos, float3 tangentTowardNext)
        {
            var nextDir = math.normalize(nextPos - knotPos);
            var tangentDir = math.normalize(tangentTowardNext);

            if (Mathf.Approximately(math.dot(nextDir, tangentDir), 1f))
                return true;

            return false;
        }

        bool AreTangentsLinear()
        {
            var knotIndex = index;
            var hasPrevKnot = spline.GetPreviousKnot(knotIndex, out var prevKnot);
            var hasNextKnot = spline.GetNextKnot(knotIndex, out var nextKnot);
            
            
            if (hasPrevKnot && hasNextKnot)
                return IsTangentLinear(localPosition, prevKnot.localPosition, this.ToSplineSpaceTangent(m_TangentIn.localPosition)) &&
                       IsTangentLinear(localPosition, nextKnot.localPosition, this.ToSplineSpaceTangent(m_TangentOut.localPosition));

            if (hasPrevKnot)
                return IsTangentLinear(localPosition, prevKnot.localPosition, this.ToSplineSpaceTangent(m_TangentIn.localPosition));

            if (hasNextKnot)
                return IsTangentLinear(localPosition, nextKnot.localPosition, this.ToSplineSpaceTangent(m_TangentOut.localPosition));

            return false;
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

        public override void OnKnotAddedToPathEnd(float3 position, float3 normal)
        {
            float3 tangentOut = tangentOut = math.forward();
            float3 tangentIn = -tangentOut;
            rotation = quaternion.identity;

            if (spline.knotCount > 1)
            {
                if (GetPrevious() is BezierEditableKnot previous)
                {
                    float3 prevPos = previous.position;
                    float3 prevTangentOut = previous.tangentOut.direction;
                    float3 prevTangentIn = previous.tangentIn.direction;

                    //For second point, we need to move the out tangent
                    if (spline.knotCount == 2)
                    {
                        float3 prevNormal = math.cross(prevTangentOut, prevTangentIn);
                        float3 prevTangentDir = SplineUtility.GetLinearTangent(prevPos, position);
                        prevTangentOut = Vector3.ProjectOnPlane(prevTangentDir, prevNormal);

                        previous.rotation *= Quaternion.FromToRotation(previous.tangentOut.direction, prevTangentOut);
                        var scaledTangent = math.normalize(previous.tangentOut.direction) * math.length(prevTangentOut);
                        previous.SetTangents(-scaledTangent, scaledTangent);
                    }
                    else
                    {
                        //Get inverse of in tangent with a modified length based on next position distance
                        float3 direction = -prevTangentIn;
                        prevTangentOut = math.normalize(direction) * (math.distance(prevPos, position) / 3.0f);
                        previous.rotation *= Quaternion.FromToRotation(previous.tangentOut.direction, prevTangentOut);
                        previous.tangentOut.direction = math.normalize(previous.tangentOut.direction) * math.length(prevTangentOut);
                    }

                    //Get reflected tangent
                    float3 prevToCurrent = prevPos - position;
                    float3 cross = math.cross(prevTangentOut, prevToCurrent);
                    float3 reflectionNormal = float3.zero;
                    if (math.length(cross) > 0f)
                        reflectionNormal = math.normalize(math.cross(cross, prevToCurrent));
                    float3 reflectedDir = Vector3.Reflect(-prevTangentOut, reflectionNormal);

                    var worldTangentOut = -Vector3.ProjectOnPlane(reflectedDir, normal);
                    rotation = quaternion.LookRotation(worldTangentOut, normal);

                    tangentOut = worldTangentOut;
                    tangentIn = -tangentOut;
                }
            }

            SetTangents(tangentIn, tangentOut);
            UpdateLinearTangentsForThisAndAdjacents();
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

            var up = math.rotate(math.nlerp(previous.localRotation, next.localRotation, t), math.up());
            localRotation = quaternion.LookRotation(rightCurve.Tangent0, up);

            SetLocalTangents(this.ToKnotSpaceTangent(leftCurve.Tangent1), this.ToKnotSpaceTangent(rightCurve.Tangent0));
            UpdateLinearTangentsForThisAndAdjacents();
        }
    }

    [Serializable]
    sealed class BezierEditableSpline : EditableSpline<BezierEditableKnot>
    {
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
