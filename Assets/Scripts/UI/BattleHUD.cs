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
        private Image selectedWeaponIconImage;
        private Image selectedWeaponIconBacking;
        private Image selectedWeaponAccentImage;
        private Text selectedNameLabel;
        private Text selectedRoleLabel;
        private Text selectedPositionLabel;
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

        private Transform alliedRosterRoot;
        private Transform enemyRosterRoot;

        private Text forecastHeaderLabel;
        private Text forecastTitleLabel;
        private Text forecastSummaryLabel;
        private Text forecastDetailLabel;
        private Text forecastFooterLabel;
        private Image forecastAccentImage;

        private Text resultLabel;
        private Text dialogueSpeakerLabel;
        private Text dialogueBodyLabel;
        private Text dialogueContinueLabel;

        private Button endTurnButton;
        private Button rerollButton;
        private Button dialogueAdvanceButton;
        private GameObject rerollButtonObject;
        private GameObject resultPanel;
        private GameObject dialogueOverlay;

        public bool IsDialogueVisible => dialogueOverlay != null && dialogueOverlay.activeSelf;

        public bool IsRerollVisible => rerollButtonObject != null && rerollButtonObject.activeSelf;

        public string CurrentObjectiveText => objectivePrimaryLabel != null ? objectivePrimaryLabel.text : string.Empty;

        public void Initialize(Transform canvasRoot, Action onEndTurn, Action onReroll, Action onDialogueAdvance)
        {
            GameObject leftPanel = CreatePanel(
                "SelectedUnitPanel",
                canvasRoot,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(18f, 0f),
                new Vector2(360f, 860f),
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
                new Vector2(392f, 860f),
                BattleUiTheme.PanelSurface);
            RectTransform rightRect = rightPanel.GetComponent<RectTransform>();
            rightRect.pivot = new Vector2(1f, 0.5f);
            BuildOverviewPanel(rightPanel.transform, onEndTurn, onReroll);

            GameObject forecastPanel = CreatePanel(
                "ForecastPanel",
                canvasRoot,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -18f),
                new Vector2(540f, 148f),
                BattleUiTheme.PanelBackdrop);
            BuildForecastPanel(forecastPanel.transform);

            resultPanel = CreatePanel(
                "ResultPanel",
                canvasRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(460f, 220f),
                BattleUiTheme.PanelBackdrop);
            resultLabel = CreateText(resultPanel.transform, string.Empty, 38, FontStyle.Bold, TextAnchor.MiddleCenter, BattleUiTheme.TextPrimary);
            RectTransform resultRect = resultLabel.GetComponent<RectTransform>();
            resultRect.anchorMin = Vector2.zero;
            resultRect.anchorMax = Vector2.one;
            resultRect.offsetMin = new Vector2(24f, 24f);
            resultRect.offsetMax = new Vector2(-24f, -24f);
            resultPanel.SetActive(false);

            BuildDialogueOverlay(canvasRoot, onDialogueAdvance);
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
                selectedPortraitImage.sprite = null;
                selectedPortraitImage.enabled = false;
                selectedPortraitBacking.color = new Color(0.18f, 0.17f, 0.16f, 1f);
                selectedWeaponIconImage.sprite = null;
                selectedWeaponIconImage.enabled = false;
                selectedWeaponIconBacking.color = new Color(0.17f, 0.16f, 0.15f, 1f);
                selectedWeaponAccentImage.color = BattleUiTheme.TextMuted;
                selectedHpFill.fillAmount = 0f;
                selectedNameLabel.text = LocalizationService.Text("ui.selected.card_none_title", "No Unit Selected");
                selectedRoleLabel.text = string.Empty;
                selectedPositionLabel.text = LocalizationService.Text("ui.selected.card_none_body", "Choose a player unit to inspect battlefield details.");
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

            selectedPortraitImage.enabled = true;
            selectedPortraitImage.sprite = RuntimeSpriteLibrary.GetUnitSprite(selected.UnitId, selected.Faction);
            selectedPortraitBacking.color = selected.Faction == UnitFaction.Player
                ? new Color(0.14f, 0.21f, 0.38f, 1f)
                : new Color(0.41f, 0.13f, 0.11f, 1f);
            selectedWeaponIconImage.enabled = true;
            selectedWeaponIconImage.sprite = RuntimeSpriteLibrary.GetWeaponSprite(selected.Role, selected.Faction);
            selectedWeaponIconBacking.color = Color.Lerp(new Color(0.11f, 0.1f, 0.09f, 1f), selected.WeaponAccentColor, 0.32f);
            selectedWeaponAccentImage.color = selected.WeaponAccentColor;
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
            selectedHpLabel.text = LocalizationService.Format("ui.label.hp_value", "HP {0}/{1}", selected.CurrentHp, selected.MaxHp);
            selectedManaLabel.text = LocalizationService.Format("ui.label.mana_value", "Mana {0}/{1}", selected.CurrentMana, selected.MaxMana);
            selectedWeaponTypeLabel.text = selected.WeaponTypeLabel;
            selectedWeaponNameLabel.text = selected.WeaponName;
            selectedWeaponDescriptionLabel.text = selected.WeaponDescription;
            selectedStatsLabel.text = LocalizationService.Format(
                "ui.selected.stats",
                "ATK {0}  DEF {1}  MOVE {2}  RANGE {3}  MP {4}/{5}",
                selected.Attack,
                selected.Defense,
                selected.MoveRange,
                selected.AttackRange,
                selected.CurrentMana,
                selected.MaxMana);
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

        private void BuildSelectedUnitPanel(Transform parent)
        {
            VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            CreateSectionHeader(parent, LocalizationService.Text("ui.panel.selected", "Character Dossier"));

            GameObject identityPanel = CreateInsetPanel("IdentityPanel", parent, 144f);
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
                new Vector2(104f, 104f),
                new Color(0.18f, 0.17f, 0.16f, 1f));
            selectedPortraitBacking = portraitFrame.GetComponent<Image>();
            LayoutElement portraitLayout = portraitFrame.AddComponent<LayoutElement>();
            portraitLayout.preferredWidth = 104f;
            portraitLayout.preferredHeight = 104f;

            GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitObject.transform.SetParent(portraitFrame.transform, false);
            RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.08f, 0.08f);
            portraitRect.anchorMax = new Vector2(0.92f, 0.92f);
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
            selectedPositionLabel.GetComponent<LayoutElement>().preferredHeight = 34f;
            selectedActionLabel = CreateText(identityTextRoot.transform, string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.96f, 0.92f, 0.82f, 1f));

            GameObject hpPanel = CreateInsetPanel("SelectedHpPanel", parent, 58f);
            selectedHpLabel = CreateText(hpPanel.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            RectTransform hpLabelRect = selectedHpLabel.GetComponent<RectTransform>();
            hpLabelRect.anchorMin = new Vector2(0f, 1f);
            hpLabelRect.anchorMax = new Vector2(1f, 1f);
            hpLabelRect.offsetMin = new Vector2(14f, -30f);
            hpLabelRect.offsetMax = new Vector2(-14f, -8f);

            CreateUiBar(hpPanel.transform, new Vector2(14f, 14f), new Vector2(-14f, 30f), out selectedHpFill);

            GameObject manaPanel = CreateInsetPanel("SelectedManaPanel", parent, 58f);
            selectedManaLabel = CreateText(manaPanel.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            RectTransform manaLabelRect = selectedManaLabel.GetComponent<RectTransform>();
            manaLabelRect.anchorMin = new Vector2(0f, 1f);
            manaLabelRect.anchorMax = new Vector2(1f, 1f);
            manaLabelRect.offsetMin = new Vector2(14f, -30f);
            manaLabelRect.offsetMax = new Vector2(-14f, -8f);
            CreateUiBar(manaPanel.transform, new Vector2(14f, 14f), new Vector2(-14f, 30f), out selectedManaFill);

            GameObject statusPanel = CreateInsetPanel("SelectedStatusPanel", parent, 150f);
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

            CreateSectionHeader(parent, LocalizationService.Text("ui.label.passive", "Passive"));
            GameObject passivePanel = CreateInsetPanel("PassivePanel", parent, 84f);
            Transform passiveRoot = CreateInsetContentRoot(passivePanel.transform, 14f);
            VerticalLayoutGroup passiveLayout = passiveRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            passiveLayout.spacing = 4f;
            passiveLayout.childControlHeight = true;
            passiveLayout.childControlWidth = true;
            passiveLayout.childForceExpandHeight = false;
            selectedPassiveNameLabel = CreateText(passiveRoot, string.Empty, 17, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            EnableBestFit(selectedPassiveNameLabel, 13, 17, true);
            selectedPassiveDescriptionLabel = CreateText(passiveRoot, string.Empty, 15, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            selectedPassiveDescriptionLabel.GetComponent<LayoutElement>().preferredHeight = 42f;

            CreateSectionHeader(parent, LocalizationService.Text("ui.label.active", "Active"));
            GameObject activePanel = CreateInsetPanel("ActivePanel", parent, 96f);
            Transform activeRoot = CreateInsetContentRoot(activePanel.transform, 14f);
            VerticalLayoutGroup activeLayout = activeRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            activeLayout.spacing = 4f;
            activeLayout.childControlHeight = true;
            activeLayout.childControlWidth = true;
            activeLayout.childForceExpandHeight = false;
            selectedActiveNameLabel = CreateText(activeRoot, string.Empty, 17, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            EnableBestFit(selectedActiveNameLabel, 13, 17, true);
            selectedActiveDescriptionLabel = CreateText(activeRoot, string.Empty, 15, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            selectedActiveDescriptionLabel.GetComponent<LayoutElement>().preferredHeight = 48f;
        }

        private void BuildOverviewPanel(Transform parent, Action onEndTurn, Action onReroll)
        {
            VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            CreateSectionHeader(parent, LocalizationService.Text("ui.panel.overview", "War Overview"));
            GameObject summaryPanel = CreateInsetPanel("OverviewSummaryPanel", parent, 132f);
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
            GameObject objectivePanel = CreateInsetPanel("ObjectivePanel", parent, 78f, BattleUiTheme.PanelCommand);
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
            buttonRow.AddComponent<LayoutElement>().preferredHeight = 44f;

            endTurnButton = CreateActionButton(buttonRow.transform, LocalizationService.Text("ui.button.end_turn", "End Turn"));
            endTurnButton.onClick.AddListener(() => onEndTurn?.Invoke());
            rerollButton = CreateActionButton(buttonRow.transform, LocalizationService.Text("ui.button.reroll", "Reroll"));
            rerollButton.onClick.AddListener(() => onReroll?.Invoke());
            rerollButtonObject = rerollButton.gameObject;

            CreateSectionHeader(parent, LocalizationService.Text("ui.panel.allies", "Allied Ledger"));
            GameObject alliesPanel = CreateInsetPanel("AlliedRosterPanel", parent, 126f);
            alliedRosterRoot = CreateRosterRoot(CreateScrollContentRoot(alliesPanel.transform, 6f));
            CreateSectionHeader(parent, LocalizationService.Text("ui.panel.enemies", "Enemy Ledger"));
            GameObject enemiesPanel = CreateInsetPanel("EnemyRosterPanel", parent, 184f);
            enemyRosterRoot = CreateRosterRoot(CreateScrollContentRoot(enemiesPanel.transform, 6f));

            CreateSectionHeader(parent, LocalizationService.Text("ui.panel.feed", "War Feed"));
            GameObject feedPanel = CreateInsetPanel("FeedPanel", parent, 108f);
            Transform feedRoot = CreateScrollContentRoot(feedPanel.transform, 8f);
            VerticalLayoutGroup feedLayout = feedRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            feedLayout.spacing = 4f;
            feedLayout.childControlHeight = true;
            feedLayout.childControlWidth = true;
            feedLayout.childForceExpandHeight = false;
            for (int index = 0; index < FeedLimit; index++)
            {
                Text feedLabel = CreateText(feedRoot, string.Empty, 12, index == 0 ? FontStyle.Bold : FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
                feedLabel.GetComponent<LayoutElement>().preferredHeight = 24f;
                feedLabels.Add(feedLabel);
            }

            instructionLabel = CreateText(feedRoot, string.Empty, 14, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.95f, 0.91f, 0.79f, 1f));
            instructionLabel.GetComponent<LayoutElement>().preferredHeight = 28f;
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
            accentRect.sizeDelta = new Vector2(8f, 0f);
            accentRect.anchoredPosition = Vector2.zero;
            forecastAccentImage = accentObject.GetComponent<Image>();
            forecastAccentImage.sprite = RuntimeSpriteLibrary.WhiteSprite;

            forecastHeaderLabel = CreateAbsoluteText(parent, new Vector2(26f, -18f), new Vector2(-20f, -14f), string.Empty, 17, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextGold);
            forecastTitleLabel = CreateAbsoluteText(parent, new Vector2(26f, -46f), new Vector2(-20f, -34f), string.Empty, 24, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            forecastSummaryLabel = CreateAbsoluteText(parent, new Vector2(26f, -80f), new Vector2(-20f, -62f), string.Empty, 17, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            forecastDetailLabel = CreateAbsoluteText(parent, new Vector2(26f, 48f), new Vector2(-20f, 64f), string.Empty, 15, FontStyle.Normal, TextAnchor.LowerLeft, BattleUiTheme.TextSecondary);
            forecastFooterLabel = CreateAbsoluteText(parent, new Vector2(26f, 18f), new Vector2(-20f, 34f), string.Empty, 13, FontStyle.Italic, TextAnchor.LowerLeft, new Color(0.95f, 0.86f, 0.72f, 1f));
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
            view.MetaLabel.text = model.RoleShortLabel + "  " + model.PositionLabel;
            view.StateLabel.text = model.StatusLabel + "  " + model.SkillLabel;
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

            Color baseColor = model.Faction == UnitFaction.Player
                ? new Color(0.11f, 0.17f, 0.27f, model.IsAlive ? 0.95f : 0.68f)
                : new Color(0.27f, 0.11f, 0.1f, model.IsAlive ? 0.95f : 0.68f);
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

            return new BattleForecastModel
            {
                Header = LocalizationService.Text("ui.panel.forecast", "Battle Forecast"),
                Title = string.IsNullOrEmpty(currentOverview.PhaseLabel)
                    ? LocalizationService.Text("ui.forecast.neutral.title", "Awaiting Orders")
                    : currentOverview.PhaseLabel,
                Summary = string.IsNullOrEmpty(currentOverview.TurnLabel)
                    ? LocalizationService.Text("ui.forecast.neutral.summary", "Review the field and choose your next move.")
                    : currentOverview.TurnLabel,
                Detail = string.IsNullOrEmpty(currentOverview.InstructionText)
                    ? latestFeed
                    : currentOverview.InstructionText,
                Footer = latestFeed,
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
            layout.spacing = 6f;
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
                new Vector2(0f, 52f),
                new Color(0.11f, 0.17f, 0.27f, 0.95f));
            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootObject.AddComponent<LayoutElement>().preferredHeight = 52f;

            Image background = rootObject.GetComponent<Image>();
            background.sprite = RuntimeSpriteLibrary.WhiteSprite;
            Outline outline = rootObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = new Color(0.38f, 0.31f, 0.24f, 0.75f);

            Button button = rootObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.06f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.1f);
            button.colors = colors;

            GameObject contentRoot = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contentRoot.transform.SetParent(rootObject.transform, false);
            RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(10f, 6f);
            contentRect.offsetMax = new Vector2(-10f, -6f);

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
            textLayout.spacing = 1f;
            textLayout.childControlHeight = true;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandHeight = false;

            Text nameLabel = CreateText(textColumn.transform, string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            EnableBestFit(nameLabel, 11, 14, true);
            nameLabel.GetComponent<LayoutElement>().preferredHeight = 16f;
            Text metaLabel = CreateText(textColumn.transform, string.Empty, 11, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextSecondary);
            EnableBestFit(metaLabel, 9, 11, true);
            metaLabel.GetComponent<LayoutElement>().preferredHeight = 14f;
            Text stateLabel = CreateText(textColumn.transform, string.Empty, 10, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.95f, 0.88f, 0.72f, 1f));
            EnableBestFit(stateLabel, 8, 10, true);
            stateLabel.GetComponent<LayoutElement>().preferredHeight = 14f;

            GameObject hpColumn = new GameObject("HpColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            hpColumn.transform.SetParent(contentRoot.transform, false);
            LayoutElement hpLayoutElement = hpColumn.GetComponent<LayoutElement>();
            hpLayoutElement.preferredWidth = 108f;
            VerticalLayoutGroup hpLayout = hpColumn.GetComponent<VerticalLayoutGroup>();
            hpLayout.spacing = 4f;
            hpLayout.childControlHeight = true;
            hpLayout.childControlWidth = true;
            hpLayout.childForceExpandHeight = false;
            hpLayout.childAlignment = TextAnchor.UpperRight;

            Text hpLabel = CreateText(hpColumn.transform, string.Empty, 11, FontStyle.Bold, TextAnchor.MiddleRight, BattleUiTheme.TextPrimary);
            EnableBestFit(hpLabel, 9, 11, true);
            hpLabel.GetComponent<LayoutElement>().preferredHeight = 16f;

            GameObject hpBarRoot = new GameObject("HpBar", typeof(RectTransform), typeof(LayoutElement));
            hpBarRoot.transform.SetParent(hpColumn.transform, false);
            hpBarRoot.GetComponent<LayoutElement>().preferredHeight = 10f;

            Image hpFill;
            CreateStretchUiBar(hpBarRoot.transform, out hpFill);

            return new RosterEntryView(rootObject, button, background, outline, nameLabel, metaLabel, stateLabel, hpLabel, hpFill);
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
            viewportImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
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
            trackImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
            trackImage.color = new Color(0.18f, 0.15f, 0.11f, 0.9f);

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(scrollbarObject.transform, false);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = new Vector2(1f, 1f);
            handleRect.offsetMax = new Vector2(-1f, -1f);

            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
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
            backImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
            backImage.color = new Color(0.12f, 0.09f, 0.08f, 0.95f);

            GameObject fillObject = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(backObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fillImage = fillObject.GetComponent<Image>();
            fillImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
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
            backImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
            backImage.color = new Color(0.12f, 0.09f, 0.08f, 0.95f);

            GameObject fillObject = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(backObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fillImage = fillObject.GetComponent<Image>();
            fillImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
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
            image.sprite = RuntimeSpriteLibrary.WhiteSprite;
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
            image.sprite = RuntimeSpriteLibrary.WhiteSprite;
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
            image.sprite = RuntimeSpriteLibrary.WhiteSprite;
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
            image.sprite = RuntimeSpriteLibrary.WhiteSprite;
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
            text.font = RuntimeSpriteLibrary.DefaultFont;
            text.fontSize = size;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

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
            text.font = RuntimeSpriteLibrary.DefaultFont;
            text.fontSize = size;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
            if (!singleLine)
            {
                return;
            }

            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.alignByGeometry = true;
        }

        private sealed class RosterEntryView
        {
            public RosterEntryView(
                GameObject root,
                Button button,
                Image background,
                Outline outline,
                Text nameLabel,
                Text metaLabel,
                Text stateLabel,
                Text hpLabel,
                Image hpFill)
            {
                Root = root;
                Button = button;
                Background = background;
                Outline = outline;
                NameLabel = nameLabel;
                MetaLabel = metaLabel;
                StateLabel = stateLabel;
                HpLabel = hpLabel;
                HpFill = hpFill;
            }

            public GameObject Root { get; }

            public Button Button { get; }

            public Image Background { get; }

            public Outline Outline { get; }

            public Text NameLabel { get; }

            public Text MetaLabel { get; }

            public Text StateLabel { get; }

            public Text HpLabel { get; }

            public Image HpFill { get; }

            public string UnitId { get; set; } = string.Empty;
        }
    }
}
