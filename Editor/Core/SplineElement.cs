using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    interface ISplineElement : IEquatable<ISplineElement>
    {
        SplineInfo SplineInfo { get; }
        int KnotIndex { get; }
        float3 LocalPosition { get; set; }
        float3 Position { get; set; }
        bool IsValid();
    }

    struct SelectableKnot : ISplineElement, IEquatable<SelectableKnot>
    {
        public SplineInfo SplineInfo { get; }
        public int KnotIndex { get; }

        public float4x4 LocalToWorld => SplineInfo.LocalToWorld;

        public float3 Position
        {
            get => math.transform(LocalToWorld, LocalPosition);
            set => LocalPosition = math.transform(math.inverse(LocalToWorld), value);
        }

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

        public bool IsValid()
        {
            return SplineInfo.Spline != null && KnotIndex >= 0 && KnotIndex < SplineInfo.Spline.Count;
        }

        public quaternion Rotation
        {
            get => math.mul(new quaternion(LocalToWorld), LocalRotation);
            set => LocalRotation = math.mul(math.inverse(new quaternion(LocalToWorld)), value);
        }

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

        public TangentMode Mode
        {
            get => SplineInfo.Spline.GetTangentMode(KnotIndex);
            set
            {
                SplineInfo.Spline.SetTangentMode(KnotIndex, value);
                SplineSelectionUtility.ValidateTangentSelection(this);
            }
        }

        public void SetTangentMode(TangentMode mode, BezierTangent main)
        {
            var spline = SplineInfo.Spline;
            spline.SetTangentMode(KnotIndex, mode, main);
            SplineSelectionUtility.ValidateTangentSelection(this);
        }

        public SelectableTangent TangentIn => new SelectableTangent(SplineInfo, KnotIndex, BezierTangent.In);
        public SelectableTangent TangentOut => new SelectableTangent(SplineInfo, KnotIndex, BezierTangent.Out);

        public SelectableKnot(SplineInfo info, int index)
        {
            this.SplineInfo = info;
            this.KnotIndex = index;
        }

        public BezierKnot GetBezierKnot(bool worldSpace)
        {
            return worldSpace ? SplineInfo.Spline[KnotIndex].Transform(LocalToWorld) : SplineInfo.Spline[KnotIndex];
        }

        public bool Equals(ISplineElement other)
        {
            if (other is SelectableKnot knot)
                return Equals(knot);
            return false;
        }

        public bool Equals(SelectableKnot other)
        {
            return Equals(SplineInfo.Spline, other.SplineInfo.Spline) && KnotIndex == other.KnotIndex;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SelectableKnot other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SplineInfo.Spline, KnotIndex);
        }
    }

    struct SelectableTangent : ISplineElement, IEquatable<SelectableTangent>
    {
        public SplineInfo SplineInfo { get; }
        public int KnotIndex { get; }
        public int TangentIndex { get; }
        public SelectableKnot Owner => new SelectableKnot(SplineInfo, KnotIndex);
        public SelectableTangent OppositeTangent => new SelectableTangent(SplineInfo, KnotIndex, 1 - TangentIndex);

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

        public float3 Direction
        {
            get => MathUtility.MultiplyVector(LocalToWorld, LocalDirection);
            set => LocalDirection = MathUtility.MultiplyVector(math.inverse(LocalToWorld), value);
        }

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

        public float4x4 LocalToWorld => math.mul(SplineInfo.LocalToWorld, new float4x4(Owner.LocalRotation, Owner.LocalPosition));

        public float3 Position
        {
            get => math.transform(LocalToWorld, LocalPosition);
            set => LocalPosition = math.transform(math.inverse(LocalToWorld), value);
        }

        public float3 LocalPosition
        {
            get => LocalDirection;
            set => LocalDirection = value;
        }

        public SelectableTangent(SplineInfo splineInfo, int knotIndex, BezierTangent tangent)
            : this(splineInfo, knotIndex, (int)tangent) { }

        public SelectableTangent(SplineInfo splineInfo, int knotIndex, int tangentIndex)
        {
            SplineInfo = splineInfo;
            KnotIndex = knotIndex;
            TangentIndex = tangentIndex;
        }

        public bool Equals(ISplineElement other)
        {
            if (other is SelectableTangent tangent)
                return Equals(tangent);
            return false;
        }

        public bool Equals(SelectableTangent other)
        {
            return Equals(SplineInfo.Spline, other.SplineInfo.Spline) && KnotIndex == other.KnotIndex && TangentIndex == other.TangentIndex;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SelectableTangent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SplineInfo.Spline, KnotIndex, TangentIndex);
        }
    }
}
