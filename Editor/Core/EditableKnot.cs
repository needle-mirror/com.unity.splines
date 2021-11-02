using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityEditor.Splines
{
    [Serializable]
    class EditableKnot : ISplineElement
    {
        internal static event Action<EditableKnot> knotModified; 

        [SerializeField]
        float3 m_LocalPosition;

        [SerializeField]
        quaternion m_LocalRotation = quaternion.identity;
        
        public IEditableSpline spline { get; internal set; }
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
                SetDirty();
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
                SetDirty();
            }
        }
        
        /// <summary>
        /// How many editable tangents a knot contains. Cubic bezier splines contain 2 tangents, except at the ends of
        /// a Spline that is not closed, in which case the knot contains a single tangent. Other spline type representations
        /// may contain more or fewer tangents (ex, a Catmull-Rom spline does not expose any editable tangents). 
        /// </summary>
        public int tangentCount => spline.tangentsPerKnot;

        public virtual void Copy(EditableKnot other)
        {
            spline = other.spline;
            index = other.index;
            m_LocalPosition = other.localPosition;
            m_LocalRotation = other.localRotation;
            for (int i = 0, count = math.min(tangentCount, other.tangentCount); i < count; ++i)
                GetTangent(i).Copy(other.GetTangent(i));
        }

        public void SetDirty()
        {
            knotModified?.Invoke(this);
            spline?.SetDirty();
        }
        
        internal virtual EditableTangent GetTangent(int index) { return null; }

        public virtual void ValidateData() {}
        public virtual void OnPathUpdatedFromTarget() {}
        public virtual void OnKnotInsertedOnCurve(EditableKnot previous, EditableKnot next, float t) {}
    }
}
