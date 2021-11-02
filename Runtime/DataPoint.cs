using System;
using System.Collections.Generic;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Defines an interpolation ratio 't' for a Data Point. 
    /// </summary>
    public interface IDataPoint
    {
        /// <summary>
        /// The interpolation ratio. How this value is interpreted depends on the <see cref="PathIndexUnit"/> specified
        /// by <see cref="SplineData{T}"/>.
        /// </summary>
        float Time { get; set; }
    }
    
    /// <summary>
    /// A pair containing an interpolation ratio and {T} value.
    /// </summary>
    /// <typeparam name="T">The type of data this data point stores.</typeparam>
    [Serializable]
    public struct DataPoint<T> : IComparable<DataPoint<T>>, IComparable<float>, IDataPoint
    {
        [SerializeField]
        float m_Time;

        [SerializeField]
        T m_Value;

        /// <summary>
        /// The interpolation ratio relative to a spline. How this value is interpolated depends on the <see cref="PathIndexUnit"/>
        /// specified by <see cref="SplineData{T}"/>.
        /// </summary>
        public float Time
        {
            get => m_Time;
            set => m_Time = value;
        }

        /// <summary>
        /// A value to store with this Data Point.
        /// </summary>
        public T Value
        {
            get => m_Value;
            set => m_Value = value;
        }
    
        /// <summary>
        /// Create a new Data Point with interpolation ratio and value.
        /// </summary>
        /// <param name="t">Interpolation ratio.</param>
        /// <param name="value">The value to store.</param>
        public DataPoint(float t, T value)
        {
            m_Time = t;
            m_Value = value;
        }
        
        /// <summary>
        /// Compare DataPoint <see cref="Time"/> values.
        /// </summary>
        /// <param name="other">The DataPoint to compare against.</param>
        /// <returns>An integer less than 0 if other.Time is greater than <see cref="Time"/>, 0 if time values are equal, and greater
        /// than 0 when other.Time is less than <see cref="Time"/>.</returns>
        public int CompareTo(DataPoint<T> other) => Time.CompareTo(other.Time);

        /// <summary>
        /// Compare DataPoint <see cref="Time"/> values.
        /// </summary>
        /// <param name="other">An interpolation ratio to compare against.</param>
        /// <returns>An integer less than 0 if other.Time is greater than <see cref="Time"/>, 0 if time values are equal, and greater
        /// than 0 when other.Time is less than <see cref="Time"/>.</returns>
        public int CompareTo(float other) => Time.CompareTo(other);

        /// <summary>
        /// A summary of the DataPoint time and value.
        /// </summary>
        /// <returns>A summary of the DataPoint time and value.</returns>
        public override string ToString() => $"{Time} {Value}";
    }
    
    class DataPointComparer<T> : IComparer<T> where T : IDataPoint
    {
        public int Compare(T x, T y)
        {
            return x.Time.CompareTo(y.Time);
        }
    }
}
