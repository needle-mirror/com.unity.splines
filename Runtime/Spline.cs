using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Describes the different types of changes that can occur to a spline.
    /// </summary>
    public enum SplineModification
    {
        /// <summary>
        /// The default modification type. This is used when no other SplineModification types apply.
        /// </summary>
        Default,
        /// <summary>
        /// The spline's <see cref="Spline.Closed"/> property was modified.
        /// </summary>
        ClosedModified,
        /// <summary>
        /// A knot was modified.
        /// </summary>
        KnotModified,
        /// <summary>
        /// A knot was inserted.
        /// </summary>
        KnotInserted,
        /// <summary>
        /// A knot was removed.
        /// </summary>
        KnotRemoved
    }
    
    /// <summary>
    /// The Spline class is a collection of <see cref="BezierKnot"/>, the closed/open state, and editing representation.
    /// </summary>
    [Serializable]
    public class Spline : ISpline, IList<BezierKnot>
    {
        const TangentMode k_DefaultTangentMode = TangentMode.Broken;
        const BezierTangent k_DefaultMainTangent = BezierTangent.Out;

        [Serializable]
        sealed class MetaData
        {
            public TangentMode Mode;
            public DistanceToInterpolation[] Length;

            public MetaData()
            {
                Mode = k_DefaultTangentMode;
                Length = null;
            }

            public MetaData(MetaData toCopy)
            {
                Mode = toCopy.Mode;
                if (toCopy.Length != null)
                {
                    Length = new DistanceToInterpolation[toCopy.Length.Length];
                    Array.Copy(toCopy.Length, Length, Length.Length);
                }
                else
                    Length = null;
            }
        }

        const int k_CurveDistanceLutResolution = 30;

        [SerializeField, Obsolete]
        SplineType m_EditModeType = SplineType.Bezier;

        [SerializeField]
        List<BezierKnot> m_Knots = new List<BezierKnot>();

        [SerializeField, HideInInspector]
        float m_Length = -1f;

        [SerializeField, HideInInspector]
        List<MetaData> m_MetaData = new List<MetaData>();

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
        /// Prefer to use <see cref="UnityEditor.Splines.EditorSplineUtility.AfterSplineWasModified"/> when
        /// working with splines in the editor.
        /// </remarks>
        [Obsolete("Deprecated, use " + nameof(Spline.Changed) + " instead.")]
        public event Action changed;

        /// <summary>
        /// Invoked any time a spline is modified.
        /// </summary>
        /// <remarks>
        /// First parameter is the target Spline that the event is raised for, second parameter is
        /// the knot index and the third parameter represents the type of change that occured.
        /// If the event does not target a specific knot, the second parameter will have the value of -1.
        /// 
        /// In the editor this callback can be invoked many times per-frame.
        /// Prefer to use <see cref="UnityEditor.Splines.EditorSplineUtility.AfterSplineWasModified"/> when
        /// working with splines in the editor.
        /// </remarks>
        /// <seealso cref="SplineModification"/>
        public static event Action<Spline, int, SplineModification> Changed;

#if UNITY_EDITOR
        internal static Action<Spline> afterSplineWasModified;
        [NonSerialized]
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

        void EnsureMetaDataValid()
        {
            while (m_MetaData.Count < m_Knots.Count)
                m_MetaData.Add(new MetaData());

            if (m_MetaData.Count > m_Knots.Count)
                m_MetaData.RemoveRange(m_Knots.Count, m_MetaData.Count - m_Knots.Count);
        }

        /// <summary>
        /// Gets the <see cref="TangentMode"/> for a knot index.
        /// </summary>
        /// <param name="index">The index to retrieve <see cref="TangentMode"/> data for.</param>
        /// <returns>A <see cref="TangentMode"/> for the knot at index.</returns>
        public TangentMode GetTangentMode(int index)
        {
            EnsureMetaDataValid();
            return m_MetaData[index].Mode;
        }

        /// <summary>
        /// Sets the <see cref="TangentMode"/> for all knots on this spline.
        /// </summary>
        /// <param name="mode">The <see cref="TangentMode"/> to apply to each knot.</param>
        public void SetTangentMode(TangentMode mode)
        {
            for(int i = 0; i < Count; ++i)
                SetTangentMode(i, mode);
        }

        /// <summary>
        /// Sets the <see cref="TangentMode"/> for a knot, and ensures that the rotation and tangent values match the
        /// behavior of the tangent mode.
        /// This function can modify the contents of the <see cref="BezierKnot"/> at the specified index.
        /// </summary>
        /// <param name="index">The index of the knot to set.</param>
        /// <param name="mode">The mode to set.</param>
        /// <param name="main">The tangent direction to align both the In and Out tangent when assigning Continuous
        /// or Mirrored tangent mode.</param>
        public void SetTangentMode(int index, TangentMode mode, BezierTangent main = k_DefaultMainTangent)
        {
            if (GetTangentMode(index) == mode)
                return;
            SetTangentModeNoNotify(index, mode);
            Changed?.Invoke(this, index, SplineModification.KnotModified);
        }

        /// <summary>
        /// Sets the <see cref="TangentMode"/> for a knot, and ensures that the rotation and tangent values match the
        /// behavior of the tangent mode. No changed callbacks will be invoked.
        /// This function can modify the contents of the <see cref="BezierKnot"/> at the specified index.
        /// </summary>
        /// <param name="index">The index of the knot to set.</param>
        /// <param name="mode">The mode to set.</param>
        /// <param name="main">The tangent direction to align both the In and Out tangent when assigning Continuous
        /// or Mirrored tangent mode.</param>
        public void SetTangentModeNoNotify(int index, TangentMode mode, BezierTangent main = k_DefaultMainTangent)
        {
            EnsureMetaDataValid();
            var previous = m_MetaData[index].Mode;
            m_MetaData[index].Mode = mode;
            var knot = m_Knots[index];

            // when coming from linear mode, re-initialize tangents with non-zero length
            if (previous == TangentMode.Linear)
            {
                switch (mode)
                {
                    case TangentMode.Broken:
                        knot.TangentIn = SplineUtility.GetExplicitLinearTangent(knot, this.Previous(index));
                        knot.TangentOut = SplineUtility.GetExplicitLinearTangent(knot, this.Next(index));
                        break;

                    case TangentMode.Continuous:
                    case TangentMode.Mirrored:
                    case TangentMode.AutoSmooth:
                        knot = SplineUtility.GetAutoSmoothKnot(knot.Position, this.Previous(index).Position, this.Next(index).Position, math.mul(knot.Rotation,math.up()));
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            // Spline tools enforce that TangentMode.{Continuous, Mirrored} are exclusively rotated. Tangents are
            // always set to +/- vec3.forward. When coming from a bezier mode where that is not enforced, we need to
            // make sure that the knot rotation is aligned to the leading tangent.
            if (previous == TangentMode.Broken && (mode == TangentMode.Continuous || mode == TangentMode.Mirrored))
            {
                var tan = math.mul(knot.Rotation, main == BezierTangent.In ? -knot.TangentIn : knot.TangentOut);
                knot.Rotation = SplineUtility.GetKnotRotation(tan, math.mul(knot.Rotation,math.up()));
            }

            m_Knots[index] = knot;
            ApplyTangentModeNoNotify(index, main);
        }

        /// <summary>
        /// Ensures that the tangents at an index conform to the <param name="mode">tangent mode</param>.
        /// </summary>
        /// <remarks>
        /// This function updates the tangents, but does not set the tangent mode.
        /// </remarks>
        /// <param name="index">The index of the knot to set tangent values for.</param>
        /// <param name="main">The tangent direction to align the In and Out tangent to when assigning Continuous
        /// or Mirrored tangent mode.</param>
        void ApplyTangentModeNoNotify(int index, BezierTangent main = k_DefaultMainTangent)
        {
            var knot = m_Knots[index];
            var mode = GetTangentMode(index);

            switch(mode)
            {
                case TangentMode.Continuous:
                    knot.TangentIn = new float3(0, 0, -math.length(knot.TangentIn));
                    knot.TangentOut = new float3(0, 0, math.length(knot.TangentOut));
                    break;

                case TangentMode.Mirrored:
                    var lead = main == BezierTangent.In ? knot.TangentIn : knot.TangentOut;
                    knot.TangentOut = new float3(0f, 0f, math.length(lead));
                    knot.TangentIn = -knot.TangentOut;
                    break;

                case TangentMode.Linear:
                    knot.TangentIn = float3.zero;
                    knot.TangentOut = float3.zero;
                    break;

                case TangentMode.AutoSmooth:
                    var tan = SplineUtility.GetCatmullRomTangent(this.Previous(index).Position, this.Next(index).Position);
                    knot.TangentOut = math.rotate(math.inverse(knot.Rotation), tan);
                    knot.TangentIn = -knot.TangentOut;
                    break;
            }

            m_Knots[index] = knot;
        }

        // todo Only Catmull Rom requires every curve to be re-evaluated when dirty.
        // Linear and cubic bezier could be more selective about dirtying cached curve lengths.
        // Important - This function also serves to enable backwards compatibility with serialized Spline instances
        // that did not have a length cache.
        void SetLengthCacheDirty()
        {
            EnsureMetaDataValid();
            m_Length = -1f;
            for (int i = 0; i < m_MetaData.Count; i++)
                m_MetaData[i].Length = null;
        }

        /// <summary>
        /// The SplineType that this spline should be presented as to the user.
        /// </summary>
        /// <remarks>
        /// Internally all splines are stored as a collection of bezier knots, and when editing converted or displayed
        /// with the handles appropriate to the editable type.
        /// </remarks>
        [Obsolete("Use GetTangentMode and SetTangentMode.")]
        public SplineType EditType
        {
            get => m_EditModeType;
            set
            {
                if (m_EditModeType == value)
                    return;
                m_EditModeType = value;
                var mode = value.GetTangentMode();
                for(int i = 0; i < Count; ++i)
                    SetTangentMode(i, mode);
                SetDirty();
                Changed?.Invoke(this, -1, SplineModification.Default);
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
                Changed?.Invoke(this, -1, SplineModification.ClosedModified);
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
        public void Insert(int index, BezierKnot knot) => Insert(index, knot, k_DefaultTangentMode);

        /// <summary>
        /// Inserts a <see cref="BezierKnot"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index to insert the new element.</param>
        /// <param name="knot">The <see cref="BezierKnot"/> to insert.</param>
        /// <param name="mode">The <see cref="TangentMode"/> to apply to this knot. Tangent modes are enforced
        /// when a knot value is set.</param>
        public void Insert(int index, BezierKnot knot, TangentMode mode)
        {
            EnsureMetaDataValid();
            m_Knots.Insert(index, knot);
            m_MetaData.Insert(index, new MetaData() { Mode = mode });
            ApplyTangentModeNoNotify(this.PreviousIndex(index));
            ApplyTangentModeNoNotify(index);
            ApplyTangentModeNoNotify(this.NextIndex(index));
            SetDirty();
            Changed?.Invoke(this, index, SplineModification.KnotInserted);
        }

        /// <summary>
        /// Removes the knot at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            EnsureMetaDataValid();
            m_Knots.RemoveAt(index);
            m_MetaData.RemoveAt(index);
            var next = Mathf.Clamp(index, 0, Count-1);
            ApplyTangentModeNoNotify(this.PreviousIndex(next));
            ApplyTangentModeNoNotify(next);
            SetDirty();
            Changed?.Invoke(this, index, SplineModification.KnotRemoved);
        }

        /// <summary>
        /// Get or set the knot at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        public BezierKnot this[int index]
        {
            get => m_Knots[index];
            set => SetKnot(index, value);
        }

        /// <summary>
        /// Sets the value of a knot at index.
        /// </summary>
        /// <param name="index">The index of the <see cref="BezierKnot"/> to set.</param>
        /// <param name="value">The <see cref="BezierKnot"/> to set.</param>
        /// <param name="main">The tangent to prioritize if the tangents are modified to conform with the
        /// <see cref="TangentMode"/> set for this knot.</param>
        public void SetKnot(int index, BezierKnot value, BezierTangent main = k_DefaultMainTangent)
        {
            SetKnotNoNotify(index, value, main);
            Changed?.Invoke(this, index, SplineModification.KnotModified);
        }

        /// <summary>
        /// Sets the value of a knot index without invoking any change callbacks.
        /// </summary>
        /// <param name="index">The index of the <see cref="BezierKnot"/> to set.</param>
        /// <param name="value">The <see cref="BezierKnot"/> to set.</param>
        /// <param name="main">The tangent to prioritize if the tangents are modified to conform with the
        /// <see cref="TangentMode"/> set for this knot.</param>
        public void SetKnotNoNotify(int index, BezierKnot value, BezierTangent main = k_DefaultMainTangent)
        {
            m_Knots[index] = value;
            ApplyTangentModeNoNotify(index, main);

            // setting knot position affects the tangents of neighbor auto-smooth (catmull-rom) knots
            int p = this.PreviousIndex(index), n = this.NextIndex(index);
            if(m_MetaData[p].Mode == TangentMode.AutoSmooth)
                ApplyTangentModeNoNotify(p, main);
            if(m_MetaData[this.NextIndex(n)].Mode == TangentMode.AutoSmooth)
                ApplyTangentModeNoNotify(n, main);
            SetDirty();
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
            Changed?.Invoke(this, -1, SplineModification.Default);
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
            EnsureMetaDataValid();
            if(m_MetaData[index].Length == null)
            {
                m_MetaData[index].Length = new DistanceToInterpolation[k_CurveDistanceLutResolution];
                CurveUtility.CalculateCurveLengths(GetCurve(index), m_MetaData[index].Length);
            }

            var cumulativeCurveLengths = m_MetaData[index].Length;
            return cumulativeCurveLengths.Length > 0 ? cumulativeCurveLengths[cumulativeCurveLengths.Length - 1].Distance : 0f;
        }

        /// <summary>
        /// Return the sum of all curve lengths, accounting for <see cref="Closed"/> state.
        /// Note that this value is not accounting for transform hierarchy. If you require length in world space use
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
            if (m_MetaData[index].Length == null)
            {
                m_MetaData[index].Length = new DistanceToInterpolation[k_CurveDistanceLutResolution];
                CurveUtility.CalculateCurveLengths(GetCurve(index), m_MetaData[index].Length);
            }

            return m_MetaData[index].Length;
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
            int originalSize = Count;
            if (newSize == originalSize)
                return;

            EnsureMetaDataValid();
            if (newSize > originalSize)
            {
                while (m_Knots.Count < newSize)
                {
                    m_Knots.Add(new BezierKnot { Rotation = quaternion.identity });
                    m_MetaData.Add(new MetaData());
                }
            }
            else if (newSize < originalSize)
            {
                m_Knots.RemoveRange(newSize, m_Knots.Count - newSize);
                m_MetaData.RemoveRange(newSize, m_Knots.Count - newSize);
            }

            SetDirty();
            Changed?.Invoke(this, -1, SplineModification.Default);
            SendSizeChangeEvent(originalSize, newSize);
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
        /// Copy the values from <paramref name="copyFrom"/> to this spline.
        /// </summary>
        /// <param name="copyFrom">The spline to copy property data from.</param>
        public void Copy(Spline copyFrom)
        {
            if (copyFrom == this)
                return;

            var previousSize = m_Knots.Count;
            m_Closed = copyFrom.Closed;
            m_Knots.Clear();
            m_Knots.AddRange(copyFrom.m_Knots);
            m_MetaData.Clear();
            for (int i = 0; i < copyFrom.m_MetaData.Count; ++i)
                m_MetaData.Add(new MetaData(copyFrom.m_MetaData[i]));

            SetDirty();
            Changed?.Invoke(this, -1, SplineModification.Default);
            SendSizeChangeEvent(previousSize, m_Knots.Count);
        }

        void SendSizeChangeEvent(int previousSize, int newSize)
        {
            var sizeDiff = newSize - previousSize;
            // Elements were removed
            if (sizeDiff < 0)
                for (int i = 0, count = math.abs(sizeDiff); i < count; ++i)
                    Changed?.Invoke(this, previousSize - 1 - i, SplineModification.KnotRemoved);

            // Elements were added
            else if (sizeDiff > 0)
                for (int i = 0; i < sizeDiff; ++i)
                    Changed?.Invoke(this, previousSize + i, SplineModification.KnotInserted);
        }

        /// <summary>
        /// Get an enumerator that iterates through the <see cref="BezierKnot"/> collection.
        /// </summary>
        /// <returns>An IEnumerator that is used to iterate the <see cref="BezierKnot"/> collection.</returns>
        public IEnumerator<BezierKnot> GetEnumerator() => m_Knots.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_Knots.GetEnumerator();

        /// <summary>
        /// Adds a knot to the spline.
        /// </summary>
        /// <param name="item">The <see cref="BezierKnot"/> to add.</param>
        public void Add(BezierKnot item) => Add(item, k_DefaultTangentMode);

        /// <summary>
        /// Adds a knot to the spline.
        /// </summary>
        /// <param name="item">The <see cref="BezierKnot"/> to add.</param>
        /// <param name="mode">The tangent mode for this knot.</param>
        public void Add(BezierKnot item, TangentMode mode)
        {
            Insert(Count, item, mode);
        }

        /// <summary>
        /// Remove all knots from the spline.
        /// </summary>
        public void Clear()
        {
            var previousSize = Count;
            m_Knots.Clear();
            m_MetaData.Clear();
            SetDirty();
            Changed?.Invoke(this, -1, SplineModification.Default);
            SendSizeChangeEvent(previousSize, 0);
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
            var index = m_Knots.IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }
    }
}