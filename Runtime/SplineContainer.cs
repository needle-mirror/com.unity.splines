using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A Component that holds a <see cref="Spline"/> object.
    /// </summary>
#if UNITY_2021_2_OR_NEWER
    [Icon(k_IconPath)]
#endif
    [AddComponentMenu("Splines/Spline")]
    [ExecuteInEditMode]
    public sealed class SplineContainer : MonoBehaviour, ISplineContainer
    {
        const string k_IconPath = "Packages/com.unity.splines/Editor/Resources/Icons/SplineComponent.png";

        //Keeping a main spline to be backwards compatible with older versions of the spline package
        [SerializeField, Obsolete, HideInInspector]
        Spline m_Spline;

        [SerializeField]
        Spline[] m_Splines = { new Spline() };

        [SerializeField]
        KnotLinkCollection m_Knots = new KnotLinkCollection();

        public IReadOnlyList<Spline> Splines
        {
            get => new ReadOnlyCollection<Spline>(m_Splines);
            set
            {
                if (value == null)
                {
                    m_Splines = new Spline[0];
                    return;
                }

                m_Splines = new Spline[value.Count];
                for (int i = 0; i < m_Splines.Length; ++i)
                    m_Splines[i] = value[i];
            }
        }

        public KnotLinkCollection KnotLinkCollection => m_Knots;

        void Awake()
        {
            //Ensure that old data that was never reserialized is still valid at creation
            UpgradeOldVersionDataIfNeeded();
        }

        void OnEnable()
        {
            Spline.Changed += OnSplineChanged;
        }

        void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
        }

        void OnSplineChanged(Spline spline, int index, SplineModification modificationType)
        {
            var splineIndex = Array.IndexOf(m_Splines, spline);
            if (splineIndex < 0)
                return;

            switch (modificationType)
            {
                case SplineModification.KnotModified:
                    this.SetLinkedKnotPosition(new SplineKnotIndex(splineIndex, index));
                    break;

                case SplineModification.KnotInserted:
                    m_Knots.KnotInserted(splineIndex, index);
                    break;

                case SplineModification.KnotRemoved:
                    m_Knots.KnotRemoved(splineIndex, index);
                    break;
            }
        }

        void OnKnotModified(Spline spline, int index)
        {
            var splineIndex = Array.IndexOf(m_Splines, spline);
            if (splineIndex >= 0)
                this.SetLinkedKnotPosition(new SplineKnotIndex(splineIndex, index));
        }

        bool IsScaled => transform.lossyScale != Vector3.one;

        /// <summary>
        /// The instantiated <see cref="Spline"/> object attached to this component.
        /// </summary>
        public Spline Spline
        {
            get => m_Splines.Length > 0 ? m_Splines[0] : null;
            set
            {
                if (m_Splines.Length > 0)
                    m_Splines[0] = value;
            }
        }

        void OnValidate()
        {
            UpgradeOldVersionDataIfNeeded();
        }

        void UpgradeOldVersionDataIfNeeded()
        {
#pragma warning disable 612, 618
            if (m_Spline != null && m_Spline.Count > 0)
            {
                if (m_Splines == null || m_Splines.Length == 0 || m_Splines.Length == 1 && m_Splines[0].Count == 0)
                    m_Splines = new[] { m_Spline };

                m_Spline = new Spline(); //Clear spline
            }
#pragma warning restore 612, 618
        }

        /// <summary>
        /// Computes interpolated position, direction and upDirection at ratio t. Calling this method to get the
        /// 3 vectors is faster than calling independently EvaluateSplinePosition, EvaluateSplineTangent and EvaluateSplineUpVector
        /// for the same time t as it reduces some redundant computation.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <param name="position">The output variable for the float3 position at t.</param>
        /// <param name="tangent">The output variable for the float3 tangent at t.</param>
        /// <param name="upVector">The output variable for the float3 up direction at t.</param>
        /// <returns>Boolean value, true if a valid set of output variables as been computed.</returns>
        public bool Evaluate(float t, out float3 position, out float3 tangent, out float3 upVector)
            => Evaluate(0, t, out position, out tangent, out upVector);

        /// <summary>
        /// Computes the interpolated position, direction and upDirection at ratio t for the spline at index `splineIndex`. Calling this method to get the
        /// 3 vectors is faster than calling independently EvaluateSplinePosition, EvaluateSplineTangent and EvaluateSplineUpVector
        /// for the same time t as it reduces some redundant computation.
        /// </summary>
        /// <param name="splineIndex">The index of the spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 that represents the ratio along the curve.</param>
        /// <param name="position">The output variable for the float3 position at t.</param>
        /// <param name="tangent">The output variable for the float3 tangent at t.</param>
        /// <param name="upVector">The output variable for the float3 up direction at t.</param>
        /// <returns>True if a valid set of output variables is computed and false otherwise.</returns>
        public bool Evaluate(int splineIndex, float t, out float3 position,  out float3 tangent,  out float3 upVector)
        {
            if (Splines[splineIndex] == null)
            {
                position = float3.zero;
                tangent = new float3(0, 0, 1);
                upVector = new float3(0, 1, 0);
                return false;
            }

            if (IsScaled)
            {
                using var nativeSpline = new NativeSpline(Splines[splineIndex], transform.localToWorldMatrix);
                return SplineUtility.Evaluate(nativeSpline, t, out position, out tangent, out upVector);
            }

            var evaluationStatus = SplineUtility.Evaluate(Splines[splineIndex], t, out position, out tangent, out upVector);
            if (evaluationStatus)
            {
                position = transform.TransformPoint(position);
                tangent = transform.TransformVector(tangent);
                upVector = transform.TransformDirection(upVector);
            }

            return evaluationStatus;
        }

        /// <summary>
        /// valuates the position of a point, t, on a spline in world space.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of the curve.</param>
        /// <returns>A tangent vector.</returns>
        public float3 EvaluatePosition(float t) => EvaluatePosition(0, t);

        /// <summary>
        /// Evaluates the position of a point, t, on a spline at an index, `splineIndex`, in world space.
        /// </summary>
        /// <param name="splineIndex">The index of the spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of the curve.</param>
        /// <returns>A world position along the spline.</returns>
        public float3 EvaluatePosition(int splineIndex, float t)
        {
            if(Splines[splineIndex] == null)
                return float.PositiveInfinity;

            if(IsScaled)
            {
                using var nativeSpline = new NativeSpline(Splines[splineIndex], transform.localToWorldMatrix);
                return SplineUtility.EvaluatePosition(nativeSpline, t);
            }
            return transform.TransformPoint(SplineUtility.EvaluatePosition(Splines[splineIndex], t));
        }

        /// <summary>
        /// Evaluates the tangent vector of a point, t, on a spline in world space.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed tangent vector.</returns>
        public float3 EvaluateTangent(float t) => EvaluateTangent(0, t);

        /// <summary>
        /// Evaluates the tangent vector of a point, t, on a spline at an index, `splineIndex`, in world space.
        /// </summary>
        /// <param name="splineIndex">The index of the spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed tangent vector.</returns>
        public float3 EvaluateTangent(int splineIndex, float t)
        {
            if (Splines[splineIndex] == null)
                return 0;

            if(IsScaled)
            {
                using var nativeSpline = new NativeSpline(Splines[splineIndex], transform.localToWorldMatrix);
                return SplineUtility.EvaluateTangent(nativeSpline, t);
            }
            return transform.TransformVector(SplineUtility.EvaluateTangent(Splines[splineIndex], t));
        }

        /// <summary>
        /// Evaluates the up vector of a point, t, on a spline in world space.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed up direction.</returns>
        public float3 EvaluateUpVector(float t) => EvaluateUpVector(0, t);

        /// <summary>
        /// Evaluates the up vector of a point, t, on a spline at an index, `splineIndex`, in world space.
        /// </summary>
        /// <param name="splineIndex">The index of the Spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed up direction.</returns>
        public float3 EvaluateUpVector(int splineIndex, float t)
        {
            if (Splines[splineIndex] == null)
                return float.PositiveInfinity;

            if(IsScaled)
            {
                using var nativeSpline = new NativeSpline(Splines[splineIndex], transform.localToWorldMatrix);
                return SplineUtility.EvaluateUpVector(nativeSpline, t);
            }

            //Using TransformDirection as up direction is Not sensible to scale
            return transform.TransformDirection(SplineUtility.EvaluateUpVector(Splines[splineIndex], t));
        }

        /// <summary>
        /// Evaluates the acceleration vector of a point, t, on a spline in world space.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed acceleration vector.</returns>
        public float3 EvaluateAcceleration(float t) => EvaluateAcceleration(0, t);

        /// <summary>
        /// Evaluates the acceleration vector of a point, t, on a spline at an index, `splineIndex,  in world space.
        /// </summary>
        /// <param name="splineIndex">The index of the spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed acceleration vector.</returns>
        public float3 EvaluateAcceleration(int splineIndex, float t)
        {
            if (Splines[splineIndex] == null)
                return float.PositiveInfinity;

            if(IsScaled)
            {
                using var nativeSpline = new NativeSpline(Splines[splineIndex], transform.localToWorldMatrix);
                return SplineUtility.EvaluateAcceleration(nativeSpline, t);
            }
            return transform.TransformVector(SplineUtility.EvaluateAcceleration(Splines[splineIndex], t));
        }

        /// <summary>
        /// Calculate the length of <see cref="Spline"/> in world space.
        /// </summary>
        /// <returns>The length of <see cref="Spline"/> in world space</returns>
        public float CalculateLength() => CalculateLength(0);

        /// <summary>
        /// Calculates the length of `Splines[splineIndex]` in world space.
        /// </summary>
        /// <param name="splineIndex">The index of the spline to evaluate.</param>
        /// <returns>The length of `Splines[splineIndex]` in world space</returns>
        public float CalculateLength(int splineIndex)
        {
            if (Splines[splineIndex] == null)
                return 0;

            return SplineUtility.CalculateLength(Splines[splineIndex], transform.localToWorldMatrix);
        }
    }
}