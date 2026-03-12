using PhalanxChronicle.Core;
using UnityEngine;

namespace PhalanxChronicle.Presentation
{
    public sealed class RoleLoadoutProfile
    {
        public RoleLoadoutProfile(
            string weaponTypeKey,
            string weaponTypeFallback,
            string weaponNameKey,
            string weaponNameFallback,
            string weaponDescriptionKey,
            string weaponDescriptionFallback,
            Color accentColor)
        {
            WeaponTypeKey = weaponTypeKey;
            WeaponTypeFallback = weaponTypeFallback;
            WeaponNameKey = weaponNameKey;
            WeaponNameFallback = weaponNameFallback;
            WeaponDescriptionKey = weaponDescriptionKey;
            WeaponDescriptionFallback = weaponDescriptionFallback;
            AccentColor = accentColor;
        }

        public string WeaponTypeKey { get; }

        public string WeaponTypeFallback { get; }

        public string WeaponNameKey { get; }

        public string WeaponNameFallback { get; }

        public string WeaponDescriptionKey { get; }

        public string WeaponDescriptionFallback { get; }

        public Color AccentColor { get; }
    }

    public static class RoleLoadoutCatalog
    {
        public static RoleLoadoutProfile GetProfile(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Commander:
                    return new RoleLoadoutProfile(
                        "weapon.commander.type",
                        "Command Sidearm",
                        "weapon.commander.name",
                        "Vermilion Jian",
                        "weapon.commander.desc",
                        "A straight sword paired with a command pennant, used to direct the line and guard nearby allies.",
                        new Color(0.94f, 0.78f, 0.33f, 1f));
                case UnitRole.Guardian:
                    return new RoleLoadoutProfile(
                        "weapon.guardian.type",
                        "Heavy Polearm",
                        "weapon.guardian.name",
                        "Iron Crescent Glaive",
                        "weapon.guardian.desc",
                        "A long glaive built for crushing armor and holding chokepoints through sheer reach and weight.",
                        new Color(0.58f, 0.74f, 0.9f, 1f));
                case UnitRole.Ranger:
                    return new RoleLoadoutProfile(
                        "weapon.ranger.type",
                        "Long Range Bow",
                        "weapon.ranger.name",
                        "Featherback War Bow",
                        "weapon.ranger.desc",
                        "A laminated bow tuned for steady volleys, allowing precision fire and area pressure from the rear line.",
                        new Color(0.33f, 0.84f, 0.66f, 1f));
                case UnitRole.Scout:
                    return new RoleLoadoutProfile(
                        "weapon.scout.type",
                        "Dual Blades",
                        "weapon.scout.name",
                        "Shadow Pair Daggers",
                        "weapon.scout.desc",
                        "Twin short blades meant for rapid feints, flanking cuts, and fast pursuit through open lanes.",
                        new Color(0.61f, 0.88f, 0.37f, 1f));
                case UnitRole.Raider:
                    return new RoleLoadoutProfile(
                        "weapon.raider.type",
                        "Cavalry Lance",
                        "weapon.raider.name",
                        "Stormpiercer Lance",
                        "weapon.raider.desc",
                        "A forward-set lance designed for charge momentum, striking deep before the enemy line can reset.",
                        new Color(0.93f, 0.47f, 0.35f, 1f));
                default:
                    return new RoleLoadoutProfile(
                        "weapon.unknown.type",
                        "Standard Issue",
                        "weapon.unknown.name",
                        "Field Steel",
                        "weapon.unknown.desc",
                        "A general battlefield weapon with no special role association.",
                        new Color(0.78f, 0.78f, 0.78f, 1f));
            }
        }

        public static Color GetSkillAccent(ActiveSkillType skillType)
        {
            switch (skillType)
            {
                case ActiveSkillType.RoyalAid:
                    return new Color(0.99f, 0.84f, 0.43f, 1f);
                case ActiveSkillType.PowerStrike:
                    return new Color(1f, 0.54f, 0.3f, 1f);
                case ActiveSkillType.DragonPierce:
                    return new Color(0.5f, 0.92f, 0.78f, 1f);
                case ActiveSkillType.Volley:
                    return new Color(0.39f, 0.84f, 0.78f, 1f);
                case ActiveSkillType.GreenDragonSlash:
                    return new Color(0.44f, 0.94f, 0.5f, 1f);
                case ActiveSkillType.WarCry:
                    return new Color(0.97f, 0.44f, 0.34f, 1f);
                case ActiveSkillType.ImperialAid:
                    return new Color(0.42f, 0.9f, 0.62f, 1f);
                case ActiveSkillType.GuardOrder:
                    return new Color(0.46f, 0.82f, 0.72f, 1f);
                case ActiveSkillType.AzureDragonSlash:
                    return new Color(0.58f, 0.96f, 0.52f, 1f);
                case ActiveSkillType.LionWarCry:
                    return new Color(1f, 0.52f, 0.36f, 1f);
                case ActiveSkillType.SkyVolley:
                    return new Color(0.42f, 0.86f, 0.94f, 1f);
                case ActiveSkillType.PinningShot:
                    return new Color(0.5f, 0.88f, 1f, 1f);
                case ActiveSkillType.WesternStampede:
                    return new Color(1f, 0.62f, 0.34f, 1f);
                case ActiveSkillType.FireStratagem:
                    return new Color(1f, 0.54f, 0.28f, 1f);
                case ActiveSkillType.EightTrigramInferno:
                    return new Color(1f, 0.42f, 0.24f, 1f);
                default:
                    return BattleUiTheme.AccentGold;
            }
        }
    }
}
