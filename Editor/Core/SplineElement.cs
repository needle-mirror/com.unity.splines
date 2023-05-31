using System;
using Unity.Mathematics;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// An interface that represents a selectable spline element. A selectable spline element can be a knot or a tangent. 
    /// `ISelectableElement` is used by the selection to get information about the spline, the knot, and the positions of the spline elements.
    /// </summary>
    public interface ISelectableElement : IEquatable<ISelectableElement>
    {
        /// <summary>
        /// The <see cref="SplineInfo"/> that describes the spline.
        /// </summary>
        SplineInfo SplineInfo { get; }
        /// <summary>
        /// The index of the knot in the spline. If the spline element is a tangent, this is the index of the knot
        /// that the tangent is attached to.
        /// </summary>
        int KnotIndex { get; }
        
        /// <summary>
        /// The position of the spline element in local space.
        /// </summary>
        float3 LocalPosition { get; set; }
        
        /// <summary>
        /// The position of the spline element in world space.
        /// </summary>
        float3 Position { get; set; }
        
        /// <summary>
        /// Checks if the element is valid. For example, checks if the spline is not null and the index is valid.
        /// </summary>
        /// <returns>Returns true if all fields from the element have valid values.</returns>
        bool IsValid();
    }

    /// <summary>
    /// Implements the <see cref="ISelectableElement"/> interface. SelectableKnot is used by the
    /// spline selection and handles to use tools and handles to manipulate spline elements.
    /// </summary>
    public struct SelectableKnot : ISelectableElement, IEquatable<SelectableKnot>
    {
        /// <inheritdoc />
        public SplineInfo SplineInfo { get; }
        
        /// <inheritdoc />
        public int KnotIndex { get; }

        /// <summary>
        /// Transforms a knot from local space to world space (Read Only).
        /// </summary>
        internal float4x4 LocalToWorld => SplineInfo.LocalToWorld;

        /// <inheritdoc />
        public float3 Position
        {
            get => math.transform(LocalToWorld, LocalPosition);
            set => LocalPosition = math.transform(math.inverse(LocalToWorld), value);
        }

        /// <inheritdoc />
        public float3 LocalPosition
        {
            get => SplineInfo.Spline[KnotIndex].Position;
            set
            {
                var knot = SplineInfo.Spline[KnotIndex];
                knot.Position = value;
                SplineInfo.Spline[KnotIndex] = knot;
            }
        }

        /// <inheritdoc />
        public bool IsValid()
        {
            return SplineInfo.Spline != null && KnotIndex >= 0 && KnotIndex < SplineInfo.Spline.Count;
        }

        /// <summary>
        /// The rotation of the spline element in world space.
        /// </summary>
        public quaternion Rotation
        {
            get => math.mul(new quaternion(LocalToWorld), LocalRotation);
            set => LocalRotation = math.mul(math.inverse(new quaternion(LocalToWorld)), value);
        }

        /// <summary>
        /// The rotation of the spline element in local space.
        /// </summary>
        public quaternion LocalRotation
        {
            get => SplineInfo.Spline[KnotIndex].Rotation;
            set
            {
                var knot = SplineInfo.Spline[KnotIndex];
                knot.Rotation = math.normalize(value);
                SplineInfo.Spline[KnotIndex] = knot;
            }
        }

        /// <summary>
        /// The <see cref="TangentMode"/> associated with a knot.
        /// </summary>
        public TangentMode Mode
        {
            get => SplineInfo.Spline.GetTangentMode(KnotIndex);
            set
            {
                SplineInfo.Spline.SetTangentMode(KnotIndex, value);
                SplineSelectionUtility.ValidateTangentSelection(this);
            }
        }

        /// <summary>
        /// The tension associated with a knot. `Tension` is only used if the tangent mode is Auto Smooth.
        /// </summary>
        public float Tension
        {
            get => SplineInfo.Spline.GetAutoSmoothTension(KnotIndex);
            set => SplineInfo.Spline.SetAutoSmoothTension(KnotIndex, value);
        }

        /// <summary>
        /// Sets the tangent mode of the knot.
        /// </summary>
        /// <param name="mode">The <see cref="TangentMode"/> to apply to the knot.</param>
        /// <param name="main">The tangent to use as the main tangent when the tangent is set to the Mirrored or Continuous tangent mode. 
        /// The main tangent is not modified, but the other tangent attached to that knot is modified to adopt the new tangent mode.</param>
        public void SetTangentMode(TangentMode mode, BezierTangent main)
        {
            var spline = SplineInfo.Spline;
            spline.SetTangentMode(KnotIndex, mode, main);
            SplineSelectionUtility.ValidateTangentSelection(this);
        }

        /// <summary>
        /// The In tangent associated with the knot. The In tangent defines the curvature of the segment that enters the knot.
        /// </summary>
        public SelectableTangent TangentIn => new SelectableTangent(SplineInfo, KnotIndex, BezierTangent.In);
        /// <summary>
        /// The Out tangent associated with the knot. The Out tangent defines the curvature of the segment that exits the knot.
        /// </summary>
        public SelectableTangent TangentOut => new SelectableTangent(SplineInfo, KnotIndex, BezierTangent.Out);

        /// <summary>
        /// Creates a <see cref="SelectableKnot"/> from a SplineInfo and a knot index.
        /// </summary>
        /// <param name="info">The <see cref="SplineInfo"/> associated with the tangent.</param>
        /// <param name="index">The index of the knot.</param>
        public SelectableKnot(SplineInfo info, int index)
        {
            this.SplineInfo = info;
            this.KnotIndex = index;
        }

        /// <summary>
        /// Creates the BezierKnot representation associated with a SelectableKnot.
        /// </summary>
        /// <param name="worldSpace">Set to true for the BezierKnot to be in world space, or set to false for the Bezierknot to be in local space.</param>
        /// <returns>The <see cref="BezierKnot"/> associated with the knot.</returns>
        public BezierKnot GetBezierKnot(bool worldSpace)
        {
            return worldSpace ? SplineInfo.Spline[KnotIndex].Transform(LocalToWorld) : SplineInfo.Spline[KnotIndex];
        }

        /// <summary>
        /// Checks if two instances of `SplineElement` are equal.
        /// </summary>
        /// <param name="other">The <see cref="ISelectableElement"/> to compare against.</param>
        /// <returns>
        /// Returns true when <paramref name="other"/> is a <see cref="SelectableKnot"/> and the values of each instance are identical.
        /// </returns>
        public bool Equals(ISelectableElement other)
        {
            if (other is SelectableKnot knot)
                return Equals(knot);
            return false;
        }

        /// <summary>
        /// Checks if two instances of SelectableKnot are equal.
        /// </summary>
        /// <param name="other">The <see cref="SelectableKnot"/> to compare against.</param>
        /// <returns>
        /// Returns true if the values of each instance of `SelectableKnot` are identical.
        /// </returns>
        public bool Equals(SelectableKnot other)
        {
            return Equals(SplineInfo.Spline, other.SplineInfo.Spline) && KnotIndex == other.KnotIndex;
        }

        /// <summary>
        /// Checks if two instances of an object are equal. 
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns>
        /// Returns true if <paramref name="obj"/> is a <see cref="SelectableKnot"/> and its values are identical to the original instance.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SelectableKnot other && Equals(other);
        }

        /// <summary>
        /// Gets a hash code for this knot.
        /// </summary>
        /// <returns>
        /// A hash code for the <see cref="SelectableKnot"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(SplineInfo.Spline, KnotIndex);
        }
    }
    /// <summary>
    /// Represents a struct that implements the <see cref="ISelectableElement"/> interface. Spline selection uses 
    /// `SelectableTangent` and handles to easily manipulate spline elements with tools and handles.
    /// </summary>
    public struct SelectableTangent : ISelectableElement, IEquatable<SelectableTangent>
    {
        /// <inheritdoc />
        public SplineInfo SplineInfo { get; }
        
        /// <inheritdoc />
        public int KnotIndex { get; }
        
        /// <summary>
        /// The index of the tangent. A value of 0 represents an In tangent. A value of 1 represents an Out tangent.
        /// </summary>
        public int TangentIndex { get; }
        
        /// <summary>
        /// The knot associated with this tangent.
        /// </summary>
        public SelectableKnot Owner => new SelectableKnot(SplineInfo, KnotIndex);
        
        /// <summary>
        /// The opposite tangent on the knot. If this tangent is the In tangent, then the opposite tangent is the Out tangent. If this tangent is the Out tangent, then the opposite tangent is the In tangent.
        /// </summary>
        public SelectableTangent OppositeTangent => new SelectableTangent(SplineInfo, KnotIndex, 1 - TangentIndex);

        /// <inheritdoc />
        public bool IsValid()
        {
            return SplineInfo.Spline != null
                && KnotIndex >= 0
                && KnotIndex < SplineInfo.Spline.Count
                && TangentIndex >= 0
                && TangentIndex < 2
                && Owner.Mode != TangentMode.Linear
                && Owner.Mode != TangentMode.AutoSmooth;
        }

        /// <summary>
        /// The direction of the tangent in world space.
        /// </summary>
        public float3 Direction
        {
            get => MathUtility.MultiplyVector(LocalToWorld, LocalDirection);
            set => LocalDirection = MathUtility.MultiplyVector(math.inverse(LocalToWorld), value);
        }

        /// <summary>
        /// The direction of the tangent in local space.
        /// </summary>
        public float3 LocalDirection
        {
            get => TangentIndex == (int)BezierTangent.In ? SplineInfo.Spline[KnotIndex].TangentIn : SplineInfo.Spline[KnotIndex].TangentOut;
            set
            {
                var spline = SplineInfo.Spline;
                var knot = spline[KnotIndex];

                switch (TangentIndex)
                {
                    case (int)BezierTangent.In:
                        knot.TangentIn = value;
                        break;

                    case (int)BezierTangent.Out:
                        knot.TangentOut = value;
                        break;
                }

                spline.SetKnot(KnotIndex, knot, (BezierTangent)TangentIndex);
            }
        }
        
        /// <summary>
        /// Matrix that transforms a tangent point from local space into world space using its associated knot (Read Only).
        /// </summary>
        internal float4x4 LocalToWorld => math.mul(SplineInfo.LocalToWorld, new float4x4(Owner.LocalRotation, Owner.LocalPosition));

        /// <inheritdoc />
        public float3 Position
        {
            get => math.transform(LocalToWorld, LocalPosition);
            set => LocalPosition = math.transform(math.inverse(LocalToWorld), value);
        }

        /// <inheritdoc />
        public float3 LocalPosition
        {
            get => LocalDirection;
            set => LocalDirection = value;
        }

        /// <summary>
        /// Creates a new <see cref="SelectableTangent"/> object.
        /// </summary>
        /// <param name="info">The <see cref="SplineInfo"/> associated with the tangent.</param>
        /// <param name="knotIndex">The index of the knot that the tangent is attached to.</param>
        /// <param name="tangent">The <see cref="BezierTangent"/> that represents this tangent.</param>
        public SelectableTangent(SplineInfo info, int knotIndex, BezierTangent tangent)
            : this(info, knotIndex, (int)tangent) { }

        /// <summary>
        /// Creates a new <see cref="SelectableTangent"/> object.
        /// </summary>
        /// <param name="info">The <see cref="SplineInfo"/> associated with the tangent.</param>
        /// <param name="knotIndex">The index of the knot that the tangent is attached to.</param>
        /// <param name="tangentIndex">The index of the tangent. A value of 0 represents an In tangent. A value of 1 represents an Out tangent.</param>
        public SelectableTangent(SplineInfo info, int knotIndex, int tangentIndex)
        {
            SplineInfo = info;
            KnotIndex = knotIndex;
            TangentIndex = tangentIndex;
        }

        /// <summary>
        /// Checks if two instances of a `SplineElement` are equal.
        /// </summary>
        /// <param name="other">The <see cref="ISelectableElement"/> to compare against.</param>
        /// <returns>
        /// Returns true when <paramref name="other"/> is a <see cref="SelectableTangent"/> and the values of each instance are identical.
        /// </returns>
        public bool Equals(ISelectableElement other)
        {
            if (other is SelectableTangent tangent)
                return Equals(tangent);
            return false;
        }

        /// <summary>
        /// Checks if two instances of `SelectableTangent` are equal.
        /// </summary>
        /// <param name="other">The <see cref="SelectableTangent"/> to compare against.</param>
        /// <returns>
        /// Returns true if the values of each instance are identical.
        /// </returns>
        public bool Equals(SelectableTangent other)
        {
            return Equals(SplineInfo.Spline, other.SplineInfo.Spline) && KnotIndex == other.KnotIndex && TangentIndex == other.TangentIndex;
        }

        /// <summary>
        /// Checks if two objects are equal. 
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns>
        /// Returns true when <paramref name="obj"/> is a <see cref="SelectableTangent"/> and the values of each instance are identical.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SelectableTangent other && Equals(other);
        }
        
        /// <summary>
        /// Gets a hash code for this tangent.
        /// </summary>
        /// <returns>
        /// A hash code for the <see cref="SelectableTangent"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(SplineInfo.Spline, KnotIndex, TangentIndex);
        }
    }
}
