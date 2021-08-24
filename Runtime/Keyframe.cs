using System;
using System.Collections.Generic;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Defines an interpolation ratio 't' for a keyframe. 
    /// </summary>
    public interface IKeyframe
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
    /// <typeparam name="T">The type of data this keyframe stores.</typeparam>
    [Serializable]
    public struct Keyframe<T> : IComparable<Keyframe<T>>, IComparable<float>, IKeyframe
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
        /// A value to store with this keyframe.
        /// </summary>
        public T Value
        {
            get => m_Value;
            set => m_Value = value;
        }
    
        /// <summary>
        /// Create a new Keyframe with interpolation ratio and value.
        /// </summary>
        /// <param name="t">Interpolation ratio.</param>
        /// <param name="value">The value to store.</param>
        public Keyframe(float t, T value)
        {
            m_Time = t;
            m_Value = value;
        }
        
        /// <summary>
        /// Compare keyframe <see cref="Time"/> values.
        /// </summary>
        /// <param name="other">The Keyframe to compare against.</param>
        /// <returns>An integer less than 0 if other.Time is greater than <see cref="Time"/>, 0 if time values are equal, and greater
        /// than 0 when other.Time is less than <see cref="Time"/>.</returns>
        public int CompareTo(Keyframe<T> other) => Time.CompareTo(other.Time);

        /// <summary>
        /// Compare keyframe <see cref="Time"/> values.
        /// </summary>
        /// <param name="other">An interpolation ratio to compare against.</param>
        /// <returns>An integer less than 0 if other.Time is greater than <see cref="Time"/>, 0 if time values are equal, and greater
        /// than 0 when other.Time is less than <see cref="Time"/>.</returns>
        public int CompareTo(float other) => Time.CompareTo(other);

        /// <summary>
        /// A summary of the keyframe time and value.
        /// </summary>
        /// <returns>A summary of the keyframe time and value.</returns>
        public override string ToString() => $"{Time} {Value}";
    }
    
    class KeyframeComparer<T> : IComparer<T> where T : IKeyframe
    {
        public int Compare(T x, T y)
        {
            return x.Time.CompareTo(y.Time);
        }
    }
}
