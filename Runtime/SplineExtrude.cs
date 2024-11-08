using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Splines.ExtrusionShapes;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A component for creating a tube mesh from a Spline at runtime.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("Splines/Spline Extrude")]
    [ExecuteAlways]
    public class SplineExtrude : MonoBehaviour
    {
        [SerializeField, Tooltip("The Spline to extrude.")]
        SplineContainer m_Container;

        [SerializeField, Tooltip("Enable to regenerate the extruded mesh when the target Spline is modified. Disable " +
             "this option if the Spline will not be modified at runtime.")]
        bool m_RebuildOnSplineChange = true;

        [SerializeField, Tooltip("The maximum number of times per-second that the mesh will be rebuilt.")]
        int m_RebuildFrequency = 30;

        [SerializeField, Tooltip("Automatically update any Mesh, Box, or Sphere collider components when the mesh is extruded.")]
#pragma warning disable 414
        bool m_UpdateColliders = true;
#pragma warning restore 414

        [SerializeField, Tooltip("The number of sides that comprise the radius of the mesh.")]
        int m_Sides = 8;

        [SerializeField, Tooltip("The number of edge loops that comprise the length of one unit of the mesh. The " +
             "total number of sections is equal to \"Spline.GetLength() * segmentsPerUnit\".")]
        float m_SegmentsPerUnit = 4;

        [SerializeField, Tooltip("Indicates if the start and end of the mesh are filled. When the target Spline is closed or when the profile of the shape to extrude is concave, this setting is ignored.")]
        bool m_Capped = true;

        [SerializeField, Tooltip("The radius of the extruded mesh.")]
        float m_Radius = .25f;

        [SerializeField, Tooltip("The section of the Spline to extrude.")]
        Vector2 m_Range = new Vector2(0f, 1f);

        [SerializeField, Tooltip("Set true to reverse the winding order of vertices so that the face normals are inverted.")]
        bool m_FlipNormals = false;

        Mesh m_Mesh;

        float m_NextScheduledRebuild;

        // This is the angle that gives the best results.
        float m_AutosmoothAngle = 180f;

        bool m_RebuildRequested;

        bool m_CanCapEnds;
        internal bool CanCapEnds => m_CanCapEnds;

        [SerializeReference]
        IExtrudeShape m_Shape;

        internal IExtrudeShape Shape
        {
            get => m_Shape;
            set => m_Shape = value;
        }

        /// <summary>The SplineContainer of the <see cref="Spline"/> to extrude.</summary>
        [Obsolete("Use Container instead.", false)]
        public SplineContainer container => Container;

        /// <summary>The SplineContainer of the <see cref="Spline"/> to extrude.</summary>
        public SplineContainer Container
        {
            get => m_Container;
            set => m_Container = value;
        }

        /// <summary>
        /// Enable to regenerate the extruded mesh when the target Spline is modified. Disable this option if the Spline
        /// will not be modified at runtime.
        /// </summary>
        [Obsolete("Use RebuildOnSplineChange instead.", false)]
        public bool rebuildOnSplineChange => RebuildOnSplineChange;

        /// <summary>
        /// Enable to regenerate the extruded mesh when the target Spline is modified. Disable this option if the Spline
        /// will not be modified at runtime.
        /// </summary>
        public bool RebuildOnSplineChange
        {
            get => m_RebuildOnSplineChange;
            set
            {
                m_RebuildOnSplineChange = value;

                if (!value)
                    m_RebuildRequested = value;
            }
        }

        /// <summary>The maximum number of times per-second that the mesh will be rebuilt.</summary>
        [Obsolete("Use RebuildFrequency instead.", false)]
        public int rebuildFrequency => RebuildFrequency;

        /// <summary>The maximum number of times per-second that the mesh will be rebuilt.</summary>
        public int RebuildFrequency
        {
            get => m_RebuildFrequency;
            set => m_RebuildFrequency = Mathf.Max(value, 1);
        }

        /// <summary>How many sides make up the radius of the mesh.</summary>
        [Obsolete("Use Sides instead.", false)]
        public int sides => Sides;

        /// <summary>How many sides make up the radius of the mesh.</summary>
        public int Sides
        {
            get => m_Sides;
            set
            {
                m_Sides = Mathf.Max(value, 3);

                if (m_Shape == null)
                {
                    var circle = new Circle();
                    circle.SideCount = m_Sides;
                    m_Shape = circle;
                }
            }
        }

        /// <summary>How many edge loops comprise the one unit length of the mesh.</summary>
        [Obsolete("Use SegmentsPerUnit instead.", false)]
        public float segmentsPerUnit => SegmentsPerUnit;

        /// <summary>How many edge loops comprise the one unit length of the mesh.</summary>
        public float SegmentsPerUnit
        {
            get => m_SegmentsPerUnit;
            set => m_SegmentsPerUnit = Mathf.Max(value, .0001f);
        }

        /// <summary>Whether the start and end of the mesh is filled. This setting is ignored when spline is closed.</summary>
        [Obsolete("Use Capped instead.", false)]
        public bool capped => Capped;

        /// <summary>Whether the start and end of the mesh is filled. This setting is ignored when spline is closed.</summary>
        public bool Capped
        {
            get => m_Capped;
            set => m_Capped = value;
        }

        /// <summary>The radius of the extruded mesh.</summary>
        [Obsolete("Use Radius instead.", false)]
        public float radius => Radius;

        /// <summary>The radius of the extruded mesh.</summary>
        public float Radius
        {
            get => m_Radius;
            set => m_Radius = Mathf.Max(value, .00001f);
        }

        /// <summary>
        /// The section of the Spline to extrude.
        /// </summary>
        [Obsolete("Use Range instead.", false)]
        public Vector2 range => Range;

        /// <summary>
        /// The section of the Spline to extrude. The X value is the start interpolation, the Y value is the end
        /// interpolation. X and Y are normalized values between 0 and 1.
        /// </summary>
        public Vector2 Range
        {
            get => m_Range;
            set => m_Range = new Vector2(Mathf.Min(value.x, value.y), Mathf.Max(value.x, value.y));
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

        /// <summary>The main Spline to extrude.</summary>
        [Obsolete("Use Spline instead.", false)]
        public Spline spline => Spline;

        /// <summary>The main Spline to extrude.</summary>
        public Spline Spline
        {
            get => m_Container?.Spline;
        }

        /// <summary>The Splines to extrude.</summary>
        public IReadOnlyList<Spline> Splines
        {
            get => m_Container?.Splines;
        }

        internal void Reset()
        {
            TryGetComponent(out m_Container);

            if (TryGetComponent<MeshFilter>(out var filter))
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
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
#endif
            {
                if (m_Container == null || m_Container.Spline == null || m_Container.Splines.Count == 0)
                    return;

                if ((m_Mesh = GetComponent<MeshFilter>().sharedMesh) == null)
                    return;
            }

            Rebuild();
        }

        internal static readonly string k_EmptyContainerError = "Spline Extrude does not have a valid SplineContainer set.";
        bool IsNullOrEmptyContainer()
        {
            var isNull = m_Container == null || m_Container.Spline == null || m_Container.Splines.Count == 0;
            if (isNull)
            {
                if (Application.isPlaying)
                    Debug.LogError(k_EmptyContainerError, this);
                
                if (!IsNullOrEmptyMeshFilter(false))
                    m_Mesh.Clear();
            }

            return isNull;
        }

        internal static readonly string k_EmptyMeshFilterError = "SplineExtrude.createMeshInstance is disabled," +
                                                                         " but there is no valid mesh assigned. " +
                                                                         "Please create or assign a writable mesh asset.";
        bool IsNullOrEmptyMeshFilter(bool logError = true)
        {
            var isNull = (m_Mesh = GetComponent<MeshFilter>().sharedMesh) == null;
            if (isNull && logError)
                Debug.LogError(k_EmptyMeshFilterError, this);

            return isNull;
        }

        internal void SetSplineContainerOnGO()
        {
            // Ensure that we use the spline container on the GameObject.
            // For example, in the case of pasting a SplineExtrude component from one
            // GameObject to another.
            var splineContainer = gameObject.GetComponent<SplineContainer>();
            if (splineContainer != null && splineContainer != m_Container)
                m_Container = splineContainer;
        }

        void OnEnable()
        {
            Spline.Changed += OnSplineChanged;
        }

        void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
        }

        void OnSplineChanged(Spline spline, int knotIndex, SplineModification modificationType)
        {
            if (!m_RebuildOnSplineChange)
                return;

            var isMainSpline = m_Container != null && Splines.Contains(spline);
            var isShapeSpline = (m_Shape is SplineShape splineShape) && splineShape.Spline != null && splineShape.Spline.Equals(spline);
            m_RebuildRequested = isMainSpline || isShapeSpline;
        }

        void Update()
        {
            if (m_RebuildRequested && Time.time >= m_NextScheduledRebuild)
                Rebuild();
        }

        /// <summary>
        /// Triggers the rebuild of a Spline's extrusion mesh and collider.
        /// </summary>
        public void Rebuild()
        {
            if (m_Shape == null)
            {
                var circle = new Circle();
                circle.SideCount = m_Sides;
                m_Shape = circle;
            }

            if (IsNullOrEmptyContainer() || IsNullOrEmptyMeshFilter())
                return;

            // SegmentCount is intentionally omitted for backwards compatibility reasons. This component extrudes many
            // Splines using the same mesh, taking into account each spline length in order to calculate the total
            // number of segments. This is unlike the normal `Extrude` method which accepts an int segment count.
            var settings = new ExtrudeSettings<IExtrudeShape>(m_Shape)
            {
                Radius = m_Radius,
                CapEnds = m_Capped,
                Range = m_Range,
                FlipNormals = m_FlipNormals
            };

            SplineMesh.Extrude(m_Container.Splines, m_Mesh, settings, SegmentsPerUnit);

            m_CanCapEnds = SplineMesh.s_IsConvex && !Spline.Closed;

            AutosmoothNormals();

            m_NextScheduledRebuild = Time.time + 1f / m_RebuildFrequency;

#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.IsPersistent(m_Mesh))
                UnityEditor.EditorApplication.delayCall += () => UnityEditor.AssetDatabase.SaveAssetIfDirty(m_Mesh);
#endif

#if UNITY_PHYSICS_MODULE
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
#endif
        }

        void AutosmoothNormals()
        {
            var vertices = m_Mesh.vertices;
            var triangles = m_Mesh.triangles;
            var normals = new Vector3[vertices.Length];

            // Dictionary to hold face normals and the vertices that make up each face.
            var faceNormals = new Dictionary<int, Vector3>();
            var vertexFaces = new Dictionary<int, List<int>>();

            // Calculate the face normals.
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var v1 = vertices[triangles[i]];
                var v2 = vertices[triangles[i + 1]];
                var v3 = vertices[triangles[i + 2]];
                var faceNormal = Vector3.Cross(v2 - v1, v3 - v1).normalized;

                var faceIndex = i / 3;
                faceNormals[faceIndex] = faceNormal;

                for (int j = 0; j < 3; j++)
                {
                    var vertexIndex = triangles[i + j];

                    if (!vertexFaces.ContainsKey(vertexIndex))
                        vertexFaces[vertexIndex] = new List<int>();

                    vertexFaces[vertexIndex].Add(faceIndex);
                }
            }

            // Calculate the vertex normals.
            foreach (var pair in vertexFaces)
            {
                var vertexIndex = pair.Key;
                var connectedFaces = pair.Value;
                var averageNormal = Vector3.zero;

                foreach (var faceIndex in connectedFaces)
                {
                    var currentFaceNormal = faceNormals[faceIndex];
                    var sharedNormal = true;

                    foreach (var otherFaceIndex in connectedFaces)
                    {
                        if (faceIndex == otherFaceIndex)
                            continue;

                        var otherFaceNormal = faceNormals[otherFaceIndex];
                        var angle = Vector3.Angle(currentFaceNormal, otherFaceNormal);

                        if (angle > m_AutosmoothAngle)
                        {
                            sharedNormal = false;
                            break;
                        }
                    }

                    if (sharedNormal) // Not a sharp normal.
                    {
                        averageNormal += currentFaceNormal;
                    }
                    else // Sharp normal.
                    {
                        normals[vertexIndex] = currentFaceNormal;
                        break;
                    }
                }

                if (normals[vertexIndex] == Vector3.zero) // If not set to a sharp normal.
                    normals[vertexIndex] = averageNormal.normalized;
            }

            // Apply the normals to the mesh.
            m_Mesh.normals = normals;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (UnityEditor.EditorApplication.isPlaying)
                return;

            Rebuild();
        }
#endif

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

            var path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"{sceneDataDir}/SplineExtrude_{mesh.name}.asset");
            UnityEditor.AssetDatabase.CreateAsset(mesh, path);
            UnityEditor.EditorGUIUtility.PingObject(mesh);
#endif
            return mesh;
        }
    }
}
