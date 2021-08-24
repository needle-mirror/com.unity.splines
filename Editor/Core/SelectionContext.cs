using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    [Serializable]
    struct SelectableSplineElement : IEquatable<SelectableSplineElement>, IEquatable<EditableKnot>, IEquatable<EditableTangent>
    {
        public Object target;
        public int pathIndex;
        public int knotIndex;
        public int tangentIndex; //-1 if knot

        public SelectableSplineElement(EditableKnot knot)
        {
            target = knot.splineConversionData.conversionTarget;
            pathIndex = knot.splineConversionData.conversionIndex;
            knotIndex = knot.index;
            tangentIndex = -1;
        }

        public SelectableSplineElement(EditableTangent tangent) : this(tangent.owner)
        {
            tangentIndex = tangent.tangentIndex;
        }

        public bool isTangent => tangentIndex >= 0;
        public bool isKnot => tangentIndex < 0;

        public bool Equals(EditableKnot other)
        {
            return IsTargetedKnot(other) && tangentIndex < 0;
        }

        public bool Equals(EditableTangent other)
        {
            return other != null && IsTargetedKnot(other.owner) && tangentIndex == other.tangentIndex;
        }

        public bool IsFromPath(IEditableSpline spline)
        {
            var pathInternal = (IEditableSplineConversionData)spline;
            return pathInternal.conversionTarget == target && pathInternal.conversionIndex == pathIndex;
        }

        bool IsTargetedKnot(EditableKnot knot)
        {
            if (knot == null)
                return false;

            return knotIndex == knot.index
                   && pathIndex == knot.splineConversionData.conversionIndex
                   && target == knot.splineConversionData.conversionTarget;
        }

        public bool Equals(SelectableSplineElement other)
        {
            return target == other.target && pathIndex == other.pathIndex && knotIndex == other.knotIndex && tangentIndex == other.tangentIndex;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SelectableSplineElement other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (target != null ? target.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ pathIndex;
                hashCode = (hashCode * 397) ^ knotIndex;
                hashCode = (hashCode * 397) ^ tangentIndex;
                return hashCode;
            }
        }
    }

    sealed class SelectionContext : ScriptableObject
    {
        static SelectionContext s_Instance;
        
        public List<SelectableSplineElement> selection = new List<SelectableSplineElement>();

        public static SelectionContext instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = CreateInstance<SelectionContext>();
                    s_Instance.hideFlags = HideFlags.HideAndDontSave;
                }

                return s_Instance;
            }
        }

        SelectionContext()
        {
            if (s_Instance == null) 
                s_Instance = this;
        }
    }
}
