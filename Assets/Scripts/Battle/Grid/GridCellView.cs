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
        private SpriteRenderer baseRenderer;
        private SpriteRenderer moveHighlightRenderer;
        private SpriteRenderer attackHighlightRenderer;
        private SpriteRenderer skillHighlightRenderer;
        private SpriteRenderer selectedHighlightRenderer;
        private Color baseColor;

        public GridPosition Position { get; private set; }

        public void Initialize(GridPosition position, Color color, Action<GridCellView> onClicked)
        {
            Position = position;
            baseColor = color;
            clickHandler = onClicked;
            baseRenderer = GetComponent<SpriteRenderer>();
            baseRenderer.sprite = RuntimeSpriteLibrary.TileSprite;
            baseRenderer.color = color;
            baseRenderer.sortingOrder = 10;

            moveHighlightRenderer = CreateOverlay("MoveHighlight", RuntimeSpriteLibrary.WhiteSprite, new Vector3(0f, 0f, -0.01f), new Vector3(0.82f, 0.82f, 1f), 11);
            attackHighlightRenderer = CreateOverlay("AttackHighlight", RuntimeSpriteLibrary.WhiteSprite, new Vector3(0f, 0f, -0.02f), new Vector3(0.48f, 0.48f, 1f), 12);
            skillHighlightRenderer = CreateOverlay("SkillHighlight", RuntimeSpriteLibrary.WhiteSprite, new Vector3(0f, 0f, -0.02f), new Vector3(0.58f, 0.58f, 1f), 12);
            skillHighlightRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            selectedHighlightRenderer = CreateOverlay("SelectedHighlight", RuntimeSpriteLibrary.FrameSprite, new Vector3(0f, 0f, -0.03f), new Vector3(1.08f, 1.08f, 1f), 13);
            ClearHighlights();

            BoxCollider2D colliderComponent = GetComponent<BoxCollider2D>();
            colliderComponent.size = Vector2.one;
            colliderComponent.isTrigger = false;
        }

        public void ClearHighlights()
        {
            baseRenderer.color = baseColor;
            moveHighlightRenderer.enabled = false;
            attackHighlightRenderer.enabled = false;
            skillHighlightRenderer.enabled = false;
            selectedHighlightRenderer.enabled = false;
        }

        public void SetMoveHighlight(Color color)
        {
            moveHighlightRenderer.color = color;
            moveHighlightRenderer.enabled = true;
        }

        public void SetAttackHighlight(Color color)
        {
            attackHighlightRenderer.color = color;
            attackHighlightRenderer.enabled = true;
        }

        public void SetSkillHighlight(Color color)
        {
            skillHighlightRenderer.color = color;
            skillHighlightRenderer.enabled = true;
        }

        public void SetSelectedHighlight(Color color)
        {
            selectedHighlightRenderer.color = color;
            selectedHighlightRenderer.enabled = true;
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
            renderer.enabled = false;
            return renderer;
        }

        private void OnMouseUpAsButton()
        {
            clickHandler?.Invoke(this);
        }
    }
}
