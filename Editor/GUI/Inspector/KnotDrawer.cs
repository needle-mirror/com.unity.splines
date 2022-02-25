using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class KnotDrawer : KnotDrawer<EditableKnot> {}

    class KnotDrawer<T> : ElementDrawer<T> where T : EditableKnot
    {
        readonly Label m_KnotLabel;
        readonly Vector3Field m_Position;
        readonly FloatField m_PositionX;
        readonly FloatField m_PositionY;
        readonly FloatField m_PositionZ;
        readonly Vector3Field m_Rotation;
        readonly FloatField m_RotationX;
        readonly FloatField m_RotationY;
        readonly FloatField m_RotationZ;

        public KnotDrawer()
        {
            Add(m_KnotLabel = new Label());
            m_KnotLabel.style.height = 24;
            m_KnotLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

            VisualElement row;
            Add(row = new VisualElement(){name = "Vector3WithIcon"});
            row.style.flexDirection = FlexDirection.Row;
            row.Add(new VisualElement(){name = "PositionIcon"});
            row.Add(m_Position = new Vector3Field() { name = "Position" });
            
            m_Position.style.flexGrow = 1;
            m_PositionX = m_Position.Q<FloatField>("unity-x-input");
            m_PositionY = m_Position.Q<FloatField>("unity-y-input");
            m_PositionZ = m_Position.Q<FloatField>("unity-z-input");
            
            Add(row = new VisualElement(){name = "Vector3WithIcon"});
            row.style.flexDirection = FlexDirection.Row;
            row.Add(new VisualElement(){name = "RotationIcon"});
            row.Add(m_Rotation = new Vector3Field() { name = "Rotation" });;
            
            m_Rotation.style.flexGrow = 1;
            m_RotationX = m_Rotation.Q<FloatField>("unity-x-input");
            m_RotationY = m_Rotation.Q<FloatField>("unity-y-input");
            m_RotationZ = m_Rotation.Q<FloatField>("unity-z-input");

            m_Position.RegisterValueChangedCallback((evt) =>
            {
                IgnoreKnotCallbacks(true);
                target.localPosition = evt.newValue;
                IgnoreKnotCallbacks(false);
            });
            m_Rotation.RegisterValueChangedCallback((evt) =>
            {
                IgnoreKnotCallbacks(true);
                target.localRotation = Quaternion.Euler(evt.newValue);
                IgnoreKnotCallbacks(false);
            });
        }

        public override void Update()
        {
            base.Update();

            m_KnotLabel.text = "Knot "+target.index.ToString()+" selected";
            m_Position.SetValueWithoutNotify(target.localPosition);
            m_Rotation.SetValueWithoutNotify(((Quaternion)target.localRotation).eulerAngles);
            
            RoundFloatFieldsValues();
        }
        
        void RoundFloatFieldsValues()
        {
            m_PositionX.SetValueWithoutNotify(Round(m_PositionX.value));
            m_PositionY.SetValueWithoutNotify(Round(m_PositionY.value));
            m_PositionZ.SetValueWithoutNotify(Round(m_PositionZ.value));
            m_RotationX.SetValueWithoutNotify(Round(m_RotationX.value));
            m_RotationY.SetValueWithoutNotify(Round(m_RotationY.value));
            m_RotationZ.SetValueWithoutNotify(Round(m_RotationZ.value));
        }
    }
}
