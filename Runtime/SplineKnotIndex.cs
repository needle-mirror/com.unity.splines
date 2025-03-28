using System;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Provides a tuple to define a couple (Spline index, Knot index) that identifies a particular knot on a spline.
    /// This tuple is used by <see cref="KnotLinkCollection"/> to maintain links between knots.
    /// </summary>
    [Serializable]
    public struct SplineKnotIndex : IEquatable<SplineKnotIndex>
    {
        /// <summary>
        /// Represents the default value for an invalid index.
        /// </summary>
        public static SplineKnotIndex Invalid = new SplineKnotIndex(-1, -1);

        /// <summary>
        /// The index of the spline in the <see cref="SplineContainer.Splines"/>.
        /// </summary>
        public int Spline;

        /// <summary>
        /// The index of the knot in the spline.
        /// </summary>
        public int Knot;

        /// <summary>
        /// Creates a new SplineKnotIndex to reference a knot.
        /// </summary>
        /// <param name="spline">The spline index.</param>
        /// <param name="knot">The knot index.</param>
        public SplineKnotIndex(int spline, int knot)
        {
            Spline = spline;
            Knot = knot;
        }

        /// <summary>
       /// Checks if two indices are equal.
        /// </summary>
        /// <param name="indexA">The first index.</param>
        /// <param name="indexB">The second index.</param>
        /// <returns>Returns true if the indices reference the same knot on the same spline, false otherwise.</returns>
        public static bool operator ==(SplineKnotIndex indexA, SplineKnotIndex indexB)
        {
            return indexA.Equals(indexB);
        }

        /// <summary>
        /// Checks if two indices are not equal.
        /// </summary>
        /// <param name="indexA">The first index.</param>
        /// <param name="indexB">The second index.</param>
        /// <returns>Returns false if the indices reference the same knot on the same spline, true otherwise.</returns>
        public static bool operator !=(SplineKnotIndex indexA, SplineKnotIndex indexB)
        {
            return !indexA.Equals(indexB);
        }

        /// <summary>
        /// Checks if two indices are equal.
        /// </summary>
        /// <param name="otherIndex">The index to compare against.</param>
        /// <returns>Returns true if the indices reference the same knot on the same spline, false otherwise.</returns>
        public bool Equals(SplineKnotIndex otherIndex)
        {
            return Spline == otherIndex.Spline && Knot == otherIndex.Knot;
        }

        /// <summary>
        /// Checks if two indices are equal.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns>Returns true if the object is a SplineKnotIndex and the indices reference the same knot on the same spline, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SplineKnotIndex other && Equals(other);
        }

        /// <summary>
        /// Checks if an index is greater than or equal to 0.
        /// </summary>
        /// <returns>Returns true if the indices are greater than or equal to 0, false otherwise.</returns>
        public bool IsValid()
        {
            return Spline >= 0 && Knot >= 0;
        }

        /// <summary>
        /// Gets a hash code for this SplineKnotIndex.
        /// </summary>
        /// <returns> A hash code for the SplineKnotIndex. </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Spline * 397) ^ Knot;
            }
        }

        /// <summary>
        /// Gets a string representation of a SplineKnotIndex.
        /// </summary>
        /// <returns> A string representation of this SplineKnotIndex. </returns>
        public override string ToString() => $"{{{Spline}, {Knot}}}";
    }
}
