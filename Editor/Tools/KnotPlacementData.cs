using Unity.Mathematics;
using UnityEngine;

namespace UnityEditor.Splines
{
    class CurvePlacementData : PlacementData
    {
        readonly SplineCurveHit m_Hit;

        public CurvePlacementData(SplineCurveHit hit) : base(hit.Position, hit.Normal)
        {
            m_Hit = hit;
        }

        public override SelectableKnot GetOrCreateLinkedKnot()
        {
            EditorSplineUtility.RecordObject(m_Hit.NextKnot.SplineInfo, "Insert Knot");
            return EditorSplineUtility.InsertKnot(m_Hit.NextKnot.SplineInfo, m_Hit.NextKnot.KnotIndex, m_Hit.T);
        }
    }

    class KnotPlacementData : PlacementData
    {
        readonly SelectableKnot m_Target;

        public KnotPlacementData(SelectableKnot target) : base(target.Position, math.mul(target.Rotation, math.up()))
        {
            m_Target = target;
        }

        public override SelectableKnot GetOrCreateLinkedKnot()
        {
            return m_Target;
        }
    }

    class PlacementData
    {
        public Vector3 TangentOut { get; set; }
        public Vector3 Position { get; }
        public Vector3 Normal { get; }
        public Plane Plane { get; }

        public PlacementData(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
            TangentOut = Vector3.zero;
            Plane = new Plane(normal, position);
        }
        
        public virtual SelectableKnot GetOrCreateLinkedKnot() => default;
    }
}
