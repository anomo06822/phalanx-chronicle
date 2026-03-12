using PhalanxChronicle.Core;
using UnityEngine;

namespace PhalanxChronicle.Presentation
{
    public static class BattleUiTheme
    {
        public static readonly Color PanelBackdrop = new Color(0.07f, 0.08f, 0.09f, 0.96f);
        public static readonly Color PanelSurface = new Color(0.09f, 0.1f, 0.11f, 0.93f);
        public static readonly Color PanelInset = new Color(0.12f, 0.11f, 0.1f, 0.9f);
        public static readonly Color PanelCommand = new Color(0.16f, 0.13f, 0.1f, 0.94f);
        public static readonly Color PanelOverlay = new Color(0f, 0f, 0f, 0.42f);
        public static readonly Color PanelForecast = new Color(0.08f, 0.09f, 0.11f, 0.88f);

        public static readonly Color AccentGold = new Color(0.77f, 0.6f, 0.29f, 1f);
        public static readonly Color AccentBlue = new Color(0.26f, 0.57f, 0.74f, 1f);
        public static readonly Color AccentRed = new Color(0.66f, 0.29f, 0.22f, 1f);
        public static readonly Color AccentGreen = new Color(0.36f, 0.57f, 0.41f, 1f);
        public static readonly Color AccentTeal = new Color(0.29f, 0.62f, 0.57f, 1f);

        public static readonly Color TextPrimary = new Color(0.93f, 0.91f, 0.87f, 1f);
        public static readonly Color TextSecondary = new Color(0.82f, 0.83f, 0.8f, 1f);
        public static readonly Color TextMuted = new Color(0.66f, 0.69f, 0.71f, 1f);
        public static readonly Color TextGold = new Color(0.85f, 0.72f, 0.45f, 1f);
        public static readonly Color TextWarning = new Color(0.88f, 0.63f, 0.56f, 1f);
        public static readonly Color TextThreat = new Color(0.9f, 0.7f, 0.56f, 1f);
        public static readonly Color TextDisabled = new Color(0.48f, 0.49f, 0.53f, 1f);

        public static readonly Color OutlineStrong = new Color(0.36f, 0.27f, 0.16f, 0.78f);
        public static readonly Color OutlineSoft = new Color(0.28f, 0.21f, 0.14f, 0.54f);

        public static readonly Color ButtonPrimary = new Color(0.72f, 0.53f, 0.24f, 1f);
        public static readonly Color ButtonPrimaryHighlight = new Color(0.82f, 0.62f, 0.3f, 1f);
        public static readonly Color ButtonPrimaryPressed = new Color(0.58f, 0.41f, 0.18f, 1f);
        public static readonly Color ButtonDisabled = new Color(0.23f, 0.23f, 0.26f, 0.92f);
        public static readonly Color ButtonText = new Color(0.13f, 0.1f, 0.08f, 1f);

        public static readonly Color GridWalkableLight = new Color(0.67f, 0.59f, 0.45f, 1f);
        public static readonly Color GridWalkableDark = new Color(0.59f, 0.5f, 0.38f, 1f);
        public static readonly Color GridBlockedLight = new Color(0.25f, 0.29f, 0.27f, 1f);
        public static readonly Color GridBlockedDark = new Color(0.18f, 0.21f, 0.2f, 1f);
        public static readonly Color GridBackdrop = new Color(0.13f, 0.11f, 0.1f, 1f);
        public static readonly Color GridFrame = new Color(0.77f, 0.6f, 0.29f, 1f);
        public static readonly Color MoveHighlight = new Color(0.32f, 0.7f, 0.84f, 0.98f);
        public static readonly Color AttackHighlight = new Color(0.79f, 0.35f, 0.27f, 0.98f);
        public static readonly Color SkillHighlight = new Color(0.42f, 0.74f, 0.48f, 0.96f);
        public static readonly Color SelectedHighlight = new Color(0.88f, 0.72f, 0.34f, 1f);
        public static readonly Color MoveTileTint = new Color(0.63f, 0.84f, 0.9f, 1f);
        public static readonly Color AttackTileTint = new Color(0.9f, 0.69f, 0.58f, 1f);
        public static readonly Color SkillTileTint = new Color(0.67f, 0.84f, 0.7f, 1f);
        public static readonly Color SelectedTileTint = new Color(0.92f, 0.82f, 0.53f, 1f);
        public static readonly Color GridFrameWalkable = new Color(0.11f, 0.09f, 0.07f, 0.36f);
        public static readonly Color GridFrameBlocked = new Color(0.06f, 0.06f, 0.07f, 0.76f);
        public static readonly Color GridFrameForest = new Color(0.16f, 0.24f, 0.15f, 0.66f);
        public static readonly Color GridFrameFort = new Color(0.4f, 0.35f, 0.28f, 0.7f);
        public static readonly Color GridFrameHazard = new Color(0.44f, 0.21f, 0.14f, 0.74f);
        public static readonly Color MoveFrameHighlight = new Color(0.43f, 0.74f, 0.85f, 0.94f);
        public static readonly Color AttackFrameHighlight = new Color(0.87f, 0.47f, 0.36f, 0.96f);
        public static readonly Color SkillFrameHighlight = new Color(0.48f, 0.78f, 0.56f, 0.94f);
        public static readonly Color SelectedFrameHighlight = new Color(0.92f, 0.77f, 0.36f, 0.98f);

        public static Color GetFactionPlateColor(UnitFaction faction, bool exhausted)
        {
            if (faction == UnitFaction.Player)
            {
                return exhausted ? new Color(0.16f, 0.17f, 0.21f, 0.95f) : new Color(0.16f, 0.24f, 0.33f, 0.95f);
            }

            return exhausted ? new Color(0.25f, 0.17f, 0.16f, 0.95f) : new Color(0.39f, 0.16f, 0.13f, 0.95f);
        }

        public static Color GetFactionRingColor(UnitFaction faction, bool exhausted)
        {
            if (faction == UnitFaction.Player)
            {
                return exhausted ? new Color(0.24f, 0.3f, 0.42f, 0.34f) : new Color(0.24f, 0.49f, 0.74f, 0.42f);
            }

            return exhausted ? new Color(0.41f, 0.24f, 0.21f, 0.34f) : new Color(0.76f, 0.31f, 0.22f, 0.42f);
        }

        public static Color GetGridTileColor(bool blocked, bool alternate)
        {
            if (blocked)
            {
                return alternate ? GridBlockedLight : GridBlockedDark;
            }

            return alternate ? GridWalkableLight : GridWalkableDark;
        }

        public static Color GetGridFrameColor(TerrainType terrainType, bool blocked)
        {
            if (blocked)
            {
                return GridFrameBlocked;
            }

            switch (terrainType)
            {
                case TerrainType.Forest:
                    return GridFrameForest;
                case TerrainType.Fort:
                    return GridFrameFort;
                case TerrainType.Hazard:
                    return GridFrameHazard;
                default:
                    return GridFrameWalkable;
            }
        }

    }
}
