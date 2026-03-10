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
        private const float HpBarWidth = 0.82f;

        private Action<Unit> clickHandler;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer frameRenderer;
        private SpriteRenderer roleAuraRenderer;
        private SpriteRenderer namePlateRenderer;
        private SpriteRenderer roleRibbonRenderer;
        private SpriteRenderer hpBackRenderer;
        private SpriteRenderer hpFillRenderer;
        private TextMesh nameText;
        private TextMesh roleText;
        private TextMesh statusText;

        private Vector3 restingScale = new Vector3(0.9f, 0.9f, 1f);
        private bool isSelected;

        public UnitRuntimeState RuntimeState { get; private set; }

        public string UnitId => RuntimeState != null ? RuntimeState.Id : string.Empty;

        public void Initialize(UnitRuntimeState runtimeState, Action<Unit> onClicked)
        {
            RuntimeState = runtimeState;
            clickHandler = onClicked;
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = RuntimeSpriteLibrary.GetUnitSprite(runtimeState.Id, runtimeState.Faction);
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 30;

            BoxCollider2D colliderComponent = GetComponent<BoxCollider2D>();
            colliderComponent.size = new Vector2(0.8f, 1.1f);
            colliderComponent.offset = new Vector2(0f, 0.02f);
            colliderComponent.isTrigger = false;
            transform.localScale = restingScale;
            gameObject.name = LocalizationService.Text(runtimeState.DisplayNameKey, runtimeState.DisplayName);

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
        }

        public Vector3 GetAnchorPosition(float yOffset = 0.88f)
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
                spriteRenderer.color = Color.Lerp(baseColor, flashColor, wave * 0.7f);
                transform.localScale = Vector3.Lerp(baseScale, restingScale * 1.12f, wave);
                yield return null;
            }

            SyncVisualState();
            ApplySelectionState();
        }

        private void BuildDecorations()
        {
            shadowRenderer = CreateSpriteChild("Shadow", RuntimeSpriteLibrary.WhiteSprite, new Color(0f, 0f, 0f, 0.17f), new Vector3(0f, -0.37f, 0f), new Vector3(0.88f, 0.2f, 1f), 20);
            roleAuraRenderer = CreateSpriteChild("RoleAura", RuntimeSpriteLibrary.WhiteSprite, GetRoleColor(RuntimeState.Role, 0.16f), new Vector3(0f, -0.04f, 0f), new Vector3(0.96f, 1.14f, 1f), 24);
            frameRenderer = CreateSpriteChild("SelectionFrame", RuntimeSpriteLibrary.FrameSprite, new Color(0.98f, 0.88f, 0.36f, 1f), new Vector3(0f, 0f, 0f), new Vector3(1.16f, 1.28f, 1f), 29);
            frameRenderer.enabled = false;

            namePlateRenderer = CreateSpriteChild(
                "NamePlate",
                RuntimeSpriteLibrary.BannerSprite,
                RuntimeState.Faction == UnitFaction.Player ? new Color(0.14f, 0.21f, 0.38f, 0.95f) : new Color(0.41f, 0.13f, 0.11f, 0.95f),
                new Vector3(0f, 0.82f, 0f),
                new Vector3(0.95f, 0.34f, 1f),
                34);
            roleRibbonRenderer = CreateSpriteChild(
                "RoleRibbon",
                RuntimeSpriteLibrary.BannerSprite,
                GetRoleColor(RuntimeState.Role, 0.96f),
                new Vector3(0f, 1.08f, 0f),
                new Vector3(0.58f, 0.18f, 1f),
                36);

            nameText = CreateTextChild("NameText", LocalizationService.Text(RuntimeState.DisplayNameKey, RuntimeState.DisplayName), new Vector3(0f, 0.82f, 0f), 35, 0.075f, TextAlignment.Center, TextAnchor.MiddleCenter);
            nameText.color = new Color(0.96f, 0.93f, 0.84f, 1f);
            roleText = CreateTextChild("RoleText", GetRoleShortText(RuntimeState.Role), new Vector3(0f, 1.08f, 0f), 37, 0.06f, TextAlignment.Center, TextAnchor.MiddleCenter);
            roleText.color = new Color(0.14f, 0.1f, 0.08f, 1f);

            statusText = CreateTextChild("StatusText", string.Empty, new Vector3(0f, 0.56f, 0f), 35, 0.055f, TextAlignment.Center, TextAnchor.MiddleCenter);
            statusText.color = new Color(0.99f, 0.87f, 0.5f, 1f);

            hpBackRenderer = CreateSpriteChild("HpBack", RuntimeSpriteLibrary.WhiteSprite, new Color(0.16f, 0.12f, 0.1f, 0.9f), new Vector3(0f, -0.82f, 0f), new Vector3(HpBarWidth, 0.1f, 1f), 33);
            hpFillRenderer = CreateSpriteChild("HpFill", RuntimeSpriteLibrary.WhiteSprite, new Color(0.45f, 0.82f, 0.39f, 1f), new Vector3(0f, -0.82f, 0f), new Vector3(HpBarWidth, 0.06f, 1f), 34);
        }

        private void SyncVisualState()
        {
            if (RuntimeState == null)
            {
                return;
            }

            bool hasActed = RuntimeState.HasActed;
            float hpRatio = RuntimeState.MaxHp > 0 ? (float)RuntimeState.CurrentHp / RuntimeState.MaxHp : 0f;
            Color spriteTint = hasActed ? new Color(0.68f, 0.68f, 0.68f, 1f) : Color.white;
            Color roleColor = GetRoleColor(RuntimeState.Role, hasActed ? 0.54f : 0.96f);
            Color plateColor = RuntimeState.Faction == UnitFaction.Player
                ? (hasActed ? new Color(0.17f, 0.18f, 0.23f, 0.95f) : new Color(0.14f, 0.21f, 0.38f, 0.95f))
                : (hasActed ? new Color(0.26f, 0.18f, 0.18f, 0.95f) : new Color(0.41f, 0.13f, 0.11f, 0.95f));

            spriteRenderer.color = spriteTint;
            roleAuraRenderer.color = GetRoleColor(RuntimeState.Role, hasActed ? 0.08f : 0.18f);
            namePlateRenderer.color = plateColor;
            roleRibbonRenderer.color = roleColor;
            nameText.text = LocalizationService.Text(RuntimeState.DisplayNameKey, RuntimeState.DisplayName);
            roleText.text = GetRoleShortText(RuntimeState.Role);
            statusText.text = hasActed
                ? LocalizationService.Text("ui.status.done", "DONE")
                : LocalizationService.Text("ui.status.ready", "READY");
            statusText.color = hasActed ? new Color(0.82f, 0.75f, 0.64f, 1f) : new Color(0.97f, 0.9f, 0.55f, 1f);

            float fillWidth = Mathf.Max(0.01f, HpBarWidth * hpRatio);
            hpFillRenderer.transform.localScale = new Vector3(fillWidth, 0.06f, 1f);
            hpFillRenderer.transform.localPosition = new Vector3(-((HpBarWidth - fillWidth) * 0.5f), -0.82f, 0f);
            hpFillRenderer.color = hpRatio > 0.55f
                ? new Color(0.41f, 0.83f, 0.37f, 1f)
                : hpRatio > 0.3f
                    ? new Color(0.9f, 0.72f, 0.21f, 1f)
                    : new Color(0.9f, 0.33f, 0.27f, 1f);
        }

        private static Color GetRoleColor(UnitRole role, float alpha)
        {
            Color baseColor;
            switch (role)
            {
                case UnitRole.Commander:
                    baseColor = new Color(0.91f, 0.75f, 0.28f, alpha);
                    break;
                case UnitRole.Guardian:
                    baseColor = new Color(0.55f, 0.69f, 0.83f, alpha);
                    break;
                case UnitRole.Ranger:
                    baseColor = new Color(0.28f, 0.77f, 0.61f, alpha);
                    break;
                case UnitRole.Scout:
                    baseColor = new Color(0.56f, 0.84f, 0.35f, alpha);
                    break;
                case UnitRole.Raider:
                    baseColor = new Color(0.88f, 0.42f, 0.33f, alpha);
                    break;
                default:
                    baseColor = new Color(0.78f, 0.78f, 0.78f, alpha);
                    break;
            }

            return baseColor;
        }

        private static string GetRoleShortText(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Commander:
                    return LocalizationService.Text("ui.role.short.commander", "CMD");
                case UnitRole.Guardian:
                    return LocalizationService.Text("ui.role.short.guardian", "GDN");
                case UnitRole.Ranger:
                    return LocalizationService.Text("ui.role.short.ranger", "RNG");
                case UnitRole.Scout:
                    return LocalizationService.Text("ui.role.short.scout", "SCT");
                case UnitRole.Raider:
                    return LocalizationService.Text("ui.role.short.raider", "RDR");
                default:
                    return LocalizationService.Text("ui.role.short.unknown", "UNIT");
            }
        }

        private void ApplySelectionState()
        {
            if (frameRenderer != null)
            {
                frameRenderer.enabled = isSelected && RuntimeState != null && RuntimeState.IsAlive;
            }

            transform.localScale = isSelected ? restingScale * 1.06f : restingScale;
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

        private SpriteRenderer CreateSpriteChild(string childName, Sprite sprite, Color color, Vector3 localPosition, Vector3 localScale, int sortingOrder)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(transform, false);
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
            string content,
            Vector3 localPosition,
            int sortingOrder,
            float characterSize,
            TextAlignment alignment,
            TextAnchor anchor)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = localPosition + new Vector3(0f, 0f, -0.01f);

            TextMesh textMesh = child.AddComponent<TextMesh>();
            textMesh.text = content;
            textMesh.font = RuntimeSpriteLibrary.DefaultFont;
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
    }
}
