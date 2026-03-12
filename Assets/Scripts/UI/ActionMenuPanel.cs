using System;
using PhalanxChronicle.Localization;
using PhalanxChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace PhalanxChronicle.UI
{
    public sealed class ActionMenuPanel : MonoBehaviour
    {
        private GameObject rootObject;
        private Text modeLabel;
        private ActionButtonView attackButtonView;
        private ActionButtonView skillButtonView;
        private ActionButtonView waitButtonView;
        private ActionButtonView backButtonView;

        public bool IsVisible => rootObject != null && rootObject.activeSelf;

        public string CurrentModeText => modeLabel != null ? modeLabel.text : string.Empty;

        public bool IsBackEnabled => backButtonView != null && backButtonView.Button.interactable;

        public bool IsSkillEnabled => skillButtonView != null && skillButtonView.Button.interactable;

        public void Initialize(Transform canvasRoot)
        {
            rootObject = CreatePanel(
                "ActionMenu",
                canvasRoot,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 24f),
                new Vector2(872f, 210f));

            VerticalLayoutGroup layout = rootObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(22, 22, 18, 20);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            GameObject modePanel = CreateInsetPanel("ActionModePanel", rootObject.transform, 36f, new Color(0.2f, 0.16f, 0.12f, 0.98f));
            modeLabel = CreateText(modePanel.transform, string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleCenter, BattleUiTheme.TextGold);
            modeLabel.resizeTextForBestFit = true;
            modeLabel.resizeTextMinSize = 16;
            modeLabel.resizeTextMaxSize = 18;
            RectTransform modeRect = modeLabel.GetComponent<RectTransform>();
            modeRect.anchorMin = Vector2.zero;
            modeRect.anchorMax = Vector2.one;
            modeRect.offsetMin = new Vector2(14f, 4f);
            modeRect.offsetMax = new Vector2(-14f, -4f);
            LayoutElement modeLayout = modePanel.GetComponent<LayoutElement>();
            modeLayout.preferredHeight = 36f;

            GameObject buttonRow = new GameObject("ActionButtons", typeof(RectTransform));
            buttonRow.transform.SetParent(rootObject.transform, false);
            HorizontalLayoutGroup buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 12f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlHeight = true;
            buttonLayout.childControlWidth = true;
            buttonLayout.childForceExpandHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonRow.AddComponent<LayoutElement>().preferredHeight = 126f;

            attackButtonView = CreateButton(buttonRow.transform, LocalizationService.Text("ui.button.attack", "Attack"));
            skillButtonView = CreateButton(buttonRow.transform, LocalizationService.Text("ui.button.skill", "Skill"));
            waitButtonView = CreateButton(buttonRow.transform, LocalizationService.Text("ui.button.wait", "Wait"));
            backButtonView = CreateButton(buttonRow.transform, LocalizationService.Text("ui.button.back", "Back"));
            Hide();
        }

        public void Show(
            BattleActionMenuModel model,
            Action onAttack,
            Action onSkill,
            Action onWait,
            Action onBack)
        {
            rootObject.SetActive(true);
            modeLabel.text = model != null ? model.ModeLabel : string.Empty;

            BindButton(attackButtonView, LocalizationService.Text("ui.button.attack", "Attack"), model?.AttackDetail, model != null && model.CanAttack, onAttack);
            BindButton(skillButtonView, model?.SkillName ?? LocalizationService.Text("ui.button.skill", "Skill"), model?.SkillDetail, model != null && model.CanUseSkill, onSkill);
            BindButton(waitButtonView, LocalizationService.Text("ui.button.wait", "Wait"), model?.WaitDetail, model == null || model.CanWait, onWait);
            BindButton(backButtonView, model?.BackLabel ?? LocalizationService.Text("ui.button.back", "Back"), model?.BackDetail, model != null && model.CanBack, onBack);
        }

        public void Hide()
        {
            if (rootObject != null)
            {
                rootObject.SetActive(false);
            }
        }

        private static void BindButton(ActionButtonView view, string title, string detail, bool interactable, Action onClick)
        {
            view.TitleLabel.text = title;
            view.DetailLabel.text = string.IsNullOrWhiteSpace(detail)
                ? LocalizationService.Text("ui.action_menu.empty_detail", " ")
                : detail;
            view.Button.interactable = interactable;
            view.Button.onClick.RemoveAllListeners();
            view.Button.onClick.AddListener(() => onClick?.Invoke());

            Color baseColor = interactable
                ? BattleUiTheme.ButtonPrimary
                : new Color(0.18f, 0.17f, 0.16f, 0.97f);
            view.Background.color = baseColor;
            view.TitleLabel.color = interactable
                ? BattleUiTheme.ButtonText
                : new Color(0.9f, 0.88f, 0.84f, 1f);
            view.DetailLabel.color = interactable
                ? new Color(0.22f, 0.15f, 0.08f, 0.94f)
                : new Color(0.74f, 0.75f, 0.78f, 1f);
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
            image.color = BattleUiTheme.PanelSurface;

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

        private static ActionButtonView CreateButton(Transform parent, string title)
        {
            GameObject buttonObject = new GameObject(title + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 124f;
            layoutElement.flexibleWidth = 1f;

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
            colors.disabledColor = new Color(0.18f, 0.17f, 0.16f, 0.97f);
            button.colors = colors;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentObject.transform.SetParent(buttonObject.transform, false);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(14f, 12f);
            contentRect.offsetMax = new Vector2(-14f, -12f);

            VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 6f;
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            Text titleLabel = CreateText(contentObject.transform, title, 22, FontStyle.Bold, TextAnchor.MiddleCenter, BattleUiTheme.ButtonText);
            titleLabel.resizeTextForBestFit = true;
            titleLabel.resizeTextMinSize = 16;
            titleLabel.resizeTextMaxSize = 22;
            titleLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleLabel.verticalOverflow = VerticalWrapMode.Truncate;
            titleLabel.GetComponent<LayoutElement>().preferredHeight = 50f;

            Text detailLabel = CreateText(contentObject.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.22f, 0.15f, 0.08f, 0.94f));
            detailLabel.resizeTextForBestFit = true;
            detailLabel.resizeTextMinSize = 13;
            detailLabel.resizeTextMaxSize = 14;
            detailLabel.GetComponent<LayoutElement>().preferredHeight = 44f;

            return new ActionButtonView(button, image, titleLabel, detailLabel);
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
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            textObject.GetComponent<LayoutElement>().preferredHeight = size + 8f;
            return text;
        }

        private sealed class ActionButtonView
        {
            public ActionButtonView(Button button, Image background, Text titleLabel, Text detailLabel)
            {
                Button = button;
                Background = background;
                TitleLabel = titleLabel;
                DetailLabel = detailLabel;
            }

            public Button Button { get; }

            public Image Background { get; }

            public Text TitleLabel { get; }

            public Text DetailLabel { get; }
        }
    }
}
