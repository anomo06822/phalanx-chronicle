using System.Collections;
using PhalanxChronicle.Battle.Units;
using PhalanxChronicle.Localization;
using PhalanxChronicle.Presentation;
using UnityEngine;
using Text = TMPro.TextMeshProUGUI;

namespace PhalanxChronicle.Battle.Effects
{
    public static class DuelVisualEffects
    {
        public static IEnumerator PlayChallengeBanner(string title)
        {
            GameObject banner = new GameObject("DuelBanner", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            Canvas canvas = banner.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            CanvasGroup canvasGroup = banner.GetComponent<CanvasGroup>();

            GameObject panel = new GameObject("BannerPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(banner.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(420f, 86f);
            UnityEngine.UI.Image panelImage = panel.GetComponent<UnityEngine.UI.Image>();
            panelImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
            panelImage.color = new Color(0.12f, 0.08f, 0.04f, 0.92f);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(panel.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 12f);
            labelRect.offsetMax = new Vector2(-18f, -12f);
            Text label = labelObject.GetComponent<Text>();
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.font = RuntimeSpriteLibrary.GetUiTmpFont(26, FontStyle.Bold);
            label.fontSize = 26f;
            label.text = string.IsNullOrWhiteSpace(title)
                ? LocalizationService.Text("ui.duel.header", "一騎對決")
                : title;
            label.color = new Color(1f, 0.9f, 0.58f, 1f);

            float elapsed = 0f;
            float duration = 0.42f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float alpha = progress < 0.35f ? progress / 0.35f : 1f - ((progress - 0.35f) / 0.65f);
                canvasGroup.alpha = Mathf.Clamp01(alpha);
                panelRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.04f, Mathf.Clamp01(progress));
                yield return null;
            }

            Object.Destroy(banner);
        }

        public static IEnumerator PlayApproachClash(Unit attackerView, Unit defenderView)
        {
            if (attackerView == null || defenderView == null)
            {
                yield break;
            }

            yield return attackerView.AnimateAttack(defenderView.transform.position);
            yield return defenderView.AnimatePulse(new Color(1f, 0.82f, 0.52f, 1f));
        }

        public static IEnumerator PlayImpactFreeze(Vector3 worldPosition)
        {
            GameObject impact = new GameObject("DuelImpact", typeof(SpriteRenderer));
            impact.transform.position = new Vector3(worldPosition.x, worldPosition.y, -0.8f);
            SpriteRenderer renderer = impact.GetComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteLibrary.SlashSprite;
            renderer.color = new Color(1f, 0.94f, 0.76f, 0f);
            renderer.sortingOrder = 72;

            float duration = 0.16f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float alpha = progress < 0.4f ? progress / 0.4f : 1f - ((progress - 0.4f) / 0.6f);
                renderer.color = new Color(1f, 0.94f, 0.76f, Mathf.Clamp01(alpha));
                impact.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.4f, progress);
                yield return null;
            }

            Object.Destroy(impact);
        }

        public static IEnumerator PlayResultOverlay(string text, Color color)
        {
            GameObject overlay = new GameObject("DuelResultOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            Canvas canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 610;
            CanvasGroup canvasGroup = overlay.GetComponent<CanvasGroup>();

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(overlay.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(360f, 70f);
            Text label = labelObject.GetComponent<Text>();
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.font = RuntimeSpriteLibrary.GetUiTmpFont(34, FontStyle.Bold);
            label.fontSize = 34f;
            label.text = text;
            label.color = color;

            float duration = 0.48f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float alpha = progress < 0.3f ? progress / 0.3f : 1f - ((progress - 0.3f) / 0.7f);
                canvasGroup.alpha = Mathf.Clamp01(alpha);
                labelRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-8f, 10f, progress));
                yield return null;
            }

            Object.Destroy(overlay);
        }
    }
}
