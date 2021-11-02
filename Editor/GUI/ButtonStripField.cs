using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class ButtonStripField : BaseField<int>
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

        string[] m_Choices = new string[0];
        readonly VisualElement m_ButtonStrip;

        public string[] choices
        {
            get => m_Choices;
            set
            {
                m_Choices = value ?? new string[0];
                RebuildButtonStrip();
            }
        }

        public ButtonStripField() : this("") {}

        public ButtonStripField(string label) : base(label, new VisualElement {name = "ButtonStrip"})
        {
            styleSheets.Add(s_StyleSheet);

            m_ButtonStrip = this.Q("ButtonStrip");
            m_ButtonStrip.AddToClassList(k_ButtonStripClass);
            m_ButtonStrip.AddToClassList(inputUssClassName);
        }

        Button CreateButton(string iconName)
        {
            var button = new Button();
            button.displayTooltipWhenElided = false;
            button.tooltip = L10n.Tr(iconName);
            var icon = new VisualElement { name = iconName };
            icon.AddToClassList(k_ButtonIconClass);
            button.AddToClassList(k_ButtonClass);
            button.Add(icon);
            return button;
        }

        public override void SetValueWithoutNotify(int newValue)
        {
            newValue = math.clamp(newValue, 0, choices.Length - 1);
            base.SetValueWithoutNotify(newValue);

            UpdateButtonsState(newValue);
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
