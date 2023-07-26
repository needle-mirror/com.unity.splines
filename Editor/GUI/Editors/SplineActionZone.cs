using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    sealed class SplineActionButtons : VisualElement
    {
        static readonly string k_LinkButtonLabel = L10n.Tr("Link");
        static readonly string k_UnlinkButtonLabel = L10n.Tr("Unlink");
        static readonly string k_SplitButtonLabel = L10n.Tr("Split");
        static readonly string k_JoinButtonLabel = L10n.Tr("Join");
        static readonly string k_ReverseFlowButtonLabel = L10n.Tr("Reverse Spline Flow");

        static readonly string k_LinkButtonTooltip = L10n.Tr("Set knots to the same position and link them.");
        static readonly string k_UnlinkButtonTooltip = L10n.Tr("Unlink knots.");
        static readonly string k_SplitButtonTooltip = L10n.Tr("Divide a knot with two segments into two knots.");
        static readonly string k_JoinButtonTooltip = L10n.Tr("Connect the ends of two splines to each other.");
        static readonly string k_ReverseFlowButtonTooltip = L10n.Tr("Reverse the direction of a spline.");

        static readonly List<SelectableKnot> m_KnotBuffer = new List<SelectableKnot>();

        IReadOnlyList<SplineInfo> m_SelectedSplines = new List<SplineInfo>();

        readonly Button m_LinkButton;
        readonly Button m_UnlinkButton;
        readonly Button m_SplitButton;
        readonly Button m_JoinButton;
        readonly Button m_ReverseFlowButton;

        public SplineActionButtons()
        {
#if !UNITY_2023_2_OR_NEWER
            Add(new Separator());

            style.flexDirection = FlexDirection.Column;

            var firstRow = new VisualElement();
            Add(firstRow);
            firstRow.AddToClassList("button-strip");
            firstRow.style.flexDirection = FlexDirection.Row;

            m_LinkButton = new Button();
            m_LinkButton.text = k_LinkButtonLabel;
            m_LinkButton.tooltip = k_LinkButtonTooltip;
            m_LinkButton.style.flexGrow = new StyleFloat(1);
            m_LinkButton.clicked += OnLinkClicked;
            m_LinkButton.AddToClassList("button-strip-button");
            m_LinkButton.AddToClassList("button-strip-button--left");
            firstRow.Add(m_LinkButton);

            m_UnlinkButton = new Button();
            m_UnlinkButton.text = k_UnlinkButtonLabel;
            m_UnlinkButton.tooltip = k_UnlinkButtonTooltip;
            m_UnlinkButton.style.flexGrow = new StyleFloat(1);
            m_UnlinkButton.clicked += OnUnlinkClicked;
            m_UnlinkButton.AddToClassList("button-strip-button");
            m_UnlinkButton.AddToClassList("button-strip-button--right");
            m_UnlinkButton.AddToClassList("button-strip-button--right-spacer");
            firstRow.Add(m_UnlinkButton);

            m_SplitButton = new Button();
            m_SplitButton.text = k_SplitButtonLabel;
            m_SplitButton.tooltip = k_SplitButtonTooltip;
            m_SplitButton.style.flexGrow = 1;
            m_SplitButton.clicked += OnSplitClicked;
            m_SplitButton.AddToClassList("button-strip-button");
            m_SplitButton.AddToClassList("button-strip-button--left");
            m_SplitButton.AddToClassList("button-strip-button--left-spacer");
            firstRow.Add(m_SplitButton);

            m_JoinButton = new Button();
            m_JoinButton.text = k_JoinButtonLabel;
            m_JoinButton.tooltip = k_JoinButtonTooltip;
            m_JoinButton.style.flexGrow = 1;
            m_JoinButton.clicked += OnJoinClicked;
            m_JoinButton.AddToClassList("button-strip-button");
            m_JoinButton.AddToClassList("button-strip-button--right");
            firstRow.Add(m_JoinButton);

            m_ReverseFlowButton = new Button();
            m_ReverseFlowButton.text = k_ReverseFlowButtonLabel;
            m_ReverseFlowButton.tooltip = k_ReverseFlowButtonTooltip;
            m_ReverseFlowButton.style.flexGrow = 1;
            m_ReverseFlowButton.clicked += OnReverseFlowClicked;
            Add(m_ReverseFlowButton);
#endif
        }

        void OnLinkClicked()
        {
            SplineSelection.GetElements(m_SelectedSplines, m_KnotBuffer);
            EditorSplineUtility.LinkKnots(m_KnotBuffer);
            RefreshSelection(m_SelectedSplines);
        }

        void OnUnlinkClicked()
        {
            SplineSelection.GetElements(m_SelectedSplines, m_KnotBuffer);
            EditorSplineUtility.UnlinkKnots(m_KnotBuffer);
            RefreshSelection(m_SelectedSplines);
        }

        void OnSplitClicked()
        {
            EditorSplineUtility.RecordSelection("Split knot");
            SplineSelection.Set(EditorSplineUtility.SplitKnot(m_KnotBuffer[0]));
        }

        void OnJoinClicked()
        {
            EditorSplineUtility.RecordSelection("Join knot");
            SplineSelection.Set(EditorSplineUtility.JoinKnots(m_KnotBuffer[0], m_KnotBuffer[1]));
        }

        public void RefreshSelection(IReadOnlyList<SplineInfo> selectedSplines)
        {
#if !UNITY_2023_2_OR_NEWER
            SplineSelection.GetElements(selectedSplines, m_KnotBuffer);

            m_LinkButton.SetEnabled(SplineSelectionUtility.CanLinkKnots(m_KnotBuffer));
            m_UnlinkButton.SetEnabled(SplineSelectionUtility.CanUnlinkKnots(m_KnotBuffer));

            m_SplitButton.SetEnabled(SplineSelectionUtility.CanSplitSelection(m_KnotBuffer));
            m_JoinButton.SetEnabled(SplineSelectionUtility.CanJoinSelection(m_KnotBuffer));

            m_SelectedSplines = selectedSplines;
#endif
        }

        void OnReverseFlowClicked()
        {
            EditorSplineUtility.RecordSelection("Reverse Selected Splines Flow");
            EditorSplineUtility.ReverseSplinesFlow(m_SelectedSplines);
        }
    }
}
