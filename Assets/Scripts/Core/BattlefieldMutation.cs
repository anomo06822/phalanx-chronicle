using System;
using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    [Serializable]
    public sealed class BlockedCellStateChange
    {
        public BlockedCellStateChange(GridPosition position, bool isBlocked)
        {
            Position = position;
            IsBlocked = isBlocked;
        }

        public GridPosition Position { get; }

        public bool IsBlocked { get; }
    }

    [Serializable]
    public sealed class BattlefieldMutation
    {
        public BattlefieldMutation(
            IReadOnlyList<TerrainTileData> terrainChanges,
            IReadOnlyList<BlockedCellStateChange> blockedStateChanges = null)
        {
            TerrainChanges = terrainChanges ?? Array.Empty<TerrainTileData>();
            BlockedStateChanges = blockedStateChanges ?? Array.Empty<BlockedCellStateChange>();
        }

        public IReadOnlyList<TerrainTileData> TerrainChanges { get; }

        public IReadOnlyList<BlockedCellStateChange> BlockedStateChanges { get; }

        public bool HasAnyChange => TerrainChanges.Count > 0 || BlockedStateChanges.Count > 0;
    }
}
