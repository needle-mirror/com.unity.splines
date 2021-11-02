using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Collections;
using Unity.Mathematics;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    static class EditableSplineUtility
    {
        public static SplineType GetSplineType(IEditableSpline spline)
        {
            switch (spline)
            {
                case BezierEditableSpline _:
                    return SplineType.Bezier;
                case CatmullRomEditableSpline _:
                    return SplineType.CatmullRom;
                case LinearEditableSpline _:
                    return SplineType.Linear;
                default:
                    throw new ArgumentException(nameof(spline));
            }
        }

        internal static IEditableSpline CreatePathOfType(SplineType type)
        {
            switch (type)
            {
                case SplineType.Bezier: return new BezierEditableSpline();
                case SplineType.CatmullRom: return new CatmullRomEditableSpline();
                case SplineType.Linear: return new LinearEditableSpline();
                default:
                    throw new InvalidEnumArgumentException(nameof(type));
            }
        }

        public static void GetSelectedSplines(IEnumerable<Object> targets, List<IEditableSpline> results)
        {
            results.Clear();
            foreach (var target in targets)
            {
                var splines = GetSelectedSpline(target);
                if (splines == null)
                    continue;

                results.AddRange(splines);
            }
        }

        public static IReadOnlyList<IEditableSpline> GetSelectedSpline(Object target)
        {
            return EditableSplineManager.GetEditableSplines(target);
        }

        internal static Bounds GetBounds(IReadOnlyList<ISplineElement> elements, 
            bool useKnotPositionForTangents = false)
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));

            if (elements.Count == 0)
                return new Bounds(Vector3.positiveInfinity, Vector3.zero);

            var element = elements[0];

            var position = (useKnotPositionForTangents && element is EditableTangent)?
                    ((EditableTangent)element).owner.position : 
                    element.position;
            
            Bounds bounds = new Bounds(position, Vector3.zero);
            for (int i = 1; i < elements.Count; ++i)
            {
                element = elements[i];
                if(useKnotPositionForTangents && element is EditableTangent tangent)
                    bounds.Encapsulate(tangent.owner.position);
                else
                    bounds.Encapsulate(element.position);
            }

            return bounds;
        }

        public static EditableKnot InsertKnotOnCurve(CurveData curve, Vector3 position, float t)
        {
            var path = curve.a.spline;
            var prev = curve.a;
            var next = curve.b;

            EditableKnot knot = path.InsertKnot(next.index);
            knot.position = position;
            knot.OnKnotInsertedOnCurve(prev, next, t);

            return knot;
        }

        public static void AddPointToEnd(IEditableSpline spline, Vector3 worldPosition, Vector3 normal, Vector3 tangentOut)
        {
            if (spline.closed)
                throw new ArgumentException("Cannot add a point to the end of a closed spline", nameof(spline));
            
            EditableKnot knot = spline.AddKnot();
            knot.position = worldPosition;
            spline.OnKnotAddedAtEnd(knot, normal, tangentOut);
        }

        public static void CloseSpline(IEditableSpline spline)
        {
            if (spline.knotCount <= 1 && !spline.canBeClosed)
                return;

            spline.closed = true;
        }
        
        internal static float3 ToSplineSpaceTangent(this EditableKnot knot, float3 knotSpaceTangent)
        {
            return  math.rotate(knot.localRotation, knotSpaceTangent);
        }
        
        internal static float3 ToKnotSpaceTangent(this EditableKnot knot, float3 splineSpaceTangent)
        {
            return  math.rotate(math.inverse(knot.localRotation), splineSpaceTangent);
        }
    }
}
