using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class TangentModeStrip : VisualElement
    {
        readonly GUIContent[] modes = new[]
        {
            EditorGUIUtility.TrTextContent("Linear", "Linear Tangents:\nTangents are pointing to the previous/next spline knot."),
            EditorGUIUtility.TrTextContent("Mirrored", "Mirrored Tangents:\nIf Knot or InTangent is selected, OutTangent will be mirrored on InTangent. Else, InTangent will be mirrored on OutTangent."),
            EditorGUIUtility.TrTextContent("Continuous", "Continuous Tangents:\nInTangent and OutTangent are always aligned."),
            EditorGUIUtility.TrTextContent("Broken", "Broken Tangents:\nInTangent and OutTangent are dissociated.")
        };
        readonly ButtonStripField m_ModeStrip;

        ISplineElement m_Target;
        
        internal TangentModeStrip()
        {
            Add(m_ModeStrip = new ButtonStripField() { name = "TangentMode" });
            m_ModeStrip.choices = modes;
        }

        internal BezierEditableKnot.Mode GetMode()
        {
            return (BezierEditableKnot.Mode)m_ModeStrip.value;
        }

        internal void SetElement(ISplineElement target)
        {
            if(m_Target != target)
            {
                m_Target = target;
                BezierEditableKnot knot = null;
                if(target is BezierEditableKnot targetedKnot)
                    knot = targetedKnot;
                else if(m_Target is EditableTangent targetedTangent && targetedTangent.owner is BezierEditableKnot tangentOwner)
                    knot = tangentOwner;
                
                m_ModeStrip.OnValueChanged += ((newMode) => UpdateMode(knot, (BezierEditableKnot.Mode) newMode));
            }
            
            if(m_Target is BezierEditableKnot tKnot)
                UpdateValue((int)tKnot.mode);
            else if(m_Target is EditableTangent tTangent && tTangent.owner is BezierEditableKnot tangentOwner)
                UpdateValue((int)tangentOwner.mode);
        }

        void UpdateMode(BezierEditableKnot knot, BezierEditableKnot.Mode mode)
        {
            if(knot.mode == mode)
                return;
            
            if(mode is BezierEditableKnot.Mode.Mirrored or BezierEditableKnot.Mode.Continuous)
            {
                // m_Target is the knot "knot", use the InTangent to resolve the new mode
                var refTangent = knot.GetTangent(0);
                var otherTangent = knot.GetTangent(1);

                //Else if target is a tangent, update the values regarding the selected tangent
                if(m_Target is EditableTangent { owner: BezierEditableKnot owner } target)
                {
                    refTangent = target;
                    for(int i = 0; i < owner.tangentCount; ++i)
                    {
                        var tangent = owner.GetTangent(i);
                        if(tangent != target)
                            otherTangent = tangent;
                    }
                }

                if(mode == BezierEditableKnot.Mode.Mirrored)
                    otherTangent.SetLocalPositionNoNotify(-refTangent.localPosition);
                else //Continuous mode
                    otherTangent.SetLocalPositionNoNotify(-math.normalize(refTangent.localPosition) * math.length(otherTangent.localPosition));
            }
            
            knot.SetMode(mode);
        }

        internal void UpdateValue(int modeValue)
        {
            m_ModeStrip.SetValueWithoutNotify(modeValue);
        }
    }
}
