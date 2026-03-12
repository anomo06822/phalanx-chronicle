using System;
using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Localization;
using PhalanxChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace PhalanxChronicle.UI
{
    public class BattleActionDockView : MonoBehaviour
    {
        protected GameObject rootObject;
        protected Text modeLabel;
        protected Text contextHintLabel;

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
            rootObject = CreatePanel(
                "ActionDock",
                canvasRoot,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 14f),
                new Vector2(1120f, 172f));

            VerticalLayoutGroup layout = rootObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = BattleUiTheme.Space8;
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            GameObject headerRow = new GameObject("HeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            headerRow.transform.SetParent(rootObject.transform, false);
            headerRow.GetComponent<LayoutElement>().preferredHeight = 30f;
            HorizontalLayoutGroup headerLayout = headerRow.GetComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = BattleUiTheme.Space8;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlHeight = true;
            headerLayout.childControlWidth = false;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childForceExpandWidth = false;

            GameObject modePanel = CreateInsetPanel("ActionModePanel", headerRow.transform, 30f, BattleUiTheme.PanelReward);
            modePanel.GetComponent<LayoutElement>().preferredWidth = 152f;
            modeLabel = CreateText(modePanel.transform, string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.14f, 0.1f, 0.06f, 1f));
            RectTransform modeRect = modeLabel.GetComponent<RectTransform>();
            modeRect.anchorMin = Vector2.zero;
            modeRect.anchorMax = Vector2.one;
            modeRect.offsetMin = new Vector2(10f, 2f);
            modeRect.offsetMax = new Vector2(-10f, -2f);

            contextHintLabel = CreateText(headerRow.transform, string.Empty, 12, FontStyle.Italic, TextAnchor.MiddleLeft, BattleUiTheme.TextSecondary);
            LayoutElement contextLayout = contextHintLabel.GetComponent<LayoutElement>();
            contextLayout.flexibleWidth = 1f;
            contextLayout.preferredHeight = 22f;
            contextHintLabel.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject actionRow = CreateRow("ActionRow", rootObject.transform, 108f);
            attackButtonView = CreateCard(actionRow.transform, true);
            skillButtonView = CreateCard(actionRow.transform, true);
            waitButtonView = CreateCard(actionRow.transform, false);
            backButtonView = CreateCard(actionRow.transform, false);

            Hide();
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

            rootObject.SetActive(true);
            modeLabel.text = model != null ? model.ModeLabel : string.Empty;
            contextHintLabel.text = model != null ? model.ContextHint : string.Empty;
            contextHintLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(contextHintLabel.text));

            BindCard(
                attackButtonView,
                GetAction(model, BattleActionDescriptorType.Attack),
                LocalizationService.Text("ui.button.attack", "攻擊"),
                onAttack);
            BindCard(
                skillButtonView,
                GetAction(model, BattleActionDescriptorType.Skill),
                LocalizationService.Text("ui.button.skill", "技能"),
                onSkill);
            BindCard(
                waitButtonView,
                GetAction(model, BattleActionDescriptorType.Wait),
                LocalizationService.Text("ui.button.wait", "待命"),
                onWait);
            BindCard(
                backButtonView,
                GetAction(model, BattleActionDescriptorType.Back),
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
                ? (interactable ? BattleUiTheme.ButtonText : new Color(0.9f, 0.88f, 0.84f, 1f))
                : (interactable ? BattleUiTheme.ButtonSecondaryText : BattleUiTheme.TextMuted);
            Color detailColor = primary
                ? new Color(0.22f, 0.15f, 0.08f, 0.94f)
                : new Color(0.78f, 0.83f, 0.9f, 1f);
            Color disabledDetailColor = primary
                ? new Color(0.74f, 0.75f, 0.78f, 1f)
                : BattleUiTheme.TextMuted;
            view.ReasonLabel.color = interactable ? detailColor : disabledDetailColor;
            view.OutcomeLabel.color = interactable ? detailColor : disabledDetailColor;
            view.RiskLabel.color = interactable
                ? (primary ? new Color(0.35f, 0.16f, 0.09f, 0.94f) : new Color(0.99f, 0.78f, 0.62f, 1f))
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
            layoutElement.preferredHeight = 108f;
            layoutElement.minWidth = 0f;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            image.color = primary ? BattleUiTheme.ButtonPrimary : BattleUiTheme.ButtonSecondary;

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
            contentRect.offsetMin = new Vector2(12f, 8f);
            contentRect.offsetMax = new Vector2(-12f, -8f);

            VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 3f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            Text titleLabel = CreateText(contentObject.transform, string.Empty, primary ? 16 : 15, FontStyle.Bold, TextAnchor.MiddleLeft, primary ? BattleUiTheme.ButtonText : BattleUiTheme.ButtonSecondaryText);
            titleLabel.GetComponent<LayoutElement>().preferredHeight = 20f;
            titleLabel.verticalOverflow = VerticalWrapMode.Truncate;

            Text reasonLabel = CreateText(contentObject.transform, string.Empty, 11, FontStyle.Normal, TextAnchor.MiddleLeft, primary ? new Color(0.22f, 0.15f, 0.08f, 0.94f) : new Color(0.78f, 0.83f, 0.9f, 1f));
            reasonLabel.GetComponent<LayoutElement>().preferredHeight = 16f;
            reasonLabel.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject metricRow = new GameObject("MetricRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            metricRow.transform.SetParent(contentObject.transform, false);
            metricRow.GetComponent<LayoutElement>().preferredHeight = 20f;
            HorizontalLayoutGroup metricLayout = metricRow.GetComponent<HorizontalLayoutGroup>();
            metricLayout.spacing = 4f;
            metricLayout.childAlignment = TextAnchor.MiddleLeft;
            metricLayout.childControlHeight = true;
            metricLayout.childControlWidth = false;
            metricLayout.childForceExpandHeight = false;
            metricLayout.childForceExpandWidth = false;

            Text outcomeLabel = CreateText(contentObject.transform, string.Empty, 11, FontStyle.Bold, TextAnchor.UpperLeft, primary ? new Color(0.24f, 0.14f, 0.08f, 0.96f) : new Color(0.95f, 0.91f, 0.85f, 1f));
            outcomeLabel.GetComponent<LayoutElement>().preferredHeight = 28f;
            outcomeLabel.verticalOverflow = VerticalWrapMode.Truncate;

            Text riskLabel = CreateText(contentObject.transform, string.Empty, 11, FontStyle.Bold, TextAnchor.MiddleLeft, primary ? new Color(0.38f, 0.17f, 0.1f, 0.96f) : new Color(0.99f, 0.78f, 0.62f, 1f));
            riskLabel.GetComponent<LayoutElement>().preferredHeight = 14f;
            riskLabel.verticalOverflow = VerticalWrapMode.Truncate;

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
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.InkPanelSprite;
            image.color = BattleUiTheme.PanelBackdrop;

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

            LayoutElement layoutElement = panel.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = preferredHeight;

            Outline outline = panel.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.effectColor = BattleUiTheme.OutlineSoft;
            return panel;
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
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.02f;

            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(0.25f, -0.25f);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);

            textObject.GetComponent<LayoutElement>().preferredHeight = size + 8f;
            return text;
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
