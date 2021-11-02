using System;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class ElementInspector : VisualElement, IDisposable
    {
        static readonly string k_NoSelectionMessage = L10n.Tr("No element selected");
        static readonly string k_MultiSelectNoAllowedMessage = L10n.Tr("Multi select not supported");

        readonly Label m_ErrorMessage;

        Type m_InspectedType;
        EditableKnot m_TargetKnot;
        IElementDrawer m_ElementDrawer;

        static StyleSheet s_CommonStyleSheet;
        static StyleSheet s_ThemeStyleSheet;
        
        public ElementInspector()
        {
            if (s_CommonStyleSheet == null)
                s_CommonStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.splines/Editor/Stylesheets/SplineInspectorCommon.uss");
            if (s_ThemeStyleSheet == null)
                s_ThemeStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"Packages/com.unity.splines/Editor/Stylesheets/SplineInspector{(EditorGUIUtility.isProSkin ? "Dark" : "Light")}.uss");
            
            styleSheets.Add(s_CommonStyleSheet);
            styleSheets.Add(s_ThemeStyleSheet);

            m_ErrorMessage = new Label();
            Add(m_ErrorMessage);

            EditableKnot.knotModified += OnKnotModified;
        }

        public void Dispose()
        {
            EditableKnot.knotModified -= OnKnotModified;
        }

        void OnKnotModified(EditableKnot knot)
        {
            if (m_TargetKnot == knot)
                m_ElementDrawer?.Update();
        }
        
        public void SetElement(ISplineElement element, bool multiSelect)
        {
            UpdateDrawerForElementType(multiSelect ? null : element?.GetType());
            
            if (multiSelect) 
            {
                ShowErrorMessage(k_MultiSelectNoAllowedMessage);
            }
            else if (element == null || m_ElementDrawer == null)
            {

                ShowErrorMessage(k_NoSelectionMessage);
            }
            else
            {
                if (element is EditableKnot knot)
                    m_TargetKnot = knot;
                else if (element is EditableTangent tangent)
                    m_TargetKnot = tangent.owner;

                HideErrorMessage();
                m_ElementDrawer.SetTarget(element);
                m_ElementDrawer.Update();
            }
        }

        void ShowErrorMessage(string error)
        {
            m_ErrorMessage.style.display = DisplayStyle.Flex;

            m_ErrorMessage.text = error;
        }

        void HideErrorMessage()
        {
            m_ErrorMessage.style.display = DisplayStyle.None;
        }

        void UpdateDrawerForElementType(Type targetType)
        {
            if (m_InspectedType == targetType)
                return;

            if (m_ElementDrawer != null)
                ((VisualElement)m_ElementDrawer).RemoveFromHierarchy();

            if (targetType == null)
                m_ElementDrawer = null;
            else if (typeof(BezierEditableKnot).IsAssignableFrom(targetType))
                m_ElementDrawer = new BezierKnotDrawer();
            else if (typeof(EditableKnot).IsAssignableFrom(targetType))
                m_ElementDrawer = new KnotDrawer();
            else if (typeof(EditableTangent).IsAssignableFrom(targetType))
                m_ElementDrawer = new TangentDrawer();
            else
                m_ElementDrawer = null;

            if (m_ElementDrawer != null)
                Add((VisualElement)m_ElementDrawer);

            m_InspectedType = targetType;
        }
    }
}
