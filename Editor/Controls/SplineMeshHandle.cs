using System;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    /// <summary>
    /// Creates a cylinder mesh along a spline.
    /// </summary>
    /// <typeparam name="T">The type of ISpline.</typeparam>
    public class SplineMeshHandle<T> : IDisposable where T : ISpline
    {
        Mesh m_Mesh;

        Material m_Material;

        /// <summary>
        /// Creates a new mesh handle. This class implements IDisposable to clean up allocated mesh resources. Call
        ///  <see cref="Dispose"/> when you are finished with the instance.
        /// </summary>
        public SplineMeshHandle()
        {
            m_Mesh = new Mesh()
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            m_Material = null;
        }

        /// <summary>
        /// Create a new mesh handle. This class implements IDisposable to clean up allocated mesh resources. Call
        /// <see cref="Dispose"/> when you are finished with the instance.
        /// </summary>
        /// <param name="material">The material to render the cylinder mesh with.</param>
        public SplineMeshHandle(Material material) : base()
        {
            m_Material = material;
        }

        /// <summary>
        /// The material to render this mesh with. If null, a default material is used.
        /// </summary>
        public Material material
        {
            get => m_Material;
            set => m_Material = value;
        }

        static Matrix4x4 StartCapDraw(Material material, Color color)
        {
            Shader.SetGlobalColor("_HandleColor", color);
            Shader.SetGlobalFloat("_HandleSize", 1f);
            Shader.SetGlobalMatrix("_ObjectToWorld", Handles.matrix);
            var mat = material == null ? HandleUtility.handleMaterial : material; 
            mat.SetFloat("_HandleZTest", (float) Handles.zTest);
            mat.SetPass(0);

            return Handles.matrix;
        }

        /// <summary>
        /// Draws a 3D mesh from a spline.
        /// </summary>
        /// <param name="spline">The target spline.</param>
        /// <param name="size">The width to use for the spline mesh.</param>
        /// <param name="color">The color to use for the spline mesh in normal mode.</param>
        /// <param name="resolution">The resolution to use for the mesh, defines the number of segments per unit
        /// with default value of <see cref="SplineUtility.DrawResolutionDefault"/>.</param>
        public void Do(T spline, float size, Color color, int resolution = SplineUtility.DrawResolutionDefault)
        {
            if(Event.current.type != EventType.Repaint)
                return;

            Do(-1, spline, size, color, resolution);
        }

        /// <summary>
        /// Draws a 3D mesh handle from a spline.
        /// </summary>
        /// <param name="controlID">The spline mesh controlID.</param>
        /// <param name="spline">The target spline.</param>
        /// <param name="size">The width to use for the spline mesh.</param>
        /// <param name="color">The color to use for the spline mesh in normal mode.</param>
        /// <param name="resolution">The resolution to use for the mesh, defines the number of segments per unit
        /// with default value of <see cref="SplineUtility.DrawResolutionDefault"/>.</param>
        public void Do(int controlID, T spline, float size, Color color, int resolution = SplineUtility.DrawResolutionDefault)
        {
            using (new Handles.DrawingScope(color))
                Do(controlID, spline, size, resolution);
        }

        /// <summary>
        /// Draws a 3D mesh from a spline.
        /// </summary>
        /// <param name="spline">The target spline.</param>
        /// <param name="size">The width to use for the spline mesh.</param>
        /// <param name="resolution">The resolution to use for the mesh, defines the number of segments per unit
        /// with default value of <see cref="SplineUtility.DrawResolutionDefault"/>.</param>
        public void Do(T spline, float size, int resolution = SplineUtility.DrawResolutionDefault)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Do(-1, spline, size, resolution);
        }

        /// <summary>
        /// Draws a 3D mesh handle from a spline.
        /// </summary>
        /// <param name="controlID">The spline mesh controlID.</param>
        /// <param name="spline">The target spline.</param>
        /// <param name="size">The width to use for the spline mesh.</param>
        /// <param name="resolution">The resolution to use for the mesh, defines the number of segments per unit
        /// with default value of <see cref="SplineUtility.DrawResolutionDefault"/>.</param>
        public void Do(int controlID, T spline, float size, int resolution = SplineUtility.DrawResolutionDefault)
        {
            var evt = Event.current;

            switch (evt.type)
            {
                case EventType.MouseMove:
                    var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                    HandleUtility.AddControl(controlID, SplineUtility.GetNearestPoint(spline, ray, out _, out _));
                    break;

                case EventType.Repaint:
                    var segments = SplineUtility.GetSubdivisionCount(spline.GetLength(), resolution);
                    SplineMesh.Extrude(spline, m_Mesh, size, 8, segments, !spline.Closed);
                    var color = GUIUtility.hotControl == controlID
                        ? Handles.selectedColor
                        : HandleUtility.nearestControl == controlID
                            ? Handles.preselectionColor
                            : Handles.color;
                    Graphics.DrawMeshNow(m_Mesh, StartCapDraw(m_Material, color));
                    break;
            }
        }

        /// <summary>
        /// Destroys the 3D mesh.
        /// </summary>
        public void Dispose() => Object.DestroyImmediate(m_Mesh);
    }
}