using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Splines;
using Unity.Mathematics;
using Unity.Splines.Examples;
using UnityEditor.Splines;
using Interpolators = UnityEngine.Splines.Interpolators;

namespace Unity.Splines.Examples
{
    public class SplineFollow : MonoBehaviour
    {
        [SerializeField]
        SplineContainer m_SplineContainer;

        [SerializeField]
        float m_StartOffset = 0f;

        [SerializeField]
        float m_Speed = 0.05f;

        [SerializeField]
        float m_RightOffset = 0.05f;

        [SerializeField]
        [SplineDataDrawer(typeof(SpeedHandle))]
        SplineData<float> m_SpeedData;
        [SerializeField]
        [SplineDataDrawer(typeof(CustomUpVectorHandle))]
        SplineData<float3> m_UpData;

        [SerializeField]
        Transform m_LookAtTarget = null;

        [SerializeField]
        float m_LookAtForwardOffset = 1f;
        [SerializeField]
        float m_LookAtStrength = 0.5f;

        float m_CurrentOffset;
        float m_CurrentSpeed;
        float m_SplineLength;
        Spline m_Spline;

        void Start()
        {
            Assert.IsNotNull(m_SplineContainer);

            m_Spline = m_SplineContainer.Spline;
            m_SplineLength = m_Spline.GetLength();
            m_CurrentOffset = m_StartOffset;
        }

        void Update()
        {
            if (m_SpeedData != null)
                m_CurrentSpeed = m_SpeedData.Evaluate(m_Spline, m_CurrentOffset, new Interpolators.LerpFloat());
            else
                m_CurrentSpeed = m_Speed;

            var speedDt = m_CurrentSpeed * Time.deltaTime;

            m_CurrentOffset = (m_CurrentOffset + speedDt / m_SplineLength) % 1f;

            var posOnSplineLocal = SplineUtility.EvaluatePosition(m_Spline, m_CurrentOffset);
        var upSplineDirection = SplineUtility.EvaluateUpVector(m_Spline, m_CurrentOffset);
            var posOnSplineWorld = m_SplineContainer.transform.TransformPoint(posOnSplineLocal);
        transform.position = posOnSplineWorld + Vector3.ProjectOnPlane(transform.right * m_RightOffset, upSplineDirection);

            var rotation = Quaternion.identity;

            var up = m_UpData == null ? Vector3.up : (Vector3)m_UpData.Evaluate(m_Spline, m_CurrentOffset, new Interpolators.LerpFloat3());

        if(m_LookAtTarget == null)
        {
            var direction = SplineUtility.EvaluateDirection(m_Spline, m_CurrentOffset);
            var rot = Quaternion.LookRotation(direction, upSplineDirection);
            rotation = Quaternion.LookRotation(direction, rot * up);
        }
            else
            {
                var lookAtPoint = (m_LookAtTarget.transform.position + m_LookAtForwardOffset * m_LookAtTarget.transform.forward);
                rotation = Quaternion.Lerp(rotation, Quaternion.LookRotation(lookAtPoint - transform.position), m_LookAtStrength);
            }
            transform.rotation = rotation;
        }
    }
}