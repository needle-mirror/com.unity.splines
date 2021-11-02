using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class KnotDrawer : KnotDrawer<EditableKnot> {}

    class KnotDrawer<T> : ElementDrawer<T> where T : EditableKnot
    {
        readonly ReadOnlyField m_KnotIndex;
        readonly Vector3Field m_Position;
        readonly Vector3Field m_Rotation;

        public KnotDrawer()
        {
            Add(m_KnotIndex = new ReadOnlyField(L10n.Tr("Knot Index")));
            Add(m_Position = new Vector3Field(L10n.Tr("Position")) { name = "Position" });
            Add(m_Rotation = new Vector3Field(L10n.Tr("Rotation")) { name = "Rotation" });

            m_Position.RegisterValueChangedCallback((evt) => target.localPosition = evt.newValue);
            m_Rotation.RegisterValueChangedCallback((evt) => target.localRotation = Quaternion.Euler(evt.newValue));
        }

        public override void Update()
        {
            base.Update();

            m_KnotIndex.SetValueWithoutNotify(target.index.ToString());
            m_Position.SetValueWithoutNotify(target.localPosition);
            m_Rotation.SetValueWithoutNotify(((Quaternion)target.localRotation).eulerAngles);
        }
    }
}
