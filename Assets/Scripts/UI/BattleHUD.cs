using System;
using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Battle;
using PhalanxChronicle.Core;
using PhalanxChronicle.Localization;
using PhalanxChronicle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace PhalanxChronicle.UI
{
    internal enum BattleHudOverlayGate
    {
        SuppressUnitInfo,
        BlockInteraction,
        PauseAutoMode,
    }

    public enum BattleHudUiVisibilityMode
    {
        Normal,
        HideBattleShell,
    }

    public sealed class BattleHUD : MonoBehaviour
    {
        private const int FeedLimit = 8;

        private readonly List<string> feedEntries = new List<string>();

        private BattleHudModelBuilder hudModelBuilder;
        private BattleOverviewModel currentOverview = new BattleOverviewModel();
        private BattleForecastModel currentForecast;
        private BattleHudDecisionContextModel currentDecisionContext;
        private BattleContextRibbonView contextRibbonView;
        private BattleSelectedUnitView selectedUnitView;
        private BattleRosterSidebarView rosterSidebarView;
        private CampaignOverlayView campaignOverlayView;

        private Text resultTitleLabel;
        private Text resultSummaryLabel;
        private Text resultRewardLabel;
        private Text resultSpecialLabel;
        private Text resultUnitsLabel;
        private Text resultContinueLabel;
        private LayoutElement resultSummaryLayout;
        private LayoutElement resultRewardLayout;
        private LayoutElement resultRewardEntriesLayout;
        private LayoutElement resultSpecialLayout;
        private LayoutElement resultUnitsLayout;
        private Transform resultRewardEntriesRoot;
        private Text dialogueSpeakerLabel;
        private Text dialogueBodyLabel;
        private Text dialogueContinueLabel;
        private Text onboardingProgressLabel;
        private Text onboardingTitleLabel;
        private Text onboardingBodyLabel;
        private Text onboardingHintLabel;

        private Button resultAdvanceButton;
        private Button dialogueAdvanceButton;
        private Button onboardingSkipButton;
        private Button confirmPrimaryButton;
        private Button confirmSecondaryButton;
        private GameObject resultPanel;
        private GameObject dialogueOverlay;
        private GameObject onboardingPanel;
        private GameObject confirmDialogOverlay;
        private Text confirmDialogTitleLabel;
        private Text confirmDialogBodyLabel;

        public bool IsDialogueVisible => dialogueOverlay != null && dialogueOverlay.activeSelf;

        public bool IsRerollVisible => rosterSidebarView != null && rosterSidebarView.IsRerollVisible;

        public bool IsResultVisible => resultPanel != null && resultPanel.activeSelf;

        public bool IsCampaignOverlayVisible => campaignOverlayView != null && campaignOverlayView.IsVisible;

        public bool IsOnboardingVisible => onboardingPanel != null && onboardingPanel.activeSelf;

        public bool IsConfirmDialogVisible => confirmDialogOverlay != null && confirmDialogOverlay.activeSelf;

        public bool IsAnyBlockingOverlayVisible => IsOverlayGateActive(BattleHudOverlayGate.SuppressUnitInfo);

        public bool HasBlockingInteractionOverlay => IsOverlayGateActive(BattleHudOverlayGate.BlockInteraction);

        public bool HasAutoModePauseOverlay => IsOverlayGateActive(BattleHudOverlayGate.PauseAutoMode);

        public string CurrentObjectiveText => rosterSidebarView != null ? rosterSidebarView.CurrentObjectiveText : string.Empty;

        public IReadOnlyList<string> FeedEntries => feedEntries;

        public void Initialize(
            Transform canvasRoot,
            Action onEndTurn,
            Action onReroll,
            Action onAutoModeRequested,
            Action onDialogueAdvance,
            Action onResultAdvance,
            Action onOnboardingSkip,
            Action onConfirmDialogPrimary,
            Action onConfirmDialogSecondary,
            BattleHudModelBuilder modelBuilder = null)
        {
            hudModelBuilder = modelBuilder ?? new BattleHudModelBuilder();

            selectedUnitView = new BattleSelectedUnitView();
            selectedUnitView.Initialize(canvasRoot);

            rosterSidebarView = new BattleRosterSidebarView();
            rosterSidebarView.Initialize(canvasRoot, onEndTurn, onReroll, onAutoModeRequested, FeedLimit);

            contextRibbonView = new BattleContextRibbonView();
            contextRibbonView.Initialize(canvasRoot);

            BuildResultPanel(canvasRoot, onResultAdvance);
            BuildDialogueOverlay(canvasRoot, onDialogueAdvance);
            BuildOnboardingPanel(canvasRoot, onOnboardingSkip);
            BuildConfirmDialog(canvasRoot, onConfirmDialogPrimary, onConfirmDialogSecondary);

            campaignOverlayView = new CampaignOverlayView();
            campaignOverlayView.Initialize(canvasRoot);

            BindOverview(new BattleOverviewModel());
            BindSelectedUnit(new BattleSelectedUnitModel());
            ClearContext();
            SetAutoModeState(false);
        }

        public void BindOverview(BattleOverviewModel model)
        {
            currentOverview = model ?? new BattleOverviewModel();
            rosterSidebarView?.BindOverview(currentOverview);
            ApplyContextRibbon();
        }

        public void BindSelectedUnit(BattleSelectedUnitModel model)
        {
            selectedUnitView?.Bind(model ?? new BattleSelectedUnitModel());
        }

        public void BindRoster(
            IReadOnlyList<BattleRosterEntryModel> alliedRoster,
            IReadOnlyList<BattleRosterEntryModel> enemyRoster,
            Action<string> onRosterSelected)
        {
            rosterSidebarView?.BindRoster(alliedRoster, enemyRoster, onRosterSelected);
        }

        public void BindForecast(BattleForecastModel model)
        {
            currentForecast = model;
            ApplyContextRibbon();
        }

        public void BindDecisionContextModel(BattleHudDecisionContextModel model)
        {
            currentDecisionContext = model ?? new BattleHudDecisionContextModel();
            currentForecast = currentDecisionContext.ForecastModel;
            ApplyContextRibbon();
        }

        public void ClearForecast()
        {
            currentForecast = null;
            ApplyContextRibbon();
        }

        public void ClearContext()
        {
            currentDecisionContext = new BattleHudDecisionContextModel();
            currentForecast = null;
            ApplyContextRibbon();
        }

        public bool ShouldSuppressUnitInfo()
        {
            return IsOverlayGateActive(BattleHudOverlayGate.SuppressUnitInfo);
        }

        public void SetUiVisibilityMode(BattleHudUiVisibilityMode mode)
        {
            bool visible = mode != BattleHudUiVisibilityMode.HideBattleShell;
            SetBattleShellVisible(visible);
        }

        public void PushFeedEntry(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            feedEntries.Insert(0, text);
            while (feedEntries.Count > FeedLimit)
            {
                feedEntries.RemoveAt(feedEntries.Count - 1);
            }

            rosterSidebarView?.BindFeed(feedEntries);
            ApplyContextRibbon();
        }

        public void SetEndTurnEnabled(bool enabled)
        {
            rosterSidebarView?.SetEndTurnEnabled(enabled);
        }

        public void SetRerollEnabled(bool enabled)
        {
            rosterSidebarView?.SetRerollEnabled(enabled);
        }

        public void SetAutoModeState(bool enabled)
        {
            rosterSidebarView?.SetAutoModeState(enabled);
        }

        internal void ApplyLayout(BattleLayoutMetrics metrics)
        {
            selectedUnitView?.ApplyLayout(metrics);
            rosterSidebarView?.ApplyLayout(metrics);
            contextRibbonView?.ApplyLayout(metrics);
        }

        public void ShowResult(BattleResultModel model)
        {
            resultPanel.SetActive(true);
            BattleResultModel battleResult = model ?? new BattleResultModel();
            resultTitleLabel.text = battleResult.Title;
            resultSummaryLabel.text = battleResult.Summary;
            bool hasStructuredRewardEntries = battleResult.RewardEntries != null && battleResult.RewardEntries.Count > 0;
            resultRewardLabel.text = !hasStructuredRewardEntries && battleResult.RewardLines != null && battleResult.RewardLines.Count > 0
                ? string.Join("\n", battleResult.RewardLines)
                : string.Empty;
            resultSpecialLabel.text = battleResult.SpecialLines != null && battleResult.SpecialLines.Count > 0
                ? string.Join("\n", battleResult.SpecialLines)
                : string.Empty;
            resultUnitsLabel.text = battleResult.UnitLines != null && battleResult.UnitLines.Count > 0
                ? string.Join("\n", battleResult.UnitLines)
                : string.Empty;
            RebuildResultRewardEntries(battleResult.RewardEntries);
            RefreshResultLayout(hasStructuredRewardEntries);
        }

        public void HideResult()
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        private void RebuildResultRewardEntries(IReadOnlyList<BattleRewardEntryModel> rewardEntries)
        {
            if (resultRewardEntriesRoot == null)
            {
                return;
            }

            for (int index = resultRewardEntriesRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(resultRewardEntriesRoot.GetChild(index).gameObject);
            }

            foreach (BattleRewardEntryModel entry in rewardEntries ?? Array.Empty<BattleRewardEntryModel>())
            {
                CreateResultRewardEntry(resultRewardEntriesRoot, entry);
            }
        }

        private static void CreateResultRewardEntry(Transform parent, BattleRewardEntryModel entry)
        {
            if (parent == null || entry == null || string.IsNullOrWhiteSpace(entry.Label))
            {
                return;
            }

            GameObject row = new GameObject("ResultRewardEntry", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = 30f;

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            Sprite iconSprite = RuntimeSpriteLibrary.GetItemIcon(entry.IconItemId);
            if (iconSprite != null)
            {
                GameObject badge = BattleHudFactory.CreateInsetPanel("ResultRewardBadge", row.transform, 28f, entry.IsPrimaryReward ? BattleUiTheme.PanelReward : BattleUiTheme.PanelGhost);
                LayoutElement badgeLayout = badge.GetComponent<LayoutElement>();
                badgeLayout.preferredWidth = 28f;
                badgeLayout.preferredHeight = 28f;
                badgeLayout.flexibleWidth = 0f;
                badgeLayout.flexibleHeight = 0f;

                GameObject iconObject = new GameObject("ResultRewardIcon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(badge.transform, false);
                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(5f, 5f);
                iconRect.offsetMax = new Vector2(-5f, -5f);
                Image iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = iconSprite;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
            }

            Text label = BattleHudFactory.CreateText(row.transform, entry.Label, 13, FontStyle.Bold, TextAnchor.MiddleLeft, entry.IsPrimaryReward ? BattleUiTheme.TextGold : BattleUiTheme.TextSecondary);
            LayoutElement labelLayout = label.GetComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            labelLayout.preferredHeight = 22f;
            BattleHudFactory.SetOverflow(label, TextOverflowModes.Truncate, false);
        }

        public void ShowDialogue(string speaker, string body)
        {
            dialogueSpeakerLabel.text = speaker ?? string.Empty;
            dialogueBodyLabel.text = body ?? string.Empty;
            dialogueOverlay.SetActive(true);
        }

        public void HideDialogue()
        {
            if (dialogueOverlay != null)
            {
                dialogueOverlay.SetActive(false);
            }
        }

        public void ShowCampaignStageSelect(CampaignStageSelectModel model, Action<int> onStageSelected)
        {
            SetBattleShellVisible(false);
            campaignOverlayView?.ShowCampaignStageSelect(model, onStageSelected);
        }

        public void ShowCampaignInterlude(CampaignInterludeModel model, Action onPrimary, Action onSecondary = null)
        {
            SetBattleShellVisible(false);
            campaignOverlayView?.ShowCampaignInterlude(model, onPrimary, onSecondary);
        }

        public void ShowCampaignOptionList(CampaignOptionListModel model, Action<string> onOptionSelected, Action onPrimary, Action onSecondary = null)
        {
            SetBattleShellVisible(false);
            campaignOverlayView?.ShowCampaignOptionList(model, onOptionSelected, onPrimary, onSecondary);
        }

        public void ShowCampaignEquipment(CampaignEquipmentDeckModel model, Action<string> onOptionSelected, Action onPrimary, Action onSecondary = null)
        {
            SetBattleShellVisible(false);
            campaignOverlayView?.ShowCampaignEquipment(model, onOptionSelected, onPrimary, onSecondary);
        }

        public void HideCampaignOverlay()
        {
            campaignOverlayView?.Hide();
            SetBattleShellVisible(true);
        }

        public void ShowOnboarding(BattleOnboardingModel model)
        {
            if (onboardingPanel == null)
            {
                return;
            }

            BattleOnboardingModel onboardingModel = model ?? new BattleOnboardingModel();
            onboardingProgressLabel.text = onboardingModel.ProgressLabel;
            onboardingTitleLabel.text = onboardingModel.Title;
            onboardingBodyLabel.text = onboardingModel.Body;
            onboardingHintLabel.text = onboardingModel.HintText;
            onboardingHintLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(onboardingModel.HintText));
            onboardingSkipButton.gameObject.SetActive(onboardingModel.CanSkip);
            if (onboardingModel.CanSkip)
            {
                BattleHudFactory.SetButtonLabel(
                    onboardingSkipButton,
                    string.IsNullOrWhiteSpace(onboardingModel.SkipLabel)
                        ? LocalizationService.Text("ui.button.skip", "略過")
                        : onboardingModel.SkipLabel);
            }

            onboardingPanel.SetActive(true);
        }

        public void HideOnboarding()
        {
            if (onboardingPanel != null)
            {
                onboardingPanel.SetActive(false);
            }
        }

        public void ShowConfirmDialog(BattleConfirmDialogModel model)
        {
            if (confirmDialogOverlay == null)
            {
                return;
            }

            BattleConfirmDialogModel dialogModel = model ?? new BattleConfirmDialogModel();
            confirmDialogTitleLabel.text = dialogModel.Title;
            confirmDialogBodyLabel.text = dialogModel.Body;
            BattleHudFactory.SetButtonLabel(
                confirmPrimaryButton,
                string.IsNullOrWhiteSpace(dialogModel.ConfirmLabel)
                    ? LocalizationService.Text("ui.button.confirm", "確認")
                    : dialogModel.ConfirmLabel);
            BattleHudFactory.SetButtonLabel(
                confirmSecondaryButton,
                string.IsNullOrWhiteSpace(dialogModel.CancelLabel)
                    ? LocalizationService.Text("ui.button.cancel", "取消")
                    : dialogModel.CancelLabel);
            confirmDialogOverlay.SetActive(true);
        }

        public void HideConfirmDialog()
        {
            if (confirmDialogOverlay != null)
            {
                confirmDialogOverlay.SetActive(false);
            }
        }

        private void ApplyContextRibbon()
        {
            if (contextRibbonView == null || hudModelBuilder == null)
            {
                return;
            }

            BattleForecastModel model = currentForecast ?? hudModelBuilder.BuildNeutralForecastModel(currentOverview, feedEntries);
            contextRibbonView.Bind(model);
        }

        private bool IsOverlayGateActive(BattleHudOverlayGate gate)
        {
            switch (gate)
            {
                case BattleHudOverlayGate.BlockInteraction:
                    return IsCampaignOverlayVisible ||
                           IsResultVisible ||
                           IsConfirmDialogVisible;
                case BattleHudOverlayGate.PauseAutoMode:
                    return IsCampaignOverlayVisible ||
                           IsDialogueVisible ||
                           IsResultVisible ||
                           IsOnboardingVisible ||
                           IsConfirmDialogVisible;
                default:
                    return IsCampaignOverlayVisible ||
                           IsDialogueVisible ||
                           IsResultVisible ||
                           IsOnboardingVisible ||
                           IsConfirmDialogVisible;
            }
        }

        private void SetBattleShellVisible(bool visible)
        {
            contextRibbonView?.SetVisible(visible);
            selectedUnitView?.SetVisible(visible);
            rosterSidebarView?.SetVisible(visible);
        }

        private void BuildResultPanel(Transform canvasRoot, Action onResultAdvance)
        {
            resultPanel = BattleHudFactory.CreatePanel(
                "ResultPanel",
                canvasRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 456f),
                new Color(0.06f, 0.07f, 0.1f, 0.9f));
            resultAdvanceButton = resultPanel.AddComponent<Button>();
            resultAdvanceButton.transition = Selectable.Transition.ColorTint;
            ColorBlock resultColors = resultAdvanceButton.colors;
            resultColors.normalColor = new Color(1f, 1f, 1f, 0f);
            resultColors.highlightedColor = new Color(1f, 1f, 1f, 0.03f);
            resultColors.pressedColor = new Color(1f, 1f, 1f, 0.06f);
            resultColors.disabledColor = new Color(1f, 1f, 1f, 0f);
            resultAdvanceButton.colors = resultColors;
            resultAdvanceButton.onClick.AddListener(() => onResultAdvance?.Invoke());
            resultTitleLabel = BattleHudFactory.CreateAbsoluteText(resultPanel.transform, new Vector2(20f, 388f), new Vector2(-20f, -20f), string.Empty, 30, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextGold);

            GameObject contentRoot = new GameObject("ResultContentRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentRoot.transform.SetParent(resultPanel.transform, false);
            RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(20f, 52f);
            contentRect.offsetMax = new Vector2(-20f, -74f);
            VerticalLayoutGroup contentLayout = contentRoot.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 8f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            resultSummaryLabel = BattleHudFactory.CreateText(contentRoot.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            resultSummaryLayout = resultSummaryLabel.GetComponent<LayoutElement>();
            resultSummaryLayout.preferredHeight = 86f;

            GameObject rewardEntriesObject = new GameObject("ResultRewardEntries", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            rewardEntriesObject.transform.SetParent(contentRoot.transform, false);
            RectTransform rewardEntriesRect = rewardEntriesObject.GetComponent<RectTransform>();
            rewardEntriesRect.anchorMin = new Vector2(0f, 1f);
            rewardEntriesRect.anchorMax = new Vector2(1f, 1f);
            rewardEntriesRect.pivot = new Vector2(0.5f, 1f);
            rewardEntriesRect.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup rewardEntriesLayout = rewardEntriesObject.GetComponent<VerticalLayoutGroup>();
            rewardEntriesLayout.spacing = 6f;
            rewardEntriesLayout.childControlHeight = true;
            rewardEntriesLayout.childControlWidth = true;
            rewardEntriesLayout.childForceExpandHeight = false;
            rewardEntriesLayout.childForceExpandWidth = true;
            resultRewardEntriesLayout = rewardEntriesObject.GetComponent<LayoutElement>();
            resultRewardEntriesLayout.preferredHeight = 0f;
            resultRewardEntriesRoot = rewardEntriesObject.transform;
            resultRewardLabel = BattleHudFactory.CreateText(contentRoot.transform, string.Empty, 13, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            resultRewardLayout = resultRewardLabel.GetComponent<LayoutElement>();
            resultRewardLayout.preferredHeight = 0f;
            resultSpecialLabel = BattleHudFactory.CreateText(contentRoot.transform, string.Empty, 13, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextGold);
            resultSpecialLayout = resultSpecialLabel.GetComponent<LayoutElement>();
            resultSpecialLayout.preferredHeight = 0f;
            resultSpecialLabel.gameObject.SetActive(false);
            resultUnitsLabel = BattleHudFactory.CreateText(contentRoot.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            resultUnitsLayout = resultUnitsLabel.GetComponent<LayoutElement>();
            resultUnitsLayout.preferredHeight = 0f;
            BattleHudFactory.SetOverflow(resultUnitsLabel, TextOverflowModes.Ellipsis, true);
            resultContinueLabel = BattleHudFactory.CreateAbsoluteText(
                resultPanel.transform,
                new Vector2(20f, 10f),
                new Vector2(-20f, 16f),
                LocalizationService.Text("ui.result.continue", "點擊任意處繼續"),
                12,
                FontStyle.Italic,
                TextAnchor.LowerRight,
                BattleUiTheme.TextMuted);
            resultPanel.SetActive(false);
        }

        private void RefreshResultLayout(bool hasStructuredRewardEntries)
        {
            float contentWidth = GetResultContentWidth();
            SetResultBlockLayout(resultSummaryLabel, resultSummaryLayout, !string.IsNullOrWhiteSpace(resultSummaryLabel.text), contentWidth, 72f);
            resultRewardEntriesRoot.gameObject.SetActive(hasStructuredRewardEntries && resultRewardEntriesRoot.childCount > 0);
            resultRewardEntriesLayout.preferredHeight = resultRewardEntriesRoot.gameObject.activeSelf
                ? (resultRewardEntriesRoot.childCount * 30f) + Mathf.Max(0f, (resultRewardEntriesRoot.childCount - 1) * 6f)
                : 0f;
            SetResultBlockLayout(resultRewardLabel, resultRewardLayout, !hasStructuredRewardEntries && !string.IsNullOrWhiteSpace(resultRewardLabel.text), contentWidth, 0f);
            SetResultBlockLayout(resultSpecialLabel, resultSpecialLayout, !string.IsNullOrWhiteSpace(resultSpecialLabel.text), contentWidth, 0f);
            SetResultBlockLayout(resultUnitsLabel, resultUnitsLayout, !string.IsNullOrWhiteSpace(resultUnitsLabel.text), contentWidth, 0f, 124f);
        }

        private float GetResultContentWidth()
        {
            RectTransform rectTransform = resultPanel != null ? resultPanel.GetComponent<RectTransform>() : null;
            float width = rectTransform != null && rectTransform.rect.width > 0f
                ? rectTransform.rect.width
                : 720f;
            return Mathf.Max(280f, width - 40f);
        }

        private static void SetResultBlockLayout(Text text, LayoutElement layoutElement, bool visible, float width, float minHeight, float maxHeight = 0f)
        {
            if (text == null || layoutElement == null)
            {
                return;
            }

            text.gameObject.SetActive(visible);
            if (!visible)
            {
                layoutElement.preferredHeight = 0f;
                return;
            }

            Vector2 preferred = text.GetPreferredValues(text.text, width, 0f);
            float preferredHeight = Mathf.Max(minHeight, preferred.y + 4f);
            if (maxHeight > 0f)
            {
                preferredHeight = Mathf.Min(preferredHeight, maxHeight);
            }

            layoutElement.preferredHeight = preferredHeight;
        }

        private void BuildDialogueOverlay(Transform canvasRoot, Action onDialogueAdvance)
        {
            dialogueOverlay = BattleHudFactory.CreateStretchPanel("DialogueOverlay", canvasRoot, BattleUiTheme.PanelOverlay);
            dialogueAdvanceButton = dialogueOverlay.AddComponent<Button>();
            dialogueAdvanceButton.transition = Selectable.Transition.ColorTint;
            ColorBlock dialogueColors = dialogueAdvanceButton.colors;
            dialogueColors.normalColor = new Color(1f, 1f, 1f, 0f);
            dialogueColors.highlightedColor = new Color(1f, 1f, 1f, 0.02f);
            dialogueColors.pressedColor = new Color(1f, 1f, 1f, 0.04f);
            dialogueColors.disabledColor = new Color(1f, 1f, 1f, 0f);
            dialogueAdvanceButton.colors = dialogueColors;
            dialogueAdvanceButton.onClick.AddListener(() => onDialogueAdvance?.Invoke());

            GameObject dialogueBox = BattleHudFactory.CreatePanel(
                "DialogueBox",
                dialogueOverlay.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 28f),
                new Vector2(980f, 226f),
                BattleUiTheme.PanelBackdrop);

            dialogueSpeakerLabel = BattleHudFactory.CreateAbsoluteText(dialogueBox.transform, new Vector2(28f, -24f), new Vector2(-28f, -18f), string.Empty, 20, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextGold);
            dialogueBodyLabel = BattleHudFactory.CreateAbsoluteText(dialogueBox.transform, new Vector2(28f, 48f), new Vector2(-28f, -64f), string.Empty, 22, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            dialogueContinueLabel = BattleHudFactory.CreateAbsoluteText(dialogueBox.transform, new Vector2(28f, 16f), new Vector2(-28f, 32f), LocalizationService.Text("ui.dialogue.continue", "點擊任意處繼續"), 14, FontStyle.Italic, TextAnchor.LowerRight, BattleUiTheme.TextMuted);
            dialogueOverlay.SetActive(false);
        }

        private void BuildOnboardingPanel(Transform canvasRoot, Action onOnboardingSkip)
        {
            onboardingPanel = BattleHudFactory.CreatePanel(
                "OnboardingPanel",
                canvasRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(620f, 280f),
                BattleUiTheme.PanelSurface);
            VerticalLayoutGroup layout = onboardingPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(20, 20, 18, 18);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            onboardingProgressLabel = BattleHudFactory.CreateText(onboardingPanel.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            onboardingProgressLabel.GetComponent<LayoutElement>().preferredHeight = 20f;

            onboardingTitleLabel = BattleHudFactory.CreateText(onboardingPanel.transform, string.Empty, 22, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            onboardingTitleLabel.GetComponent<LayoutElement>().preferredHeight = 28f;

            onboardingBodyLabel = BattleHudFactory.CreateText(onboardingPanel.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            onboardingBodyLabel.GetComponent<LayoutElement>().preferredHeight = 120f;

            onboardingHintLabel = BattleHudFactory.CreateText(onboardingPanel.transform, string.Empty, 13, FontStyle.Italic, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            onboardingHintLabel.GetComponent<LayoutElement>().preferredHeight = 24f;

            onboardingSkipButton = BattleHudFactory.CreateButton(onboardingPanel.transform, LocalizationService.Text("ui.button.skip", "略過"), false);
            onboardingSkipButton.onClick.AddListener(() => onOnboardingSkip?.Invoke());
            onboardingPanel.SetActive(false);
        }

        private void BuildConfirmDialog(Transform canvasRoot, Action onConfirmPrimary, Action onConfirmSecondary)
        {
            confirmDialogOverlay = BattleHudFactory.CreateStretchPanel("ConfirmDialogOverlay", canvasRoot, BattleUiTheme.PanelOverlay);
            GameObject dialogPanel = BattleHudFactory.CreatePanel(
                "ConfirmDialogPanel",
                confirmDialogOverlay.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(560f, 272f),
                BattleUiTheme.PanelSurface);

            VerticalLayoutGroup layout = dialogPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(20, 20, 18, 18);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            confirmDialogTitleLabel = BattleHudFactory.CreateText(dialogPanel.transform, string.Empty, 24, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            confirmDialogTitleLabel.GetComponent<LayoutElement>().preferredHeight = 28f;

            confirmDialogBodyLabel = BattleHudFactory.CreateText(dialogPanel.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            confirmDialogBodyLabel.GetComponent<LayoutElement>().preferredHeight = 132f;

            GameObject buttonRow = new GameObject("ConfirmDialogButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            buttonRow.transform.SetParent(dialogPanel.transform, false);
            buttonRow.GetComponent<LayoutElement>().preferredHeight = 42f;
            HorizontalLayoutGroup buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 12f;
            buttonLayout.childAlignment = TextAnchor.MiddleRight;
            buttonLayout.childControlHeight = true;
            buttonLayout.childControlWidth = true;
            buttonLayout.childForceExpandHeight = true;
            buttonLayout.childForceExpandWidth = true;

            confirmSecondaryButton = BattleHudFactory.CreateButton(buttonRow.transform, LocalizationService.Text("ui.button.cancel", "取消"), false);
            confirmSecondaryButton.onClick.AddListener(() => onConfirmSecondary?.Invoke());
            confirmPrimaryButton = BattleHudFactory.CreateButton(buttonRow.transform, LocalizationService.Text("ui.button.confirm", "確認"), true);
            confirmPrimaryButton.onClick.AddListener(() => onConfirmPrimary?.Invoke());
            confirmDialogOverlay.SetActive(false);
        }
    }

    #if false
    internal sealed class BattleSelectedUnitView
    {
        private GameObject rootObject;
        private Image portraitImage;
        private Image portraitBacking;
        private Image hpFill;
        private Image manaFill;
        private Text nameLabel;
        private Text roleLabel;
        private Text positionLabel;
        private Text hpLabel;
        private Text manaLabel;
        private Transform identityFactsRoot;
        private Transform primaryFactsRoot;
        private Transform chipRoot;
        private Transform threatChipRoot;
        private Text threatLineLabel;
        private Text equipmentSummaryLabel;
        private Text detailHeaderLabel;
        private Text detailToggleLabel;
        private Transform detailLinesRoot;
        private GameObject detailContent;
        private Button detailToggleButton;
        private bool detailsExpanded;
        private string lastBoundUnitId = string.Empty;

        public Text NameLabel => nameLabel;

        public void Initialize(Transform canvasRoot)
        {
            rootObject = BattleHudFactory.CreatePanel(
                "SelectedUnitPanel",
                canvasRoot,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(18f, 0f),
                new Vector2(292f, 836f),
                BattleUiTheme.PanelSurface);
            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.pivot = new Vector2(0f, 0.5f);

            VerticalLayoutGroup layout = rootObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            BattleHudFactory.CreateSectionHeader(rootObject.transform, LocalizationService.Text("ui.panel.selected", "角色戰報"));

            GameObject identityPanel = BattleHudFactory.CreateInsetPanel("IdentityPanel", rootObject.transform, 176f, BattleUiTheme.PanelSelected);
            HorizontalLayoutGroup identityLayout = identityPanel.AddComponent<HorizontalLayoutGroup>();
            identityLayout.spacing = 12f;
            identityLayout.padding = new RectOffset(14, 14, 14, 14);
            identityLayout.childAlignment = TextAnchor.MiddleLeft;
            identityLayout.childControlHeight = true;
            identityLayout.childControlWidth = false;
            identityLayout.childForceExpandHeight = false;
            identityLayout.childForceExpandWidth = false;

            GameObject portraitFrame = BattleHudFactory.CreatePanel(
                "PortraitFrame",
                identityPanel.transform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                Vector2.zero,
                new Vector2(104f, 104f),
                new Color(0.16f, 0.16f, 0.15f, 1f));
            portraitBacking = portraitFrame.GetComponent<Image>();
            LayoutElement portraitLayout = portraitFrame.AddComponent<LayoutElement>();
            portraitLayout.preferredWidth = 104f;
            portraitLayout.preferredHeight = 104f;

            GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitObject.transform.SetParent(portraitFrame.transform, false);
            RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.06f, 0.06f);
            portraitRect.anchorMax = new Vector2(0.94f, 0.94f);
            portraitRect.offsetMin = Vector2.zero;
            portraitRect.offsetMax = Vector2.zero;
            portraitImage = portraitObject.GetComponent<Image>();
            portraitImage.preserveAspect = true;

            GameObject textRoot = new GameObject("IdentityTextRoot", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textRoot.transform.SetParent(identityPanel.transform, false);
            textRoot.GetComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup textLayout = textRoot.GetComponent<VerticalLayoutGroup>();
            textLayout.spacing = 4f;
            textLayout.childControlHeight = true;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandHeight = false;

            nameLabel = BattleHudFactory.CreateText(textRoot.transform, string.Empty, 22, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            nameLabel.GetComponent<LayoutElement>().preferredHeight = 26f;
            BattleHudFactory.EnableBestFit(nameLabel, 16, 22, true);
            roleLabel = BattleHudFactory.CreateText(textRoot.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            roleLabel.GetComponent<LayoutElement>().preferredHeight = 20f;
            positionLabel = BattleHudFactory.CreateText(textRoot.transform, string.Empty, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextMuted);
            positionLabel.GetComponent<LayoutElement>().preferredHeight = 38f;

            GameObject identityFacts = new GameObject("IdentityFacts", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            identityFacts.transform.SetParent(textRoot.transform, false);
            identityFacts.GetComponent<LayoutElement>().preferredHeight = 24f;
            HorizontalLayoutGroup identityFactsLayout = identityFacts.GetComponent<HorizontalLayoutGroup>();
            identityFactsLayout.spacing = 6f;
            identityFactsLayout.childControlHeight = true;
            identityFactsLayout.childControlWidth = false;
            identityFactsLayout.childForceExpandHeight = false;
            identityFactsLayout.childForceExpandWidth = false;
            identityFactsRoot = identityFacts.transform;

            GameObject vitalPanel = BattleHudFactory.CreateInsetPanel("VitalsPanel", rootObject.transform, 116f, new Color(0.16f, 0.13f, 0.1f, 0.96f));
            Transform vitalRoot = BattleHudFactory.CreateInsetContentRoot(vitalPanel.transform, 12f);
            VerticalLayoutGroup vitalLayout = vitalRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            vitalLayout.spacing = 6f;
            vitalLayout.childControlHeight = true;
            vitalLayout.childControlWidth = true;
            vitalLayout.childForceExpandHeight = false;

            hpLabel = BattleHudFactory.CreateText(vitalRoot, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            hpLabel.GetComponent<LayoutElement>().preferredHeight = 20f;
            GameObject hpBarRoot = new GameObject("HpBarRoot", typeof(RectTransform), typeof(LayoutElement));
            hpBarRoot.transform.SetParent(vitalRoot, false);
            hpBarRoot.GetComponent<LayoutElement>().preferredHeight = 14f;
            BattleHudFactory.CreateStretchUiBar(hpBarRoot.transform, out hpFill);

            manaLabel = BattleHudFactory.CreateText(vitalRoot, string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            manaLabel.GetComponent<LayoutElement>().preferredHeight = 18f;
            GameObject manaBarRoot = new GameObject("ManaBarRoot", typeof(RectTransform), typeof(LayoutElement));
            manaBarRoot.transform.SetParent(vitalRoot, false);
            manaBarRoot.GetComponent<LayoutElement>().preferredHeight = 12f;
            BattleHudFactory.CreateStretchUiBar(manaBarRoot.transform, out manaFill);

            GameObject primaryFactsPanel = BattleHudFactory.CreateInsetPanel("PrimaryFactsPanel", rootObject.transform, 138f, new Color(0.15f, 0.13f, 0.11f, 0.96f));
            Transform factsContent = BattleHudFactory.CreateInsetContentRoot(primaryFactsPanel.transform, 12f);
            VerticalLayoutGroup factLayout = factsContent.gameObject.AddComponent<VerticalLayoutGroup>();
            factLayout.spacing = 8f;
            factLayout.childControlHeight = true;
            factLayout.childControlWidth = true;
            factLayout.childForceExpandHeight = false;

            Text primaryHeader = BattleHudFactory.CreateText(factsContent, LocalizationService.Text("ui.selected.primary_header", "首屏決策"), 13, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            primaryHeader.GetComponent<LayoutElement>().preferredHeight = 18f;

            GameObject factsGrid = new GameObject("FactsGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            factsGrid.transform.SetParent(factsContent, false);
            factsGrid.GetComponent<LayoutElement>().preferredHeight = 58f;
            GridLayoutGroup gridLayout = factsGrid.GetComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(118f, 24f);
            gridLayout.spacing = new Vector2(8f, 8f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            primaryFactsRoot = factsGrid.transform;

            GameObject chipRow = new GameObject("PrimaryChipRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            chipRow.transform.SetParent(factsContent, false);
            chipRow.GetComponent<LayoutElement>().preferredHeight = 24f;
            HorizontalLayoutGroup chipLayout = chipRow.GetComponent<HorizontalLayoutGroup>();
            chipLayout.spacing = 6f;
            chipLayout.childControlHeight = true;
            chipLayout.childControlWidth = false;
            chipLayout.childForceExpandHeight = false;
            chipLayout.childForceExpandWidth = false;
            chipRoot = chipRow.transform;

            GameObject threatPanel = BattleHudFactory.CreateInsetPanel("ThreatPanel", rootObject.transform, 136f, BattleUiTheme.PanelCommand);
            Transform threatRoot = BattleHudFactory.CreateInsetContentRoot(threatPanel.transform, 12f);
            VerticalLayoutGroup threatLayout = threatRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            threatLayout.spacing = 6f;
            threatLayout.childControlHeight = true;
            threatLayout.childControlWidth = true;
            threatLayout.childForceExpandHeight = false;

            GameObject threatChipRow = new GameObject("ThreatChipRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            threatChipRow.transform.SetParent(threatRoot, false);
            threatChipRow.GetComponent<LayoutElement>().preferredHeight = 24f;
            HorizontalLayoutGroup threatChipLayout = threatChipRow.GetComponent<HorizontalLayoutGroup>();
            threatChipLayout.spacing = 6f;
            threatChipLayout.childControlHeight = true;
            threatChipLayout.childControlWidth = false;
            threatChipLayout.childForceExpandHeight = false;
            threatChipLayout.childForceExpandWidth = false;
            threatChipRoot = threatChipRow.transform;

            threatLineLabel = BattleHudFactory.CreateText(threatRoot, string.Empty, 13, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextThreat);
            threatLineLabel.GetComponent<LayoutElement>().preferredHeight = 42f;

            equipmentSummaryLabel = BattleHudFactory.CreateText(threatRoot, string.Empty, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            equipmentSummaryLabel.GetComponent<LayoutElement>().preferredHeight = 42f;

            GameObject detailPanel = BattleHudFactory.CreateInsetPanel("DetailPanel", rootObject.transform, 174f, BattleUiTheme.PanelInsetStrong);
            Transform detailRoot = BattleHudFactory.CreateInsetContentRoot(detailPanel.transform, 12f);
            VerticalLayoutGroup detailLayout = detailRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            detailLayout.spacing = 8f;
            detailLayout.childControlHeight = true;
            detailLayout.childControlWidth = true;
            detailLayout.childForceExpandHeight = false;

            GameObject detailHeaderRow = new GameObject("DetailHeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            detailHeaderRow.transform.SetParent(detailRoot, false);
            detailHeaderRow.GetComponent<LayoutElement>().preferredHeight = 24f;
            HorizontalLayoutGroup detailHeaderLayout = detailHeaderRow.GetComponent<HorizontalLayoutGroup>();
            detailHeaderLayout.spacing = 8f;
            detailHeaderLayout.childControlHeight = true;
            detailHeaderLayout.childControlWidth = true;
            detailHeaderLayout.childForceExpandHeight = false;
            detailHeaderLayout.childForceExpandWidth = false;

            detailHeaderLabel = BattleHudFactory.CreateText(detailHeaderRow.transform, string.Empty, 13, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            detailHeaderLabel.GetComponent<LayoutElement>().flexibleWidth = 1f;
            detailHeaderLabel.GetComponent<LayoutElement>().preferredHeight = 20f;
            detailToggleButton = BattleHudFactory.CreateButton(detailHeaderRow.transform, LocalizationService.Text("ui.selected.details_expand", "展開"), false);
            detailToggleButton.GetComponent<LayoutElement>().preferredWidth = 92f;
            detailToggleButton.GetComponent<LayoutElement>().preferredHeight = 28f;
            detailToggleButton.onClick.AddListener(ToggleDetails);
            detailToggleLabel = detailToggleButton.GetComponentInChildren<Text>();

            detailContent = new GameObject("DetailContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            detailContent.transform.SetParent(detailRoot, false);
            detailContent.GetComponent<LayoutElement>().preferredHeight = 126f;
            VerticalLayoutGroup detailContentLayout = detailContent.GetComponent<VerticalLayoutGroup>();
            detailContentLayout.spacing = 6f;
            detailContentLayout.childControlHeight = true;
            detailContentLayout.childControlWidth = true;
            detailContentLayout.childForceExpandHeight = false;
            detailLinesRoot = detailContent.transform;

            SetDetailsExpanded(false);
            Bind(new BattleSelectedUnitModel());
        }

        public void SetVisible(bool visible)
        {
            if (rootObject != null)
            {
                rootObject.SetActive(visible);
            }
        }

        public void Bind(BattleSelectedUnitModel model)
        {
            BattleSelectedUnitModel selected = model ?? new BattleSelectedUnitModel();
            if (!selected.HasSelection)
            {
                lastBoundUnitId = string.Empty;
                SetDetailsExpanded(false);
                portraitImage.sprite = null;
                portraitImage.enabled = false;
                portraitBacking.color = new Color(0.18f, 0.17f, 0.16f, 1f);
                hpFill.fillAmount = 0f;
                manaFill.fillAmount = 0f;
                nameLabel.text = LocalizationService.Text("ui.selected.card_none_title", "未選擇單位");
                roleLabel.text = string.Empty;
                positionLabel.text = LocalizationService.Text("ui.selected.card_none_body", "請點選我方單位查看戰場檔案。");
                hpLabel.text = string.Empty;
                manaLabel.text = string.Empty;
                threatLineLabel.text = string.Empty;
                equipmentSummaryLabel.text = string.Empty;
                detailHeaderLabel.text = LocalizationService.Text("ui.selected.details_header", "武裝與技能");
                BattleHudFactory.DestroyChildren(identityFactsRoot);
                BattleHudFactory.DestroyChildren(primaryFactsRoot);
                BattleHudFactory.DestroyChildren(chipRoot);
                BattleHudFactory.DestroyChildren(threatChipRoot);
                BattleHudFactory.DestroyChildren(detailLinesRoot);
                return;
            }

            if (!string.Equals(lastBoundUnitId, selected.UnitId, StringComparison.Ordinal))
            {
                lastBoundUnitId = selected.UnitId;
                SetDetailsExpanded(false);
            }

            UnitVisualProfile visualProfile = UnitVisualCatalog.GetProfile(selected.UnitId, selected.Faction, selected.Role);
            portraitImage.enabled = true;
            portraitImage.sprite = RuntimeSpriteLibrary.GetPortraitSprite(visualProfile);
            portraitBacking.color = visualProfile.PortraitBackdropColor;
            nameLabel.text = selected.DisplayName;
            roleLabel.text = selected.RoleLabel;
            positionLabel.text = string.Join(
                "\n",
                new[] { selected.PositionLabel, selected.TerrainName }
                    .Where(line => !string.IsNullOrWhiteSpace(line)));
            hpLabel.text = LocalizationService.Format("ui.label.hp_value", "HP {0}/{1}", selected.CurrentHp, selected.MaxHp);
            hpFill.fillAmount = selected.MaxHp <= 0 ? 0f : (float)selected.CurrentHp / selected.MaxHp;
            hpFill.color = hpFill.fillAmount > 0.55f
                ? new Color(0.39f, 0.81f, 0.42f, 1f)
                : hpFill.fillAmount > 0.3f
                    ? new Color(0.91f, 0.74f, 0.22f, 1f)
                    : new Color(0.88f, 0.35f, 0.28f, 1f);
            manaLabel.text = LocalizationService.Format("ui.label.mana_value", "士氣 {0}/{1}", selected.CurrentMana, selected.MaxMana);
            manaFill.fillAmount = selected.MaxMana <= 0 ? 0f : (float)selected.CurrentMana / selected.MaxMana;
            manaFill.color = manaFill.fillAmount > 0.55f
                ? new Color(0.38f, 0.78f, 0.95f, 1f)
                : manaFill.fillAmount > 0.3f
                    ? new Color(0.46f, 0.66f, 0.98f, 1f)
                    : new Color(0.52f, 0.42f, 0.85f, 1f);
            threatLineLabel.text = selected.ThreatLine;
            equipmentSummaryLabel.text = selected.EquipmentSummary;
            detailHeaderLabel.text = string.IsNullOrWhiteSpace(selected.DetailHeader)
                ? LocalizationService.Text("ui.selected.details_header", "武裝與技能")
                : selected.DetailHeader;

            RebuildFacts(identityFactsRoot, selected.IdentityFacts, 106f, 22f);
            RebuildFacts(primaryFactsRoot, selected.PrimaryFacts.Count > 0 ? selected.PrimaryFacts : selected.CombatFacts, 116f, 24f);
            RebuildChips(chipRoot, selected.PrimaryChips.Count > 0 ? selected.PrimaryChips : selected.StatusPills, 22f);
            BattleHudFactory.DestroyChildren(threatChipRoot);
            if (selected.ThreatChip != null && !string.IsNullOrWhiteSpace(selected.ThreatChip.Text))
            {
                BattleHudFactory.CreateAdaptiveChip(threatChipRoot, selected.ThreatChip, 24f);
            }

            BattleHudFactory.DestroyChildren(detailLinesRoot);
            foreach (string line in (selected.DetailLines ?? Array.Empty<string>()).Where(line => !string.IsNullOrWhiteSpace(line)))
            {
                Text detailLabel = BattleHudFactory.CreateText(detailLinesRoot, line, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
                detailLabel.GetComponent<LayoutElement>().preferredHeight = 24f;
            }
        }

        private void RebuildFacts(Transform root, IReadOnlyList<HudFactModel> facts, float width, float height)
        {
            BattleHudFactory.DestroyChildren(root);
            if (facts == null)
            {
                return;
            }

            foreach (HudFactModel fact in facts.Where(fact => fact != null && !string.IsNullOrWhiteSpace(fact.Value)).Take(4))
            {
                BattleHudFactory.CreateFactCard(root, fact, width, height);
            }
        }

        private void RebuildChips(Transform root, IReadOnlyList<HudChipModel> chips, float height)
        {
            BattleHudFactory.DestroyChildren(root);
            if (chips == null)
            {
                return;
            }

            foreach (HudChipModel chip in chips.Where(chip => chip != null && !string.IsNullOrWhiteSpace(chip.Text)).Take(4))
            {
                BattleHudFactory.CreateAdaptiveChip(root, chip, height);
            }
        }

        private void ToggleDetails()
        {
            SetDetailsExpanded(!detailsExpanded);
        }

        private void SetDetailsExpanded(bool expanded)
        {
            detailsExpanded = expanded;
            if (detailContent != null)
            {
                detailContent.SetActive(expanded);
            }

            if (detailToggleLabel != null)
            {
                detailToggleLabel.text = expanded
                    ? LocalizationService.Text("ui.selected.details_collapse", "收合")
                    : LocalizationService.Text("ui.selected.details_expand", "展開");
            }
        }
    }

    internal sealed class BattleRosterSidebarView
    {
        private readonly List<RosterEntryView> alliedRosterViews = new List<RosterEntryView>();
        private readonly List<RosterEntryView> enemyRosterViews = new List<RosterEntryView>();
        private readonly List<Text> feedLabels = new List<Text>();

        private GameObject rootObject;
        private Action<string> rosterSelectionHandler;
        private Text stageLabel;
        private Text seedLabel;
        private Text phaseLabel;
        private Text turnLabel;
        private Text playerAliveLabel;
        private Text enemyAliveLabel;
        private Text readyLabel;
        private Text skillReadyLabel;
        private Text objectivePrimaryLabel;
        private Text objectiveFailureLabel;
        private Text secondaryObjectivesLabel;
        private Text instructionLabel;
        private Text secondaryInstructionLabel;
        private Button endTurnButton;
        private Button rerollButton;
        private GameObject rerollButtonObject;
        private Transform alliedRosterRoot;
        private Transform enemyRosterRoot;
        private TabButtonView alliedTabView;
        private TabButtonView enemyTabView;
        private TabButtonView feedTabView;
        private GameObject alliedTabContent;
        private GameObject enemyTabContent;
        private GameObject feedTabContent;
        private string activeOverviewTab = "allies";
        private int feedLimit;

        public bool IsRerollVisible => rerollButtonObject != null && rerollButtonObject.activeSelf;

        public string CurrentObjectiveText => objectivePrimaryLabel != null ? objectivePrimaryLabel.text : string.Empty;

        public void Initialize(Transform canvasRoot, Action onEndTurn, Action onReroll, int feedLimit)
        {
            this.feedLimit = Mathf.Max(1, feedLimit);

            rootObject = BattleHudFactory.CreatePanel(
                "OverviewPanel",
                canvasRoot,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-18f, 0f),
                new Vector2(318f, 836f),
                BattleUiTheme.PanelSurface);
            RectTransform rightRect = rootObject.GetComponent<RectTransform>();
            rightRect.pivot = new Vector2(1f, 0.5f);

            VerticalLayoutGroup layout = rootObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            BattleHudFactory.CreateSectionHeader(rootObject.transform, LocalizationService.Text("ui.panel.overview", "戰況總覽"));

            GameObject summaryPanel = BattleHudFactory.CreateInsetPanel("OverviewSummaryPanel", rootObject.transform, 142f, BattleUiTheme.PanelInsetStrong);
            Transform summaryRoot = BattleHudFactory.CreateInsetContentRoot(summaryPanel.transform, 10f);
            VerticalLayoutGroup summaryLayout = summaryRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            summaryLayout.spacing = 4f;
            summaryLayout.childControlHeight = true;
            summaryLayout.childControlWidth = true;
            summaryLayout.childForceExpandHeight = false;

            stageLabel = BattleHudFactory.CreateText(summaryRoot, string.Empty, 20, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            stageLabel.GetComponent<LayoutElement>().preferredHeight = 24f;
            stageLabel.verticalOverflow = VerticalWrapMode.Truncate;
            seedLabel = BattleHudFactory.CreateText(summaryRoot, string.Empty, 11, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            seedLabel.GetComponent<LayoutElement>().preferredHeight = 16f;
            seedLabel.verticalOverflow = VerticalWrapMode.Truncate;
            phaseLabel = BattleHudFactory.CreateText(summaryRoot, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            phaseLabel.GetComponent<LayoutElement>().preferredHeight = 20f;
            phaseLabel.verticalOverflow = VerticalWrapMode.Truncate;
            turnLabel = BattleHudFactory.CreateText(summaryRoot, string.Empty, 13, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextSecondary);
            turnLabel.GetComponent<LayoutElement>().preferredHeight = 18f;
            turnLabel.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject factGrid = new GameObject("FactGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            factGrid.transform.SetParent(summaryRoot, false);
            factGrid.GetComponent<LayoutElement>().preferredHeight = 52f;
            GridLayoutGroup factLayout = factGrid.GetComponent<GridLayoutGroup>();
            factLayout.cellSize = new Vector2(128f, 22f);
            factLayout.spacing = new Vector2(6f, 6f);
            factLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            factLayout.constraintCount = 2;
            playerAliveLabel = BattleHudFactory.CreateText(factGrid.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.62f, 0.8f, 1f, 1f));
            enemyAliveLabel = BattleHudFactory.CreateText(factGrid.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.66f, 0.58f, 1f));
            readyLabel = BattleHudFactory.CreateText(factGrid.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.93f, 0.95f, 0.87f, 1f));
            skillReadyLabel = BattleHudFactory.CreateText(factGrid.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            playerAliveLabel.verticalOverflow = VerticalWrapMode.Truncate;
            enemyAliveLabel.verticalOverflow = VerticalWrapMode.Truncate;
            readyLabel.verticalOverflow = VerticalWrapMode.Truncate;
            skillReadyLabel.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject objectivePanel = BattleHudFactory.CreateInsetPanel("ObjectivePanel", rootObject.transform, 148f, BattleUiTheme.PanelCommand);
            Transform objectiveRoot = BattleHudFactory.CreateInsetContentRoot(objectivePanel.transform, 12f);
            VerticalLayoutGroup objectiveLayout = objectiveRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            objectiveLayout.spacing = 5f;
            objectiveLayout.childControlHeight = true;
            objectiveLayout.childControlWidth = true;
            objectiveLayout.childForceExpandHeight = false;
            objectivePrimaryLabel = BattleHudFactory.CreateText(objectiveRoot, string.Empty, 14, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            objectivePrimaryLabel.GetComponent<LayoutElement>().preferredHeight = 20f;
            objectivePrimaryLabel.verticalOverflow = VerticalWrapMode.Truncate;
            objectiveFailureLabel = BattleHudFactory.CreateText(objectiveRoot, string.Empty, 12, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextWarning);
            objectiveFailureLabel.GetComponent<LayoutElement>().preferredHeight = 18f;
            objectiveFailureLabel.verticalOverflow = VerticalWrapMode.Truncate;
            secondaryObjectivesLabel = BattleHudFactory.CreateText(objectiveRoot, string.Empty, 11, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextGold);
            secondaryObjectivesLabel.GetComponent<LayoutElement>().preferredHeight = 34f;
            secondaryObjectivesLabel.verticalOverflow = VerticalWrapMode.Truncate;
            instructionLabel = BattleHudFactory.CreateText(objectiveRoot, string.Empty, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            instructionLabel.GetComponent<LayoutElement>().preferredHeight = 20f;
            instructionLabel.verticalOverflow = VerticalWrapMode.Truncate;
            secondaryInstructionLabel = BattleHudFactory.CreateText(objectiveRoot, string.Empty, 12, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.78f, 0.88f, 0.98f, 1f));
            secondaryInstructionLabel.GetComponent<LayoutElement>().preferredHeight = 16f;
            secondaryInstructionLabel.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject commandRow = new GameObject("CommandRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            commandRow.transform.SetParent(rootObject.transform, false);
            commandRow.GetComponent<LayoutElement>().preferredHeight = 46f;
            HorizontalLayoutGroup commandLayout = commandRow.GetComponent<HorizontalLayoutGroup>();
            commandLayout.spacing = 10f;
            commandLayout.childAlignment = TextAnchor.MiddleRight;
            commandLayout.childControlHeight = true;
            commandLayout.childControlWidth = true;
            commandLayout.childForceExpandHeight = true;
            commandLayout.childForceExpandWidth = false;
            endTurnButton = BattleHudFactory.CreateButton(commandRow.transform, LocalizationService.Text("ui.button.end_turn", "結束回合"), true);
            endTurnButton.onClick.AddListener(() => onEndTurn?.Invoke());
            LayoutElement endTurnLayout = endTurnButton.GetComponent<LayoutElement>();
            endTurnLayout.preferredWidth = 138f;
            endTurnLayout.flexibleWidth = 0f;
            rerollButton = BattleHudFactory.CreateButton(commandRow.transform, LocalizationService.Text("ui.button.reroll", "重擲"), false);
            rerollButtonObject = rerollButton.gameObject;
            rerollButton.onClick.AddListener(() => onReroll?.Invoke());
            LayoutElement rerollLayout = rerollButton.GetComponent<LayoutElement>();
            rerollLayout.preferredWidth = 112f;
            rerollLayout.flexibleWidth = 0f;

            GameObject contentPanel = BattleHudFactory.CreateInsetPanel("OverviewContentPanel", rootObject.transform, 0f, new Color(0.11f, 0.12f, 0.14f, 0.92f));
            LayoutElement contentPanelLayout = contentPanel.GetComponent<LayoutElement>();
            contentPanelLayout.flexibleHeight = 1f;
            Transform contentRoot = BattleHudFactory.CreateInsetContentRoot(contentPanel.transform, 10f);
            VerticalLayoutGroup contentLayout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;

            GameObject tabRow = new GameObject("TabRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            tabRow.transform.SetParent(contentRoot, false);
            tabRow.GetComponent<LayoutElement>().preferredHeight = 28f;
            HorizontalLayoutGroup tabLayout = tabRow.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 8f;
            tabLayout.childControlHeight = true;
            tabLayout.childControlWidth = true;
            tabLayout.childForceExpandHeight = false;
            tabLayout.childForceExpandWidth = true;
            alliedTabView = BattleHudFactory.CreateTabButton(tabRow.transform, LocalizationService.Text("ui.panel.allies.count", "友軍"));
            enemyTabView = BattleHudFactory.CreateTabButton(tabRow.transform, LocalizationService.Text("ui.panel.enemies.count", "敵軍"));
            feedTabView = BattleHudFactory.CreateTabButton(tabRow.transform, LocalizationService.Text("ui.panel.feed", "戰報"));
            alliedTabView.Button.onClick.AddListener(() => SetOverviewTab("allies"));
            enemyTabView.Button.onClick.AddListener(() => SetOverviewTab("enemies"));
            feedTabView.Button.onClick.AddListener(() => SetOverviewTab("feed"));

            alliedTabContent = BuildTabContent(contentRoot, "AlliedRosterPanel", out alliedRosterRoot);
            enemyTabContent = BuildTabContent(contentRoot, "EnemyRosterPanel", out enemyRosterRoot);
            feedTabContent = BuildTabContent(contentRoot, "FeedPanel", out Transform feedRoot);
            for (int index = 0; index < this.feedLimit; index++)
            {
                Text feedLabel = BattleHudFactory.CreateText(feedRoot, index == 0 ? LocalizationService.Text("ui.feed.empty", "目前還沒有新的戰場紀錄。") : string.Empty, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
                feedLabel.GetComponent<LayoutElement>().preferredHeight = 28f;
                feedLabel.verticalOverflow = VerticalWrapMode.Truncate;
                feedLabels.Add(feedLabel);
            }

            SetOverviewTab("allies");
        }

        public void SetVisible(bool visible)
        {
            if (rootObject != null)
            {
                rootObject.SetActive(visible);
            }
        }

        public void BindOverview(BattleOverviewModel model)
        {
            BattleOverviewModel overview = model ?? new BattleOverviewModel();
            stageLabel.text = overview.StageLabel;
            seedLabel.text = overview.SeedLabel;
            phaseLabel.text = overview.PhaseLabel;
            turnLabel.text = overview.TurnLabel;
            playerAliveLabel.text = overview.PlayerAliveLabel;
            enemyAliveLabel.text = overview.EnemyAliveLabel;
            readyLabel.text = overview.ReadyLabel;
            skillReadyLabel.text = overview.SkillReadyLabel;
            objectivePrimaryLabel.text = overview.ObjectivePrimary;
            objectiveFailureLabel.text = overview.ObjectiveFailure;
            secondaryObjectivesLabel.text = overview.SecondaryObjectiveLines != null && overview.SecondaryObjectiveLines.Count > 0
                ? string.Join("\n", overview.SecondaryObjectiveLines.Take(2))
                : string.Empty;
            secondaryObjectivesLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(secondaryObjectivesLabel.text));
            instructionLabel.text = overview.InstructionText;
            secondaryInstructionLabel.text = overview.SecondaryInstructionText;
            secondaryInstructionLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(overview.SecondaryInstructionText));
        }

        public void BindRoster(IReadOnlyList<BattleRosterEntryModel> alliedRoster, IReadOnlyList<BattleRosterEntryModel> enemyRoster, Action<string> onRosterSelected)
        {
            rosterSelectionHandler = onRosterSelected;
            BindRosterGroup(alliedRosterRoot, alliedRosterViews, alliedRoster);
            BindRosterGroup(enemyRosterRoot, enemyRosterViews, enemyRoster);
            UpdateOverviewTabLabels(alliedRoster != null ? alliedRoster.Count : 0, enemyRoster != null ? enemyRoster.Count : 0);
        }

        public void BindFeed(IReadOnlyList<string> entries)
        {
            for (int index = 0; index < feedLabels.Count; index++)
            {
                feedLabels[index].text = entries != null && index < entries.Count
                    ? entries[index]
                    : index == 0
                        ? LocalizationService.Text("ui.feed.empty", "目前還沒有新的戰場紀錄。")
                        : string.Empty;
            }
        }

        public void SetEndTurnEnabled(bool enabled)
        {
            if (endTurnButton != null)
            {
                endTurnButton.interactable = enabled;
            }
        }

        public void SetRerollEnabled(bool enabled)
        {
            if (rerollButtonObject != null)
            {
                rerollButtonObject.SetActive(enabled);
            }

            if (rerollButton != null)
            {
                rerollButton.interactable = enabled;
            }
        }

        private static GameObject BuildTabContent(Transform parent, string name, out Transform contentRoot)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.flexibleHeight = 1f;
            Image rootImage = root.GetComponent<Image>();
            rootImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
            rootImage.color = new Color(1f, 1f, 1f, 0.02f);

            ScrollRect scrollRect = root.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 24f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(root.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("ContentRoot", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            VerticalLayoutGroup verticalLayout = content.GetComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 6f;
            verticalLayout.childControlHeight = true;
            verticalLayout.childControlWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childForceExpandWidth = true;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            contentRoot = content.transform;
            return root;
        }

        private void SetOverviewTab(string tabId)
        {
            activeOverviewTab = string.IsNullOrWhiteSpace(tabId) ? "allies" : tabId;
            alliedTabContent.SetActive(string.Equals(activeOverviewTab, "allies", StringComparison.Ordinal));
            enemyTabContent.SetActive(string.Equals(activeOverviewTab, "enemies", StringComparison.Ordinal));
            feedTabContent.SetActive(string.Equals(activeOverviewTab, "feed", StringComparison.Ordinal));
            RefreshTabView(alliedTabView, string.Equals(activeOverviewTab, "allies", StringComparison.Ordinal));
            RefreshTabView(enemyTabView, string.Equals(activeOverviewTab, "enemies", StringComparison.Ordinal));
            RefreshTabView(feedTabView, string.Equals(activeOverviewTab, "feed", StringComparison.Ordinal));
        }

        private void UpdateOverviewTabLabels(int alliedCount, int enemyCount)
        {
            alliedTabView.Label.text = LocalizationService.Format("ui.panel.allies.count", "友軍 {0}", alliedCount);
            enemyTabView.Label.text = LocalizationService.Format("ui.panel.enemies.count", "敵軍 {0}", enemyCount);
            feedTabView.Label.text = LocalizationService.Text("ui.panel.feed", "戰報");
        }

        private static void RefreshTabView(TabButtonView view, bool isActive)
        {
            if (view == null)
            {
                return;
            }

            view.Background.color = isActive ? BattleUiTheme.TabActive : BattleUiTheme.TabIdle;
            view.Label.color = isActive ? BattleUiTheme.TabActiveText : BattleUiTheme.TabIdleText;
            view.Outline.effectColor = isActive ? BattleUiTheme.Divider : BattleUiTheme.DividerSoft;
        }

        private void BindRosterGroup(Transform root, List<RosterEntryView> views, IReadOnlyList<BattleRosterEntryModel> models)
        {
            int requiredCount = models != null ? models.Count : 0;
            while (views.Count < requiredCount)
            {
                views.Add(CreateRosterEntryView(root));
            }

            for (int index = 0; index < views.Count; index++)
            {
                bool visible = index < requiredCount;
                views[index].Root.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                BindRosterEntry(views[index], models[index]);
            }
        }

        private void BindRosterEntry(RosterEntryView view, BattleRosterEntryModel model)
        {
            view.UnitId = model.UnitId;
            view.NameLabel.text = model.DisplayName;
            view.RoleLabel.text = model.RoleShortLabel;
            view.PositionLabel.text = model.PositionLabel;
            view.HpLabel.text = model.IsAlive
                ? LocalizationService.Format("ui.label.hp_value", "HP {0}/{1}", model.CurrentHp, model.MaxHp)
                : LocalizationService.Text("ui.roster.defeated", "已擊破");
            view.HpFill.fillAmount = model.IsAlive && model.MaxHp > 0 ? (float)model.CurrentHp / model.MaxHp : 0f;
            view.HpFill.color = model.IsAlive
                ? (view.HpFill.fillAmount > 0.55f
                    ? new Color(0.39f, 0.81f, 0.42f, 1f)
                    : view.HpFill.fillAmount > 0.3f
                        ? new Color(0.91f, 0.74f, 0.22f, 1f)
                        : new Color(0.88f, 0.35f, 0.28f, 1f))
                : new Color(0.42f, 0.42f, 0.45f, 1f);
            view.RoleLabel.color = model.Faction == UnitFaction.Player
                ? new Color(0.95f, 0.88f, 0.62f, 1f)
                : new Color(0.99f, 0.78f, 0.58f, 1f);
            view.PositionLabel.color = model.IsAlive ? new Color(0.82f, 0.84f, 0.88f, 1f) : BattleUiTheme.TextMuted;

            Color accentColor = model.IsSelected
                ? new Color(0.98f, 0.86f, 0.4f, 0.98f)
                : model.IsThreateningSelection
                    ? new Color(0.95f, 0.52f, 0.38f, 0.95f)
                    : model.IsExposed
                        ? new Color(0.96f, 0.62f, 0.34f, 0.95f)
                        : model.IsLowHp
                            ? new Color(0.94f, 0.42f, 0.32f, 0.95f)
                            : model.Faction == UnitFaction.Player
                                ? new Color(0.43f, 0.69f, 0.98f, 0.95f)
                                : new Color(0.95f, 0.44f, 0.35f, 0.95f);
            view.Accent.color = accentColor;

            Color baseColor = model.Faction == UnitFaction.Player
                ? new Color(0.1f, 0.16f, 0.25f, model.IsAlive ? 0.95f : 0.68f)
                : new Color(0.24f, 0.1f, 0.1f, model.IsAlive ? 0.95f : 0.68f);
            if (model.IsExposed && model.IsAlive)
            {
                baseColor = Color.Lerp(baseColor, new Color(0.36f, 0.16f, 0.1f, baseColor.a), 0.5f);
            }
            if (model.HasActed && model.IsAlive)
            {
                baseColor *= new Color(0.82f, 0.82f, 0.82f, 1f);
            }

            view.Background.color = baseColor;
            view.Outline.effectColor = model.IsSelected
                ? new Color(0.98f, 0.86f, 0.4f, 0.95f)
                : model.IsThreateningSelection
                    ? new Color(0.95f, 0.52f, 0.38f, 0.9f)
                    : new Color(0.38f, 0.31f, 0.24f, 0.75f);
            BindTagChip(view.PrimaryTagView, model.PrimaryTag);
            BindTagChip(view.SecondaryTagView, model.SecondaryTag);

            view.Button.interactable = model.IsAlive;
            view.Button.onClick.RemoveAllListeners();
            view.Button.onClick.AddListener(() => rosterSelectionHandler?.Invoke(view.UnitId));
        }

        private static void BindTagChip(TagChipView view, BattleRosterTag? tag)
        {
            if (view == null)
            {
                return;
            }

            if (!tag.HasValue)
            {
                view.Root.SetActive(false);
                return;
            }

            view.Root.SetActive(true);
            view.Label.text = GetRosterTagText(tag.Value);
            switch (tag.Value)
            {
                case BattleRosterTag.Exposed:
                    view.Background.color = BattleUiTheme.ChipWarning;
                    view.Label.color = BattleUiTheme.TextPrimary;
                    break;
                case BattleRosterTag.Threatening:
                    view.Background.color = new Color(0.31f, 0.18f, 0.14f, 0.96f);
                    view.Label.color = new Color(1f, 0.88f, 0.74f, 1f);
                    break;
                case BattleRosterTag.SkillReady:
                    view.Background.color = BattleUiTheme.ChipInfo;
                    view.Label.color = BattleUiTheme.TextPrimary;
                    break;
                case BattleRosterTag.LowHp:
                    view.Background.color = new Color(0.26f, 0.18f, 0.12f, 0.96f);
                    view.Label.color = BattleUiTheme.TextPrimary;
                    break;
                case BattleRosterTag.Done:
                    view.Background.color = BattleUiTheme.ChipNeutral;
                    view.Label.color = BattleUiTheme.TextMuted;
                    break;
                default:
                    view.Background.color = BattleUiTheme.ChipPositive;
                    view.Label.color = BattleUiTheme.TextPrimary;
                    break;
            }
        }

        private static string GetRosterTagText(BattleRosterTag tag)
        {
            switch (tag)
            {
                case BattleRosterTag.Ready:
                    return LocalizationService.Text("ui.roster.ready", "可動");
                case BattleRosterTag.Done:
                    return LocalizationService.Text("ui.roster.done", "已動");
                case BattleRosterTag.SkillReady:
                    return LocalizationService.Text("ui.roster.skill_ready", "技能可用");
                case BattleRosterTag.Threatening:
                    return LocalizationService.Text("ui.roster.threatening", "威脅");
                case BattleRosterTag.LowHp:
                    return LocalizationService.Text("ui.roster.low_hp", "危急");
                case BattleRosterTag.Exposed:
                    return LocalizationService.Text("ui.threat.exposed_short", "暴露");
                default:
                    return tag.ToString();
            }
        }

        private static RosterEntryView CreateRosterEntryView(Transform parent)
        {
            GameObject rootObject = BattleHudFactory.CreatePanel(
                "RosterEntry",
                parent,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(0f, 76f),
                new Color(0.14f, 0.16f, 0.2f, 0.94f));
            LayoutElement rootLayout = rootObject.AddComponent<LayoutElement>();
            rootLayout.preferredHeight = 76f;

            Button button = rootObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.04f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.08f);
            colors.disabledColor = new Color(0.65f, 0.65f, 0.68f, 0.85f);
            button.colors = colors;

            Image background = rootObject.GetComponent<Image>();
            Outline outline = rootObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = BattleUiTheme.DividerSoft;

            GameObject accentObject = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accentObject.transform.SetParent(rootObject.transform, false);
            RectTransform accentRect = accentObject.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.sizeDelta = new Vector2(4f, 0f);
            accentRect.anchoredPosition = Vector2.zero;
            Image accentImage = accentObject.GetComponent<Image>();
            accentImage.sprite = RuntimeSpriteLibrary.WhiteSprite;

            GameObject contentRoot = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contentRoot.transform.SetParent(rootObject.transform, false);
            RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(10f, 8f);
            contentRect.offsetMax = new Vector2(-10f, -8f);
            HorizontalLayoutGroup contentLayout = contentRoot.GetComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = 10f;
            contentLayout.childAlignment = TextAnchor.MiddleLeft;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = false;

            GameObject textColumn = new GameObject("TextColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textColumn.transform.SetParent(contentRoot.transform, false);
            textColumn.GetComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup textLayout = textColumn.GetComponent<VerticalLayoutGroup>();
            textLayout.spacing = 3f;
            textLayout.childControlHeight = true;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandHeight = false;

            GameObject topRow = new GameObject("TopRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            topRow.transform.SetParent(textColumn.transform, false);
            topRow.GetComponent<LayoutElement>().preferredHeight = 18f;
            HorizontalLayoutGroup topLayout = topRow.GetComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 6f;
            topLayout.childControlHeight = true;
            topLayout.childControlWidth = true;
            topLayout.childForceExpandHeight = false;
            topLayout.childForceExpandWidth = false;

            Text nameLabel = BattleHudFactory.CreateText(topRow.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            nameLabel.GetComponent<LayoutElement>().flexibleWidth = 1f;
            nameLabel.verticalOverflow = VerticalWrapMode.Truncate;
            Text roleLabel = BattleHudFactory.CreateText(topRow.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleRight, BattleUiTheme.TextGold);
            roleLabel.GetComponent<LayoutElement>().preferredWidth = 56f;
            roleLabel.verticalOverflow = VerticalWrapMode.Truncate;

            Text positionLabel = BattleHudFactory.CreateText(textColumn.transform, string.Empty, 11, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextSecondary);
            positionLabel.GetComponent<LayoutElement>().preferredHeight = 16f;
            positionLabel.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject tagRow = new GameObject("TagRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            tagRow.transform.SetParent(textColumn.transform, false);
            tagRow.GetComponent<LayoutElement>().preferredHeight = 22f;
            HorizontalLayoutGroup tagLayout = tagRow.GetComponent<HorizontalLayoutGroup>();
            tagLayout.spacing = 6f;
            tagLayout.childControlHeight = true;
            tagLayout.childControlWidth = false;
            tagLayout.childForceExpandHeight = false;
            tagLayout.childForceExpandWidth = false;
            TagChipView primaryTagView = CreateTagChip(tagRow.transform);
            TagChipView secondaryTagView = CreateTagChip(tagRow.transform);

            GameObject hpColumn = new GameObject("HpColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            hpColumn.transform.SetParent(contentRoot.transform, false);
            hpColumn.GetComponent<LayoutElement>().preferredWidth = 92f;
            VerticalLayoutGroup hpLayout = hpColumn.GetComponent<VerticalLayoutGroup>();
            hpLayout.spacing = 4f;
            hpLayout.childControlHeight = true;
            hpLayout.childControlWidth = true;
            hpLayout.childForceExpandHeight = false;
            hpLayout.childForceExpandWidth = true;

            Text hpLabel = BattleHudFactory.CreateText(hpColumn.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleRight, BattleUiTheme.TextPrimary);
            hpLabel.GetComponent<LayoutElement>().preferredHeight = 18f;
            hpLabel.verticalOverflow = VerticalWrapMode.Truncate;
            GameObject hpBarRoot = new GameObject("HpBarRoot", typeof(RectTransform), typeof(LayoutElement));
            hpBarRoot.transform.SetParent(hpColumn.transform, false);
            hpBarRoot.GetComponent<LayoutElement>().preferredHeight = 12f;
            BattleHudFactory.CreateStretchUiBar(hpBarRoot.transform, out Image hpFill);

            return new RosterEntryView(rootObject, button, background, outline, accentImage, nameLabel, roleLabel, positionLabel, primaryTagView, secondaryTagView, hpLabel, hpFill);
        }

        private static TagChipView CreateTagChip(Transform parent)
        {
            GameObject root = BattleHudFactory.CreateInsetPanel("TagChip", parent, 20f, BattleUiTheme.ChipNeutral);
            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.preferredWidth = 86f;
            Text label = BattleHudFactory.CreateText(root.transform, string.Empty, 10, FontStyle.Bold, TextAnchor.MiddleCenter, BattleUiTheme.TextPrimary);
            RectTransform textRect = label.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 2f);
            textRect.offsetMax = new Vector2(-6f, -2f);
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return new TagChipView(root, root.GetComponent<Image>(), label);
        }
    }

    internal sealed class CampaignOverlayView
    {
        private GameObject overlay;
        private GameObject buttonRow;
        private Text eyebrowLabel;
        private Text titleLabel;
        private Text bodyLabel;
        private Text progressLabel;
        private Text highlightLabel;
        private Text deckTitleLabel;
        private Text deckBodyLabel;
        private Transform contentRoot;
        private GameObject highlightPanel;
        private Button primaryButton;
        private Button secondaryButton;
        private ScrollRect contentScrollRect;

        private Action<int> stageSelectionHandler;
        private Action<string> optionSelectionHandler;
        private Action primaryHandler;
        private Action secondaryHandler;

        public bool IsVisible => overlay != null && overlay.activeSelf;

        public Text BodyLabel => bodyLabel;

        public Button PrimaryButton => primaryButton;

        public void Initialize(Transform canvasRoot)
        {
            overlay = BattleHudFactory.CreateStretchPanel("CampaignOverlay", canvasRoot, BattleUiTheme.PanelOverlay);
            GameObject campaignBox = BattleHudFactory.CreatePanel(
                "CampaignBox",
                overlay.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1220f, 760f),
                BattleUiTheme.ShellBackdrop);

            HorizontalLayoutGroup shellLayout = campaignBox.AddComponent<HorizontalLayoutGroup>();
            shellLayout.spacing = 18f;
            shellLayout.padding = new RectOffset(22, 22, 22, 22);
            shellLayout.childControlHeight = true;
            shellLayout.childControlWidth = true;
            shellLayout.childForceExpandHeight = true;
            shellLayout.childForceExpandWidth = false;

            GameObject leftRail = BattleHudFactory.CreateInsetPanel("CampaignLeftRail", campaignBox.transform, 0f, BattleUiTheme.PanelSurface);
            LayoutElement leftRailLayout = leftRail.GetComponent<LayoutElement>();
            leftRailLayout.preferredWidth = 336f;
            leftRailLayout.flexibleHeight = 1f;
            VerticalLayoutGroup leftLayout = leftRail.AddComponent<VerticalLayoutGroup>();
            leftLayout.spacing = 10f;
            leftLayout.padding = new RectOffset(18, 18, 18, 18);
            leftLayout.childControlHeight = true;
            leftLayout.childControlWidth = true;
            leftLayout.childForceExpandHeight = false;

            eyebrowLabel = BattleHudFactory.CreateText(leftRail.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            eyebrowLabel.GetComponent<LayoutElement>().preferredHeight = 16f;

            titleLabel = BattleHudFactory.CreateText(leftRail.transform, string.Empty, 30, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            titleLabel.GetComponent<LayoutElement>().preferredHeight = 64f;
            BattleHudFactory.EnableBestFit(titleLabel, 20, 30, true);

            GameObject summaryPanel = BattleHudFactory.CreateInsetPanel("CampaignSummaryPanel", leftRail.transform, 0f, BattleUiTheme.PanelInsetStrong);
            summaryPanel.GetComponent<LayoutElement>().flexibleHeight = 1f;
            Transform summaryRoot = BattleHudFactory.CreateInsetContentRoot(summaryPanel.transform, 16f);
            VerticalLayoutGroup summaryLayout = summaryRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            summaryLayout.spacing = 10f;
            summaryLayout.childControlHeight = true;
            summaryLayout.childControlWidth = true;
            summaryLayout.childForceExpandHeight = false;

            bodyLabel = BattleHudFactory.CreateText(summaryRoot, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            BattleHudFactory.EnableAutoHeight(bodyLabel, 120f);

            progressLabel = BattleHudFactory.CreateText(summaryRoot, string.Empty, 13, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            BattleHudFactory.EnableAutoHeight(progressLabel, 18f);

            highlightPanel = BattleHudFactory.CreateInsetPanel("CampaignHighlightPanel", summaryRoot.transform, 76f, BattleUiTheme.PanelReward);
            Transform highlightRoot = BattleHudFactory.CreateInsetContentRoot(highlightPanel.transform, 12f);
            highlightLabel = BattleHudFactory.CreateText(highlightRoot, string.Empty, 14, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.16f, 0.1f, 0.04f, 1f));
            BattleHudFactory.EnableAutoHeight(highlightLabel, 42f);

            GameObject rightDeck = BattleHudFactory.CreateInsetPanel("CampaignRightDeck", campaignBox.transform, 0f, BattleUiTheme.PanelBackdrop);
            LayoutElement rightDeckLayout = rightDeck.GetComponent<LayoutElement>();
            rightDeckLayout.flexibleWidth = 1f;
            rightDeckLayout.flexibleHeight = 1f;
            VerticalLayoutGroup rightLayout = rightDeck.AddComponent<VerticalLayoutGroup>();
            rightLayout.spacing = 10f;
            rightLayout.padding = new RectOffset(18, 18, 18, 18);
            rightLayout.childControlHeight = true;
            rightLayout.childControlWidth = true;
            rightLayout.childForceExpandHeight = false;

            deckTitleLabel = BattleHudFactory.CreateText(rightDeck.transform, string.Empty, 22, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            deckTitleLabel.GetComponent<LayoutElement>().preferredHeight = 28f;
            deckBodyLabel = BattleHudFactory.CreateText(rightDeck.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            BattleHudFactory.EnableAutoHeight(deckBodyLabel, 24f);

            GameObject contentPanel = BattleHudFactory.CreateInsetPanel("CampaignContentPanel", rightDeck.transform, 0f, new Color(0.12f, 0.11f, 0.1f, 0.95f));
            LayoutElement contentLayout = contentPanel.GetComponent<LayoutElement>();
            contentLayout.flexibleHeight = 1f;
            contentScrollRect = contentPanel.AddComponent<ScrollRect>();
            contentScrollRect.horizontal = false;
            contentScrollRect.vertical = true;
            contentScrollRect.movementType = ScrollRect.MovementType.Clamped;
            contentScrollRect.scrollSensitivity = 28f;

            GameObject viewport = new GameObject("CampaignViewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(contentPanel.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(14f, 14f);
            viewportRect.offsetMax = new Vector2(-14f, -14f);
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentObject = new GameObject("CampaignContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.offsetMin = new Vector2(0f, 0f);
            contentRect.offsetMax = new Vector2(0f, 0f);
            VerticalLayoutGroup contentGroup = contentObject.GetComponent<VerticalLayoutGroup>();
            contentGroup.spacing = 10f;
            contentGroup.childControlHeight = true;
            contentGroup.childControlWidth = true;
            contentGroup.childForceExpandHeight = false;
            contentGroup.childForceExpandWidth = true;
            ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentRoot = contentObject.transform;
            contentScrollRect.viewport = viewportRect;
            contentScrollRect.content = contentRect;

            buttonRow = new GameObject("CampaignButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            buttonRow.transform.SetParent(rightDeck.transform, false);
            LayoutElement buttonRowLayout = buttonRow.GetComponent<LayoutElement>();
            buttonRowLayout.preferredHeight = 48f;
            buttonRowLayout.flexibleHeight = 0f;
            HorizontalLayoutGroup buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 12f;
            buttonLayout.childAlignment = TextAnchor.MiddleRight;
            buttonLayout.childControlHeight = true;
            buttonLayout.childControlWidth = true;
            buttonLayout.childForceExpandHeight = true;
            buttonLayout.childForceExpandWidth = false;
            primaryButton = BattleHudFactory.CreateButton(buttonRow.transform, string.Empty, true);
            LayoutElement primaryLayout = primaryButton.GetComponent<LayoutElement>();
            primaryLayout.preferredWidth = 198f;
            primaryLayout.flexibleWidth = 0f;
            primaryButton.onClick.AddListener(() => primaryHandler?.Invoke());
            secondaryButton = BattleHudFactory.CreateButton(buttonRow.transform, string.Empty, false);
            LayoutElement secondaryLayout = secondaryButton.GetComponent<LayoutElement>();
            secondaryLayout.preferredWidth = 176f;
            secondaryLayout.flexibleWidth = 0f;
            secondaryButton.onClick.AddListener(() => secondaryHandler?.Invoke());

            overlay.SetActive(false);
        }

        public void ShowCampaignStageSelect(CampaignStageSelectModel model, Action<int> onStageSelected)
        {
            stageSelectionHandler = onStageSelected;
            optionSelectionHandler = null;
            primaryHandler = null;
            secondaryHandler = null;

            CampaignStageSelectModel stageSelectModel = model ?? new CampaignStageSelectModel();
            ApplyFrame(
                stageSelectModel.Eyebrow,
                stageSelectModel.Title,
                stageSelectModel.Body,
                stageSelectModel.ProgressLabel,
                stageSelectModel.HighlightLabel,
                stageSelectModel.DeckTitle,
                LocalizationService.Text("campaign.deck.stage_select_body", "選擇下一條戰線，先看清楚地形定位、戰鬥長度與首通收益。"));
            RebuildContent(root =>
            {
                foreach (CampaignStageEntryModel entry in (stageSelectModel.Stages ?? Array.Empty<CampaignStageEntryModel>())
                    .OrderBy(stage => stage.SortWeight)
                    .ThenBy(stage => stage.StageIndex))
                {
                    CreateStageEntry(root, entry);
                }
            });
            ConfigureButtons(null, null);
            ShowOverlay();
        }

        public void ShowCampaignInterlude(CampaignInterludeModel model, Action onPrimary, Action onSecondary = null)
        {
            stageSelectionHandler = null;
            optionSelectionHandler = null;
            primaryHandler = onPrimary;
            secondaryHandler = onSecondary;

            CampaignInterludeModel interludeModel = model ?? new CampaignInterludeModel();
            ApplyFrame(
                interludeModel.Eyebrow,
                interludeModel.Title,
                interludeModel.Body,
                interludeModel.ProgressLabel,
                interludeModel.HighlightLine,
                interludeModel.DeckTitle,
                string.IsNullOrWhiteSpace(interludeModel.DeckBody)
                    ? interludeModel.RiskLabel
                    : interludeModel.DeckBody);
            RebuildContent(root =>
            {
                GameObject narrativePanel = BattleHudFactory.CreateInsetPanel("CampaignNarrativePanel", root, 0f, new Color(0.14f, 0.12f, 0.1f, 0.94f));
                LayoutElement narrativePanelLayout = narrativePanel.GetComponent<LayoutElement>();
                Transform narrativeRoot = BattleHudFactory.CreateInsetContentRoot(narrativePanel.transform, 18f);
                VerticalLayoutGroup narrativeLayout = narrativeRoot.gameObject.AddComponent<VerticalLayoutGroup>();
                narrativeLayout.spacing = 10f;
                narrativeLayout.childControlHeight = true;
                narrativeLayout.childControlWidth = true;
                narrativeLayout.childForceExpandHeight = false;

                List<(Text text, float minHeight)> narrativeBlocks = new List<(Text text, float minHeight)>();
                Text narrativeBody = BattleHudFactory.CreateText(narrativeRoot, interludeModel.Body, 18, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
                BattleHudFactory.EnableAutoHeight(narrativeBody, 56f);
                narrativeBlocks.Add((narrativeBody, 56f));

                foreach (string line in (interludeModel.DetailLines ?? Array.Empty<string>()).Where(line => !string.IsNullOrWhiteSpace(line)))
                {
                    Text detailLabel = BattleHudFactory.CreateText(narrativeRoot, line, 15, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
                    BattleHudFactory.EnableAutoHeight(detailLabel, 20f);
                    narrativeBlocks.Add((detailLabel, 20f));
                }

                if (!string.IsNullOrWhiteSpace(interludeModel.HighlightLine))
                {
                    Text highlight = BattleHudFactory.CreateText(narrativeRoot, interludeModel.HighlightLine, 16, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextGold);
                    BattleHudFactory.EnableAutoHeight(highlight, 22f);
                    narrativeBlocks.Add((highlight, 22f));
                }

                if (!string.IsNullOrWhiteSpace(interludeModel.RiskLabel))
                {
                    Text risk = BattleHudFactory.CreateText(narrativeRoot, interludeModel.RiskLabel, 14, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextWarning);
                    BattleHudFactory.EnableAutoHeight(risk, 20f);
                    narrativeBlocks.Add((risk, 20f));
                }

                Canvas.ForceUpdateCanvases();
                float panelHeight = 36f;
                foreach ((Text text, float minHeight) block in narrativeBlocks)
                {
                    panelHeight += BattleHudFactory.RefreshAutoHeight(block.text, block.minHeight);
                }

                if (narrativeBlocks.Count > 1)
                {
                    panelHeight += (narrativeBlocks.Count - 1) * narrativeLayout.spacing;
                }

                narrativePanelLayout.preferredHeight = panelHeight;
            });
            ConfigureButtons(interludeModel.PrimaryActionLabel, interludeModel.SecondaryActionLabel);
            ShowOverlay();
        }

        public void ShowCampaignOptionList(CampaignOptionListModel model, Action<string> onOptionSelected, Action onPrimary, Action onSecondary = null)
        {
            stageSelectionHandler = null;
            optionSelectionHandler = onOptionSelected;
            primaryHandler = onPrimary;
            secondaryHandler = onSecondary;

            CampaignOptionListModel optionListModel = model ?? new CampaignOptionListModel();
            ApplyFrame(
                optionListModel.Eyebrow,
                optionListModel.Title,
                optionListModel.Body,
                optionListModel.ProgressLabel,
                optionListModel.HighlightLabel,
                optionListModel.DeckTitle,
                LocalizationService.Text("campaign.deck.option_list_body", "主要操作、整備與獎勵資訊都集中在右側卡片列。"));
            RebuildContent(root =>
            {
                string currentSection = null;
                foreach (CampaignOptionEntryModel entry in (optionListModel.Options ?? Array.Empty<CampaignOptionEntryModel>())
                    .OrderBy(option => option.SortWeight)
                    .ThenBy(option => option.Title, StringComparer.Ordinal))
                {
                    if (!string.Equals(currentSection, entry.Section, StringComparison.Ordinal) &&
                        !string.IsNullOrWhiteSpace(entry.Section))
                    {
                        currentSection = entry.Section;
                        CreateListSectionHeader(root, currentSection);
                    }

                    CreateOptionEntry(root, entry);
                }
            });
            ConfigureButtons(optionListModel.PrimaryActionLabel, optionListModel.SecondaryActionLabel);
            ShowOverlay();
        }

        public void Hide()
        {
            stageSelectionHandler = null;
            optionSelectionHandler = null;
            primaryHandler = null;
            secondaryHandler = null;
            if (overlay != null)
            {
                overlay.SetActive(false);
            }
        }

        private void ApplyFrame(string eyebrow, string title, string body, string progress, string highlight, string deckTitle, string deckBody)
        {
            eyebrowLabel.text = eyebrow ?? string.Empty;
            eyebrowLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(eyebrow));
            titleLabel.text = title ?? string.Empty;
            bodyLabel.text = body ?? string.Empty;
            progressLabel.text = progress ?? string.Empty;
            progressLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(progress));
            deckTitleLabel.text = string.IsNullOrWhiteSpace(deckTitle)
                ? LocalizationService.Text("campaign.deck.default", "作戰卡片")
                : deckTitle;
            deckBodyLabel.text = deckBody ?? string.Empty;
            deckBodyLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(deckBody));
            bool hasHighlight = !string.IsNullOrWhiteSpace(highlight);
            highlightPanel.SetActive(hasHighlight);
            if (hasHighlight)
            {
                highlightLabel.text = highlight;
            }

            BattleHudFactory.RefreshAutoHeight(bodyLabel, 120f);
            BattleHudFactory.RefreshAutoHeight(progressLabel, 18f);
            BattleHudFactory.RefreshAutoHeight(deckBodyLabel, 24f);
            if (hasHighlight)
            {
                float highlightHeight = BattleHudFactory.RefreshAutoHeight(highlightLabel, 42f);
                LayoutElement highlightLayout = highlightPanel.GetComponent<LayoutElement>();
                if (highlightLayout != null)
                {
                    highlightLayout.preferredHeight = Mathf.Max(76f, highlightHeight + 24f);
                }
            }
        }

        private void ConfigureButtons(string primaryLabel, string secondaryLabel)
        {
            bool hasPrimary = !string.IsNullOrWhiteSpace(primaryLabel);
            bool hasSecondary = !string.IsNullOrWhiteSpace(secondaryLabel);
            if (buttonRow != null)
            {
                buttonRow.SetActive(hasPrimary || hasSecondary);
            }

            primaryButton.gameObject.SetActive(hasPrimary);
            secondaryButton.gameObject.SetActive(hasSecondary);
            if (hasPrimary)
            {
                BattleHudFactory.SetButtonLabel(primaryButton, primaryLabel);
            }

            if (hasSecondary)
            {
                BattleHudFactory.SetButtonLabel(secondaryButton, secondaryLabel);
            }
        }

        private void RebuildContent(Action<Transform> buildAction)
        {
            BattleHudFactory.DestroyChildren(contentRoot);
            buildAction?.Invoke(contentRoot);
            Canvas.ForceUpdateCanvases();
            if (contentScrollRect != null)
            {
                contentScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void CreateStageEntry(Transform parent, CampaignStageEntryModel model)
        {
            GameObject root = BattleHudFactory.CreateInsetPanel("CampaignStageEntry", parent, 0f, model.IsUnlocked ? BattleUiTheme.PanelInsetStrong : BattleUiTheme.PanelGhost);
            LayoutElement rootLayout = root.GetComponent<LayoutElement>();
            rootLayout.preferredHeight = !string.IsNullOrWhiteSpace(model.RecommendedReason) ? 134f : 114f;
            rootLayout.flexibleHeight = 0f;
            Button button = root.AddComponent<Button>();
            button.interactable = model.IsUnlocked;
            button.onClick.AddListener(() => stageSelectionHandler?.Invoke(model.StageIndex));
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.04f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.08f);
            colors.disabledColor = new Color(0.7f, 0.7f, 0.72f, 0.8f);
            button.colors = colors;

            Transform content = BattleHudFactory.CreateInsetContentRoot(root.transform, 16f);
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            GameObject titleRow = new GameObject("TitleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            titleRow.transform.SetParent(content, false);
            titleRow.GetComponent<LayoutElement>().preferredHeight = 30f;
            HorizontalLayoutGroup titleLayout = titleRow.GetComponent<HorizontalLayoutGroup>();
            titleLayout.spacing = 8f;
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.childControlHeight = true;
            titleLayout.childControlWidth = true;
            titleLayout.childForceExpandHeight = false;
            titleLayout.childForceExpandWidth = false;

            Text title = BattleHudFactory.CreateText(titleRow.transform, model.Title, 19, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            LayoutElement titleLayoutElement = title.GetComponent<LayoutElement>();
            titleLayoutElement.minWidth = 120f;
            titleLayoutElement.preferredWidth = 180f;
            titleLayoutElement.flexibleWidth = 1f;
            ClampText(title, 26f, VerticalWrapMode.Truncate);
            BattleHudFactory.EnableBestFit(title, 14, 19, false);
            Text status = BattleHudFactory.CreateText(titleRow.transform, model.Status, 12, FontStyle.Bold, TextAnchor.MiddleRight, model.IsUnlocked ? BattleUiTheme.TextGold : BattleUiTheme.TextMuted);
            LayoutElement statusLayout = status.GetComponent<LayoutElement>();
            statusLayout.minWidth = 96f;
            statusLayout.preferredWidth = 132f;
            statusLayout.flexibleWidth = 0f;
            ClampText(status, 18f, VerticalWrapMode.Truncate);

            if (!string.IsNullOrWhiteSpace(model.Description))
            {
                Text description = BattleHudFactory.CreateText(content, model.Description, 14, FontStyle.Normal, TextAnchor.UpperLeft, model.IsUnlocked ? BattleUiTheme.TextSecondary : BattleUiTheme.TextMuted);
                ClampText(description, 34f, VerticalWrapMode.Truncate);
            }

            string metrics = string.Join("  ", new[] { model.BattlefieldLabel, model.DurationLabel, model.RewardLabel }.Where(line => !string.IsNullOrWhiteSpace(line)));
            if (!string.IsNullOrWhiteSpace(metrics))
            {
                Text metricLabel = BattleHudFactory.CreateText(content, metrics, 12, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
                ClampText(metricLabel, 18f, VerticalWrapMode.Truncate);
            }

            if (!string.IsNullOrWhiteSpace(model.RecommendedReason))
            {
                Text reason = BattleHudFactory.CreateText(content, model.RecommendedReason, 13, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.86f, 0.91f, 0.98f, 1f));
                ClampText(reason, 24f, VerticalWrapMode.Truncate);
            }
        }

        private void CreateOptionEntry(Transform parent, CampaignOptionEntryModel model)
        {
            GameObject root = BattleHudFactory.CreateInsetPanel("CampaignOptionEntry", parent, 0f, model.IsEnabled ? (model.IsEmphasized ? BattleUiTheme.PanelCommand : BattleUiTheme.PanelInset) : BattleUiTheme.PanelGhost);
            LayoutElement rootLayout = root.GetComponent<LayoutElement>();
            bool hasSupportingLine = !string.IsNullOrWhiteSpace(model.RecommendedReason) || !string.IsNullOrWhiteSpace(model.AvailabilityReason);
            bool hasDetailLines = (model.DetailLines ?? Array.Empty<string>()).Any(line => !string.IsNullOrWhiteSpace(line));
            rootLayout.preferredHeight = hasDetailLines ? 132f : (hasSupportingLine || model.IsPromotionOption ? 124f : 98f);
            rootLayout.flexibleHeight = 0f;
            Button button = root.AddComponent<Button>();
            button.interactable = model.IsEnabled;
            button.onClick.AddListener(() => optionSelectionHandler?.Invoke(model.OptionId));
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.04f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.08f);
            colors.disabledColor = new Color(0.7f, 0.7f, 0.72f, 0.8f);
            button.colors = colors;

            Transform content = BattleHudFactory.CreateInsetContentRoot(root.transform, 14f);
            HorizontalLayoutGroup layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            GameObject glyphPanel = BattleHudFactory.CreateInsetPanel("GlyphPanel", content.transform, 56f, model.IsPromotionOption ? BattleUiTheme.PanelReward : BattleUiTheme.PanelGhost);
            LayoutElement glyphLayout = glyphPanel.GetComponent<LayoutElement>();
            glyphLayout.preferredWidth = 56f;
            glyphLayout.preferredHeight = 56f;
            glyphLayout.flexibleWidth = 0f;
            glyphLayout.flexibleHeight = 0f;
            Text glyph = BattleHudFactory.CreateAbsoluteText(glyphPanel.transform, Vector2.zero, Vector2.zero, model.IconGlyph, 22, FontStyle.Bold, TextAnchor.MiddleCenter, BattleUiTheme.TextGold);
            glyph.resizeTextForBestFit = true;
            glyph.resizeTextMinSize = 14;
            glyph.resizeTextMaxSize = 22;
            glyph.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject textColumn = new GameObject("TextColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textColumn.transform.SetParent(content, false);
            LayoutElement textColumnLayout = textColumn.GetComponent<LayoutElement>();
            textColumnLayout.flexibleWidth = 1f;
            textColumnLayout.flexibleHeight = 0f;
            VerticalLayoutGroup textLayout = textColumn.GetComponent<VerticalLayoutGroup>();
            textLayout.spacing = 4f;
            textLayout.childControlHeight = true;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandHeight = false;

            GameObject titleRow = new GameObject("TitleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            titleRow.transform.SetParent(textColumn.transform, false);
            titleRow.GetComponent<LayoutElement>().preferredHeight = model.IsPromotionOption ? 30f : 28f;
            HorizontalLayoutGroup titleLayout = titleRow.GetComponent<HorizontalLayoutGroup>();
            titleLayout.spacing = 8f;
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.childControlHeight = true;
            titleLayout.childControlWidth = true;
            titleLayout.childForceExpandHeight = false;
            titleLayout.childForceExpandWidth = false;

            Text title = BattleHudFactory.CreateText(titleRow.transform, model.Title, model.IsPromotionOption ? 21 : 18, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            LayoutElement titleLayoutElement = title.GetComponent<LayoutElement>();
            titleLayoutElement.minWidth = 120f;
            titleLayoutElement.preferredWidth = 176f;
            titleLayoutElement.flexibleWidth = 1f;
            ClampText(title, 26f, VerticalWrapMode.Truncate);
            BattleHudFactory.EnableBestFit(title, 14, model.IsPromotionOption ? 21 : 18, false);
            Text status = BattleHudFactory.CreateText(titleRow.transform, model.Status, 12, FontStyle.Bold, TextAnchor.MiddleRight, model.IsEnabled ? BattleUiTheme.TextGold : BattleUiTheme.TextMuted);
            LayoutElement statusLayout = status.GetComponent<LayoutElement>();
            statusLayout.minWidth = 96f;
            statusLayout.preferredWidth = 132f;
            statusLayout.flexibleWidth = 0f;
            ClampText(status, 18f, VerticalWrapMode.Truncate);

            if (!string.IsNullOrWhiteSpace(model.MetricLine))
            {
                Text metric = BattleHudFactory.CreateText(textColumn.transform, model.MetricLine, 12, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextSecondary);
                ClampText(metric, 16f, VerticalWrapMode.Truncate);
            }

            Text description = BattleHudFactory.CreateText(textColumn.transform, model.Description, model.IsPromotionOption ? 15 : 14, FontStyle.Normal, TextAnchor.UpperLeft, model.IsEnabled ? BattleUiTheme.TextPrimary : new Color(0.62f, 0.59f, 0.54f, 1f));
            ClampText(description, 36f, VerticalWrapMode.Truncate);

            if (!string.IsNullOrWhiteSpace(model.RecommendedReason))
            {
                Text reason = BattleHudFactory.CreateText(textColumn.transform, model.RecommendedReason, 13, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.82f, 0.91f, 0.99f, 1f));
                ClampText(reason, 18f, VerticalWrapMode.Truncate);
            }

            if (!string.IsNullOrWhiteSpace(model.AvailabilityReason))
            {
                Text availability = BattleHudFactory.CreateText(textColumn.transform, model.AvailabilityReason, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextWarning);
                ClampText(availability, 18f, VerticalWrapMode.Truncate);
            }

            if (hasDetailLines)
            {
                foreach (string line in model.DetailLines.Where(line => !string.IsNullOrWhiteSpace(line)))
                {
                    Text detailLabel = BattleHudFactory.CreateText(textColumn.transform, line, 11, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextMuted);
                    BattleHudFactory.EnableAutoHeight(detailLabel, 18f);
                }

                FinalizeDynamicEntryHeight(root, 132f);
            }
        }

        private void ShowOverlay()
        {
            if (overlay == null)
            {
                return;
            }

            overlay.transform.SetAsLastSibling();
            overlay.SetActive(true);
            Canvas.ForceUpdateCanvases();
            if (contentScrollRect != null)
            {
                contentScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private static void ClampText(Text text, float preferredHeight, VerticalWrapMode verticalMode)
        {
            if (text == null)
            {
                return;
            }

            LayoutElement layout = text.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = preferredHeight;
                layout.flexibleHeight = 0f;
            }

            text.verticalOverflow = verticalMode;
        }

        private static void FinalizeDynamicEntryHeight(GameObject root, float minimumHeight)
        {
            if (root == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutElement rootLayout = root.GetComponent<LayoutElement>();
            if (rootLayout == null)
            {
                return;
            }

            float preferredHeight = LayoutUtility.GetPreferredHeight(root.transform as RectTransform);
            rootLayout.preferredHeight = Mathf.Max(minimumHeight, preferredHeight);
        }

        private static void CreateListSectionHeader(Transform parent, string label)
        {
            GameObject headerRoot = new GameObject("SectionHeader", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            headerRoot.transform.SetParent(parent, false);
            headerRoot.GetComponent<LayoutElement>().preferredHeight = 22f;
            HorizontalLayoutGroup layout = headerRoot.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            Text sectionLabel = BattleHudFactory.CreateText(headerRoot.transform, label, 13, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            sectionLabel.GetComponent<LayoutElement>().preferredWidth = 140f;

            GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            divider.transform.SetParent(headerRoot.transform, false);
            divider.GetComponent<LayoutElement>().preferredHeight = 2f;
            divider.GetComponent<LayoutElement>().flexibleWidth = 1f;
            Image dividerImage = divider.GetComponent<Image>();
            dividerImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
            dividerImage.color = BattleUiTheme.Divider;
        }
    }

    #endif

    internal enum BattleTextRole
    {
        SingleLineTitle,
        DenseMeta,
        ChipText,
        TwoLineSummary,
        BodyAuto,
    }

    internal static class BattleTextLayoutPolicy
    {
        public static void Apply(Text text, BattleTextRole role)
        {
            if (text == null)
            {
                return;
            }

            LayoutElement layout = text.GetComponent<LayoutElement>();
            ContentSizeFitter fitter = text.GetComponent<ContentSizeFitter>();

            switch (role)
            {
                case BattleTextRole.SingleLineTitle:
                    ConfigureSingleLine(text, layout, 1, 4f);
                    DisableAutoHeight(fitter);
                    break;
                case BattleTextRole.DenseMeta:
                    ConfigureSingleLine(text, layout, 1, 2f);
                    DisableAutoHeight(fitter);
                    break;
                case BattleTextRole.ChipText:
                    ConfigureSingleLine(text, layout, 1, 1f);
                    DisableAutoHeight(fitter);
                    break;
                case BattleTextRole.TwoLineSummary:
                    ConfigureLimitedMultiLine(text, layout, 2, 4f);
                    DisableAutoHeight(fitter);
                    break;
                case BattleTextRole.BodyAuto:
                    ConfigureAutoHeight(text, layout);
                    EnsureAutoHeight(fitter, text.gameObject);
                    break;
            }
        }

        public static float Refresh(Text text, BattleTextRole role, float minHeight = 0f)
        {
            if (text == null)
            {
                return minHeight;
            }

            Apply(text, role);
            LayoutElement layout = text.GetComponent<LayoutElement>();
            if (layout == null)
            {
                return minHeight;
            }

            if (role != BattleTextRole.BodyAuto)
            {
                float currentHeight = layout.preferredHeight > 0f ? layout.preferredHeight : layout.minHeight;
                return Mathf.Max(minHeight, currentHeight);
            }

            layout.preferredHeight = -1f;
            Canvas.ForceUpdateCanvases();

            float availableWidth = text.rectTransform.rect.width;
            if (availableWidth <= 1f)
            {
                availableWidth = 600f;
            }

            float measuredHeight = Mathf.Ceil(text.GetPreferredValues(text.text, availableWidth, 0f).y);
            float finalHeight = Mathf.Max(minHeight, measuredHeight);
            layout.minHeight = minHeight;
            layout.preferredHeight = finalHeight;
            layout.flexibleHeight = 0f;
            return finalHeight;
        }

        private static void ConfigureSingleLine(Text text, LayoutElement layout, int lines, float padding)
        {
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.maxVisibleLines = lines;
            if (layout != null)
            {
                float height = EstimateHeight(text.fontSize, lines, padding);
                layout.minHeight = height;
                layout.preferredHeight = height;
                layout.flexibleHeight = 0f;
            }
        }

        private static void ConfigureLimitedMultiLine(Text text, LayoutElement layout, int lines, float padding)
        {
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.maxVisibleLines = lines;
            if (layout != null)
            {
                float height = EstimateHeight(text.fontSize, lines, padding);
                layout.minHeight = height;
                layout.preferredHeight = height;
                layout.flexibleHeight = 0f;
            }
        }

        private static void ConfigureAutoHeight(Text text, LayoutElement layout)
        {
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.maxVisibleLines = 0;
            if (layout != null)
            {
                layout.preferredHeight = -1f;
                layout.flexibleHeight = 0f;
            }
        }

        private static float EstimateHeight(float fontSize, int lines, float padding)
        {
            float lineHeight = Mathf.Max(fontSize * 1.2f, fontSize + 2f);
            return Mathf.Ceil(lineHeight * lines + padding);
        }

        private static void DisableAutoHeight(ContentSizeFitter fitter)
        {
            if (fitter == null)
            {
                return;
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        private static void EnsureAutoHeight(ContentSizeFitter fitter, GameObject owner)
        {
            if (fitter == null)
            {
                fitter = owner.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    internal static class BattlePanelHeightPolicy
    {
        public const float OverviewSectionSpacing = 6f;
        public const float OverviewSummaryHeight = 120f;
        public const float OverviewObjectiveHeight = 96f;
        public const float OverviewCommandHeight = 46f;
        public const float OverviewTabHeight = 28f;
        public const float OverviewVisibleEntryCount = 5f;
        public const float OverviewRosterEntryHeight = 84f;
        public const float OverviewRosterEntrySpacing = 6f;
        public const float OverviewContentInset = 20f;
        public const float OverviewContentSpacing = 8f;
        public const float SelectedDetailCollapsedHeight = 52f;
        public const float SelectedDetailExpandedMinHeight = 188f;
        public const float SelectedDetailScrollMinHeight = 132f;

        public static float CalculateOverviewRosterViewportMinHeight(float scale = 1f)
        {
            return OverviewVisibleEntryCount * ScaleValue(OverviewRosterEntryHeight, scale) +
                   (OverviewVisibleEntryCount - 1f) * ScaleValue(OverviewRosterEntrySpacing, scale);
        }

        public static float CalculateOverviewContentTargetHeight(float panelHeight, float scale = 1f, float sectionSpacing = OverviewSectionSpacing)
        {
            float consumedHeight = 32f +
                                   18f +
                                   ScaleValue(OverviewSummaryHeight, scale) +
                                   ScaleValue(OverviewObjectiveHeight, scale) +
                                   ScaleValue(OverviewCommandHeight, scale) +
                                   sectionSpacing * 4f;
            float available = Mathf.Max(320f, panelHeight - consumedHeight);
            float requiredForFiveRows = ScaleValue(OverviewContentInset, scale) +
                                        ScaleValue(OverviewTabHeight, scale) +
                                        ScaleValue(OverviewContentSpacing, scale) +
                                        CalculateOverviewRosterViewportMinHeight(scale);
            return Mathf.Min(available, Mathf.Max(320f, requiredForFiveRows));
        }

        public static float GetSelectedDetailCollapsedHeight(float scale = 1f)
        {
            return ScaleValue(SelectedDetailCollapsedHeight, scale);
        }

        public static float GetSelectedDetailExpandedMinHeight(float scale = 1f)
        {
            return ScaleValue(SelectedDetailExpandedMinHeight, scale);
        }

        public static float GetSelectedDetailScrollMinHeight(float scale = 1f)
        {
            return ScaleValue(SelectedDetailScrollMinHeight, scale);
        }

        private static float ScaleValue(float value, float scale)
        {
            return Mathf.Ceil(value * Mathf.Max(0.8f, scale));
        }
    }

    internal static class BattleHudFactory
    {
        public static GameObject CreatePanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            image.color = color;
            image.type = Image.Type.Sliced;

            Outline outline = panel.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = BattleUiTheme.OutlineStrong;
            return panel;
        }

        public static GameObject CreatePanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            return CreatePanel(name, parent, anchorMin, anchorMax, anchoredPosition, size, BattleUiTheme.PanelBackdrop);
        }

        public static GameObject CreateStretchPanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = panel.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.WhiteSprite;
            image.color = color;
            return panel;
        }

        public static GameObject CreateInsetPanel(string name, Transform parent, float preferredHeight)
        {
            return CreateInsetPanel(name, parent, preferredHeight, BattleUiTheme.PanelInset);
        }

        public static GameObject CreateInsetPanel(string name, Transform parent, float preferredHeight, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            image.color = color;
            image.type = Image.Type.Sliced;

            LayoutElement layoutElement = panel.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = preferredHeight;

            Outline outline = panel.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = BattleUiTheme.OutlineSoft;
            return panel;
        }

        public static Button CreateButton(Transform parent, string label, bool primary)
        {
            GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 40f;
            layoutElement.flexibleWidth = 1f;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            image.color = primary ? BattleUiTheme.ButtonPrimary : BattleUiTheme.ButtonSecondary;
            image.type = Image.Type.Sliced;

            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = primary ? new Color(0.34f, 0.22f, 0.08f, 0.66f) : new Color(0.24f, 0.33f, 0.48f, 0.6f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = primary ? BattleUiTheme.ButtonPrimaryHighlight : BattleUiTheme.ButtonSecondaryHighlight;
            colors.pressedColor = primary ? BattleUiTheme.ButtonPrimaryPressed : BattleUiTheme.ButtonSecondaryPressed;
            colors.disabledColor = BattleUiTheme.ButtonDisabled;
            button.colors = colors;

            Text text = CreateAbsoluteText(buttonObject.transform, Vector2.zero, Vector2.zero, label, 16, FontStyle.Bold, TextAnchor.MiddleCenter, primary ? BattleUiTheme.ButtonText : BattleUiTheme.ButtonSecondaryText);
            EnableBestFit(text, 12, 16, false);
            SetOverflow(text, TextOverflowModes.Truncate, false);
            return button;
        }

        public static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        public static void CreateSectionHeader(Transform parent, string text)
        {
            Text header = CreateText(parent, text, 14, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold, BattleTextRole.SingleLineTitle);
            LayoutElement layout = header.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = 18f;
                layout.minHeight = 18f;
            }
        }

        public static Text CreateText(Transform parent, string content, int size, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = RuntimeSpriteLibrary.GetUiTmpFont(size, fontStyle);
            text.fontSize = size;
            text.fontStyle = ToTmpFontStyle(fontStyle);
            text.alignment = ToTmpAlignment(alignment);
            text.color = color;
            text.enableWordWrapping = true;
            text.lineSpacing = 0f;
            text.margin = Vector4.zero;
            text.richText = false;
            SetOverflow(text, TextOverflowModes.Overflow, true);

            if (size >= 14)
            {
                Shadow shadow = textObject.AddComponent<Shadow>();
                shadow.effectDistance = new Vector2(0.15f, -0.15f);
                shadow.effectColor = new Color(0f, 0f, 0f, 0.2f);
            }

            textObject.GetComponent<LayoutElement>().preferredHeight = size + 8f;
            return text;
        }

        public static Text CreateText(
            Transform parent,
            string content,
            int size,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color,
            BattleTextRole role)
        {
            Text text = CreateText(parent, content, size, fontStyle, alignment, color);
            ApplyTextRole(text, role);
            return text;
        }

        public static Text CreateAbsoluteText(
            Transform parent,
            Vector2 offsetMin,
            Vector2 offsetMax,
            string content,
            int size,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = RuntimeSpriteLibrary.GetUiTmpFont(size, fontStyle);
            text.fontSize = size;
            text.fontStyle = ToTmpFontStyle(fontStyle);
            text.alignment = ToTmpAlignment(alignment);
            text.color = color;
            text.enableWordWrapping = true;
            text.lineSpacing = 0f;
            text.margin = Vector4.zero;
            text.richText = false;
            SetOverflow(text, TextOverflowModes.Overflow, true);

            if (size >= 14)
            {
                Shadow shadow = textObject.AddComponent<Shadow>();
                shadow.effectDistance = new Vector2(0.18f, -0.18f);
                shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            }
            return text;
        }

        public static Text CreateAbsoluteText(
            Transform parent,
            Vector2 offsetMin,
            Vector2 offsetMax,
            string content,
            int size,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color,
            BattleTextRole role)
        {
            Text text = CreateAbsoluteText(parent, offsetMin, offsetMax, content, size, fontStyle, alignment, color);
            ApplyTextRole(text, role);
            return text;
        }

        public static Transform CreateInsetContentRoot(Transform parent, float padding)
        {
            GameObject root = new GameObject("Content", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return root.transform;
        }

        public static void EnableBestFit(Text text, int minSize, int maxSize, bool resizeHeight)
        {
            if (text == null)
            {
                return;
            }

            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            if (resizeHeight)
            {
                ContentSizeFitter fitter = text.gameObject.GetComponent<ContentSizeFitter>();
                if (fitter == null)
                {
                    fitter = text.gameObject.AddComponent<ContentSizeFitter>();
                }

                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        public static void EnableAutoHeight(Text text, float minHeight = 0f)
        {
            if (text == null)
            {
                return;
            }

            LayoutElement layout = text.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = -1f;
                layout.minHeight = minHeight;
                layout.flexibleHeight = 0f;
            }

            text.enableWordWrapping = true;
            SetOverflow(text, TextOverflowModes.Overflow, true);

            ContentSizeFitter fitter = text.gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = text.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public static void ApplyTextRole(Text text, BattleTextRole role)
        {
            EnsureTextRoleBinding(text, role);
            BattleTextLayoutPolicy.Apply(text, role);
        }

        public static float RefreshTextRole(Text text, BattleTextRole role, float minHeight = 0f)
        {
            EnsureTextRoleBinding(text, role);
            return BattleTextLayoutPolicy.Refresh(text, role, minHeight);
        }

        public static void ApplyResponsiveTextScale(Transform root, float scale)
        {
            if (root == null)
            {
                return;
            }

            float safeScale = Mathf.Clamp(scale, 0.82f, 1f);
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                if (text == null)
                {
                    continue;
                }

                BattleTextScaleState scaleState = text.GetComponent<BattleTextScaleState>();
                if (scaleState == null)
                {
                    scaleState = text.gameObject.AddComponent<BattleTextScaleState>();
                }

                scaleState.Capture(text);
                scaleState.Apply(text, safeScale);

                BattleTextRoleBinding roleBinding = text.GetComponent<BattleTextRoleBinding>();
                if (roleBinding != null)
                {
                    BattleTextLayoutPolicy.Apply(text, roleBinding.Role);
                }
            }
        }

        public static float RefreshAutoHeight(Text text, float minHeight = 0f)
        {
            if (text == null)
            {
                return minHeight;
            }

            LayoutElement layout = text.GetComponent<LayoutElement>();
            if (layout == null)
            {
                return minHeight;
            }

            layout.preferredHeight = -1f;
            Canvas.ForceUpdateCanvases();
            float availableWidth = text.rectTransform.rect.width;
            if (availableWidth <= 1f)
            {
                availableWidth = 600f;
            }

            float measuredHeight = Mathf.Ceil(text.GetPreferredValues(text.text, availableWidth, 0f).y);
            float finalHeight = Mathf.Max(minHeight, measuredHeight);
            layout.preferredHeight = finalHeight;
            layout.minHeight = minHeight;
            layout.flexibleHeight = 0f;
            return finalHeight;
        }

        public static void CreateStretchUiBar(Transform parent, out Image fill)
        {
            GameObject background = new GameObject("BarBackground", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(parent, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
            backgroundImage.color = new Color(0.13f, 0.13f, 0.14f, 0.96f);

            GameObject fillObject = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(background.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill = fillObject.GetComponent<Image>();
            fill.sprite = RuntimeSpriteLibrary.WhiteSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
        }

        public static GameObject CreateAdaptiveChip(Transform parent, HudChipModel chip, float preferredHeight)
        {
            GameObject root = CreateInsetPanel("AdaptiveChip", parent, preferredHeight, chip.BackgroundColor);
            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.preferredWidth = Mathf.Clamp(40f + chip.Text.Length * 7f, 56f, 220f);
            Text text = CreateAbsoluteText(root.transform, Vector2.zero, Vector2.zero, chip.Text, 11, FontStyle.Bold, TextAnchor.MiddleCenter, chip.TextColor);
            EnableBestFit(text, 9, 11, false);
            SetOverflow(text, TextOverflowModes.Truncate, false);
            return root;
        }

        public static GameObject CreateFactCard(Transform parent, HudFactModel fact, float preferredWidth, float preferredHeight)
        {
            GameObject root = CreateInsetPanel("FactCard", parent, preferredHeight, new Color(0.15f, 0.13f, 0.1f, 0.94f));
            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            Image accent = new GameObject("Accent", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            accent.transform.SetParent(root.transform, false);
            RectTransform accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.sizeDelta = new Vector2(4f, 0f);
            accentRect.anchoredPosition = Vector2.zero;
            accent.sprite = RuntimeSpriteLibrary.WhiteSprite;
            accent.color = fact.AccentColor;

            Text label = CreateAbsoluteText(root.transform, new Vector2(12f, 3f), new Vector2(-12f, -12f), fact.Label, 10, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextMuted);
            Text value = CreateAbsoluteText(root.transform, new Vector2(12f, 11f), new Vector2(-12f, -3f), fact.Value, 12, FontStyle.Bold, TextAnchor.LowerRight, BattleUiTheme.TextPrimary);
            EnableBestFit(label, 8, 10, false);
            EnableBestFit(value, 10, 12, false);
            SetOverflow(label, TextOverflowModes.Truncate, true);
            SetOverflow(value, TextOverflowModes.Truncate, true);
            return root;
        }

        public static TabButtonView CreateTabButton(Transform parent, string label)
        {
            GameObject root = new GameObject(label + "Tab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            root.GetComponent<LayoutElement>().preferredHeight = 28f;
            Image background = root.GetComponent<Image>();
            background.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            background.color = BattleUiTheme.TabIdle;
            background.type = Image.Type.Sliced;
            Button button = root.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.04f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.08f);
            colors.disabledColor = new Color(0.7f, 0.7f, 0.72f, 0.8f);
            button.colors = colors;
            Outline outline = root.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = BattleUiTheme.DividerSoft;
            Text text = CreateAbsoluteText(root.transform, Vector2.zero, Vector2.zero, label, 12, FontStyle.Bold, TextAnchor.MiddleCenter, BattleUiTheme.TabIdleText);
            return new TabButtonView(button, background, outline, text);
        }

        public static void DestroyChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int index = root.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.Destroy(root.GetChild(index).gameObject);
            }
        }

        public static void SetOverflow(Text text, TextOverflowModes overflowMode, bool enableWordWrapping)
        {
            if (text == null)
            {
                return;
            }

            text.enableWordWrapping = enableWordWrapping;
            text.overflowMode = overflowMode;
        }

        private static FontStyles ToTmpFontStyle(FontStyle fontStyle)
        {
            switch (fontStyle)
            {
                case FontStyle.Bold:
                    return FontStyles.Bold;
                case FontStyle.Italic:
                    return FontStyles.Italic;
                case FontStyle.BoldAndItalic:
                    return FontStyles.Bold | FontStyles.Italic;
                default:
                    return FontStyles.Normal;
            }
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleCenter:
                    return TextAlignmentOptions.Midline;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                default:
                    return TextAlignmentOptions.TopLeft;
            }
        }

        private static void EnsureTextRoleBinding(Text text, BattleTextRole role)
        {
            if (text == null)
            {
                return;
            }

            BattleTextRoleBinding binding = text.GetComponent<BattleTextRoleBinding>();
            if (binding == null)
            {
                binding = text.gameObject.AddComponent<BattleTextRoleBinding>();
            }

            binding.Role = role;
        }
    }

    internal sealed class BattleTextRoleBinding : MonoBehaviour
    {
        public BattleTextRole Role;
    }

    internal sealed class BattleTextScaleState : MonoBehaviour
    {
        private bool captured;
        private float baseFontSize;
        private float baseFontSizeMin;
        private float baseFontSizeMax;
        private bool autoSizing;

        public void Capture(Text text)
        {
            if (captured || text == null)
            {
                return;
            }

            baseFontSize = text.fontSize;
            baseFontSizeMin = text.fontSizeMin;
            baseFontSizeMax = text.fontSizeMax;
            autoSizing = text.enableAutoSizing;
            captured = true;
        }

        public void Apply(Text text, float scale)
        {
            if (!captured || text == null)
            {
                return;
            }

            float safeScale = Mathf.Clamp(scale, 0.82f, 1f);
            text.fontSize = Mathf.Max(8f, Mathf.Round(baseFontSize * safeScale));
            if (!autoSizing)
            {
                return;
            }

            text.fontSizeMin = Mathf.Max(7f, Mathf.Round(baseFontSizeMin * safeScale));
            text.fontSizeMax = Mathf.Max(text.fontSizeMin, Mathf.Round(baseFontSizeMax * safeScale));
        }
    }

    internal sealed class TabButtonView
    {
        public TabButtonView(Button button, Image background, Outline outline, Text label)
        {
            Button = button;
            Background = background;
            Outline = outline;
            Label = label;
        }

        public Button Button { get; }

        public Image Background { get; }

        public Outline Outline { get; }

        public Text Label { get; }
    }

    internal sealed class TagChipView
    {
        public TagChipView(GameObject root, Image background, Text label)
        {
            Root = root;
            Background = background;
            Label = label;
        }

        public GameObject Root { get; }

        public Image Background { get; }

        public Text Label { get; }
    }

    internal sealed class RosterEntryView
    {
        public RosterEntryView(
            GameObject root,
            Button button,
            Image background,
            Outline outline,
            Image accent,
            Text nameLabel,
            Text roleLabel,
            Text positionLabel,
            TagChipView primaryTagView,
            TagChipView secondaryTagView,
            Text hpLabel,
            Image hpFill)
        {
            Root = root;
            Button = button;
            Background = background;
            Outline = outline;
            Accent = accent;
            NameLabel = nameLabel;
            RoleLabel = roleLabel;
            PositionLabel = positionLabel;
            PrimaryTagView = primaryTagView;
            SecondaryTagView = secondaryTagView;
            HpLabel = hpLabel;
            HpFill = hpFill;
        }

        public string UnitId { get; set; }

        public GameObject Root { get; }

        public Button Button { get; }

        public Image Background { get; }

        public Outline Outline { get; }

        public Image Accent { get; }

        public Text NameLabel { get; }

        public Text RoleLabel { get; }

        public Text PositionLabel { get; }

        public TagChipView PrimaryTagView { get; }

        public TagChipView SecondaryTagView { get; }

        public Text HpLabel { get; }

        public Image HpFill { get; }
    }
}
