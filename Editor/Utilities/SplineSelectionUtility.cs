using UnityEditor;

namespace UnityEditor.Splines
{
    static class SplineSelectionUtility
    {
        internal static bool IsSelectable(IEditableSpline spline, int knotIndex, ISplineElement element)
        {
            if (element is EditableTangent tangent)
            {
                var ownerKnot = spline.GetKnot(knotIndex);
                if (ownerKnot is BezierEditableKnot knot)
                {
                    if (!spline.closed)
                    {
                        if ((knotIndex == 0 && knot.tangentIn == element) ||
                            (knotIndex == spline.knotCount - 1 && knot.tangentOut == element))
                            return false;
                    }
                }
            }
            // TODO: add additional checks once linear tangent behaviour and selection for Bezier Splines is properly fleshed out

            return true;
        }
    }
}
