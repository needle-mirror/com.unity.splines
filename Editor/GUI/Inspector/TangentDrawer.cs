using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

#if !UNITY_2022_1_OR_NEWER
using UnityEditor.UIElements;
#endif

namespace UnityEditor.Splines
{
    sealed class TangentDrawer : ElementDrawer<EditableTangent>
    {
        const string k_TangentDrawerStyle = "tangent-drawer";
        const string k_TangentLabelStyle = "tangent-label";
        const string k_TangentFillerStyle = "tangent-filler";
        
        readonly Label m_TangentLabel;
        readonly TangentModeStrip m_Mode;

        FloatField m_Magnitude;
        Label m_DirectionLabel;
        Vector3Field m_Direction;
        FloatField m_DirectionX;
        FloatField m_DirectionY;
        FloatField m_DirectionZ;

        public TangentDrawer()
        {
            AddToClassList(k_TangentDrawerStyle);
            Add(m_TangentLabel = new Label());
            m_TangentLabel.style.height = 24;
            m_TangentLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            Add(m_Mode = new TangentModeStrip());
            
            CreateTangentFields();
            
            m_Magnitude.RegisterValueChangedCallback((evt) =>
            {
                UpdateTangentMagnitude(evt.newValue);
                m_Direction.SetValueWithoutNotify(target.localPosition);
                RoundFloatFieldsValues();
            });

            m_Direction.RegisterValueChangedCallback((evt) =>
            {
                IgnoreKnotCallbacks(true);
                target.localPosition = evt.newValue;
                IgnoreKnotCallbacks(false);
                m_Magnitude.SetValueWithoutNotify(Round(math.length(target.localPosition)));
            });
        }

        public override void Update()
        {
            base.Update();

            m_TangentLabel.text = GetTangentLabel();
            m_Mode.SetElement(target);
            m_Magnitude.SetValueWithoutNotify(math.length(target.localPosition));
            m_Direction.SetValueWithoutNotify(target.localPosition);

            RoundFloatFieldsValues();
            //Disabling edition when using linear, mirrored or continuous tangents
            EnableElements(m_Mode.GetMode());
        }

        void CreateTangentFields()
        {
            m_Magnitude = new FloatField("Magnitude",6);

            m_DirectionLabel = new Label("Direction");
            m_DirectionLabel.AddToClassList(k_TangentLabelStyle);
            
            var filler = new VisualElement();
            filler.AddToClassList(k_TangentFillerStyle);

            m_Direction = new Vector3Field(){name = "direction"};
            m_DirectionX = m_Direction.Q<FloatField>("unity-x-input");
            m_DirectionY = m_Direction.Q<FloatField>("unity-y-input");
            m_DirectionZ = m_Direction.Q<FloatField>("unity-z-input");
            
            //Build UI Hierarchy
            Add(m_Magnitude);
            Add(m_DirectionLabel);
            Add(filler);
            filler.Add(m_Direction);
        }

        string GetTangentLabel()
        {
            var inOutLabel = target.tangentIndex == 0 ? "In" : "Out";
            string label = "Tangent "+inOutLabel+" selected (Knot "+target.owner.index+")";
            return label;
        }

        void UpdateTangentMagnitude(float value)
        {
            var direction = new float3(0, 0, 1);
            
            if(math.length(target.localPosition) > 0)
                direction = math.normalize(target.localPosition);

            IgnoreKnotCallbacks(true);
            target.localPosition = value * direction;
            IgnoreKnotCallbacks(false);
        }

        void RoundFloatFieldsValues()
        {
            m_Magnitude.SetValueWithoutNotify(Round(m_Magnitude.value));
            m_DirectionX.SetValueWithoutNotify(Round(m_DirectionX.value));
            m_DirectionY.SetValueWithoutNotify(Round(m_DirectionY.value));
            m_DirectionZ.SetValueWithoutNotify(Round(m_DirectionZ.value));
        }

        void EnableElements(BezierEditableKnot.Mode mode)
        {
            var bezierTangent = m_Mode.GetMode() != BezierEditableKnot.Mode.Linear;
            var brokenMode = m_Mode.GetMode() == BezierEditableKnot.Mode.Broken;
            m_Magnitude.SetEnabled(bezierTangent);
            m_DirectionLabel.SetEnabled(brokenMode);
            m_Direction.SetEnabled(brokenMode);
        }
    }
}
