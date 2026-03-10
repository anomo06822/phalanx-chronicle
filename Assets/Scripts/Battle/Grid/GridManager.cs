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
                    Color baseColor = BattleUiTheme.GetGridTileColor(blocked, (x + y) % 2 == 0);
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
                    cellView.SetMoveHighlight(BattleUiTheme.MoveHighlight);
                }
            }
        }

        public void ShowAttackRange(IEnumerable<GridPosition> positions)
        {
            foreach (GridPosition position in positions)
            {
                if (cellViews.TryGetValue(position, out GridCellView cellView))
                {
                    cellView.SetAttackHighlight(BattleUiTheme.AttackHighlight);
                }
            }
        }

        public void ShowSkillRange(IEnumerable<GridPosition> positions)
        {
            foreach (GridPosition position in positions)
            {
                if (cellViews.TryGetValue(position, out GridCellView cellView))
                {
                    cellView.SetSkillHighlight(BattleUiTheme.SkillHighlight);
                }
            }
        }

        public void HighlightSelectedCell(GridPosition position)
        {
            if (cellViews.TryGetValue(position, out GridCellView cellView))
            {
                cellView.SetSelectedHighlight(BattleUiTheme.SelectedHighlight);
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
            backdropRenderer.color = BattleUiTheme.GridBackdrop;
            backdropRenderer.sortingOrder = 0;

            GameObject frameObject = new GameObject("BoardFrame");
            frameObject.transform.SetParent(transform, false);
            frameObject.transform.position = new Vector3(0f, 0f, 1.1f);
            frameObject.transform.localScale = new Vector3(width + 0.6f, height + 0.6f, 1f);

            SpriteRenderer frameRenderer = frameObject.AddComponent<SpriteRenderer>();
            frameRenderer.sprite = RuntimeSpriteLibrary.FrameSprite;
            frameRenderer.color = BattleUiTheme.GridFrame;
            frameRenderer.sortingOrder = 1;
        }
    }
}
