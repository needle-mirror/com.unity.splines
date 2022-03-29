using System;
using System.IO;
using UnityEngine;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A component for creating a tube mesh from a Spline at runtime.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("Splines/Spline Extrude")]
    public class SplineExtrude : MonoBehaviour
    {
        [SerializeField, Tooltip("The Spline to extrude.")]
        SplineContainer m_Container;

        [SerializeField, Tooltip("Enable to regenerate the extruded mesh when the target Spline is modified. Disable " +
             "this option if the Spline will not be modified at runtime.")]
        bool m_RebuildOnSplineChange;

        [SerializeField, Tooltip("The maximum number of times per-second that the mesh will be rebuilt.")]
        int m_RebuildFrequency = 30;

        [SerializeField, Tooltip("Automatically update any Mesh, Box, or Sphere collider components when the mesh is extruded.")]
        bool m_UpdateColliders = true;

        [SerializeField, Tooltip("The number of sides that comprise the radius of the mesh.")]
        int m_Sides = 8;

        [SerializeField, Tooltip("The number of edge loops that comprise the length of one unit of the mesh. The " +
             "total number of sections is equal to \"Spline.GetLength() * segmentsPerUnit\".")]
        float m_SegmentsPerUnit = 4;

        [SerializeField, Tooltip("Indicates if the start and end of the mesh are filled. When the Spline is closed this setting is ignored.")]
        bool m_Capped = true;

        [SerializeField, Tooltip("The radius of the extruded mesh.")]
        float m_Radius = .25f;

        [SerializeField, Tooltip("The section of the Spline to extrude.")]
        Vector2 m_Range = new Vector2(0f, 1f);

        Spline m_Spline;
        Mesh m_Mesh;
        bool m_RebuildRequested;
        float m_NextScheduledRebuild;

        /// <summary>The SplineContainer of the <see cref="Spline"/> to extrude.</summary>
        public SplineContainer container
        {
            get => m_Container;
            set => m_Container = value;
        }

        /// <summary>
        /// Enable to regenerate the extruded mesh when the target Spline is modified. Disable this option if the Spline
        /// will not be modified at runtime.
        /// </summary>
        public bool rebuildOnSplineChange
        {
            get => m_RebuildOnSplineChange;
            set => m_RebuildOnSplineChange = value;
        }

        /// <summary>The maximum number of times per-second that the mesh will be rebuilt.</summary>
        public int rebuildFrequency
        {
            get => m_RebuildFrequency;
            set => m_RebuildFrequency = Mathf.Max(value, 1);
        }

        /// <summary>How many sides make up the radius of the mesh.</summary>
        public int sides
        {
            get => m_Sides;
            set => m_Sides = Mathf.Max(value, 3);
        }

        /// <summary>How many edge loops comprise the one unit length of the mesh.</summary>
        public float segmentsPerUnit
        {
            get => m_SegmentsPerUnit;
            set => m_SegmentsPerUnit = Mathf.Max(value, .0001f);
        }

        /// <summary>Whether the start and end of the mesh is filled. This setting is ignored when spline is closed.</summary>
        public bool capped
        {
            get => m_Capped;
            set => m_Capped = value;
        }

        /// <summary>The radius of the extruded mesh.</summary>
        public float radius
        {
            get => m_Radius;
            set => m_Radius = Mathf.Max(value, .00001f);
        }

        /// <summary>
        /// The section of the Spline to extrude.
        /// </summary>
        public Vector2 range
        {
            get => m_Range;
            set => m_Range = new Vector2(Mathf.Min(value.x, value.y), Mathf.Max(value.x, value.y));
        }

        /// <summary>The Spline to extrude.</summary>
        public Spline spline
        {
            get
            {
                // m_Spline is cached in the Start() method, meaning that it is not valid in the Editor.
#if UNITY_EDITOR
                return m_Container.Spline;
#else
                return m_Spline;
#endif
            }
        }

        void Reset()
        {
            TryGetComponent(out m_Container);

            if (TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                m_Mesh = filter.sharedMesh;
            else
                filter.sharedMesh = m_Mesh = CreateMeshAsset();

            if (TryGetComponent<MeshRenderer>(out var renderer) && renderer.sharedMaterial == null)
            {
                // todo Make Material.GetDefaultMaterial() public
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var mat = cube.GetComponent<MeshRenderer>().sharedMaterial;
                DestroyImmediate(cube);
                renderer.sharedMaterial = mat;
            }

            Rebuild();
        }

        void Start()
        {
            if (m_Container == null || (m_Spline = m_Container.Spline) == null)
            {
                Debug.LogError("Spline Extrude does not have a valid SplineContainer set.");
                return;
            }

            if((m_Mesh = GetComponent<MeshFilter>().sharedMesh) == null)
                Debug.LogError("SplineExtrude.createMeshInstance is disabled, but there is no valid mesh assigned. " +
                    "Please create or assign a writable mesh asset.");

            if (m_RebuildOnSplineChange)
                m_Spline.changed += () => m_RebuildRequested = true;

            Rebuild();
        }

        void Update()
        {
            if(m_RebuildRequested && Time.time >= m_NextScheduledRebuild)
                Rebuild();
        }

        /// <summary>
        /// Triggers the rebuild of a Spline's extrusion mesh and collider.
        /// </summary>
        public void Rebuild()
        {
            if(m_Mesh == null && (m_Mesh = GetComponent<MeshFilter>().sharedMesh) == null)
                return;

            float span = Mathf.Abs(range.y - range.x);
            int segments = Mathf.Max((int)Mathf.Ceil(spline.GetLength() * span * m_SegmentsPerUnit), 1);
            SplineMesh.Extrude(spline, m_Mesh, m_Radius, m_Sides, segments, m_Capped, m_Range);
            m_NextScheduledRebuild = Time.time + 1f / m_RebuildFrequency;

            if (m_UpdateColliders)
            {
                if (TryGetComponent<MeshCollider>(out var meshCollider))
                    meshCollider.sharedMesh = m_Mesh;

                if (TryGetComponent<BoxCollider>(out var boxCollider))
                {
                    boxCollider.center = m_Mesh.bounds.center;
                    boxCollider.size = m_Mesh.bounds.size;
                }

                if (TryGetComponent<SphereCollider>(out var sphereCollider))
                {
                    sphereCollider.center = m_Mesh.bounds.center;
                    var ext = m_Mesh.bounds.extents;
                    sphereCollider.radius = Mathf.Max(ext.x, ext.y, ext.z);
                }
            }
        }

        void OnValidate()
        {
            Rebuild();
        }

        internal Mesh CreateMeshAsset()
        {
            var mesh = new Mesh();
            mesh.name = name;

#if UNITY_EDITOR
            var scene = SceneManagement.SceneManager.GetActiveScene();
            var sceneDataDir = "Assets";

            if (!string.IsNullOrEmpty(scene.path))
            {
                var dir = Path.GetDirectoryName(scene.path);
                sceneDataDir = $"{dir}/{Path.GetFileNameWithoutExtension(scene.path)}";
                if (!Directory.Exists(sceneDataDir))
                    Directory.CreateDirectory(sceneDataDir);
            }

            var path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"{sceneDataDir}/{mesh.name}.asset");
            UnityEditor.AssetDatabase.CreateAsset(mesh, path);
            mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
            UnityEditor.EditorGUIUtility.PingObject(mesh);
#endif
            return mesh;
        }
    }
}
