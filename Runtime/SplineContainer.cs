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
    public sealed class SplineContainer : MonoBehaviour, ISplineProvider
    {
        const string k_IconPath = "Packages/com.unity.splines/Editor/Resources/Icons/KnotPlacementTool.png";

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

        /// <summary>
        /// The instantiated <see cref="Spline"/> object attached to this component.
        /// </summary>
        public Spline Spline
        {
            get => m_Spline;
            set => m_Spline = value;
        }

        /// <summary>
        /// Evaluate the position on a curve at a specific t in world space.
        /// Use <see cref="SplineUtility.SplineToCurveInterpolation{T}"/> to convert a time value from
        /// spline to curve space.
        /// </summary>
        /// <param name="curveIndex">The index of the <see cref="BezierCurve"/> to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of the curve.</param>
        /// <returns>A position in world space.</returns>
        public float3 EvaluateCurvePosition(int curveIndex, float t)
        {
            if (Spline == null)
                return float.PositiveInfinity;

            return transform.TransformPoint(CurveUtility.EvaluatePosition(Spline.GetCurve(curveIndex), t));
        }

        /// <summary>
        /// Evaluate a tangent vector on a curve at a specific t in world space.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of the curve.</param>
        /// <returns>A tangent vector.</returns>
        public float3 EvaluateSplinePosition(float t)
        {
            if (Spline == null)
                return float.PositiveInfinity;

            return transform.TransformPoint(SplineUtility.EvaluatePosition(Spline, t));
        }

        /// <summary>
        /// Evaluate a tangent vector on a curve at a specific t in world space.
        /// Use <see cref="SplineUtility.SplineToCurveInterpolation{T}"/> to convert a time value from
        /// spline to curve space.
        /// </summary>
        /// <param name="curveIndex">The index of the <see cref="BezierCurve"/> to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of the curve.</param>
        /// <returns>A tangent vector in world space.</returns>
        public float3 EvaluateCurveTangent(int curveIndex, float t)
        {
            if (Spline == null)
                return float.PositiveInfinity;

            return transform.TransformDirection(CurveUtility.EvaluateTangent(Spline.GetCurve(curveIndex), t));
        }

        /// <summary>
        /// Evaluate a tangent vector at a specific t in world space.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline</param>
        /// <returns>A tangent vector</returns>
        public float3 EvaluateSplineTangent(float t)
        {
            if (Spline == null)
                return 0;

            return transform.TransformDirection(SplineUtility.EvaluateDirection(Spline, t));
        }
        
        /// <summary>
        /// Evaluate an up vector at a specific t
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline</param>
        /// <returns>An up vector</returns>
        public float3 EvaluateSplineUpVector(float t)
        {
            if (Spline == null)
                return float.PositiveInfinity;

            return transform.TransformDirection(SplineUtility.EvaluateUpVector(Spline, t));
        }

        /// <summary>
        /// Calculate the length of a <see cref="BezierCurve"/> in world space.
        /// </summary>
        /// <param name="curveIndex">The index of the curve to fetch length for.</param>
        /// <returns>The length of a <see cref="BezierCurve"/> in world space.</returns>
        public float CalculateCurveLength(int curveIndex)
        {
            if (Spline == null)
                return 0;

            using var spline = Spline.ToNativeSpline(transform.localToWorldMatrix);
            return spline.GetCurveLength(curveIndex);
        }

        /// <summary>
        /// Calculate the length of <see cref="Spline"/> in world space.
        /// </summary>
        /// <returns>The length of <see cref="Spline"/> in world space</returns>
        public float CalculateSplineLength()
        {
            if(Spline == null)
                return 0;

            return SplineUtility.CalculateLength(Spline, transform.localToWorldMatrix);
        }
    }
}
