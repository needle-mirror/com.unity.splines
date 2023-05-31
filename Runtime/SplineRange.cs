using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Describes the direction that a <see cref="SplineRange"/> interpolates. Use <see cref="SplineSlice{T}"/> and
    /// <see cref="SplineRange"/> to create paths that interpolate along a <see cref="Spline"/> in either a forward
    /// or backward direction.
    /// </summary>
    /// <seealso cref="SplineSlice{T}"/>
    /// <seealso cref="SplineRange"/>
    public enum SliceDirection
    {
        /// <summary>
        /// The <see cref="SplineSlice{T}"/> interpolates along the direction of the referenced spline.
        /// </summary>
        Forward,
        /// <summary>
        /// The <see cref="SplineSlice{T}"/> interpolates in the reverse direction of the referenced spline.
        /// </summary>
        Backward
    }

    /// <summary>
    /// Describes a subset of knot indices in a <see cref="Spline"/>. The range might iterate in either the
    /// forward or backward direction.
    /// </summary>
    [Serializable]
    public struct SplineRange : IEnumerable<int>
    {
        [SerializeField]
        int m_Start;

        [SerializeField]
        int m_Count;

        [SerializeField]
        SliceDirection m_Direction;

        /// <summary>
        /// The inclusive first index, starting at 0.
        /// </summary>
        public int Start
        {
            get => m_Start;
            set => m_Start = value;
        }

        /// <summary>
        /// The inclusive end index of this range.
        /// </summary>
        public int End => this[Count - 1];

        /// <summary>
        /// Returns the number of indices.
        /// </summary>
        public int Count
        {
            get => m_Count;
            set => m_Count = math.max(value, 0);
        }

        /// <summary>
        /// The direction that this range interpolates. <see cref="SliceDirection.Forward"/> increments
        /// the knot index when it iterates, whereas <see cref="SliceDirection.Backward"/> decrements this index.
        /// </summary>
        public SliceDirection Direction
        {
            get => m_Direction;
            set => m_Direction = value;
        }

        /// <summary>
        /// Creates a new <see cref="SplineRange"/> from a start index and count.
        /// </summary>
        /// <param name="start">The inclusive first index of a range.</param>
        /// <param name="count">The number of elements this range encompasses. This value might be negative,
        /// which is shorthand to call the constructor with an explicit <see cref="SliceDirection"/> parameter.
        /// </param>
        public SplineRange(int start, int count) : this(start, count,
            count < 0 ? SliceDirection.Backward : SliceDirection.Forward)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SplineRange"/> from a start index and count.
        /// </summary>
        /// <param name="start">The inclusive first index of a range.</param>
        /// <param name="count">The number of elements this range encompasses.</param>
        /// <param name="direction">Whether when iterating this range it is incrementing from start to start + count, or
        /// decrementing from start to start - count.
        /// </param>
        public SplineRange(int start, int count, SliceDirection direction)
        {
            m_Start = start;
            m_Count = math.abs(count);
            m_Direction = direction;
        }

        /// <summary>
        /// Get or set the <see cref="Spline"/> knot index at an index <paramref name="index"/>.
        /// This indexer allows you to write a for loop to iterate through a range without needing to know in which
        /// direction the range is iterating.
        /// <code>
        /// // Create a new range into an existing Spline starting at knot 5, and interpolating the span of 3 knots.
        /// // range[0,1,2] will map to { 6, 5, 4 } respectively.
        /// var range = new SplineRange(6, 3, SplineDirection.Backward);
        /// </code>
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        public int this[int index] => Direction == SliceDirection.Backward ? m_Start - index : m_Start + index;

        /// <summary>
        /// Get an enumerator that iterates through the index collection. Note that this will either increment or
        /// decrement indices depending on the value of the <see cref="Direction"/> property.
        /// </summary>
        /// <returns>An IEnumerator that is used to iterate the collection.</returns>
        public IEnumerator<int> GetEnumerator() => new SplineRangeEnumerator(this);

        /// <summary>
        /// Gets an enumerator that iterates through the index collection. It either increments or
        /// decrements indices depending on the value of the <see cref="Direction"/> property.
        /// </summary>
        /// <returns>Returns an IEnumerator that is used to iterate the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// A struct for iterating a <see cref="SplineRange"/>.
        /// </summary>
        public struct SplineRangeEnumerator : IEnumerator<int>
        {
            int m_Index, m_Start, m_End, m_Count;
            bool m_Reverse;

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns></returns>
            public bool MoveNext() => ++m_Index < m_Count;

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset() => m_Index = -1;

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            public int Current => m_Reverse ? m_End - m_Index : m_Start + m_Index;

            object IEnumerator.Current => Current;

            /// <summary>
            /// Constructor for an IEnumerator of a <see cref="SplineRange"/>.
            /// </summary>
            /// <param name="range">The <see cref="SplineRange"/> to iterate.</param>
            public SplineRangeEnumerator(SplineRange range)
            {
                m_Index = -1;
                m_Reverse = range.Direction == SliceDirection.Backward;
                int a = range.Start,
                    b = m_Reverse ? range.Start - range.Count : range.Start + range.Count;
                m_Start = math.min(a, b);
                m_End = math.max(a, b);
                m_Count = range.Count;
            }

            /// <summary>
            /// IDisposable implementation. SplineSliceEnumerator does not allocate any resources.
            /// </summary>
            public void Dispose() { }
        }

        /// <summary>
        /// Returns a string summary of this range.
        /// </summary>
        /// <returns>Returns a string summary of this range.</returns>
        public override string ToString() => $"{{{Start}..{End}}}";
    }
}
