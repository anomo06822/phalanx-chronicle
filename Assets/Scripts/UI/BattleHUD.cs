using System;
using System.Collections.Generic;
using PhalanxChronicle.Core;
using PhalanxChronicle.Localization;
using PhalanxChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace PhalanxChronicle.UI
{
    public sealed class BattleHUD : MonoBehaviour
    {
        private const int FeedLimit = 8;

        private readonly List<RosterEntryView> alliedRosterViews = new List<RosterEntryView>();
        private readonly List<RosterEntryView> enemyRosterViews = new List<RosterEntryView>();
        private readonly List<string> feedEntries = new List<string>();
        private readonly List<Text> feedLabels = new List<Text>();

        private BattleOverviewModel currentOverview = new BattleOverviewModel();
        private BattleForecastModel currentForecast;
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
        private Text instructionLabel;

        private Image selectedPortraitImage;
        private Image selectedPortraitBacking;
        private Image selectedHpFill;
        private Image selectedManaFill;
        private GameObject selectedTerrainInfoRoot;
        private Image selectedWeaponIconImage;
        private Image selectedWeaponIconBacking;
        private Image selectedWeaponAccentImage;
        private Text selectedNameLabel;
        private Text selectedRoleLabel;
        private Text selectedPositionLabel;
        private Text selectedTerrainNameLabel;
        private Text selectedTerrainEffectLabel;
        private Text selectedHpLabel;
        private Text selectedManaLabel;
        private Text selectedWeaponTypeLabel;
        private Text selectedWeaponNameLabel;
        private Text selectedWeaponDescriptionLabel;
        private Text selectedStatsLabel;
        private Text selectedActionLabel;
        private Text selectedCooldownLabel;
        private Text selectedStatusLabel;
        private Text selectedThreatSummaryLabel;
        private Text selectedThreatDetailLabel;
        private Text selectedPassiveNameLabel;
        private Text selectedPassiveDescriptionLabel;
        private Text selectedActiveNameLabel;
        private Text selectedActiveDescriptionLabel;
        private GameObject selectedDetailSectionsRoot;

        private Transform alliedRosterRoot;
        private Transform enemyRosterRoot;

        private Text forecastHeaderLabel;
        private Text forecastTitleLabel;
        private Text forecastSummaryLabel;
        private Text forecastDetailLabel;
        private Text forecastFooterLabel;
        private Image forecastAccentImage;

        private Text resultLabel;
        private Text resultContinueLabel;
        private Text dialogueSpeakerLabel;
        private Text dialogueBodyLabel;
        private Text dialogueContinueLabel;
        private Text campaignTitleLabel;
        private Text campaignBodyLabel;

        private Button endTurnButton;
        private Button rerollButton;
        private Button resultAdvanceButton;
        private Button dialogueAdvanceButton;
        private Button campaignPrimaryButton;
        private Button campaignSecondaryButton;
        private GameObject rerollButtonObject;
        private GameObject resultPanel;
        private GameObject dialogueOverlay;
        private GameObject campaignOverlay;
        private Transform campaignContentRoot;

        private Action<int> campaignStageSelectionHandler;
        private Action<string> campaignOptionSelectionHandler;
        private Action campaignPrimaryHandler;
        private Action campaignSecondaryHandler;

        public bool IsDialogueVisible => dialogueOverlay != null && dialogueOverlay.activeSelf;

        public bool IsRerollVisible => rerollButtonObject != null && rerollButtonObject.activeSelf;

        public bool IsResultVisible => resultPanel != null && resultPanel.activeSelf;

        public bool IsCampaignOverlayVisible => campaignOverlay != null && campaignOverlay.activeSelf;

        public string CurrentObjectiveText => objectivePrimaryLabel != null ? objectivePrimaryLabel.text : string.Empty;

        public void Initialize(Transform canvasRoot, Action onEndTurn, Action onReroll, Action onDialogueAdvance, Action onResultAdvance)
        {
            GameObject leftPanel = CreatePanel(
                "SelectedUnitPanel",
                canvasRoot,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(18f, 0f),
                new Vector2(284f, 860f),
                BattleUiTheme.PanelSurface);
            RectTransform leftRect = leftPanel.GetComponent<RectTransform>();
            leftRect.pivot = new Vector2(0f, 0.5f);
            BuildSelectedUnitPanel(leftPanel.transform);

            GameObject rightPanel = CreatePanel(
                "OverviewPanel",
                canvasRoot,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-18f, 0f),
                new Vector2(300f, 860f),
                BattleUiTheme.PanelSurface);
            RectTransform rightRect = rightPanel.GetComponent<RectTransform>();
            rightRect.pivot = new Vector2(1f, 0.5f);
            BuildOverviewPanel(rightPanel.transform, onEndTurn, onReroll);

            GameObject forecastPanel = CreatePanel(
                "ForecastPanel",
                canvasRoot,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -8f),
                new Vector2(460f, 92f),
                BattleUiTheme.PanelForecast);
            BuildForecastPanel(forecastPanel.transform);

            resultPanel = CreatePanel(
                "ResultPanel",
                canvasRoot,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 24f),
                new Vector2(620f, 252f),
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
            resultLabel = CreateText(resultPanel.transform, string.Empty, 24, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            RectTransform resultRect = resultLabel.GetComponent<RectTransform>();
            resultRect.anchorMin = Vector2.zero;
            resultRect.anchorMax = Vector2.one;
            resultRect.offsetMin = new Vector2(20f, 38f);
            resultRect.offsetMax = new Vector2(-20f, -18f);
            resultContinueLabel = CreateAbsoluteText(
                resultPanel.transform,
                new Vector2(20f, 10f),
                new Vector2(-20f, 24f),
                LocalizationService.Text("ui.result.continue", "Click to continue"),
                12,
                FontStyle.Italic,
                TextAnchor.LowerRight,
                BattleUiTheme.TextMuted);
            resultPanel.SetActive(false);

            BuildDialogueOverlay(canvasRoot, onDialogueAdvance);
            BuildCampaignOverlay(canvasRoot);
            BindOverview(new BattleOverviewModel());
            BindSelectedUnit(new BattleSelectedUnitModel());
            BindForecast(null);
        }

        public void BindOverview(BattleOverviewModel model)
        {
            currentOverview = model ?? new BattleOverviewModel();
            stageLabel.text = currentOverview.StageLabel;
            seedLabel.text = currentOverview.SeedLabel;
            phaseLabel.text = currentOverview.PhaseLabel;
            turnLabel.text = currentOverview.TurnLabel;
            playerAliveLabel.text = currentOverview.PlayerAliveLabel;
            enemyAliveLabel.text = currentOverview.EnemyAliveLabel;
            readyLabel.text = currentOverview.ReadyLabel;
            skillReadyLabel.text = currentOverview.SkillReadyLabel;
            objectivePrimaryLabel.text = currentOverview.ObjectivePrimary;
            objectiveFailureLabel.text = currentOverview.ObjectiveFailure;
            instructionLabel.text = currentOverview.InstructionText;

            if (currentForecast == null)
            {
                ApplyForecastModel(BuildNeutralForecastModel());
            }
        }

        public void BindSelectedUnit(BattleSelectedUnitModel model)
        {
            BattleSelectedUnitModel selected = model ?? new BattleSelectedUnitModel();
            if (!selected.HasSelection)
            {
                if (selectedDetailSectionsRoot != null)
                {
                    selectedDetailSectionsRoot.SetActive(false);
                }

                selectedPortraitImage.sprite = null;
                selectedPortraitImage.enabled = false;
                selectedPortraitBacking.color = new Color(0.18f, 0.17f, 0.16f, 1f);
                selectedWeaponIconImage.sprite = null;
                selectedWeaponIconImage.enabled = false;
                selectedWeaponIconBacking.color = new Color(0.17f, 0.16f, 0.15f, 1f);
                selectedWeaponAccentImage.color = BattleUiTheme.TextMuted;
                if (selectedTerrainInfoRoot != null)
                {
                    selectedTerrainInfoRoot.SetActive(false);
                }
                selectedHpFill.fillAmount = 0f;
                selectedNameLabel.text = LocalizationService.Text("ui.selected.card_none_title", "No Unit Selected");
                selectedRoleLabel.text = string.Empty;
                selectedPositionLabel.text = LocalizationService.Text("ui.selected.card_none_body", "Choose a player unit to inspect battlefield details.");
                selectedTerrainNameLabel.text = string.Empty;
                selectedTerrainEffectLabel.text = string.Empty;
                selectedHpLabel.text = string.Empty;
                selectedManaLabel.text = string.Empty;
                selectedManaFill.fillAmount = 0f;
                selectedWeaponTypeLabel.text = string.Empty;
                selectedWeaponNameLabel.text = string.Empty;
                selectedWeaponDescriptionLabel.text = string.Empty;
                selectedStatsLabel.text = string.Empty;
                selectedActionLabel.text = string.Empty;
                selectedCooldownLabel.text = string.Empty;
                selectedStatusLabel.text = string.Empty;
                selectedThreatSummaryLabel.text = string.Empty;
                selectedThreatDetailLabel.text = string.Empty;
                selectedPassiveNameLabel.text = string.Empty;
                selectedPassiveDescriptionLabel.text = string.Empty;
                selectedActiveNameLabel.text = string.Empty;
                selectedActiveDescriptionLabel.text = string.Empty;
                return;
            }

            if (selectedDetailSectionsRoot != null)
            {
                selectedDetailSectionsRoot.SetActive(true);
            }

            UnitVisualProfile visualProfile = UnitVisualCatalog.GetProfile(selected.UnitId, selected.Faction, selected.Role);
            selectedPortraitImage.enabled = true;
            selectedPortraitImage.sprite = RuntimeSpriteLibrary.GetPortraitSprite(visualProfile);
            selectedPortraitBacking.color = visualProfile.PortraitBackdropColor;
            selectedWeaponIconImage.enabled = true;
            selectedWeaponIconImage.sprite = RuntimeSpriteLibrary.GetWeaponSprite(visualProfile);
            selectedWeaponIconBacking.color = Color.Lerp(visualProfile.SecondaryColor, Color.black, 0.52f);
            selectedWeaponAccentImage.color = Color.Lerp(selected.WeaponAccentColor, visualProfile.AccentColor, 0.32f);
            selectedHpFill.fillAmount = selected.MaxHp <= 0 ? 0f : (float)selected.CurrentHp / selected.MaxHp;
            selectedHpFill.color = selectedHpFill.fillAmount > 0.55f
                ? new Color(0.39f, 0.81f, 0.42f, 1f)
                : selectedHpFill.fillAmount > 0.3f
                    ? new Color(0.91f, 0.74f, 0.22f, 1f)
                    : new Color(0.88f, 0.35f, 0.28f, 1f);
            selectedManaFill.fillAmount = selected.MaxMana <= 0 ? 0f : (float)selected.CurrentMana / selected.MaxMana;
            selectedManaFill.color = selectedManaFill.fillAmount > 0.55f
                ? new Color(0.38f, 0.78f, 0.95f, 1f)
                : selectedManaFill.fillAmount > 0.3f
                    ? new Color(0.46f, 0.66f, 0.98f, 1f)
                    : new Color(0.52f, 0.42f, 0.85f, 1f);
            selectedNameLabel.text = selected.DisplayName;
            selectedRoleLabel.text = selected.RoleLabel;
            selectedPositionLabel.text = selected.PositionLabel;
            if (selectedTerrainInfoRoot != null)
            {
                selectedTerrainInfoRoot.SetActive(true);
            }

            selectedTerrainNameLabel.text = selected.TerrainName;
            selectedTerrainEffectLabel.text = selected.TerrainEffectSummary;
            selectedHpLabel.text = LocalizationService.Format("ui.label.hp_value", "HP {0}/{1}", selected.CurrentHp, selected.MaxHp);
            selectedManaLabel.text = LocalizationService.Format("ui.label.mana_value", "Mana {0}/{1}", selected.CurrentMana, selected.MaxMana);
            selectedWeaponTypeLabel.text = selected.WeaponTypeLabel;
            selectedWeaponNameLabel.text = selected.WeaponName;
            selectedWeaponDescriptionLabel.text = string.IsNullOrWhiteSpace(selected.ArmorSummary)
                ? selected.WeaponDescription
                : selected.WeaponDescription + "\n" + selected.ArmorSummary;
            selectedStatsLabel.text = LocalizationService.Format(
                "ui.selected.stats",
                "ATK {0}  DEF {1}  MOVE {2}  RANGE {3}  MP {4}/{5}  EXP {6}/{7}",
                selected.Attack,
                selected.Defense,
                selected.MoveRange,
                selected.AttackRange,
                selected.CurrentMana,
                selected.MaxMana,
                selected.CurrentExp,
                selected.NextLevelExp);
            selectedActionLabel.text = selected.ActionSummary;
            selectedCooldownLabel.text = selected.CooldownLabel;
            selectedStatusLabel.text = selected.StatusSummary;
            selectedThreatSummaryLabel.text = selected.ThreatSummary;
            selectedThreatDetailLabel.text = selected.ThreatDetail;
            selectedPassiveNameLabel.text = selected.PassiveName;
            selectedPassiveDescriptionLabel.text = selected.PassiveDescription;
            selectedActiveNameLabel.text = selected.ActiveName;
            selectedActiveDescriptionLabel.text = selected.ActiveDescription;
        }

        public void BindRoster(
            IReadOnlyList<BattleRosterEntryModel> alliedRoster,
            IReadOnlyList<BattleRosterEntryModel> enemyRoster,
            Action<string> onRosterSelected)
        {
            rosterSelectionHandler = onRosterSelected;
            BindRosterGroup(alliedRosterRoot, alliedRosterViews, alliedRoster);
            BindRosterGroup(enemyRosterRoot, enemyRosterViews, enemyRoster);
        }

        public void BindForecast(BattleForecastModel model)
        {
            currentForecast = model;
            ApplyForecastModel(model ?? BuildNeutralForecastModel());
        }

        public void ClearForecast()
        {
            currentForecast = null;
            ApplyForecastModel(BuildNeutralForecastModel());
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

            RefreshFeed();
            if (currentForecast == null)
            {
                ApplyForecastModel(BuildNeutralForecastModel());
            }
        }

        public void SetEndTurnEnabled(bool enabled)
        {
            endTurnButton.interactable = enabled;
        }

        public void SetRerollEnabled(bool enabled)
        {
            rerollButtonObject.SetActive(enabled);
            rerollButton.interactable = enabled;
        }

        public void ShowResult(string text)
        {
            resultPanel.SetActive(true);
            resultLabel.text = text;
            resultLabel.alignment = text != null && text.Contains("\n")
                ? TextAnchor.UpperLeft
                : TextAnchor.MiddleCenter;
            resultLabel.fontSize = text != null && text.Contains("\n") ? 18 : 26;
        }

        public void HideResult()
        {
            resultPanel.SetActive(false);
        }

        public void ShowDialogue(string speaker, string body)
        {
            dialogueSpeakerLabel.text = speaker;
            dialogueBodyLabel.text = body;
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
            campaignStageSelectionHandler = onStageSelected;
            campaignOptionSelectionHandler = null;
            campaignPrimaryHandler = null;
            campaignSecondaryHandler = null;

            CampaignStageSelectModel stageSelectModel = model ?? new CampaignStageSelectModel();
            campaignTitleLabel.text = stageSelectModel.Title;
            campaignBodyLabel.text = stageSelectModel.Body;
            RebuildCampaignContent(root =>
            {
                IReadOnlyList<CampaignStageEntryModel> stages = stageSelectModel.Stages ?? Array.Empty<CampaignStageEntryModel>();
                foreach (CampaignStageEntryModel entry in stages)
                {
                    CreateCampaignStageEntry(root, entry);
                }
            });

            ConfigureCampaignButtons(null, null);
            campaignOverlay.SetActive(true);
        }

        public void ShowCampaignInterlude(CampaignInterludeModel model, Action onPrimary, Action onSecondary = null)
        {
            campaignStageSelectionHandler = null;
            campaignOptionSelectionHandler = null;
            campaignPrimaryHandler = onPrimary;
            campaignSecondaryHandler = onSecondary;

            CampaignInterludeModel interludeModel = model ?? new CampaignInterludeModel();
            campaignTitleLabel.text = interludeModel.Title;
            campaignBodyLabel.text = string.Empty;
            RebuildCampaignContent(root =>
            {
                GameObject narrativePanel = CreateInsetPanel("CampaignNarrativePanel", root, 252f, new Color(0.14f, 0.12f, 0.1f, 0.94f));
                Transform narrativeRoot = CreateInsetContentRoot(narrativePanel.transform, 18f);
                VerticalLayoutGroup narrativeLayout = narrativeRoot.gameObject.AddComponent<VerticalLayoutGroup>();
                narrativeLayout.spacing = 8f;
                narrativeLayout.childControlHeight = true;
                narrativeLayout.childControlWidth = true;
                narrativeLayout.childForceExpandHeight = false;

                Text narrativeBody = CreateText(narrativeRoot, interludeModel.Body, 18, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
                narrativeBody.GetComponent<LayoutElement>().preferredHeight = 188f;
            });

            ConfigureCampaignButtons(interludeModel.PrimaryActionLabel, interludeModel.SecondaryActionLabel);
            campaignOverlay.SetActive(true);
        }

        public void ShowCampaignOptionList(CampaignOptionListModel model, Action<string> onOptionSelected, Action onPrimary, Action onSecondary = null)
        {
            campaignStageSelectionHandler = null;
            campaignOptionSelectionHandler = onOptionSelected;
            campaignPrimaryHandler = onPrimary;
            campaignSecondaryHandler = onSecondary;

            CampaignOptionListModel optionListModel = model ?? new CampaignOptionListModel();
            campaignTitleLabel.text = optionListModel.Title;
            campaignBodyLabel.text = optionListModel.Body;
            RebuildCampaignContent(root =>
            {
                IReadOnlyList<CampaignOptionEntryModel> options = optionListModel.Options ?? Array.Empty<CampaignOptionEntryModel>();
                foreach (CampaignOptionEntryModel entry in options)
                {
                    CreateCampaignOptionEntry(root, entry);
                }
            });

            ConfigureCampaignButtons(optionListModel.PrimaryActionLabel, optionListModel.SecondaryActionLabel);
            campaignOverlay.SetActive(true);
        }

        public void HideCampaignOverlay()
        {
            campaignStageSelectionHandler = null;
            campaignOptionSelectionHandler = null;
            campaignPrimaryHandler = null;
            campaignSecondaryHandler = null;
            if (campaignOverlay != null)
            {
                campaignOverlay.SetActive(false);
            }
        }

        private void BuildSelectedUnitPanel(Transform parent)
        {
            VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            CreateSectionHeader(parent, LocalizationService.Text("ui.panel.selected", "Character Dossier"));

            GameObject identityPanel = CreateInsetPanel("IdentityPanel", parent, 200f);
            HorizontalLayoutGroup identityLayout = identityPanel.AddComponent<HorizontalLayoutGroup>();
            identityLayout.spacing = 16f;
            identityLayout.padding = new RectOffset(16, 16, 16, 16);
            identityLayout.childAlignment = TextAnchor.MiddleLeft;
            identityLayout.childForceExpandHeight = false;
            identityLayout.childForceExpandWidth = false;

            GameObject portraitFrame = CreatePanel(
                "PortraitFrame",
                identityPanel.transform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                Vector2.zero,
                new Vector2(128f, 128f),
                new Color(0.16f, 0.16f, 0.15f, 1f));
            selectedPortraitBacking = portraitFrame.GetComponent<Image>();
            LayoutElement portraitLayout = portraitFrame.AddComponent<LayoutElement>();
            portraitLayout.preferredWidth = 128f;
            portraitLayout.preferredHeight = 128f;

            GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitObject.transform.SetParent(portraitFrame.transform, false);
            RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.04f, 0.04f);
            portraitRect.anchorMax = new Vector2(0.96f, 0.96f);
            portraitRect.offsetMin = Vector2.zero;
            portraitRect.offsetMax = Vector2.zero;
            selectedPortraitImage = portraitObject.GetComponent<Image>();
            selectedPortraitImage.preserveAspect = true;

            GameObject identityTextRoot = new GameObject("IdentityTextRoot", typeof(RectTransform));
            identityTextRoot.transform.SetParent(identityPanel.transform, false);
            VerticalLayoutGroup identityTextLayout = identityTextRoot.AddComponent<VerticalLayoutGroup>();
            identityTextLayout.spacing = 5f;
            identityTextLayout.childControlHeight = true;
            identityTextLayout.childControlWidth = true;
            identityTextLayout.childForceExpandHeight = false;
            identityTextRoot.AddComponent<LayoutElement>().flexibleWidth = 1f;

            selectedNameLabel = CreateText(identityTextRoot.transform, string.Empty, 24, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            EnableBestFit(selectedNameLabel, 16, 24, true);
            selectedRoleLabel = CreateText(identityTextRoot.transform, string.Empty, 16, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            EnableBestFit(selectedRoleLabel, 12, 16, true);
            selectedPositionLabel = CreateText(identityTextRoot.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextMuted);
            selectedPositionLabel.GetComponent<LayoutElement>().preferredHeight = 32f;

            selectedTerrainInfoRoot = CreateInsetPanel("TerrainInfoPanel", identityTextRoot.transform, 44f, new Color(0.16f, 0.14f, 0.11f, 0.92f));
            Transform terrainInfoRoot = CreateInsetContentRoot(selectedTerrainInfoRoot.transform, 10f);
            HorizontalLayoutGroup terrainLayout = terrainInfoRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            terrainLayout.spacing = 10f;
            terrainLayout.childAlignment = TextAnchor.MiddleLeft;
            terrainLayout.childControlHeight = true;
            terrainLayout.childControlWidth = true;
            terrainLayout.childForceExpandHeight = false;
            terrainLayout.childForceExpandWidth = false;

            selectedTerrainNameLabel = CreateText(terrainInfoRoot, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            LayoutElement terrainNameLayout = selectedTerrainNameLabel.GetComponent<LayoutElement>();
            terrainNameLayout.preferredWidth = 78f;
            terrainNameLayout.preferredHeight = 24f;
            EnableBestFit(selectedTerrainNameLabel, 11, 12, true);

            selectedTerrainEffectLabel = CreateText(terrainInfoRoot, string.Empty, 12, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            LayoutElement terrainEffectLayout = selectedTerrainEffectLabel.GetComponent<LayoutElement>();
            terrainEffectLayout.flexibleWidth = 1f;
            terrainEffectLayout.preferredHeight = 28f;
            EnableBestFit(selectedTerrainEffectLabel, 11, 12, false);
            selectedTerrainInfoRoot.SetActive(false);

            selectedActionLabel = CreateText(identityTextRoot.transform, string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.96f, 0.92f, 0.82f, 1f));

            selectedDetailSectionsRoot = new GameObject("SelectedDetails", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            selectedDetailSectionsRoot.transform.SetParent(parent, false);
            VerticalLayoutGroup detailsLayout = selectedDetailSectionsRoot.GetComponent<VerticalLayoutGroup>();
            detailsLayout.spacing = 6f;
            detailsLayout.childControlHeight = true;
            detailsLayout.childControlWidth = true;
            detailsLayout.childForceExpandHeight = false;
            selectedDetailSectionsRoot.GetComponent<LayoutElement>().flexibleHeight = 1f;

            GameObject hpPanel = CreateInsetPanel("SelectedHpPanel", selectedDetailSectionsRoot.transform, 58f);
            selectedHpLabel = CreateText(hpPanel.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            RectTransform hpLabelRect = selectedHpLabel.GetComponent<RectTransform>();
            hpLabelRect.anchorMin = new Vector2(0f, 1f);
            hpLabelRect.anchorMax = new Vector2(1f, 1f);
            hpLabelRect.offsetMin = new Vector2(14f, -30f);
            hpLabelRect.offsetMax = new Vector2(-14f, -8f);

            CreateUiBar(hpPanel.transform, new Vector2(14f, 14f), new Vector2(-14f, 30f), out selectedHpFill);

            GameObject manaPanel = CreateInsetPanel("SelectedManaPanel", selectedDetailSectionsRoot.transform, 58f);
            selectedManaLabel = CreateText(manaPanel.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            RectTransform manaLabelRect = selectedManaLabel.GetComponent<RectTransform>();
            manaLabelRect.anchorMin = new Vector2(0f, 1f);
            manaLabelRect.anchorMax = new Vector2(1f, 1f);
            manaLabelRect.offsetMin = new Vector2(14f, -30f);
            manaLabelRect.offsetMax = new Vector2(-14f, -8f);
            CreateUiBar(manaPanel.transform, new Vector2(14f, 14f), new Vector2(-14f, 30f), out selectedManaFill);

            CreateSectionHeader(selectedDetailSectionsRoot.transform, LocalizationService.Text("ui.label.weapon", "Armory"));
            GameObject weaponPanel = CreateInsetPanel("WeaponPanel", selectedDetailSectionsRoot.transform, 120f, new Color(0.16f, 0.14f, 0.11f, 0.94f));
            Transform weaponRoot = CreateInsetContentRoot(weaponPanel.transform, 12f);
            GameObject weaponAccent = new GameObject("WeaponAccent", typeof(RectTransform), typeof(Image));
            weaponAccent.transform.SetParent(weaponRoot, false);
            RectTransform weaponAccentRect = weaponAccent.GetComponent<RectTransform>();
            weaponAccentRect.anchorMin = new Vector2(0f, 0f);
            weaponAccentRect.anchorMax = new Vector2(0f, 1f);
            weaponAccentRect.sizeDelta = new Vector2(5f, 0f);
            weaponAccentRect.anchoredPosition = Vector2.zero;
            selectedWeaponAccentImage = weaponAccent.GetComponent<Image>();
            selectedWeaponAccentImage.sprite = RuntimeSpriteLibrary.WhiteSprite;

            GameObject weaponLayoutRoot = new GameObject("WeaponLayout", typeof(RectTransform));
            weaponLayoutRoot.transform.SetParent(weaponRoot, false);
            RectTransform weaponLayoutRect = weaponLayoutRoot.GetComponent<RectTransform>();
            weaponLayoutRect.anchorMin = Vector2.zero;
            weaponLayoutRect.anchorMax = Vector2.one;
            weaponLayoutRect.offsetMin = new Vector2(12f, 0f);
            weaponLayoutRect.offsetMax = Vector2.zero;
            HorizontalLayoutGroup weaponLayout = weaponLayoutRoot.AddComponent<HorizontalLayoutGroup>();
            weaponLayout.spacing = 12f;
            weaponLayout.childAlignment = TextAnchor.MiddleLeft;
            weaponLayout.childControlHeight = true;
            weaponLayout.childControlWidth = true;
            weaponLayout.childForceExpandHeight = false;
            weaponLayout.childForceExpandWidth = false;

            GameObject weaponIconFrame = CreatePanel(
                "WeaponIconFrame",
                weaponLayoutRoot.transform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                Vector2.zero,
                new Vector2(82f, 82f),
                new Color(0.17f, 0.16f, 0.15f, 1f));
            selectedWeaponIconBacking = weaponIconFrame.GetComponent<Image>();
            LayoutElement weaponIconLayout = weaponIconFrame.AddComponent<LayoutElement>();
            weaponIconLayout.preferredWidth = 82f;
            weaponIconLayout.preferredHeight = 82f;

            GameObject weaponIcon = new GameObject("WeaponIcon", typeof(RectTransform), typeof(Image));
            weaponIcon.transform.SetParent(weaponIconFrame.transform, false);
            RectTransform weaponIconRect = weaponIcon.GetComponent<RectTransform>();
            weaponIconRect.anchorMin = new Vector2(0.16f, 0.16f);
            weaponIconRect.anchorMax = new Vector2(0.84f, 0.84f);
            weaponIconRect.offsetMin = Vector2.zero;
            weaponIconRect.offsetMax = Vector2.zero;
            selectedWeaponIconImage = weaponIcon.GetComponent<Image>();
            selectedWeaponIconImage.preserveAspect = true;

            GameObject weaponTextRoot = new GameObject("WeaponTextRoot", typeof(RectTransform));
            weaponTextRoot.transform.SetParent(weaponLayoutRoot.transform, false);
            weaponTextRoot.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup weaponTextLayout = weaponTextRoot.AddComponent<VerticalLayoutGroup>();
            weaponTextLayout.spacing = 2f;
            weaponTextLayout.childControlHeight = true;
            weaponTextLayout.childControlWidth = true;
            weaponTextLayout.childForceExpandHeight = false;
            weaponTextLayout.childForceExpandWidth = true;

            selectedWeaponTypeLabel = CreateText(weaponTextRoot.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            selectedWeaponNameLabel = CreateText(weaponTextRoot.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            EnableBestFit(selectedWeaponNameLabel, 12, 18, true);
            selectedWeaponDescriptionLabel = CreateText(weaponTextRoot.transform, string.Empty, 13, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            selectedWeaponDescriptionLabel.GetComponent<LayoutElement>().preferredHeight = 78f;

            GameObject statusPanel = CreateInsetPanel("SelectedStatusPanel", selectedDetailSectionsRoot.transform, 132f);
            Transform statusRoot = CreateInsetContentRoot(statusPanel.transform, 14f);
            VerticalLayoutGroup statusLayout = statusRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            statusLayout.spacing = 4f;
            statusLayout.childControlHeight = true;
            statusLayout.childControlWidth = true;
            statusLayout.childForceExpandHeight = false;

            selectedStatsLabel = CreateText(statusRoot, string.Empty, 16, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            selectedCooldownLabel = CreateText(statusRoot, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.78f, 0.92f, 0.98f, 1f));
            selectedStatusLabel = CreateText(statusRoot, string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.94f, 0.88f, 0.72f, 1f));
            selectedThreatSummaryLabel = CreateText(statusRoot, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextThreat);
            selectedThreatDetailLabel = CreateText(statusRoot, string.Empty, 14, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.93f, 0.82f, 0.76f, 1f));
            selectedThreatDetailLabel.GetComponent<LayoutElement>().preferredHeight = 34f;

            CreateSectionHeader(selectedDetailSectionsRoot.transform, LocalizationService.Text("ui.label.passive", "Passive"));
            GameObject passivePanel = CreateInsetPanel("PassivePanel", selectedDetailSectionsRoot.transform, 78f);
            Transform passiveRoot = CreateInsetContentRoot(passivePanel.transform, 14f);
            VerticalLayoutGroup passiveLayout = passiveRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            passiveLayout.spacing = 4f;
            passiveLayout.childControlHeight = true;
            passiveLayout.childControlWidth = true;
            passiveLayout.childForceExpandHeight = false;
            selectedPassiveNameLabel = CreateText(passiveRoot, string.Empty, 17, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            EnableBestFit(selectedPassiveNameLabel, 13, 17, true);
            selectedPassiveDescriptionLabel = CreateText(passiveRoot, string.Empty, 15, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            selectedPassiveDescriptionLabel.GetComponent<LayoutElement>().preferredHeight = 34f;

            CreateSectionHeader(selectedDetailSectionsRoot.transform, LocalizationService.Text("ui.label.active", "Active"));
            GameObject activePanel = CreateInsetPanel("ActivePanel", selectedDetailSectionsRoot.transform, 108f);
            Transform activeRoot = CreateInsetContentRoot(activePanel.transform, 14f);
            VerticalLayoutGroup activeLayout = activeRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            activeLayout.spacing = 4f;
            activeLayout.childControlHeight = true;
            activeLayout.childControlWidth = true;
            activeLayout.childForceExpandHeight = false;
            selectedActiveNameLabel = CreateText(activeRoot, string.Empty, 17, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            EnableBestFit(selectedActiveNameLabel, 13, 17, true);
            selectedActiveDescriptionLabel = CreateText(activeRoot, string.Empty, 15, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            selectedActiveDescriptionLabel.GetComponent<LayoutElement>().preferredHeight = 56f;
        }

        private void BuildOverviewPanel(Transform parent, Action onEndTurn, Action onReroll)
        {
            VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            CreateSectionHeader(parent, LocalizationService.Text("ui.panel.overview", "War Overview"));
            GameObject summaryPanel = CreateInsetPanel("OverviewSummaryPanel", parent, 112f);
            Transform summaryRoot = CreateInsetContentRoot(summaryPanel.transform, 10f);
            VerticalLayoutGroup summaryLayout = summaryRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            summaryLayout.spacing = 2f;
            summaryLayout.childControlHeight = true;
            summaryLayout.childControlWidth = true;
            summaryLayout.childForceExpandHeight = false;

            stageLabel = CreateText(summaryRoot, string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            EnableBestFit(stageLabel, 13, 18, true);
            seedLabel = CreateText(summaryRoot, string.Empty, 13, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextMuted);
            phaseLabel = CreateText(summaryRoot, string.Empty, 16, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            turnLabel = CreateText(summaryRoot, string.Empty, 14, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            playerAliveLabel = CreateText(summaryRoot, string.Empty, 13, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.62f, 0.8f, 1f, 1f));
            enemyAliveLabel = CreateText(summaryRoot, string.Empty, 13, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(1f, 0.66f, 0.58f, 1f));
            readyLabel = CreateText(summaryRoot, string.Empty, 13, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.93f, 0.95f, 0.87f, 1f));
            skillReadyLabel = CreateText(summaryRoot, string.Empty, 13, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            stageLabel.GetComponent<LayoutElement>().preferredHeight = 18f;
            seedLabel.GetComponent<LayoutElement>().preferredHeight = 14f;
            phaseLabel.GetComponent<LayoutElement>().preferredHeight = 16f;
            turnLabel.GetComponent<LayoutElement>().preferredHeight = 14f;
            playerAliveLabel.GetComponent<LayoutElement>().preferredHeight = 14f;
            enemyAliveLabel.GetComponent<LayoutElement>().preferredHeight = 14f;
            readyLabel.GetComponent<LayoutElement>().preferredHeight = 14f;
            skillReadyLabel.GetComponent<LayoutElement>().preferredHeight = 14f;

            CreateSectionHeader(parent, LocalizationService.Text("ui.objective.header", "Objective"));
            GameObject objectivePanel = CreateInsetPanel("ObjectivePanel", parent, 76f, BattleUiTheme.PanelCommand);
            Transform objectiveRoot = CreateInsetContentRoot(objectivePanel.transform, 12f);
            VerticalLayoutGroup objectiveLayout = objectiveRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            objectiveLayout.spacing = 4f;
            objectiveLayout.childControlHeight = true;
            objectiveLayout.childControlWidth = true;
            objectiveLayout.childForceExpandHeight = false;
            objectivePrimaryLabel = CreateText(objectiveRoot, string.Empty, 15, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            objectiveFailureLabel = CreateText(objectiveRoot, string.Empty, 14, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextWarning);
            objectivePrimaryLabel.GetComponent<LayoutElement>().preferredHeight = 34f;
            objectiveFailureLabel.GetComponent<LayoutElement>().preferredHeight = 22f;

            GameObject buttonRow = new GameObject("OverviewButtons", typeof(RectTransform));
            buttonRow.transform.SetParent(parent, false);
            HorizontalLayoutGroup buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 8f;
            buttonLayout.childForceExpandHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonRow.AddComponent<LayoutElement>().preferredHeight = 42f;

            endTurnButton = CreateActionButton(buttonRow.transform, LocalizationService.Text("ui.button.end_turn", "End Turn"));
            endTurnButton.onClick.AddListener(() => onEndTurn?.Invoke());
            rerollButton = CreateActionButton(buttonRow.transform, LocalizationService.Text("ui.button.reroll", "Reroll"));
            rerollButton.onClick.AddListener(() => onReroll?.Invoke());
            rerollButtonObject = rerollButton.gameObject;

            CreateSectionHeader(parent, LocalizationService.Text("ui.panel.allies", "Allied Ledger"));
            GameObject alliesPanel = CreateInsetPanel("AlliedRosterPanel", parent, 148f);
            alliedRosterRoot = CreateRosterRoot(CreateScrollContentRoot(alliesPanel.transform, 6f));
            CreateSectionHeader(parent, LocalizationService.Text("ui.panel.enemies", "Enemy Ledger"));
            GameObject enemiesPanel = CreateInsetPanel("EnemyRosterPanel", parent, 228f);
            enemyRosterRoot = CreateRosterRoot(CreateScrollContentRoot(enemiesPanel.transform, 6f));

            CreateSectionHeader(parent, LocalizationService.Text("ui.panel.feed", "War Feed"));
            GameObject feedPanel = CreateInsetPanel("FeedPanel", parent, 78f);
            Transform feedRoot = CreateScrollContentRoot(feedPanel.transform, 8f);
            VerticalLayoutGroup feedLayout = feedRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            feedLayout.spacing = 4f;
            feedLayout.childControlHeight = true;
            feedLayout.childControlWidth = true;
            feedLayout.childForceExpandHeight = false;
            for (int index = 0; index < FeedLimit; index++)
            {
                Text feedLabel = CreateText(feedRoot, string.Empty, 12, index == 0 ? FontStyle.Bold : FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
                feedLabel.GetComponent<LayoutElement>().preferredHeight = 22f;
                feedLabels.Add(feedLabel);
            }

            instructionLabel = CreateText(feedRoot, string.Empty, 14, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.95f, 0.91f, 0.79f, 1f));
            instructionLabel.GetComponent<LayoutElement>().preferredHeight = 24f;
            RefreshFeed();
        }

        private void BuildForecastPanel(Transform parent)
        {
            GameObject accentObject = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accentObject.transform.SetParent(parent, false);
            RectTransform accentRect = accentObject.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.sizeDelta = new Vector2(6f, 0f);
            accentRect.anchoredPosition = Vector2.zero;
            forecastAccentImage = accentObject.GetComponent<Image>();
            forecastAccentImage.sprite = RuntimeSpriteLibrary.WhiteSprite;

            forecastHeaderLabel = CreateAbsoluteText(parent, new Vector2(18f, -12f), new Vector2(-14f, -6f), string.Empty, 12, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextGold);
            forecastTitleLabel = CreateAbsoluteText(parent, new Vector2(18f, -28f), new Vector2(-14f, -20f), string.Empty, 18, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            forecastSummaryLabel = CreateAbsoluteText(parent, new Vector2(18f, -46f), new Vector2(-14f, -36f), string.Empty, 12, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            forecastDetailLabel = CreateAbsoluteText(parent, new Vector2(18f, 24f), new Vector2(-14f, 32f), string.Empty, 11, FontStyle.Normal, TextAnchor.LowerLeft, BattleUiTheme.TextSecondary);
            forecastFooterLabel = CreateAbsoluteText(parent, new Vector2(18f, 10f), new Vector2(-14f, 18f), string.Empty, 10, FontStyle.Italic, TextAnchor.LowerLeft, new Color(0.95f, 0.86f, 0.72f, 1f));
            EnableBestFit(forecastHeaderLabel, 10, 12, true);
            EnableBestFit(forecastTitleLabel, 14, 18, true);
            EnableBestFit(forecastSummaryLabel, 10, 12, true);
        }

        private void BuildDialogueOverlay(Transform canvasRoot, Action onDialogueAdvance)
        {
            dialogueOverlay = CreateStretchPanel("DialogueOverlay", canvasRoot, BattleUiTheme.PanelOverlay);
            dialogueAdvanceButton = dialogueOverlay.AddComponent<Button>();
            dialogueAdvanceButton.transition = Selectable.Transition.ColorTint;
            ColorBlock dialogueColors = dialogueAdvanceButton.colors;
            dialogueColors.normalColor = new Color(1f, 1f, 1f, 0f);
            dialogueColors.highlightedColor = new Color(1f, 1f, 1f, 0.02f);
            dialogueColors.pressedColor = new Color(1f, 1f, 1f, 0.04f);
            dialogueColors.disabledColor = new Color(1f, 1f, 1f, 0f);
            dialogueAdvanceButton.colors = dialogueColors;
            dialogueAdvanceButton.onClick.AddListener(() => onDialogueAdvance?.Invoke());

            GameObject dialogueBox = CreatePanel(
                "DialogueBox",
                dialogueOverlay.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 28f),
                new Vector2(980f, 226f),
                BattleUiTheme.PanelBackdrop);

            dialogueSpeakerLabel = CreateAbsoluteText(dialogueBox.transform, new Vector2(28f, -24f), new Vector2(-28f, -18f), string.Empty, 20, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextGold);
            dialogueBodyLabel = CreateAbsoluteText(dialogueBox.transform, new Vector2(28f, 48f), new Vector2(-28f, -64f), string.Empty, 22, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            dialogueContinueLabel = CreateAbsoluteText(dialogueBox.transform, new Vector2(28f, 16f), new Vector2(-28f, 32f), LocalizationService.Text("ui.dialogue.continue", "Click to continue"), 14, FontStyle.Italic, TextAnchor.LowerRight, BattleUiTheme.TextMuted);
            dialogueOverlay.SetActive(false);
        }

        private void BuildCampaignOverlay(Transform canvasRoot)
        {
            campaignOverlay = CreateStretchPanel("CampaignOverlay", canvasRoot, BattleUiTheme.PanelOverlay);
            GameObject campaignBox = CreatePanel(
                "CampaignBox",
                campaignOverlay.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1040f, 700f),
                BattleUiTheme.PanelBackdrop);

            VerticalLayoutGroup layout = campaignBox.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            campaignTitleLabel = CreateText(campaignBox.transform, string.Empty, 32, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            campaignTitleLabel.GetComponent<LayoutElement>().preferredHeight = 44f;
            campaignBodyLabel = CreateText(campaignBox.transform, string.Empty, 18, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            campaignBodyLabel.GetComponent<LayoutElement>().preferredHeight = 104f;

            GameObject contentPanel = CreateInsetPanel("CampaignContentPanel", campaignBox.transform, 470f, new Color(0.12f, 0.11f, 0.1f, 0.95f));
            campaignContentRoot = CreateScrollContentRoot(contentPanel.transform, 10f);
            VerticalLayoutGroup contentLayout = campaignContentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 12f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;

            GameObject buttonRow = new GameObject("CampaignButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            buttonRow.transform.SetParent(campaignBox.transform, false);
            buttonRow.GetComponent<LayoutElement>().preferredHeight = 50f;
            HorizontalLayoutGroup buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 10f;
            buttonLayout.childForceExpandHeight = true;
            buttonLayout.childForceExpandWidth = true;

            campaignPrimaryButton = CreateActionButton(buttonRow.transform, LocalizationService.Text("ui.button.continue", "Continue"));
            campaignPrimaryButton.onClick.AddListener(() => campaignPrimaryHandler?.Invoke());
            campaignSecondaryButton = CreateActionButton(buttonRow.transform, LocalizationService.Text("ui.button.back", "Back"));
            campaignSecondaryButton.onClick.AddListener(() => campaignSecondaryHandler?.Invoke());
            campaignOverlay.SetActive(false);
        }

        private void RebuildCampaignContent(Action<Transform> builder)
        {
            for (int index = campaignContentRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = campaignContentRoot.GetChild(index);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            builder?.Invoke(campaignContentRoot);
        }

        private void CreateCampaignStageEntry(Transform parent, CampaignStageEntryModel model)
        {
            GameObject root = CreateInsetPanel(
                "CampaignStageEntry",
                parent,
                112f,
                model.IsUnlocked
                    ? (model.IsCleared ? new Color(0.14f, 0.18f, 0.14f, 0.94f) : new Color(0.16f, 0.14f, 0.11f, 0.95f))
                    : new Color(0.12f, 0.11f, 0.11f, 0.88f));
            Button button = root.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.05f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.08f);
            colors.disabledColor = BattleUiTheme.ButtonDisabled;
            button.colors = colors;
            button.interactable = model.IsUnlocked;
            button.onClick.AddListener(() => campaignStageSelectionHandler?.Invoke(model.StageIndex));

            Transform contentRoot = CreateInsetContentRoot(root.transform, 14f);
            VerticalLayoutGroup layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            Text titleLabel = CreateText(contentRoot, model.Title, 18, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            titleLabel.GetComponent<LayoutElement>().preferredHeight = 24f;
            Text statusLabel = CreateText(
                contentRoot,
                model.Status,
                13,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                model.IsUnlocked
                    ? (model.IsRecommended ? new Color(0.99f, 0.84f, 0.34f, 1f) : new Color(0.72f, 0.92f, 0.78f, 1f))
                    : BattleUiTheme.TextMuted);
            statusLabel.GetComponent<LayoutElement>().preferredHeight = 18f;
            Text descriptionLabel = CreateText(
                contentRoot,
                model.Description,
                14,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                model.IsUnlocked ? BattleUiTheme.TextSecondary : new Color(0.62f, 0.59f, 0.54f, 1f));
            descriptionLabel.GetComponent<LayoutElement>().preferredHeight = 48f;
        }

        private void CreateCampaignOptionEntry(Transform parent, CampaignOptionEntryModel model)
        {
            GameObject root = CreateInsetPanel(
                "CampaignOptionEntry",
                parent,
                model.IsPromotionOption ? 148f : 124f,
                model.IsEnabled
                    ? (model.IsPromotionOption
                        ? new Color(0.15f, 0.19f, 0.12f, 0.97f)
                        : model.IsEmphasized
                            ? new Color(0.15f, 0.17f, 0.12f, 0.95f)
                            : new Color(0.16f, 0.14f, 0.11f, 0.95f))
                    : new Color(0.12f, 0.11f, 0.11f, 0.88f));
            Button button = root.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.05f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.08f);
            colors.disabledColor = BattleUiTheme.ButtonDisabled;
            button.colors = colors;
            button.interactable = model.IsEnabled && campaignOptionSelectionHandler != null;
            button.onClick.AddListener(() => campaignOptionSelectionHandler?.Invoke(model.OptionId));

            if (model.IsPromotionOption)
            {
                GameObject accent = new GameObject("PromotionAccent", typeof(RectTransform), typeof(Image));
                accent.transform.SetParent(root.transform, false);
                RectTransform accentRect = accent.GetComponent<RectTransform>();
                accentRect.anchorMin = new Vector2(0f, 0f);
                accentRect.anchorMax = new Vector2(0f, 1f);
                accentRect.sizeDelta = new Vector2(8f, 0f);
                accentRect.anchoredPosition = Vector2.zero;
                Image accentImage = accent.GetComponent<Image>();
                accentImage.color = new Color(0.95f, 0.78f, 0.28f, 0.98f);
            }

            Transform contentRoot = CreateInsetContentRoot(root.transform, model.IsPromotionOption ? 18f : 14f);
            VerticalLayoutGroup layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = model.IsPromotionOption ? 6f : 4f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            Text titleLabel = CreateText(contentRoot, model.Title, model.IsPromotionOption ? 22 : 19, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            titleLabel.GetComponent<LayoutElement>().preferredHeight = model.IsPromotionOption ? 30f : 26f;
            Text statusLabel = CreateText(
                contentRoot,
                model.Status,
                model.IsPromotionOption ? 16 : 14,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                model.IsEnabled
                    ? (model.IsPromotionOption
                        ? new Color(0.99f, 0.84f, 0.34f, 1f)
                        : model.IsEmphasized
                            ? new Color(0.99f, 0.84f, 0.34f, 1f)
                            : new Color(0.72f, 0.92f, 0.78f, 1f))
                    : BattleUiTheme.TextMuted);
            statusLabel.GetComponent<LayoutElement>().preferredHeight = model.IsPromotionOption ? 22f : 18f;
            Text descriptionLabel = CreateText(
                contentRoot,
                model.Description,
                model.IsPromotionOption ? 16 : 15,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                model.IsEnabled ? BattleUiTheme.TextPrimary : new Color(0.62f, 0.59f, 0.54f, 1f));
            descriptionLabel.GetComponent<LayoutElement>().preferredHeight = model.IsPromotionOption ? 74f : 56f;
        }

        private void ConfigureCampaignButtons(string primaryLabel, string secondaryLabel)
        {
            bool hasPrimary = !string.IsNullOrWhiteSpace(primaryLabel);
            bool hasSecondary = !string.IsNullOrWhiteSpace(secondaryLabel);
            campaignPrimaryButton.gameObject.SetActive(hasPrimary);
            campaignSecondaryButton.gameObject.SetActive(hasSecondary);

            if (hasPrimary)
            {
                SetButtonLabel(campaignPrimaryButton, primaryLabel);
            }

            if (hasSecondary)
            {
                SetButtonLabel(campaignSecondaryButton, secondaryLabel);
            }
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

                BattleRosterEntryModel model = models[index];
                BindRosterEntry(views[index], model);
            }
        }

        private void BindRosterEntry(RosterEntryView view, BattleRosterEntryModel model)
        {
            view.UnitId = model.UnitId;
            view.NameLabel.text = model.DisplayName;
            view.RoleLabel.text = model.RoleShortLabel;
            view.PositionLabel.text = model.PositionLabel;
            view.StateLabel.text = model.StatusLabel;
            view.SkillLabel.text = model.SkillLabel;
            view.HpLabel.text = model.IsAlive
                ? LocalizationService.Format("ui.label.hp_value", "HP {0}/{1}", model.CurrentHp, model.MaxHp)
                : LocalizationService.Text("ui.roster.defeated", "Defeated");
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
            view.PositionLabel.color = model.IsAlive
                ? new Color(0.82f, 0.84f, 0.88f, 1f)
                : BattleUiTheme.TextMuted;
            view.StateLabel.color = !model.IsAlive
                ? BattleUiTheme.TextMuted
                : model.HasActed
                    ? new Color(0.86f, 0.82f, 0.72f, 1f)
                    : new Color(0.76f, 0.94f, 0.8f, 1f);
            view.SkillLabel.color = !model.IsAlive
                ? BattleUiTheme.TextMuted
                : model.CanUseSkill
                    ? new Color(0.81f, 0.9f, 1f, 1f)
                    : new Color(0.72f, 0.74f, 0.8f, 1f);
            view.Accent.color = model.IsSelected
                ? new Color(0.98f, 0.86f, 0.4f, 0.98f)
                : model.IsThreateningSelection
                    ? new Color(0.95f, 0.52f, 0.38f, 0.95f)
                    : model.Faction == UnitFaction.Player
                        ? new Color(0.43f, 0.69f, 0.98f, 0.95f)
                        : new Color(0.95f, 0.44f, 0.35f, 0.95f);

            Color baseColor = model.Faction == UnitFaction.Player
                ? new Color(0.1f, 0.16f, 0.25f, model.IsAlive ? 0.95f : 0.68f)
                : new Color(0.24f, 0.1f, 0.1f, model.IsAlive ? 0.95f : 0.68f);
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
            view.Button.interactable = model.IsAlive;
            view.Button.onClick.RemoveAllListeners();
            view.Button.onClick.AddListener(() => rosterSelectionHandler?.Invoke(view.UnitId));
        }

        private void RefreshFeed()
        {
            for (int index = 0; index < feedLabels.Count; index++)
            {
                feedLabels[index].text = index < feedEntries.Count
                    ? feedEntries[index]
                    : index == 0
                        ? LocalizationService.Text("ui.feed.empty", "No battle events yet.")
                        : string.Empty;
            }
        }

        private BattleForecastModel BuildNeutralForecastModel()
        {
            string latestFeed = feedEntries.Count > 0
                ? feedEntries[0]
                : LocalizationService.Text("ui.feed.empty", "No battle events yet.");
            string detail = string.IsNullOrEmpty(currentOverview.ObjectivePrimary)
                ? latestFeed
                : currentOverview.ObjectivePrimary;
            string footer = string.IsNullOrEmpty(currentOverview.InstructionText)
                ? latestFeed
                : currentOverview.InstructionText;

            return new BattleForecastModel
            {
                Header = string.IsNullOrEmpty(currentOverview.StageLabel)
                    ? LocalizationService.Text("ui.panel.forecast", "Battle Forecast")
                    : currentOverview.StageLabel,
                Title = string.IsNullOrEmpty(currentOverview.PhaseLabel)
                    ? LocalizationService.Text("ui.forecast.neutral.title", "Awaiting Orders")
                    : currentOverview.PhaseLabel,
                Summary = string.IsNullOrEmpty(currentOverview.TurnLabel)
                    ? LocalizationService.Text("ui.forecast.neutral.summary", "Review the field and choose your next move.")
                    : currentOverview.TurnLabel,
                Detail = detail,
                Footer = footer,
                AccentColor = new Color(0.78f, 0.62f, 0.28f, 1f),
            };
        }

        private void ApplyForecastModel(BattleForecastModel model)
        {
            forecastHeaderLabel.text = model.Header;
            forecastTitleLabel.text = model.Title;
            forecastSummaryLabel.text = model.Summary;
            forecastDetailLabel.text = model.Detail;
            forecastFooterLabel.text = model.Footer;
            forecastAccentImage.color = model.AccentColor;
        }

        private static Transform CreateRosterRoot(Transform parent)
        {
            parent.gameObject.name = "RosterContent";
            VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            return parent;
        }

        private static RosterEntryView CreateRosterEntryView(Transform parent)
        {
            GameObject rootObject = CreatePanel(
                "RosterEntry",
                parent,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                Vector2.zero,
                new Vector2(0f, 70f),
                new Color(0.11f, 0.17f, 0.27f, 0.95f));
            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootObject.AddComponent<LayoutElement>().preferredHeight = 70f;

            Image background = rootObject.GetComponent<Image>();
            background.sprite = RuntimeSpriteLibrary.WhiteSprite;
            Outline outline = rootObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.effectColor = new Color(0.38f, 0.31f, 0.24f, 0.75f);

            Button button = rootObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.06f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.1f);
            button.colors = colors;

            GameObject accentObject = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accentObject.transform.SetParent(rootObject.transform, false);
            RectTransform accentRect = accentObject.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.sizeDelta = new Vector2(6f, 0f);
            accentRect.anchoredPosition = Vector2.zero;
            Image accent = accentObject.GetComponent<Image>();
            accent.sprite = RuntimeSpriteLibrary.WhiteSprite;

            GameObject contentRoot = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contentRoot.transform.SetParent(rootObject.transform, false);
            RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(14f, 7f);
            contentRect.offsetMax = new Vector2(-12f, -7f);

            HorizontalLayoutGroup contentLayout = contentRoot.GetComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = 10f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childAlignment = TextAnchor.MiddleLeft;

            GameObject textColumn = new GameObject("TextColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textColumn.transform.SetParent(contentRoot.transform, false);
            LayoutElement textLayoutElement = textColumn.GetComponent<LayoutElement>();
            textLayoutElement.flexibleWidth = 1f;
            VerticalLayoutGroup textLayout = textColumn.GetComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2f;
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

            Text nameLabel = CreateText(topRow.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            EnableBestFit(nameLabel, 12, 15, true);
            nameLabel.GetComponent<LayoutElement>().flexibleWidth = 1f;
            nameLabel.GetComponent<LayoutElement>().preferredHeight = 18f;
            Text roleLabel = CreateText(topRow.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleRight, BattleUiTheme.TextGold);
            EnableBestFit(roleLabel, 10, 12, true);
            roleLabel.GetComponent<LayoutElement>().preferredWidth = 42f;
            roleLabel.GetComponent<LayoutElement>().preferredHeight = 18f;

            Text positionLabel = CreateText(textColumn.transform, string.Empty, 11, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextSecondary);
            EnableBestFit(positionLabel, 10, 11, true);
            positionLabel.GetComponent<LayoutElement>().preferredHeight = 14f;

            GameObject tagRow = new GameObject("TagRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            tagRow.transform.SetParent(textColumn.transform, false);
            tagRow.GetComponent<LayoutElement>().preferredHeight = 16f;
            HorizontalLayoutGroup tagLayout = tagRow.GetComponent<HorizontalLayoutGroup>();
            tagLayout.spacing = 8f;
            tagLayout.childControlHeight = true;
            tagLayout.childControlWidth = true;
            tagLayout.childForceExpandHeight = false;
            tagLayout.childForceExpandWidth = false;

            Text stateLabel = CreateText(tagRow.transform, string.Empty, 11, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.95f, 0.88f, 0.72f, 1f));
            EnableBestFit(stateLabel, 10, 11, true);
            stateLabel.GetComponent<LayoutElement>().flexibleWidth = 1f;
            stateLabel.GetComponent<LayoutElement>().preferredHeight = 16f;
            Text skillLabel = CreateText(tagRow.transform, string.Empty, 11, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.8f, 0.87f, 0.99f, 1f));
            EnableBestFit(skillLabel, 10, 11, true);
            skillLabel.GetComponent<LayoutElement>().preferredWidth = 108f;
            skillLabel.GetComponent<LayoutElement>().preferredHeight = 16f;

            GameObject hpColumn = new GameObject("HpColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            hpColumn.transform.SetParent(contentRoot.transform, false);
            LayoutElement hpLayoutElement = hpColumn.GetComponent<LayoutElement>();
            hpLayoutElement.preferredWidth = 118f;
            VerticalLayoutGroup hpLayout = hpColumn.GetComponent<VerticalLayoutGroup>();
            hpLayout.spacing = 5f;
            hpLayout.childControlHeight = true;
            hpLayout.childControlWidth = true;
            hpLayout.childForceExpandHeight = false;
            hpLayout.childAlignment = TextAnchor.UpperRight;

            Text hpLabel = CreateText(hpColumn.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleRight, BattleUiTheme.TextPrimary);
            EnableBestFit(hpLabel, 10, 12, true);
            hpLabel.GetComponent<LayoutElement>().preferredHeight = 18f;

            GameObject hpBarRoot = new GameObject("HpBar", typeof(RectTransform), typeof(LayoutElement));
            hpBarRoot.transform.SetParent(hpColumn.transform, false);
            hpBarRoot.GetComponent<LayoutElement>().preferredHeight = 14f;

            Image hpFill;
            CreateStretchUiBar(hpBarRoot.transform, out hpFill);

            return new RosterEntryView(rootObject, button, background, outline, accent, nameLabel, roleLabel, positionLabel, stateLabel, skillLabel, hpLabel, hpFill);
        }

        private static Transform CreateScrollContentRoot(Transform parent, float padding)
        {
            ScrollRect scrollRect = parent.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.inertia = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(parent, false);
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(padding, padding);
            viewportRect.offsetMax = new Vector2(-(padding + 12f), -padding);

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            viewportImage.color = new Color(1f, 1f, 1f, 0.015f);
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateVerticalScrollbar(parent, padding);
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 4f;
            return contentObject.transform;
        }

        private static Scrollbar CreateVerticalScrollbar(Transform parent, float padding)
        {
            GameObject scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarObject.transform.SetParent(parent, false);
            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.offsetMin = new Vector2(-(padding + 10f), padding);
            scrollbarRect.offsetMax = new Vector2(-padding, -padding);

            Image trackImage = scrollbarObject.GetComponent<Image>();
            trackImage.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            trackImage.color = new Color(0.18f, 0.15f, 0.11f, 0.9f);

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(scrollbarObject.transform, false);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = new Vector2(1f, 1f);
            handleRect.offsetMax = new Vector2(-1f, -1f);

            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            handleImage.color = BattleUiTheme.AccentGold;

            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.value = 1f;
            return scrollbar;
        }

        private static void CreateUiBar(Transform parent, Vector2 offsetMin, Vector2 offsetMax, out Image fillImage)
        {
            GameObject backObject = new GameObject("BarBack", typeof(RectTransform), typeof(Image));
            backObject.transform.SetParent(parent, false);
            RectTransform backRect = backObject.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 0f);
            backRect.anchorMax = new Vector2(1f, 0f);
            backRect.offsetMin = offsetMin;
            backRect.offsetMax = offsetMax;
            Image backImage = backObject.GetComponent<Image>();
            backImage.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            backImage.color = new Color(0.12f, 0.09f, 0.08f, 0.95f);

            GameObject fillObject = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(backObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fillImage = fillObject.GetComponent<Image>();
            fillImage.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;
        }

        private static void CreateStretchUiBar(Transform parent, out Image fillImage)
        {
            GameObject backObject = new GameObject("BarBack", typeof(RectTransform), typeof(Image));
            backObject.transform.SetParent(parent, false);
            RectTransform backRect = backObject.GetComponent<RectTransform>();
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = Vector2.one;
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;
            Image backImage = backObject.GetComponent<Image>();
            backImage.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            backImage.color = new Color(0.12f, 0.09f, 0.08f, 0.95f);

            GameObject fillObject = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(backObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fillImage = fillObject.GetComponent<Image>();
            fillImage.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;
        }

        private static GameObject CreatePanel(
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
            rectTransform.pivot = new Vector2(anchorMax.x, anchorMax.y);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            image.color = color;

            Outline outline = panel.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = BattleUiTheme.OutlineStrong;
            return panel;
        }

        private static GameObject CreateStretchPanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            image.color = color;
            return panel;
        }

        private static GameObject CreateInsetPanel(string name, Transform parent, float preferredHeight)
        {
            return CreateInsetPanel(name, parent, preferredHeight, BattleUiTheme.PanelInset);
        }

        private static GameObject CreateInsetPanel(string name, Transform parent, float preferredHeight, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            image.color = color;
            panel.GetComponent<LayoutElement>().preferredHeight = preferredHeight;
            Outline outline = panel.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = BattleUiTheme.OutlineSoft;
            return panel;
        }

        private static Button CreateActionButton(Transform parent, string label)
        {
            GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<LayoutElement>().preferredHeight = 44f;
            buttonObject.GetComponent<LayoutElement>().flexibleWidth = 1f;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            image.color = BattleUiTheme.ButtonPrimary;

            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = new Color(0.34f, 0.22f, 0.08f, 0.66f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = BattleUiTheme.ButtonPrimaryHighlight;
            colors.pressedColor = BattleUiTheme.ButtonPrimaryPressed;
            colors.disabledColor = BattleUiTheme.ButtonDisabled;
            button.colors = colors;

            Text text = CreateAbsoluteText(buttonObject.transform, Vector2.zero, Vector2.zero, label, 18, FontStyle.Bold, TextAnchor.MiddleCenter, BattleUiTheme.ButtonText);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 4f);
            textRect.offsetMax = new Vector2(-6f, -4f);
            EnableBestFit(text, 12, 18, true);
            return button;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label ?? string.Empty;
            }
        }

        private static void CreateSectionHeader(Transform parent, string text)
        {
            Text header = CreateText(parent, text, 15, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            header.GetComponent<LayoutElement>().preferredHeight = 20f;
        }

        private static Text CreateText(Transform parent, string content, int size, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = RuntimeSpriteLibrary.GetUiFont(size, fontStyle);
            text.fontSize = size;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.alignByGeometry = true;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            ApplyTextLegibility(text, size >= 18);

            textObject.GetComponent<LayoutElement>().preferredHeight = size + 10f;
            return text;
        }

        private static Text CreateAbsoluteText(
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
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;

            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = RuntimeSpriteLibrary.GetUiFont(size, fontStyle);
            text.fontSize = size;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.alignByGeometry = true;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            ApplyTextLegibility(text, size >= 18);
            return text;
        }

        private static Transform CreateInsetContentRoot(Transform parent, float padding)
        {
            GameObject rootObject = new GameObject("Content", typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            RectTransform rectTransform = rootObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(padding, padding);
            rectTransform.offsetMax = new Vector2(-padding, -padding);
            return rootObject.transform;
        }

        private static void EnableBestFit(Text text, int minSize, int maxSize, bool singleLine)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(minSize, maxSize - 2);
            text.resizeTextMaxSize = maxSize;
            if (!singleLine)
            {
                return;
            }

            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.alignByGeometry = true;
        }

        private static void ApplyTextLegibility(Text text, bool strong)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = strong
                ? new Color(0f, 0f, 0f, 0.88f)
                : new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = strong
                ? new Vector2(1.2f, -1.2f)
                : new Vector2(0.8f, -0.8f);
            text.material = text.font != null ? text.font.material : text.material;
        }

        private sealed class RosterEntryView
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
                Text stateLabel,
                Text skillLabel,
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
                StateLabel = stateLabel;
                SkillLabel = skillLabel;
                HpLabel = hpLabel;
                HpFill = hpFill;
            }

            public GameObject Root { get; }

            public Button Button { get; }

            public Image Background { get; }

            public Outline Outline { get; }

            public Image Accent { get; }

            public Text NameLabel { get; }

            public Text RoleLabel { get; }

            public Text PositionLabel { get; }

            public Text StateLabel { get; }

            public Text SkillLabel { get; }

            public Text HpLabel { get; }

            public Image HpFill { get; }

            public string UnitId { get; set; } = string.Empty;
        }
    }
}
