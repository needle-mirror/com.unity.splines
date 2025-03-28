using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Splines;
#if !UNITY_2022_1_OR_NEWER
using UnityEditor.UIElements;
#endif

namespace UnityEditor.Splines
{
    sealed class BezierKnotDrawer : ElementDrawer<SelectableKnot>
    {
        static readonly string k_PositionTooltip = L10n.Tr("Knot Position");
        static readonly string k_RotationTooltip = L10n.Tr("Knot Rotation");

        readonly Float3PropertyField<SelectableKnot> m_Position;
        readonly Float3PropertyField<SelectableKnot> m_Rotation;
        readonly TangentModePropertyField<SelectableKnot> m_Mode;
        readonly BezierTangentPropertyField<SelectableKnot> m_BezierMode;
        readonly TangentPropertyField m_TangentIn;
        readonly TangentPropertyField m_TangentOut;

        public BezierKnotDrawer()
        {
            VisualElement row;
            Add(row = new VisualElement(){name = "Vector3WithIcon"});
            row.tooltip = k_PositionTooltip;
            row.style.flexDirection = FlexDirection.Row;
            row.Add(new VisualElement(){name = "PositionIcon"});
            row.Add(m_Position = new Float3PropertyField<SelectableKnot>("",
                (knot) => knot.LocalPosition,
                (knot, value) => knot.LocalPosition = value)
                { name = "Position" });

            m_Position.style.flexGrow = 1;

            Add(row = new VisualElement(){name = "Vector3WithIcon"});
            row.tooltip = k_RotationTooltip;
            row.style.flexDirection = FlexDirection.Row;
            row.Add(new VisualElement(){name = "RotationIcon"});
            row.Add(m_Rotation = new Float3PropertyField<SelectableKnot>("",
                (knot) => ((Quaternion)knot.LocalRotation).eulerAngles,
                (knot, value) => knot.LocalRotation = Quaternion.Euler(value))
                { name = "Rotation" });

            m_Rotation.style.flexGrow = 1;

            Add(new Separator());

            Add(m_Mode = new TangentModePropertyField<SelectableKnot>());
            m_Mode.changed += Update;

            Add(m_BezierMode = new BezierTangentPropertyField<SelectableKnot>());
            m_BezierMode.changed += Update;

            Add(m_TangentIn = new TangentPropertyField("In", "TangentIn", BezierTangent.In));
            Add(m_TangentOut = new TangentPropertyField("Out", "TangentOut", BezierTangent.Out));

            //Update opposite to take into account some tangent modes
            m_TangentIn.changed += () => m_TangentOut.Update(targets);
            m_TangentOut.changed += () => m_TangentIn.Update(targets);
        }

        public override string GetLabelForTargets()
        {
            if (targets.Count > 1)
                return $"<b>({targets.Count}) Knots</b> selected";

            return $"<b>Knot {target.KnotIndex}</b> (<b>Spline {target.SplineInfo.Index}</b>) selected";
        }

        public override void Update()
        {
            base.Update();

            m_Position.Update(targets);
            m_Rotation.Update(targets);

            m_Mode.Update(targets);
            m_BezierMode.Update(targets);

            m_TangentIn.Update(targets);
            m_TangentOut.Update(targets);

            //Disabling edition when using linear tangents
            UpdateTangentsState();
        }

        void UpdateTangentsState()
        {
            bool tangentsModifiable = true;
            bool tangentsBroken = true;
            bool tangentInSelectable = false;
            bool tangentOutSelectable = false;
            for (int i = 0; i < targets.Count; ++i)
            {
                var mode = targets[i].Mode;
                tangentsModifiable &= SplineUtility.AreTangentsModifiable(mode);
                tangentsBroken &= mode == TangentMode.Broken;
                tangentInSelectable |= SplineSelectionUtility.IsSelectable(targets[i].TangentIn);
                tangentOutSelectable |= SplineSelectionUtility.IsSelectable(targets[i].TangentOut);
            }

            m_TangentIn.SetEnabled(tangentsModifiable && tangentInSelectable);
            m_TangentOut.SetEnabled(tangentsModifiable && tangentOutSelectable);

            if(tangentsModifiable)
            {
                m_TangentIn.vector3field.SetEnabled(tangentsBroken);
                m_TangentOut.vector3field.SetEnabled(tangentsBroken);
            }
        }
    }
}
