using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    [Serializable]
    struct SelectableSplineElement : IEquatable<SelectableSplineElement>
    {
        public Object target;
        public int targetIndex;
        public int knotIndex;
        public int tangentIndex;

        public SelectableSplineElement(ISelectableElement element)
        {
            target = element.SplineInfo.Object;
            targetIndex = element.SplineInfo.Index;
            knotIndex = element.KnotIndex;
            tangentIndex = element is SelectableTangent tangent ? tangent.TangentIndex : -1;
        }

        public bool Equals(SelectableSplineElement other)
        {
            return target == other.target && targetIndex == other.targetIndex && knotIndex == other.knotIndex && tangentIndex == other.tangentIndex;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SelectableSplineElement other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(target, targetIndex, knotIndex, tangentIndex);
        }
    }

    sealed class SelectionContext : ScriptableSingleton<SelectionContext>
    {
        public List<SelectableSplineElement> selection = new List<SelectableSplineElement>();
        public int version;
    }
}
