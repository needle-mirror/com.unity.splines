using System;
using System.Collections.Generic;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class SplineSelectionUtility
    {
        static readonly List<SelectableKnot> s_KnotBuffer = new List<SelectableKnot>();

        internal static void HandleSelection<T>(T element, bool appendElement, bool addLinkedKnots = true)
            where T : struct, ISplineElement
        {
            if (appendElement)
            {
                if(element is SelectableKnot knot)
                {
                    if(addLinkedKnots)
                    {
                        EditorSplineUtility.GetKnotLinks(knot, s_KnotBuffer);
                        var allContained = true;
                        foreach(var k in s_KnotBuffer)
                            allContained &= SplineSelection.Contains(k);

                        if (allContained)
                            SplineSelection.RemoveRange(s_KnotBuffer);
                        else
                            SplineSelection.AddRange(s_KnotBuffer);
                    }
                    else
                    {
                        var activeKnot = GetSelectedKnot(knot);
                        if(SplineSelection.Contains(activeKnot))
                            SplineSelection.Remove(activeKnot);
                        else
                            SplineSelection.Add(activeKnot);
                    }
                }
                else
                {
                    if(SplineSelection.Contains(element))
                        SplineSelection.Remove(element);
                    else
                        SplineSelection.Add(element);
                }
            }
            else
            {
                List<ISplineElement> newSelection = new List<ISplineElement>();
                if(element is SelectableKnot knot)
                {
                    if(addLinkedKnots)
                    {
                        EditorSplineUtility.GetKnotLinks(knot, s_KnotBuffer);
                        foreach(var k in s_KnotBuffer)
                            newSelection.Add(k);
                    }
                    else
                        newSelection.Add(GetSelectedKnot(knot));
                }
                else
                    newSelection.Add(element);

                SplineSelection.ClearNoUndo(false);
                SplineSelection.AddRange(newSelection);
            }
        }

        static SelectableKnot GetSelectedKnot(SelectableKnot knot)
        {
            EditorSplineUtility.GetKnotLinks(knot, s_KnotBuffer);

            var activeKnot = new SelectableKnot();
            float minDist = Single.PositiveInfinity, dist;
            foreach(var k in s_KnotBuffer)
            {
                var spline = k.SplineInfo.Spline;
                var localToWorld = k.SplineInfo.LocalToWorld;

                if(k.KnotIndex > 0)
                {
                    var curve = spline.GetCurve(k.KnotIndex - 1).Transform(localToWorld);
                    dist = CurveHandles.DistanceToCurve(curve);
                    if(dist < minDist)
                    {
                        minDist = dist;
                        activeKnot = k;
                    }
                }

                if(k.KnotIndex < spline.Count - 1)
                {
                    var curve = spline.GetCurve(k.KnotIndex).Transform(localToWorld);
                    dist = CurveHandles.DistanceToCurve(curve);
                    if(dist < minDist)
                    {
                        minDist = dist;
                        activeKnot = k;
                    }
                }
            }

            return activeKnot;
        }

        internal static bool IsSelectable(SelectableTangent tangent)
        {
            // Tangents should not be selectable if not modifiable
            if(!EditorSplineUtility.AreTangentsModifiable(tangent.Owner.Mode))
                return false;

            // For open splines, tangentIn of first knot and tangentOut of last knot should not be selectable
            switch (tangent.TangentIndex)
            {
                case (int)BezierTangent.In:
                    return tangent.KnotIndex != 0 || tangent.SplineInfo.Spline.Closed;

                case (int)BezierTangent.Out:
                    return tangent.KnotIndex != tangent.SplineInfo.Spline.Count - 1 || tangent.SplineInfo.Spline.Closed;
            }
            return true;
        }

        internal static bool IsSelectable(ISplineElement element)
        {
            if (element is SelectableTangent tangent)
                return IsSelectable(tangent);

            return true;
        }

        internal static bool CanLinkKnots(List<SelectableKnot> knots)
        {
            if (knots.Count == 0)
                return false;

            var knotCounts = new Dictionary<ISplineContainer, List<SelectableKnot>>();

            foreach (var knot in knots)
            {
                var container = knot.SplineInfo.Container;
                if (!knotCounts.ContainsKey(container))
                {
                    var knotList = new List<SelectableKnot> {knot};
                    knotCounts.Add(container, knotList);
                }
                else
                    knotCounts[container].Add(knot);

                EditorSplineUtility.GetKnotLinks(knot, s_KnotBuffer);
                if (s_KnotBuffer.Count > 1)
                    return true;

                var otherSelectedKnots = knotCounts[container];
                if (otherSelectedKnots.Count > 1)
                {
                    for (int i = 0; i < otherSelectedKnots.Count - 1; ++i)
                    {
                        var otherSelectedKnot = otherSelectedKnots[i];
                        if (!s_KnotBuffer.Contains(otherSelectedKnot))
                            return true;
                    }
                }
            }

            return false;
        }

        internal static bool CanUnlinkKnots(List<SelectableKnot> knots)
        {
            if (knots.Count == 0)
                return false;

            foreach (var knot in knots)
            {
                EditorSplineUtility.GetKnotLinks(knot, s_KnotBuffer);
                if (s_KnotBuffer.Count > 1)
                    return true;
            }

            return false;
        }

        internal static bool CanSplitSelection(List<SelectableKnot> knots)
        {
            if(knots.Count != 1)
                return false;

            var knot = knots[0];

            bool endKnot = knot.KnotIndex == 0 || knot.KnotIndex == knot.SplineInfo.Spline.Count - 1;
            return !endKnot || knot.SplineInfo.Spline.Closed;
        }

        internal static bool CanJoinSelection(List<SelectableKnot> knots)
        {
            if(knots.Count != 2)
                return false;

            var isActiveKnotInSelection = SplineSelection.IsActive(knots[0]) || SplineSelection.IsActive(knots[1]);
            var areInSameContainer = knots[0].SplineInfo.Container == knots[1].SplineInfo.Container;
            var areOnDifferentSplines = knots[0].SplineInfo.Index != knots[1].SplineInfo.Index;

            var areKnotsOnExtremities =
                   (knots[0].KnotIndex == 0 || knots[0].KnotIndex == knots[0].SplineInfo.Spline.Count - 1)
                && (knots[1].KnotIndex == 0 || knots[1].KnotIndex == knots[1].SplineInfo.Spline.Count - 1);

            return isActiveKnotInSelection && areInSameContainer && areOnDifferentSplines && areKnotsOnExtremities;
        }
    }
}