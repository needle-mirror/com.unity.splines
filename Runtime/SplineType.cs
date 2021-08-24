namespace UnityEngine.Splines
{
    /// <summary>
    /// Describes the different supported Spline representations.
    /// </summary>
    /// <remarks>
    /// Internally all <see cref="Spline"/> objects are saved as series of cubic curves. In the editor Splines can be
    /// manipulated in a lower order form.
    /// </remarks>
    public enum SplineType : byte
    {
        /// <summary>
        /// Catmull-Rom Spline is a type of Cubic Hermite Spline. Tangents are calculated from control points rather than
        /// discretely defined.
        /// See https://en.wikipedia.org/wiki/Cubic_Hermite_spline#Catmull%E2%80%93Rom_spline for more information.
        /// </summary>
        CatmullRom,
        /// <summary>
        /// A series of connected cubic bezier curves. This is the default Spline type.
        /// </summary>
        Bezier,
        /// <summary>
        /// A series of connected straight line segments.
        /// </summary>
        Linear
    }
}
