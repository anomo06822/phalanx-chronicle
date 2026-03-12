using System;
using PhalanxChronicle.Core;

namespace PhalanxChronicle.Presentation
{
    internal sealed class TerrainVariantDefinition
    {
        public TerrainVariantDefinition(string baseResourcePath, string overlayResourcePath, string propResourcePath)
        {
            BaseResourcePath = baseResourcePath ?? string.Empty;
            OverlayResourcePath = overlayResourcePath ?? string.Empty;
            PropResourcePath = propResourcePath ?? string.Empty;
        }

        public string BaseResourcePath { get; }

        public string OverlayResourcePath { get; }

        public string PropResourcePath { get; }
    }

    internal static class TerrainVariantResolver
    {
        public static TerrainVariantDefinition Resolve(
            BattleContext context,
            GridPosition position,
            TerrainType terrainType,
            bool blocked,
            string paletteId)
        {
            string normalizedPalette = NormalizePaletteId(paletteId);
            string terrainKey = blocked ? "blocked" : GetTerrainKey(terrainType);
            char variantKey = SelectVariantKey(context, position, terrainType, blocked, normalizedPalette);
            string resourcePrefix = $"Terrain/{normalizedPalette}/{terrainKey}_{variantKey}";
            return new TerrainVariantDefinition(
                resourcePrefix + "_base",
                resourcePrefix + "_overlay",
                resourcePrefix + "_prop");
        }

        private static char SelectVariantKey(BattleContext context, GridPosition position, TerrainType terrainType, bool blocked, string paletteId)
        {
            int mask = 0;
            int matchingNeighbors = 0;

            if (Matches(context, position, 0, 1, terrainType, blocked))
            {
                mask |= 1;
                matchingNeighbors++;
            }

            if (Matches(context, position, 1, 0, terrainType, blocked))
            {
                mask |= 2;
                matchingNeighbors++;
            }

            if (Matches(context, position, 0, -1, terrainType, blocked))
            {
                mask |= 4;
                matchingNeighbors++;
            }

            if (Matches(context, position, -1, 0, terrainType, blocked))
            {
                mask |= 8;
                matchingNeighbors++;
            }

            if (!blocked)
            {
                bool verticalLane = (mask & 1) != 0 && (mask & 4) != 0 && (mask & 2) == 0 && (mask & 8) == 0;
                bool horizontalLane = (mask & 2) != 0 && (mask & 8) != 0 && (mask & 1) == 0 && (mask & 4) == 0;
                if (verticalLane)
                {
                    return 'b';
                }

                if (horizontalLane)
                {
                    return 'c';
                }

                if (matchingNeighbors >= 3)
                {
                    return 'a';
                }
            }

            int hash = Math.Abs((GetStableHash(paletteId) * 397) ^ (position.X * 31) ^ (position.Y * 17) ^ mask);
            return (char)('a' + (hash % 3));
        }

        private static bool Matches(BattleContext context, GridPosition origin, int offsetX, int offsetY, TerrainType terrainType, bool blocked)
        {
            if (context == null)
            {
                return false;
            }

            GridPosition target = new GridPosition(origin.X + offsetX, origin.Y + offsetY);
            if (!context.IsInside(target))
            {
                return false;
            }

            GridCell cell = context.GetCell(target);
            if (cell == null)
            {
                return false;
            }

            if (blocked)
            {
                return cell.IsBlocked;
            }

            return !cell.IsBlocked && cell.TerrainType == terrainType;
        }

        private static string NormalizePaletteId(string paletteId)
        {
            return string.IsNullOrWhiteSpace(paletteId) ? "frontier-plain" : paletteId.Trim().ToLowerInvariant();
        }

        private static int GetStableHash(string value)
        {
            unchecked
            {
                int hash = 23;
                if (!string.IsNullOrEmpty(value))
                {
                    for (int index = 0; index < value.Length; index++)
                    {
                        hash = (hash * 31) + value[index];
                    }
                }

                return hash;
            }
        }

        private static string GetTerrainKey(TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.Forest:
                    return "forest";
                case TerrainType.Fort:
                    return "fort";
                case TerrainType.Hazard:
                    return "hazard";
                default:
                    return "plain";
            }
        }
    }
}
