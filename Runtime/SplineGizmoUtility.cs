using Unity.Mathematics;
using UnityEngine;

namespace UnityEngine.Splines
{
    /// <summary>
    /// SplineGizmoUtility provides methods for drawing in-scene representations of Splines.
    /// </summary>
    public static class SplineGizmoUtility
    {
        /// <summary>
        /// Draw a line gizmo for a <see cref="ISplineProvider"/>.
        /// </summary>
        /// <param name="provider">An object implementing the ISplineProvider interface. Usually this will be a MonoBehaviour.</param>
        public static void DrawGizmos(ISplineProvider provider)
        {
            var splines = provider.Splines;

            if (splines == null)
                return;

            var localToWorld = ((MonoBehaviour)provider).transform.localToWorldMatrix;
            foreach (var spline in splines)
            {
                if (spline == null || spline.Count < 2)
                    continue;

                for (int i = 0, c = spline.Closed ? spline.Count : spline.Count - 1; i < c; ++i)
                    DrawCurve(spline.GetCurve(i), localToWorld);
            }
        }

        static void DrawCurve(BezierCurve curve, Matrix4x4 localToWorld, int segments = 32)
        {
            float inv = 1f / (segments - 1);
            float3 p0 = localToWorld.MultiplyPoint(CurveUtility.EvaluatePosition(curve, 0f));

            for (int n = 1; n < segments; n++)
            {
                // todo Replace this with a Handles.BeginLineDrawing and handwritten GL.Vertex
                var p1 = localToWorld.MultiplyPoint(CurveUtility.EvaluatePosition(curve, n * inv));
                Gizmos.DrawLine(p0, p1);
                p0 = p1;
            }
        }
    }
}
