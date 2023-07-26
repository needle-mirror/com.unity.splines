namespace UnityEditor.Splines
{
    sealed class CommonElementDrawer : ElementDrawer<ISelectableElement>
    {
        readonly TangentModePropertyField<ISelectableElement> m_Mode;
        readonly BezierTangentPropertyField<ISelectableElement> m_BezierMode;

        public CommonElementDrawer()
        {
            Add(m_Mode = new TangentModePropertyField<ISelectableElement>());
            m_Mode.changed += () => { m_BezierMode.Update(targets); };

            Add(m_BezierMode = new BezierTangentPropertyField<ISelectableElement>());
            m_BezierMode.changed += () => { m_Mode.Update(targets); };
        }

        public override void Update()
        {
            base.Update();

            m_Mode.Update(targets);
            m_BezierMode.Update(targets);
        }

        public override string GetLabelForTargets()
        {
            int knotCount = 0;
            int tangentCount = 0;
            for (int i = 0; i < targets.Count; ++i)
            {
                if (targets[i] is SelectableKnot)
                    ++knotCount;
                else if (targets[i] is SelectableTangent)
                    ++tangentCount;
            }

            return $"<b>({knotCount}) Knots</b>, <b>({tangentCount}) Tangents</b> selected";
        }
    }
}
