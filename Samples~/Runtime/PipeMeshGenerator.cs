using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(SplineContainer),typeof(MeshRenderer),
    typeof(MeshFilter))]
public class PipeMeshGenerator: MonoBehaviour
{
    SplineContainer m_Spline;

    public Material m_PreviewMaterial = null;

    Material previewMaterial
    {
        get
        {
            return m_PreviewMaterial != null ? m_PreviewMaterial : defaultMaterial;
        }
        
        set
        {
            if(m_PreviewMaterial != value)
            {
                m_PreviewMaterial = value;

                if(TryGetComponent(out MeshRenderer renderer))
                    renderer.sharedMaterial = previewMaterial;
            }
        }
    }

    Material m_DefaultMaterial = null;
    Material defaultMaterial
    {
        get
        {
            if(m_DefaultMaterial == null)
            {            
                GameObject tmpPrimitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
                m_DefaultMaterial = new Material(tmpPrimitive.GetComponent<MeshRenderer>().sharedMaterial);
                
#if UNITY_EDITOR
                EditorApplication.delayCall += () => DestroyImmediate(tmpPrimitive);
#endif
            }

            return m_DefaultMaterial;
        }
    }
    
    [Min(0.001f)]
    public float m_Radius = 2f;
    
    [Min(0.001f)]
    public float m_SegmentsLength = .5f;
    
    [Min(3)]
    public int m_SidesCount = 8;

    public bool m_GenerateMeshCollider = true;

    
    public float m_TextureScale = 1f;

    Spline spline
    {
        get
        {
            if (m_Spline == null)
                m_Spline = GetComponent<SplineContainer>();

            if(m_Spline == null)
                return null;
            
            return m_Spline.Spline;
        }
    }

    Mesh m_Mesh;
    Mesh mesh
    {
        get
        {
            if (m_Mesh != null)
                return m_Mesh;

            m_Mesh = new Mesh();
            GetComponent<MeshRenderer>().sharedMaterial = previewMaterial;
            return m_Mesh;
        }
    }

    MeshFilter m_MeshFilter;
    MeshFilter meshFilter
    {
        get
        {
            if(m_MeshFilter == null)
                m_MeshFilter = GetComponent<MeshFilter>();

            return m_MeshFilter;
        }
    }

    MeshCollider m_MeshCollider;
    MeshCollider meshCollider
    {
        get
        {
            if (m_MeshCollider != null)
                return m_MeshCollider;
    
            if(!gameObject.TryGetComponent(out m_MeshCollider))
                m_MeshCollider = gameObject.AddComponent<MeshCollider>();
            
            return m_MeshCollider;
        }
    }
    
    List<Vector3> m_Positions = new List<Vector3>();
    List<Vector3> m_Normals = new List<Vector3>();
    List<Vector2> m_Textures = new List<Vector2>();
    List<int> m_Indices = new List<int>();

    void OnEnable()
    {
        m_Spline = GetComponent<SplineContainer>();
#if UNITY_EDITOR
        Undo.undoRedoPerformed += UndoRedoPerformed;
        EditorSplineUtility.afterSplineWasModified += OnSplineUpdate;
#endif
    }

    void UndoRedoPerformed()
    {
        UpdatePipe();
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        Undo.undoRedoPerformed -= UndoRedoPerformed;
        EditorSplineUtility.afterSplineWasModified -= OnSplineUpdate;
#endif
    }

    public void OnValidate()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall += () => UpdatePipe();
#endif
    }

    public void OnDestroy()
    {
        DestroyImmediate(m_Mesh);
    }

    void OnSplineUpdate(Spline s)
    {
        if(s == spline)
            UpdatePipe();
    }

    public void UpdatePipe()
    {
        if (m_Spline == null || m_Spline.Spline == null)
            return;

        if (m_Spline.Spline == null || m_Spline.Spline.Count < 2)
            return;

        mesh.Clear();

        float length = spline.GetLength();

        if (length < 1)
            return;

        var circlePositions = GetCirclePositions();
        int segments = (int)(length / m_SegmentsLength);
        int vertexCount = segments * m_SidesCount;
        int triangleCount = (spline.Closed ? segments : segments - 1) * 6 * m_SidesCount;
        int sidesTriangleCount = triangleCount;
        if(!spline.Closed)
        {
            vertexCount += 2;
            triangleCount += 3 * m_SidesCount * 2;
        }
        
        m_Positions.Clear();
        m_Normals.Clear();
        m_Textures.Clear();
        m_Indices.Clear();

        m_Positions.Capacity = vertexCount;
        m_Normals.Capacity = vertexCount;
        m_Textures.Capacity = vertexCount;
        m_Indices.Capacity = triangleCount;

        float3 center, tangent, up;
        for (int i = 0; i < segments; i++)
        {
            var index = i / (segments - 1f);
            spline.Evaluate(index, out center, out tangent, out up);
            up = math.normalize(up);

            if(math.length(tangent) == 0f)
                tangent = new float3(0, 0, 1f);
                
            var normal = math.normalize(math.cross(up, tangent));

            for(int j = 0; j < m_SidesCount; j++)
            {
                var circlePos = circlePositions[j];
                var circleDir = circlePos.x * normal + circlePos.y * up;
                m_Positions.Add(center + circleDir * m_Radius);
                m_Normals.Add(circlePos.x * Vector3.right + circlePos.y * Vector3.up);
                m_Textures.Add(new Vector2(circlePos.x, math.frac(index * m_TextureScale)));
            }
        }
        
        for (int i = 0, n = 0; i < sidesTriangleCount; i += 6, n += 1)
        {
            var nextIndex = (n+1) % m_SidesCount == 0 ? n+1-m_SidesCount: n+1;
            m_Indices.Add((n) % vertexCount);
            m_Indices.Add((nextIndex) % vertexCount);
            m_Indices.Add((n + m_SidesCount) % vertexCount);
            m_Indices.Add((nextIndex) % vertexCount);
            m_Indices.Add((nextIndex + m_SidesCount) % vertexCount);
            m_Indices.Add((n + m_SidesCount) % vertexCount);
        }

        if(!spline.Closed)
        {
            center = spline.EvaluatePosition(0);
            m_Positions.Add(center);
            m_Normals.Add(Vector3.back);
            m_Textures.Add(new Vector2(0,0));

            var vertexIndex = m_Positions.Count - 1;
            for(int i = 0; i < m_SidesCount; i++)
            {
                m_Indices.Add(vertexIndex);
                m_Indices.Add((i+1) % m_SidesCount);
                m_Indices.Add(i);
            }
            
            center = spline.EvaluatePosition(1);
            m_Positions.Add(center);
            m_Normals.Add(Vector3.forward);
            m_Textures.Add(new Vector2(1,1));

            vertexIndex = m_Positions.Count - 1;
            var lastSideVertexIndex = m_Positions.Count - 3;
            for(int i = 0; i < m_SidesCount; i++)
            {
                m_Indices.Add(vertexIndex);
                m_Indices.Add(lastSideVertexIndex - ((i+1) % m_SidesCount));
                m_Indices.Add(lastSideVertexIndex - i);
            }
        }

        mesh.SetVertices(m_Positions);
        mesh.SetNormals(m_Normals);
        mesh.SetUVs(0, m_Textures);
        mesh.subMeshCount = 1;
        mesh.SetIndices(m_Indices, MeshTopology.Triangles, 0);
        mesh.UploadMeshData(false);

        meshFilter.sharedMesh = m_Mesh;

        if(m_GenerateMeshCollider)
        {
            try {meshCollider.sharedMesh = m_Mesh;}
            catch(Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        else
        {
            if(m_MeshCollider != null)
#if UNITY_EDITOR                
                EditorApplication.delayCall += () => DestroyImmediate(m_MeshCollider);
#else
                Destroy(m_MeshCollider);
#endif
        }
    }
    
    Vector2[] GetCirclePositions()
    {
        Vector2[] positions = new Vector2[m_SidesCount];

        for(int i = 0; i < m_SidesCount; i++)
        {
            var radAngle = Mathf.Deg2Rad * i * 360f / (float)m_SidesCount;
            positions[i] = new Vector2(Mathf.Cos(radAngle), Mathf.Sin(radAngle));
        }
        
        return positions;
    }
}
