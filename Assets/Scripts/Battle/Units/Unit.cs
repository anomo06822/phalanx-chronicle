using System;
using System.Collections;
using PhalanxChronicle.Core;
using PhalanxChronicle.Localization;
using PhalanxChronicle.Presentation;
using TMPro;
using UnityEngine;

namespace PhalanxChronicle.Battle.Units
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Unit : MonoBehaviour
    {
        private const float HpBarWidth = 0.62f;
        private const float HpBarBackHeight = 0.07f;
        private const float HpBarFillHeight = 0.045f;
        private const float BaseNameFontSize = 6.4f;
        private const float GlobalVisualScale = 1.22f;
        private const float NameInfoWorldY = 0.72f;
        private const float HpInfoWorldY = 0.56f;
        private const float GeneralSoldierScaleMultiplier = 1.10f;
        private const float HeroScaleMultiplier = 1.16f;
        private const float BossScaleMultiplier = 1.18f;
        private const float RoleScaleFallback = 1.00f;
        private const float NamePlateHeight = 0.16f;
        private const float NamePlateMinWidth = 0.5f;
        private const float NamePlateMidWidth = 0.58f;
        private const float NamePlateMaxWidth = 0.66f;
        private const float WorldTextScale = 0.1f;
        private const float WorldTextHeight = 1.6f;

        private Action<Unit> clickHandler;
        private Action<Unit, bool> hoverChangedHandler;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer silhouetteRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer frameRenderer;
        private SpriteRenderer factionRingRenderer;
        private SpriteRenderer statusBadgeRenderer;
        private SpriteRenderer skillReadyRenderer;
        private SpriteRenderer namePlateRenderer;
        private SpriteRenderer hpBackRenderer;
        private SpriteRenderer hpFillRenderer;
        private TextMeshPro nameText;
        private TextMeshPro nameTextShadow;
        private GameObject nameInfoRoot;
        private GameObject hpInfoRoot;
        private UnitVisualProfile visualProfile;

        private Vector3 restingScale = new Vector3(0.94f, 0.94f, 1f);
        private bool isSelected;
        private bool isHovered;
        private bool isInfoSuppressed;

        public UnitRuntimeState RuntimeState { get; private set; }

        public string UnitId => RuntimeState != null ? RuntimeState.Id : string.Empty;

        public void Initialize(UnitRuntimeState runtimeState, Action<Unit> onClicked, Action<Unit, bool> onHoverChanged = null)
        {
            RuntimeState = runtimeState;
            clickHandler = onClicked;
            hoverChangedHandler = onHoverChanged;
            visualProfile = UnitVisualCatalog.GetProfile(runtimeState.Id, runtimeState.Faction, runtimeState.Role);
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = RuntimeSpriteLibrary.GetUnitSprite(visualProfile);
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 30;

            BoxCollider2D colliderComponent = GetComponent<BoxCollider2D>();
            colliderComponent.size = new Vector2(0.5f, 0.74f);
            colliderComponent.offset = new Vector2(0f, -0.06f);
            colliderComponent.isTrigger = false;
            float scaledSize = visualProfile.BattleScale * GlobalVisualScale * GetRoleScaleMultiplier(runtimeState, visualProfile);
            restingScale = new Vector3(scaledSize, scaledSize, 1f);
            transform.localScale = restingScale;
            gameObject.name = LocalizationService.Text(runtimeState.DisplayNameKey, runtimeState.DisplayName);

            if (visualProfile.IdleAnimationController != null)
            {
                Animator animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = gameObject.AddComponent<Animator>();
                }

                animator.runtimeAnimatorController = visualProfile.IdleAnimationController;
            }

            BuildDecorations();
            SyncVisualState();
        }

        public void Sync(Vector3 worldPosition)
        {
            if (RuntimeState == null)
            {
                return;
            }

            transform.position = new Vector3(worldPosition.x, worldPosition.y, -0.5f);
            gameObject.SetActive(RuntimeState.IsAlive);
            if (!RuntimeState.IsAlive)
            {
                return;
            }

            SyncVisualState();
            ApplySelectionState();
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            ApplySelectionState();
            UpdateInfoVisibility();
        }

        public void SetHovered(bool hovered)
        {
            isHovered = hovered;
            ApplySelectionState();
            UpdateInfoVisibility();
        }

        public void SetInfoSuppressed(bool suppressed)
        {
            if (isInfoSuppressed == suppressed)
            {
                return;
            }

            isInfoSuppressed = suppressed;
            UpdateInfoVisibility();
        }

        public Vector3 GetAnchorPosition(float yOffset = 0.58f)
        {
            return transform.position + new Vector3(0f, yOffset, 0f);
        }

        public IEnumerator AnimateMove(Vector3 startPosition, Vector3 endPosition, float duration)
        {
            duration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                transform.position = Vector3.Lerp(startPosition, endPosition, eased) + new Vector3(0f, Mathf.Sin(progress * Mathf.PI) * 0.08f, 0f);
                yield return null;
            }

            transform.position = endPosition;
        }

        public IEnumerator AnimateAttack(Vector3 targetPosition)
        {
            Vector3 origin = transform.position;
            Vector3 strikePosition = Vector3.Lerp(origin, targetPosition, 0.28f);
            yield return AnimateBetween(origin, strikePosition, 0.1f);
            yield return AnimateBetween(strikePosition, origin, 0.1f);
        }

        public IEnumerator AnimateHit()
        {
            Vector3 origin = transform.position;
            for (int index = 0; index < 3; index++)
            {
                transform.position = origin + new Vector3(index % 2 == 0 ? 0.08f : -0.08f, 0f, 0f);
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.045f);
                transform.position = origin;
                SyncVisualState();
                yield return new WaitForSeconds(0.025f);
            }

            transform.position = origin;
            SyncVisualState();
        }

        public IEnumerator AnimatePulse(Color flashColor)
        {
            Color baseColor = spriteRenderer.color;
            Vector3 baseScale = transform.localScale;
            float duration = 0.24f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(progress * Mathf.PI);
                spriteRenderer.color = Color.Lerp(baseColor, flashColor, wave * 0.6f);
                transform.localScale = Vector3.Lerp(baseScale, restingScale * 1.1f, wave);
                yield return null;
            }

            SyncVisualState();
            ApplySelectionState();
        }

        private void BuildDecorations()
        {
            shadowRenderer = CreateSpriteChild("Shadow", transform, RuntimeSpriteLibrary.MistBandSprite, new Color(0f, 0f, 0f, 0.22f), new Vector3(0f, -0.38f, 0f), new Vector3(0.32f, 0.07f, 1f), 20);
            silhouetteRenderer = CreateSpriteChild(
                "Silhouette",
                transform,
                spriteRenderer != null ? spriteRenderer.sprite : null,
                new Color(0.04f, 0.04f, 0.05f, 0.92f),
                new Vector3(0f, -0.01f, 0f),
                new Vector3(1.08f, 1.08f, 1f),
                28);
            factionRingRenderer = CreateSpriteChild(
                "FactionMarker",
                transform,
                RuntimeSpriteLibrary.GetFactionMarkerSprite(visualProfile),
                visualProfile.MarkerColor,
                new Vector3(0f, -0.35f, 0f),
                new Vector3(0.7f, 0.16f, 1f),
                21);
            frameRenderer = CreateSpriteChild(
                "SelectionFrame",
                transform,
                visualProfile.SelectionFrame != null ? visualProfile.SelectionFrame : RuntimeSpriteLibrary.FrameSprite,
                visualProfile.FrameColor,
                new Vector3(0f, -0.02f, 0f),
                new Vector3(0.82f, 0.92f, 1f),
                29);
            frameRenderer.enabled = false;

            statusBadgeRenderer = CreateSpriteChild(
                "StatusBadge",
                transform,
                RuntimeSpriteLibrary.SparkSprite,
                Color.white,
                new Vector3(0.24f, 0.36f, 0f),
                new Vector3(0.1f, 0.1f, 1f),
                34);
            statusBadgeRenderer.enabled = false;

            skillReadyRenderer = CreateSpriteChild(
                "SkillReady",
                transform,
                RuntimeSpriteLibrary.RingSprite,
                Color.white,
                new Vector3(0f, 0.54f, 0f),
                new Vector3(0.11f, 0.11f, 1f),
                35);
            skillReadyRenderer.enabled = false;

            nameInfoRoot = new GameObject("NameInfoRoot");
            nameInfoRoot.transform.SetParent(transform, false);
            nameInfoRoot.transform.localPosition = new Vector3(0f, NameInfoWorldY, 0f);
            string localizedName = LocalizationService.Text(RuntimeState.DisplayNameKey, RuntimeState.DisplayName);
            float namePlateWidth = GetNamePlateWidth(localizedName);

            namePlateRenderer = CreateSpriteChild(
                "NamePlate",
                nameInfoRoot.transform,
                RuntimeSpriteLibrary.BannerSprite,
                BattleUiTheme.GetFactionPlateColor(RuntimeState.Faction, exhausted: false),
                new Vector3(0f, 0f, 0f),
                new Vector3(namePlateWidth, NamePlateHeight, 1f),
                36);

            nameTextShadow = CreateTextChild(
                "NameTextShadow",
                nameInfoRoot.transform,
                localizedName,
                new Vector3(0.018f, -0.01f, -0.015f),
                36,
                GetNameFontSize(localizedName),
                namePlateWidth,
                RuntimeSpriteLibrary.GetWorldTmpFont(FontStyle.Bold),
                new Color(0.09f, 0.06f, 0.04f, 0.95f),
                new Color(0.02f, 0.015f, 0.01f, 0.7f),
                0.08f);
            nameText = CreateTextChild(
                "NameText",
                nameInfoRoot.transform,
                localizedName,
                new Vector3(0f, 0f, -0.01f),
                37,
                GetNameFontSize(localizedName),
                namePlateWidth,
                RuntimeSpriteLibrary.GetWorldTmpFont(FontStyle.Bold),
                BattleUiTheme.TextPrimary,
                new Color(0.18f, 0.1f, 0.04f, 0.92f),
                0.14f,
                new Color(0.04f, 0.03f, 0.02f, 0.7f),
                new Vector2(0.12f, -0.12f),
                0.08f);

            hpInfoRoot = new GameObject("HpInfoRoot");
            hpInfoRoot.transform.SetParent(transform, false);
            hpInfoRoot.transform.localPosition = new Vector3(0f, HpInfoWorldY, 0f);

            CreateSpriteChild(
                "HpFrame",
                hpInfoRoot.transform,
                RuntimeSpriteLibrary.WhiteSprite,
                new Color(0.03f, 0.03f, 0.04f, 0.88f),
                Vector3.zero,
                new Vector3(HpBarWidth + 0.04f, HpBarBackHeight + 0.03f, 1f),
                35);

            hpBackRenderer = CreateSpriteChild(
                "HpBack",
                hpInfoRoot.transform,
                RuntimeSpriteLibrary.WhiteSprite,
                new Color(0.13f, 0.11f, 0.1f, 0.94f),
                Vector3.zero,
                new Vector3(HpBarWidth, HpBarBackHeight, 1f),
                36);
            hpFillRenderer = CreateSpriteChild(
                "HpFill",
                hpInfoRoot.transform,
                RuntimeSpriteLibrary.WhiteSprite,
                new Color(0.45f, 0.82f, 0.39f, 1f),
                Vector3.zero,
                new Vector3(HpBarWidth, HpBarFillHeight, 1f),
                37);

            nameInfoRoot.SetActive(false);
            hpInfoRoot.SetActive(false);
            UpdateInfoAnchorLayout();
        }

        private void SyncVisualState()
        {
            if (RuntimeState == null)
            {
                return;
            }

            bool hasActed = RuntimeState.HasActed;
            float hpRatio = RuntimeState.MaxHp > 0 ? (float)RuntimeState.CurrentHp / RuntimeState.MaxHp : 0f;
            string localizedName = LocalizationService.Text(RuntimeState.DisplayNameKey, RuntimeState.DisplayName);

            spriteRenderer.color = hasActed
                ? Color.Lerp(Color.white, new Color(0.58f, 0.6f, 0.62f, 1f), 0.5f)
                : Color.white;
            if (silhouetteRenderer != null)
            {
                silhouetteRenderer.sprite = spriteRenderer.sprite;
                silhouetteRenderer.color = hasActed
                    ? new Color(0.12f, 0.13f, 0.16f, 0.82f)
                    : new Color(0.04f, 0.04f, 0.05f, 0.94f);
            }
            factionRingRenderer.color = hasActed
                ? Color.Lerp(visualProfile.MarkerColor, new Color(0.34f, 0.35f, 0.39f, 1f), 0.42f)
                : visualProfile.MarkerColor;
            frameRenderer.color = visualProfile.FrameColor;

            Sprite statusSprite = GetStatusSprite(RuntimeState);
            statusBadgeRenderer.sprite = statusSprite;
            statusBadgeRenderer.color = GetStatusColor(RuntimeState);
            statusBadgeRenderer.enabled = statusSprite != null;

            skillReadyRenderer.color = Color.Lerp(visualProfile.AccentColor, Color.white, 0.36f);
            skillReadyRenderer.enabled = RuntimeState.CanUseSkill &&
                                         RuntimeState.HasEnoughMana(ActiveSkillRules.GetManaCost(RuntimeState)) &&
                                         !RuntimeState.HasActed &&
                                         RuntimeState.IsAlive;

            namePlateRenderer.color = Color.Lerp(
                BattleUiTheme.GetFactionPlateColor(RuntimeState.Faction, hasActed),
                visualProfile.SecondaryColor,
                0.2f);
            float namePlateWidth = GetNamePlateWidth(localizedName);
            namePlateRenderer.transform.localScale = new Vector3(namePlateWidth, NamePlateHeight, 1f);
            nameText.text = localizedName;
            nameText.fontSize = GetNameFontSize(localizedName);
            ApplyTextBounds(nameText, namePlateWidth);
            if (nameTextShadow != null)
            {
                nameTextShadow.text = localizedName;
                nameTextShadow.fontSize = nameText.fontSize;
                ApplyTextBounds(nameTextShadow, namePlateWidth);
            }

            float fillWidth = Mathf.Max(0.01f, HpBarWidth * hpRatio);
            hpFillRenderer.transform.localScale = new Vector3(fillWidth, HpBarFillHeight, 1f);
            hpFillRenderer.transform.localPosition = new Vector3(-((HpBarWidth - fillWidth) * 0.5f), 0f, 0f);
            hpFillRenderer.color = hpRatio > 0.55f
                ? new Color(0.41f, 0.83f, 0.37f, 1f)
                : hpRatio > 0.3f
                    ? new Color(0.9f, 0.72f, 0.21f, 1f)
                    : new Color(0.9f, 0.33f, 0.27f, 1f);

            UpdateInfoVisibility();
        }

        private void ApplySelectionState()
        {
            if (frameRenderer != null)
            {
                frameRenderer.enabled = isSelected && RuntimeState != null && RuntimeState.IsAlive;
            }

            transform.localScale = isSelected
                ? restingScale * 1.035f
                : isHovered
                    ? restingScale * 1.01f
                    : restingScale;
            UpdateInfoAnchorLayout();
        }

        private void UpdateInfoVisibility()
        {
            if (RuntimeState == null)
            {
                return;
            }

            if (isInfoSuppressed)
            {
                if (nameInfoRoot != null)
                {
                    nameInfoRoot.SetActive(false);
                }

                if (hpInfoRoot != null)
                {
                    hpInfoRoot.SetActive(false);
                }

                return;
            }

            bool lowHealth = RuntimeState.MaxHp > 0 && ((float)RuntimeState.CurrentHp / RuntimeState.MaxHp) <= 0.45f;
            bool alwaysShowIdentity = visualProfile.FrameStyle == UnitFrameStyle.Boss;
            bool showName = RuntimeState.IsAlive && (alwaysShowIdentity || isHovered || isSelected);
            bool showHp = RuntimeState.IsAlive && (isHovered || isSelected || lowHealth || RuntimeState.Faction == UnitFaction.Player || visualProfile.FrameStyle == UnitFrameStyle.Boss);

            if (nameInfoRoot != null)
            {
                nameInfoRoot.SetActive(showName);
            }

            if (hpInfoRoot != null)
            {
                hpInfoRoot.SetActive(showHp);
            }
        }

        private static Sprite GetStatusSprite(UnitRuntimeState runtimeState)
        {
            if (runtimeState == null)
            {
                return null;
            }

            if (runtimeState.HasStatus(StatusEffectType.Inspired))
            {
                return RuntimeSpriteLibrary.SparkSprite;
            }

            if (runtimeState.HasStatus(StatusEffectType.ShatteredArmor))
            {
                return RuntimeSpriteLibrary.SlashSprite;
            }

            if (runtimeState.HasStatus(StatusEffectType.Intimidated))
            {
                return RuntimeSpriteLibrary.ArrowSprite;
            }

            return null;
        }

        private static Color GetStatusColor(UnitRuntimeState runtimeState)
        {
            if (runtimeState == null)
            {
                return Color.white;
            }

            if (runtimeState.HasStatus(StatusEffectType.Inspired))
            {
                return new Color(0.97f, 0.89f, 0.46f, 1f);
            }

            if (runtimeState.HasStatus(StatusEffectType.ShatteredArmor))
            {
                return new Color(0.98f, 0.58f, 0.42f, 1f);
            }

            if (runtimeState.HasStatus(StatusEffectType.Intimidated))
            {
                return new Color(0.72f, 0.84f, 0.98f, 1f);
            }

            return Color.white;
        }

        private static float GetNameFontSize(string localizedName)
        {
            if (string.IsNullOrWhiteSpace(localizedName))
            {
                return BaseNameFontSize;
            }

            int visibleLength = localizedName.Replace(" ", string.Empty).Length;
            if (visibleLength <= 4)
            {
                return BaseNameFontSize;
            }

            if (visibleLength <= 6)
            {
                return 5.8f;
            }

            if (visibleLength <= 9)
            {
                return 5.2f;
            }

            if (visibleLength <= 12)
            {
                return 4.7f;
            }

            return 4.3f;
        }

        private static float GetNamePlateWidth(string localizedName)
        {
            if (string.IsNullOrWhiteSpace(localizedName))
            {
                return NamePlateMidWidth;
            }

            int visibleLength = localizedName.Replace(" ", string.Empty).Length;
            if (visibleLength <= 4)
            {
                return NamePlateMinWidth;
            }

            if (visibleLength <= 8)
            {
                return NamePlateMidWidth;
            }

            return NamePlateMaxWidth;
        }

        private static float GetRoleScaleMultiplier(UnitRuntimeState runtimeState, UnitVisualProfile unitVisualProfile)
        {
            if (unitVisualProfile == null)
            {
                return RoleScaleFallback;
            }

            if (unitVisualProfile.FrameStyle == UnitFrameStyle.Boss)
            {
                return BossScaleMultiplier;
            }

            if (unitVisualProfile.FrameStyle == UnitFrameStyle.Hero)
            {
                return HeroScaleMultiplier;
            }

            return runtimeState == null ? RoleScaleFallback : GeneralSoldierScaleMultiplier;
        }

        private void UpdateInfoAnchorLayout()
        {
            float currentScale = Mathf.Max(0.01f, transform.localScale.x);
            float inverseScale = 1f / currentScale;

            if (nameInfoRoot != null)
            {
                nameInfoRoot.transform.localScale = new Vector3(inverseScale, inverseScale, 1f);
                nameInfoRoot.transform.localPosition = new Vector3(0f, NameInfoWorldY * inverseScale, 0f);
            }

            if (hpInfoRoot != null)
            {
                hpInfoRoot.transform.localScale = new Vector3(inverseScale, inverseScale, 1f);
                hpInfoRoot.transform.localPosition = new Vector3(0f, HpInfoWorldY * inverseScale, 0f);
            }
        }

        private IEnumerator AnimateBetween(Vector3 start, Vector3 end, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
            }
        }

        private SpriteRenderer CreateSpriteChild(
            string childName,
            Transform parent,
            Sprite sprite,
            Color color,
            Vector3 localPosition,
            Vector3 localScale,
            int sortingOrder)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private TextMeshPro CreateTextChild(
            string childName,
            Transform parent,
            string content,
            Vector3 localPosition,
            int sortingOrder,
            float fontSize,
            float worldWidth,
            TMP_FontAsset font,
            Color faceColor,
            Color outlineColor,
            float outlineWidth,
            Color? underlayColor = null,
            Vector2? underlayOffset = null,
            float underlaySoftness = 0f)
        {
            GameObject child = new GameObject(childName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = new Vector3(WorldTextScale, WorldTextScale, 1f);

            TextMeshPro textMesh = child.AddComponent<TextMeshPro>();
            textMesh.text = content;
            textMesh.font = font != null ? font : RuntimeSpriteLibrary.DefaultTmpFont;
            textMesh.fontSize = fontSize;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.alignment = TextAlignmentOptions.Midline;
            textMesh.enableWordWrapping = false;
            textMesh.overflowMode = TextOverflowModes.Truncate;
            textMesh.enableAutoSizing = false;
            textMesh.richText = false;
            textMesh.color = faceColor;
            textMesh.margin = Vector4.zero;
            ApplyTextBounds(textMesh, worldWidth);

            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            renderer.sortingOrder = sortingOrder;
            Material material = RuntimeSpriteLibrary.CreateTmpMaterialInstance(
                textMesh.font,
                faceColor,
                outlineColor,
                outlineWidth,
                underlayColor,
                underlayOffset,
                underlaySoftness);
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            return textMesh;
        }

        private static void ApplyTextBounds(TMP_Text textMesh, float worldWidth)
        {
            if (textMesh == null)
            {
                return;
            }

            textMesh.rectTransform.sizeDelta = new Vector2(
                Mathf.Max(4.6f, worldWidth / WorldTextScale),
                WorldTextHeight);
        }

        private void OnMouseUpAsButton()
        {
            if (RuntimeState != null && RuntimeState.IsAlive)
            {
                clickHandler?.Invoke(this);
            }
        }

        private void OnMouseEnter()
        {
            if (RuntimeState != null && RuntimeState.IsAlive)
            {
                SetHovered(true);
                hoverChangedHandler?.Invoke(this, true);
            }
        }

        private void OnMouseExit()
        {
            if (RuntimeState != null)
            {
                SetHovered(false);
                hoverChangedHandler?.Invoke(this, false);
            }
        }
    }
}
