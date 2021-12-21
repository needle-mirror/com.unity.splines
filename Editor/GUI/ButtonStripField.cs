using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class ButtonStripField : VisualElement
    {
        static readonly StyleSheet s_StyleSheet;

        static ButtonStripField()
        {
            s_StyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.splines/Editor/Stylesheets/ButtonStripField.uss");
        }

        const string k_ButtonStripClass = "button-strip";
        const string k_ButtonClass = "button-strip-button";
        const string k_ButtonIconClass = "button-strip-button__icon";
        const string k_LeftButtonClass = k_ButtonClass + "--left";
        const string k_MiddleButtonClass = k_ButtonClass + "--middle";
        const string k_RightButtonClass = k_ButtonClass + "--right";
        const string k_AloneButtonClass = k_ButtonClass + "--alone";
        const string k_CheckedButtonClass = k_ButtonClass + "--checked";

        GUIContent[] m_Choices = new GUIContent[0];
        readonly VisualElement m_ButtonStrip;

        public GUIContent[] choices
        {
            get => m_Choices;
            set
            {
                m_Choices = value ?? new GUIContent[0];
                RebuildButtonStrip();
            }
        }

        int m_Value;

        public int value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                UpdateButtonsState(m_Value);
                OnValueChanged?.Invoke(m_Value);
            }
        }

        public event Action<int> OnValueChanged;

        public ButtonStripField()
        {
            styleSheets.Add(s_StyleSheet);

            m_ButtonStrip = this;
            m_ButtonStrip.AddToClassList(k_ButtonStripClass);
        }

        Button CreateButton(GUIContent content)
        {
            var button = new Button();
            button.displayTooltipWhenElided = false;
            button.tooltip = L10n.Tr(content.tooltip);
            var icon = new VisualElement { name = content.text };
            icon.AddToClassList(k_ButtonIconClass);
            button.AddToClassList(k_ButtonClass);
            button.Add(icon);
            return button;
        }

        //public override void SetValueWithoutNotify(int newValue)
        public void SetValueWithoutNotify(int newValue)
        {
            m_Value = math.clamp(newValue, 0, choices.Length - 1);
            UpdateButtonsState(m_Value);
        }

        void UpdateButtonsState(int value)
        {
            List<Button> buttons = m_ButtonStrip.Query<Button>().ToList();
            for (int i = 0; i < buttons.Count; ++i)
            {
                buttons[i].EnableInClassList(k_CheckedButtonClass, value == i);
            }
        }

        void RebuildButtonStrip()
        {
            m_ButtonStrip.Clear();
            for (int i = 0, count = choices.Length; i < count; ++i)
            {
                var button = CreateButton(choices[i]);
                var targetValue = i;
                button.clicked += () => { value = targetValue; };
                if (choices.Length == 1)
                    button.AddToClassList(k_AloneButtonClass);
                else if (i == 0)
                    button.AddToClassList(k_LeftButtonClass);
                else if (i == count - 1)
                    button.AddToClassList(k_RightButtonClass);
                else 
                    button.AddToClassList(k_MiddleButtonClass);

                m_ButtonStrip.Add(button);
            }

            UpdateButtonsState(value);
        }
    }
}
