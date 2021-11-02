using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class TangentDrawer : ElementDrawer<EditableTangent>
    {
        readonly Vector3Field m_Position;

        public TangentDrawer()
        {
            Add(m_Position = new Vector3Field(L10n.Tr("Position")) {name = "Position"});

            m_Position.RegisterValueChangedCallback((evt) => target.localPosition = evt.newValue);
        }

        public override void Update()
        {
            base.Update();

            m_Position.SetValueWithoutNotify(target.localPosition);
        }
    }
}
