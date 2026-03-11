namespace PhalanxChronicle.Core
{
    public sealed class GridCell
    {
        public GridCell(GridPosition position)
        {
            Position = position;
            TerrainType = TerrainType.Plain;
        }

        public GridPosition Position { get; }

        public bool IsBlocked { get; private set; }

        public TerrainType TerrainType { get; private set; }

        public string OccupantUnitId { get; private set; }

        public bool IsOccupied => !string.IsNullOrEmpty(OccupantUnitId);

        public bool IsWalkable => !IsBlocked;

        public void SetBlocked(bool blocked)
        {
            IsBlocked = blocked;
        }

        public void SetTerrain(TerrainType terrainType)
        {
            TerrainType = terrainType;
        }

        public void SetOccupant(string unitId)
        {
            OccupantUnitId = unitId;
        }

        public void ClearOccupant()
        {
            OccupantUnitId = null;
        }
    }
}
