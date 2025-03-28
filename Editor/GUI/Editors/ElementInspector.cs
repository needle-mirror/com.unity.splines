using System;
using UnityEngine.Splines;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Splines
{
    sealed class ElementInspector : VisualElement, IDisposable
    {
        public static bool ignoreKnotCallbacks = false;

        static readonly string k_NoSelectionMessage = L10n.Tr("No element selected");

        readonly Label m_KnotIdentifierLabel;
        readonly Label m_ErrorMessage;
        readonly SplineActionButtons m_SplineActionButtons;
        readonly VisualElement m_SplineDrawerRoot;

        IElementDrawer m_ElementDrawer;
        readonly CommonElementDrawer m_CommonElementDrawer = new CommonElementDrawer();
        readonly BezierKnotDrawer m_BezierKnotDrawer = new BezierKnotDrawer();
        readonly TangentDrawer m_TangentDrawer = new TangentDrawer();

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

            Add(m_KnotIdentifierLabel = new Label());
            m_KnotIdentifierLabel.style.height = 24;
            m_KnotIdentifierLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

            Add(m_ErrorMessage = new Label { name = "ErrorMessage"});
            Add(m_SplineDrawerRoot = new VisualElement());
            Add(m_SplineActionButtons = new SplineActionButtons());

            Spline.Changed += OnKnotModified;
        }

        public void Dispose()
        {
            Spline.Changed -= OnKnotModified;
        }

        void OnKnotModified(Spline spline, int index, SplineModification modification)
        {
            if (modification == SplineModification.KnotModified && !ignoreKnotCallbacks && m_ElementDrawer != null && m_ElementDrawer.HasKnot(spline, index))
                m_ElementDrawer.Update();
        }

        public void UpdateSelection(IReadOnlyList<SplineInfo> selectedSplines)
        {
            UpdateDrawerForElements(selectedSplines);

            if (SplineSelection.Count < 1 || m_ElementDrawer == null)
            {
                ShowErrorMessage(k_NoSelectionMessage);
                m_KnotIdentifierLabel.style.display = DisplayStyle.None;
                m_SplineActionButtons.style.display = DisplayStyle.None;
            }
            else
            {
                HideErrorMessage();
                m_ElementDrawer.PopulateTargets(selectedSplines);
                m_ElementDrawer.Update();
                m_KnotIdentifierLabel.text = m_ElementDrawer.GetLabelForTargets();
                m_KnotIdentifierLabel.style.display = DisplayStyle.Flex;
                m_SplineActionButtons.style.display = DisplayStyle.Flex;
            }

            m_SplineActionButtons.RefreshSelection(selectedSplines);
        }

        void UpdateDrawerForElements(IReadOnlyList<SplineInfo> selectedSplines)
        {
            bool hasKnot = SplineSelection.HasAny<SelectableKnot>(selectedSplines);
            bool hasTangent = SplineSelection.HasAny<SelectableTangent>(selectedSplines);

            IElementDrawer targetDrawer;
            if (hasKnot && hasTangent)
                targetDrawer = m_CommonElementDrawer;
            else if (hasKnot)
                targetDrawer = m_BezierKnotDrawer;
            else if (hasTangent)
                targetDrawer = m_TangentDrawer;
            else
                targetDrawer = null;

            if (targetDrawer == m_ElementDrawer)
                return;

            m_SplineDrawerRoot.Clear();
            if (targetDrawer != null)
                m_SplineDrawerRoot.Add((VisualElement) targetDrawer);

            m_ElementDrawer = targetDrawer;
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
    }
}