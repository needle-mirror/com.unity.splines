using System;

namespace UnityEngine.Splines
{
    [Serializable]
    public struct SplineKnotIndex : IEquatable<SplineKnotIndex>
    {
        public int Spline;
        public int Knot;

        public SplineKnotIndex(int spline, int knot)
        {
            Spline = spline;
            Knot = knot;
        }

        public static bool operator ==(SplineKnotIndex indexA, SplineKnotIndex indexB)
        {
            return indexA.Equals(indexB);
        }

        public static bool operator !=(SplineKnotIndex indexA, SplineKnotIndex indexB)
        {
            return !indexA.Equals(indexB);
        }

        public bool Equals(SplineKnotIndex other)
        {
            return Spline == other.Spline && Knot == other.Knot;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SplineKnotIndex other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Spline * 397) ^ Knot;
            }
        }

        public override string ToString() => $"{{{Spline}, {Knot}}}";
    }
}