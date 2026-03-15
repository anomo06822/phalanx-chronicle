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
    internal sealed class BattleSelectedUnitView
    {
        private BattleSelectedUnitModel currentModel = new BattleSelectedUnitModel();
        private BattleLayoutMetrics currentLayoutMetrics;
        private GameObject rootObject;
        private RectTransform rootRect;
        private VerticalLayoutGroup rootLayout;
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
        private GameObject detailScrollRoot;
        private Button detailToggleButton;
        private HorizontalLayoutGroup identityLayout;
        private LayoutElement portraitLayout;
        private LayoutElement identityFactsLayout;
        private VerticalLayoutGroup vitalLayout;
        private LayoutElement hpBarLayout;
        private LayoutElement manaBarLayout;
        private VerticalLayoutGroup primaryFactsLayout;
        private GridLayoutGroup primaryFactsGrid;
        private LayoutElement primaryFactsGridLayout;
        private LayoutElement chipRowLayout;
        private VerticalLayoutGroup threatLayout;
        private LayoutElement threatChipRowLayout;
        private VerticalLayoutGroup detailLayout;
        private LayoutElement detailHeaderRowLayout;
        private HorizontalLayoutGroup detailHeaderLayout;
        private LayoutElement detailToggleLayout;
        private LayoutElement detailScrollLayout;
        private LayoutElement detailPanelLayout;
        private bool detailsExpanded;
        private string lastBoundUnitId = string.Empty;

        public Text NameLabel => nameLabel;

        public void Initialize(Transform canvasRoot)
        {
            BattleLayoutMetrics layoutMetrics = BattleHudLayoutPolicy.Evaluate(canvasRoot as RectTransform, 12f, 12f);
            currentLayoutMetrics = layoutMetrics;
            rootObject = BattleHudFactory.CreatePanel(
                "SelectedUnitPanel",
                canvasRoot,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(layoutMetrics.PanelOuterMargin, layoutMetrics.SidePanelAnchoredY),
                new Vector2(layoutMetrics.SelectedPanelWidth, layoutMetrics.SidePanelHeight),
                BattleUiTheme.PanelSurface);
            rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.pivot = new Vector2(0f, 0.5f);

            rootLayout = rootObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = layoutMetrics.PanelSectionSpacing;
            rootLayout.padding = new RectOffset(layoutMetrics.PanelPadding, layoutMetrics.PanelPadding, layoutMetrics.PanelPadding, layoutMetrics.PanelPadding);
            rootLayout.childControlHeight = true;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandHeight = false;

            BattleHudFactory.CreateSectionHeader(rootObject.transform, LocalizationService.Text("ui.panel.selected", "角色戰報"));

            GameObject identityPanel = BattleHudFactory.CreateInsetPanel("IdentityPanel", rootObject.transform, 176f, BattleUiTheme.PanelSelected);
            identityLayout = identityPanel.AddComponent<HorizontalLayoutGroup>();
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
            portraitLayout = portraitFrame.AddComponent<LayoutElement>();
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

            nameLabel = BattleHudFactory.CreateText(textRoot.transform, string.Empty, 22, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary, BattleTextRole.SingleLineTitle);
            BattleHudFactory.EnableBestFit(nameLabel, 16, 22, true);
            roleLabel = BattleHudFactory.CreateText(textRoot.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold, BattleTextRole.DenseMeta);
            positionLabel = BattleHudFactory.CreateText(textRoot.transform, string.Empty, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextMuted, BattleTextRole.TwoLineSummary);

            GameObject identityFacts = new GameObject("IdentityFacts", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            identityFacts.transform.SetParent(textRoot.transform, false);
            identityFactsLayout = identityFacts.GetComponent<LayoutElement>();
            identityFactsLayout.preferredHeight = 22f;
            HorizontalLayoutGroup identityFactsRowLayout = identityFacts.GetComponent<HorizontalLayoutGroup>();
            identityFactsRowLayout.spacing = 6f;
            identityFactsRowLayout.childControlHeight = true;
            identityFactsRowLayout.childControlWidth = false;
            identityFactsRowLayout.childForceExpandHeight = false;
            identityFactsRowLayout.childForceExpandWidth = false;
            identityFactsRoot = identityFacts.transform;

            GameObject vitalPanel = BattleHudFactory.CreateInsetPanel("VitalsPanel", rootObject.transform, 116f, new Color(0.16f, 0.13f, 0.1f, 0.96f));
            Transform vitalRoot = BattleHudFactory.CreateInsetContentRoot(vitalPanel.transform, 12f);
            vitalLayout = vitalRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            vitalLayout.spacing = 6f;
            vitalLayout.childControlHeight = true;
            vitalLayout.childControlWidth = true;
            vitalLayout.childForceExpandHeight = false;

            hpLabel = BattleHudFactory.CreateText(vitalRoot, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            BattleHudFactory.ApplyTextRole(hpLabel, BattleTextRole.DenseMeta);
            GameObject hpBarRoot = new GameObject("HpBarRoot", typeof(RectTransform), typeof(LayoutElement));
            hpBarRoot.transform.SetParent(vitalRoot, false);
            hpBarLayout = hpBarRoot.GetComponent<LayoutElement>();
            hpBarLayout.preferredHeight = 14f;
            BattleHudFactory.CreateStretchUiBar(hpBarRoot.transform, out hpFill);

            manaLabel = BattleHudFactory.CreateText(vitalRoot, string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            BattleHudFactory.ApplyTextRole(manaLabel, BattleTextRole.DenseMeta);
            GameObject manaBarRoot = new GameObject("ManaBarRoot", typeof(RectTransform), typeof(LayoutElement));
            manaBarRoot.transform.SetParent(vitalRoot, false);
            manaBarLayout = manaBarRoot.GetComponent<LayoutElement>();
            manaBarLayout.preferredHeight = 12f;
            BattleHudFactory.CreateStretchUiBar(manaBarRoot.transform, out manaFill);

            GameObject primaryFactsPanel = BattleHudFactory.CreateInsetPanel("PrimaryFactsPanel", rootObject.transform, 138f, new Color(0.15f, 0.13f, 0.11f, 0.96f));
            Transform factsContent = BattleHudFactory.CreateInsetContentRoot(primaryFactsPanel.transform, 12f);
            primaryFactsLayout = factsContent.gameObject.AddComponent<VerticalLayoutGroup>();
            primaryFactsLayout.spacing = 8f;
            primaryFactsLayout.childControlHeight = true;
            primaryFactsLayout.childControlWidth = true;
            primaryFactsLayout.childForceExpandHeight = false;

            Text primaryHeader = BattleHudFactory.CreateText(factsContent, LocalizationService.Text("ui.selected.primary_header", "首屏決策"), 13, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold, BattleTextRole.SingleLineTitle);

            GameObject factsGrid = new GameObject("FactsGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            factsGrid.transform.SetParent(factsContent, false);
            primaryFactsGridLayout = factsGrid.GetComponent<LayoutElement>();
            primaryFactsGridLayout.preferredHeight = 58f;
            primaryFactsGrid = factsGrid.GetComponent<GridLayoutGroup>();
            primaryFactsGrid.cellSize = new Vector2(118f, 24f);
            primaryFactsGrid.spacing = new Vector2(8f, 8f);
            primaryFactsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            primaryFactsGrid.constraintCount = 2;
            primaryFactsRoot = factsGrid.transform;

            GameObject chipRow = new GameObject("PrimaryChipRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            chipRow.transform.SetParent(factsContent, false);
            chipRowLayout = chipRow.GetComponent<LayoutElement>();
            chipRowLayout.preferredHeight = 24f;
            HorizontalLayoutGroup chipLayout = chipRow.GetComponent<HorizontalLayoutGroup>();
            chipLayout.spacing = 6f;
            chipLayout.childControlHeight = true;
            chipLayout.childControlWidth = false;
            chipLayout.childForceExpandHeight = false;
            chipLayout.childForceExpandWidth = false;
            chipRoot = chipRow.transform;

            GameObject threatPanel = BattleHudFactory.CreateInsetPanel("ThreatPanel", rootObject.transform, 136f, BattleUiTheme.PanelCommand);
            Transform threatRoot = BattleHudFactory.CreateInsetContentRoot(threatPanel.transform, 12f);
            threatLayout = threatRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            threatLayout.spacing = 6f;
            threatLayout.childControlHeight = true;
            threatLayout.childControlWidth = true;
            threatLayout.childForceExpandHeight = false;

            GameObject threatChipRow = new GameObject("ThreatChipRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            threatChipRow.transform.SetParent(threatRoot, false);
            threatChipRowLayout = threatChipRow.GetComponent<LayoutElement>();
            threatChipRowLayout.preferredHeight = 24f;
            HorizontalLayoutGroup threatChipLayout = threatChipRow.GetComponent<HorizontalLayoutGroup>();
            threatChipLayout.spacing = 6f;
            threatChipLayout.childControlHeight = true;
            threatChipLayout.childControlWidth = false;
            threatChipLayout.childForceExpandHeight = false;
            threatChipLayout.childForceExpandWidth = false;
            threatChipRoot = threatChipRow.transform;

            threatLineLabel = BattleHudFactory.CreateText(threatRoot, string.Empty, 13, FontStyle.Bold, TextAnchor.UpperLeft, BattleUiTheme.TextThreat, BattleTextRole.TwoLineSummary);

            equipmentSummaryLabel = BattleHudFactory.CreateText(threatRoot, string.Empty, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary, BattleTextRole.TwoLineSummary);

            GameObject detailPanel = BattleHudFactory.CreateInsetPanel("DetailPanel", rootObject.transform, BattlePanelHeightPolicy.SelectedDetailCollapsedHeight, BattleUiTheme.PanelInsetStrong);
            detailPanelLayout = detailPanel.GetComponent<LayoutElement>();
            detailPanelLayout.minHeight = BattlePanelHeightPolicy.SelectedDetailCollapsedHeight;
            Transform detailRoot = BattleHudFactory.CreateInsetContentRoot(detailPanel.transform, 12f);
            detailLayout = detailRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            detailLayout.spacing = 6f;
            detailLayout.childControlHeight = true;
            detailLayout.childControlWidth = true;
            detailLayout.childForceExpandHeight = false;

            GameObject detailHeaderRow = new GameObject("DetailHeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            detailHeaderRow.transform.SetParent(detailRoot, false);
            detailHeaderRowLayout = detailHeaderRow.GetComponent<LayoutElement>();
            detailHeaderRowLayout.preferredHeight = 24f;
            detailHeaderLayout = detailHeaderRow.GetComponent<HorizontalLayoutGroup>();
            detailHeaderLayout.spacing = 8f;
            detailHeaderLayout.childControlHeight = true;
            detailHeaderLayout.childControlWidth = true;
            detailHeaderLayout.childForceExpandHeight = false;
            detailHeaderLayout.childForceExpandWidth = false;

            detailHeaderLabel = BattleHudFactory.CreateText(detailHeaderRow.transform, string.Empty, 13, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold, BattleTextRole.SingleLineTitle);
            detailHeaderLabel.GetComponent<LayoutElement>().flexibleWidth = 1f;
            detailToggleButton = BattleHudFactory.CreateButton(detailHeaderRow.transform, LocalizationService.Text("ui.selected.details_expand", "展開"), false);
            detailToggleLayout = detailToggleButton.GetComponent<LayoutElement>();
            detailToggleLayout.preferredWidth = 92f;
            detailToggleLayout.preferredHeight = 28f;
            detailToggleButton.onClick.AddListener(ToggleDetails);
            detailToggleLabel = detailToggleButton.GetComponentInChildren<Text>();

            detailScrollRoot = BuildScrollableDetailContent(detailRoot, out detailLinesRoot);
            detailScrollLayout = detailScrollRoot.GetComponent<LayoutElement>();

            SetDetailsExpanded(false);
            Bind(new BattleSelectedUnitModel());
        }

        public void ApplyLayout(BattleLayoutMetrics metrics)
        {
            currentLayoutMetrics = metrics;
            if (rootRect == null)
            {
                return;
            }

            rootRect.anchoredPosition = new Vector2(metrics.PanelOuterMargin, metrics.SidePanelAnchoredY);
            rootRect.sizeDelta = new Vector2(metrics.SelectedPanelWidth, metrics.SidePanelHeight);
            rootLayout.spacing = metrics.PanelSectionSpacing;
            rootLayout.padding = new RectOffset(metrics.PanelPadding, metrics.PanelPadding, metrics.PanelPadding, metrics.PanelPadding);
            identityLayout.spacing = ScaleValue(12f);
            int identityPadding = Mathf.RoundToInt(ScaleValue(14f));
            identityLayout.padding = new RectOffset(identityPadding, identityPadding, identityPadding, identityPadding);
            portraitLayout.preferredWidth = ScaleValue(104f);
            portraitLayout.preferredHeight = ScaleValue(104f);
            identityFactsLayout.preferredHeight = ScaleValue(22f);
            vitalLayout.spacing = ScaleValue(6f);
            hpBarLayout.preferredHeight = ScaleValue(14f);
            manaBarLayout.preferredHeight = ScaleValue(12f);
            primaryFactsLayout.spacing = ScaleValue(8f);
            float factsSpacing = ScaleValue(8f);
            float factsCellHeight = ScaleValue(24f);
            float factsCellWidth = Mathf.Max(92f, (metrics.SelectedPanelWidth - metrics.PanelPadding * 2f - 32f - factsSpacing) * 0.5f);
            primaryFactsGrid.cellSize = new Vector2(factsCellWidth, factsCellHeight);
            primaryFactsGrid.spacing = new Vector2(factsSpacing, factsSpacing);
            primaryFactsGridLayout.preferredHeight = factsCellHeight * 2f + factsSpacing;
            chipRowLayout.preferredHeight = ScaleValue(24f);
            threatLayout.spacing = ScaleValue(6f);
            threatChipRowLayout.preferredHeight = ScaleValue(24f);
            detailLayout.spacing = ScaleValue(6f);
            detailHeaderRowLayout.preferredHeight = ScaleValue(24f);
            detailHeaderLayout.spacing = ScaleValue(8f);
            detailToggleLayout.preferredWidth = ScaleValue(92f);
            detailToggleLayout.preferredHeight = ScaleValue(28f);
            detailScrollLayout.minHeight = BattlePanelHeightPolicy.GetSelectedDetailScrollMinHeight(metrics.TextScale);
            detailScrollLayout.preferredHeight = BattlePanelHeightPolicy.GetSelectedDetailScrollMinHeight(metrics.TextScale);

            BattleHudFactory.ApplyResponsiveTextScale(rootObject.transform, metrics.TextScale);
            Bind(currentModel);
            SetDetailsExpanded(detailsExpanded);
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
            currentModel = model ?? new BattleSelectedUnitModel();
            if (!currentModel.HasSelection)
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

            if (!string.Equals(lastBoundUnitId, currentModel.UnitId, StringComparison.Ordinal))
            {
                lastBoundUnitId = currentModel.UnitId;
                SetDetailsExpanded(false);
            }

            UnitVisualProfile visualProfile = UnitVisualCatalog.GetProfile(currentModel.UnitId, currentModel.Faction, currentModel.Role);
            portraitImage.enabled = true;
            portraitImage.sprite = RuntimeSpriteLibrary.GetPortraitSprite(visualProfile);
            portraitBacking.color = visualProfile.PortraitBackdropColor;
            nameLabel.text = currentModel.DisplayName;
            roleLabel.text = currentModel.RoleLabel;
            positionLabel.text = string.Join(
                "\n",
                new[] { currentModel.PositionLabel, currentModel.TerrainName }
                    .Where(line => !string.IsNullOrWhiteSpace(line)));
            hpLabel.text = LocalizationService.Format("ui.label.hp_value", "HP {0}/{1}", currentModel.CurrentHp, currentModel.MaxHp);
            hpFill.fillAmount = currentModel.MaxHp <= 0 ? 0f : (float)currentModel.CurrentHp / currentModel.MaxHp;
            hpFill.color = hpFill.fillAmount > 0.55f
                ? new Color(0.39f, 0.81f, 0.42f, 1f)
                : hpFill.fillAmount > 0.3f
                    ? new Color(0.91f, 0.74f, 0.22f, 1f)
                    : new Color(0.88f, 0.35f, 0.28f, 1f);
            manaLabel.text = LocalizationService.Format("ui.label.mana_value", "士氣 {0}/{1}", currentModel.CurrentMana, currentModel.MaxMana);
            manaFill.fillAmount = currentModel.MaxMana <= 0 ? 0f : (float)currentModel.CurrentMana / currentModel.MaxMana;
            manaFill.color = manaFill.fillAmount > 0.55f
                ? new Color(0.38f, 0.78f, 0.95f, 1f)
                : manaFill.fillAmount > 0.3f
                    ? new Color(0.46f, 0.66f, 0.98f, 1f)
                    : new Color(0.52f, 0.42f, 0.85f, 1f);
            threatLineLabel.text = currentModel.ThreatLine;
            equipmentSummaryLabel.text = currentModel.EquipmentSummary;
            BattleHudFactory.RefreshTextRole(positionLabel, BattleTextRole.TwoLineSummary, ScaleValue(32f));
            BattleHudFactory.RefreshTextRole(threatLineLabel, BattleTextRole.TwoLineSummary, ScaleValue(34f));
            BattleHudFactory.RefreshTextRole(equipmentSummaryLabel, BattleTextRole.TwoLineSummary, ScaleValue(30f));
            detailHeaderLabel.text = string.IsNullOrWhiteSpace(currentModel.DetailHeader)
                ? LocalizationService.Text("ui.selected.details_header", "武裝與技能")
                : currentModel.DetailHeader;

            RebuildFacts(identityFactsRoot, currentModel.IdentityFacts, GetIdentityFactWidth(), ScaleValue(22f));
            RebuildFacts(primaryFactsRoot, currentModel.PrimaryFacts.Count > 0 ? currentModel.PrimaryFacts : currentModel.CombatFacts, GetPrimaryFactWidth(), ScaleValue(24f));
            RebuildChips(chipRoot, currentModel.PrimaryChips.Count > 0 ? currentModel.PrimaryChips : currentModel.StatusPills, ScaleValue(22f));
            BattleHudFactory.DestroyChildren(threatChipRoot);
            if (currentModel.ThreatChip != null && !string.IsNullOrWhiteSpace(currentModel.ThreatChip.Text))
            {
                BattleHudFactory.CreateAdaptiveChip(threatChipRoot, currentModel.ThreatChip, ScaleValue(24f));
            }

            BattleHudFactory.DestroyChildren(detailLinesRoot);
            foreach (string line in (currentModel.DetailLines ?? Array.Empty<string>()).Where(line => !string.IsNullOrWhiteSpace(line)))
            {
                Text detailLabel = BattleHudFactory.CreateText(detailLinesRoot, line, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary, BattleTextRole.BodyAuto);
                BattleHudFactory.RefreshTextRole(detailLabel, BattleTextRole.BodyAuto, ScaleValue(18f));
            }
        }

        private static GameObject BuildScrollableDetailContent(Transform parent, out Transform contentRoot)
        {
            GameObject scrollRoot = new GameObject("DetailScrollRoot", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollRoot.transform.SetParent(parent, false);
            LayoutElement layout = scrollRoot.GetComponent<LayoutElement>();
            layout.flexibleHeight = 1f;
            layout.minHeight = BattlePanelHeightPolicy.SelectedDetailScrollMinHeight;
            layout.preferredHeight = BattlePanelHeightPolicy.SelectedDetailScrollMinHeight;

            Image rootImage = scrollRoot.GetComponent<Image>();
            rootImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
            rootImage.color = new Color(1f, 1f, 1f, 0.01f);

            ScrollRect scrollRect = scrollRoot.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 24f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollRoot.transform, false);
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

            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            contentRoot = content.transform;
            return scrollRoot;
        }

        private static void RebuildFacts(Transform root, IReadOnlyList<HudFactModel> facts, float width, float height)
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

        private static void RebuildChips(Transform root, IReadOnlyList<HudChipModel> chips, float height)
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
            if (detailScrollRoot != null)
            {
                detailScrollRoot.SetActive(expanded);
            }

            if (detailPanelLayout != null)
            {
                if (expanded)
                {
                    detailPanelLayout.minHeight = BattlePanelHeightPolicy.GetSelectedDetailExpandedMinHeight(GetTextScale());
                    detailPanelLayout.preferredHeight = BattlePanelHeightPolicy.GetSelectedDetailExpandedMinHeight(GetTextScale());
                    detailPanelLayout.flexibleHeight = 1f;
                }
                else
                {
                    detailPanelLayout.minHeight = BattlePanelHeightPolicy.GetSelectedDetailCollapsedHeight(GetTextScale());
                    detailPanelLayout.preferredHeight = BattlePanelHeightPolicy.GetSelectedDetailCollapsedHeight(GetTextScale());
                    detailPanelLayout.flexibleHeight = 0f;
                }
            }

            if (detailToggleLabel != null)
            {
                detailToggleLabel.text = expanded
                    ? LocalizationService.Text("ui.selected.details_collapse", "收合")
                    : LocalizationService.Text("ui.selected.details_expand", "展開");
            }
        }

        private float GetIdentityFactWidth()
        {
            return Mathf.Clamp((currentLayoutMetrics.SelectedPanelWidth - currentLayoutMetrics.PanelPadding * 2f - 48f) * 0.5f, 88f, 106f);
        }

        private float GetPrimaryFactWidth()
        {
            return primaryFactsGrid != null ? primaryFactsGrid.cellSize.x : 116f;
        }

        private float GetTextScale()
        {
            return currentLayoutMetrics.TextScale > 0f ? currentLayoutMetrics.TextScale : 1f;
        }

        private float ScaleValue(float value)
        {
            return Mathf.Ceil(value * GetTextScale());
        }
    }
}
