using System;
using System.Collections.Generic;
using PhalanxChronicle.Core;
using PhalanxChronicle.Data;
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

        public void BuildGrid(BattleContext context, Action<GridCellView> onCellClicked)
        {
            if (context == null)
            {
                return;
            }

            width = context.Width;
            height = context.Height;
            ClearGrid();
            StageVisualDefinition stageVisual = StageVisualCatalog.GetDefinition(context.StageNameKey);
            CreateBoardBackdrop(stageVisual);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    GridCell cell = context.GetCell(position);
                    GameObject cellObject = new GameObject($"Cell_{x}_{y}");
                    cellObject.transform.SetParent(transform, false);
                    cellObject.transform.position = GetWorldPosition(position);
                    cellObject.transform.localScale = Vector3.one * 0.98f;

                    GridCellView cellView = cellObject.AddComponent<GridCellView>();
                    cellView.Initialize(position, cell.TerrainType, cell.IsBlocked, (x + y) % 2 == 0, stageVisual.TerrainPaletteId, onCellClicked);
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

        private void CreateBoardBackdrop(StageVisualDefinition stageVisual)
        {
            CreateBackdropLayer(
                "SkyGradient",
                RuntimeSpriteLibrary.GetVerticalGradientSprite(stageVisual.SkyTopColor, stageVisual.SkyBottomColor),
                Color.white,
                new Vector3(0f, 0.55f, 1.58f),
                new Vector3(width + 5f, height + 4f, 1f),
                0);

            Sprite farBackdrop = stageVisual.FarBackdropSprite != null
                ? stageVisual.FarBackdropSprite
                : RuntimeSpriteLibrary.GetStageBackdropSprite(stageVisual.StageNameKey, StageBackdropLayer.Far);
            CreateBackdropLayer(
                "FarBackdrop",
                farBackdrop,
                BuildBackdropTint(stageVisual.SkyBottomColor, stageVisual.HazeColor, 0.68f),
                new Vector3(0f, 1.55f, 1.46f),
                new Vector3(width + 4.4f, Mathf.Max(2.1f, height * 0.24f), 1f),
                1);

            Sprite midBackdrop = stageVisual.MidBackdropSprite != null
                ? stageVisual.MidBackdropSprite
                : RuntimeSpriteLibrary.GetStageBackdropSprite(stageVisual.StageNameKey, StageBackdropLayer.Mid);
            CreateBackdropLayer(
                "MidBackdrop",
                midBackdrop,
                BuildBackdropTint(stageVisual.BoardFrameColor, stageVisual.HazeColor, 0.84f),
                new Vector3(0f, 1.05f, 1.38f),
                new Vector3(width + 2.8f, Mathf.Max(2.4f, height * 0.28f), 1f),
                2);

            CreateBackdropLayer(
                "HazeFar",
                RuntimeSpriteLibrary.MistBandSprite,
                stageVisual.HazeColor,
                new Vector3(0f, 1.32f, 1.33f),
                new Vector3(width + 3.4f, Mathf.Max(2.4f, height * 0.34f), 1f),
                3);

            CreateBackdropLayer(
                "HazeNear",
                RuntimeSpriteLibrary.MistBandSprite,
                new Color(stageVisual.HazeColor.r, stageVisual.HazeColor.g, stageVisual.HazeColor.b, stageVisual.HazeColor.a * 0.82f),
                new Vector3(0f, 0.28f, 1.18f),
                new Vector3(width + 2.4f, Mathf.Max(1.8f, height * 0.18f), 1f),
                4);

            Sprite landmarkSprite = stageVisual.LandmarkSprite != null
                ? stageVisual.LandmarkSprite
                : RuntimeSpriteLibrary.GetStageLandmarkSprite(stageVisual.StageNameKey);
            if (landmarkSprite != null)
            {
                float landmarkScaleX = width + 1.4f;
                float landmarkScaleY = Mathf.Max(2f, height * 0.34f);
                Color landmarkTint = Color.Lerp(stageVisual.BoardFrameColor, stageVisual.HazeColor, 0.32f);
                landmarkTint.a = 0.78f;
                CreateBackdropLayer(
                    "StageLandmark",
                    landmarkSprite,
                    landmarkTint,
                    new Vector3(0f, 1.35f, 1.25f),
                    new Vector3(landmarkScaleX, landmarkScaleY, 1f),
                    5);
            }

            CreateBackdropLayer(
                "BoardBackdrop",
                RuntimeSpriteLibrary.WhiteSprite,
                stageVisual.BoardBackdropColor,
                new Vector3(0f, 0f, 1.12f),
                new Vector3(width + 1.6f, height + 1.6f, 1f),
                6);

            CreateBackdropLayer(
                "BoardFrame",
                RuntimeSpriteLibrary.FrameSprite,
                stageVisual.BoardFrameColor,
                new Vector3(0f, 0f, 1.06f),
                new Vector3(width + 0.65f, height + 0.65f, 1f),
                7);

            CreateAmbientLayers(stageVisual);
        }

        private void CreateAmbientLayers(StageVisualDefinition stageVisual)
        {
            string presetId = stageVisual != null ? stageVisual.AmbientParticlePresetId : string.Empty;
            if (string.IsNullOrWhiteSpace(presetId))
            {
                return;
            }

            if (presetId == "embers")
            {
                CreateBackdropLayer(
                    "AmbientFront",
                    RuntimeSpriteLibrary.GetAmbientOverlaySprite(presetId),
                    new Color(0.92f, 0.61f, 0.31f, 0.36f),
                    new Vector3(0f, -0.2f, 1.08f),
                    new Vector3(width + 1.4f, 1.2f, 1f),
                    8);
                return;
            }

            if (presetId == "river-mist")
            {
                CreateBackdropLayer(
                    "AmbientFront",
                    RuntimeSpriteLibrary.GetAmbientOverlaySprite(presetId),
                    new Color(0.83f, 0.87f, 0.88f, 0.22f),
                    new Vector3(0f, -0.24f, 1.08f),
                    new Vector3(width + 1.8f, 1.5f, 1f),
                    8);
                return;
            }

            if (presetId == "mountain-wind")
            {
                CreateBackdropLayer(
                    "AmbientFront",
                    RuntimeSpriteLibrary.GetAmbientOverlaySprite(presetId),
                    new Color(0.79f, 0.82f, 0.76f, 0.24f),
                    new Vector3(0f, 0.06f, 1.08f),
                    new Vector3(width + 1.5f, 1.8f, 1f),
                    8);
                return;
            }

            CreateBackdropLayer(
                "AmbientFront",
                RuntimeSpriteLibrary.GetAmbientOverlaySprite(presetId),
                new Color(0.82f, 0.74f, 0.6f, 0.2f),
                new Vector3(0f, -0.2f, 1.08f),
                new Vector3(width + 1.4f, 1.1f, 1f),
                8);
        }

        private static Color BuildBackdropTint(Color primary, Color secondary, float alpha)
        {
            Color tint = Color.Lerp(primary, secondary, 0.28f);
            tint.a = alpha;
            return tint;
        }

        private void CreateBackdropLayer(string name, Sprite sprite, Color color, Vector3 position, Vector3 scale, int sortingOrder)
        {
            if (sprite == null)
            {
                return;
            }

            GameObject layerObject = new GameObject(name);
            layerObject.transform.SetParent(transform, false);
            layerObject.transform.position = position;
            layerObject.transform.localScale = scale;

            SpriteRenderer renderer = layerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
