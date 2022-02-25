using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

#if !UNITY_2022_1_OR_NEWER
using UnityEditor.UIElements;
#endif

namespace UnityEditor.Splines
{
    sealed class BezierKnotDrawer : KnotDrawer<BezierEditableKnot>
    {
        const string k_TangentFoldoutStyle = "tangent-drawer";
        
        readonly TangentModeStrip m_Mode;
        
        readonly FloatField m_InMagnitude;
        readonly Vector3Field m_In;
        readonly FloatField m_InX;
        readonly FloatField m_InY;
        readonly FloatField m_InZ;
        readonly FloatField m_OutMagnitude;
        readonly Vector3Field m_Out;
        readonly FloatField m_OutX;
        readonly FloatField m_OutY;
        readonly FloatField m_OutZ;

        public BezierKnotDrawer()
        {
            Add(m_Mode = new TangentModeStrip());
            
            ( m_InMagnitude, m_In ) = CreateTangentFoldout("Tangent In", "TangentIn");
            m_InX = m_In.Q<FloatField>("unity-x-input");
            m_InY = m_In.Q<FloatField>("unity-y-input");
            m_InZ = m_In.Q<FloatField>("unity-z-input");
            
            ( m_OutMagnitude, m_Out ) = CreateTangentFoldout("Tangent Out", "TangentOut");
            m_OutX = m_Out.Q<FloatField>("unity-x-input");
            m_OutY = m_Out.Q<FloatField>("unity-y-input");
            m_OutZ = m_Out.Q<FloatField>("unity-z-input");
            
            m_InMagnitude.RegisterValueChangedCallback((evt) =>
            {
                UpdateTangentMagnitude(target.tangentIn, m_InMagnitude, evt.newValue, -1f);
                m_In.SetValueWithoutNotify(target.tangentIn.localPosition);
                m_Out.SetValueWithoutNotify(target.tangentOut.localPosition);
                m_OutMagnitude.SetValueWithoutNotify(Round(math.length(target.tangentOut.localPosition)));
                RoundFloatFieldsValues();
            });
            
            m_In.RegisterValueChangedCallback((evt) =>
            {
                IgnoreKnotCallbacks(true);
                target.tangentIn.localPosition = evt.newValue;
                IgnoreKnotCallbacks(false);
                m_InMagnitude.SetValueWithoutNotify(Round(math.length(target.tangentIn.localPosition)));
            });
            
            m_OutMagnitude.RegisterValueChangedCallback((evt) =>
            {
                UpdateTangentMagnitude(target.tangentOut, m_OutMagnitude, evt.newValue, 1f);
                m_Out.SetValueWithoutNotify(target.tangentOut.localPosition);
                m_In.SetValueWithoutNotify(target.tangentIn.localPosition);
                m_InMagnitude.SetValueWithoutNotify(Round(math.length(target.tangentIn.localPosition)));
                RoundFloatFieldsValues();
            });
            
            m_Out.RegisterValueChangedCallback((evt) =>
            {
                IgnoreKnotCallbacks(true);
                target.tangentOut.localPosition = evt.newValue;
                IgnoreKnotCallbacks(false);
                m_OutMagnitude.SetValueWithoutNotify(Round(math.length(target.tangentOut.localPosition)));
            });
        }

        public override void Update()
        {
            base.Update();

            m_Mode.SetElement(target);
            m_In.SetValueWithoutNotify(target.tangentIn.localPosition);
            m_Out.SetValueWithoutNotify(target.tangentOut.localPosition);
            m_InMagnitude.SetValueWithoutNotify(math.length(target.tangentIn.localPosition));
            m_OutMagnitude.SetValueWithoutNotify(math.length(target.tangentOut.localPosition));

            RoundFloatFieldsValues();
            //Disabling edition when using linear tangents
            EnableElements(target.mode);
        }

        void UpdateTangentMagnitude(EditableTangent tangent, FloatField magnitudeField, float value, float directionSign)
        {
            if (value < 0f)
            {
                magnitudeField.SetValueWithoutNotify(0f);
                value = 0f;
            }

            var direction = new float3(0, 0, directionSign);
            if(math.length(tangent.localPosition) > 0)
                direction = math.normalize(tangent.localPosition);
         
            IgnoreKnotCallbacks(true);
            tangent.localPosition = value * direction;
            IgnoreKnotCallbacks(false);
        }

        void RoundFloatFieldsValues()
        {
            m_InMagnitude.SetValueWithoutNotify(Round(m_InMagnitude.value));
            m_InX.SetValueWithoutNotify(Round(m_InX.value));
            m_InY.SetValueWithoutNotify(Round(m_InY.value));
            m_InZ.SetValueWithoutNotify(Round(m_InZ.value));
            m_OutMagnitude.SetValueWithoutNotify(Round(m_OutMagnitude.value));
            m_OutX.SetValueWithoutNotify(Round(m_OutX.value));
            m_OutY.SetValueWithoutNotify(Round(m_OutY.value));
            m_OutZ.SetValueWithoutNotify(Round(m_OutZ.value));
        }

        void EnableElements(BezierEditableKnot.Mode mode)
        {
            var bezierTangent = mode != BezierEditableKnot.Mode.Linear;
            var brokenTangents = mode == BezierEditableKnot.Mode.Broken;
            m_InMagnitude.SetEnabled(bezierTangent);
            m_OutMagnitude.SetEnabled(bezierTangent);
            m_In.SetEnabled(brokenTangents);
            m_Out.SetEnabled(brokenTangents);
        }
        
        (FloatField,Vector3Field)  CreateTangentFoldout(string text, string vect3name)
        {
            //Create Elements
            var foldoutRoot = new VisualElement();
            foldoutRoot.AddToClassList(k_TangentFoldoutStyle);
            
            var foldout = new Foldout() { value = false };
            var foldoutToggle = foldout.Q<Toggle>();
            var magnitude = new FloatField(L10n.Tr(text), 3);
            var vector3Field = new Vector3Field() { name = vect3name };
                
            //Build UI Hierarchy
            Add(foldoutRoot);
            foldoutRoot.Add(foldout);
            foldoutToggle.Add(magnitude);
            foldout.Add(vector3Field);

            return (magnitude, vector3Field);
        }

        public override void OnTargetSet()
        {
            m_In.parent.SetEnabled(SplineSelectionUtility.IsSelectable(target.spline, target.index, target.tangentIn));
            m_Out.parent.SetEnabled(SplineSelectionUtility.IsSelectable(target.spline, target.index, target.tangentOut));
        }
    }
}
