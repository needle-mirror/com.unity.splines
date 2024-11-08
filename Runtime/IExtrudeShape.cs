using System;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// <para>Implement this class to create a customized shape that can be extruded along a <see cref="Spline"/> using the
    /// <see cref="SplineMesh"/> class.
    ///
    /// Some default shape implementations are available in the <see cref="ExtrusionShapes"/> namespace.
    ///
    /// <see cref="SplineMesh"/> generates extruded mesh geometry in the following manner (pseudo-code):</para>
    /// <code>
    /// extrudeShape.Setup(spline, numberOfSegments);
    /// for(int i = 0; i &lt; numberOfSegments; ++i)
    /// {
    ///     float t = i / (numberOfSegments - 1);
    ///     extrudeShape.SetSegment(i, t, splinePositionAtT, splineTangentAtT, splineUpAtT);
    ///     for(int n = 0; n &lt; extrudeShape.SideCount; ++n)
    ///         vertices.Add(extrudeShape.GetPosition(n / (extrudeShape.SideCount - 1), n));
    /// }
    /// </code>
    /// <para>This example IExtrudeShape implementation creates a tube.</para>
    /// </summary>
    /// <example>
    /// ```lang-csharp
    /// <![CDATA[
    /// // While not strictly necessary, marking the class as Serializable means that
    /// // this can be edited in the Inspector.
    /// [Serializable]
    /// public class Circle : IExtrudeShape
    /// {
    ///     [SerializeField, Min(2)]
    ///     int m_Sides = 8;
    ///
    ///     float m_Rads;
    ///
    ///     // We only need to calculate the radians step once, so do it in the Setup method.
    ///     public void Setup(ISpline path, int segmentCount)
    ///     {
    ///         m_Rads = math.radians(360f / SideCount);
    ///     }
    ///
    ///     public float2 GetPosition(float t, int index)
    ///     {
    ///         return new float2(math.cos(index * m_Rads), math.sin(index * m_Rads));
    ///     }
    ///
    ///     public int SideCount
    ///     {
    ///         get => m_Sides;
    ///         set => m_Sides = value;
    ///     }
    /// }
    /// ]]>
    /// ```
    /// </example>
    public interface IExtrudeShape
    {
        /// <summary>
        /// Implement this function to access information about the <see cref="ISpline"/> path being extruded and
        /// number of segments. <see cref="SplineMesh"/> invokes this method once prior to extruding the mesh.
        /// </summary>
        /// <param name="path">The <see cref="ISpline"/> that this template is being extruded along.</param>
        /// <param name="segmentCount">The total number of segments to be created on the extruded mesh. This is
        /// equivalent to the number of vertex "rings" that make up the mesh positions.</param>
        public void Setup(ISpline path, int segmentCount) {}

        /// <summary>
        /// Implement this function to access information about the spline path being extruded for each segment.
        /// <see cref="SplineMesh"/> invokes this method once before each ring of vertices is calculated.
        /// </summary>
        /// <param name="index">The segment index for the current vertex ring.</param>
        /// <param name="t">The normalized interpolation ratio corresponding to the segment index. Equivalent to index divided by segmentCount - 1.</param>
        /// <param name="position">The position on the <see cref="Spline"/> path being extruded along at <paramref name="t"/>.</param>
        /// <param name="tangent">The tangent on the <see cref="Spline"/> path being extruded along at <paramref name="t"/>.</param>
        /// <param name="up">The up vector on the <see cref="Spline"/> path being extruded along at <paramref name="t"/>.</param>
        public void SetSegment(int index, float t, float3 position, float3 tangent, float3 up) {}

        /// <summary>
        /// How many vertices make up a single ring around the mesh.
        /// </summary>
        /// <value>How many vertices make up a revolution for each segment of the extruded mesh.</value>
        public int SideCount { get; }

        /// <summary>
        /// This method is responsible for returning a 2D position of the template shape for each vertex of a single
        /// ring around the extruded mesh.
        /// Note that both interpolation <paramref name="t"/> and <paramref name="index"/> are provided as a convenience.
        /// </summary>
        /// <param name="t">The normalized interpolation [0...1] for a vertex around an extruded ring.</param>
        /// <param name="index">The index of the vertex in the extruded ring.</param>
        /// <returns>A 2D position interpolated along a template shape to be extruded. This value will be converted to
        /// a 3D point and rotated to align with the spline at the current segment index.</returns>
        public float2 GetPosition(float t, int index);
    }
}

namespace UnityEngine.Splines.ExtrusionShapes
{
    // This is intentionally not public. It is used only by the SplineExtrudeEditor class to provide
    // an enum popup.
    // when updating this, make sure to also update ShapeTypeUtility.{GetShapeType, CreateShape}
    enum ShapeType
    {
        Circle,
        Square,
        Road,
        [InspectorName("Spline Profile")]
        Spline
    }

    static class ShapeTypeUtility
    {
        public static ShapeType GetShapeType(object obj)
        {
            return obj switch
            {
                Circle => ShapeType.Circle,
                Square => ShapeType.Square,
                Road => ShapeType.Road,
                SplineShape => ShapeType.Spline,
                _ => throw new ArgumentException($"{nameof(obj)} is not a recognized shape", nameof(obj))
            };
        }

        public static IExtrudeShape CreateShape(ShapeType type)
        {
            return type switch
            {
                ShapeType.Square => new Square(),
                ShapeType.Road => new Road(),
                ShapeType.Spline => new SplineShape(),
                ShapeType.Circle => new Circle(),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }
    }

    /// <summary>
    /// Create a circle shape to be extruded along a spline.
    /// <![CDATA[
    ///        ___
    ///      /    \
    ///     |     |
    ///     \    /
    ///      ---
    /// ]]>
    /// </summary>
    /// <seealso cref="SplineMesh"/>
    /// <seealso cref="IExtrudeShape"/>
    /// <seealso cref="SplineExtrude"/>
    [Serializable]
    public sealed class Circle : IExtrudeShape
    {
        [SerializeField, Min(2)]
        int m_Sides = 8;

        float m_Rads;

        /// <inheritdoc cref="IExtrudeShape.Setup"/>
        public void Setup(ISpline path, int segmentCount) => m_Rads = math.radians(360f / SideCount);

        /// <inheritdoc cref="IExtrudeShape.GetPosition"/>
        public float2 GetPosition(float t, int index)
        {
            return new float2(math.cos(index * m_Rads), math.sin(index * m_Rads));
        }

        /// <inheritdoc cref="IExtrudeShape.SideCount"/>
        public int SideCount
        {
            get => m_Sides;
            set => m_Sides = value;
        }
    }

    /// <summary>
    /// Create a square shape to be extruded along a spline.
    /// </summary>
    /// <seealso cref="SplineMesh"/>
    /// <seealso cref="IExtrudeShape"/>
    /// <seealso cref="SplineExtrude"/>
    [Serializable]
    public sealed class Square : IExtrudeShape
    {
        /// <inheritdoc cref="IExtrudeShape.SideCount"/>
        /// <value>Square is fixed to 4 sides.</value>
        public int SideCount => 4;

        static readonly float2[] k_Sides = new[]
        {
            new float2(-.5f, -.5f),
            new float2(.5f, -.5f),
            new float2(.5f, .5f),
            new float2(-.5f, .5f),
        };

        /// <inheritdoc cref="IExtrudeShape.GetPosition"/>
        public float2 GetPosition(float t, int index) => k_Sides[index];
    }

    /// <summary>
    /// A simple plane with skirts at the edges to better blend with uneven terrain.
    /// <![CDATA[
    ///     // Looks like this
    ///      __________
    ///     /          \
    /// ]]>
    /// </summary>
    /// <seealso cref="SplineMesh"/>
    /// <seealso cref="IExtrudeShape"/>
    /// <seealso cref="SplineExtrude"/>
    [Serializable]
    public sealed class Road : IExtrudeShape
    {
        /// <inheritdoc cref="IExtrudeShape.SideCount"/>
        /// <value>A road is fixed to 3 sides.</value>
        public int SideCount => 3;

        static readonly float2[] k_Sides = new[]
        {
            new float2(-.6f, -.1f),
            new float2(-.5f,   0f),
            new float2( .5f,   0f),
            new float2( .6f, -.1f)
        };

        /// <inheritdoc cref="IExtrudeShape.GetPosition"/>
        public float2 GetPosition(float t, int index) => k_Sides[3-index];
    }

    /// <summary>
    /// Create a shape using a <see cref="Spline"/> as the template path. The
    /// referenced Spline is sampled at <see cref="IExtrudeShape.SideCount"/>
    /// points along the path to form the vertex rings at each segment.
    /// </summary>
    [Serializable]
    public class SplineShape : IExtrudeShape
    {
        [SerializeField]
        SplineContainer m_Template;

        [SerializeField, SplineIndex(nameof(m_Template))]
        int m_SplineIndex;

        [SerializeField, Min(2)]
        int m_SideCount = 12;

        /// <summary>
        /// Defines the axes used to project the input spline template to 2D coordinates.
        /// </summary>
        public enum Axis
        {
            /// <summary>
            /// Project from the horizontal (X) axis. Uses the {Y, Z} components of
            /// the position to form the 2D template coordinates.
            /// </summary>
            X,
            /// <summary>
            /// Project from the vertical (Y) axis. Uses the {X, Z} components of
            /// the position to form the 2D template coordinates.
            /// </summary>
            Y,
            /// <summary>
            /// Project from the forward (Z) axis. Uses the {X, Y} components of
            /// the position to form the 2D template coordinates.
            /// </summary>
            Z
        }

        /// <summary>
        /// Defines the axes used to project the input spline template to 2D coordinates.
        /// </summary>
        [SerializeField, Tooltip("The axis of the template spline to be used when winding the vertices along the " +
                                 "extruded mesh.")]
        public Axis m_Axis = Axis.Y;

        /// <inheritdoc cref="IExtrudeShape.SideCount"/>
        public int SideCount
        {
            get => m_SideCount;
            set => m_SideCount = value;
        }

        /// <summary>
        /// The <see cref="SplineContainer"/> which contains the
        /// <see cref="Spline"/> to use as the shape template.
        /// </summary>
        public SplineContainer SplineContainer
        {
            get => m_Template;
            set => m_Template = value;
        }

        /// <summary>
        /// The index of the <see cref="Spline"/> in the <see cref="SplineContainer"/>
        /// to use as the shape template. This value must be greater than or
        /// equal to 0. If the index is out of bounds of the container spline
        /// count, the modulo of SplineIndex and Container.Splines.Count is used
        /// as the index.
        /// </summary>
        public int SplineIndex
        {
            get => m_SplineIndex;
            set => m_SplineIndex = math.max(0, value);
        }

        /// <summary>
        /// Returns the <see cref="Spline"/> referenced by the
        /// <see cref="SplineContainer"/> and <see cref="SplineIndex"/>.
        /// </summary>
        public Spline Spline => m_Template != null
            ? m_Template[m_SplineIndex % m_Template.Splines.Count]
            : null;

        /// <inheritdoc cref="IExtrudeShape.GetPosition"/>
        public float2 GetPosition(float t, int index)
        {
            if (Spline == null)
                return 0f;

            if (t == 1)
                t = 0.9999f;
            else if (t == 0)
                t = 0.0001f;

            return m_Axis switch
            {
                Axis.X => Spline.EvaluatePosition(1 - t).zy,
                Axis.Y => Spline.EvaluatePosition(1 - t).xz,
                Axis.Z => Spline.EvaluatePosition(1 - t).xy,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
