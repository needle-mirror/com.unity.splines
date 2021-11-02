using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityEditor.Splines
{
    [Serializable]
    sealed class EditableTangent : ISplineElement
    {
        internal event Action directionChanged;

        [SerializeField]
        float3 m_LocalPosition;
        
        /// <summary> Local (knot space) position of the tangent. </summary>
        public float3 localPosition
        {
            get => m_LocalPosition;
            set
            {
                if (m_LocalPosition.Equals(value))
                    return;

                m_LocalPosition = value;
                directionChanged?.Invoke();
            }
        }

        /// <summary> World space direction of the tangent. </summary>
        public float3 direction
        {
            get => owner.localToWorldMatrix.MultiplyVector(localPosition);
            set => localPosition = owner.worldToLocalMatrix.MultiplyVector(value);
        }

        /// <summary> World space position of the tangent. </summary>
        public float3 position
        {
            get => owner.localToWorldMatrix.MultiplyPoint3x4(localPosition);
            set => localPosition = owner.worldToLocalMatrix.MultiplyPoint3x4(value);
        }

        internal void SetLocalPositionNoNotify(float3 localPosition)
        {
            m_LocalPosition = localPosition;
        }

        public int tangentIndex { get; private set; }

        public EditableKnot owner { get; private set; }

        /// <summary> Matrix that transforms a point from local (tangent) into world space. </summary>
        public Matrix4x4 localToWorldMatrix => owner.localToWorldMatrix * 
                                               Matrix4x4.TRS(localPosition, quaternion.identity, Vector3.one);
        /// <summary> Matrix that transforms a point from world space into local (tangent) space. </summary>
        public Matrix4x4 worldToLocalMatrix => localToWorldMatrix.inverse;

        public EditableTangent() : this(null, -1) {}

        public EditableTangent(EditableKnot owner, int tangentIndex)
        {
            this.owner = owner;
            this.tangentIndex = tangentIndex;
        }
        
        public void Copy(EditableTangent other)
        {
            tangentIndex = other.tangentIndex;
            m_LocalPosition = other.localPosition;
        }
    }
}
