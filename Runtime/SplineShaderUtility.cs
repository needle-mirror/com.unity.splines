using System;
using Unity.Collections;

namespace UnityEngine.Splines
{
    /// <summary>
    /// SplineComputeBufferScope is a convenient way to extract from a spline the information necessary to evaluate
    /// spline values in a ComputeShader.
    /// To access Spline evaluation methods in a shader, include the "Splines.cginc" file:
    /// </summary>
    /// <example>
    /// <code>#include "Packages/com.unity.splines/Shader/Spline.cginc"</code>
    /// </example>
    /// <typeparam name="T">The type of spline.</typeparam>
    public struct SplineComputeBufferScope<T> : IDisposable where T : ISpline
    {
        T m_Spline;
        int m_KnotCount;
        ComputeBuffer m_CurveBuffer, m_LengthBuffer;

        // Optional shader property bindings
        ComputeShader m_Shader;
        string m_Info, m_Curves, m_CurveLengths;
        int m_Kernel;

        /// <summary>
        /// Create a new SplineComputeBufferScope.
        /// </summary>
        /// <param name="spline">The spline to create GPU data for.</param>
        public SplineComputeBufferScope(T spline)
        {
            m_Spline = spline;
            m_KnotCount = 0;
            m_CurveBuffer = m_LengthBuffer = null;

            m_Shader = null;
            m_Info = m_Curves = m_CurveLengths = null;
            m_Kernel = 0;

            Upload();
        }

        /// <summary>
        /// Set up a shader with all of the necessary ComputeBuffer and Spline metadata for working with functions found
        /// in Spline.cginc.
        /// </summary>
        /// <param name="shader">The compute shader to bind.</param>
        /// <param name="kernel">The kernel to target.</param>
        /// <param name="info">The float4 (typedef to SplineData in Spline.cginc) Spline info.</param>
        /// <param name="curves">A StructuredBuffer{BezierCurve} or RWStructuredBuffer{BezierCurve}.</param>
        /// <param name="lengths">A StructuredBuffer{float} or RWStructuredBuffer{float}.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the expected properties are invalid.</exception>
        public void Bind(ComputeShader shader, int kernel, string info, string curves, string lengths)
        {
            if (shader == null) throw new ArgumentNullException(nameof(shader));
            if (string.IsNullOrEmpty(info)) throw new ArgumentNullException(nameof(info));
            if (string.IsNullOrEmpty(curves)) throw new ArgumentNullException(nameof(curves));
            if (string.IsNullOrEmpty(lengths)) throw new ArgumentNullException(nameof(lengths));

            m_Shader = shader;
            m_Info = info;
            m_Curves = curves;
            m_CurveLengths = lengths;
            m_Kernel = kernel;

            m_Shader.SetVector(m_Info, Info);
            m_Shader.SetBuffer(m_Kernel, m_Curves, Curves);
            m_Shader.SetBuffer(m_Kernel, m_CurveLengths, CurveLengths);
        }

        /// <summary>
        /// Free resources allocated by this object.
        /// </summary>
        public void Dispose()
        {
            m_CurveBuffer?.Dispose();
            m_LengthBuffer?.Dispose();
        }

        /// <summary>
        /// Copy Spline curve, info, and length caches to their GPU buffers.
        /// </summary>
        public void Upload()
        {
            int knotCount = m_Spline.Count;

            if (m_KnotCount != knotCount)
            {
                m_KnotCount = m_Spline.Count;

                m_CurveBuffer?.Dispose();
                m_LengthBuffer?.Dispose();

                m_CurveBuffer = new ComputeBuffer(m_KnotCount, sizeof(float) * 3 * 4);
                m_LengthBuffer = new ComputeBuffer(m_KnotCount, sizeof(float));
            }

            var curves = new NativeArray<BezierCurve>(m_KnotCount, Allocator.Temp);
            var lengths = new NativeArray<float>(m_KnotCount, Allocator.Temp);

            for (int i = 0; i < m_KnotCount; ++i)
            {
                curves[i] = m_Spline.GetCurve(i);
                lengths[i] = m_Spline.GetCurveLength(i);
            }

            if(!string.IsNullOrEmpty(m_Info))
                m_Shader.SetVector(m_Info, Info);

            m_CurveBuffer.SetData(curves);
            m_LengthBuffer.SetData(lengths);

            curves.Dispose();
            lengths.Dispose();
        }

        /// <summary>
        /// Returns a SplineInfo Vector4.
        /// </summary>
        public Vector4 Info => new Vector4(m_Spline.Count, m_Spline.Closed ? 1 : 0, m_Spline.GetLength(), 0);

        /// <summary>
        /// A ComputeBuffer containing <see cref="BezierCurve"/>.
        /// </summary>
        public ComputeBuffer Curves => m_CurveBuffer;

        /// <summary>
        /// A ComputeBuffer containing the cached length of all spline curves.
        /// </summary>
        public ComputeBuffer CurveLengths => m_LengthBuffer;
    }
}
