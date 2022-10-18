#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Splines;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Interpolators = UnityEngine.Splines.Interpolators;

namespace Unity.Splines.Examples
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SplineContainer), typeof(MeshRenderer), typeof(MeshFilter))]
    public class MultipleRoadBehaviour : MonoBehaviour
    {
        [SerializeField]
        SplineContainer m_Spline;

        [SerializeField]
        int m_SegmentsPerMeter = 1;

        [SerializeField]
        Mesh m_Mesh;

        [SerializeField]
        float m_TextureScale = 1f;

        public IReadOnlyList<Spline> RoadSplines
        {
            get
            {
                if (m_Spline == null) m_Spline = GetComponent<SplineContainer>();
                if (m_Spline == null) return null;
                return m_Spline.Splines;
            }
        }

        public Mesh RoadsMesh
        {
            get
            {
                if (m_Mesh != null)
                    return m_Mesh;

                m_Mesh = new Mesh();
                GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Road");
                return m_Mesh;
            }
        }
        
        public int SegmentsPerMeter => Mathf.Min(10, Mathf.Max(1, m_SegmentsPerMeter));

        List<Vector3> m_Positions = new List<Vector3>();
        List<Vector3> m_Normals = new List<Vector3>();
        List<Vector2> m_Textures = new List<Vector2>();
        List<int> m_Indices = new List<int>();

        public void OnEnable()
        {
            //Avoid to point to an existing instance when duplicating the GameObject
            if (m_Mesh != null)
                m_Mesh = null;

            CreateRoads();
#if UNITY_EDITOR
            EditorSplineUtility.AfterSplineWasModified += OnAfterSplineWasModified;
            EditorSplineUtility.RegisterSplineDataChanged<float>(OnAfterSplineDataWasModified);
            Undo.undoRedoPerformed += CreateRoads;
#endif
        }

        public void OnDisable()
        {
#if UNITY_EDITOR
            EditorSplineUtility.AfterSplineWasModified -= OnAfterSplineWasModified;
            EditorSplineUtility.UnregisterSplineDataChanged<float>(OnAfterSplineDataWasModified);
            Undo.undoRedoPerformed -= CreateRoads;
#endif

            if (m_Mesh != null)
#if  UNITY_EDITOR
                DestroyImmediate(m_Mesh);
#else
                Destroy(m_Mesh);
#endif
        }

        void OnAfterSplineWasModified(Spline s)
        {
            if (m_Spline == null)
            {
                return;
            }
            if (RoadSplines.Contains(s))
            {
                CreateRoads();
            }
        }

        void OnAfterSplineDataWasModified(SplineData<float> splineData)
        {
            if (m_Spline == null)
            {
                return;
            }
            CreateRoads();
        }

        public void CreateRoads()
        {
            RoadsMesh.Clear();
            m_Positions.Clear();
            m_Normals.Clear();
            m_Textures.Clear();
            m_Indices.Clear();
            
            foreach (var spl in RoadSplines)
            {
                CreateRoad(spl);
            }

            RoadsMesh.SetVertices(m_Positions);
            RoadsMesh.SetNormals(m_Normals);
            RoadsMesh.SetUVs(0, m_Textures);
            RoadsMesh.subMeshCount = 1;
            RoadsMesh.SetIndices(m_Indices, MeshTopology.Triangles, 0);
            RoadsMesh.UploadMeshData(false);

            GetComponent<MeshFilter>().sharedMesh = m_Mesh;
        }
        
        void CreateRoad(Spline roadSpline)
        {
            if (roadSpline == null || roadSpline.Count < 2)
                return;

            float length = roadSpline.GetLength();

            if (length < 1)
                return;

            int segments = (int)(SegmentsPerMeter * length);
            int vertexCount = segments * 2, triangleCount = (roadSpline.Closed ? segments : segments - 1) * 6;

            int prevVertexCount = m_Positions.Count;
            m_Positions.Capacity += vertexCount;
            m_Normals.Capacity += vertexCount;
            m_Textures.Capacity += vertexCount;
            m_Indices.Capacity += triangleCount;

            for (int i = 0; i < segments; i++)
            {
                var index = i / (segments - 1f);
                var control = SplineUtility.EvaluatePosition(roadSpline, index);
                var dir = SplineUtility.EvaluateTangent(roadSpline, index);
                var up = SplineUtility.EvaluateUpVector(roadSpline, index);

                var scale = transform.lossyScale;
                //var tangent = math.normalize((float3)math.mul(math.cross(up, dir), new float3(1f / scale.x, 1f / scale.y, 1f / scale.z)));
                var tangent = math.normalize(math.cross(up, dir)) * new float3(1f / scale.x, 1f / scale.y, 1f / scale.z);

                var w = 1f;

                m_Positions.Add(control - (tangent * w));
                m_Positions.Add(control + (tangent * w));
                m_Normals.Add(Vector3.up);
                m_Normals.Add(Vector3.up);
                m_Textures.Add(new Vector2(0f, index * m_TextureScale));
                m_Textures.Add(new Vector2(1f, index * m_TextureScale));
            }

            for (int i = 0, n = prevVertexCount; i < triangleCount; i += 6, n += 2)
            {
                m_Indices.Add((n + 2) % (prevVertexCount + vertexCount));
                m_Indices.Add((n + 1) % (prevVertexCount + vertexCount));
                m_Indices.Add((n + 0) % (prevVertexCount + vertexCount));
                m_Indices.Add((n + 2) % (prevVertexCount + vertexCount));
                m_Indices.Add((n + 3) % (prevVertexCount + vertexCount));
                m_Indices.Add((n + 1) % (prevVertexCount + vertexCount));
            }
        }
    }
}
