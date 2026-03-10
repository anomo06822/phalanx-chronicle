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
        private Button attackButton;
        private Button skillButton;
        private Button waitButton;
        private Text attackButtonLabel;
        private Text skillButtonLabel;
        private Text waitButtonLabel;

        public void Initialize(Transform canvasRoot)
        {
            rootObject = CreatePanel("ActionMenu", canvasRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(420f, 94f));
            HorizontalLayoutGroup layout = rootObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            attackButton = CreateButton(rootObject.transform, out attackButtonLabel, LocalizationService.Text("ui.button.attack", "Attack"));
            skillButton = CreateButton(rootObject.transform, out skillButtonLabel, LocalizationService.Text("ui.button.skill", "Skill"));
            waitButton = CreateButton(rootObject.transform, out waitButtonLabel, LocalizationService.Text("ui.button.wait", "Wait"));
            Hide();
        }

        public void Show(
            bool canAttack,
            string skillLabel,
            bool canUseSkill,
            Action onAttack,
            Action onSkill,
            Action onWait)
        {
            rootObject.SetActive(true);
            attackButtonLabel.text = LocalizationService.Text("ui.button.attack", "Attack");
            skillButtonLabel.text = LocalizationService.Text("ui.button.skill", "Skill") + "\n" + skillLabel;
            waitButtonLabel.text = LocalizationService.Text("ui.button.wait", "Wait");
            attackButton.interactable = canAttack;
            skillButton.interactable = canUseSkill;
            attackButton.onClick.RemoveAllListeners();
            skillButton.onClick.RemoveAllListeners();
            waitButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(() => onAttack?.Invoke());
            skillButton.onClick.AddListener(() => onSkill?.Invoke());
            waitButton.onClick.AddListener(() => onWait?.Invoke());
        }

        public void Hide()
        {
            if (rootObject != null)
            {
                rootObject.SetActive(false);
            }
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
            image.color = new Color(0.09f, 0.09f, 0.12f, 0.94f);
            return panel;
        }

        private static Button CreateButton(Transform parent, out Text labelText, string label)
        {
            GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.79f, 0.58f, 0.24f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.9f, 0.67f, 0.29f, 1f);
            colors.pressedColor = new Color(0.67f, 0.49f, 0.2f, 1f);
            colors.disabledColor = new Color(0.32f, 0.32f, 0.34f, 0.9f);
            button.colors = colors;

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(110f, 52f);

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            labelText = textObject.GetComponent<Text>();
            labelText.text = label;
            labelText.font = RuntimeSpriteLibrary.DefaultFont;
            labelText.fontSize = 18;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.12f, 0.08f, 0.06f, 1f);
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Overflow;
            return button;
        }
    }
}
