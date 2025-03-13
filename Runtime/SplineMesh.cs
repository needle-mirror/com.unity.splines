using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.Splines.ExtrusionShapes;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Contains settings pertaining to the creation of mesh geometry for an
    /// extruded shape. Use with <see cref="SplineMesh"/>.
    /// </summary>
    /// <typeparam name="T">A type implementing <see cref="IExtrudeShape"/>.</typeparam>
    public struct ExtrudeSettings<T> where T : IExtrudeShape
    {
        const int k_SegmentsMin = 2, k_SegmentsMax = 4096;
        const float k_RadiusMin = .00001f, k_RadiusMax = 10000f;

        [SerializeField]
        T m_Shape;

        [SerializeField]
        bool m_CapEnds;

        [SerializeField]
        bool m_FlipNormals;

        [SerializeField]
        int m_SegmentCount;

        [SerializeField]
        float m_Radius;

        [SerializeField]
        Vector2 m_Range;

        /// <summary>
        /// How many sections compose the length of the mesh.
        /// </summary>
        public int SegmentCount
        {
            get => m_SegmentCount;
            set => m_SegmentCount = math.clamp(value, k_SegmentsMin, k_SegmentsMax);
        }

        /// <summary>
        /// Whether the start and end of the mesh is filled. This setting is ignored
        /// when the extruded spline is closed.
        /// Important note - cap are triangulated using a method that assumes convex geometry.
        /// If the input shape is concave, caps may show visual artifacts or overlaps.
        /// </summary>
        public bool CapEnds
        {
            get => m_CapEnds;
            set => m_CapEnds = value;
        }

        /// <summary>
        /// Set true to reverse the winding order of vertices so that the face normals are inverted. This is useful
        /// primarily for <see cref="SplineShape"/> templates where the input path may not produce a counter-clockwise
        /// vertex ring. Counter-clockwise winding equates to triangles facing outwards.
        /// </summary>
        public bool FlipNormals
        {
            get => m_FlipNormals;
            set => m_FlipNormals = value;
        }

        /// <summary>
        /// The section of the Spline to extrude. This value expects a normalized interpolation start and end.
        /// I.e., [0,1] is the entire Spline, whereas [.5, 1] is the last half of the Spline.
        /// </summary>
        public float2 Range
        {
            get => m_Range;
            set => m_Range = math.clamp(new float2(math.min(value.x, value.y), math.max(value.x, value.y)), 0f, 1f);
        }

        /// <summary>
        /// The radius of the extruded mesh. Radius is half of the width of the entire shape.
        /// The return value of <see cref="IExtrudeShape.GetPosition"/> is multiplied by this
        /// value to determine the size of the resulting shape.
        /// </summary>
        public float Radius
        {
            get => m_Radius;
            set => m_Radius = math.clamp(value, k_RadiusMin, k_RadiusMax);
        }

        /// <summary>
        /// The <see cref="IExtrudeShape"/> object defines the outline path of each segment.
        /// </summary>
        public T Shape
        {
            get => m_Shape;
            set => m_Shape = value;
        }

        internal bool DoCapEnds<K>(K spline) where K : ISpline => m_CapEnds && !spline.Closed;

        internal bool DoCloseSpline<K>(K spline) where K : ISpline => math.abs(1f - (Range.y - Range.x)) < float.Epsilon && spline.Closed;

        internal int sides
        {
            get
            {
                if (Shape is SplineShape)
                    return wrapped ? Shape.SideCount + 1 : Shape.SideCount;

                return wrapped ? Shape.SideCount : Shape.SideCount + 1;
            }
        }

        // true if a revolution ends at the start vertex, or false if it ends at the last vertex in the ring
        internal bool wrapped
        {
            get
            {
                if (Shape is SplineShape splineShape)
                {
                    if (splineShape.Spline != null)
                        return splineShape.Spline.Closed;
                }

                if (Shape is Road)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Create a new settings object with an <see cref="IExtrudeShape"/>
        /// instance. A default set of parameters will be used.
        /// </summary>
        /// <param name="shape">The <see cref="IExtrudeShape"/> template to
        /// be used as the shape template when extruding.</param>
        public ExtrudeSettings(T shape) : this(16, false, new float2(0, 1), .5f, shape)
        {
        }

        /// <summary>
        /// Create a new settings object. This is used by functions in
        /// <see cref="SplineMesh"/> to extrude a shape template along a spline.
        /// </summary>
        /// <param name="segments">The number of segments to divide the extruded spline into when creating vertex rings.</param>
        /// <param name="capped">Defines whether the ends of the extruded spline mesh should be closed.</param>
        /// <param name="range">The start and end points as normalized interpolation values.</param>
        /// <param name="radius">Defines the size of the extruded mesh.</param>
        /// <param name="shape">The <see cref="IExtrudeShape"/> template to
        /// be used as the shape template when extruding.</param>
        public ExtrudeSettings(int segments, bool capped, float2 range, float radius, T shape)
        {
            m_SegmentCount = math.clamp(segments, k_SegmentsMin, k_SegmentsMax);
            m_FlipNormals = false;
            m_Range = math.clamp(new float2(math.min(range.x, range.y), math.max(range.x, range.y)), 0f, 1f);
            m_CapEnds = capped;
            m_Radius = math.clamp(radius, k_RadiusMin, k_RadiusMax);
            m_Shape = shape;
        }
    }

    /// <summary>
    /// Static functions for extruding meshes along <see cref="Spline"/> paths.
    /// Use this class to build extruded meshes from code, or <see cref="SplineExtrude"/>
    /// for a pre-built Component that can be attached to any GameObject with
    /// a <see cref="SplineContainer"/>.
    /// </summary>
    public static class SplineMesh
    {
        const int k_SidesMin = 2, k_SidesMax = 2084;

        static readonly VertexAttributeDescriptor[] k_PipeVertexAttribs = new VertexAttributeDescriptor[]
        {
            new (VertexAttribute.Position),
            new (VertexAttribute.Normal),
            new (VertexAttribute.TexCoord0, dimension: 2)
        };

        static readonly Circle s_DefaultShape = new Circle();

        internal static bool s_IsConvex;
        static bool s_IsConvexComputed;

        /// <summary>
        /// Interface for Spline mesh vertex data. Implement this interface if you are extruding custom mesh data and
        /// do not want to use the vertex layout provided by <see cref="SplineMesh"/>."/>.
        /// </summary>
        public interface ISplineVertexData
        {
            /// <summary>
            /// Vertex position.
            /// </summary>
            public Vector3 position { get; set; }

            /// <summary>
            /// Vertex normal.
            /// </summary>
            public Vector3 normal { get; set; }

            /// <summary>
            /// Vertex texture, corresponds to UV0.
            /// </summary>
            public Vector2 texture { get; set; }
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct VertexData : ISplineVertexData
        {
            public Vector3 position { get; set; }
            public Vector3 normal { get; set; }
            public Vector2 texture { get; set; }
        }

        static void ExtrudeRing<TSpline, TShape, TVertex>(
            TSpline spline,
            ExtrudeSettings<TShape> settings,
            int segment,
            NativeArray<TVertex> data,
            int start,
            bool uvsAreCaps = false)
            where TSpline : ISpline
            where TShape : IExtrudeShape
            where TVertex : struct, ISplineVertexData
        {
            TShape shape = settings.Shape;
            int sideCount = settings.sides;
            float radius = settings.Radius;
            bool wrap = settings.wrapped;
            float t = math.lerp(settings.Range.x, settings.Range.y, segment / (settings.SegmentCount - 1f));

            var evaluationT = spline.Closed ? math.frac(t) : math.clamp(t, 0f, 1f);
            spline.Evaluate(evaluationT, out var sp, out var st, out var up);

            var tangentLength = math.lengthsq(st);
            if (tangentLength == 0f || float.IsNaN(tangentLength))
            {
                var adjustedT = math.clamp(evaluationT + (0.0001f * (t < 1f ? 1f : -1f)), 0f, 1f);
                spline.Evaluate(adjustedT, out _, out st, out up);
            }

            st = math.normalize(st);

            var rot = quaternion.LookRotationSafe(st, up);
            shape.SetSegment(segment, t, sp, st, up);
            var flip = settings.FlipNormals;

            for (int n = 0; n < sideCount; ++n)
            {
                var vertex = new TVertex();
                int index = flip ? sideCount - n - 1 : n;
                float v = index / (sideCount - 1f);
                var p = shape.GetPosition(v, index) * radius;
                vertex.position = sp + math.rotate(rot, new float3(p, 0f));
                vertex.normal = (vertex.position - (Vector3)sp).normalized * (flip ? -1f : 1f);

                // first branch is a special case for wrapping uvs at the caps
                if (uvsAreCaps)
                {
                    // the division by 2 is just a guess at a decent default for matching the uvs around the
                    // circumference of extruded shapes. the more accurate solution would be calculate the actual
                    // circumference and use that value to set cap scale.
                    vertex.texture = (p.xy / radius) / 2;
                }
                else if (wrap)
                {
                    // instead of inserting a vertex seam wrap UVs using a triangle wave so that
                    // texture wraps back onto itself
                    float ut = index / ((float)sideCount + sideCount % 2);
                    float u = math.abs(ut - math.floor(ut + 0.5f)) * 2f;
                    vertex.texture = new Vector2(1f - u, t * spline.GetLength());
                }
                else
                {
                    vertex.texture = new Vector2(1 - index / (sideCount - 1f), t * spline.GetLength());
                }

                data[start + n] = vertex;
            }

            if (s_IsConvexComputed)
                return;

            ComputeIsConvex(data, st, start, sideCount);
        }

        static void ComputeIsConvex<TVertex>(
            NativeArray<TVertex> data,
            float3 normal,
            int start,
            int sideCount)
            where TVertex : struct, ISplineVertexData
        {
            s_IsConvexComputed = true;

            bool isNegative = false;
            bool isPositive = false;

            for (int n = 0; n < sideCount; ++n)
            {
                var indexA = start + n;
                var indexB = (indexA + 1) % (sideCount - 1);
                var indexC = (indexB + 1) % (sideCount - 1);

                var vertexA = data[indexA].position;
                var vertexB = data[indexB].position;
                var vertexC = data[indexC].position;

                var vectorAB = vertexB - vertexA;
                var vectorBC = vertexC - vertexB;

                var cross = math.cross(vectorAB, vectorBC);
                var crossNormalized = math.normalizesafe(cross);
                var dot = math.dot(normal, crossNormalized);

                if (dot < 0)
                    isNegative = true;
                else if (dot > 0)
                    isPositive = true;

                if (isNegative && isPositive)
                {
                    s_IsConvex = false;
                    return;
                }
            }

            s_IsConvex = true;
        }

        /// <summary>
        /// Calculate the vertex and index count required for an extruded mesh.
        /// Use this method to allocate attribute and index buffers for use with Extrude.
        /// </summary>
        /// <param name="closeRing">Whether the extruded vertex ring is open (has a start and end point) or closed (forms an unbroken loop).</param>
        /// <param name="vertexCount">The number of vertices required for an extruded mesh using the provided settings.</param>
        /// <param name="indexCount">The number of indices required for an extruded mesh using the provided settings.</param>
        /// <param name="sides">How many sides make up the radius of the mesh.</param>
        /// <param name="segments">How many sections compose the length of the mesh.</param>
        /// <param name="capped">Whether the start and end of the mesh is filled. This setting is ignored when spline is closed.</param>
        /// <param name="closed">Whether the extruded mesh is closed or open. This can be separate from the Spline.Closed value.</param>
        /// <returns>Returns true if the computed vertex count exceeds 3 and the computed index count exceeds 5.</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool GetVertexAndIndexCount(int sides, int segments, bool capped, bool closed, bool closeRing, out int vertexCount, out int indexCount)
        {
            vertexCount = sides * (segments + (capped ? 2 : 0));
            indexCount = (closeRing ? sides : sides - 1) * 6 * (segments - (closed ? 0 : 1)) + (capped ? (sides - 2) * 3 * 2 : 0);
            // make sure we at least have enough vertices and indices for a quad
            return vertexCount > 3 && indexCount > 5;
        }

        /// <summary>
        /// Calculate the vertex and index count required for an extruded mesh.
        /// Use this method to allocate attribute and index buffers for use with Extrude.
        /// </summary>
        /// <param name="vertexCount">The number of vertices required for an extruded mesh using the provided settings.</param>
        /// <param name="indexCount">The number of indices required for an extruded mesh using the provided settings.</param>
        /// <param name="sides">How many sides make up the radius of the mesh.</param>
        /// <param name="segments">How many sections compose the length of the mesh.</param>
        /// <param name="range">
        /// The section of the Spline to extrude. This value expects a normalized interpolation start and end.
        /// I.e., [0,1] is the entire Spline, whereas [.5, 1] is the last half of the Spline.
        /// </param>
        /// <param name="capped">Whether the start and end of the mesh is filled. This setting is ignored when spline is closed.</param>
        /// <param name="closed">Whether the extruded mesh is closed or open. This can be separate from the Spline.Closed value.</param>
        public static void GetVertexAndIndexCount(
            int sides,
            int segments,
            bool capped,
            bool closed,
            Vector2 range,
            out int vertexCount, out int indexCount)
        {
            GetVertexAndIndexCount(sides, segments, capped, closed, true, out vertexCount, out indexCount);
        }

        static bool GetVertexAndIndexCount<T, K>(T spline, ExtrudeSettings<K> settings, out int vertexCount, out int indexCount)
            where T : ISpline
            where K : IExtrudeShape
        {
            return GetVertexAndIndexCount(settings.sides,
                settings.SegmentCount,
                settings.DoCapEnds(spline),
                settings.DoCloseSpline(spline),
                settings.wrapped,
                out vertexCount, out indexCount);
        }

        /// <summary>
        /// Extrude a mesh along a spline in a tube-like shape.
        /// </summary>
        /// <param name="spline">The spline to extrude.</param>
        /// <param name="mesh">A mesh that will be cleared and filled with vertex data for the shape.</param>
        /// <param name="radius">The radius of the extruded mesh.</param>
        /// <param name="sides">How many sides make up the radius of the mesh.</param>
        /// <param name="segments">How many sections compose the length of the mesh.</param>
        /// <param name="capped">Whether the start and end of the mesh is filled. This setting is ignored when spline is closed.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        public static void Extrude<T>(T spline, Mesh mesh, float radius, int sides, int segments, bool capped = true) where T : ISpline
        {
            Extrude(spline, mesh, radius, sides, segments, capped, new float2(0f, 1f));
        }

        /// <summary>
        /// Extrude a mesh along a spline with a customised shape.
        /// </summary>
        /// <param name="spline">The spline to extrude.</param>
        /// <param name="mesh">A mesh that will be cleared and filled with vertex data for the shape.</param>
        /// <param name="radius">The radius of the extruded mesh.</param>
        /// <param name="segments">How many sections compose the length of the mesh.</param>
        /// <param name="capped">Whether the start and end of the mesh is filled. This setting is ignored when spline is closed.</param>
        /// <param name="shape">The <see cref="IExtrudeShape"/> object defines the outline path of each segment.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <typeparam name="K">A type implementing <see cref="IExtrudeShape"/>.</typeparam>
        public static void Extrude<T, K>(T spline, Mesh mesh, float radius, int segments, bool capped, K shape)
            where T : ISpline
            where K : IExtrudeShape
        {
            Extrude(spline, mesh, radius, segments, capped, new float2(0f, 1f), shape);
        }

        /// <summary>
        /// Extrude a mesh along a spline in a tube-like shape.
        /// </summary>
        /// <param name="spline">The spline to extrude.</param>
        /// <param name="mesh">A mesh that will be cleared and filled with vertex data for the shape.</param>
        /// <param name="radius">The radius of the extruded mesh.</param>
        /// <param name="sides">How many sides make up the radius of the mesh.</param>
        /// <param name="segments">How many sections compose the length of the mesh.</param>
        /// <param name="capped">Whether the start and end of the mesh is filled. This setting is ignored when spline is closed.</param>
        /// <param name="range">
        /// The section of the Spline to extrude. This value expects a normalized interpolation start and end.
        /// I.e., [0,1] is the entire Spline, whereas [.5, 1] is the last half of the Spline.
        /// </param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        public static void Extrude<T>(T spline, Mesh mesh, float radius, int sides, int segments, bool capped, float2 range) where T : ISpline
        {
            s_DefaultShape.SideCount = sides;
            Extrude(spline, mesh, radius, segments, capped, range, s_DefaultShape);
        }

        /// <summary>
        /// Extrude a mesh along a spline following a shape template.
        /// </summary>
        /// <param name="spline">The spline to extrude.</param>
        /// <param name="mesh">A mesh that will be cleared and filled with vertex data for the shape.</param>
        /// <param name="radius">The radius of the extruded mesh.</param>
        /// <param name="segments">How many sections compose the length of the mesh.</param>
        /// <param name="capped">Whether the start and end of the mesh is filled. This setting is ignored when spline is closed.</param>
        /// <param name="range">
        /// The section of the Spline to extrude. This value expects a normalized interpolation start and end.
        /// I.e., [0,1] is the entire Spline, whereas [.5, 1] is the last half of the Spline.
        /// </param>
        /// <param name="shape">The <see cref="IExtrudeShape"/> object defines the outline path of each segment.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <typeparam name="K">A type implementing <see cref="IExtrudeShape"/>.</typeparam>
        public static void Extrude<T, K>(T spline, Mesh mesh, float radius, int segments, bool capped, float2 range,
            K shape)
            where T : ISpline
            where K : IExtrudeShape
        {
            var settings = new ExtrudeSettings<K>()
            {
                Radius = radius,
                CapEnds = capped,
                Range = range,
                SegmentCount = segments,
                Shape = shape
            };

            Extrude(spline, mesh, settings);
        }

        /// <summary>
        /// Extrude a mesh along a spline following a shape template.
        /// </summary>
        /// <param name="spline">The spline to extrude.</param>
        /// <param name="mesh">A mesh that will be cleared and filled with vertex data for the shape.</param>
        /// <param name="settings">The <see cref="ExtrudeSettings{T}"/> parameters used when creating mesh geometry.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        /// <typeparam name="K">A type implementing <see cref="IExtrudeShape"/>.</typeparam>
        /// <returns>Returns true if mesh was created, or false if the settings configuration resulted in an invalid state (ex, too few vertices).</returns>
        public static bool Extrude<T, K>(T spline, Mesh mesh, ExtrudeSettings<K> settings)
            where T : ISpline
            where K : IExtrudeShape
        {
            if (!GetVertexAndIndexCount(spline, settings, out var vertexCount, out var indexCount))
                return false;

            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var data = meshDataArray[0];

            var indexFormat = vertexCount >= ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            data.SetIndexBufferParams(indexCount, indexFormat);
            data.SetVertexBufferParams(vertexCount, k_PipeVertexAttribs);

            var vertices = data.GetVertexData<VertexData>();

            if (indexFormat == IndexFormat.UInt16)
            {
                var indices = data.GetIndexData<UInt16>();
                Extrude(spline, vertices, indices, settings);
            }
            else
            {
                var indices = data.GetIndexData<UInt32>();
                Extrude(spline, vertices, indices, settings);
            }

            mesh.Clear();
            data.subMeshCount = 1;
            data.SetSubMesh(0, new SubMeshDescriptor(0, indexCount));
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateBounds();

            return true;
        }

        /// <summary>
        /// Extrude a mesh along a list of splines in a tube-like shape.
        /// </summary>
        /// <param name="splines">The splines to extrude.</param>
        /// <param name="mesh">A mesh that will be cleared and filled with vertex data for the shape.</param>
        /// <param name="radius">The radius of the extruded mesh.</param>
        /// <param name="sides">How many sides make up the radius of the mesh.</param>
        /// <param name="segmentsPerUnit">The number of edge loops that comprise the length of one unit of the mesh.</param>
        /// <param name="capped">Whether the start and end of the mesh is filled. This setting is ignored when spline is closed.</param>
        /// <param name="range">
        /// The section of the Spline to extrude. This value expects a normalized interpolation start and end.
        /// I.e., [0,1] is the entire Spline, whereas [.5, 1] is the last half of the Spline.
        /// </param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        public static void Extrude<T>(IReadOnlyList<T> splines, Mesh mesh, float radius, int sides, float segmentsPerUnit, bool capped, float2 range) where T : ISpline
        {
            s_DefaultShape.SideCount = sides;

            var settings = new ExtrudeSettings<Circle>(s_DefaultShape)
            {
                Radius = radius,
                SegmentCount = (int) segmentsPerUnit,
                CapEnds = capped,
                Range = range
            };

            Extrude(splines, mesh, settings, segmentsPerUnit);
        }

        // this is not public for good reason. it mutates the settings object by necessity, to preserve the behaviour
        // of `segmentsPerUnit` rather than use the `Settings.SegmentCount` property.
        internal static void Extrude<T, K>(IReadOnlyList<T> splines, Mesh mesh, ExtrudeSettings<K> settings, float segmentsPerUnit)
            where T : ISpline
            where K : IExtrudeShape
        {
            mesh.Clear();
            if (splines == null)
            {
                if(Application.isPlaying)
                    Debug.LogError("Trying to extrude a spline mesh with no valid splines.");
                return;
            }

            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var data = meshDataArray[0];
            data.subMeshCount = 1;

            var totalVertexCount = 0;
            var totalIndexCount = 0;
            var splineMeshOffsets = new (int indexStart, int vertexStart)[splines.Count];

            int GetSegmentCount(T spline)
            {
                var span = Mathf.Abs(settings.Range.y - settings.Range.x);
                return Mathf.Max((int)Mathf.Ceil(spline.GetLength() * span * segmentsPerUnit), 1);
            }

            for (int i = 0; i < splines.Count; ++i)
            {
                if(splines[i].Count < 2)
                    continue;

                settings.SegmentCount = GetSegmentCount(splines[i]);
                GetVertexAndIndexCount(splines[i], settings, out int vertexCount, out int indexCount);
                splineMeshOffsets[i] = (totalIndexCount, totalVertexCount);
                totalVertexCount += vertexCount;
                totalIndexCount += indexCount;
            }

            var indexFormat = totalVertexCount >= ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;

            data.SetIndexBufferParams(totalIndexCount, indexFormat);
            data.SetVertexBufferParams(totalVertexCount, k_PipeVertexAttribs);

            var vertices = data.GetVertexData<VertexData>();
            if (indexFormat == IndexFormat.UInt16)
            {
                var indices = data.GetIndexData<UInt16>();
                for (int i = 0; i < splines.Count; ++i)
                {
                    if (splines[i].Count < 2)
                        continue;
                    settings.SegmentCount = GetSegmentCount(splines[i]);
                    Extrude(splines[i], vertices, indices, settings, splineMeshOffsets[i].vertexStart, splineMeshOffsets[i].indexStart);
                }
            }
            else
            {
                var indices = data.GetIndexData<UInt32>();
                for (int i = 0; i < splines.Count; ++i)
                {
                    if (splines[i].Count < 2)
                        continue;
                    settings.SegmentCount = GetSegmentCount(splines[i]);
                    Extrude(splines[i], vertices, indices, settings, splineMeshOffsets[i].vertexStart, splineMeshOffsets[i].indexStart);
                }
            }

            data.SetSubMesh(0, new SubMeshDescriptor(0, totalIndexCount));
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Extrude a mesh along a spline in a tube-like shape.
        /// </summary>
        /// <param name="spline">The spline to extrude.</param>
        /// <param name="vertices">A pre-allocated buffer of vertex data.</param>
        /// <param name="indices">A pre-allocated index buffer. Must be of type UInt16 or UInt32.</param>
        /// <param name="radius">The radius of the extruded mesh.</param>
        /// <param name="sides">How many sides make up the radius of the mesh.</param>
        /// <param name="segments">How many sections compose the length of the mesh.</param>
        /// <param name="capped">Whether the start and end of the mesh is filled. This setting is ignored when spline
        /// is closed.</param>
        /// <param name="range">
        /// The section of the Spline to extrude. This value expects a normalized interpolation start and end.
        /// I.e., [0,1] is the entire Spline, whereas [.5, 1] is the last half of the Spline.
        /// </param>
        /// <typeparam name="TSplineType">A type implementing ISpline.</typeparam>
        /// <typeparam name="TVertexType">A type implementing ISplineVertexData.</typeparam>
        /// <typeparam name="TIndexType">The mesh index format. Must be UInt16 or UInt32.</typeparam>
        /// <exception cref="ArgumentOutOfRangeException">An out of range exception is thrown if the vertex or index
        /// buffer lengths do not match the expected size. Use <see cref="GetVertexAndIndexCount"/> to calculate the
        /// expected buffer sizes.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An argument exception is thrown if {TIndexType} is not UInt16 or UInt32.
        /// </exception>
        public static void Extrude<TSplineType, TVertexType, TIndexType>(
            TSplineType spline,
            NativeArray<TVertexType> vertices,
            NativeArray<TIndexType> indices,
            float radius,
            int sides,
            int segments,
            bool capped,
            float2 range)
            where TSplineType : ISpline
            where TVertexType : struct, ISplineVertexData
            where TIndexType : struct
        {
            s_DefaultShape.SideCount = math.clamp(sides, k_SidesMin, k_SidesMax);
            Extrude(spline, vertices, indices, new ExtrudeSettings<Circle>(segments, capped, range, radius, s_DefaultShape));
        }

        static void Extrude<TSplineType, TVertexType, TIndexType, TShapeType>(
                TSplineType spline,
                NativeArray<TVertexType> vertices,
                NativeArray<TIndexType> indices,
                ExtrudeSettings<TShapeType> settings,
                int vertexArrayOffset = 0,
                int indicesArrayOffset = 0)
                where TSplineType : ISpline
                where TVertexType : struct, ISplineVertexData
                where TIndexType : struct
                where TShapeType : IExtrudeShape
        {
            var sides = settings.sides;
            var segments = settings.SegmentCount;
            var range = settings.Range;
            var capped = settings.DoCapEnds(spline);

            if (!GetVertexAndIndexCount(spline, settings, out var vertexCount, out var indexCount))
                return;

            if (settings.Shape == null)
                throw new ArgumentNullException(nameof(settings.Shape), "Shape template is null.");

            if (sides < 2)
                throw new ArgumentOutOfRangeException(nameof(sides), "Sides must be greater than 2");

            if (segments < 2)
                throw new ArgumentOutOfRangeException(nameof(segments), "Segments must be greater than 2");

            if (vertices.Length < vertexCount)
                throw new ArgumentOutOfRangeException($"Vertex array is incorrect size. Expected {vertexCount} or more, but received {vertices.Length}.");

            if (indices.Length < indexCount)
                throw new ArgumentOutOfRangeException($"Index array is incorrect size. Expected {indexCount} or more, but received {indices.Length}.");

            if (typeof(TIndexType) == typeof(UInt16))
            {
                var ushortIndices = indices.Reinterpret<UInt16>();
                WindTris(ushortIndices, spline, settings, vertexArrayOffset, indicesArrayOffset);
            }
            else if (typeof(TIndexType) == typeof(UInt32))
            {
                var ulongIndices = indices.Reinterpret<UInt32>();
                WindTris(ulongIndices, spline, settings, vertexArrayOffset, indicesArrayOffset);
            }
            else
            {
                throw new ArgumentException("Indices must be UInt16 or UInt32", nameof(indices));
            }

            var shape = settings.Shape;

            shape.Setup(spline, segments);

            s_IsConvexComputed = false;

            for (int i = 0; i < segments; ++i)
                ExtrudeRing(spline, settings, i, vertices, vertexArrayOffset + i * sides);

            if (capped)
            {
                var capVertexStart = vertexArrayOffset + segments * sides;
                var endCapVertexStart = vertexArrayOffset + (segments + 1) * sides;

                var rng = spline.Closed ? math.frac(range) : math.clamp(range, 0f, 1f);
                ExtrudeRing(spline, settings, 0, vertices, capVertexStart, true);
                ExtrudeRing(spline, settings, segments-1, vertices, endCapVertexStart, true);

                var beginAccel = math.normalize(spline.EvaluateTangent(rng.x));
                var accelLen = math.lengthsq(beginAccel);
                if (accelLen == 0f || float.IsNaN(accelLen))
                    beginAccel = math.normalize(spline.EvaluateTangent(rng.x + 0.0001f));
                var endAccel = math.normalize(spline.EvaluateTangent(rng.y));
                accelLen = math.lengthsq(endAccel);
                if (accelLen == 0f || float.IsNaN(accelLen))
                    endAccel = math.normalize(spline.EvaluateTangent(rng.y - 0.0001f));

                for (int i = 0; i < sides; ++i)
                {
                    var v0 = vertices[capVertexStart + i];
                    var v1 = vertices[endCapVertexStart + i];

                    v0.normal = -beginAccel;
                    v1.normal = endAccel;

                    vertices[capVertexStart + i] = v0;
                    vertices[endCapVertexStart + i] = v1;
                }
            }
        }

        // Two overloads for winding triangles because there is no generic constraint for UInt{16, 32}
        static void WindTris<T, K>(NativeArray<UInt16> indices, T spline, ExtrudeSettings<K> settings, int vertexArrayOffset = 0, int indexArrayOffset = 0)
            where T : ISpline
            where K : IExtrudeShape
        {
            var closed = settings.DoCloseSpline(spline);
            var segments = settings.SegmentCount;
            var sides = settings.sides;
            var wrap = settings.wrapped;
            var capped = settings.DoCapEnds(spline);
            var sideFaceCount = wrap ? sides : sides - 1;

            for (int i = 0; i < (closed ? segments : segments - 1); ++i)
            {
                for (int n = 0; n < (wrap ? sides : sides - 1); ++n)
                {
                    var index0 = vertexArrayOffset + i * sides + n;
                    var index1 = vertexArrayOffset + i * sides + ((n + 1) % sides);
                    var index2 = vertexArrayOffset + ((i+1) % segments) * sides + n;
                    var index3 = vertexArrayOffset + ((i+1) % segments) * sides + ((n + 1) % sides);

                    indices[indexArrayOffset + i * sideFaceCount * 6 + n * 6 + 0] = (UInt16) index0;
                    indices[indexArrayOffset + i * sideFaceCount * 6 + n * 6 + 1] = (UInt16) index1;
                    indices[indexArrayOffset + i * sideFaceCount * 6 + n * 6 + 2] = (UInt16) index2;
                    indices[indexArrayOffset + i * sideFaceCount * 6 + n * 6 + 3] = (UInt16) index1;
                    indices[indexArrayOffset + i * sideFaceCount * 6 + n * 6 + 4] = (UInt16) index3;
                    indices[indexArrayOffset + i * sideFaceCount * 6 + n * 6 + 5] = (UInt16) index2;
                }
            }

            if (capped)
            {
                var capVertexStart = vertexArrayOffset + segments * sides;
                var capIndexStart = indexArrayOffset + sideFaceCount * 6 * (segments-1);
                var endCapVertexStart = vertexArrayOffset + (segments + 1) * sides;
                var endCapIndexStart = indexArrayOffset + (segments-1) * 6 * sideFaceCount + (sides-2) * 3;

                for(ushort i = 0; i < sides - 2; ++i)
                {
                    indices[capIndexStart + i * 3 + 0] = (UInt16)(capVertexStart);
                    indices[capIndexStart + i * 3 + 1] = (UInt16)(capVertexStart + i + 2);
                    indices[capIndexStart + i * 3 + 2] = (UInt16)(capVertexStart + i + 1);

                    indices[endCapIndexStart + i * 3 + 0] = (UInt16) (endCapVertexStart);
                    indices[endCapIndexStart + i * 3 + 1] = (UInt16) (endCapVertexStart + i + 1);
                    indices[endCapIndexStart + i * 3 + 2] = (UInt16) (endCapVertexStart + i + 2);
                }
            }
        }

        // Two overloads for winding triangles because there is no generic constraint for UInt{16, 32}
        static void WindTris<T, K>(NativeArray<UInt32> indices, T spline, ExtrudeSettings<K> settings, int vertexArrayOffset = 0, int indexArrayOffset = 0)
            where T : ISpline
            where K : IExtrudeShape
        {
            var closed = settings.DoCloseSpline(spline);
            var segments = settings.SegmentCount;
            var sides = settings.sides;
            var wrap = settings.wrapped;
            var capped = settings.DoCapEnds(spline);
            var sideFaceCount = wrap ? sides : sides - 1;

            for (int i = 0; i < (closed ? segments : segments - 1); ++i)
            {
                for (int n = 0; n < (wrap ? sides : sides - 1); ++n)
                {
                    var index0 = vertexArrayOffset + i * sides + n;
                    var index1 = vertexArrayOffset + i * sides + ((n + 1) % sides);
                    var index2 = vertexArrayOffset + ((i+1) % segments) * sides + n;
                    var index3 = vertexArrayOffset + ((i+1) % segments) * sides + ((n + 1) % sides);

                    indices[indexArrayOffset + i * sideFaceCount * 6 + n * 6 + 0] = (UInt16) index0;
                    indices[indexArrayOffset + i * sideFaceCount * 6 + n * 6 + 1] = (UInt16) index1;
                    indices[indexArrayOffset + i * sideFaceCount * 6 + n * 6 + 2] = (UInt16) index2;
                    indices[indexArrayOffset + i * sideFaceCount * 6 + n * 6 + 3] = (UInt16) index1;
                    indices[indexArrayOffset + i * sideFaceCount * 6 + n * 6 + 4] = (UInt16) index3;
                    indices[indexArrayOffset + i * sideFaceCount * 6 + n * 6 + 5] = (UInt16) index2;
                }
            }

            if (capped)
            {
                var capVertexStart = vertexArrayOffset + segments * sides;
                var capIndexStart = indexArrayOffset + sideFaceCount * 6 * (segments-1);
                var endCapVertexStart = vertexArrayOffset + (segments + 1) * sides;
                var endCapIndexStart = indexArrayOffset + (segments-1) * 6 * sideFaceCount + (sides-2) * 3;

                for(ushort i = 0; i < sides - 2; ++i)
                {
                    indices[capIndexStart + i * 3 + 0] = (UInt16)(capVertexStart);
                    indices[capIndexStart + i * 3 + 1] = (UInt16)(capVertexStart + i + 2);
                    indices[capIndexStart + i * 3 + 2] = (UInt16)(capVertexStart + i + 1);

                    indices[endCapIndexStart + i * 3 + 0] = (UInt16) (endCapVertexStart);
                    indices[endCapIndexStart + i * 3 + 1] = (UInt16) (endCapVertexStart + i + 1);
                    indices[endCapIndexStart + i * 3 + 2] = (UInt16) (endCapVertexStart + i + 2);
                }
            }
        }
    }
}
