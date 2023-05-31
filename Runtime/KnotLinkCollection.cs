using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A collection of KnotLinks to track how spline knots are linked and the utilities to
    /// update these links when splines are modified.
    /// </summary>
    [Serializable]
    public sealed class KnotLinkCollection
    {
        [Serializable]
        sealed class KnotLink : IReadOnlyList<SplineKnotIndex>
        {
            public SplineKnotIndex[] Knots;

            public IEnumerator<SplineKnotIndex> GetEnumerator() => ((IEnumerable<SplineKnotIndex>)Knots).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => Knots.GetEnumerator();

            public int Count => Knots.Length;

            public SplineKnotIndex this[int index] => Knots[index];
        }

        [SerializeField]
        KnotLink[] m_KnotsLink = new KnotLink[0];

        /// <summary>
        /// How many KnotLinks the collection contains.
        /// </summary>
        public int Count => m_KnotsLink.Length;

        KnotLink GetKnotLinksInternal(SplineKnotIndex index)
        {
            foreach (var knotLink in m_KnotsLink)
                if (Array.IndexOf(knotLink.Knots, index) >= 0)
                    return knotLink;

            return null;
        }

        /// <summary>
        /// Gets the knots linked to a specific knot.
        /// </summary>
        /// <param name="knotIndex">The <see cref="SplineKnotIndex"/> of the knot.</param>
        /// <param name="linkedKnots">The output list of the knots linked to the specified knot if they exist or null if they do not exist.</param>
        /// <returns>Returns true if linked knots are found, false otherwise.</returns>
        public bool TryGetKnotLinks(SplineKnotIndex knotIndex, out IReadOnlyList<SplineKnotIndex> linkedKnots)
        {
            linkedKnots = GetKnotLinksInternal(knotIndex);
            return linkedKnots != null;
        }

        /// <summary>
        /// Gets the knots linked to a specific knot.
        /// </summary>
        /// <param name="knotIndex">The <see cref="SplineKnotIndex"/> of the knot.</param>
        /// <returns>Returns a list of knots linked to the specified knot. The specified knot is also in the list. </returns>
        public IReadOnlyList<SplineKnotIndex> GetKnotLinks(SplineKnotIndex knotIndex)
        {
            if(TryGetKnotLinks(knotIndex, out var linkedKnots))
                return linkedKnots;

            return new KnotLink { Knots = new[] { knotIndex } };
        }

        /// <summary>
        /// Clears all the links in the collection.
        /// </summary>
        public void Clear()
        {
            m_KnotsLink = new KnotLink[0];
        }

        /// <summary>
        /// Links two knots positions to each other. If you link knots that are already linked to other knots, then all of the knots link to each other.
        /// </summary>
        /// <param name="knotA">The first knot to link.</param>
        /// <param name="knotB">The first knot to link.</param>
        public void Link(SplineKnotIndex knotA, SplineKnotIndex knotB)
        {
            if (knotA.Equals(knotB))
                return;

            var originalLink = GetKnotLinksInternal(knotA);
            var targetLink = GetKnotLinksInternal(knotB);

            // If the knot was already linked to other knots, merge both shared knot
            if (originalLink != null && targetLink != null)
            {
                if (originalLink.Equals(targetLink))
                    return;

                var indices = new SplineKnotIndex[originalLink.Knots.Length + targetLink.Knots.Length];
                Array.Copy(originalLink.Knots, indices, originalLink.Knots.Length);
                Array.Copy(targetLink.Knots, 0, indices, originalLink.Knots.Length, targetLink.Knots.Length);
                originalLink.Knots = indices;
                ArrayUtility.Remove(ref m_KnotsLink, targetLink);
            }
            else if (targetLink != null)
            {
                var indices = targetLink.Knots;
                ArrayUtility.Add(ref indices, knotA);
                targetLink.Knots = indices;
            }
            else if (originalLink != null)
            {
                var indices = originalLink.Knots;
                ArrayUtility.Add(ref indices, knotB);
                originalLink.Knots = indices;
            }
            else
            {
                var newShared = new KnotLink { Knots = new[] {knotA, knotB}};
                ArrayUtility.Add(ref m_KnotsLink, newShared);
            }
        }

        /// <summary>
        /// Unlinks a knot from the knots it is linked to. This method unlinks the knot specified, but does not unlink the other knots from each other.
        /// </summary>
        /// <param name="knot">The knot to unlink.</param>
        public void Unlink(SplineKnotIndex knot)
        {
            var shared = GetKnotLinksInternal(knot);
            if (shared == null)
                return;

            var indices = shared.Knots;
            ArrayUtility.Remove(ref indices, knot);
            shared.Knots = indices;

            // Remove shared knot if empty or alone
            if (shared.Knots.Length < 2)
                ArrayUtility.Remove(ref m_KnotsLink, shared);
        }

        /// <summary>
        /// Updates the KnotLinkCollection after a spline is removed.
        /// </summary>
        /// <param name="splineIndex">The index of the removed spline.</param>
        public void SplineRemoved(int splineIndex)
        {
            List<int> indicesToRemove = new List<int>(1);
            for (var index = m_KnotsLink.Length - 1; index >= 0; --index)
            {
                var sharedKnot = m_KnotsLink[index];

                indicesToRemove.Clear();
                for (int i = 0; i < sharedKnot.Knots.Length; ++i)
                    if (sharedKnot.Knots[i].Spline == splineIndex)
                        indicesToRemove.Add(i);

                // Remove shared knot if it will become empty
                if (sharedKnot.Knots.Length - indicesToRemove.Count < 2)
                    ArrayUtility.RemoveAt(ref m_KnotsLink, index);
                else
                {
                    var indices = sharedKnot.Knots;
                    ArrayUtility.SortedRemoveAt(ref indices, indicesToRemove);
                    sharedKnot.Knots = indices;
                }

                // Decrement by one every knot of a spline higher than this one
                for (int i = 0; i < sharedKnot.Knots.Length; ++i)
                {
                    var knotIndex = sharedKnot.Knots[i];
                    if (knotIndex.Spline > splineIndex)
                        sharedKnot.Knots[i] = new SplineKnotIndex(knotIndex.Spline - 1, knotIndex.Knot);
                }
            }
        }

        /// <summary>
        /// Updates the KnotLinkCollection indices after a spline index changes.
        /// </summary>
        /// <param name="previousIndex">The previous index of that spline in the SplineContainer.</param>
        /// <param name="newIndex">The new index of that spline in the SplineContainer.</param>
        public void SplineIndexChanged(int previousIndex, int newIndex)
        {
            for (var index = m_KnotsLink.Length - 1; index >= 0; --index)
            {
                var sharedKnot = m_KnotsLink[index];
                for (int i = 0; i < sharedKnot.Knots.Length; ++i)
                {
                    var knotIndex = sharedKnot.Knots[i];
                    if (knotIndex.Spline == previousIndex)
                        sharedKnot.Knots[i] = new SplineKnotIndex(newIndex, knotIndex.Knot);
                    else if (knotIndex.Spline > previousIndex && knotIndex.Spline <= newIndex)
                        sharedKnot.Knots[i] = new SplineKnotIndex(knotIndex.Spline - 1, knotIndex.Knot);
                    else if (knotIndex.Spline < previousIndex && knotIndex.Spline >= newIndex)
                        sharedKnot.Knots[i] = new SplineKnotIndex(knotIndex.Spline + 1, knotIndex.Knot);
                }
            }
        }

        /// <summary>
        /// Updates the KnotLinkCollection indices after a knot index changes.
        /// </summary>
        /// <param name="splineIndex">The index of the spline.</param>
        /// <param name="previousKnotIndex">The previous index of the knot in the spline.</param>
        /// <param name="newKnotIndex">The new index of the knot in the spline.</param>
        public void KnotIndexChanged(int splineIndex, int previousKnotIndex, int newKnotIndex) => KnotIndexChanged(new SplineKnotIndex(splineIndex, previousKnotIndex),new SplineKnotIndex(splineIndex, newKnotIndex));

        /// <summary>
        /// Updates the KnotLinkCollection indices after a knot index changes.
        /// </summary>
        /// <param name="previousIndex">The previous SplineKnotIndex of the knot.</param>
        /// <param name="newIndex">The new SplineKnotIndex of the knot.</param>
        public void KnotIndexChanged(SplineKnotIndex previousIndex, SplineKnotIndex newIndex)
        {
            if (previousIndex.Knot > newIndex.Knot)
                previousIndex.Knot += 1;
            else
                newIndex.Knot += 1;

            //Insert the knot to shift indices
            KnotInserted(newIndex);
            //Link the 2 knots together temporary to link the new knot to the same knots as previous knot
            Link(previousIndex, newIndex);
            //Now that links has been updated, remove the previous knot
            KnotRemoved(previousIndex);
        }

        /// <summary>
        /// Updates the KnotLinkCollection indices after a knot has been removed.
        /// </summary>
        /// <param name="splineIndex">The index of the spline.</param>
        /// <param name="knotIndex">The index of the removed knot in the spline.</param>
        public void KnotRemoved(int splineIndex, int knotIndex) => KnotRemoved(new SplineKnotIndex(splineIndex, knotIndex));

        /// <summary>
        /// Updates the KnotLinkCollection indices after a knot has been removed.
        /// </summary>
        /// <param name="index">The SplineKnotIndex of the removed knot.</param>
        public void KnotRemoved(SplineKnotIndex index)
        {
            Unlink(index);
            ShiftKnotIndices(index, -1);
        }

        /// <summary>
        /// Updates the KnotLinkCollection indices after a knot has been inserted.
        /// </summary>
        /// <param name="splineIndex">The index of the spline.</param>
        /// <param name="knotIndex">The index of the inserted knot in the spline.</param>
        public void KnotInserted(int splineIndex, int knotIndex) => KnotInserted(new SplineKnotIndex(splineIndex, knotIndex));

        /// <summary>
        /// Updates the KnotLinkCollection indices after a knot has been inserted.
        /// </summary>
        /// <param name="index">The SplineKnotIndex of the inserted knot.</param>
        public void KnotInserted(SplineKnotIndex index) => ShiftKnotIndices(index, 1);

        /// <summary>
        /// Changes the indices of the KnotLinkCollection to ensure they are valid. This is mainly used when splines or
        /// knots are inserted or removed from a <see cref="SplineContainer"/>.
        /// </summary>
        /// <param name="index">The SplineKnotIndex of the knot.</param>
        /// <param name="offset">The offset to apply on other knots.</param>
        public void ShiftKnotIndices(SplineKnotIndex index, int offset)
        {
            foreach (var sharedKnot in m_KnotsLink)
            {
                for (int i = 0; i < sharedKnot.Knots.Length; ++i)
                {
                    var current = sharedKnot.Knots[i];
                    // Increment by one every knot of the same spline above or equal to the inserted knot
                    if (current.Spline == index.Spline
                        && current.Knot >= index.Knot)
                        sharedKnot.Knots[i] = new SplineKnotIndex(current.Spline, current.Knot + offset);
                }
            }
        }
    }
}