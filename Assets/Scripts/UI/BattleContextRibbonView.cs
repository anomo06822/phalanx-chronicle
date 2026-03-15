using System;
using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;

namespace PhalanxChronicle.UI
{
    internal sealed class BattleContextRibbonView
    {
        private BattleForecastModel currentModel = new BattleForecastModel();
        private GameObject rootObject;
        private RectTransform rootRect;
        private Image accentImage;
        private Text headerLabel;
        private Text titleLabel;
        private Transform factRoot;
        private Text primaryLineLabel;
        private Text secondaryLineLabel;
        private Transform chipRoot;
        private VerticalLayoutGroup contentLayout;
        private LayoutElement headerLayout;
        private LayoutElement titleLayout;
        private LayoutElement factsRowLayout;
        private LayoutElement primaryLineLayout;
        private LayoutElement secondaryLineLayout;
        private LayoutElement chipRowLayout;

        public void Initialize(Transform canvasRoot)
        {
            rootObject = BattleHudFactory.CreatePanel(
                "BattleContextRibbon",
                canvasRoot,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -12f),
                new Vector2(792f, 118f),
                BattleUiTheme.PanelForecast);
            rootRect = rootObject.GetComponent<RectTransform>();
            HorizontalLayoutGroup shellLayout = rootObject.AddComponent<HorizontalLayoutGroup>();
            shellLayout.spacing = 0f;
            shellLayout.padding = new RectOffset(0, 0, 0, 0);
            shellLayout.childControlHeight = true;
            shellLayout.childControlWidth = true;
            shellLayout.childForceExpandHeight = true;
            shellLayout.childForceExpandWidth = false;

            GameObject accentPanel = new GameObject("Accent", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            accentPanel.transform.SetParent(rootObject.transform, false);
            accentPanel.GetComponent<LayoutElement>().preferredWidth = 6f;
            accentImage = accentPanel.GetComponent<Image>();
            accentImage.sprite = RuntimeSpriteLibrary.WhiteSprite;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            content.transform.SetParent(rootObject.transform, false);
            content.GetComponent<LayoutElement>().flexibleWidth = 1f;
            contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4f;
            contentLayout.padding = new RectOffset(14, 14, 10, 10);
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;

            headerLabel = BattleHudFactory.CreateText(content.transform, string.Empty, 11, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            headerLayout = headerLabel.GetComponent<LayoutElement>();
            headerLayout.preferredHeight = 14f;
            BattleHudFactory.SetOverflow(headerLabel, TextOverflowModes.Truncate, false);

            titleLabel = BattleHudFactory.CreateText(content.transform, string.Empty, 19, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            titleLayout = titleLabel.GetComponent<LayoutElement>();
            titleLayout.preferredHeight = 22f;
            BattleHudFactory.SetOverflow(titleLabel, TextOverflowModes.Truncate, false);

            GameObject factsRow = new GameObject("FactsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            factsRow.transform.SetParent(content.transform, false);
            factsRowLayout = factsRow.GetComponent<LayoutElement>();
            factsRowLayout.preferredHeight = 24f;
            HorizontalLayoutGroup factsLayout = factsRow.GetComponent<HorizontalLayoutGroup>();
            factsLayout.spacing = 6f;
            factsLayout.childControlHeight = true;
            factsLayout.childControlWidth = false;
            factsLayout.childForceExpandHeight = false;
            factsLayout.childForceExpandWidth = false;
            factRoot = factsRow.transform;

            primaryLineLabel = BattleHudFactory.CreateText(content.transform, string.Empty, 13, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            primaryLineLayout = primaryLineLabel.GetComponent<LayoutElement>();
            primaryLineLayout.preferredHeight = 16f;
            BattleHudFactory.SetOverflow(primaryLineLabel, TextOverflowModes.Truncate, true);

            secondaryLineLabel = BattleHudFactory.CreateText(content.transform, string.Empty, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            secondaryLineLayout = secondaryLineLabel.GetComponent<LayoutElement>();
            secondaryLineLayout.preferredHeight = 16f;
            BattleHudFactory.SetOverflow(secondaryLineLabel, TextOverflowModes.Truncate, true);

            GameObject chipRow = new GameObject("ChipRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            chipRow.transform.SetParent(content.transform, false);
            chipRowLayout = chipRow.GetComponent<LayoutElement>();
            chipRowLayout.preferredHeight = 22f;
            HorizontalLayoutGroup chipLayout = chipRow.GetComponent<HorizontalLayoutGroup>();
            chipLayout.spacing = 6f;
            chipLayout.childControlHeight = true;
            chipLayout.childControlWidth = false;
            chipLayout.childForceExpandHeight = false;
            chipLayout.childForceExpandWidth = false;
            chipRoot = chipRow.transform;
        }

        public void ApplyLayout(BattleLayoutMetrics metrics)
        {
            if (rootRect == null)
            {
                return;
            }

            rootRect.sizeDelta = new Vector2(metrics.ContextRibbonWidth, metrics.ContextRibbonHeight);
            int horizontalPadding = Mathf.Max(10, metrics.PanelPadding - 2);
            int verticalPadding = Mathf.Max(8, Mathf.RoundToInt(metrics.PanelPadding * 0.65f));
            contentLayout.padding = new RectOffset(horizontalPadding, horizontalPadding, verticalPadding, verticalPadding);
            contentLayout.spacing = Mathf.Max(3f, metrics.PanelSectionSpacing - 1f);

            BattleHudFactory.ApplyResponsiveTextScale(rootObject.transform, metrics.TextScale);
            headerLayout.preferredHeight = Mathf.Ceil(14f * metrics.TextScale);
            titleLayout.preferredHeight = Mathf.Ceil(22f * metrics.TextScale);
            factsRowLayout.preferredHeight = Mathf.Ceil(24f * metrics.TextScale);
            primaryLineLayout.preferredHeight = Mathf.Ceil(16f * metrics.TextScale);
            secondaryLineLayout.preferredHeight = Mathf.Ceil(16f * metrics.TextScale);
            chipRowLayout.preferredHeight = Mathf.Ceil(22f * metrics.TextScale);

            Bind(currentModel);
        }

        public void SetVisible(bool visible)
        {
            if (rootObject != null)
            {
                rootObject.SetActive(visible);
            }
        }

        public void Bind(BattleForecastModel model)
        {
            currentModel = model ?? new BattleForecastModel();
            headerLabel.text = currentModel.Header;
            titleLabel.text = currentModel.Title;
            primaryLineLabel.text = currentModel.PrimaryLine;
            secondaryLineLabel.text = string.Join(
                " · ",
                (currentModel.SecondaryLines ?? Array.Empty<string>())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Take(2));
            secondaryLineLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(secondaryLineLabel.text));
            accentImage.color = currentModel.AccentColor;
            RebuildFacts(currentModel.OutcomeFacts);
            RebuildChips(currentModel.RiskChip, currentModel.CommitChip);
        }

        private void RebuildFacts(IReadOnlyList<HudFactModel> facts)
        {
            BattleHudFactory.DestroyChildren(factRoot);
            if (facts == null)
            {
                return;
            }

            foreach (HudFactModel fact in facts.Where(fact => fact != null && !string.IsNullOrWhiteSpace(fact.Value)).Take(3))
            {
                BattleHudFactory.CreateFactCard(factRoot, fact, 96f, 22f);
            }
        }

        private void RebuildChips(HudChipModel riskChip, HudChipModel commitChip)
        {
            BattleHudFactory.DestroyChildren(chipRoot);
            if (riskChip != null && !string.IsNullOrWhiteSpace(riskChip.Text))
            {
                BattleHudFactory.CreateAdaptiveChip(chipRoot, riskChip, 24f);
            }

            if (commitChip != null && !string.IsNullOrWhiteSpace(commitChip.Text))
            {
                BattleHudFactory.CreateAdaptiveChip(chipRoot, commitChip, 24f);
            }
        }
    }
}
