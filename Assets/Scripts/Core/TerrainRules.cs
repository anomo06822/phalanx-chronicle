using System;

namespace PhalanxChronicle.Core
{
    public enum TerrainType
    {
        Plain = 0,
        Forest = 1,
        Fort = 2,
        Hazard = 3,
    }

    [Serializable]
    public sealed class TerrainTileData
    {
        public TerrainTileData(GridPosition position, TerrainType terrainType)
        {
            Position = position;
            TerrainType = terrainType;
        }

        public GridPosition Position { get; }

        public TerrainType TerrainType { get; }
    }

    public static class TerrainRules
    {
        public static int GetMoveCost(UnitRuntimeState unit, TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.Forest:
                    return unit != null && (unit.Role == UnitRole.Ranger || unit.Role == UnitRole.Scout || unit.PassiveSkill == PassiveSkillType.GaleStride) ? 1 : 2;
                case TerrainType.Hazard:
                    return unit != null && (unit.Role == UnitRole.Raider || unit.PassiveSkill == PassiveSkillType.GaleStride) ? 1 : 2;
                default:
                    return 1;
            }
        }

        public static int GetDefenseBonus(TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.Forest:
                    return 1;
                case TerrainType.Fort:
                    return 2;
                default:
                    return 0;
            }
        }

        public static int GetEndTurnHealing(TerrainType terrainType)
        {
            return terrainType == TerrainType.Fort ? 2 : 0;
        }

        public static int GetEndTurnDamage(TerrainType terrainType)
        {
            return terrainType == TerrainType.Hazard ? 2 : 0;
        }

        public static string GetNameKey(TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.Forest:
                    return "terrain.forest.name";
                case TerrainType.Fort:
                    return "terrain.fort.name";
                case TerrainType.Hazard:
                    return "terrain.hazard.name";
                default:
                    return "terrain.plain.name";
            }
        }
    }
}
