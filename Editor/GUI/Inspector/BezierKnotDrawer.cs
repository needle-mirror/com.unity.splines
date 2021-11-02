using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class BezierKnotDrawer : KnotDrawer<BezierEditableKnot>
    {
        const float k_Indent = 15;

        readonly ButtonStripField m_Mode;
        readonly Vector3Field m_In;
        readonly Vector3Field m_Out;

        public BezierKnotDrawer()
        {
            Add(m_Mode = new ButtonStripField(L10n.Tr("Tangents")) { name = "TangentMode" });
            Add(m_In = new Vector3Field("In") { name = "TangentIn" });   
            Add(m_Out = new Vector3Field("Out") { name = "TangentOut" });
            
            m_In.style.marginLeft = k_Indent;
            m_Out.style.marginLeft = k_Indent;

            m_Mode.choices = new[] {"Linear", "Mirrored", "Continuous", "Broken"};
            m_Mode.RegisterValueChangedCallback((evt) => target.SetMode((BezierEditableKnot.Mode) evt.newValue));
            m_In.RegisterValueChangedCallback((evt) => target.tangentIn.localPosition = evt.newValue);
            m_Out.RegisterValueChangedCallback((evt) => target.tangentOut.localPosition = evt.newValue);
        }

        public override void Update()
        {
            base.Update();

            m_Mode.SetValueWithoutNotify((int)target.mode);
            m_In.SetValueWithoutNotify(target.tangentIn.localPosition);
            m_Out.SetValueWithoutNotify(target.tangentOut.localPosition);

            m_In.SetEnabled(target.mode != BezierEditableKnot.Mode.Linear);
            m_Out.SetEnabled(target.mode != BezierEditableKnot.Mode.Linear);
        }
    }
}
