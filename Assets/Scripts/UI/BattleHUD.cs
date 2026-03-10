using System;
using PhalanxChronicle.Localization;
using PhalanxChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace PhalanxChronicle.UI
{
    public sealed class BattleHUD : MonoBehaviour
    {
        private Text stageLabel;
        private Text mapSeedLabel;
        private Text turnLabel;
        private Text selectedUnitLabel;
        private Text logLabel;
        private Text resultLabel;
        private Text combatHeaderLabel;
        private Text combatDetailLabel;
        private Button endTurnButton;
        private Button rerollButton;
        private GameObject resultPanel;
        private GameObject combatPanel;
        private GameObject rerollContainer;

        public void Initialize(Transform canvasRoot, Action onEndTurn, Action onReroll)
        {
            GameObject infoPanel = CreatePanel("InfoPanel", canvasRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(430f, 300f));
            VerticalLayoutGroup infoLayout = infoPanel.AddComponent<VerticalLayoutGroup>();
            infoLayout.spacing = 4f;
            infoLayout.padding = new RectOffset(16, 16, 14, 14);
            infoLayout.childControlHeight = true;
            infoLayout.childControlWidth = true;
            infoLayout.childForceExpandHeight = false;

            stageLabel = CreateText(infoPanel.transform, LocalizationService.Text("ui.stage", "Stage"), 17, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.95f, 0.87f, 0.62f, 1f));
            mapSeedLabel = CreateText(infoPanel.transform, LocalizationService.Text("ui.seed.fixed", "Seed: Fixed"), 16, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.73f, 0.84f, 0.93f, 1f));
            turnLabel = CreateText(infoPanel.transform, LocalizationService.Text("ui.turn.player", "Turn: Player Phase"), 28, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            selectedUnitLabel = CreateText(infoPanel.transform, LocalizationService.Text("ui.selected.none", "Selected: None"), 20, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.91f, 0.93f, 0.95f, 1f));
            LayoutElement selectedLayout = selectedUnitLabel.GetComponent<LayoutElement>();
            selectedLayout.preferredHeight = 176f;

            GameObject logPanel = CreatePanel("LogPanel", canvasRoot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(520f, 108f));
            logLabel = CreateText(logPanel.transform, LocalizationService.Text("ui.log.select_player", "Select a blue officer to act."), 20, FontStyle.Italic, TextAnchor.MiddleLeft, new Color(0.97f, 0.93f, 0.85f, 1f));
            RectTransform logRect = logLabel.GetComponent<RectTransform>();
            logRect.anchorMin = Vector2.zero;
            logRect.anchorMax = Vector2.one;
            logRect.offsetMin = new Vector2(18f, 16f);
            logRect.offsetMax = new Vector2(-18f, -16f);

            GameObject endTurnContainer = CreatePanel("EndTurnContainer", canvasRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(190f, 82f));
            endTurnButton = CreateButton(endTurnContainer.transform, LocalizationService.Text("ui.button.end_turn", "End Turn"));
            endTurnButton.onClick.AddListener(() => onEndTurn?.Invoke());

            rerollContainer = CreatePanel("RerollContainer", canvasRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -112f), new Vector2(190f, 82f));
            rerollButton = CreateButton(rerollContainer.transform, LocalizationService.Text("ui.button.reroll", "Reroll"));
            rerollButton.onClick.AddListener(() => onReroll?.Invoke());

            combatPanel = CreatePanel("CombatPanel", canvasRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(390f, 94f));
            combatHeaderLabel = CreateText(combatPanel.transform, "Combat", 24, FontStyle.Bold, TextAnchor.UpperCenter, new Color(0.99f, 0.9f, 0.59f, 1f));
            combatDetailLabel = CreateText(combatPanel.transform, string.Empty, 22, FontStyle.Bold, TextAnchor.LowerCenter, Color.white);
            RectTransform headerRect = combatHeaderLabel.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.5f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.offsetMin = new Vector2(16f, -4f);
            headerRect.offsetMax = new Vector2(-16f, -10f);

            RectTransform detailRect = combatDetailLabel.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0f, 0f);
            detailRect.anchorMax = new Vector2(1f, 0.58f);
            detailRect.offsetMin = new Vector2(16f, 10f);
            detailRect.offsetMax = new Vector2(-16f, 4f);
            combatPanel.SetActive(false);

            resultPanel = CreatePanel("ResultPanel", canvasRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 184f));
            resultLabel = CreateText(resultPanel.transform, string.Empty, 38, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            RectTransform resultRect = resultLabel.GetComponent<RectTransform>();
            resultRect.anchorMin = Vector2.zero;
            resultRect.anchorMax = Vector2.one;
            resultRect.offsetMin = new Vector2(24f, 24f);
            resultRect.offsetMax = new Vector2(-24f, -24f);
            resultPanel.SetActive(false);
        }

        public void SetStage(string text)
        {
            stageLabel.text = text;
        }

        public void SetTurn(string text)
        {
            turnLabel.text = text;
        }

        public void SetMapSeed(string text)
        {
            mapSeedLabel.text = text;
        }

        public void SetSelectedUnit(string text)
        {
            selectedUnitLabel.text = text;
        }

        public void SetLog(string text)
        {
            logLabel.text = text;
        }

        public void SetEndTurnEnabled(bool enabled)
        {
            endTurnButton.interactable = enabled;
        }

        public void SetRerollEnabled(bool enabled)
        {
            rerollContainer.SetActive(enabled);
            rerollButton.interactable = enabled;
        }

        public void ShowResult(string text)
        {
            resultPanel.SetActive(true);
            resultLabel.text = text;
        }

        public void HideResult()
        {
            resultPanel.SetActive(false);
        }

        public void ShowCombatBanner(string header, string detail)
        {
            combatPanel.SetActive(true);
            combatHeaderLabel.text = header;
            combatDetailLabel.text = detail;
        }

        public void HideCombatBanner()
        {
            combatPanel.SetActive(false);
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
            rectTransform.pivot = new Vector2(anchorMax.x, anchorMax.y);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.09f, 0.09f, 0.12f, 0.9f);
            return panel;
        }

        private static Text CreateText(Transform parent, string content, int size, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = RuntimeSpriteLibrary.DefaultFont;
            text.fontSize = size;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = size + 14f;
            return text;
        }

        private static Button CreateButton(Transform parent, string label)
        {
            GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.offsetMin = new Vector2(12f, 12f);
            rectTransform.offsetMax = new Vector2(-12f, -12f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.78f, 0.56f, 0.23f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.89f, 0.66f, 0.29f, 1f);
            colors.pressedColor = new Color(0.67f, 0.48f, 0.18f, 1f);
            colors.disabledColor = new Color(0.28f, 0.28f, 0.3f, 0.9f);
            button.colors = colors;

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
            text.text = label;
            text.font = RuntimeSpriteLibrary.DefaultFont;
            text.fontSize = 24;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.13f, 0.09f, 0.06f, 1f);

            return button;
        }
    }
}
