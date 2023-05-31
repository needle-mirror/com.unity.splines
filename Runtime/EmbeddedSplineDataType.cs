namespace UnityEngine.Splines
{
    /// <summary>
    /// Describes a type of data stored by a <see cref="SplineData{T}"/> collection embedded in <see cref="Spline"/>.
    /// </summary>
    public enum EmbeddedSplineDataType
    {
        /// <summary>
        /// Integer data type.
        /// </summary>
        /// <seealso cref="Spline.GetOrCreateIntData"/>
        Int = 0,

        /// <summary>
        /// Float data type.
        /// </summary>
        /// <seealso cref="Spline.GetOrCreateFloatData"/>
        Float = 1,

        /// <summary>
        /// Float4 data type.
        /// </summary>
        /// <seealso cref="Spline.GetOrCreateFloat4Data"/>
        Float4 = 2,

        /// <summary>
        /// UnityEngine.Object data type.
        /// </summary>
        /// <seealso cref="Spline.GetOrCreateObjectData"/>
        Object = 3
    }
}
