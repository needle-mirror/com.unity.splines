using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Utility methods for creating and working with meshes.
    /// </summary>
    public static class SplineMesh
    {
        const float k_RadiusMin = .00001f, k_RadiusMax = 10000f;
        const int k_SidesMin = 3, k_SidesMax = 2084;
        const int k_SegmentsMin = 2, k_SegmentsMax = 4096;

        static readonly VertexAttributeDescriptor[] k_PipeVertexAttribs = new VertexAttributeDescriptor[]
        {
            new (VertexAttribute.Position),
            new (VertexAttribute.Normal),
            new (VertexAttribute.TexCoord0, dimension: 2)
        };

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

        static void ExtrudeRing<T, K>(T spline, float t, NativeArray<K> data, int start, int count, float radius)
            where T : ISpline
            where K : struct, ISplineVertexData
        {
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
            var rad = math.radians(360f / count);

            for (int n = 0; n < count; ++n)
            {
                var vertex = new K();
                var p = new float3(math.cos(n * rad), math.sin(n * rad), 0f) * radius;
                vertex.position = sp + math.rotate(rot, p);
                vertex.normal = (vertex.position - (Vector3)sp).normalized;

                // instead of inserting a seam, wrap UVs using a triangle wave so that texture wraps back onto itself
                float ut = n / ((float)count + count%2);
                float u = math.abs(ut - math.floor(ut + .5f)) * 2f;
                vertex.texture = new Vector2(u, t * spline.GetLength());

                data[start + n] = vertex;
            }
        }

        // The logic around when caps and closing is a little complicated and easy to confuse. This wraps settings in a
        // consistent way so that methods aren't working with mixed data.
        struct Settings
        {
            public int sides { get; private set; }
            public int segments { get; private set; }
            public bool capped { get; private set; }
            public bool closed { get; private set; }
            public float2 range { get; private set; }
            public float radius { get; private set; }

            public Settings(int sides, int segments, bool capped, bool closed, float2 range, float radius)
            {
                this.sides = math.clamp(sides, k_SidesMin, k_SidesMax);
                this.segments = math.clamp(segments, k_SegmentsMin, k_SegmentsMax);
                this.range = new float2(math.min(range.x, range.y), math.max(range.x, range.y));
                this.closed = math.abs(1f - (this.range.y - this.range.x)) < float.Epsilon && closed;
                this.capped = capped && !this.closed;
                this.radius = math.clamp(radius, k_RadiusMin, k_RadiusMax);
            }
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
        public static void GetVertexAndIndexCount(int sides, int segments, bool capped, bool closed, Vector2 range, out int vertexCount, out int indexCount)
        {
            var settings = new Settings(sides, segments, capped, closed, range, 1f);
            GetVertexAndIndexCount(settings, out vertexCount, out indexCount);
        }

        static void GetVertexAndIndexCount(Settings settings, out int vertexCount, out int indexCount)
        {
            vertexCount = settings.sides * (settings.segments + (settings.capped ? 2 : 0));
            indexCount = settings.sides * 6 * (settings.segments - (settings.closed ? 0 : 1)) + (settings.capped ? (settings.sides - 2) * 3 * 2 : 0);
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
            var settings = new Settings(sides, segments, capped, spline.Closed, range, radius);
            GetVertexAndIndexCount(settings, out var vertexCount, out var indexCount);

            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var data = meshDataArray[0];

            var indexFormat = vertexCount >= ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            data.SetIndexBufferParams(indexCount, indexFormat);
            data.SetVertexBufferParams(vertexCount, k_PipeVertexAttribs);

            var vertices = data.GetVertexData<VertexData>();

            if (indexFormat == IndexFormat.UInt16)
            {
                var indices = data.GetIndexData<UInt16>();
                Extrude(spline, vertices, indices, radius, sides, segments, capped, range);
            }
            else
            {
                var indices = data.GetIndexData<UInt32>();
                Extrude(spline, vertices, indices, radius, sides, segments, capped, range);
            }

            mesh.Clear();
            data.subMeshCount = 1;
            data.SetSubMesh(0, new SubMeshDescriptor(0, indexCount));
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateBounds();
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
            var settings = new Settings[splines.Count];
            var span = Mathf.Abs(range.y - range.x);
            var splineMeshOffsets = new (int indexStart, int vertexStart)[splines.Count];
            for (int i = 0; i < splines.Count; ++i)
            {
                var spline = splines[i];
                
                var segments = Mathf.Max((int)Mathf.Ceil(spline.GetLength() * span * segmentsPerUnit), 1);
                settings[i] = new Settings(sides, segments, capped, spline.Closed, range, radius);
            
                GetVertexAndIndexCount(settings[i], out var vertexCount, out var indexCount);

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
                    Extrude(splines[i], vertices, indices, settings[i], splineMeshOffsets[i].vertexStart, splineMeshOffsets[i].indexStart);
            }
            else
            {
                var indices = data.GetIndexData<UInt32>();
                for (int i = 0; i < splines.Count; ++i)
                    Extrude(splines[i], vertices, indices, settings[i], splineMeshOffsets[i].vertexStart, splineMeshOffsets[i].indexStart);
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
            Extrude(spline, vertices, indices, new Settings(sides, segments, capped, spline.Closed, range, radius));
        }

        static void Extrude<TSplineType, TVertexType, TIndexType>(
                TSplineType spline,
                NativeArray<TVertexType> vertices,
                NativeArray<TIndexType> indices,
                Settings settings,
                int vertexArrayOffset = 0,
                int indicesArrayOffset = 0)
                where TSplineType : ISpline
                where TVertexType : struct, ISplineVertexData
                where TIndexType : struct
        {
            var radius = settings.radius;
            var sides = settings.sides;
            var segments = settings.segments;
            var range = settings.range;
            var capped = settings.capped;

            GetVertexAndIndexCount(settings, out var vertexCount, out var indexCount);

            if (sides < 3)
                throw new ArgumentOutOfRangeException(nameof(sides), "Sides must be greater than 3");

            if (segments < 2)
                throw new ArgumentOutOfRangeException(nameof(segments), "Segments must be greater than 2");
            
            if (vertices.Length < vertexCount)
                throw new ArgumentOutOfRangeException($"Vertex array is incorrect size. Expected {vertexCount} or more, but received {vertices.Length}.");

            if (indices.Length < indexCount)
                throw new ArgumentOutOfRangeException($"Index array is incorrect size. Expected {indexCount} or more, but received {indices.Length}.");

            if (typeof(TIndexType) == typeof(UInt16))
            {
                var ushortIndices = indices.Reinterpret<UInt16>();
                WindTris(ushortIndices, settings, vertexArrayOffset, indicesArrayOffset);
            }
            else if (typeof(TIndexType) == typeof(UInt32))
            {
                var ulongIndices = indices.Reinterpret<UInt32>();
                WindTris(ulongIndices, settings, vertexArrayOffset, indicesArrayOffset);
            }
            else
            {
                throw new ArgumentException("Indices must be UInt16 or UInt32", nameof(indices));
            }

            for (int i = 0; i < segments; ++i)
                ExtrudeRing(spline, math.lerp(range.x, range.y, i / (segments - 1f)), vertices, vertexArrayOffset + i * sides, sides, radius);

            if (capped)
            {
                var capVertexStart = vertexArrayOffset + segments * sides;
                var endCapVertexStart = vertexArrayOffset + (segments + 1) * sides;

                var rng = spline.Closed ? math.frac(range) : math.clamp(range, 0f, 1f);
                ExtrudeRing(spline, rng.x, vertices, capVertexStart, sides, radius);
                ExtrudeRing(spline, rng.y, vertices, endCapVertexStart, sides, radius);

                var beginAccel = math.normalize(spline.EvaluateTangent(rng.x));
                var accelLen = math.lengthsq(beginAccel);
                if (accelLen == 0f || float.IsNaN(accelLen))
                    beginAccel = math.normalize(spline.EvaluateTangent(rng.x + 0.0001f));
                var endAccel = math.normalize(spline.EvaluateTangent(rng.y));
                accelLen = math.lengthsq(endAccel);
                if (accelLen == 0f || float.IsNaN(accelLen))
                    endAccel = math.normalize(spline.EvaluateTangent(rng.y - 0.0001f));

                var rad = math.radians(360f / sides);
                var off = new float2(.5f, .5f);

                for (int i = 0; i < sides; ++i)
                {
                    var v0 = vertices[capVertexStart + i];
                    var v1 = vertices[endCapVertexStart + i];

                    v0.normal = -beginAccel;
                    v0.texture = off + new float2(math.cos(i * rad), math.sin(i * rad)) * .5f;

                    v1.normal = endAccel;
                    v1.texture = off + new float2(-math.cos(i * rad), math.sin(i * rad)) * .5f;

                    vertices[capVertexStart + i] = v0;
                    vertices[endCapVertexStart + i] = v1;
                }
            }
        }

        // Two overloads for winding triangles because there is no generic constraint for UInt{16, 32}
        static void WindTris(NativeArray<UInt16> indices, Settings settings, int vertexArrayOffset = 0, int indexArrayOffset = 0)
        {
            var closed = settings.closed;
            var segments = settings.segments;
            var sides = settings.sides;
            var capped = settings.capped;

            for (int i = 0; i < (closed ? segments : segments - 1); ++i)
            {
                for (int n = 0; n < sides; ++n)
                {
                    var index0 = vertexArrayOffset + i * sides + n;
                    var index1 = vertexArrayOffset + i * sides + ((n + 1) % sides);
                    var index2 = vertexArrayOffset + ((i+1) % segments) * sides + n;
                    var index3 = vertexArrayOffset + ((i+1) % segments) * sides + ((n + 1) % sides);

                    indices[indexArrayOffset + i * sides * 6 + n * 6 + 0] = (UInt16) index0;
                    indices[indexArrayOffset + i * sides * 6 + n * 6 + 1] = (UInt16) index1;
                    indices[indexArrayOffset + i * sides * 6 + n * 6 + 2] = (UInt16) index2;
                    indices[indexArrayOffset + i * sides * 6 + n * 6 + 3] = (UInt16) index1;
                    indices[indexArrayOffset + i * sides * 6 + n * 6 + 4] = (UInt16) index3;
                    indices[indexArrayOffset + i * sides * 6 + n * 6 + 5] = (UInt16) index2;
                }
            }

            if (capped)
            {
                var capVertexStart = vertexArrayOffset + segments * sides;
                var capIndexStart = indexArrayOffset + sides * 6 * (segments-1);
                var endCapVertexStart = vertexArrayOffset + (segments + 1) * sides;
                var endCapIndexStart = indexArrayOffset + (segments-1) * 6 * sides + (sides-2) * 3;

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
        static void WindTris(NativeArray<UInt32> indices, Settings settings, int vertexArrayOffset = 0, int indexArrayOffset = 0)
        {
            var closed = settings.closed;
            var segments = settings.segments;
            var sides = settings.sides;
            var capped = settings.capped;

            for (int i = 0; i < (closed ? segments : segments - 1); ++i)
            {
                for (int n = 0; n < sides; ++n)
                {
                    var index0 = vertexArrayOffset + i * sides + n;
                    var index1 = vertexArrayOffset + i * sides + ((n + 1) % sides);
                    var index2 = vertexArrayOffset + ((i+1) % segments) * sides + n;
                    var index3 = vertexArrayOffset + ((i+1) % segments) * sides + ((n + 1) % sides);

                    indices[indexArrayOffset + i * sides * 6 + n * 6 + 0] = (UInt32) index0;
                    indices[indexArrayOffset + i * sides * 6 + n * 6 + 1] = (UInt32) index1;
                    indices[indexArrayOffset + i * sides * 6 + n * 6 + 2] = (UInt32) index2;
                    indices[indexArrayOffset + i * sides * 6 + n * 6 + 3] = (UInt32) index1;
                    indices[indexArrayOffset + i * sides * 6 + n * 6 + 4] = (UInt32) index3;
                    indices[indexArrayOffset + i * sides * 6 + n * 6 + 5] = (UInt32) index2;
                }
            }

            if (capped)
            {
                var capVertexStart = vertexArrayOffset + segments * sides;
                var capIndexStart = indexArrayOffset + sides * 6 * (segments-1);
                var endCapVertexStart = vertexArrayOffset + (segments + 1) * sides;
                var endCapIndexStart = indexArrayOffset + (segments-1) * 6 * sides + (sides-2) * 3;

                for(ushort i = 0; i < sides - 2; ++i)
                {
                    indices[capIndexStart + i * 3 + 0] = (UInt32)(capVertexStart);
                    indices[capIndexStart + i * 3 + 1] = (UInt32)(capVertexStart + i + 2);
                    indices[capIndexStart + i * 3 + 2] = (UInt32)(capVertexStart + i + 1);

                    indices[endCapIndexStart + i * 3 + 0] = (UInt32) (endCapVertexStart);
                    indices[endCapIndexStart + i * 3 + 1] = (UInt32) (endCapVertexStart + i + 1);
                    indices[endCapIndexStart + i * 3 + 2] = (UInt32) (endCapVertexStart + i + 2);
                }
            }
        }
    }
}
