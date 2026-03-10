using System;
using System.Collections.Generic;

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
        {
            StageName = stageName;
            StageNameKey = stageNameKey;
            Width = width;
            Height = height;
            UnitSpawns = unitSpawns;
            BlockedCells = blockedCells;
            IsRandomMap = isRandomMap;
            MapSeed = mapSeed;
        }

        public string StageName { get; }

        public string StageNameKey { get; }

        public int Width { get; }

        public int Height { get; }

        public IReadOnlyList<UnitSpawnData> UnitSpawns { get; }

        public IReadOnlyList<GridPosition> BlockedCells { get; }

        public bool IsRandomMap { get; }

        public int MapSeed { get; }
    }
}
