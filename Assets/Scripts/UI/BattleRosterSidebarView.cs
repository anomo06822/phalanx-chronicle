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
        private Text instructionLabel;
        private Text secondaryInstructionLabel;
        private Button endTurnButton;
        private Button rerollButton;
        private Button autoModeButton;
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

        public void Initialize(Transform canvasRoot, Action onEndTurn, Action onReroll, Action onAutoModeRequested, int feedLimit)
        {
            this.feedLimit = Mathf.Max(1, feedLimit);

            RectTransform canvasRect = canvasRoot as RectTransform;
            float panelHeight = BattleHudLayoutPolicy.CalculatePanelHeight(canvasRect);
            float anchoredY = BattleHudLayoutPolicy.CalculateSafeAnchoredY(canvasRect, panelHeight);
            rootObject = BattleHudFactory.CreatePanel(
                "OverviewPanel",
                canvasRoot,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-18f, anchoredY),
                new Vector2(BattleHudLayoutPolicy.RosterSidebarWidth, panelHeight),
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
            BattleHudFactory.SetOverflow(stageLabel, TextOverflowModes.Truncate, false);
            seedLabel = BattleHudFactory.CreateText(summaryRoot, string.Empty, 11, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            seedLabel.GetComponent<LayoutElement>().preferredHeight = 16f;
            BattleHudFactory.SetOverflow(seedLabel, TextOverflowModes.Truncate, false);
            phaseLabel = BattleHudFactory.CreateText(summaryRoot, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            phaseLabel.GetComponent<LayoutElement>().preferredHeight = 20f;
            BattleHudFactory.SetOverflow(phaseLabel, TextOverflowModes.Truncate, false);
            turnLabel = BattleHudFactory.CreateText(summaryRoot, string.Empty, 13, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextSecondary);
            turnLabel.GetComponent<LayoutElement>().preferredHeight = 18f;
            BattleHudFactory.SetOverflow(turnLabel, TextOverflowModes.Truncate, false);

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
            BattleHudFactory.SetOverflow(playerAliveLabel, TextOverflowModes.Truncate, false);
            BattleHudFactory.SetOverflow(enemyAliveLabel, TextOverflowModes.Truncate, false);
            BattleHudFactory.SetOverflow(readyLabel, TextOverflowModes.Truncate, false);
            BattleHudFactory.SetOverflow(skillReadyLabel, TextOverflowModes.Truncate, false);

            GameObject objectivePanel = BattleHudFactory.CreateInsetPanel("ObjectivePanel", rootObject.transform, 112f, BattleUiTheme.PanelCommand);
            Transform objectiveRoot = BattleHudFactory.CreateInsetContentRoot(objectivePanel.transform, 12f);
            VerticalLayoutGroup objectiveLayout = objectiveRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            objectiveLayout.spacing = 5f;
            objectiveLayout.childControlHeight = true;
            objectiveLayout.childControlWidth = true;
            objectiveLayout.childForceExpandHeight = false;
            objectivePrimaryLabel = BattleHudFactory.CreateText(objectiveRoot, string.Empty, 14, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary);
            objectivePrimaryLabel.GetComponent<LayoutElement>().preferredHeight = 20f;
            BattleHudFactory.SetOverflow(objectivePrimaryLabel, TextOverflowModes.Truncate, true);
            objectiveFailureLabel = BattleHudFactory.CreateText(objectiveRoot, string.Empty, 12, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextWarning);
            objectiveFailureLabel.GetComponent<LayoutElement>().preferredHeight = 18f;
            BattleHudFactory.SetOverflow(objectiveFailureLabel, TextOverflowModes.Truncate, true);
            instructionLabel = BattleHudFactory.CreateText(objectiveRoot, string.Empty, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            instructionLabel.GetComponent<LayoutElement>().preferredHeight = 20f;
            BattleHudFactory.SetOverflow(instructionLabel, TextOverflowModes.Truncate, true);
            secondaryInstructionLabel = BattleHudFactory.CreateText(objectiveRoot, string.Empty, 12, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.78f, 0.88f, 0.98f, 1f));
            secondaryInstructionLabel.GetComponent<LayoutElement>().preferredHeight = 16f;
            BattleHudFactory.SetOverflow(secondaryInstructionLabel, TextOverflowModes.Truncate, true);

            GameObject commandRow = new GameObject("CommandRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            commandRow.transform.SetParent(rootObject.transform, false);
            commandRow.GetComponent<LayoutElement>().preferredHeight = 46f;
            HorizontalLayoutGroup commandLayout = commandRow.GetComponent<HorizontalLayoutGroup>();
            commandLayout.spacing = 10f;
            commandLayout.childAlignment = TextAnchor.MiddleRight;
            commandLayout.childControlHeight = true;
            commandLayout.childControlWidth = true;
            commandLayout.childForceExpandHeight = true;
            commandLayout.childForceExpandWidth = true;
            endTurnButton = BattleHudFactory.CreateButton(commandRow.transform, LocalizationService.Text("ui.button.end_turn", "結束回合"), true);
            endTurnButton.onClick.AddListener(() => onEndTurn?.Invoke());
            LayoutElement endTurnLayout = endTurnButton.GetComponent<LayoutElement>();
            endTurnLayout.minWidth = 88f;
            endTurnLayout.preferredWidth = 0f;
            endTurnLayout.flexibleWidth = 1f;
            rerollButton = BattleHudFactory.CreateButton(commandRow.transform, LocalizationService.Text("ui.button.reroll", "重擲"), false);
            rerollButtonObject = rerollButton.gameObject;
            rerollButton.onClick.AddListener(() => onReroll?.Invoke());
            LayoutElement rerollLayout = rerollButton.GetComponent<LayoutElement>();
            rerollLayout.minWidth = 84f;
            rerollLayout.preferredWidth = 0f;
            rerollLayout.flexibleWidth = 1f;
            autoModeButton = BattleHudFactory.CreateButton(commandRow.transform, LocalizationService.Text("ui.button.auto_mode", "AI 自動"), false);
            autoModeButton.onClick.AddListener(() => onAutoModeRequested?.Invoke());
            LayoutElement autoModeLayout = autoModeButton.GetComponent<LayoutElement>();
            autoModeLayout.minWidth = 92f;
            autoModeLayout.preferredWidth = 0f;
            autoModeLayout.flexibleWidth = 1f;

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
                BattleHudFactory.SetOverflow(feedLabel, TextOverflowModes.Truncate, true);
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
            instructionLabel.text = overview.InstructionText;
            secondaryInstructionLabel.text = overview.SecondaryInstructionText;
            secondaryInstructionLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(overview.SecondaryInstructionText));
        }

        public void BindRoster(
            IReadOnlyList<BattleRosterEntryModel> alliedRoster,
            IReadOnlyList<BattleRosterEntryModel> enemyRoster,
            Action<string> onRosterSelected)
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

        public void SetAutoModeState(bool enabled)
        {
            if (autoModeButton == null)
            {
                return;
            }

            BattleHudFactory.SetButtonLabel(
                autoModeButton,
                enabled
                    ? LocalizationService.Text("ui.button.stop_auto_mode", "停止 AI")
                    : LocalizationService.Text("ui.button.auto_mode", "AI 自動"));
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
            return tag switch
            {
                BattleRosterTag.Ready => LocalizationService.Text("ui.roster.ready", "可動"),
                BattleRosterTag.Done => LocalizationService.Text("ui.roster.done", "已動"),
                BattleRosterTag.SkillReady => LocalizationService.Text("ui.roster.skill_ready", "技能可用"),
                BattleRosterTag.Threatening => LocalizationService.Text("ui.roster.threatening", "威脅"),
                BattleRosterTag.LowHp => LocalizationService.Text("ui.roster.low_hp", "危急"),
                BattleRosterTag.Exposed => LocalizationService.Text("ui.threat.exposed_short", "暴露"),
                _ => tag.ToString(),
            };
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
            accentRect.anchorMin = Vector2.zero;
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
            BattleHudFactory.SetOverflow(nameLabel, TextOverflowModes.Truncate, false);
            Text roleLabel = BattleHudFactory.CreateText(topRow.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleRight, BattleUiTheme.TextGold);
            roleLabel.GetComponent<LayoutElement>().preferredWidth = 56f;
            BattleHudFactory.SetOverflow(roleLabel, TextOverflowModes.Truncate, false);

            Text positionLabel = BattleHudFactory.CreateText(textColumn.transform, string.Empty, 11, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextSecondary);
            positionLabel.GetComponent<LayoutElement>().preferredHeight = 16f;
            BattleHudFactory.SetOverflow(positionLabel, TextOverflowModes.Truncate, false);

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
            BattleHudFactory.SetOverflow(hpLabel, TextOverflowModes.Truncate, false);
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
            BattleHudFactory.SetOverflow(label, TextOverflowModes.Truncate, false);
            return new TagChipView(root, root.GetComponent<Image>(), label);
        }
    }
}
