#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;
using Interpolators = UnityEngine.Splines.Interpolators;

namespace Unity.Splines.Examples
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SplineContainer), typeof(MeshRenderer), typeof(MeshFilter))]
    public class LoftRoadBehaviour : MonoBehaviour
    {
        const string k_GeneratedMeshDirectory = "Assets/Generated/Roads";

        [SerializeField]
        SplineContainer m_Spline;

        [SerializeField]
        int m_SegmentsPerMeter = 1;

        [SerializeField]
        Mesh m_Mesh;

        [SerializeField]
        float m_TextureScale = 1f;

        [SerializeField]
        [SplineDataDrawer(typeof(CustomWidthHandle))]
        SplineData<float> m_Width = new SplineData<float>(new [] { new Keyframe<float>(0f, .5f) });

        public Spline spline
        {
            get
            {
                if (m_Spline == null)
                    m_Spline = GetComponent<SplineContainer>();
                return m_Spline.Spline;
            }
        }

        public Mesh mesh
        {
            get
            {
                if (m_Mesh != null)
                    return m_Mesh;

                m_Mesh = new Mesh();

#if UNITY_EDITOR
                Directory.CreateDirectory(k_GeneratedMeshDirectory);
                var path = AssetDatabase.GenerateUniqueAssetPath($"{k_GeneratedMeshDirectory}/{name}.asset");
                AssetDatabase.CreateAsset(m_Mesh, path);
                AssetDatabase.ImportAsset(path);
#endif
                return m_Mesh;
            }
        }

        public int segmentsPerMeter => Mathf.Min(10, Mathf.Max(1, m_SegmentsPerMeter));

        public SplineData<float> width
        {
            get => m_Width;
            set => m_Width = value;
        }

        List<Vector3> m_Positions = new List<Vector3>();
        List<Vector3> m_Normals = new List<Vector3>();
        List<Vector2> m_Textures = new List<Vector2>();
        List<int> m_Indices = new List<int>();

        public void OnEnable()
        {
            Loft();
        }

        public void Loft()
        {
            if (m_Spline == null || m_Spline.Spline == null)
            {
                Debug.LogError("Cannot loft road mesh because Spline reference is null");
                return;
            }

            if (m_Spline.Spline == null || m_Spline.Spline.KnotCount < 2)
                return;

            mesh.Clear();

            float length = spline.GetLength();

            if (length < 1)
                return;

            int segments = (int)(segmentsPerMeter * length);
            int vertexCount = segments * 2, triangleCount = (spline.Closed ? segments : segments - 1) * 6;

            m_Positions.Clear();
            m_Normals.Clear();
            m_Textures.Clear();
            m_Indices.Clear();

            m_Positions.Capacity = vertexCount;
            m_Normals.Capacity = vertexCount;
            m_Textures.Capacity = vertexCount;
            m_Indices.Capacity = triangleCount;

            for (int i = 0; i < segments; i++)
            {
                var index = i / (segments - 1f);
                var control = SplineUtility.EvaluatePosition(spline, index);
                var dir = SplineUtility.EvaluateDirection(spline, index);
                var up = SplineUtility.EvaluateUpVector(spline, index);
                
                var tangent = math.normalize(math.cross(up, dir));
                var convertedTime = SplineUtility.GetConvertedTime(spline, index, PathIndexUnit.Normalized, m_Width.PathIndexUnit);
                var w = m_Width.Evaluate(spline, convertedTime, new Interpolators.LerpFloat());
                w = math.clamp(w, .001f, 10000f);

                m_Positions.Add(control - (tangent * w));
                m_Positions.Add(control + (tangent * w));
                m_Normals.Add(Vector3.up);
                m_Normals.Add(Vector3.up);
                m_Textures.Add(new Vector2(0f, index * m_TextureScale));
                m_Textures.Add(new Vector2(1f, index * m_TextureScale));
            }

            for (int i = 0, n = 0; i < triangleCount; i += 6, n += 2)
            {
                m_Indices.Add((n + 2) % vertexCount);
                m_Indices.Add((n + 1) % vertexCount);
                m_Indices.Add((n + 0) % vertexCount);
                m_Indices.Add((n + 2) % vertexCount);
                m_Indices.Add((n + 3) % vertexCount);
                m_Indices.Add((n + 1) % vertexCount);
            }

            mesh.SetVertices(m_Positions);
            mesh.SetNormals(m_Normals);
            mesh.SetUVs(0, m_Textures);
            mesh.subMeshCount = 1;
            mesh.SetIndices(m_Indices, MeshTopology.Triangles, 0);
            mesh.UploadMeshData(false);

            GetComponent<MeshFilter>().sharedMesh = m_Mesh;
        }
    }
}
