using System;
using System.Collections.Generic;
using PhalanxChronicle.Core;
using PhalanxChronicle.Presentation;
using UnityEngine;

namespace PhalanxChronicle.Battle.Grid
{
    public sealed class GridManager : MonoBehaviour
    {
        private readonly Dictionary<GridPosition, GridCellView> cellViews = new Dictionary<GridPosition, GridCellView>();
        private int width;
        private int height;
        private float cellSize = 1f;

        public void BuildGrid(int gridWidth, int gridHeight, IReadOnlyCollection<GridPosition> blockedCells, Action<GridCellView> onCellClicked)
        {
            width = gridWidth;
            height = gridHeight;
            ClearGrid();
            CreateBoardBackdrop();

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    GameObject cellObject = new GameObject($"Cell_{x}_{y}");
                    cellObject.transform.SetParent(transform, false);
                    cellObject.transform.position = GetWorldPosition(position);
                    cellObject.transform.localScale = Vector3.one * 0.98f;

                    GridCellView cellView = cellObject.AddComponent<GridCellView>();
                    bool blocked = ContainsPosition(blockedCells, position);
                    Color baseColor = blocked
                        ? ((x + y) % 2 == 0
                            ? new Color(0.33f, 0.43f, 0.31f, 1f)
                            : new Color(0.28f, 0.37f, 0.27f, 1f))
                        : ((x + y) % 2 == 0
                            ? new Color(0.93f, 0.86f, 0.67f, 1f)
                            : new Color(0.87f, 0.78f, 0.58f, 1f));
                    cellView.Initialize(position, baseColor, onCellClicked);
                    cellViews[position] = cellView;
                }
            }
        }

        public Vector3 GetWorldPosition(GridPosition position)
        {
            float offsetX = -((width - 1) * cellSize) * 0.5f;
            float offsetY = -((height - 1) * cellSize) * 0.5f;
            return new Vector3(offsetX + (position.X * cellSize), offsetY + (position.Y * cellSize), 0f);
        }

        public void ClearHighlights()
        {
            foreach (GridCellView cellView in cellViews.Values)
            {
                cellView.ClearHighlights();
            }
        }

        public void ShowMoveRange(IEnumerable<GridPosition> positions)
        {
            foreach (GridPosition position in positions)
            {
                if (cellViews.TryGetValue(position, out GridCellView cellView))
                {
                    cellView.SetMoveHighlight(new Color(0.3f, 0.55f, 0.95f, 0.7f));
                }
            }
        }

        public void ShowAttackRange(IEnumerable<GridPosition> positions)
        {
            foreach (GridPosition position in positions)
            {
                if (cellViews.TryGetValue(position, out GridCellView cellView))
                {
                    cellView.SetAttackHighlight(new Color(0.91f, 0.28f, 0.22f, 0.92f));
                }
            }
        }

        public void ShowSkillRange(IEnumerable<GridPosition> positions)
        {
            foreach (GridPosition position in positions)
            {
                if (cellViews.TryGetValue(position, out GridCellView cellView))
                {
                    cellView.SetSkillHighlight(new Color(0.34f, 0.78f, 0.42f, 0.92f));
                }
            }
        }

        public void HighlightSelectedCell(GridPosition position)
        {
            if (cellViews.TryGetValue(position, out GridCellView cellView))
            {
                cellView.SetSelectedHighlight(new Color(0.98f, 0.85f, 0.25f, 0.95f));
            }
        }

        private void ClearGrid()
        {
            cellViews.Clear();
            for (int index = transform.childCount - 1; index >= 0; index--)
            {
                Destroy(transform.GetChild(index).gameObject);
            }
        }

        private static bool ContainsPosition(IReadOnlyCollection<GridPosition> positions, GridPosition candidate)
        {
            if (positions == null)
            {
                return false;
            }

            foreach (GridPosition position in positions)
            {
                if (position == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateBoardBackdrop()
        {
            GameObject backdropObject = new GameObject("BoardBackdrop");
            backdropObject.transform.SetParent(transform, false);
            backdropObject.transform.position = new Vector3(0f, 0f, 1.2f);
            backdropObject.transform.localScale = new Vector3(width + 1.6f, height + 1.6f, 1f);

            SpriteRenderer backdropRenderer = backdropObject.AddComponent<SpriteRenderer>();
            backdropRenderer.sprite = RuntimeSpriteLibrary.WhiteSprite;
            backdropRenderer.color = new Color(0.27f, 0.2f, 0.13f, 1f);
            backdropRenderer.sortingOrder = 0;

            GameObject frameObject = new GameObject("BoardFrame");
            frameObject.transform.SetParent(transform, false);
            frameObject.transform.position = new Vector3(0f, 0f, 1.1f);
            frameObject.transform.localScale = new Vector3(width + 0.6f, height + 0.6f, 1f);

            SpriteRenderer frameRenderer = frameObject.AddComponent<SpriteRenderer>();
            frameRenderer.sprite = RuntimeSpriteLibrary.FrameSprite;
            frameRenderer.color = new Color(0.93f, 0.81f, 0.45f, 1f);
            frameRenderer.sortingOrder = 1;
        }
    }
}
