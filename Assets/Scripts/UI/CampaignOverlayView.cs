using System;
using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Core;
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

        public void ShowCampaignEquipment(CampaignEquipmentDeckModel model, Action<string> onOptionSelected, Action onPrimary, Action onSecondary = null)
        {
            stageSelectionHandler = null;
            optionSelectionHandler = onOptionSelected;
            primaryHandler = onPrimary;
            secondaryHandler = onSecondary;

            CampaignEquipmentDeckModel equipmentModel = model ?? new CampaignEquipmentDeckModel();
            ApplyFrame(
                equipmentModel.Eyebrow,
                equipmentModel.Title,
                equipmentModel.Body,
                equipmentModel.ProgressLabel,
                equipmentModel.HighlightLabel,
                equipmentModel.DeckTitle,
                string.IsNullOrWhiteSpace(equipmentModel.PreviewMessage)
                    ? LocalizationService.Text("campaign.deck.equipment_body", "先選部位，再從下方名單替換裝備。")
                    : equipmentModel.PreviewMessage);
            RebuildContent(root =>
            {
                IReadOnlyList<CampaignOptionEntryModel> promotionOptions = equipmentModel.PromotionOptions ?? Array.Empty<CampaignOptionEntryModel>();
                if (promotionOptions.Count > 0)
                {
                    string currentSection = null;
                    foreach (CampaignOptionEntryModel entry in promotionOptions
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
                }

                if ((equipmentModel.SlotCards ?? Array.Empty<CampaignEquipmentSlotCardModel>()).Count > 0)
                {
                    CreateListSectionHeader(root, LocalizationService.Text("camp.equip.section.slots", "裝備槽位"));
                    CreateEquipmentSlotRow(root, equipmentModel.SlotCards);
                }

                bool hasChoices = false;
                foreach (CampaignEquipmentChoiceSectionModel section in (equipmentModel.ChoiceSections ?? Array.Empty<CampaignEquipmentChoiceSectionModel>()))
                {
                    IReadOnlyList<CampaignEquipmentChoiceModel> choices = section?.Choices ?? Array.Empty<CampaignEquipmentChoiceModel>();
                    if (choices.Count == 0)
                    {
                        continue;
                    }

                    hasChoices = true;
                    CreateListSectionHeader(root, section.Title);
                    foreach (CampaignEquipmentChoiceModel choice in choices
                        .OrderBy(entry => entry.SortWeight)
                        .ThenBy(entry => entry.ItemName, StringComparer.Ordinal))
                    {
                        CreateEquipmentChoiceEntry(root, choice);
                    }
                }

                if (!hasChoices)
                {
                    CreateEmptyStatePanel(
                        root,
                        LocalizationService.Text("camp.equip.empty.title", "目前沒有可替換裝備"),
                        LocalizationService.Text("camp.equip.empty.desc", "這個部位沒有其他可直接換上的裝備，也沒有可轉裝的對象。"));
                }
            });
            ConfigureButtons(equipmentModel.PrimaryActionLabel, equipmentModel.SecondaryActionLabel);
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
            if (model.PromotionPreview != null)
            {
                CreatePromotionPreviewEntry(parent, model);
                return;
            }

            if (model.PromotionComparisons != null && model.PromotionComparisons.Count > 0)
            {
                CreatePromotionComparisonEntry(parent, model);
                return;
            }

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

        private void CreatePromotionPreviewEntry(Transform parent, CampaignOptionEntryModel model)
        {
            GameObject root = BattleHudFactory.CreateInsetPanel("PromotionPreviewEntry", parent, 0f, model.IsEmphasized ? BattleUiTheme.PanelInsetStrong : BattleUiTheme.PanelInset);
            LayoutElement rootLayout = root.GetComponent<LayoutElement>();
            rootLayout.flexibleHeight = 0f;

            Transform content = BattleHudFactory.CreateInsetContentRoot(root.transform, 16f);
            VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;

            GameObject headerStack = new GameObject("PromotionPreviewHeaderStack", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            headerStack.transform.SetParent(content, false);
            LayoutElement headerStackLayout = headerStack.GetComponent<LayoutElement>();
            headerStackLayout.flexibleHeight = 0f;
            VerticalLayoutGroup headerStackGroup = headerStack.GetComponent<VerticalLayoutGroup>();
            headerStackGroup.spacing = 8f;
            headerStackGroup.childControlHeight = true;
            headerStackGroup.childControlWidth = true;
            headerStackGroup.childForceExpandHeight = false;
            headerStackGroup.childForceExpandWidth = true;

            GameObject headerRow = new GameObject("PromotionPreviewHeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            headerRow.transform.SetParent(headerStack.transform, false);
            headerRow.GetComponent<LayoutElement>().preferredHeight = 40f;
            HorizontalLayoutGroup headerLayout = headerRow.GetComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 10f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlHeight = true;
            headerLayout.childControlWidth = true;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childForceExpandWidth = false;

            GameObject badgePanel = BattleHudFactory.CreateInsetPanel("PromotionPreviewBadge", headerRow.transform, 40f, BattleUiTheme.PanelReward);
            LayoutElement badgeLayout = badgePanel.GetComponent<LayoutElement>();
            badgeLayout.preferredWidth = 40f;
            badgeLayout.preferredHeight = 40f;
            badgeLayout.flexibleWidth = 0f;
            badgeLayout.flexibleHeight = 0f;
            PopulateBadgeContent(badgePanel.transform, model.IconItemId, string.IsNullOrWhiteSpace(model.IconGlyph) ? "階" : model.IconGlyph);

            Text title = BattleHudFactory.CreateText(headerRow.transform, model.Title, 20, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            LayoutElement titleLayout = title.GetComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;
            ClampText(title, 26f, TextOverflowModes.Truncate);

            Text stageBadge = BattleHudFactory.CreateText(headerRow.transform, model.PromotionPreview.CurrentStageLabel, 12, FontStyle.Bold, TextAnchor.MiddleRight, BattleUiTheme.TextGold);
            LayoutElement stageLayout = stageBadge.GetComponent<LayoutElement>();
            stageLayout.minWidth = 68f;
            stageLayout.preferredWidth = 84f;
            stageLayout.flexibleWidth = 0f;
            ClampText(stageBadge, 20f, TextOverflowModes.Truncate);

            GameObject actionRow = new GameObject("PromotionPreviewActionRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            actionRow.transform.SetParent(headerStack.transform, false);
            actionRow.GetComponent<LayoutElement>().preferredHeight = 40f;
            HorizontalLayoutGroup actionLayout = actionRow.GetComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 10f;
            actionLayout.childAlignment = TextAnchor.MiddleRight;
            actionLayout.childControlHeight = true;
            actionLayout.childControlWidth = true;
            actionLayout.childForceExpandHeight = false;
            actionLayout.childForceExpandWidth = false;

            GameObject actionSpacer = new GameObject("PromotionPreviewActionSpacer", typeof(RectTransform), typeof(LayoutElement));
            actionSpacer.transform.SetParent(actionRow.transform, false);
            LayoutElement actionSpacerLayout = actionSpacer.GetComponent<LayoutElement>();
            actionSpacerLayout.flexibleWidth = 1f;
            actionSpacerLayout.flexibleHeight = 0f;

            Button toggleButton = BattleHudFactory.CreateButton(actionRow.transform, model.PromotionPreview.ToggleLabel, false);
            LayoutElement toggleLayout = toggleButton.GetComponent<LayoutElement>();
            toggleLayout.minWidth = 156f;
            toggleLayout.preferredWidth = 176f;
            toggleLayout.flexibleWidth = 0f;
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(() => optionSelectionHandler?.Invoke(model.OptionId));

            if (!string.IsNullOrWhiteSpace(model.PromotionPreview.NextStageLabel))
            {
                Text nextStage = BattleHudFactory.CreateText(content, model.PromotionPreview.NextStageLabel, 13, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
                BattleHudFactory.EnableAutoHeight(nextStage, 18f);
            }

            if (!string.IsNullOrWhiteSpace(model.PromotionPreview.PrimarySummary))
            {
                Text primarySummary = BattleHudFactory.CreateText(content, model.PromotionPreview.PrimarySummary, 14, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
                BattleHudFactory.EnableAutoHeight(primarySummary, 22f);
            }

            if (!string.IsNullOrWhiteSpace(model.PromotionPreview.SecondarySummary))
            {
                Text secondarySummary = BattleHudFactory.CreateText(content, model.PromotionPreview.SecondarySummary, 12, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.82f, 0.91f, 0.99f, 1f));
                BattleHudFactory.EnableAutoHeight(secondarySummary, 18f);
            }

            if (model.IsStageIntroExpanded && model.StageIntro != null && model.StageIntro.Count > 0)
            {
                GameObject introContainer = new GameObject("PromotionStageIntroContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
                introContainer.transform.SetParent(content, false);
                LayoutElement introLayout = introContainer.GetComponent<LayoutElement>();
                introLayout.flexibleHeight = 0f;
                VerticalLayoutGroup introColumn = introContainer.GetComponent<VerticalLayoutGroup>();
                introColumn.spacing = 8f;
                introColumn.childControlHeight = true;
                introColumn.childControlWidth = true;
                introColumn.childForceExpandHeight = false;

                foreach (ProgressionStageIntroModel stage in model.StageIntro)
                {
                    CreateProgressionStageCard(introContainer.transform, stage);
                }
            }

            FinalizeDynamicEntryHeight(root, 148f);
        }

        private void CreatePromotionComparisonEntry(Transform parent, CampaignOptionEntryModel model)
        {
            GameObject root = BattleHudFactory.CreateInsetPanel(
                "PromotionComparisonEntry",
                parent,
                0f,
                model.IsEnabled ? (model.IsEmphasized ? BattleUiTheme.PanelCommand : BattleUiTheme.PanelInsetStrong) : BattleUiTheme.PanelGhost);
            LayoutElement rootLayout = root.GetComponent<LayoutElement>();
            rootLayout.flexibleHeight = 0f;

            Button button = root.AddComponent<Button>();
            button.interactable = model.IsEnabled && !string.IsNullOrWhiteSpace(model.OptionId);
            button.onClick.AddListener(() => optionSelectionHandler?.Invoke(model.OptionId));
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.04f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.08f);
            colors.disabledColor = new Color(0.7f, 0.7f, 0.72f, 0.8f);
            button.colors = colors;

            Transform content = BattleHudFactory.CreateInsetContentRoot(root.transform, 16f);
            HorizontalLayoutGroup contentLayout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = 12f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childAlignment = TextAnchor.UpperLeft;

            GameObject badgePanel = BattleHudFactory.CreateInsetPanel("PromotionComparisonBadge", content.transform, 60f, BattleUiTheme.PanelReward);
            LayoutElement badgeLayout = badgePanel.GetComponent<LayoutElement>();
            badgeLayout.preferredWidth = 60f;
            badgeLayout.preferredHeight = 60f;
            badgeLayout.flexibleWidth = 0f;
            badgeLayout.flexibleHeight = 0f;
            PopulateBadgeContent(badgePanel.transform, model.IconItemId, string.IsNullOrWhiteSpace(model.IconGlyph) ? "進" : model.IconGlyph);

            GameObject textColumn = new GameObject("PromotionComparisonTextColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textColumn.transform.SetParent(content, false);
            LayoutElement columnLayout = textColumn.GetComponent<LayoutElement>();
            columnLayout.flexibleWidth = 1f;
            columnLayout.flexibleHeight = 0f;
            VerticalLayoutGroup columnGroup = textColumn.GetComponent<VerticalLayoutGroup>();
            columnGroup.spacing = 6f;
            columnGroup.childControlHeight = true;
            columnGroup.childControlWidth = true;
            columnGroup.childForceExpandHeight = false;

            GameObject titleRow = new GameObject("PromotionComparisonTitleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            titleRow.transform.SetParent(textColumn.transform, false);
            titleRow.GetComponent<LayoutElement>().preferredHeight = 28f;
            HorizontalLayoutGroup titleLayout = titleRow.GetComponent<HorizontalLayoutGroup>();
            titleLayout.spacing = 8f;
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.childControlHeight = true;
            titleLayout.childControlWidth = true;
            titleLayout.childForceExpandHeight = false;
            titleLayout.childForceExpandWidth = false;

            Text title = BattleHudFactory.CreateText(titleRow.transform, model.Title, 19, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            LayoutElement titleLayoutElement = title.GetComponent<LayoutElement>();
            titleLayoutElement.flexibleWidth = 1f;
            ClampText(title, 24f, TextOverflowModes.Truncate);

            Text status = BattleHudFactory.CreateText(titleRow.transform, model.Status, 12, FontStyle.Bold, TextAnchor.MiddleRight, model.IsEnabled ? BattleUiTheme.TextGold : BattleUiTheme.TextMuted);
            LayoutElement statusLayout = status.GetComponent<LayoutElement>();
            statusLayout.preferredWidth = 108f;
            statusLayout.flexibleWidth = 0f;
            ClampText(status, 18f, TextOverflowModes.Truncate);

            PromotionComparisonModel comparison = model.PromotionComparisons[0];
            if (!string.IsNullOrWhiteSpace(comparison.StatDeltaLabel))
            {
                Text statDelta = BattleHudFactory.CreateText(textColumn.transform, comparison.StatDeltaLabel, 12, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
                BattleHudFactory.EnableAutoHeight(statDelta, 18f);
            }

            if (!string.IsNullOrWhiteSpace(model.Description))
            {
                Text description = BattleHudFactory.CreateText(textColumn.transform, model.Description, 13, FontStyle.Normal, TextAnchor.UpperLeft, model.IsEnabled ? BattleUiTheme.TextPrimary : BattleUiTheme.TextMuted);
                BattleHudFactory.EnableAutoHeight(description, 20f);
            }

            CreateSkillComparisonBlock(
                textColumn.transform,
                LocalizationService.Text("ui.label.passive", "被動戰法"),
                comparison.PassiveCurrentName,
                comparison.PassiveTargetName,
                comparison.PassiveChangeLabel,
                comparison.PassiveDetail,
                model.IsEnabled);
            CreateSkillComparisonBlock(
                textColumn.transform,
                LocalizationService.Text("ui.label.active", "主動戰技"),
                comparison.ActiveCurrentName,
                comparison.ActiveTargetName,
                comparison.ActiveChangeLabel,
                comparison.ActiveDetail,
                model.IsEnabled);

            if (!string.IsNullOrWhiteSpace(comparison.MasteryPreview))
            {
                Text masteryPreview = BattleHudFactory.CreateText(textColumn.transform, comparison.MasteryPreview, 12, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.82f, 0.91f, 0.99f, 1f));
                BattleHudFactory.EnableAutoHeight(masteryPreview, 18f);
            }

            if (!string.IsNullOrWhiteSpace(model.AvailabilityReason))
            {
                Text availability = BattleHudFactory.CreateText(textColumn.transform, model.AvailabilityReason, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextWarning);
                BattleHudFactory.EnableAutoHeight(availability, 18f);
            }

            FinalizeDynamicEntryHeight(root, 186f);
        }

        private void CreateProgressionStageCard(Transform parent, ProgressionStageIntroModel model)
        {
            GameObject card = BattleHudFactory.CreateInsetPanel("ProgressionStageCard", parent, 0f, model.IsReached ? BattleUiTheme.PanelBackdrop : BattleUiTheme.PanelGhost);
            LayoutElement cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.flexibleHeight = 0f;
            Transform content = BattleHudFactory.CreateInsetContentRoot(card.transform, 12f);
            VerticalLayoutGroup column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 4f;
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;

            GameObject titleRow = new GameObject("ProgressionStageTitleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            titleRow.transform.SetParent(content, false);
            titleRow.GetComponent<LayoutElement>().preferredHeight = 24f;
            HorizontalLayoutGroup titleLayout = titleRow.GetComponent<HorizontalLayoutGroup>();
            titleLayout.spacing = 8f;
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.childControlHeight = true;
            titleLayout.childControlWidth = true;
            titleLayout.childForceExpandHeight = false;
            titleLayout.childForceExpandWidth = false;

            Text title = BattleHudFactory.CreateText(titleRow.transform, model.Title, 14, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            LayoutElement titleElement = title.GetComponent<LayoutElement>();
            titleElement.flexibleWidth = 1f;
            ClampText(title, 20f, TextOverflowModes.Truncate);

            Text badge = BattleHudFactory.CreateText(titleRow.transform, model.StatusBadge, 11, FontStyle.Bold, TextAnchor.MiddleRight, model.IsReached ? BattleUiTheme.TextGold : BattleUiTheme.TextMuted);
            LayoutElement badgeElement = badge.GetComponent<LayoutElement>();
            badgeElement.preferredWidth = 92f;
            badgeElement.flexibleWidth = 0f;
            ClampText(badge, 18f, TextOverflowModes.Truncate);

            Text range = BattleHudFactory.CreateText(content, model.LevelRangeLabel, 12, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextSecondary);
            ClampText(range, 18f, TextOverflowModes.Truncate);

            Text summary = BattleHudFactory.CreateText(content, model.Summary, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            BattleHudFactory.EnableAutoHeight(summary, 18f);

            Text unlocks = BattleHudFactory.CreateText(content, model.UnlocksLabel, 12, FontStyle.Italic, TextAnchor.UpperLeft, model.IsReached ? BattleUiTheme.TextSecondary : BattleUiTheme.TextMuted);
            BattleHudFactory.EnableAutoHeight(unlocks, 18f);

            FinalizeDynamicEntryHeight(card, 92f);
        }

        private static void CreateSkillComparisonBlock(Transform parent, string label, string currentValue, string targetValue, string changeLabel, string detail, bool enabled)
        {
            GameObject block = new GameObject("SkillComparisonBlock", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            block.transform.SetParent(parent, false);
            LayoutElement blockLayout = block.GetComponent<LayoutElement>();
            blockLayout.flexibleHeight = 0f;
            VerticalLayoutGroup blockGroup = block.GetComponent<VerticalLayoutGroup>();
            blockGroup.spacing = 2f;
            blockGroup.childControlHeight = true;
            blockGroup.childControlWidth = true;
            blockGroup.childForceExpandHeight = false;

            GameObject headerRow = new GameObject("SkillComparisonHeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            headerRow.transform.SetParent(block.transform, false);
            headerRow.GetComponent<LayoutElement>().preferredHeight = 18f;
            HorizontalLayoutGroup headerLayout = headerRow.GetComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlHeight = true;
            headerLayout.childControlWidth = true;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childForceExpandWidth = false;

            Text header = BattleHudFactory.CreateText(headerRow.transform, label, 12, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            LayoutElement headerLayoutElement = header.GetComponent<LayoutElement>();
            headerLayoutElement.flexibleWidth = 1f;
            ClampText(header, 18f, TextOverflowModes.Truncate);

            if (!string.IsNullOrWhiteSpace(changeLabel))
            {
                Text change = BattleHudFactory.CreateText(headerRow.transform, changeLabel, 11, FontStyle.Bold, TextAnchor.MiddleRight, BattleUiTheme.TextGold);
                LayoutElement changeLayout = change.GetComponent<LayoutElement>();
                changeLayout.preferredWidth = 54f;
                changeLayout.flexibleWidth = 0f;
                ClampText(change, 18f, TextOverflowModes.Truncate);
            }

            bool showTarget = !string.Equals(currentValue, targetValue, StringComparison.Ordinal) || !string.IsNullOrWhiteSpace(changeLabel);
            Text current = BattleHudFactory.CreateText(
                block.transform,
                showTarget
                    ? LocalizationService.Format("camp.promote.compare.current_line", "目前：{0}", currentValue)
                    : currentValue,
                12,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                enabled ? BattleUiTheme.TextPrimary : BattleUiTheme.TextMuted);
            BattleHudFactory.EnableAutoHeight(current, 18f);

            if (showTarget)
            {
                Text target = BattleHudFactory.CreateText(block.transform, LocalizationService.Format("camp.promote.compare.target_line", "升階後：{0}", targetValue), 12, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextGold);
                BattleHudFactory.EnableAutoHeight(target, 18f);
            }

            if (!string.IsNullOrWhiteSpace(detail))
            {
                Text detailLabel = BattleHudFactory.CreateText(block.transform, detail, 11, FontStyle.Italic, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
                BattleHudFactory.EnableAutoHeight(detailLabel, 16f);
            }
        }

        private static void FinalizeDynamicEntryHeight(GameObject root, float minimumHeight)
        {
            if (root == null)
            {
                return;
            }

            LayoutElement layout = root.GetComponent<LayoutElement>();
            RectTransform rect = root.GetComponent<RectTransform>();
            if (layout == null || rect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            float preferredHeight = LayoutUtility.GetPreferredHeight(rect);
            RectTransform contentRect = root.transform
                .Cast<Transform>()
                .Select(child => child as RectTransform)
                .FirstOrDefault(child => child != null);

            if (contentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
                float contentPadding = Mathf.Abs(contentRect.offsetMin.y) + Mathf.Abs(contentRect.offsetMax.y);
                preferredHeight = Mathf.Max(preferredHeight, LayoutUtility.GetPreferredHeight(contentRect) + contentPadding);
            }

            layout.preferredHeight = Mathf.Max(minimumHeight, preferredHeight + 4f);
        }

        private void CreateEquipmentSlotRow(Transform parent, IReadOnlyList<CampaignEquipmentSlotCardModel> slotCards)
        {
            GameObject row = new GameObject("EquipmentSlotRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = 104f;
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            foreach (CampaignEquipmentSlotCardModel slotCard in slotCards ?? Array.Empty<CampaignEquipmentSlotCardModel>())
            {
                CreateEquipmentSlotCard(row.transform, slotCard);
            }
        }

        private void CreateEquipmentSlotCard(Transform parent, CampaignEquipmentSlotCardModel model)
        {
            Color panelColor = model != null && model.IsSelected
                ? BattleUiTheme.PanelSelected
                : BattleUiTheme.PanelInset;
            GameObject root = BattleHudFactory.CreateInsetPanel("EquipmentSlotCard", parent, 0f, panelColor);
            LayoutElement rootLayout = root.GetComponent<LayoutElement>();
            rootLayout.preferredHeight = 104f;
            rootLayout.flexibleWidth = 1f;
            rootLayout.flexibleHeight = 0f;
            Button button = root.AddComponent<Button>();
            button.interactable = model != null && !string.IsNullOrWhiteSpace(model.OptionId);
            button.onClick.AddListener(() => optionSelectionHandler?.Invoke(model.OptionId));
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.04f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.08f);
            button.colors = colors;

            Transform content = BattleHudFactory.CreateInsetContentRoot(root.transform, 12f);
            VerticalLayoutGroup column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 4f;
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;

            GameObject topRow = new GameObject("EquipmentSlotTopRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            topRow.transform.SetParent(content, false);
            topRow.GetComponent<LayoutElement>().preferredHeight = 20f;
            HorizontalLayoutGroup topLayout = topRow.GetComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 6f;
            topLayout.childAlignment = TextAnchor.MiddleLeft;
            topLayout.childControlHeight = true;
            topLayout.childControlWidth = true;
            topLayout.childForceExpandHeight = false;
            topLayout.childForceExpandWidth = false;

            Text slotLabel = BattleHudFactory.CreateText(topRow.transform, model.SlotLabel, 12, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            LayoutElement slotLayout = slotLabel.GetComponent<LayoutElement>();
            slotLayout.flexibleWidth = 1f;

            if (model.IsTreasure)
            {
                BattleHudFactory.CreateAdaptiveChip(topRow.transform, new HudChipModel
                {
                    Text = LocalizationService.Text("camp.equip.badge.treasure", "寶物"),
                    BackgroundColor = BattleUiTheme.PanelReward,
                    TextColor = BattleUiTheme.TextGold,
                }, 20f);
            }

            Text itemName = BattleHudFactory.CreateText(
                content,
                model.IsEmpty ? LocalizationService.Text("camp.equip.empty_slot", "尚未裝備") : model.ItemName,
                17,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                model.IsEmpty ? BattleUiTheme.TextMuted : BattleUiTheme.TextPrimary);
            ClampText(itemName, 24f, TextOverflowModes.Truncate);

            Text summary = BattleHudFactory.CreateText(content, model.SummaryLine, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            ClampText(summary, 34f, TextOverflowModes.Truncate);
        }

        private void CreateEquipmentChoiceEntry(Transform parent, CampaignEquipmentChoiceModel model)
        {
            GameObject root = BattleHudFactory.CreateInsetPanel("EquipmentChoiceEntry", parent, 0f, GetEquipmentChoicePanelColor(model));
            LayoutElement rootLayout = root.GetComponent<LayoutElement>();
            bool hasHint = !string.IsNullOrWhiteSpace(model.HintLine);
            bool hasBadges = model.Badges != null && model.Badges.Count > 0;
            rootLayout.preferredHeight = hasHint || hasBadges ? 138f : 122f;
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

            GameObject iconPanel = BattleHudFactory.CreateInsetPanel("EquipmentChoiceIcon", content.transform, 60f, model.IsEmphasized ? BattleUiTheme.PanelReward : BattleUiTheme.PanelGhost);
            LayoutElement iconLayout = iconPanel.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = 60f;
            iconLayout.preferredHeight = 60f;
            iconLayout.flexibleWidth = 0f;
            iconLayout.flexibleHeight = 0f;
            string fallbackGlyph = string.IsNullOrWhiteSpace(model.ItemId)
                ? (model.IsEnabled ? "卸" : model.StateKind == EquipmentChoiceStateKind.Current ? "空" : "備")
                : model.StateKind == EquipmentChoiceStateKind.Current ? "裝" : "備";
            PopulateBadgeContent(iconPanel.transform, model.ItemId, fallbackGlyph);

            GameObject textColumn = new GameObject("EquipmentChoiceTextColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textColumn.transform.SetParent(content, false);
            LayoutElement columnLayout = textColumn.GetComponent<LayoutElement>();
            columnLayout.flexibleWidth = 1f;
            columnLayout.flexibleHeight = 0f;
            VerticalLayoutGroup textLayout = textColumn.GetComponent<VerticalLayoutGroup>();
            textLayout.spacing = 5f;
            textLayout.childControlHeight = true;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandHeight = false;

            Text title = BattleHudFactory.CreateText(textColumn.transform, model.ItemName, 18, FontStyle.Bold, TextAnchor.MiddleLeft, model.IsEnabled ? BattleUiTheme.TextPrimary : BattleUiTheme.TextMuted);
            ClampText(title, 24f, TextOverflowModes.Truncate);

            if (hasBadges)
            {
                GameObject badgeRow = new GameObject("EquipmentChoiceBadgeRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                badgeRow.transform.SetParent(textColumn.transform, false);
                badgeRow.GetComponent<LayoutElement>().preferredHeight = 24f;
                HorizontalLayoutGroup badgeLayout = badgeRow.GetComponent<HorizontalLayoutGroup>();
                badgeLayout.spacing = 6f;
                badgeLayout.childControlHeight = true;
                badgeLayout.childControlWidth = false;
                badgeLayout.childForceExpandHeight = false;
                badgeLayout.childForceExpandWidth = false;
                foreach (HudChipModel badge in model.Badges.Where(chip => chip != null && !string.IsNullOrWhiteSpace(chip.Text)).Take(4))
                {
                    BattleHudFactory.CreateAdaptiveChip(badgeRow.transform, badge, 22f);
                }
            }

            Text compare = BattleHudFactory.CreateText(textColumn.transform, model.CompareSummary, 12, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            ClampText(compare, 18f, TextOverflowModes.Truncate);

            Text effect = BattleHudFactory.CreateText(textColumn.transform, model.EffectSummary, 13, FontStyle.Normal, TextAnchor.UpperLeft, model.IsEnabled ? BattleUiTheme.TextSecondary : BattleUiTheme.TextMuted);
            ClampText(effect, 36f, TextOverflowModes.Truncate);

            if (hasHint)
            {
                Text hint = BattleHudFactory.CreateText(
                    textColumn.transform,
                    model.HintLine,
                    12,
                    FontStyle.Italic,
                    TextAnchor.UpperLeft,
                    model.StateKind == EquipmentChoiceStateKind.EquippedByOther ? BattleUiTheme.TextWarning : BattleUiTheme.TextSecondary);
                ClampText(hint, 18f, TextOverflowModes.Truncate);
            }
        }

        private static Color GetEquipmentChoicePanelColor(CampaignEquipmentChoiceModel model)
        {
            if (model == null)
            {
                return BattleUiTheme.PanelInset;
            }

            if (!model.IsEnabled)
            {
                return model.IsEmphasized ? BattleUiTheme.PanelCommand : BattleUiTheme.PanelGhost;
            }

            if (string.IsNullOrWhiteSpace(model.ItemId))
            {
                return BattleUiTheme.PanelGhost;
            }

            return model.IsEmphasized ? BattleUiTheme.PanelInsetStrong : BattleUiTheme.PanelInset;
        }

        private static void CreateEmptyStatePanel(Transform parent, string title, string body)
        {
            GameObject panel = BattleHudFactory.CreateInsetPanel("CampaignEmptyState", parent, 0f, BattleUiTheme.PanelGhost);
            LayoutElement layout = panel.GetComponent<LayoutElement>();
            layout.preferredHeight = 88f;
            Transform content = BattleHudFactory.CreateInsetContentRoot(panel.transform, 16f);
            VerticalLayoutGroup column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 4f;
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;

            Text titleLabel = BattleHudFactory.CreateText(content, title, 15, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            ClampText(titleLabel, 22f, TextOverflowModes.Truncate);
            Text bodyLabel = BattleHudFactory.CreateText(content, body, 13, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            ClampText(bodyLabel, 34f, TextOverflowModes.Truncate);
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
