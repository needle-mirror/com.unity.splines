using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

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
        float Index { get; set; }
    }

    /// <summary>
    /// A pair containing an interpolation ratio and {TDataType} value.
    /// </summary>
    /// <typeparam name="TDataType">The type of data this data point stores.</typeparam>
    [Serializable]
    public struct DataPoint<TDataType> : IComparable<DataPoint<TDataType>>, IComparable<float>, IDataPoint
    {
        [FormerlySerializedAs("m_Time")]
        [SerializeField]
        float m_Index;

        [SerializeField]
        TDataType m_Value;

        /// <summary>
        /// The interpolation ratio relative to a spline. How this value is interpolated depends on the <see cref="PathIndexUnit"/>
        /// specified by <see cref="SplineData{T}"/>.
        /// </summary>
        public float Index
        {
            get => m_Index;
            set => m_Index = value;
        }

        /// <summary>
        /// A value to store with this Data Point.
        /// </summary>
        public TDataType Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        /// <summary>
        /// Create a new Data Point with interpolation ratio and value.
        /// </summary>
        /// <param name="index">Interpolation ratio.</param>
        /// <param name="value">The value to store.</param>
        public DataPoint(float index, TDataType value)
        {
            m_Index = index;
            m_Value = value;
        }

        /// <summary>
        /// Compare DataPoint <see cref="Index"/> values.
        /// </summary>
        /// <param name="other">The DataPoint to compare against.</param>
        /// <returns>An integer less than 0 if other.Key is greater than <see cref="Index"/>, 0 if key values are equal, and greater
        /// than 0 when other.Key is less than <see cref="Index"/>.</returns>
        public int CompareTo(DataPoint<TDataType> other) => Index.CompareTo(other.Index);

        /// <summary>
        /// Compare DataPoint <see cref="Index"/> values.
        /// </summary>
        /// <param name="other">An interpolation ratio to compare against.</param>
        /// <returns>An integer less than 0 if other.Key is greater than <see cref="Index"/>, 0 if key values are equal, and greater
        /// than 0 when other.Key is less than <see cref="Index"/>.</returns>
        public int CompareTo(float other) => Index.CompareTo(other);

        /// <summary>
        /// A summary of the DataPoint time and value.
        /// </summary>
        /// <returns>A summary of the DataPoint key and value.</returns>
        public override string ToString() => $"{Index} {Value}";
    }

    class DataPointComparer<T> : IComparer<T> where T : IDataPoint
    {
        public int Compare(T x, T y)
        {
            return x.Index.CompareTo(y.Index);
        }
    }
}
