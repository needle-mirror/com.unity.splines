using System;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// SplineInfo is used to wrap a reference to the <see cref="SplineContainer"/> and index within that container to
    /// describe a <see cref="Spline"/>.
    /// </summary>
    [Serializable]
    public struct SplineInfo : IEquatable<SplineInfo>, ISerializationCallbackReceiver
    {
        [SerializeField]
        Object m_Object;

        [SerializeReference]
        ISplineContainer m_Container;

        [SerializeField]
        int m_SplineIndex;

        /// <summary>
        /// If the referenced <see cref="ISplineContainer"/> is a UnityEngine.Object this field contains that object.
        /// </summary>
        public Object Object => m_Object;

        /// <summary>
        /// The <see cref="ISplineContainer"/> that contains the spline.
        /// </summary>
        public ISplineContainer Container
        {
            get => m_Container;
            set => m_Container = value;
        }

        /// <summary>
        /// The associated <see cref="UnityEngine.Transform"/> of the target. This may be null if the container is not
        /// a MonoBehaviour.
        /// </summary>
        public Transform Transform => Object is Component component ? component.transform : null;

        /// <summary>
        /// A reference to the <see cref="UnityEngine.Splines.Spline"/>. This may be null if the container or index are
        /// invalid.
        /// </summary>
        public Spline Spline => Container != null && Index > -1 && Index < Container.Splines.Count
            ? Container.Splines[Index]
            : null;

        /// <summary>
        /// The index of the spline in the enumerable returned by the <see cref="ISplineContainer"/>.
        /// </summary>
        public int Index
        {
            get => m_SplineIndex;
            set => m_SplineIndex = value;
        }

        /// <summary>
        /// Matrix that transforms the <see cref="Object"/> from local space into world space.
        /// </summary>
        public float4x4 LocalToWorld => Transform != null ? (float4x4)Transform.localToWorldMatrix : float4x4.identity;

        /// <summary>
        /// Create a new <see cref="SplineInfo"/> object.
        /// </summary>
        /// <param name="container">The <see cref="ISplineContainer"/> that contains the spline.</param>
        /// <param name="index">The index of the spline in the enumerable returned by the <see cref="ISplineContainer"/>.</param>
        public SplineInfo(ISplineContainer container, int index)
        {
            m_Container = container;
            m_Object = container as Object;
            m_SplineIndex = index;
        }

        /// <summary>
        /// Compare against another <see cref="SplineInfo"/> for equality.
        /// </summary>
        /// <param name="other">The other instance to compare against.</param>
        /// <returns>
        /// Returns true when <paramref name="other"/> is a <see cref="SplineInfo"/> and the values of each instance are
        /// identical.
        /// </returns>
        public bool Equals(SplineInfo other)
        {
            return Equals(Container, other.Container) && Index == other.Index;
        }

        /// <summary>
        /// Compare against an object for equality.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns>
        /// Returns true when <paramref name="obj"/> is a <see cref="SplineInfo"/> and the values of each instance are
        /// identical.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SplineInfo other && Equals(other);
        }

        /// <summary>
        /// Calculate a hash code for this info.
        /// </summary>
        /// <returns>
        /// A hash code for the <see cref="SplineInfo"/>.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Container != null ? Container.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Index;
                return hashCode;
            }
        }

        /// <summary>
        /// Called before this object is serialized by the Editor.
        /// </summary>
        public void OnBeforeSerialize()
        {
            if (m_Container is Object obj)
            {
                m_Object = obj;
                m_Container = null;
            }
        }

        /// <summary>
        /// Called after this object is deserialized by the Editor.
        /// </summary>
        public void OnAfterDeserialize()
        {
            m_Container ??= m_Object as ISplineContainer;
        }
    }
}
