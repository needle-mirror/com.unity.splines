using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="from">The starting value. At t = 0 this method should return an unmodified 'from' value.</param>
        /// <param name="to">The ending value. At t = 1 this method should return an unmodified 'to' value.</param>
        /// <param name="t">A percentage between 'from' and 'to'. Must be between 0 and 1.</param>
        /// <returns>A value between 'from' and 'to'.</returns>
        T Interpolate(T from, T to, float t);
    }

    /// <summary>
    /// Describes the unit of measurement used by <see cref="DataPoint{T}"/>.
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

    // Used internally to try preserving index positioning for SplineData embedded in Spline class. It is not very
    // useful outside of this specific context, which is why it is not public. Additionally, in the future I'd like to
    // explore passing the previous and new spline length to enable better preservation of distance and normalized
    // indices.
    interface ISplineModificationHandler
    {
        void OnSplineModified(SplineModificationData info);
    }

    /// <summary>
    /// The SplineData{T} class is used to store information relative to a <see cref="Spline"/> without coupling data
    /// directly to the Spline class. SplineData can store any type of data, and provides options for how to index
    /// DataPoints.
    /// </summary>
    /// <typeparam name="T"> The type of data to store. </typeparam>
    [Serializable]
    public class SplineData<T> : IEnumerable<DataPoint<T>>, ISplineModificationHandler
    {
        static readonly DataPointComparer<DataPoint<T>> k_DataPointComparer = new DataPointComparer<DataPoint<T>>();

        [SerializeField]
        PathIndexUnit m_IndexUnit = PathIndexUnit.Knot;

        [SerializeField]
        T m_DefaultValue;

        [SerializeField]
        List<DataPoint<T>> m_DataPoints = new List<DataPoint<T>>();

        // When working with IMGUI it's necessary to keep keys array consistent while a hotcontrol is active. Most
        // accessors will keep the SplineData sorted, but sometimes it's not possible.
        [NonSerialized]
        bool m_NeedsSort;

        /// <summary>
        /// Access a <see cref="DataPoint{T}"/> by index. DataPoints are sorted in ascending order by the
        /// <see cref="DataPoint{DataType}.Index"/> value.
        /// </summary>
        /// <param name="index">
        /// The index of the DataPoint to access.
        /// </param>
        public DataPoint<T> this[int index]
        {
            get => m_DataPoints[index];
            set => SetDataPoint(index, value);
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
        /// Default value to use when a new DataPoint is automatically added.
        /// </summary>
        /// <seealso cref="PathIndexUnit"/>
        public T DefaultValue
        {
            get => m_DefaultValue;
            set => m_DefaultValue = value;
        }

        /// <summary>
        /// How many data points the SplineData collection contains.
        /// </summary>
        public int Count => m_DataPoints.Count;

        /// <summary>
        /// The DataPoint Indexes of the current SplineData.
        /// </summary>
        public IEnumerable<float> Indexes => m_DataPoints.Select(dp => dp.Index);

        /// <summary>
        /// Invoked any time a SplineData is modified.
        /// </summary>
        /// <remarks>
        /// In the editor this can be invoked many times per-frame.
        /// Prefer to use <see cref="UnityEditor.Splines.EditorSplineUtility.RegisterSplineDataChanged"/> when working with
        /// splines in the editor.
        /// </remarks>
        [Obsolete("Use Changed instead.", false)]
        public event Action changed;
        /// <summary>
        /// Invoked any time a SplineData is modified.
        /// </summary>
        /// <remarks>
        /// In the editor this can be invoked many times per-frame.
        /// Prefer to use <see cref="UnityEditor.Splines.EditorSplineUtility.RegisterSplineDataChanged"/> when working with
        /// splines in the editor.
        /// </remarks>
        public event Action Changed;

#if UNITY_EDITOR
        bool m_Dirty = false;
        internal static Action<SplineData<T>> afterSplineDataWasModified;
#endif

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
        /// Create a new SplineData instance and initialize it with a collection of data points. DataPoints will be sorted and stored
        /// in ascending order by <see cref="DataPoint{DataType}.Index"/>.
        /// </summary>
        /// <param name="dataPoints">
        /// A collection of DataPoints to initialize SplineData.`
        /// </param>
        public SplineData(IEnumerable<DataPoint<T>> dataPoints)
        {
            foreach(var dataPoint in dataPoints)
                Add(dataPoint);
            SetDirty();
        }

        void SetDirty()
        {
            changed?.Invoke();
            Changed?.Invoke();

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
        /// Append a <see cref="DataPoint{T}"/> to this collection.
        /// </summary>
        /// <param name="t">
        /// The interpolant relative to Spline. How this value is interpreted is dependent on <see cref="get_PathIndexUnit"/>.
        /// </param>
        /// <param name="data">
        /// The data to store in the created data point.
        /// </param>
        public void Add(float t, T data) => Add(new DataPoint<T>(t, data));

        /// <summary>
        /// Append a <see cref="DataPoint{T}"/> to this collection.
        /// </summary>
        /// <param name="dataPoint">
        /// The data point to append to the SplineData collection.
        /// </param>
        /// <returns>
        /// The index of the inserted dataPoint.
        /// </returns>
        public int Add(DataPoint<T> dataPoint)
        {
            int index = m_DataPoints.BinarySearch(0, Count, dataPoint, k_DataPointComparer);

            index = index < 0 ? ~index : index;
            m_DataPoints.Insert(index, dataPoint);

            SetDirty();
            return index;
        }

        /// <summary>
        /// Append a <see cref="DataPoint{T}"/> with default value to this collection.
        /// </summary>
        /// <param name="t">
        /// The interpolant relative to Spline. How this value is interpreted is dependent on <see cref="get_PathIndexUnit"/>.
        /// </param>
        /// <param name="useDefaultValue">
        /// If true will use <see cref="m_DefaultValue"/> to set the value, otherwise will interpolate the value regarding the closest DataPoints.
        /// </param>
        /// <returns>
        /// The index of the inserted dataPoint.
        /// </returns>
        public int AddDataPointWithDefaultValue(float t, bool useDefaultValue = false)
        {
            var dataPoint = new DataPoint<T>(t, m_DefaultValue);
            if(Count == 0 || useDefaultValue)
                return Add(dataPoint);

            if(Count == 1)
            {
                dataPoint.Value = m_DataPoints[0].Value;
                return Add(dataPoint);
            }

            int index = m_DataPoints.BinarySearch(0, Count, dataPoint, k_DataPointComparer);
            index = index < 0 ? ~index : index;

            dataPoint.Value = index == 0 ? m_DataPoints[0].Value : m_DataPoints[index-1].Value;
            m_DataPoints.Insert(index, dataPoint);
            SetDirty();

            return index;
        }

        /// <summary>
        /// Remove a <see cref="DataPoint{T}"/> at index.
        /// </summary>
        /// <param name="index">The index to remove.</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            m_DataPoints.RemoveAt(index);

            SetDirty();
        }

        /// <summary>
        /// Remove a <see cref="DataPoint{T}"/> from this collection, if one exists.
        /// </summary>
        /// <param name="t">
        /// The interpolant relative to Spline. How this value is interpreted is dependent on <see cref="get_PathIndexUnit"/>.
        /// </param>
        /// <returns>
        /// True is deleted, false otherwise.
        /// </returns>
        public bool RemoveDataPoint(float t)
        {
            var removed = m_DataPoints.Remove(m_DataPoints.FirstOrDefault(point => Mathf.Approximately(point.Index, t)));
            if(removed)
                SetDirty();
            return removed;
        }

        /// <summary>
        /// Move a <see cref="DataPoint{T}"/> (if it exists) from this collection, from one index to the another.
        /// </summary>
        /// <param name="index">The index of the  <see cref="DataPoint{T}"/> to move. This is the index into the collection, not the PathIndexUnit.Knot.</param>
        /// <param name="newIndex">The new index (<see cref="UnityEngine.Splines.PathIndexUnit.Knot"/>) for this <see cref="DataPoint{T}"/>.</param>
        /// <returns>The index of the modified <see cref="DataPoint{T}"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified <paramref name="index"/> is out of range.</exception>
        public int MoveDataPoint(int index, float newIndex)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var dataPoint = m_DataPoints[index];
            if(Mathf.Approximately(newIndex, dataPoint.Index))
                return index;

            RemoveAt(index);
            dataPoint.Index = newIndex;
            int newRealIndex = Add(dataPoint);

            return newRealIndex;
        }

        /// <summary>
        /// Remove all data points.
        /// </summary>
        public void Clear()
        {
            m_DataPoints.Clear();
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

        (int, int, float) GetIndex(float t, float splineLength, int knotCount, bool closed)
        {
            if(Count < 1)
                return default;

            SortIfNecessary();

            float splineLengthInIndexUnits = splineLength;
            if(m_IndexUnit == PathIndexUnit.Normalized)
                splineLengthInIndexUnits = 1f;
            else if(m_IndexUnit == PathIndexUnit.Knot)
                splineLengthInIndexUnits = closed ? knotCount : knotCount - 1;

            float maxDataPointTime = m_DataPoints[m_DataPoints.Count - 1].Index;
            float maxRevolutionLength = math.ceil(maxDataPointTime / splineLengthInIndexUnits) * splineLengthInIndexUnits;
            float maxTime = closed ? math.max(maxRevolutionLength, splineLengthInIndexUnits) : splineLengthInIndexUnits;

            if(closed)
            {
                if(t < 0f)
                    t = maxTime + t % maxTime;
                else
                    t = t % maxTime;
            }
            else
                t = math.clamp(t, 0f, maxTime);

            int index = m_DataPoints.BinarySearch(0, Count, new DataPoint<T>(t, default), k_DataPointComparer);
            int fromIndex = ResolveBinaryIndex(index, closed);
            int toIndex = closed ? ( fromIndex + 1 ) % Count : math.clamp(fromIndex + 1, 0, Count - 1);

            float fromTime = m_DataPoints[fromIndex].Index;
            float toTime = m_DataPoints[toIndex].Index;

            if(fromIndex > toIndex)
                toTime += maxTime;

            if(t < fromTime && closed)
                t += maxTime;

            if(fromTime == toTime)
                return ( fromIndex, toIndex, fromTime );

            return ( fromIndex, toIndex, math.abs(math.max(0f, t - fromTime) / ( toTime - fromTime )) );
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

            return Evaluate(spline, SplineUtility.ConvertIndexUnit(spline, t, indexUnit, m_IndexUnit), interpolator);
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
            var knotCount = spline.Count;
            if(knotCount < 1 || m_DataPoints.Count == 0)
                return default;

            var indices = GetIndex(t, spline.GetLength(), knotCount, spline.Closed);
            DataPoint<T> a = m_DataPoints[indices.Item1];
            DataPoint<T> b = m_DataPoints[indices.Item2];
            return interpolator.Interpolate(a.Value, b.Value, indices.Item3);
        }

        /// <summary>
        /// Set the data for a <see cref="DataPoint{T}"/> at an index.
        /// </summary>
        /// <param name="index">The DataPoint index.</param>
        /// <param name="value">The value to set.</param>
        /// <remarks>
        /// Using this method will search the DataPoint list and invoke the <see cref="Changed"/>
        /// callback every time. This may be inconvenient when setting multiple DataPoints during the same frame.
        /// In this case, consider calling <see cref="SetDataPointNoSort"/> for each DataPoint, followed by
        /// a single call to <see cref="SortIfNecessary"/>. Note that the call to <see cref="SortIfNecessary"/> is
        /// optional and can be omitted if DataPoint sorting is not required and the <see cref="Changed"/> callback
        /// should not be invoked.
        /// </remarks>
        public void SetDataPoint(int index, DataPoint<T> value)
        {
            if(index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index");
            RemoveAt(index);
            Add(value);
            SetDirty();
        }

        /// <summary>
        /// Set the data for a <see cref="DataPoint{T}"/> at an index.
        /// </summary>
        /// <param name="index">The DataPoint index.</param>
        /// <param name="value">The value to set.</param>
        /// <remarks>
        /// Use this method as an altenative to <see cref="SetDataPoint"/> when manual control
        /// over DataPoint sorting and the <see cref="Changed"/> callback is required.
        /// See also <see cref="SortIfNecessary"/>.
        /// </remarks>
        public void SetDataPointNoSort(int index, DataPoint<T> value)
        {
            if(index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index");

            // could optimize this by storing affected range
            m_NeedsSort = true;
            m_DataPoints[index] = value;
        }

        /// <summary>
        /// Triggers sorting of the <see cref="DataPoint{T}"/> list if the data is dirty.
        /// </summary>
        /// <remarks>
        /// Call this after a single or series of calls to <see cref="SetDataPointNoSort"/>.
        /// This will trigger DataPoint sort and invoke the <see cref="Changed"/> callback.
        /// This method has two main use cases: to prevent frequent <see cref="Changed"/> callback
        /// calls within the same frame and to reduce multiple DataPoints list searches
        /// to a single sort in performance critical paths.
        /// </remarks>
        public void SortIfNecessary()
        {
            if(!m_NeedsSort)
                return;
            m_NeedsSort = false;
            m_DataPoints.Sort();
            SetDirty();
        }

        internal void ForceSort()
        {
            m_NeedsSort = true;
            SortIfNecessary();
        }

        /// <summary>
        /// Given a spline and a target PathIndex Unit, convert the SplineData to a new PathIndexUnit without changing the final positions on the Spline.
        /// </summary>
        /// <typeparam name="TSplineType">The Spline type.</typeparam>
        /// <param name="spline">The Spline to use for the conversion, this is necessary to compute most of PathIndexUnits.</param>
        /// <param name="toUnit">The unit to convert SplineData to.</param>
        public void ConvertPathUnit<TSplineType>(TSplineType spline, PathIndexUnit toUnit)
            where TSplineType : ISpline
        {
            if(toUnit == m_IndexUnit)
                return;

            for(int i = 0; i < m_DataPoints.Count; i++)
            {
                var dataPoint = m_DataPoints[i];
                var newTime = spline.ConvertIndexUnit(dataPoint.Index, m_IndexUnit, toUnit);
                m_DataPoints[i] = new DataPoint<T>(newTime, dataPoint.Value);
            }
            m_IndexUnit = toUnit;
            SetDirty();
        }

        /// <summary>
        /// Given a time value using a certain PathIndexUnit type, calculate the normalized time value regarding a specific spline.
        /// </summary>
        /// <param name="spline">The Spline to use for the conversion, this is necessary to compute Normalized and Distance PathIndexUnits.</param>
        /// <param name="t">The time to normalize in the original PathIndexUnit.</param>
        /// <typeparam name="TSplineType">The Spline type.</typeparam>
        /// <returns>The normalized time.</returns>
        public float GetNormalizedInterpolation<TSplineType>(TSplineType spline, float t) where TSplineType : ISpline
        {
            return SplineUtility.GetNormalizedInterpolation(spline, t, m_IndexUnit);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the DataPoints collection.
        /// </summary>
        /// <returns>
        /// An IEnumerator{DataPoint{T}} for this collection.</returns>
        public IEnumerator<DataPoint<T>> GetEnumerator()
        {
            for (int i = 0, c = Count; i < c; ++i)
                yield return m_DataPoints[i];
        }

        /// <summary>
        /// Returns an enumerator that iterates through the DataPoints collection.
        /// </summary>
        /// <returns>
        /// An IEnumerator{DataPoint{T}} for this collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        static float WrapInt(float index, int lowerBound, int upperBound)
        {
            return Wrap((int)math.floor(index), lowerBound, upperBound) + math.frac(index);
        }

        static float ClampInt(float index, int lowerBound, int upperBound)
        {
            return math.clamp((int)math.floor(index), lowerBound, upperBound) + math.frac(index);
        }

        // IMPORTANT - NOT PUBLIC API. See ISplineModificationHandler for more information.
        /// <summary>
        /// Attempts to preserve knot indices relative to their current position after a Spline has been modified. This
        /// is only valid for SplineData that is indexed using <see cref="UnityEngine.Splines.PathIndexUnit.Knot"/>.
        /// </summary>
        /// <remarks>
        /// This function is only valid for PathIndexUnit.Knot because other representations are (1) implicitly better
        /// suited to handle knot insertion/deletion while preserving locality, and (2) converting to the Knot index
        /// format for the purposes of order preservation would need to be done _before_ knot insertion/deletion in
        /// order to be correct.
        /// </remarks>
        void ISplineModificationHandler.OnSplineModified(SplineModificationData data)
        {
            if (m_IndexUnit != PathIndexUnit.Knot)
                return;

            if (data.Modification == SplineModification.KnotModified || data.Modification == SplineModification.KnotReordered || data.Modification == SplineModification.Default)
                return;

            var editedKnotOldIdx = data.KnotIndex;
            var prevLength = data.PrevCurveLength;
            var nextLength = data.NextCurveLength;

            var dataPointsToRemove = new List<int>();
            for (int dataIdx = 0, c = Count; dataIdx < c; ++dataIdx)
            {
                var point = m_DataPoints[dataIdx];
                var dataKnotOldIdx = (int)math.floor(point.Index);
                var fracIdx = point.Index - dataKnotOldIdx;

                if (data.Modification == SplineModification.KnotInserted)
                {
                    var currentLength = data.Spline.GetCurveLength(data.Spline.PreviousIndex(editedKnotOldIdx));
                    if (dataKnotOldIdx == editedKnotOldIdx - 1)
                    {
                        if (fracIdx < currentLength / prevLength)
                            point.Index = dataKnotOldIdx + fracIdx * (prevLength / currentLength);
                        else
                            point.Index = (dataKnotOldIdx + 1) + (fracIdx * prevLength - currentLength) / (prevLength - currentLength);
                    }
                    else if (data.Spline.Closed && dataKnotOldIdx == data.Spline.Count - 2 && editedKnotOldIdx == 0)
                    {
                        if (fracIdx < currentLength / prevLength)
                            point.Index = (dataKnotOldIdx + 1) + fracIdx * (prevLength / currentLength);
                        else
                            point.Index = (fracIdx * prevLength - currentLength) / (prevLength - currentLength);
                    }
                    else
                        point.Index += 1;
                }
                else if (data.Modification == SplineModification.KnotRemoved)
                {
                    // The spline is cleared.
                    if (editedKnotOldIdx == -1)
                    {
                        dataPointsToRemove.Add(dataIdx);
                        continue;
                    }

                    var removingHardLink = (fracIdx == 0f && dataKnotOldIdx == editedKnotOldIdx);

                    var removingEndKnots = !data.Spline.Closed &&
                        ((dataKnotOldIdx <= 0 && editedKnotOldIdx == 0) || // Removed first curve with data points on it.
                        (editedKnotOldIdx == data.Spline.Count &&          // Removed last curve
                        math.ceil(point.Index) >= editedKnotOldIdx));      //  and data point is either on last curve or beyond it/clamped.

                    if (removingHardLink || removingEndKnots || data.Spline.Count == 1)
                        dataPointsToRemove.Add(dataIdx);
                    else if (dataKnotOldIdx == editedKnotOldIdx - 1)
                        point.Index = dataKnotOldIdx + fracIdx * prevLength / (prevLength + nextLength);
                    else if (dataKnotOldIdx == editedKnotOldIdx)
                        point.Index = (dataKnotOldIdx - 1) + (prevLength + fracIdx * nextLength) / (prevLength + nextLength);
                    else if ((data.Spline.Closed && editedKnotOldIdx == 0) /*acting on the previous first knot of a closed spline*/
                        && dataKnotOldIdx == data.Spline.Count /*and considering data that is on the last curve closing the spline*/)
                        point.Index = (dataKnotOldIdx - 1) + (fracIdx * prevLength) / (prevLength + nextLength);
                    else if (dataKnotOldIdx >= editedKnotOldIdx)
                        point.Index -= 1;
                }
                else // Closed Modified
                {
                    if (!data.Spline.Closed &&                          // Spline has been opened and
                        (math.ceil(point.Index) >= data.Spline.Count))  // data point is on connecting curve or the very end of it (treat same as open spline last curve deletion).
                        dataPointsToRemove.Add(dataIdx);
                }

                m_DataPoints[dataIdx] = point;
            }

            for (int i = dataPointsToRemove.Count - 1; i > -1; --i)
            {
                m_DataPoints.RemoveAt(dataPointsToRemove[i]);
            }
        }
    }
}
