using System;
using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Localization;
using PhalanxChronicle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace PhalanxChronicle.UI
{
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
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
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

                if (!string.IsNullOrWhiteSpace(interludeModel.RewardLabel))
                {
                    CreateRewardRow(narrativeRoot, interludeModel.RewardIconItemId, interludeModel.RewardLabel, true);
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

                if (!string.IsNullOrWhiteSpace(interludeModel.RewardLabel))
                {
                    panelHeight += 44f;
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
            ClampText(title, 26f, TextOverflowModes.Truncate);
            BattleHudFactory.EnableBestFit(title, 14, 19, false);
            Text status = BattleHudFactory.CreateText(titleRow.transform, model.Status, 12, FontStyle.Bold, TextAnchor.MiddleRight, model.IsUnlocked ? BattleUiTheme.TextGold : BattleUiTheme.TextMuted);
            LayoutElement statusLayout = status.GetComponent<LayoutElement>();
            statusLayout.minWidth = 96f;
            statusLayout.preferredWidth = 132f;
            statusLayout.flexibleWidth = 0f;
            ClampText(status, 18f, TextOverflowModes.Truncate);

            if (!string.IsNullOrWhiteSpace(model.Description))
            {
                Text description = BattleHudFactory.CreateText(content, model.Description, 14, FontStyle.Normal, TextAnchor.UpperLeft, model.IsUnlocked ? BattleUiTheme.TextSecondary : BattleUiTheme.TextMuted);
                ClampText(description, 34f, TextOverflowModes.Truncate);
            }

            string metrics = string.Join("  ", new[] { model.BattlefieldLabel, model.DurationLabel }.Where(line => !string.IsNullOrWhiteSpace(line)));
            if (!string.IsNullOrWhiteSpace(metrics))
            {
                Text metricLabel = BattleHudFactory.CreateText(content, metrics, 12, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
                ClampText(metricLabel, 18f, TextOverflowModes.Truncate);
            }

            if (!string.IsNullOrWhiteSpace(model.RewardLabel))
            {
                CreateRewardRow(content, model.RewardIconItemId, model.RewardLabel, true);
            }

            if (!string.IsNullOrWhiteSpace(model.RecommendedReason))
            {
                Text reason = BattleHudFactory.CreateText(content, model.RecommendedReason, 13, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.86f, 0.91f, 0.98f, 1f));
                ClampText(reason, 24f, TextOverflowModes.Truncate);
            }
        }

        private void CreateOptionEntry(Transform parent, CampaignOptionEntryModel model)
        {
            GameObject root = BattleHudFactory.CreateInsetPanel("CampaignOptionEntry", parent, 0f, model.IsEnabled ? (model.IsEmphasized ? BattleUiTheme.PanelCommand : BattleUiTheme.PanelInset) : BattleUiTheme.PanelGhost);
            LayoutElement rootLayout = root.GetComponent<LayoutElement>();
            bool hasSupportingLine = !string.IsNullOrWhiteSpace(model.RecommendedReason) || !string.IsNullOrWhiteSpace(model.AvailabilityReason);
            rootLayout.preferredHeight = hasSupportingLine || model.IsPromotionOption ? 124f : 98f;
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
            PopulateBadgeContent(glyphPanel.transform, model.IconItemId, model.IconGlyph);

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
            ClampText(title, 26f, TextOverflowModes.Truncate);
            BattleHudFactory.EnableBestFit(title, 14, model.IsPromotionOption ? 21 : 18, false);
            Text status = BattleHudFactory.CreateText(titleRow.transform, model.Status, 12, FontStyle.Bold, TextAnchor.MiddleRight, model.IsEnabled ? BattleUiTheme.TextGold : BattleUiTheme.TextMuted);
            LayoutElement statusLayout = status.GetComponent<LayoutElement>();
            statusLayout.minWidth = 96f;
            statusLayout.preferredWidth = 132f;
            statusLayout.flexibleWidth = 0f;
            ClampText(status, 18f, TextOverflowModes.Truncate);

            if (!string.IsNullOrWhiteSpace(model.MetricLine))
            {
                Text metric = BattleHudFactory.CreateText(textColumn.transform, model.MetricLine, 12, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextSecondary);
                ClampText(metric, 16f, TextOverflowModes.Truncate);
            }

            Text description = BattleHudFactory.CreateText(textColumn.transform, model.Description, model.IsPromotionOption ? 15 : 14, FontStyle.Normal, TextAnchor.UpperLeft, model.IsEnabled ? BattleUiTheme.TextPrimary : new Color(0.62f, 0.59f, 0.54f, 1f));
            ClampText(description, 36f, TextOverflowModes.Truncate);

            if (!string.IsNullOrWhiteSpace(model.RecommendedReason))
            {
                Text reason = BattleHudFactory.CreateText(textColumn.transform, model.RecommendedReason, 13, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.82f, 0.91f, 0.99f, 1f));
                ClampText(reason, 18f, TextOverflowModes.Truncate);
            }

            if (!string.IsNullOrWhiteSpace(model.AvailabilityReason))
            {
                Text availability = BattleHudFactory.CreateText(textColumn.transform, model.AvailabilityReason, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextWarning);
                ClampText(availability, 18f, TextOverflowModes.Truncate);
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

        private static void ClampText(Text text, float preferredHeight, TextOverflowModes overflowMode)
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

            BattleHudFactory.SetOverflow(text, overflowMode, false);
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

        private static void PopulateBadgeContent(Transform parent, string iconItemId, string iconGlyph)
        {
            Sprite iconSprite = RuntimeSpriteLibrary.GetItemIcon(iconItemId);
            if (iconSprite != null)
            {
                GameObject imageRoot = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
                imageRoot.transform.SetParent(parent, false);
                RectTransform rect = imageRoot.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(8f, 8f);
                rect.offsetMax = new Vector2(-8f, -8f);
                Image image = imageRoot.GetComponent<Image>();
                image.sprite = iconSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
                return;
            }

            Text glyph = BattleHudFactory.CreateAbsoluteText(parent, Vector2.zero, Vector2.zero, iconGlyph, 22, FontStyle.Bold, TextAnchor.MiddleCenter, BattleUiTheme.TextGold);
            BattleHudFactory.EnableBestFit(glyph, 14, 22, false);
            BattleHudFactory.SetOverflow(glyph, TextOverflowModes.Truncate, false);
        }

        private static GameObject CreateRewardRow(Transform parent, string iconItemId, string label, bool emphasized)
        {
            GameObject row = new GameObject("RewardRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = 44f;
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            GameObject badgePanel = BattleHudFactory.CreateInsetPanel("RewardBadge", row.transform, 40f, emphasized ? BattleUiTheme.PanelReward : BattleUiTheme.PanelGhost);
            LayoutElement badgeLayout = badgePanel.GetComponent<LayoutElement>();
            badgeLayout.preferredWidth = 40f;
            badgeLayout.preferredHeight = 40f;
            badgeLayout.flexibleWidth = 0f;
            badgeLayout.flexibleHeight = 0f;
            PopulateBadgeContent(badgePanel.transform, iconItemId, "賞");

            Text rewardLabel = BattleHudFactory.CreateText(row.transform, label, 13, FontStyle.Bold, TextAnchor.MiddleLeft, emphasized ? BattleUiTheme.TextGold : BattleUiTheme.TextSecondary);
            LayoutElement labelLayout = rewardLabel.GetComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            ClampText(rewardLabel, 24f, TextOverflowModes.Truncate);
            return row;
        }
    }
}
