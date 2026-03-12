using System.Collections.Generic;
using PhalanxChronicle.Core;
using PhalanxChronicle.Data;
using UnityEngine;

namespace PhalanxChronicle.Presentation
{
    public enum UnitVisualArchetype
    {
        Default = 0,
        LiuBei = 1,
        GuanYu = 2,
        ZhangFei = 3,
        HuangZhong = 4,
        YellowTurban = 5,
        YellowTurbanBoss = 6,
        WeiCommander = 7,
        WeiGuardian = 8,
        WeiRanger = 9,
        WeiRaider = 10,
        BossCommander = 11,
    }

    public enum UnitFrameStyle
    {
        Common = 0,
        Hero = 1,
        Boss = 2,
    }

    public sealed class UnitVisualProfile
    {
        public UnitVisualProfile(
            string unitId,
            UnitFaction faction,
            UnitRole role,
            bool isHero,
            UnitVisualArchetype archetype,
            UnitFrameStyle frameStyle,
            Color primaryColor,
            Color secondaryColor,
            Color accentColor,
            Color frameColor,
            Color markerColor,
            Color portraitBackdropColor,
            float battleScale,
            Sprite portraitSprite = null,
            Sprite battleSprite = null,
            Sprite weaponIcon = null,
            Sprite factionMarker = null,
            Sprite selectionFrame = null,
            RuntimeAnimatorController idleAnimationController = null)
        {
            UnitId = unitId;
            Faction = faction;
            Role = role;
            IsHero = isHero;
            Archetype = archetype;
            FrameStyle = frameStyle;
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
            AccentColor = accentColor;
            FrameColor = frameColor;
            MarkerColor = markerColor;
            PortraitBackdropColor = portraitBackdropColor;
            BattleScale = Mathf.Max(0.7f, battleScale);
            PortraitSprite = portraitSprite;
            BattleSprite = battleSprite;
            WeaponIcon = weaponIcon;
            FactionMarker = factionMarker;
            SelectionFrame = selectionFrame;
            IdleAnimationController = idleAnimationController;
        }

        public string UnitId { get; }

        public UnitFaction Faction { get; }

        public UnitRole Role { get; }

        public bool IsHero { get; }

        public UnitVisualArchetype Archetype { get; }

        public UnitFrameStyle FrameStyle { get; }

        public Color PrimaryColor { get; }

        public Color SecondaryColor { get; }

        public Color AccentColor { get; }

        public Color FrameColor { get; }

        public Color MarkerColor { get; }

        public Color PortraitBackdropColor { get; }

        public float BattleScale { get; }

        public Sprite PortraitSprite { get; }

        public Sprite BattleSprite { get; }

        public Sprite WeaponIcon { get; }

        public Sprite FactionMarker { get; }

        public Sprite SelectionFrame { get; }

        public RuntimeAnimatorController IdleAnimationController { get; }
    }

    public static class UnitVisualCatalog
    {
        private const string ResourcePath = "UnitVisualDefinitions";

        private static readonly Dictionary<string, UnitVisualDefinition> definitionsById = new Dictionary<string, UnitVisualDefinition>();
        private static readonly Dictionary<string, UnitVisualProfile> fallbackProfiles = new Dictionary<string, UnitVisualProfile>();
        private static bool definitionsLoaded;

        public static UnitVisualProfile GetProfile(string unitId, UnitFaction faction, UnitRole role)
        {
            EnsureDefinitionsLoaded();
            if (!string.IsNullOrWhiteSpace(unitId) && definitionsById.TryGetValue(unitId, out UnitVisualDefinition definition))
            {
                return CreateProfileFromDefinition(unitId, faction, role, definition);
            }

            string fallbackKey = $"{unitId}:{faction}:{role}";
            if (!fallbackProfiles.TryGetValue(fallbackKey, out UnitVisualProfile profile))
            {
                profile = BuildFallbackProfile(unitId, faction, role);
                fallbackProfiles[fallbackKey] = profile;
            }

            return profile;
        }

        private static void EnsureDefinitionsLoaded()
        {
            if (definitionsLoaded)
            {
                return;
            }

            definitionsLoaded = true;
            definitionsById.Clear();
            UnitVisualDefinition[] definitions = Resources.LoadAll<UnitVisualDefinition>(ResourcePath);
            foreach (UnitVisualDefinition definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.UnitId) || definitionsById.ContainsKey(definition.UnitId))
                {
                    continue;
                }

                definitionsById.Add(definition.UnitId, definition);
            }
        }

        private static UnitVisualProfile CreateProfileFromDefinition(string unitId, UnitFaction faction, UnitRole role, UnitVisualDefinition definition)
        {
            UnitVisualProfile fallback = BuildFallbackProfile(unitId, faction, role);
            bool useCustomPalette = definition.UseCustomPalette;
            UnitVisualArchetype archetype = definition.Archetype == UnitVisualArchetype.Default
                ? fallback.Archetype
                : definition.Archetype;
            UnitFrameStyle frameStyle = definition.FrameStyle == UnitFrameStyle.Common && fallback.FrameStyle != UnitFrameStyle.Common
                ? fallback.FrameStyle
                : definition.FrameStyle;
            float requestedBattleScale = definition.BattleScale > 0f ? definition.BattleScale : fallback.BattleScale;
            float normalizedBattleScale = NormalizeBattleScale(role, frameStyle, requestedBattleScale);

            return new UnitVisualProfile(
                unitId,
                faction,
                role,
                definition.HeroProfile || fallback.IsHero,
                archetype,
                frameStyle,
                useCustomPalette ? definition.PrimaryColor : fallback.PrimaryColor,
                useCustomPalette ? definition.SecondaryColor : fallback.SecondaryColor,
                useCustomPalette ? definition.AccentColor : fallback.AccentColor,
                useCustomPalette ? definition.FrameColor : fallback.FrameColor,
                useCustomPalette ? definition.MarkerColor : fallback.MarkerColor,
                useCustomPalette ? definition.PortraitBackdropColor : fallback.PortraitBackdropColor,
                normalizedBattleScale,
                definition.PortraitSprite != null ? definition.PortraitSprite : fallback.PortraitSprite,
                definition.BattleSprite != null ? definition.BattleSprite : fallback.BattleSprite,
                definition.WeaponIcon != null ? definition.WeaponIcon : fallback.WeaponIcon,
                definition.FactionMarker != null ? definition.FactionMarker : fallback.FactionMarker,
                definition.SelectionFrame != null ? definition.SelectionFrame : fallback.SelectionFrame,
                definition.IdleAnimationController != null ? definition.IdleAnimationController : fallback.IdleAnimationController);
        }

        private static UnitVisualProfile BuildFallbackProfile(string unitId, UnitFaction faction, UnitRole role)
        {
            string normalizedId = (unitId ?? string.Empty).ToLowerInvariant();
            if (normalizedId == "player-liu-bei")
            {
                return CreateProfile(unitId, faction, role, true, UnitVisualArchetype.LiuBei, UnitFrameStyle.Hero, Hex("4C6B3A"), Hex("D3C3A3"), Hex("E3BE63"), Hex("CFA34A"), Hex("5D8A5C"), Hex("2A3125"), 0.98f);
            }

            if (normalizedId == "player-guan-yu")
            {
                return CreateProfile(unitId, faction, role, true, UnitVisualArchetype.GuanYu, UnitFrameStyle.Hero, Hex("2E5A43"), Hex("15241C"), Hex("D6B564"), Hex("D2AC57"), Hex("3E9158"), Hex("1A231D"), 1f);
            }

            if (normalizedId == "player-zhang-fei")
            {
                return CreateProfile(unitId, faction, role, true, UnitVisualArchetype.ZhangFei, UnitFrameStyle.Hero, Hex("37374B"), Hex("1A1821"), Hex("BB5B45"), Hex("D59659"), Hex("8A4238"), Hex("211B20"), 1.03f);
            }

            if (normalizedId == "player-huang-zhong")
            {
                return CreateProfile(unitId, faction, role, true, UnitVisualArchetype.HuangZhong, UnitFrameStyle.Hero, Hex("7A6533"), Hex("304D3F"), Hex("D8BF72"), Hex("C8A55A"), Hex("7F8E70"), Hex("2B2B25"), 0.97f);
            }

            if (normalizedId == "player-zhuge-liang")
            {
                return CreateProfile(unitId, faction, role, true, UnitVisualArchetype.LiuBei, UnitFrameStyle.Hero, Hex("5A6E62"), Hex("DEE2D3"), Hex("BFCB88"), Hex("C0A768"), Hex("89A68C"), Hex("243028"), 0.96f);
            }

            if (normalizedId == "player-zhao-yun")
            {
                return CreateProfile(unitId, faction, role, true, UnitVisualArchetype.WeiRaider, UnitFrameStyle.Hero, Hex("E4E2D7"), Hex("4D6478"), Hex("CDBB77"), Hex("C3A166"), Hex("8DB6C8"), Hex("24282D"), 0.99f);
            }

            if (normalizedId == "player-ma-chao")
            {
                return CreateProfile(unitId, faction, role, true, UnitVisualArchetype.WeiRaider, UnitFrameStyle.Hero, Hex("D8D7CF"), Hex("365266"), Hex("D8BC76"), Hex("C79C5A"), Hex("76A6C0"), Hex("21252A"), 1.01f);
            }

            if (normalizedId.Contains("yellow_turban") || normalizedId.Contains("zhang-bao") || normalizedId.Contains("zhang-liang"))
            {
                bool boss = normalizedId.Contains("zhang-bao") || normalizedId.Contains("zhang-liang");
                return CreateProfile(
                    unitId,
                    faction,
                    role,
                    boss,
                    boss ? UnitVisualArchetype.YellowTurbanBoss : UnitVisualArchetype.YellowTurban,
                    boss ? UnitFrameStyle.Boss : UnitFrameStyle.Common,
                    Hex("A6812D"),
                    Hex("5B3A1F"),
                    Hex("E0BF61"),
                    boss ? Hex("D8A54B") : Hex("B98438"),
                    Hex("BC9B45"),
                    Hex("372611"),
                    boss ? 1.02f : 0.95f);
            }

            if (normalizedId.Contains("xiahou-yuan") || normalizedId.Contains("pursuit_commander"))
            {
                return CreateProfile(unitId, faction, role, true, UnitVisualArchetype.BossCommander, UnitFrameStyle.Boss, Hex("4A5E84"), Hex("1F2A39"), Hex("C87F4A"), Hex("D1A169"), Hex("6D8CA5"), Hex("1C202A"), 1f);
            }

            if (normalizedId.Contains("wei") || normalizedId.Contains("tiger_guard") || normalizedId.Contains("tiger_leopard"))
            {
                UnitVisualArchetype archetype = role == UnitRole.Commander
                    ? UnitVisualArchetype.WeiCommander
                    : role == UnitRole.Guardian
                        ? UnitVisualArchetype.WeiGuardian
                        : role == UnitRole.Ranger
                            ? UnitVisualArchetype.WeiRanger
                            : UnitVisualArchetype.WeiRaider;
                return CreateProfile(unitId, faction, role, false, archetype, UnitFrameStyle.Common, Hex("51627B"), Hex("2B3441"), Hex("A7B3C3"), Hex("8F9AAF"), Hex("6285A8"), Hex("222832"), 0.95f);
            }

            if (role == UnitRole.Commander && faction == UnitFaction.Enemy)
            {
                return CreateProfile(unitId, faction, role, true, UnitVisualArchetype.BossCommander, UnitFrameStyle.Boss, Hex("7A4336"), Hex("321F19"), Hex("D09156"), Hex("D3A261"), Hex("994D37"), Hex("2A1915"), 0.98f);
            }

            return faction == UnitFaction.Player
                ? CreatePlayerFallback(unitId, role)
                : CreateEnemyFallback(unitId, role);
        }

        private static UnitVisualProfile CreatePlayerFallback(string unitId, UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Commander:
                    return CreateProfile(unitId, UnitFaction.Player, role, false, UnitVisualArchetype.LiuBei, UnitFrameStyle.Common, Hex("4E6D52"), Hex("28362A"), Hex("D8C070"), Hex("C4A35B"), Hex("6E9766"), Hex("252D24"), 0.96f);
                case UnitRole.Guardian:
                    return CreateProfile(unitId, UnitFaction.Player, role, false, UnitVisualArchetype.WeiGuardian, UnitFrameStyle.Common, Hex("45637A"), Hex("253645"), Hex("C6B08A"), Hex("A99975"), Hex("5A93B8"), Hex("1F2731"), 0.98f);
                case UnitRole.Ranger:
                    return CreateProfile(unitId, UnitFaction.Player, role, false, UnitVisualArchetype.HuangZhong, UnitFrameStyle.Common, Hex("6E6A3D"), Hex("2B4737"), Hex("D3BA74"), Hex("BFA15D"), Hex("6E9F7F"), Hex("252A22"), 0.95f);
                default:
                    return CreateProfile(unitId, UnitFaction.Player, role, false, UnitVisualArchetype.WeiRaider, UnitFrameStyle.Common, Hex("58636F"), Hex("2E343B"), Hex("BDA77B"), Hex("A18961"), Hex("6798A8"), Hex("23272C"), 0.94f);
            }
        }

        private static UnitVisualProfile CreateEnemyFallback(string unitId, UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Guardian:
                    return CreateProfile(unitId, UnitFaction.Enemy, role, false, UnitVisualArchetype.WeiGuardian, UnitFrameStyle.Common, Hex("6E4B41"), Hex("3D2520"), Hex("C7936A"), Hex("B57D55"), Hex("A9533F"), Hex("291917"), 0.97f);
                case UnitRole.Ranger:
                    return CreateProfile(unitId, UnitFaction.Enemy, role, false, UnitVisualArchetype.WeiRanger, UnitFrameStyle.Common, Hex("7B5A42"), Hex("402A20"), Hex("D8A567"), Hex("BF874D"), Hex("C36245"), Hex("2D1B16"), 0.95f);
                default:
                    return CreateProfile(unitId, UnitFaction.Enemy, role, false, UnitVisualArchetype.WeiRaider, UnitFrameStyle.Common, Hex("7A4B3B"), Hex("381E18"), Hex("D1845D"), Hex("BD754D"), Hex("BF5F46"), Hex("2C1714"), 0.95f);
            }
        }

        private static UnitVisualProfile CreateProfile(
            string unitId,
            UnitFaction faction,
            UnitRole role,
            bool isHero,
            UnitVisualArchetype archetype,
            UnitFrameStyle frameStyle,
            Color primaryColor,
            Color secondaryColor,
            Color accentColor,
            Color frameColor,
            Color markerColor,
            Color portraitBackdropColor,
            float battleScale)
        {
            return new UnitVisualProfile(
                unitId,
                faction,
                role,
                isHero,
                archetype,
                frameStyle,
                primaryColor,
                secondaryColor,
                accentColor,
                frameColor,
                markerColor,
                portraitBackdropColor,
                NormalizeBattleScale(role, frameStyle, battleScale));
        }

        private static float NormalizeBattleScale(UnitRole role, UnitFrameStyle frameStyle, float requestedBattleScale)
        {
            float baseline = GetBaselineBattleScale(role, frameStyle);
            float minimum = Mathf.Max(0.72f, baseline - 0.04f);
            float maximum = Mathf.Min(0.96f, baseline + 0.04f);
            if (requestedBattleScale <= 0f)
            {
                return baseline;
            }

            return Mathf.Clamp(requestedBattleScale, minimum, maximum);
        }

        private static float GetBaselineBattleScale(UnitRole role, UnitFrameStyle frameStyle)
        {
            if (frameStyle == UnitFrameStyle.Boss)
            {
                return 0.92f;
            }

            switch (role)
            {
                case UnitRole.Commander:
                    return 0.84f;
                case UnitRole.Guardian:
                    return 0.88f;
                case UnitRole.Ranger:
                case UnitRole.Scout:
                case UnitRole.Raider:
                default:
                    return 0.8f;
            }
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString("#" + value, out Color color) ? color : Color.white;
        }
    }
}
