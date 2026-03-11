using System;
using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    [Serializable]
    public sealed class StageDefinitionData
    {
        public StageDefinitionData(
            string stageName,
            string stageNameKey,
            int width,
            int height,
            IReadOnlyList<UnitSpawnData> unitSpawns,
            IReadOnlyList<GridPosition> blockedCells,
            bool isRandomMap = false,
            int mapSeed = 0)
            : this(
                stageName,
                stageNameKey,
                width,
                height,
                unitSpawns,
                blockedCells,
                new List<TerrainTileData>(),
                isRandomMap,
                mapSeed)
        {
        }

        public StageDefinitionData(
            string stageName,
            string stageNameKey,
            int width,
            int height,
            IReadOnlyList<UnitSpawnData> unitSpawns,
            IReadOnlyList<GridPosition> blockedCells,
            IReadOnlyList<TerrainTileData> terrainTiles,
            bool isRandomMap = false,
            int mapSeed = 0)
        {
            StageName = stageName;
            StageNameKey = stageNameKey;
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            UnitSpawns = unitSpawns != null ? unitSpawns.ToList() : new List<UnitSpawnData>();
            BlockedCells = blockedCells != null ? blockedCells.ToList() : new List<GridPosition>();
            TerrainTiles = terrainTiles != null ? terrainTiles.ToList() : new List<TerrainTileData>();
            IsRandomMap = isRandomMap;
            MapSeed = mapSeed;
            Validate();
        }

        public string StageName { get; }

        public string StageNameKey { get; }

        public int Width { get; }

        public int Height { get; }

        public IReadOnlyList<UnitSpawnData> UnitSpawns { get; }

        public IReadOnlyList<GridPosition> BlockedCells { get; }

        public IReadOnlyList<TerrainTileData> TerrainTiles { get; }

        public bool IsRandomMap { get; }

        public int MapSeed { get; }

        private void Validate()
        {
            HashSet<GridPosition> blockedLookup = new HashSet<GridPosition>();
            foreach (GridPosition blockedCell in BlockedCells)
            {
                ValidateInsideBounds(blockedCell, "blocked cell");
                blockedLookup.Add(blockedCell);
            }

            HashSet<GridPosition> occupied = new HashSet<GridPosition>();
            foreach (UnitSpawnData spawn in UnitSpawns)
            {
                if (spawn == null || spawn.Definition == null)
                {
                    throw new ArgumentException("Stage contains a null unit spawn.", nameof(UnitSpawns));
                }

                ValidateInsideBounds(spawn.StartPosition, "unit spawn");
                if (blockedLookup.Contains(spawn.StartPosition))
                {
                    throw new ArgumentException($"Unit spawn {spawn.Definition.Id} cannot be placed on a blocked cell.", nameof(UnitSpawns));
                }

                if (!occupied.Add(spawn.StartPosition))
                {
                    throw new ArgumentException($"Multiple units cannot share the same spawn position {spawn.StartPosition}.", nameof(UnitSpawns));
                }
            }

            foreach (TerrainTileData terrainTile in TerrainTiles)
            {
                if (terrainTile == null)
                {
                    throw new ArgumentException("Stage contains a null terrain tile.", nameof(TerrainTiles));
                }

                ValidateInsideBounds(terrainTile.Position, "terrain tile");
            }
        }

        private void ValidateInsideBounds(GridPosition position, string label)
        {
            if (position.X < 0 || position.X >= Width || position.Y < 0 || position.Y >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(position), $"{label} {position} is outside the {Width}x{Height} board.");
            }
        }
    }
}
