using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// To calculate a value at some distance along a spline, interpolation is required. The IInterpolator interface
    /// allows you to define how data is interpreted given a start value, end value, and normalized interpolation value
    /// (commonly referred to as 't').
    /// </summary>
    /// <typeparam name="T">
    /// The data type to interpolate.
    /// </typeparam>
    public interface IInterpolator<T>
    {
        /// <summary>
        /// Calculate a value between from and to at time interval.
        /// </summary>
        /// <param name="from">The starting value. At time = 0 this method should return an unmodified 'from' value.</param>
        /// <param name="to">The ending value. At time = 1 this method should return an unmodified 'to' value.</param>
        /// <param name="t">A percentage between 'from' and 'to'. Must be between 0 and 1.</param>
        /// <returns>A value between 'from' and 'to'.</returns>
        T Interpolate(T from, T to, float t);
    }
    
    /// <summary>
    /// Describes the unit of measurement used by <see cref="Keyframe{T}"/>.
    /// </summary>
    public enum PathIndexUnit
    {
        /// <summary>
        /// The 't' value used when interpolating is measured in game units. Values range from 0 (start of Spline) to
        /// <see cref="Spline.GetLength()"/> (end of Spline).
        /// </summary>
        Distance,
        /// <summary>
        /// The 't' value used when interpolating is normalized. Values range from 0 (start of Spline) to 1 (end of Spline). 
        /// </summary>
        Normalized,
        /// <summary>
        /// The 't' value used when interpolating is defined by knot indices and a fractional value representing the
        /// normalized interpolation between the specific knot index and the next knot.
        /// </summary>
        Knot
    }

    /// <summary>
    /// The SplineData{T} class is used to store information relative to a <see cref="Spline"/> without coupling data
    /// directly to the Spline class. SplineData can store any type of data, and provides options for how to index
    /// keyframes.
    /// </summary>
    /// <typeparam name="T">
    /// The type of data to store.
    /// </typeparam>
    [Serializable]
    public class SplineData<T>
    {
        static readonly KeyframeComparer<Keyframe<T>> m_KeyframeComparer = new KeyframeComparer<Keyframe<T>>();

        [SerializeField]
        PathIndexUnit m_IndexUnit;

        [SerializeField]
        List<Keyframe<T>> m_Keyframes = new List<Keyframe<T>>();

        // When working with IMGUI it's necessary to keep keys array consistent while a hotcontrol is active. Most
        // accessors will keep the SplineData sorted, but sometimes it's not possible.
        [NonSerialized]
        bool m_NeedsSort;

        /// <summary>
        /// Access a <see cref="Keyframe{T}"/> by index. Keyframes are sorted in ascending order by the
        /// <see cref="Keyframe{T}.Time"/> value.
        /// </summary>
        /// <param name="index">
        /// The index of the keyframe to access.
        /// </param>
        public Keyframe<T> this[int index]
        {
            get => m_Keyframes[index];
            set => SetKeyframe(index, value);
        }
        
        /// <summary>
        /// PathIndexUnit defines how SplineData will interpret 't' values when interpolating data.
        /// </summary>
        /// <seealso cref="PathIndexUnit"/>
        public PathIndexUnit PathIndexUnit
        {
            get => m_IndexUnit;
            set => m_IndexUnit = value;
        }

        /// <summary>
        /// How many keyframes the SplineData collection contains.
        /// </summary>
        public int Count => m_Keyframes.Count;

        /// <summary>
        /// Invoked any time a SplineData is modified.
        /// </summary>
        /// <remarks>
        /// In the editor this can be invoked many times per-frame.
        /// Prefer to use <see cref="UnityEditor.Splines.EditorSplineUtility.RegisterSplineDataChanged"/> when working with
        /// splines in the editor.
        /// </remarks>
        public event Action changed;

#if UNITY_EDITOR
        bool m_Dirty = false;
#endif
        
        internal static Action<SplineData<T>> afterSplineDataWasModified;
        
        /// <summary>
        /// Create a new SplineData instance.
        /// </summary>
        public SplineData() {}

        /// <summary>
        /// Create a new SplineData instance with a single value in it.
        /// </summary>
        /// <param name="init">
        /// A single value to add to the spline data at t = 0.`
        /// </param>
        public SplineData(T init)
        {
            Add(0f, init);
            SetDirty();
        }

        /// <summary>
        /// Create a new SplineData instance and initialize it with a collection of keys. Keys will be sorted and stored
        /// in ascending order by <see cref="Keyframe{T}.Time"/>.
        /// </summary>
        /// <param name="keys">
        /// A collection of Keyframes to initialize SplineData.`
        /// </param>
        public SplineData(IEnumerable<Keyframe<T>> keys)
        {
            foreach(var key in keys)
                Add(key);

            SetDirty();
        }

        void SetDirty()
        {
            changed?.Invoke();

#if UNITY_EDITOR
            if(m_Dirty)
                return;

            m_Dirty = true;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                afterSplineDataWasModified?.Invoke(this);
                m_Dirty = false;
            };
#endif
        }

        /// <summary>
        /// Append a <see cref="Keyframe{T}"/> to this collection.
        /// </summary>
        /// <param name="t">
        /// The interpolant relative to Spline. How this value is interpreted is dependent on <see cref="get_PathIndexUnit"/>.
        /// </param>
        /// <param name="data">
        /// The data to store in the created keyframe.
        /// </param>
        public void Add(float t, T data) => Add(new Keyframe<T>(t, data));
    
        /// <summary>
        /// Append a <see cref="Keyframe{T}"/> to this collection.
        /// </summary>
        /// <param name="key">
        /// The keyframe to append to the SplineData collection.
        /// </param>
        public void Add(Keyframe<T> key)
        {
            int index = m_Keyframes.BinarySearch(0, Count, key, m_KeyframeComparer);
            m_Keyframes.Insert(index < 0 ? ~index : index, key);

            SetDirty();
        }

        /// <summary>
        /// Remove a <see cref="Keyframe{T}"/> at index.
        /// </summary>
        /// <param name="index">The index to remove.</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            m_Keyframes.RemoveAt(index);

            SetDirty();
        }

        /// <summary>
        /// Remove all keyframes.
        /// </summary>
        public void Clear()
        {
            m_Keyframes.Clear();
            SetDirty();
        }

        static int Wrap(int value, int lowerBound, int upperBound)
        {
            int range_size = upperBound - lowerBound + 1;
            if(value < lowerBound)
                value += range_size * ( ( lowerBound - value ) / range_size + 1 );
            return lowerBound + ( value - lowerBound ) % range_size;
        }

        int ResolveBinaryIndex(int index, bool wrap)
        {
            index = ( index < 0 ? ~index : index ) - 1;
            if(wrap)
                index = Wrap(index, 0, Count - 1);
            return math.clamp(index, 0, Count - 1);
        }

        (int, int, float) GetIndex(float time, float splineLength, int knotCount, bool closed)
        {
            if(Count < 1)
                return default;

            SortIfNecessary();

            float splineLengthInIndexUnits = splineLength;
            if(m_IndexUnit == PathIndexUnit.Normalized)
                splineLengthInIndexUnits = 1f;
            else if(m_IndexUnit == PathIndexUnit.Knot)
                splineLengthInIndexUnits = closed ? knotCount : knotCount - 1;

            float maxKeyframeTime = m_Keyframes[m_Keyframes.Count - 1].Time;
            float maxRevolutionLength = math.ceil(maxKeyframeTime / splineLengthInIndexUnits) * splineLengthInIndexUnits;
            float maxTime = closed ? math.max(maxRevolutionLength, splineLengthInIndexUnits) : splineLengthInIndexUnits;

            if(closed)
            {
                if(time < 0f)
                    time = maxTime + time % maxTime;
                else
                    time = time % maxTime;
            }
            else
                time = math.clamp(time, 0f, maxTime);

            int index = m_Keyframes.BinarySearch(0, Count, new Keyframe<T>(time, default), m_KeyframeComparer);
            int fromIndex = ResolveBinaryIndex(index, closed);
            int toIndex = closed ? ( fromIndex + 1 ) % Count : math.clamp(fromIndex + 1, 0, Count - 1);

            float fromTime = m_Keyframes[fromIndex].Time;
            float toTime = m_Keyframes[toIndex].Time;

            if(fromIndex > toIndex)
                toTime += maxTime;

            if(time < fromTime && closed)
                time += maxTime;

            if(fromTime == toTime)
                return ( fromIndex, toIndex, fromTime );

            return ( fromIndex, toIndex, math.abs(math.max(0f, time - fromTime) / ( toTime - fromTime )) );
        }

        /// <summary>
        /// Calculate an interpolated value at a given 't' along a spline.
        /// </summary>
        /// <param name="spline">The Spline to interpolate.</param>
        /// <param name="t">The interpolator value. How this is interpreted is defined by <see cref="PathIndexUnit"/>.</param>
        /// <param name="indexUnit">The <see cref="PathIndexUnit"/> that <paramref name="t"/> is represented as.</param>
        /// <param name="interpolator">The <see cref="IInterpolator{T}"/> to use. A collection of commonly used
        /// interpolators are available in the <see cref="UnityEngine.Splines.Interpolators"/> namespace.</param>
        /// <typeparam name="TInterpolator">The IInterpolator type.</typeparam>
        /// <typeparam name="TSpline">The Spline type.</typeparam>
        /// <returns>An interpolated value.</returns>
        public T Evaluate<TSpline, TInterpolator>(TSpline spline, float t, PathIndexUnit indexUnit, TInterpolator interpolator) 
            where TSpline : ISpline
            where TInterpolator : IInterpolator<T>
        {
            if(indexUnit == m_IndexUnit)
                return Evaluate(spline, t, interpolator);
            
            return Evaluate(spline, SplineUtility.GetConvertedTime(spline, t, indexUnit, m_IndexUnit), interpolator);
        }

        /// <summary>
        /// Calculate an interpolated value at a given 't' along a spline.
        /// </summary>
        /// <param name="spline">The Spline to interpolate.</param>
        /// <param name="t">The interpolator value. How this is interpreted is defined by <see cref="PathIndexUnit"/>.</param>
        /// <param name="interpolator">The <see cref="IInterpolator{T}"/> to use. A collection of commonly used
        /// interpolators are available in the <see cref="UnityEngine.Splines.Interpolators"/> namespace.</param>
        /// <typeparam name="TInterpolator">The IInterpolator type.</typeparam>
        /// <typeparam name="TSpline">The Spline type.</typeparam>
        /// <returns>An interpolated value.</returns>
        public T Evaluate<TSpline, TInterpolator>(TSpline spline, float t, TInterpolator interpolator) 
            where TSpline : ISpline 
            where TInterpolator : IInterpolator<T>
        {
            var knotCount = spline.KnotCount;
            if(knotCount < 1)
                return default;

            var indices = GetIndex(t, spline.GetLength(), knotCount, spline.Closed);
            Keyframe<T> a = m_Keyframes[indices.Item1];
            Keyframe<T> b = m_Keyframes[indices.Item2];
            return interpolator.Interpolate(a.Value, b.Value, indices.Item3);
        }

        /// <summary>
        /// Set the data for a <see cref="Keyframe{T}"/> at an index.
        /// </summary>
        /// <param name="index">The Keyframe index.</param>
        /// <param name="value">The value to set.</param>
        public void SetKeyframe(int index, Keyframe<T> value)
        {
            if(index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index");
            RemoveAt(index);
            Add(value);
            SetDirty();
        }

        public void SetKeyframeNoSort(int index, Keyframe<T> value)
        {
            if(index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index");

            // could optimize this by storing affected range
            m_NeedsSort = true;
            m_Keyframes[index] = value;
        }

        public void SortIfNecessary()
        {
            if(!m_NeedsSort)
                return;
            m_NeedsSort = false;
            m_Keyframes.Sort();
            SetDirty();
        }
        
        /// <summary>
        /// Given a time value using a certain PathIndexUnit type, calculate the normalized time value regarding a specific spline.
        /// </summary>
        /// <param name="spline">The Spline to use for the conversion, this is necessary to compute Normalized and Distance PathIndexUnits.</param>
        /// <param name="time">The time to normalize in the original PathIndexUnit.</param>>
        /// <returns>The normalized time.</returns>
        public float GetNormalizedTime(NativeSpline spline, float time)
        {
            return SplineUtility.GetNormalizedTime(spline, time, m_IndexUnit);
        }
        
    }
}
