using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    /// <summary>
    /// Provides information about a spline. Used in editor utility functions.
    /// </summary>
    struct SplineInfo : IEquatable<SplineInfo>
    {
        /// <summary>
        /// The <see cref="UnityEngine.Object"/> that contains the spline.
        /// </summary>
        public Object Target => Container as Object;

        /// <summary>
        /// The <see cref="ISplineContainer"/> that contains the spline.
        /// </summary>
        public ISplineContainer Container { get; }

        /// <summary>
        /// The associated <see cref="UnityEngine.Transform"/> of the target.
        /// </summary>
        public Transform Transform { get; }

        /// <summary>
        /// A reference to the <see cref="UnityEngine.Splines.Spline"/>.
        /// </summary>
        public Spline Spline => Container != null && Index < Container.Splines.Count ? Container.Splines[Index] : null;

        /// <summary>
        /// The index of the spline in the enumerable returned by the <see cref="ISplineContainer"/>.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Matrix that transforms the <see cref="SplineInfo.Target"/> from local space into world space.
        /// </summary>
        public float4x4 LocalToWorld => Transform != null ? (float4x4)Transform.localToWorldMatrix : float4x4.identity;

        internal SplineInfo(ISplineContainer container, int index)
        {
            Container = container;
            Transform = container is Component component ? component.transform : null;
            Index = index;
        }

        public bool Equals(SplineInfo other)
        {
            return Equals(Container, other.Container) && Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SplineInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Container != null ? Container.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Transform != null ? Transform.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Spline != null ? Spline.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Index;
                return hashCode;
            }
        }
    }
}