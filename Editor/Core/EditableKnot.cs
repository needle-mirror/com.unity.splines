using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityEditor.Splines
{
    [Serializable]
    class EditableKnot : ISplineElement
    {
        [SerializeField]
        float3 m_LocalPosition;

        [SerializeField]
        quaternion m_LocalRotation = quaternion.identity;
        
        public IEditableSpline spline { get; internal set; }
        internal IEditableSplineConversionData splineConversionData { get; set; }
        public int index { get; internal set; }

        public bool IsValid()
        {
            return index >= 0;
        }

        /// <summary> Matrix that transforms a point from local (knot) into world space. </summary>
        public Matrix4x4 localToWorldMatrix => spline.localToWorldMatrix * Matrix4x4.TRS(localPosition, localRotation, Vector3.one);
        /// <summary> Matrix that transforms a point from world space into local (knot) space. </summary>
        public Matrix4x4 worldToLocalMatrix => localToWorldMatrix.inverse;

        public EditableKnot GetPrevious()
        {
            return spline.GetPreviousKnot(index, out EditableKnot previous) ? previous : null;
        }

        public EditableKnot GetNext()
        {
            return spline.GetNextKnot(index, out EditableKnot next) ? next : null;
        }

        /// <summary>
        /// World space position of the knot.
        /// </summary>
        public float3 position
        {
            get => spline.localToWorldMatrix.MultiplyPoint3x4(localPosition);
            set => localPosition = spline.worldToLocalMatrix.MultiplyPoint3x4(value);
        }

        /// <summary>
        /// Local (spline space) position of the knot.
        /// </summary>
        public float3 localPosition
        {
            get => m_LocalPosition;
            set
            {
                if (m_LocalPosition.Equals(value))
                    return;

                m_LocalPosition = value;
                spline.SetDirty();
            }
        }

        /// <summary>
        /// World space rotation of the knot.
        /// </summary>
        public quaternion rotation
        {
            get => spline.localToWorldMatrix.rotation * localRotation;
            set => localRotation = math.mul(spline.worldToLocalMatrix.rotation, value);
        }

        /// <summary>
        /// Local (spline space) rotation of the knot.
        /// </summary>
        public quaternion localRotation
        {
            get => m_LocalRotation;
            set
            {
                if (m_LocalRotation.Equals(value))
                    return;

                m_LocalRotation = math.normalize(value);
                spline.SetDirty();
            }
        }

        public virtual int tangentCount => 0;
        internal virtual EditableTangent GetTangent(int index) { return null; }
        public virtual void ValidateData() {}
        public virtual void OnPathUpdatedFromTarget() {}
        public virtual void OnKnotInsertedOnCurve(EditableKnot previous, EditableKnot next, float t) {}
        public virtual void OnKnotAddedToPathEnd(float3 position, float3 normal) {}
    }
}
