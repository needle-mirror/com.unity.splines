using UnityEditor;

namespace UnityEditor.Splines
{
    static class SplineSelectionUtility
    {
        internal static bool IsSelectable(IEditableSpline spline, int knotIndex, ISplineElement element)
        {
            if (element is EditableTangent)
            {
                var ownerKnot = spline.GetKnot(knotIndex);
                if (ownerKnot is BezierEditableKnot knot)
                {
                    // For open splines, tangentIn of first knot and tangentOut of last knot should not be selectable.
                    if (!spline.closed)
                    {
                        if ((knotIndex == 0 && knot.tangentIn == element) ||
                            (knotIndex == spline.knotCount - 1 && knot.tangentOut == element))
                            return false;
                    }

                    // Tangents should not be selectable if knot is Linear.
                    if (knot.mode == BezierEditableKnot.Mode.Linear)
                        return false;
                }
            }

            return true;
        }
    }
}
