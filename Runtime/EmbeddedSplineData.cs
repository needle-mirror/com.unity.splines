using System;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Wrapper for accessing a <see cref="SplineData{T}"/> value stored on <see cref="Spline"/> through one of the
    /// embedded key value collections. It is not required to use this class to access embedded
    /// <see cref="SplineData{T}"/>, however it does provide some convenient functionality for working with this data
    /// in the Inspector.
    /// </summary>
    /// <seealso cref="EmbeddedSplineDataFieldsAttribute"/>
    /// <seealso cref="Spline.GetOrCreateFloatData"/>
    /// <seealso cref="Spline.GetOrCreateFloat4Data"/>
    /// <seealso cref="Spline.GetOrCreateIntData"/>
    /// <seealso cref="Spline.GetOrCreateObjectData"/>
    [Serializable]
    public class EmbeddedSplineData
    {
        [SerializeField]
        SplineContainer m_Container;

        [SerializeField]
        int m_SplineIndex;

        [SerializeField]
        EmbeddedSplineDataType m_Type;

        [SerializeField]
        string m_Key;

        /// <summary>
        /// The <see cref="SplineContainer"/> that holds the <see cref="Spline"/>.
        /// </summary>
        public SplineContainer Container
        {
            get => m_Container;
            set => m_Container = value;
        }

        /// <summary>
        /// The index of the <see cref="Spline"/> on the <see cref="SplineContainer"/>.
        /// </summary>
        public int SplineIndex
        {
            get => m_SplineIndex;
            set => m_SplineIndex = value;
        }

        /// <summary>
        /// The type of data stored by the <see cref="SplineData{T}"/> collection. Embedded <see cref="SplineData{T}"/>
        /// is restricted to a pre-defined set of primitive types.
        /// </summary>
        public EmbeddedSplineDataType Type
        {
            get => m_Type;
            set => m_Type = value;
        }

        /// <summary>
        /// A unique string value used to identify and access a <see cref="SplineData{T}"/> collection stored in a
        /// <see cref="Spline"/>.
        /// </summary>
        public string Key
        {
            get => m_Key;
            set => m_Key = value;
        }

        /// <summary>
        /// Create a new <see cref="EmbeddedSplineData"/> instance with no parameters.
        /// </summary>
        public EmbeddedSplineData() : this(null, EmbeddedSplineDataType.Float)
        {
        }

        /// <summary>
        /// Create a new <see cref="EmbeddedSplineData"/> with parameters.
        /// </summary>
        /// <param name="key">A unique string value used to identify and access a <see cref="SplineData{T}"/> collection
        /// stored in a <see cref="Spline"/>.</param>
        /// <param name="type">The type of data stored by the <see cref="SplineData{T}"/> collection.</param>
        /// <param name="container">The <see cref="SplineContainer"/> that holds the <see cref="Spline"/>.</param>
        /// <param name="splineIndex">The index of the <see cref="Spline"/> on the <see cref="SplineContainer"/>.</param>
        public EmbeddedSplineData(
            string key,
            EmbeddedSplineDataType type,
            SplineContainer container = null,
            int splineIndex = 0)
        {
            m_Container = container;
            m_SplineIndex = splineIndex;
            m_Key = key;
            m_Type = type;
        }

        /// <summary>
        /// Attempt to get a reference to the <see cref="Spline"/> described by this object.
        /// </summary>
        /// <param name="spline">A <see cref="Spline"/> if the <see cref="Container"/> and <see cref="SplineIndex"/> are
        /// valid, otherwise null.</param>
        /// <returns>Returns true if the <see cref="Container"/> and <see cref="SplineIndex"/> are valid, otherwise
        /// false.</returns>
        public bool TryGetSpline(out Spline spline)
        {
            if(Container == null || SplineIndex < 0 || SplineIndex >= Container.Splines.Count)
                spline = null;
            else
                spline = Container.Splines[SplineIndex];
            return spline != null;
        }

        /// <summary>
        /// Attempt to get a reference to the <see cref="SplineData{T}"/> described by this object.
        /// </summary>
        /// <param name="data">A <see cref="SplineData{T}"/> reference if the <see cref="Container"/>,
        /// <see cref="SplineIndex"/>, <see cref="Key"/>, and <see cref="Type"/> are valid, otherwise null.</param>
        /// <returns>Returns true if a <see cref="SplineData{T}"/> value exists, otherwise false.</returns>
        /// <exception cref="InvalidCastException">An exception is thrown if the requested <see cref="SplineData{T}"/>
        /// does not match the <see cref="EmbeddedSplineDataType"/>.</exception>
        public bool TryGetFloatData(out SplineData<float> data)
        {
            if(Type != EmbeddedSplineDataType.Float)
                throw new InvalidCastException($"EmbeddedSplineDataType {Type} does not match requested SplineData collection: {typeof(float)}");
            return Container.Splines[SplineIndex].TryGetFloatData(Key, out data);
        }

        /// <inheritdoc cref="TryGetFloatData"/>
        public bool TryGetFloat4Data(out SplineData<float4> data)
        {
            if(Type != EmbeddedSplineDataType.Float4)
                throw new InvalidCastException($"EmbeddedSplineDataType {Type} does not match requested SplineData collection: {typeof(float4)}");
            return Container.Splines[SplineIndex].TryGetFloat4Data(Key, out data);
        }

        /// <inheritdoc cref="TryGetFloatData"/>
        public bool TryGetIntData(out SplineData<int> data)
        {
            if(Type != EmbeddedSplineDataType.Int)
                throw new InvalidCastException($"EmbeddedSplineDataType {Type} does not match requested SplineData collection: {typeof(int)}");
            return Container.Splines[SplineIndex].TryGetIntData(Key, out data);
        }

        /// <inheritdoc cref="TryGetFloatData"/>
        public bool TryGetObjectData(out SplineData<Object> data)
        {
            if(Type != EmbeddedSplineDataType.Object)
                throw new InvalidCastException($"EmbeddedSplineDataType {Type} does not match requested SplineData collection: {typeof(Object)}");
            return Container.Splines[SplineIndex].TryGetObjectData(Key, out data);
        }

        /// <summary>
        /// Returns a <see cref="SplineData{T}"/> for <see cref="Key"/> and <see cref="Type"/>. If an instance matching
        /// the key and type does not exist, a new entry is appended to the internal collection and returned.
        /// Note that this is a reference to the stored <see cref="SplineData{T}"/>, not a copy. Any modifications to
        /// this collection will affect the <see cref="Spline"/> data.
        /// </summary>
        /// <returns>A <see cref="SplineData{T}"/> of the requested type.</returns>
        /// <exception cref="InvalidCastException">An exception is thrown if the requested <see cref="SplineData{T}"/>
        /// does not match the <see cref="EmbeddedSplineDataType"/>.</exception>
        public SplineData<float> GetOrCreateFloatData()
        {
            if(Type != EmbeddedSplineDataType.Float)
                throw new InvalidCastException($"EmbeddedSplineDataType {Type} does not match requested SplineData collection: {typeof(float)}");
            return Container.Splines[SplineIndex].GetOrCreateFloatData(Key);
        }

        /// <inheritdoc cref="GetOrCreateFloatData"/>
        public SplineData<float4> GetOrCreateFloat4Data()
        {
            if(Type != EmbeddedSplineDataType.Float4)
                throw new InvalidCastException($"EmbeddedSplineDataType {Type} does not match requested SplineData collection: {typeof(float4)}");
            return Container.Splines[SplineIndex].GetOrCreateFloat4Data(Key);
        }

        /// <inheritdoc cref="GetOrCreateFloatData"/>
        public SplineData<int> GetOrCreateIntData()
        {
            if(Type != EmbeddedSplineDataType.Int)
                throw new InvalidCastException($"EmbeddedSplineDataType {Type} does not match requested SplineData collection: {typeof(int)}");
            return Container.Splines[SplineIndex].GetOrCreateIntData(Key);
        }

        /// <inheritdoc cref="GetOrCreateFloatData"/>
        public SplineData<Object> GetOrCreateObjectData()
        {
            if(Type != EmbeddedSplineDataType.Object)
                throw new InvalidCastException($"EmbeddedSplineDataType {Type} does not match requested SplineData collection: {typeof(Object)}");
            return Container.Splines[SplineIndex].GetOrCreateObjectData(Key);
        }
    }
}
