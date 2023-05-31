using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class ReadOnlyField : BaseField<string>
    {
        readonly Label m_IndexField;

        public ReadOnlyField(string label) : base(label, new Label() { name = "ReadOnlyValue" })
        {
            style.flexDirection = FlexDirection.Row;

            m_IndexField = this.Q<Label>("ReadOnlyValue");
            m_IndexField.text = value;
            m_IndexField.style.unityTextAlign = TextAnchor.MiddleLeft;
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            m_IndexField.text = newValue;
        }
    }
}
