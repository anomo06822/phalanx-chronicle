using System;
using System.Collections;
using PhalanxChronicle.Core;
using PhalanxChronicle.Localization;
using PhalanxChronicle.Presentation;
using UnityEngine;

namespace PhalanxChronicle.Battle.Units
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Unit : MonoBehaviour
    {
        private const float HpBarWidth = 0.72f;
        private const float HpBarBackHeight = 0.09f;
        private const float HpBarFillHeight = 0.06f;
        private const float BaseNameCharacterSize = 0.038f;
        private const float GlobalVisualScale = 0.84f;

        private Action<Unit> clickHandler;
        private Action<Unit, bool> hoverChangedHandler;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer frameRenderer;
        private SpriteRenderer factionRingRenderer;
        private SpriteRenderer statusBadgeRenderer;
        private SpriteRenderer skillReadyRenderer;
        private SpriteRenderer namePlateRenderer;
        private SpriteRenderer hpBackRenderer;
        private SpriteRenderer hpFillRenderer;
        private TextMesh nameText;
        private GameObject nameInfoRoot;
        private GameObject hpInfoRoot;
        private UnitVisualProfile visualProfile;

        private Vector3 restingScale = new Vector3(0.94f, 0.94f, 1f);
        private bool isSelected;
        private bool isHovered;

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
            colliderComponent.size = new Vector2(0.56f, 0.82f);
            colliderComponent.offset = new Vector2(0f, -0.02f);
            colliderComponent.isTrigger = false;
            float scaledSize = visualProfile.BattleScale * GlobalVisualScale;
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

        public Vector3 GetAnchorPosition(float yOffset = 0.64f)
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
            shadowRenderer = CreateSpriteChild("Shadow", transform, RuntimeSpriteLibrary.MistBandSprite, new Color(0f, 0f, 0f, 0.2f), new Vector3(0f, -0.34f, 0f), new Vector3(0.34f, 0.08f, 1f), 20);
            factionRingRenderer = CreateSpriteChild(
                "FactionMarker",
                transform,
                RuntimeSpriteLibrary.GetFactionMarkerSprite(visualProfile),
                visualProfile.MarkerColor,
                new Vector3(0f, -0.33f, 0f),
                new Vector3(0.72f, 0.18f, 1f),
                21);
            frameRenderer = CreateSpriteChild(
                "SelectionFrame",
                transform,
                visualProfile.SelectionFrame != null ? visualProfile.SelectionFrame : RuntimeSpriteLibrary.FrameSprite,
                visualProfile.FrameColor,
                new Vector3(0f, -0.01f, 0f),
                new Vector3(0.94f, 1.02f, 1f),
                29);
            frameRenderer.enabled = false;

            statusBadgeRenderer = CreateSpriteChild(
                "StatusBadge",
                transform,
                RuntimeSpriteLibrary.SparkSprite,
                Color.white,
                new Vector3(0.28f, 0.44f, 0f),
                new Vector3(0.13f, 0.13f, 1f),
                34);
            statusBadgeRenderer.enabled = false;

            skillReadyRenderer = CreateSpriteChild(
                "SkillReady",
                transform,
                RuntimeSpriteLibrary.RingSprite,
                Color.white,
                new Vector3(0f, 0.62f, 0f),
                new Vector3(0.15f, 0.15f, 1f),
                35);
            skillReadyRenderer.enabled = false;

            nameInfoRoot = new GameObject("NameInfoRoot");
            nameInfoRoot.transform.SetParent(transform, false);
            nameInfoRoot.transform.localPosition = new Vector3(0f, 0.68f, 0f);

            namePlateRenderer = CreateSpriteChild(
                "NamePlate",
                nameInfoRoot.transform,
                RuntimeSpriteLibrary.BannerSprite,
                BattleUiTheme.GetFactionPlateColor(RuntimeState.Faction, exhausted: false),
                new Vector3(0f, 0f, 0f),
                new Vector3(0.64f, 0.15f, 1f),
                36);

            string localizedName = LocalizationService.Text(RuntimeState.DisplayNameKey, RuntimeState.DisplayName);
            nameText = CreateTextChild(
                "NameText",
                nameInfoRoot.transform,
                localizedName,
                new Vector3(0f, 0f, -0.01f),
                37,
                GetNameCharacterSize(localizedName),
                TextAlignment.Center,
                TextAnchor.MiddleCenter);
            nameText.color = BattleUiTheme.TextPrimary;

            hpInfoRoot = new GameObject("HpInfoRoot");
            hpInfoRoot.transform.SetParent(transform, false);
            hpInfoRoot.transform.localPosition = new Vector3(0f, 0.54f, 0f);

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
            nameText.text = localizedName;
            nameText.characterSize = GetNameCharacterSize(localizedName);

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
        }

        private void UpdateInfoVisibility()
        {
            if (RuntimeState == null)
            {
                return;
            }

            bool lowHealth = RuntimeState.MaxHp > 0 && ((float)RuntimeState.CurrentHp / RuntimeState.MaxHp) <= 0.45f;
            bool showName = RuntimeState.IsAlive && (isHovered || isSelected);
            bool showHp = RuntimeState.IsAlive && (isHovered || isSelected || lowHealth || RuntimeState.Faction == UnitFaction.Player);

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

        private static float GetNameCharacterSize(string localizedName)
        {
            if (string.IsNullOrWhiteSpace(localizedName))
            {
                return BaseNameCharacterSize;
            }

            int visibleLength = localizedName.Replace(" ", string.Empty).Length;
            if (visibleLength <= 4)
            {
                return BaseNameCharacterSize;
            }

            if (visibleLength <= 6)
            {
                return 0.039f;
            }

            if (visibleLength <= 9)
            {
                return 0.036f;
            }

            if (visibleLength <= 12)
            {
                return 0.033f;
            }

            return 0.03f;
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

        private TextMesh CreateTextChild(
            string childName,
            Transform parent,
            string content,
            Vector3 localPosition,
            int sortingOrder,
            float characterSize,
            TextAlignment alignment,
            TextAnchor anchor)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;

            TextMesh textMesh = child.AddComponent<TextMesh>();
            textMesh.text = content;
            textMesh.font = RuntimeSpriteLibrary.HeadingFont;
            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.alignment = alignment;
            textMesh.anchor = anchor;
            textMesh.color = Color.white;

            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.sharedMaterial = RuntimeSpriteLibrary.DefaultFont.material;
            return textMesh;
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
