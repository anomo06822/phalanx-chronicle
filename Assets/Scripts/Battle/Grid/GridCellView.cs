using System;
using PhalanxChronicle.Core;
using PhalanxChronicle.Presentation;
using UnityEngine;

namespace PhalanxChronicle.Battle.Grid
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class GridCellView : MonoBehaviour
    {
        private Action<GridCellView> clickHandler;
        private Action<GridCellView, bool> hoverChangedHandler;
        private SpriteRenderer baseRenderer;
        private SpriteRenderer terrainRenderer;
        private SpriteRenderer propRenderer;
        private SpriteRenderer frameRenderer;
        private SpriteRenderer moveHighlightRenderer;
        private SpriteRenderer attackHighlightRenderer;
        private SpriteRenderer skillHighlightRenderer;
        private SpriteRenderer pathHighlightRenderer;
        private SpriteRenderer selectedHighlightRenderer;
        private SpriteRenderer previewDestinationRenderer;
        private Color baseColor = Color.white;
        private Color frameBaseColor = Color.white;

        public GridPosition Position { get; private set; }

        internal void Initialize(
            GridPosition position,
            TerrainType terrainType,
            bool blocked,
            bool alternate,
            string terrainPaletteId,
            TerrainVariantDefinition terrainVariant,
            Action<GridCellView> onClicked,
            Action<GridCellView, bool> onHoverChanged)
        {
            Position = position;
            clickHandler = onClicked;
            hoverChangedHandler = onHoverChanged;
            Color terrainBaseTint = RuntimeSpriteLibrary.GetTerrainBaseTint(terrainPaletteId, terrainType, blocked);
            Color terrainOverlayTint = RuntimeSpriteLibrary.GetTerrainOverlayTint(terrainPaletteId, terrainType, blocked);
            Color terrainPropTint = RuntimeSpriteLibrary.GetTerrainPropTint(terrainPaletteId, terrainType, blocked);
            baseColor = alternate
                ? Color.Lerp(terrainBaseTint, Color.white, 0.08f)
                : Color.Lerp(terrainBaseTint, Color.black, 0.08f);
            frameBaseColor = BattleUiTheme.GetGridFrameColor(terrainType, blocked);

            baseRenderer = GetComponent<SpriteRenderer>();
            baseRenderer.sprite = RuntimeSpriteLibrary.GetTerrainBaseSprite(terrainVariant != null ? terrainVariant.BaseResourcePath : string.Empty, terrainType, blocked);
            baseRenderer.color = baseColor;
            baseRenderer.sortingOrder = 10;

            moveHighlightRenderer = CreateOverlay("MoveHighlight", RuntimeSpriteLibrary.GetGridOverlaySprite(GridOverlayKind.Move), Vector3.zero, new Vector3(0.96f, 0.96f, 1f), 11);
            attackHighlightRenderer = CreateOverlay("AttackHighlight", RuntimeSpriteLibrary.GetGridOverlaySprite(GridOverlayKind.Attack), Vector3.zero, new Vector3(0.96f, 0.96f, 1f), 11);
            skillHighlightRenderer = CreateOverlay("SkillHighlight", RuntimeSpriteLibrary.GetGridOverlaySprite(GridOverlayKind.Skill), Vector3.zero, new Vector3(0.96f, 0.96f, 1f), 11);
            pathHighlightRenderer = CreateOverlay("PathHighlight", RuntimeSpriteLibrary.GetGridOverlaySprite(GridOverlayKind.Selected), Vector3.zero, new Vector3(0.9f, 0.9f, 1f), 11);

            terrainRenderer = CreateOverlay("TerrainOverlay", RuntimeSpriteLibrary.GetTerrainOverlaySprite(terrainVariant != null ? terrainVariant.OverlayResourcePath : string.Empty, terrainType, blocked), Vector3.zero, Vector3.one, 12);
            terrainRenderer.color = terrainOverlayTint;
            terrainRenderer.enabled = terrainRenderer.sprite != null;

            propRenderer = CreateOverlay("TerrainProp", RuntimeSpriteLibrary.GetTerrainPropSprite(terrainVariant != null ? terrainVariant.PropResourcePath : string.Empty, terrainType, blocked), new Vector3(0f, 0.02f, 0f), new Vector3(0.92f, 0.92f, 1f), 13);
            propRenderer.color = terrainPropTint;
            propRenderer.enabled = propRenderer.sprite != null;

            frameRenderer = CreateOverlay("GridFrame", RuntimeSpriteLibrary.FrameSprite, Vector3.zero, new Vector3(1.03f, 1.03f, 1f), 14);
            frameRenderer.color = frameBaseColor;
            frameRenderer.enabled = true;

            selectedHighlightRenderer = CreateOverlay("SelectedHighlight", RuntimeSpriteLibrary.GetGridOverlaySprite(GridOverlayKind.Selected), Vector3.zero, new Vector3(1.01f, 1.01f, 1f), 15);
            previewDestinationRenderer = CreateOverlay("PreviewDestinationHighlight", RuntimeSpriteLibrary.GetGridOverlaySprite(GridOverlayKind.Selected), Vector3.zero, new Vector3(0.94f, 0.94f, 1f), 15);
            ClearHighlights();

            BoxCollider2D colliderComponent = GetComponent<BoxCollider2D>();
            colliderComponent.size = Vector2.one;
            colliderComponent.isTrigger = false;
        }

        public void ClearHighlights()
        {
            baseRenderer.color = baseColor;
            frameRenderer.color = frameBaseColor;
            moveHighlightRenderer.enabled = false;
            attackHighlightRenderer.enabled = false;
            skillHighlightRenderer.enabled = false;
            pathHighlightRenderer.enabled = false;
            selectedHighlightRenderer.enabled = false;
            previewDestinationRenderer.enabled = false;
        }

        public void SetMoveHighlight(Color color)
        {
            ApplyHighlightTint(BattleUiTheme.MoveTileTint, 0.16f, BattleUiTheme.MoveFrameHighlight);
            moveHighlightRenderer.color = color;
            moveHighlightRenderer.enabled = true;
        }

        public void SetAttackHighlight(Color color)
        {
            ApplyHighlightTint(BattleUiTheme.AttackTileTint, 0.14f, BattleUiTheme.AttackFrameHighlight);
            attackHighlightRenderer.color = color;
            attackHighlightRenderer.enabled = true;
        }

        public void SetSkillHighlight(Color color)
        {
            ApplyHighlightTint(BattleUiTheme.SkillTileTint, 0.14f, BattleUiTheme.SkillFrameHighlight);
            skillHighlightRenderer.color = color;
            skillHighlightRenderer.enabled = true;
        }

        public void SetSelectedHighlight(Color color)
        {
            ApplyHighlightTint(BattleUiTheme.SelectedTileTint, 0.18f, BattleUiTheme.SelectedFrameHighlight);
            selectedHighlightRenderer.color = color;
            selectedHighlightRenderer.enabled = true;
        }

        public void SetPathHighlight(Color color)
        {
            ApplyHighlightTint(BattleUiTheme.PathTileTint, 0.1f, BattleUiTheme.PathFrameHighlight);
            pathHighlightRenderer.color = color;
            pathHighlightRenderer.enabled = true;
        }

        public void SetPreviewDestinationHighlight(Color color)
        {
            ApplyHighlightTint(BattleUiTheme.SelectedTileTint, 0.12f, BattleUiTheme.SelectedFrameHighlight);
            previewDestinationRenderer.color = color;
            previewDestinationRenderer.enabled = true;
        }

        private void ApplyHighlightTint(Color tileTint, float blendStrength, Color frameColor)
        {
            baseRenderer.color = Color.Lerp(baseColor, tileTint, Mathf.Clamp01(blendStrength));
            frameRenderer.color = frameColor;
        }

        private SpriteRenderer CreateOverlay(string childName, Sprite sprite, Vector3 localPosition, Vector3 localScale, int sortingOrder)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;

            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;
            renderer.enabled = false;
            return renderer;
        }

        private void OnMouseUpAsButton()
        {
            clickHandler?.Invoke(this);
        }

        private void OnMouseEnter()
        {
            hoverChangedHandler?.Invoke(this, true);
        }

        private void OnMouseExit()
        {
            hoverChangedHandler?.Invoke(this, false);
        }
    }
}
