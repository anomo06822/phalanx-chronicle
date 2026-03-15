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

        private BattleOverviewModel currentOverview = new BattleOverviewModel();
        private IReadOnlyList<BattleRosterEntryModel> currentAlliedRoster = Array.Empty<BattleRosterEntryModel>();
        private IReadOnlyList<BattleRosterEntryModel> currentEnemyRoster = Array.Empty<BattleRosterEntryModel>();
        private IReadOnlyList<string> currentFeedEntries = Array.Empty<string>();
        private BattleLayoutMetrics currentLayoutMetrics;
        private GameObject rootObject;
        private RectTransform rootRect;
        private VerticalLayoutGroup rootLayout;
        private VerticalLayoutGroup summaryLayout;
        private GridLayoutGroup overviewFactGrid;
        private VerticalLayoutGroup objectiveLayout;
        private LayoutElement commandRowLayout;
        private HorizontalLayoutGroup commandLayout;
        private LayoutElement contentPanelLayout;
        private VerticalLayoutGroup contentLayout;
        private LayoutElement tabRowLayout;
        private HorizontalLayoutGroup tabLayout;
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
        private RectTransform alliedTabContentRect;
        private RectTransform enemyTabContentRect;
        private RectTransform feedTabContentRect;

        public bool IsRerollVisible => rerollButtonObject != null && rerollButtonObject.activeSelf;

        public string CurrentObjectiveText => objectivePrimaryLabel != null ? objectivePrimaryLabel.text : string.Empty;

        public void Initialize(Transform canvasRoot, Action onEndTurn, Action onReroll, Action onAutoModeRequested, int feedLimit)
        {
            this.feedLimit = Mathf.Max(1, feedLimit);

            BattleLayoutMetrics layoutMetrics = BattleHudLayoutPolicy.Evaluate(canvasRoot as RectTransform, 12f, 12f);
            currentLayoutMetrics = layoutMetrics;
            rootObject = BattleHudFactory.CreatePanel(
                "OverviewPanel",
                canvasRoot,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-layoutMetrics.PanelOuterMargin, layoutMetrics.SidePanelAnchoredY),
                new Vector2(layoutMetrics.RosterPanelWidth, layoutMetrics.SidePanelHeight),
                BattleUiTheme.PanelSurface);
            rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.pivot = new Vector2(1f, 0.5f);

            rootLayout = rootObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = layoutMetrics.PanelSectionSpacing;
            rootLayout.padding = new RectOffset(layoutMetrics.PanelPadding, layoutMetrics.PanelPadding, layoutMetrics.PanelPadding, layoutMetrics.PanelPadding);
            rootLayout.childControlHeight = true;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandHeight = false;

            BattleHudFactory.CreateSectionHeader(rootObject.transform, LocalizationService.Text("ui.panel.overview", "戰況總覽"));

            GameObject summaryPanel = BattleHudFactory.CreateInsetPanel("OverviewSummaryPanel", rootObject.transform, BattlePanelHeightPolicy.OverviewSummaryHeight, BattleUiTheme.PanelInsetStrong);
            Transform summaryRoot = BattleHudFactory.CreateInsetContentRoot(summaryPanel.transform, 10f);
            summaryLayout = summaryRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            summaryLayout.spacing = 2f;
            summaryLayout.childControlHeight = true;
            summaryLayout.childControlWidth = true;
            summaryLayout.childForceExpandHeight = false;

            stageLabel = BattleHudFactory.CreateText(summaryRoot, string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary, BattleTextRole.SingleLineTitle);
            seedLabel = BattleHudFactory.CreateText(summaryRoot, string.Empty, 10, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold, BattleTextRole.DenseMeta);
            phaseLabel = BattleHudFactory.CreateText(summaryRoot, string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary, BattleTextRole.SingleLineTitle);
            turnLabel = BattleHudFactory.CreateText(summaryRoot, string.Empty, 12, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextSecondary, BattleTextRole.DenseMeta);

            GameObject factGrid = new GameObject("FactGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            factGrid.transform.SetParent(summaryRoot, false);
            factGrid.GetComponent<LayoutElement>().preferredHeight = 44f;
            overviewFactGrid = factGrid.GetComponent<GridLayoutGroup>();
            overviewFactGrid.cellSize = new Vector2(128f, 19f);
            overviewFactGrid.spacing = new Vector2(6f, 4f);
            overviewFactGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            overviewFactGrid.constraintCount = 2;
            playerAliveLabel = BattleHudFactory.CreateText(factGrid.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.62f, 0.8f, 1f, 1f), BattleTextRole.DenseMeta);
            enemyAliveLabel = BattleHudFactory.CreateText(factGrid.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.66f, 0.58f, 1f), BattleTextRole.DenseMeta);
            readyLabel = BattleHudFactory.CreateText(factGrid.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.93f, 0.95f, 0.87f, 1f), BattleTextRole.DenseMeta);
            skillReadyLabel = BattleHudFactory.CreateText(factGrid.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold, BattleTextRole.DenseMeta);

            GameObject objectivePanel = BattleHudFactory.CreateInsetPanel("ObjectivePanel", rootObject.transform, BattlePanelHeightPolicy.OverviewObjectiveHeight, BattleUiTheme.PanelCommand);
            Transform objectiveRoot = BattleHudFactory.CreateInsetContentRoot(objectivePanel.transform, 12f);
            objectiveLayout = objectiveRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            objectiveLayout.spacing = 4f;
            objectiveLayout.childControlHeight = true;
            objectiveLayout.childControlWidth = true;
            objectiveLayout.childForceExpandHeight = false;
            objectivePrimaryLabel = BattleHudFactory.CreateText(objectiveRoot, string.Empty, 14, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextPrimary, BattleTextRole.TwoLineSummary);
            objectiveFailureLabel = BattleHudFactory.CreateText(objectiveRoot, string.Empty, 12, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextWarning, BattleTextRole.DenseMeta);
            instructionLabel = BattleHudFactory.CreateText(objectiveRoot, string.Empty, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary, BattleTextRole.TwoLineSummary);
            secondaryInstructionLabel = BattleHudFactory.CreateText(objectiveRoot, string.Empty, 12, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.78f, 0.88f, 0.98f, 1f), BattleTextRole.DenseMeta);

            GameObject commandRow = new GameObject("CommandRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            commandRow.transform.SetParent(rootObject.transform, false);
            commandRowLayout = commandRow.GetComponent<LayoutElement>();
            commandRowLayout.preferredHeight = BattlePanelHeightPolicy.OverviewCommandHeight;
            commandLayout = commandRow.GetComponent<HorizontalLayoutGroup>();
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
            contentPanelLayout = contentPanel.GetComponent<LayoutElement>();
            contentPanelLayout.flexibleHeight = 1f;
            float contentHeight = BattlePanelHeightPolicy.CalculateOverviewContentTargetHeight(layoutMetrics.SidePanelHeight, layoutMetrics.TextScale, layoutMetrics.PanelSectionSpacing);
            contentPanelLayout.minHeight = contentHeight;
            contentPanelLayout.preferredHeight = contentHeight;
            Transform contentRoot = BattleHudFactory.CreateInsetContentRoot(contentPanel.transform, 10f);
            contentLayout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;

            GameObject tabRow = new GameObject("TabRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            tabRow.transform.SetParent(contentRoot, false);
            tabRowLayout = tabRow.GetComponent<LayoutElement>();
            tabRowLayout.preferredHeight = BattlePanelHeightPolicy.OverviewTabHeight;
            tabLayout = tabRow.GetComponent<HorizontalLayoutGroup>();
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
            alliedTabContentRect = alliedTabContent.GetComponent<RectTransform>();
            enemyTabContentRect = enemyTabContent.GetComponent<RectTransform>();
            feedTabContentRect = feedTabContent.GetComponent<RectTransform>();
            for (int index = 0; index < this.feedLimit; index++)
            {
                Text feedLabel = BattleHudFactory.CreateText(feedRoot, index == 0 ? LocalizationService.Text("ui.feed.empty", "目前還沒有新的戰場紀錄。") : string.Empty, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary, BattleTextRole.TwoLineSummary);
                feedLabels.Add(feedLabel);
            }

            SetOverviewTab("allies");
        }

        public void ApplyLayout(BattleLayoutMetrics metrics)
        {
            currentLayoutMetrics = metrics;
            if (rootRect == null)
            {
                return;
            }

            rootRect.anchoredPosition = new Vector2(-metrics.PanelOuterMargin, metrics.SidePanelAnchoredY);
            rootRect.sizeDelta = new Vector2(metrics.RosterPanelWidth, metrics.SidePanelHeight);
            rootLayout.spacing = metrics.PanelSectionSpacing;
            rootLayout.padding = new RectOffset(metrics.PanelPadding, metrics.PanelPadding, metrics.PanelPadding, metrics.PanelPadding);
            summaryLayout.spacing = Mathf.Max(1f, metrics.PanelSectionSpacing - 3f);
            objectiveLayout.spacing = Mathf.Max(3f, metrics.PanelSectionSpacing - 2f);
            commandRowLayout.preferredHeight = ScaleValue(BattlePanelHeightPolicy.OverviewCommandHeight);
            commandLayout.spacing = ScaleValue(10f);
            contentPanelLayout.minHeight = BattlePanelHeightPolicy.CalculateOverviewContentTargetHeight(metrics.SidePanelHeight, metrics.TextScale, metrics.PanelSectionSpacing);
            contentPanelLayout.preferredHeight = contentPanelLayout.minHeight;
            contentLayout.spacing = ScaleValue(8f);
            tabRowLayout.preferredHeight = ScaleValue(BattlePanelHeightPolicy.OverviewTabHeight);
            tabLayout.spacing = ScaleValue(8f);
            overviewFactGrid.cellSize = new Vector2(GetFactCellWidth(metrics), ScaleValue(19f));
            overviewFactGrid.spacing = new Vector2(ScaleValue(6f), ScaleValue(4f));

            BattleHudFactory.ApplyResponsiveTextScale(rootObject.transform, metrics.TextScale);
            BindOverview(currentOverview);
            BindRoster(currentAlliedRoster, currentEnemyRoster, rosterSelectionHandler);
            BindFeed(currentFeedEntries);
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
            secondaryInstructionLabel.text = currentOverview.SecondaryInstructionText;
            secondaryInstructionLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(currentOverview.SecondaryInstructionText));
            BattleHudFactory.RefreshTextRole(objectivePrimaryLabel, BattleTextRole.TwoLineSummary, ScaleValue(34f));
            BattleHudFactory.RefreshTextRole(objectiveFailureLabel, BattleTextRole.DenseMeta, ScaleValue(16f));
            BattleHudFactory.RefreshTextRole(instructionLabel, BattleTextRole.TwoLineSummary, ScaleValue(32f));
            BattleHudFactory.RefreshTextRole(secondaryInstructionLabel, BattleTextRole.DenseMeta, ScaleValue(16f));
        }

        public void BindRoster(
            IReadOnlyList<BattleRosterEntryModel> alliedRoster,
            IReadOnlyList<BattleRosterEntryModel> enemyRoster,
            Action<string> onRosterSelected)
        {
            currentAlliedRoster = alliedRoster ?? Array.Empty<BattleRosterEntryModel>();
            currentEnemyRoster = enemyRoster ?? Array.Empty<BattleRosterEntryModel>();
            rosterSelectionHandler = onRosterSelected;
            BindRosterGroup(alliedRosterRoot, alliedRosterViews, currentAlliedRoster);
            BindRosterGroup(enemyRosterRoot, enemyRosterViews, currentEnemyRoster);
            UpdateOverviewTabLabels(currentAlliedRoster.Count, currentEnemyRoster.Count);
            RefreshTabLayouts();
        }

        public void BindFeed(IReadOnlyList<string> entries)
        {
            currentFeedEntries = entries ?? Array.Empty<string>();
            for (int index = 0; index < feedLabels.Count; index++)
            {
                feedLabels[index].text = index < currentFeedEntries.Count
                    ? currentFeedEntries[index]
                    : index == 0
                        ? LocalizationService.Text("ui.feed.empty", "目前還沒有新的戰場紀錄。")
                        : string.Empty;
            }

            RefreshTabLayouts();
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
            RefreshTabLayouts();
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

        private void RefreshTabLayouts()
        {
            Canvas.ForceUpdateCanvases();
            RefreshTabLayout(alliedTabContentRect, GetTextScale());
            RefreshTabLayout(enemyTabContentRect, GetTextScale());
            RefreshTabLayout(feedTabContentRect, GetTextScale());
            RectTransform rootRect = rootObject != null ? rootObject.GetComponent<RectTransform>() : null;
            if (rootRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            }
        }

        private static void RefreshTabLayout(RectTransform tabRect, float textScale)
        {
            if (tabRect == null)
            {
                return;
            }

            LayoutElement layout = tabRect.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minHeight = Mathf.Max(layout.minHeight, BattlePanelHeightPolicy.CalculateOverviewRosterViewportMinHeight(textScale));
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(tabRect);
            ScrollRect scrollRect = tabRect.GetComponent<ScrollRect>();
            if (scrollRect != null && scrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void BindRosterEntry(RosterEntryView view, BattleRosterEntryModel model)
        {
            view.UnitId = model.UnitId;
            view.NameLabel.text = string.IsNullOrWhiteSpace(model.RoleShortLabel)
                ? model.DisplayName
                : string.Concat(model.DisplayName, "  ", model.RoleShortLabel);
            view.RoleLabel.text = string.Empty;
            view.RoleLabel.gameObject.SetActive(false);
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
            view.NameLabel.color = model.IsAlive
                ? new Color(0.96f, 0.95f, 0.91f, 1f)
                : BattleUiTheme.TextMuted;
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
                new Vector2(0f, BattlePanelHeightPolicy.OverviewRosterEntryHeight),
                new Color(0.14f, 0.16f, 0.2f, 0.94f));
            LayoutElement rootLayout = rootObject.AddComponent<LayoutElement>();
            rootLayout.preferredHeight = BattlePanelHeightPolicy.OverviewRosterEntryHeight;

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
            contentRect.offsetMin = new Vector2(10f, 9f);
            contentRect.offsetMax = new Vector2(-10f, -9f);
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
            topLayout.childForceExpandWidth = true;

            Text nameLabel = BattleHudFactory.CreateText(topRow.transform, string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary, BattleTextRole.SingleLineTitle);
            LayoutElement nameLayout = nameLabel.GetComponent<LayoutElement>();
            nameLayout.minWidth = 104f;
            nameLayout.flexibleWidth = 1f;
            Text roleLabel = BattleHudFactory.CreateText(topRow.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleRight, BattleUiTheme.TextGold, BattleTextRole.DenseMeta);
            LayoutElement roleLayout = roleLabel.GetComponent<LayoutElement>();
            roleLayout.preferredWidth = 56f;

            Text positionLabel = BattleHudFactory.CreateText(textColumn.transform, string.Empty, 11, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextSecondary, BattleTextRole.DenseMeta);

            GameObject tagRow = new GameObject("TagRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            tagRow.transform.SetParent(textColumn.transform, false);
            tagRow.GetComponent<LayoutElement>().preferredHeight = 18f;
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
            hpColumn.GetComponent<LayoutElement>().preferredWidth = 84f;
            VerticalLayoutGroup hpLayout = hpColumn.GetComponent<VerticalLayoutGroup>();
            hpLayout.spacing = 4f;
            hpLayout.childControlHeight = true;
            hpLayout.childControlWidth = true;
            hpLayout.childForceExpandHeight = false;
            hpLayout.childForceExpandWidth = true;

            Text hpLabel = BattleHudFactory.CreateText(hpColumn.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleRight, BattleUiTheme.TextPrimary, BattleTextRole.DenseMeta);
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
            Text label = BattleHudFactory.CreateText(root.transform, string.Empty, 10, FontStyle.Bold, TextAnchor.MiddleCenter, BattleUiTheme.TextPrimary, BattleTextRole.ChipText);
            RectTransform textRect = label.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 2f);
            textRect.offsetMax = new Vector2(-6f, -2f);
            return new TagChipView(root, root.GetComponent<Image>(), label);
        }

        private float GetTextScale()
        {
            return currentLayoutMetrics.TextScale > 0f ? currentLayoutMetrics.TextScale : 1f;
        }

        private float ScaleValue(float value)
        {
            return Mathf.Ceil(value * GetTextScale());
        }

        private static float GetFactCellWidth(BattleLayoutMetrics metrics)
        {
            return Mathf.Max(104f, (metrics.RosterPanelWidth - metrics.PanelPadding * 2f - 32f - Mathf.Ceil(6f * metrics.TextScale)) * 0.5f);
        }
    }
}
