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
        private const float BasePanelHeight = 836f;
        private const float ActionDockHeight = 292f;
        private const float ActionDockBottomMargin = 14f;
        private const float SidePanelBottomMargin = 16f;
        private const float SidePanelTopMargin = 22f;

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
            RectTransform canvasRect = canvasRoot as RectTransform;
            float panelHeight = CalculatePanelHeight(canvasRect);
            float anchoredY = CalculateSafeAnchoredY(canvasRect, panelHeight);
            rootObject = BattleHudFactory.CreatePanel(
                "SelectedUnitPanel",
                canvasRoot,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(18f, anchoredY),
                new Vector2(292f, panelHeight),
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

        private static float CalculatePanelHeight(RectTransform canvasRect)
        {
            if (canvasRect == null || canvasRect.rect.height <= 0f)
            {
                return BasePanelHeight;
            }

            float reservedBottom = ActionDockBottomMargin + ActionDockHeight + SidePanelBottomMargin;
            float usableHeight = canvasRect.rect.height - reservedBottom - SidePanelTopMargin;
            if (usableHeight < 260f)
            {
                usableHeight = 260f;
            }

            return Mathf.Min(BasePanelHeight, usableHeight);
        }

        private static float CalculateSafeAnchoredY(RectTransform canvasRect, float panelHeight)
        {
            if (canvasRect == null || canvasRect.rect.height <= 0f)
            {
                return 0f;
            }

            float reservedBottom = ActionDockBottomMargin + ActionDockHeight + SidePanelBottomMargin;
            return reservedBottom + panelHeight * 0.5f - canvasRect.rect.height * 0.5f;
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
}
