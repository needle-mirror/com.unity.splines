using System;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Attribute used to make an integer variable show in the Inspector as a popup menu with spline choices relative
    /// to a <see cref="ISplineContainer"/>.
    /// </summary>
    public class SplineIndexAttribute : PropertyAttribute
    {
        /// <summary>
        /// The name of the field that references an <see cref="ISplineContainer"/>.
        /// </summary>
        public readonly string SplineContainerProperty;

        /// <summary>
        /// Creates a new SplineIndexAttribute with the name of a variable that references an <see cref="ISplineContainer"/>.
        /// </summary>
        /// <param name="splineContainerProperty">The name of the field that references an <see cref="ISplineContainer"/>.
        /// It is recommended to pass this value using `nameof()` to avoid typos.</param>
        public SplineIndexAttribute(string splineContainerProperty)
        {
            SplineContainerProperty = splineContainerProperty;
        }
    }

    /// <summary>
    /// Describes the fields on an <see cref="EmbeddedSplineData"/> type.
    /// </summary>
    [Flags]
    public enum EmbeddedSplineDataField
    {
        /// <summary>The <see cref="ISplineContainer"/> that holds the <see cref="SplineData{T}"/> collection.</summary>
        Container = 1 << 0,
        /// <summary>The index of the <see cref="Spline"/> in the referenced <see cref="SplineContainer"/>.</summary>
        SplineIndex = 1 << 1,
        /// <summary>A string value used to identify and access <see cref="SplineData{T}"/> stored on a <see cref="Spline"/>.</summary>
        Key = 1 << 2,
        /// <summary>The <see cref="EmbeddedSplineDataType"/> for the target <see cref="SplineData{T}"/>.</summary>
        Type = 1 << 3,
        /// <summary>All fields will be shown in the Inspector.</summary>
        All = 0xFF
    }

    /// <summary>
    /// Attribute used to make an <see cref="EmbeddedSplineData"/> variable show in the Inspector with a filtered set
    /// of fields editable. Use this in situations where you want to specify <see cref="EmbeddedSplineData"/> parameters
    /// in code and not allow them to be modified in the Inspector.
    /// to a <see cref="ISplineContainer"/>.
    /// </summary>
    public class EmbeddedSplineDataFieldsAttribute : PropertyAttribute
    {
        /// <summary>
        /// The fields to show in the Inspector.
        /// </summary>
        public readonly EmbeddedSplineDataField Fields;

        /// <summary>
        /// Create an <see cref="EmbeddedSplineDataFieldsAttribute"/> attribute.
        /// </summary>
        /// <param name="fields">The fields to show in the Inspector. <see cref="EmbeddedSplineDataField"/>.</param>
        public EmbeddedSplineDataFieldsAttribute(EmbeddedSplineDataField fields)
        {
            Fields = fields;
        }
    }
}
