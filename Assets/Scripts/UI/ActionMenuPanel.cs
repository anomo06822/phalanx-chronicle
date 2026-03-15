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
    public class BattleActionDockView : MonoBehaviour
    {
        private const float MaxDockWidth = 1120f;
        private const float MinDockWidth = 540f;
        private const float ActionDockBottomMargin = 14f;
        private static readonly Color DockHeaderTextColor = new Color(0.94f, 0.88f, 0.74f, 1f);
        private static readonly Color CardPrimaryTextColor = new Color(0.98f, 0.97f, 0.94f, 1f);
        private static readonly Color CardSecondaryTextColor = new Color(0.94f, 0.96f, 0.99f, 1f);
        private static readonly Color CardPrimaryDetailColor = new Color(0.21f, 0.13f, 0.07f, 1f);
        private static readonly Color CardSecondaryDetailColor = new Color(0.85f, 0.89f, 0.94f, 1f);
        private static readonly Color CardPrimaryMutedColor = new Color(0.46f, 0.35f, 0.24f, 1f);
        private static readonly Color CardSecondaryMutedColor = new Color(0.64f, 0.67f, 0.72f, 1f);
        private static readonly Color CardPrimaryRiskColor = new Color(0.45f, 0.17f, 0.08f, 1f);
        private static readonly Color CardSecondaryRiskColor = new Color(1f, 0.86f, 0.69f, 1f);

        protected GameObject rootObject;
        protected RectTransform rootRect;
        protected Text modeLabel;
        protected Text contextHintLabel;

        private BattleActionMenuModel currentModel = new BattleActionMenuModel();
        private BattleLayoutMetrics currentLayoutMetrics;
        private Action onAttackHandler;
        private Action onSkillHandler;
        private Action onWaitHandler;
        private Action onBackHandler;
        private VerticalLayoutGroup rootLayout;
        private LayoutElement headerRowLayout;
        private HorizontalLayoutGroup headerLayout;
        private LayoutElement modePanelLayout;
        private LayoutElement primaryRowLayout;
        private LayoutElement secondaryRowLayout;
        private ActionCardView attackButtonView;
        private ActionCardView skillButtonView;
        private ActionCardView waitButtonView;
        private ActionCardView backButtonView;

        public bool IsVisible => rootObject != null && rootObject.activeSelf;

        public string CurrentModeText => modeLabel != null ? modeLabel.text : string.Empty;

        public bool IsBackEnabled => backButtonView != null && backButtonView.Button.interactable;

        public bool IsSkillEnabled => skillButtonView != null && skillButtonView.Button.interactable;

        public void Initialize(Transform canvasRoot)
        {
            BattleLayoutMetrics layoutMetrics = BattleHudLayoutPolicy.Evaluate(canvasRoot as RectTransform, 12f, 12f);
            currentLayoutMetrics = layoutMetrics;
            float dockWidth = Mathf.Clamp(layoutMetrics.ActionDockWidth, MinDockWidth, MaxDockWidth);

            rootObject = CreatePanel(
                "ActionDock",
                canvasRoot,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, ActionDockBottomMargin),
                new Vector2(dockWidth, layoutMetrics.ActionDockHeight));
            rootRect = rootObject.GetComponent<RectTransform>();

            rootLayout = rootObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = BattleUiTheme.Space8;
            rootLayout.padding = new RectOffset(14, 14, 12, 12);
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlHeight = true;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.childForceExpandWidth = true;

            GameObject headerRow = new GameObject("HeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            headerRow.transform.SetParent(rootObject.transform, false);
            headerRowLayout = headerRow.GetComponent<LayoutElement>();
            headerRowLayout.preferredHeight = 34f;
            headerLayout = headerRow.GetComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = BattleUiTheme.Space8;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlHeight = true;
            headerLayout.childControlWidth = false;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childForceExpandWidth = false;

            GameObject modePanel = CreateInsetPanel("ActionModePanel", headerRow.transform, 30f, BattleUiTheme.PanelInsetStrong);
            modePanelLayout = modePanel.GetComponent<LayoutElement>();
            modePanelLayout.preferredWidth = 152f;
            modeLabel = CreateText(modePanel.transform, string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleCenter, DockHeaderTextColor);
            BattleHudFactory.ApplyTextRole(modeLabel, BattleTextRole.SingleLineTitle);
            RectTransform modeRect = modeLabel.GetComponent<RectTransform>();
            modeRect.anchorMin = Vector2.zero;
            modeRect.anchorMax = Vector2.one;
            modeRect.offsetMin = new Vector2(10f, 2f);
            modeRect.offsetMax = new Vector2(-10f, -2f);

            contextHintLabel = CreateText(headerRow.transform, string.Empty, 12, FontStyle.Normal, TextAnchor.MiddleLeft, BattleUiTheme.TextSecondary);
            LayoutElement contextLayout = contextHintLabel.GetComponent<LayoutElement>();
            contextLayout.flexibleWidth = 1f;
            BattleHudFactory.ApplyTextRole(contextHintLabel, BattleTextRole.TwoLineSummary);

            GameObject actionRow = CreateRow("PrimaryActionRow", rootObject.transform, 128f);
            primaryRowLayout = actionRow.GetComponent<LayoutElement>();
            attackButtonView = CreateCard(actionRow.transform, true);
            skillButtonView = CreateCard(actionRow.transform, true);
            GameObject secondaryRow = CreateRow("SecondaryActionRow", rootObject.transform, 128f);
            secondaryRowLayout = secondaryRow.GetComponent<LayoutElement>();
            waitButtonView = CreateCard(secondaryRow.transform, false);
            backButtonView = CreateCard(secondaryRow.transform, false);

            ApplyLayout(layoutMetrics);
            Hide();
        }

        internal void ApplyLayout(BattleLayoutMetrics metrics)
        {
            currentLayoutMetrics = metrics;
            if (rootRect == null)
            {
                return;
            }

            rootRect.anchoredPosition = new Vector2(0f, ActionDockBottomMargin);
            rootRect.sizeDelta = new Vector2(Mathf.Clamp(metrics.ActionDockWidth, MinDockWidth, MaxDockWidth), metrics.ActionDockHeight);
            int horizontalPadding = Mathf.RoundToInt(14f * metrics.TextScale);
            int verticalPadding = Mathf.RoundToInt(12f * metrics.TextScale);
            rootLayout.padding = new RectOffset(horizontalPadding, horizontalPadding, verticalPadding, verticalPadding);
            rootLayout.spacing = Mathf.Ceil(BattleUiTheme.Space8 * metrics.TextScale);
            headerRowLayout.preferredHeight = Mathf.Ceil(34f * metrics.TextScale);
            headerLayout.spacing = Mathf.Ceil(BattleUiTheme.Space8 * metrics.TextScale);
            modePanelLayout.preferredWidth = Mathf.Ceil(152f * metrics.TextScale);

            float remainingHeight = metrics.ActionDockHeight -
                                    rootLayout.padding.top -
                                    rootLayout.padding.bottom -
                                    rootLayout.spacing * 2f -
                                    headerRowLayout.preferredHeight;
            float rowHeight = Mathf.Max(104f, remainingHeight * 0.5f);
            primaryRowLayout.preferredHeight = rowHeight;
            secondaryRowLayout.preferredHeight = rowHeight;

            BattleHudFactory.ApplyResponsiveTextScale(rootObject.transform, metrics.TextScale);
            if (currentModel != null)
            {
                Show(currentModel, onAttackHandler, onSkillHandler, onWaitHandler, onBackHandler);
            }
        }

        public void Show(
            BattleActionMenuModel model,
            Action onAttack,
            Action onSkill,
            Action onWait,
            Action onBack)
        {
            if (rootObject == null)
            {
                return;
            }

            currentModel = model ?? new BattleActionMenuModel();
            onAttackHandler = onAttack;
            onSkillHandler = onSkill;
            onWaitHandler = onWait;
            onBackHandler = onBack;
            rootObject.SetActive(true);
            modeLabel.text = currentModel.ModeLabel;
            contextHintLabel.text = currentModel.ContextHint;
            contextHintLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(contextHintLabel.text));

            BindCard(
                attackButtonView,
                GetAction(currentModel, BattleActionDescriptorType.Attack),
                LocalizationService.Text("ui.button.attack", "攻擊"),
                onAttack);
            BindCard(
                skillButtonView,
                GetAction(currentModel, BattleActionDescriptorType.Skill),
                LocalizationService.Text("ui.button.skill", "技能"),
                onSkill);
            BindCard(
                waitButtonView,
                GetAction(currentModel, BattleActionDescriptorType.Wait),
                LocalizationService.Text("ui.button.wait", "待命"),
                onWait);
            BindCard(
                backButtonView,
                GetAction(currentModel, BattleActionDescriptorType.Back),
                LocalizationService.Text("ui.button.back", "返回"),
                onBack);
        }

        public void Hide()
        {
            if (rootObject != null)
            {
                rootObject.SetActive(false);
            }
        }

        private static BattleActionDescriptor GetAction(BattleActionMenuModel model, BattleActionDescriptorType type)
        {
            return model?.Actions != null
                ? model.Actions.FirstOrDefault(action => action != null && action.Type == type)
                : null;
        }

        private static GameObject CreateRow(string name, Transform parent, float height)
        {
            GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = height;
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = BattleUiTheme.Space12;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            return row;
        }

        private static void BindCard(ActionCardView view, BattleActionDescriptor descriptor, string fallbackLabel, Action onClick)
        {
            if (view == null)
            {
                return;
            }

            string label = descriptor != null && !string.IsNullOrWhiteSpace(descriptor.Label) ? descriptor.Label : fallbackLabel;
            view.TitleLabel.text = label;
            view.ReasonLabel.text = descriptor != null && !string.IsNullOrWhiteSpace(descriptor.Reason)
                ? descriptor.Reason
                : LocalizationService.Text("ui.action_menu.empty_detail", " ");
            view.OutcomeLabel.text = descriptor != null && !string.IsNullOrWhiteSpace(descriptor.OutcomeLine)
                ? descriptor.OutcomeLine
                : LocalizationService.Text("ui.action_menu.empty_detail", " ");
            view.RiskLabel.text = descriptor != null && descriptor.RiskChip != null
                ? descriptor.RiskChip.Text
                : string.Empty;
            view.RiskLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(view.RiskLabel.text));
            RebuildMetricRow(view.MetricRoot, descriptor != null ? descriptor.MetricChips : null);
            BattleHudFactory.RefreshTextRole(view.ReasonLabel, BattleTextRole.DenseMeta, 16f);
            BattleHudFactory.RefreshTextRole(view.OutcomeLabel, BattleTextRole.TwoLineSummary, 34f);
            BattleHudFactory.RefreshTextRole(view.RiskLabel, BattleTextRole.DenseMeta, 16f);

            bool interactable = descriptor != null && descriptor.IsEnabled;
            if (descriptor == null)
            {
                interactable = false;
            }
            else if (descriptor.Type == BattleActionDescriptorType.Wait)
            {
                interactable = descriptor.IsEnabled;
            }

            view.Button.interactable = interactable;
            view.Button.onClick.RemoveAllListeners();
            view.Button.onClick.AddListener(() => onClick?.Invoke());

            bool primary = descriptor == null || descriptor.Priority == BattleActionDescriptorPriority.Primary;
            Color normalColor = primary ? BattleUiTheme.ButtonPrimary : BattleUiTheme.ButtonSecondary;
            Color disabledColor = primary ? new Color(0.18f, 0.17f, 0.16f, 0.97f) : new Color(0.12f, 0.13f, 0.16f, 0.92f);
            view.Background.color = interactable ? normalColor : disabledColor;
            view.Outline.effectColor = interactable
                ? (primary ? new Color(0.34f, 0.22f, 0.08f, 0.66f) : new Color(0.24f, 0.33f, 0.48f, 0.6f))
                : new Color(0.18f, 0.18f, 0.2f, 0.42f);
            view.TitleLabel.color = primary
                ? (interactable ? CardPrimaryTextColor : CardPrimaryMutedColor)
                : (interactable ? CardSecondaryTextColor : CardSecondaryMutedColor);
            Color detailColor = primary ? CardPrimaryDetailColor : CardSecondaryDetailColor;
            Color disabledDetailColor = primary ? CardPrimaryMutedColor : CardSecondaryMutedColor;
            view.ReasonLabel.color = interactable ? detailColor : disabledDetailColor;
            view.OutcomeLabel.color = interactable ? detailColor : disabledDetailColor;
            view.RiskLabel.color = interactable
                ? (primary ? CardPrimaryRiskColor : CardSecondaryRiskColor)
                : disabledDetailColor;
        }

        private static void RebuildMetricRow(Transform root, IReadOnlyList<HudChipModel> chips)
        {
            if (root == null)
            {
                return;
            }

            for (int index = root.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.Destroy(root.GetChild(index).gameObject);
            }

            if (chips == null)
            {
                return;
            }

            foreach (HudChipModel chip in chips.Where(chip => chip != null && !string.IsNullOrWhiteSpace(chip.Text)).Take(4))
            {
                GameObject chipObject = CreateInsetPanel("MetricChip", root, 22f, chip.BackgroundColor);
                LayoutElement layout = chipObject.GetComponent<LayoutElement>();
                layout.preferredWidth = Mathf.Clamp(42f + chip.Text.Length * 7f, 62f, 190f);
                Text chipLabel = CreateText(chipObject.transform, chip.Text, 11, FontStyle.Bold, TextAnchor.MiddleCenter, chip.TextColor);
                RectTransform chipRect = chipLabel.GetComponent<RectTransform>();
                chipRect.anchorMin = Vector2.zero;
                chipRect.anchorMax = Vector2.one;
                chipRect.offsetMin = new Vector2(8f, 2f);
                chipRect.offsetMax = new Vector2(-8f, -2f);
            }
        }

        private static ActionCardView CreateCard(Transform parent, bool primary)
        {
            GameObject buttonObject = new GameObject(primary ? "PrimaryCard" : "SecondaryCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;
            layoutElement.preferredHeight = 128f;
            layoutElement.minWidth = 0f;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            image.color = primary ? BattleUiTheme.ButtonPrimary : BattleUiTheme.ButtonSecondary;
            image.type = Image.Type.Sliced;

            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = primary
                ? new Color(0.34f, 0.22f, 0.08f, 0.66f)
                : new Color(0.24f, 0.33f, 0.48f, 0.6f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = primary ? BattleUiTheme.ButtonPrimaryHighlight : BattleUiTheme.ButtonSecondaryHighlight;
            colors.pressedColor = primary ? BattleUiTheme.ButtonPrimaryPressed : BattleUiTheme.ButtonSecondaryPressed;
            colors.disabledColor = BattleUiTheme.ButtonDisabled;
            button.colors = colors;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentObject.transform.SetParent(buttonObject.transform, false);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(12f, 10f);
            contentRect.offsetMax = new Vector2(-12f, -10f);

            VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            GameObject titleBand = CreateInsetPanel(
                "TitleBand",
                contentObject.transform,
                28f,
                primary ? new Color(0.19f, 0.14f, 0.09f, 0.34f) : new Color(0.09f, 0.12f, 0.17f, 0.46f));
            Transform titleRoot = BattleHudFactory.CreateInsetContentRoot(titleBand.transform, 10f);
            Text titleLabel = BattleHudFactory.CreateText(
                titleRoot,
                string.Empty,
                primary ? 16 : 15,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                primary ? CardPrimaryTextColor : CardSecondaryTextColor,
                BattleTextRole.SingleLineTitle);

            Text reasonLabel = CreateText(contentObject.transform, string.Empty, 12, FontStyle.Normal, TextAnchor.MiddleLeft, primary ? CardPrimaryDetailColor : CardSecondaryDetailColor);
            BattleHudFactory.ApplyTextRole(reasonLabel, BattleTextRole.DenseMeta);

            GameObject metricRow = new GameObject("MetricRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            metricRow.transform.SetParent(contentObject.transform, false);
            metricRow.GetComponent<LayoutElement>().preferredHeight = 22f;
            HorizontalLayoutGroup metricLayout = metricRow.GetComponent<HorizontalLayoutGroup>();
            metricLayout.spacing = 4f;
            metricLayout.childAlignment = TextAnchor.MiddleLeft;
            metricLayout.childControlHeight = true;
            metricLayout.childControlWidth = false;
            metricLayout.childForceExpandHeight = false;
            metricLayout.childForceExpandWidth = false;

            Text outcomeLabel = CreateText(contentObject.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.UpperLeft, primary ? CardPrimaryDetailColor : CardSecondaryTextColor);
            BattleHudFactory.ApplyTextRole(outcomeLabel, BattleTextRole.TwoLineSummary);

            Text riskLabel = CreateText(contentObject.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft, primary ? CardPrimaryRiskColor : CardSecondaryRiskColor);
            BattleHudFactory.ApplyTextRole(riskLabel, BattleTextRole.DenseMeta);

            return new ActionCardView(button, image, outline, titleLabel, reasonLabel, metricRow.transform, outcomeLabel, riskLabel);
        }

        private static GameObject CreatePanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            image.color = BattleUiTheme.PanelBackdrop;
            image.type = Image.Type.Sliced;

            Outline outline = panel.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = BattleUiTheme.OutlineStrong;
            return panel;
        }

        private static GameObject CreateInsetPanel(string name, Transform parent, float preferredHeight, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            image.color = color;
            image.type = Image.Type.Sliced;

            LayoutElement layoutElement = panel.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = preferredHeight;

            Outline outline = panel.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = BattleUiTheme.OutlineSoft;
            return panel;
        }

        private static Text CreateText(Transform parent, string content, int size, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            return BattleHudFactory.CreateText(parent, content, size, fontStyle, alignment, color);
        }

        private sealed class ActionCardView
        {
            public ActionCardView(Button button, Image background, Outline outline, Text titleLabel, Text reasonLabel, Transform metricRoot, Text outcomeLabel, Text riskLabel)
            {
                Button = button;
                Background = background;
                Outline = outline;
                TitleLabel = titleLabel;
                ReasonLabel = reasonLabel;
                MetricRoot = metricRoot;
                OutcomeLabel = outcomeLabel;
                RiskLabel = riskLabel;
            }

            public Button Button { get; }

            public Image Background { get; }

            public Outline Outline { get; }

            public Text TitleLabel { get; }

            public Text ReasonLabel { get; }

            public Transform MetricRoot { get; }

            public Text OutcomeLabel { get; }

            public Text RiskLabel { get; }
        }
    }

    public sealed class ActionMenuPanel : BattleActionDockView
    {
    }
}
