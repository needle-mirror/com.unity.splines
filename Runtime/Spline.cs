using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// The Spline class is a collection of <see cref="BezierKnot"/>, the closed/open state, and editing representation.
    /// </summary>
    [Serializable]
    public class Spline : ISpline, IList<BezierKnot>
    {
        const int k_CurveDistanceLutResolution = 30;

        [SerializeField]
        SplineType m_EditModeType = SplineType.Bezier;

        [SerializeField]
        List<BezierKnot> m_Knots = new List<BezierKnot>();

        [SerializeField]
        List<DistanceToInterpolation[]> m_LengthsLookupTable = new List<DistanceToInterpolation[]>();

        [SerializeField]
        float m_Length = -1f;

        [SerializeField]
        bool m_Closed;

        /// <summary>
        /// Return the number of knots.
        /// </summary>
        public int Count => m_Knots.Count;

        /// <summary>
        /// Returns true if this Spline is read-only, false if it is mutable.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Invoked in the editor any time a spline property is modified.
        /// </summary>
        /// <remarks>
        /// In the editor this can be invoked many times per-frame.
        /// Prefer to use <see cref="UnityEditor.Splines.EditorSplineUtility.afterSplineDataWasModified"/> when
        /// working with splines in the editor.
        /// </remarks>
        public event Action changed;

#if UNITY_EDITOR
        internal static Action<Spline> afterSplineWasModified;
        [NonSerialized] //In the editor, this seemed to be surviving domain reloads
        bool m_Dirty;
#endif

        internal void SetDirty()
        {
            SetLengthCacheDirty();
            changed?.Invoke();
            OnSplineChanged();

#if UNITY_EDITOR
            if (m_Dirty)
                return;

            m_Dirty = true;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                afterSplineWasModified?.Invoke(this);
                m_Dirty = false;
            };
#endif
        }

        /// <summary>
        /// Invoked any time a spline property is modified.
        /// </summary>
        /// <remarks>
        /// In the editor this can be invoked many times per-frame.
        /// Prefer to use <see cref="UnityEditor.Splines.EditorSplineUtility.afterSplineWasModified"/> when working
        /// with splines in the editor.
        /// </remarks>
        protected virtual void OnSplineChanged()
        {
        }

        // todo Remove this and refactor m_Knots to store a struct with knot+cached data
        void EnsureCurveLengthCacheValid()
        {
            if (m_LengthsLookupTable.Count != m_Knots.Count)
            {
                m_LengthsLookupTable.Clear();
                m_LengthsLookupTable.Capacity = m_Knots.Count;
                for (int i = 0; i < m_Knots.Count; i++)
                    m_LengthsLookupTable.Add(null);
            }
        }

        // todo Only Catmull Rom requires every curve to be re-evaluated when dirty.
        // Linear and cubic bezier could be more selective about dirtying cached curve lengths.
        // Important - This function also serves to enable backwards compatibility with serialized Spline instances
        // that did not have a length cache.
        void SetLengthCacheDirty()
        {
            EnsureCurveLengthCacheValid();
            m_Length = -1f;
            for (int i = 0; i < m_Knots.Count; i++)
                m_LengthsLookupTable[i] = null;
        }

        /// <summary>
        /// The SplineType that this spline should be presented as to the user.
        /// </summary>
        /// <remarks>
        /// Internally all splines are stored as a collection of bezier knots, and when editing converted or displayed
        /// with the handles appropriate to the editable type.
        /// </remarks>
        public SplineType EditType
        {
            get => m_EditModeType;
            set
            {
                if (m_EditModeType == value)
                    return;

                m_EditModeType = value;
                SetDirty();
            }
        }

        /// <summary>
        /// A collection of <see cref="BezierKnot"/>.
        /// </summary>
        public IEnumerable<BezierKnot> Knots => m_Knots;

        /// <summary>
        /// Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).
        /// </summary>
        public bool Closed
        {
            get => m_Closed;
            set
            {
                if (m_Closed == value)
                    return;
                m_Closed = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Return the first index of an element matching item.
        /// </summary>
        /// <param name="item">The knot to locate.</param>
        /// <returns>The zero-based index of the knot, or -1 if not found.</returns>
        public int IndexOf(BezierKnot item) => m_Knots.IndexOf(item);

        /// <summary>
        /// Insert a <see cref="BezierKnot"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index to insert the new element.</param>
        /// <param name="knot">The <see cref="BezierKnot"/> to insert.</param>
        public void Insert(int index, BezierKnot knot)
        {
            m_Knots.Insert(index, knot);
            m_LengthsLookupTable.Insert(index, null);
            SetDirty();
        }

        /// <summary>
        /// Removes the knot at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            m_Knots.RemoveAt(index);
            m_LengthsLookupTable.RemoveAt(index);
            SetDirty();
        }

        /// <summary>
        /// Get or set the knot at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        public BezierKnot this[int index]
        {
            get => m_Knots[index];
            set
            {
                m_Knots[index] = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Default constructor creates a spline with no knots, not closed.
        /// </summary>
        public Spline() { }

        /// <summary>
        /// Create a spline with a pre-allocated knot capacity.
        /// </summary>
        /// <param name="knotCapacity">The capacity of the knot collection.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        public Spline(int knotCapacity, bool closed = false)
        {
            m_Knots = new List<BezierKnot>(knotCapacity);
            m_Closed = closed;
        }

        /// <summary>
        /// Create a spline from a collection of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="knots">A collection of <see cref="BezierKnot"/>.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        public Spline(IEnumerable<BezierKnot> knots, bool closed = false)
        {
            m_Knots = knots.ToList();
            m_Closed = closed;
            SetDirty();
        }

        /// <summary>
        /// Get a <see cref="BezierCurve"/> from a knot index.
        /// </summary>
        /// <param name="index">The knot index that serves as the first control point for this curve.</param>
        /// <returns>
        /// A <see cref="BezierCurve"/> formed by the knot at index and the next knot.
        /// </returns>
        public BezierCurve GetCurve(int index)
        {
            int next = m_Closed ? (index + 1) % m_Knots.Count : math.min(index + 1, m_Knots.Count - 1);
            return new BezierCurve(m_Knots[index], m_Knots[next]);
        }

        /// <summary>
        /// Return the length of a curve.
        /// </summary>
        /// <param name="index"></param>
        /// <seealso cref="Warmup"/>
        /// <seealso cref="GetLength"/>
        /// <returns></returns>
        public float GetCurveLength(int index)
        {
            EnsureCurveLengthCacheValid();
            if(m_LengthsLookupTable[index] == null)
            {
                m_LengthsLookupTable[index] = new DistanceToInterpolation[k_CurveDistanceLutResolution];
                CurveUtility.CalculateCurveLengths(GetCurve(index), m_LengthsLookupTable[index]);
            }

            var cumulativeCurveLengths = m_LengthsLookupTable[index];
            return cumulativeCurveLengths.Length > 0 ? cumulativeCurveLengths[cumulativeCurveLengths.Length - 1].Distance : 0f;
        }

        /// <summary>
        /// Return the sum of all curve lengths, accounting for <see cref="Closed"/> state.
        /// Note that this value is not accounting for transform hierarchy. If you require length in world space use
        /// <see cref="SplineUtility.CalculateLength"/>.
        /// </summary>
        /// <remarks>
        /// This value is cached. It is recommended to call this once in a non-performance critical path to ensure that
        /// the cache is valid.
        /// </remarks>
        /// <seealso cref="Warmup"/>
        /// <seealso cref="GetCurveLength"/>
        /// <returns>
        /// Returns the sum length of all curves composing this spline, accounting for closed state.
        /// </returns>
        public float GetLength()
        {
            if (m_Length < 0f)
            {
                m_Length = 0f;
                for (int i = 0, c = Closed ? Count : Count - 1; i < c; ++i)
                    m_Length += GetCurveLength(i);
            }

            return m_Length;
        }

        DistanceToInterpolation[] GetCurveDistanceLut(int index)
        {
            if (m_LengthsLookupTable[index] == null)
            {
                m_LengthsLookupTable[index] = new DistanceToInterpolation[k_CurveDistanceLutResolution];
                CurveUtility.CalculateCurveLengths(GetCurve(index), m_LengthsLookupTable[index]);
            }

            return m_LengthsLookupTable[index];
        }

        /// <summary>
        /// Return the normalized interpolation (t) corresponding to a distance on a <see cref="BezierCurve"/>.
        /// </summary>
        /// <param name="curveIndex"> The zero-based index of the curve.</param>
        /// <param name="curveDistance">The curve-relative distance to convert to an interpolation ratio (also referred to as 't').</param>
        /// <returns>  The normalized interpolation ratio associated to distance on the designated curve.</returns>
        public float GetCurveInterpolation(int curveIndex, float curveDistance)
            => CurveUtility.GetDistanceToInterpolation(GetCurveDistanceLut(curveIndex), curveDistance);

        /// <summary>
        /// Ensure that all caches contain valid data. Call this to avoid unexpected performance costs when accessing
        /// spline data. Caches remain valid until any part of the spline state is modified.
        /// </summary>
        public void Warmup()
        {
            var _ = GetLength();
        }

        /// <summary>
        /// Change the size of the <see cref="BezierKnot"/> list.
        /// </summary>
        /// <param name="newSize">The new size of the knots collection.</param>
        public void Resize(int newSize)
        {
            int count = Count;
            if (newSize == count)
                return;

            if (newSize > count)
            {
                while (m_Knots.Count < newSize)
                {
                    m_Knots.Add(new BezierKnot { Rotation = quaternion.identity });
                    m_LengthsLookupTable.Add(null);
                }
            }
            else if (newSize < count)
            {
                m_Knots.RemoveRange(newSize, m_Knots.Count - newSize);
                m_LengthsLookupTable.RemoveRange(newSize, m_Knots.Count - newSize);
            }

            SetDirty();
        }

        /// <summary>
        /// Create an array of spline knots.
        /// </summary>
        /// <returns>Return a new array copy of the knots collection.</returns>
        public BezierKnot[] ToArray()
        {
            return m_Knots.ToArray();
        }

        /// <summary>
        /// Copy the values from <paramref name="toCopy"/> to this spline.
        /// </summary>
        /// <param name="toCopy">The Spline to copy property data from.</param>
        public void Copy(Spline toCopy)
        {
            if (toCopy == this)
                return;

            m_EditModeType = toCopy.m_EditModeType;
            m_Closed = toCopy.Closed;
            m_Knots.Clear();
            m_Knots.AddRange(toCopy.m_Knots);
            m_LengthsLookupTable.AddRange(toCopy.m_LengthsLookupTable);
            SetDirty();
        }

        /// <summary>
        /// Get an enumerator that iterates through the <see cref="BezierKnot"/> collection.
        /// </summary>
        /// <returns>An IEnumerator that is used to iterate the <see cref="BezierKnot"/> collection.</returns>
        public IEnumerator<BezierKnot> GetEnumerator() => m_Knots.GetEnumerator();

        /// <inheritdoc cref="GetEnumerator"/>
        IEnumerator IEnumerable.GetEnumerator() => m_Knots.GetEnumerator();

        /// <summary>
        /// Adds a knot to the spline.
        /// </summary>
        /// <param name="item">The <see cref="BezierKnot"/> to add.</param>
        public void Add(BezierKnot item)
        {
            m_Knots.Add(item);
            SetDirty();
        }

        /// <summary>
        /// Remove all knots from the spline.
        /// </summary>
        public void Clear()
        {
            m_Knots.Clear();
            SetDirty();
        }

        /// <summary>
        /// Return true if a knot is present in the spline.
        /// </summary>
        /// <param name="item">The <see cref="BezierKnot"/> to locate.</param>
        /// <returns>Returns true if the knot is found, false if it is not present.</returns>
        public bool Contains(BezierKnot item) => m_Knots.Contains(item);

        /// <summary>
        /// Copies the contents of the knot list to an array starting at an index.
        /// </summary>
        /// <param name="array">The destination array to place the copied item in.</param>
        /// <param name="arrayIndex">The zero-based index to copy.</param>
        public void CopyTo(BezierKnot[] array, int arrayIndex) => m_Knots.CopyTo(array, arrayIndex);

        /// <summary>
        /// Removes the first matching knot.
        /// </summary>
        /// <param name="item">The <see cref="BezierKnot"/> to locate and remove.</param>
        /// <returns>Returns true if a matching item was found and removed, false if no match was discovered.</returns>
        public bool Remove(BezierKnot item)
        {
            if (m_Knots.Remove(item))
            {
                SetDirty();
                return true;
            }

            return false;
        }
    }
}
