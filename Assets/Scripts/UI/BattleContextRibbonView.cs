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
        private GameObject rootObject;
        private Image accentImage;
        private Text headerLabel;
        private Text titleLabel;
        private Transform factRoot;
        private Text primaryLineLabel;
        private Text secondaryLineLabel;
        private Transform chipRoot;

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
            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4f;
            contentLayout.padding = new RectOffset(14, 14, 10, 10);
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;

            headerLabel = BattleHudFactory.CreateText(content.transform, string.Empty, 11, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextGold);
            headerLabel.GetComponent<LayoutElement>().preferredHeight = 14f;
            BattleHudFactory.SetOverflow(headerLabel, TextOverflowModes.Truncate, false);

            titleLabel = BattleHudFactory.CreateText(content.transform, string.Empty, 19, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            titleLabel.GetComponent<LayoutElement>().preferredHeight = 22f;
            BattleHudFactory.SetOverflow(titleLabel, TextOverflowModes.Truncate, false);

            GameObject factsRow = new GameObject("FactsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            factsRow.transform.SetParent(content.transform, false);
            factsRow.GetComponent<LayoutElement>().preferredHeight = 24f;
            HorizontalLayoutGroup factsLayout = factsRow.GetComponent<HorizontalLayoutGroup>();
            factsLayout.spacing = 6f;
            factsLayout.childControlHeight = true;
            factsLayout.childControlWidth = false;
            factsLayout.childForceExpandHeight = false;
            factsLayout.childForceExpandWidth = false;
            factRoot = factsRow.transform;

            primaryLineLabel = BattleHudFactory.CreateText(content.transform, string.Empty, 13, FontStyle.Bold, TextAnchor.MiddleLeft, BattleUiTheme.TextPrimary);
            primaryLineLabel.GetComponent<LayoutElement>().preferredHeight = 16f;
            BattleHudFactory.SetOverflow(primaryLineLabel, TextOverflowModes.Truncate, true);

            secondaryLineLabel = BattleHudFactory.CreateText(content.transform, string.Empty, 12, FontStyle.Normal, TextAnchor.UpperLeft, BattleUiTheme.TextSecondary);
            secondaryLineLabel.GetComponent<LayoutElement>().preferredHeight = 16f;
            BattleHudFactory.SetOverflow(secondaryLineLabel, TextOverflowModes.Truncate, true);

            GameObject chipRow = new GameObject("ChipRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            chipRow.transform.SetParent(content.transform, false);
            chipRow.GetComponent<LayoutElement>().preferredHeight = 22f;
            HorizontalLayoutGroup chipLayout = chipRow.GetComponent<HorizontalLayoutGroup>();
            chipLayout.spacing = 6f;
            chipLayout.childControlHeight = true;
            chipLayout.childControlWidth = false;
            chipLayout.childForceExpandHeight = false;
            chipLayout.childForceExpandWidth = false;
            chipRoot = chipRow.transform;
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
            BattleForecastModel ribbonModel = model ?? new BattleForecastModel();
            headerLabel.text = ribbonModel.Header;
            titleLabel.text = ribbonModel.Title;
            primaryLineLabel.text = ribbonModel.PrimaryLine;
            secondaryLineLabel.text = string.Join(
                " · ",
                (ribbonModel.SecondaryLines ?? Array.Empty<string>())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Take(2));
            secondaryLineLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(secondaryLineLabel.text));
            accentImage.color = ribbonModel.AccentColor;
            RebuildFacts(ribbonModel.OutcomeFacts);
            RebuildChips(ribbonModel.RiskChip, ribbonModel.CommitChip);
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
