using System.Collections.Generic;
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
    public sealed class SplineContainer : MonoBehaviour, ISplineProvider
    {
        const string k_IconPath = "Packages/com.unity.splines/Editor/Resources/Icons/SplineComponent.png";

        readonly Spline[] m_SplineArray = new Spline[1];

        [SerializeField]
        Spline m_Spline = new Spline();

        IEnumerable<Spline> ISplineProvider.Splines
        {
            get
            {
                m_SplineArray[0] = Spline;
                return m_SplineArray;
            }
        }

        bool IsScaled => transform.lossyScale != Vector3.one;

        /// <summary>
        /// The instantiated <see cref="Spline"/> object attached to this component.
        /// </summary>
        public Spline Spline
        {
            get => m_Spline;
            set => m_Spline = value;
        }

        /// <summary>
        /// Compute interpolated position, direction and upDirection at ratio t. Calling this method to get the
        /// 3 vectors is faster than calling independently EvaluateSplinePosition, EvaluateSplineTangent and EvaluateSplineUpVector
        /// for the same time t as it reduces some redundant computation.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <param name="position">Output variable for the float3 position at t.</param>
        /// <param name="tangent">Output variable for the float3 tangent at t.</param>
        /// <param name="upVector">Output variable for the float3 up direction at t.</param>
        /// <returns>Boolean value, true if a valid set of output variables as been computed.</returns>
        public bool Evaluate(float t, out float3 position,  out float3 tangent,  out float3 upVector)
        {
            if (Spline == null)
            {
                position = float3.zero;
                tangent = new float3(0, 0, 1);
                upVector = new float3(0, 1, 0);
                return false;
            }

            if (IsScaled)
            {
                using var nativeSpline = new NativeSpline(Spline, transform.localToWorldMatrix);
                return SplineUtility.Evaluate(nativeSpline, t, out position, out tangent, out upVector);
            }

            var evaluationStatus = SplineUtility.Evaluate(Spline, t, out position, out tangent, out upVector);
            if (evaluationStatus)
            {
                position = transform.TransformPoint(position);
                tangent = transform.TransformVector(tangent);
                upVector = transform.TransformDirection(upVector);
            }

            return evaluationStatus;
        }

        /// <summary>
        /// Evaluate a tangent vector on a curve at a specific t in world space.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of the curve.</param>
        /// <returns>A tangent vector.</returns>
        public float3 EvaluatePosition(float t)
        {
            if(Spline == null)
                return float.PositiveInfinity;

            if(IsScaled)
            {
                using var nativeSpline = new NativeSpline(Spline, transform.localToWorldMatrix);
                return SplineUtility.EvaluatePosition(nativeSpline, t);
            }
            return transform.TransformPoint(SplineUtility.EvaluatePosition(Spline, t));
        }

        /// <summary>
        /// Evaluate a tangent vector at a specific t in world space.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline</param>
        /// <returns>A tangent vector</returns>
        public float3 EvaluateTangent(float t)
        {
            if (Spline == null)
                return 0;

            if(IsScaled)
            {
                using var nativeSpline = new NativeSpline(Spline, transform.localToWorldMatrix);
                return SplineUtility.EvaluateTangent(nativeSpline, t);
            }
            return transform.TransformVector(SplineUtility.EvaluateTangent(Spline, t));
        }

        /// <summary>
        /// Evaluate an up vector direction at a specific t
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline</param>
        /// <returns>An up direction.</returns>
        public float3 EvaluateUpVector(float t)
        {
            if (Spline == null)
                return float.PositiveInfinity;

            if(IsScaled)
            {
                using var nativeSpline = new NativeSpline(Spline, transform.localToWorldMatrix);
                return SplineUtility.EvaluateUpVector(nativeSpline, t);
            }

            //Using TransformDirection as up direction is Not sensible to scale
            return transform.TransformDirection(SplineUtility.EvaluateUpVector(Spline, t));
        }

        /// <summary>
        /// Evaluate an acceleration vector at a specific t
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline</param>
        /// <returns>An acceleration vector.</returns>
        public float3 EvaluateAcceleration(float t)
        {
            if (Spline == null)
                return float.PositiveInfinity;

            if(IsScaled)
            {
                using var nativeSpline = new NativeSpline(Spline, transform.localToWorldMatrix);
                return SplineUtility.EvaluateAcceleration(nativeSpline, t);
            }
            return transform.TransformVector(SplineUtility.EvaluateAcceleration(Spline, t));
        }

        /// <summary>
        /// Calculate the length of <see cref="Spline"/> in world space.
        /// </summary>
        /// <returns>The length of <see cref="Spline"/> in world space</returns>
        public float CalculateLength()
        {
            if(Spline == null)
                return 0;

            return SplineUtility.CalculateLength(Spline, transform.localToWorldMatrix);
        }
    }
}
