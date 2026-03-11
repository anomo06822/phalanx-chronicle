using System;
using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    [Serializable]
    public sealed class EquipmentLoadout
    {
        public EquipmentLoadout(string weaponId, string armorId)
        {
            WeaponId = weaponId ?? string.Empty;
            ArmorId = armorId ?? string.Empty;
        }

        public static EquipmentLoadout Empty { get; } = new EquipmentLoadout(string.Empty, string.Empty);

        public string WeaponId { get; }

        public string ArmorId { get; }
    }

    [Serializable]
    public sealed class BondState
    {
        public BondState(int supportLevel = 0, int sharedBattles = 0)
        {
            SupportLevel = supportLevel < 0 ? 0 : supportLevel;
            SharedBattles = sharedBattles < 0 ? 0 : sharedBattles;
        }

        public int SupportLevel { get; private set; }

        public int SharedBattles { get; private set; }

        public void RegisterBattle()
        {
            SharedBattles++;
            if (SharedBattles % 3 == 0)
            {
                SupportLevel++;
            }
        }
    }

    [Serializable]
    public sealed class RewardBundle
    {
        public RewardBundle(int supplies, int renown, string rewardItemId = "", IReadOnlyList<string> recruitUnitIds = null)
        {
            Supplies = supplies < 0 ? 0 : supplies;
            Renown = renown < 0 ? 0 : renown;
            RewardItemId = rewardItemId ?? string.Empty;
            RecruitUnitIds = recruitUnitIds ?? Array.Empty<string>();
        }

        public int Supplies { get; }

        public int Renown { get; }

        public string RewardItemId { get; }

        public IReadOnlyList<string> RecruitUnitIds { get; }

        public bool HasAnyReward =>
            Supplies > 0 ||
            Renown > 0 ||
            !string.IsNullOrWhiteSpace(RewardItemId) ||
            RecruitUnitIds.Count > 0;
    }

    public sealed class ExpRewardRule
    {
        public ExpRewardRule(int minimum, int maximum, float scale)
        {
            Minimum = minimum;
            Maximum = maximum;
            Scale = scale;
        }

        public int Minimum { get; }

        public int Maximum { get; }

        public float Scale { get; }
    }

    public sealed class UnitClassDefinition
    {
        public UnitClassDefinition(
            string classId,
            string displayNameKey,
            AiProfileType defaultAiProfile,
            int supportAttackBonus,
            int supportDefenseBonus,
            int supportManaRecovery)
        {
            ClassId = classId;
            DisplayNameKey = displayNameKey;
            DefaultAiProfile = defaultAiProfile;
            SupportAttackBonus = supportAttackBonus;
            SupportDefenseBonus = supportDefenseBonus;
            SupportManaRecovery = supportManaRecovery;
        }

        public string ClassId { get; }

        public string DisplayNameKey { get; }

        public AiProfileType DefaultAiProfile { get; }

        public int SupportAttackBonus { get; }

        public int SupportDefenseBonus { get; }

        public int SupportManaRecovery { get; }
    }

    public sealed class GrowthProfileDefinition
    {
        public GrowthProfileDefinition(
            string id,
            int hpPerLevel,
            int attackEveryLevels,
            int defenseEveryLevels,
            int manaEveryLevels,
            int advancedSkillUnlockLevel = 3,
            int traitSlotUnlockLevel = 5,
            int promotionUnlockLevel = 10,
            int signaturePassiveUnlockLevel = 15)
        {
            Id = id;
            HpPerLevel = hpPerLevel;
            AttackEveryLevels = attackEveryLevels;
            DefenseEveryLevels = defenseEveryLevels;
            ManaEveryLevels = manaEveryLevels;
            AdvancedSkillUnlockLevel = advancedSkillUnlockLevel;
            TraitSlotUnlockLevel = traitSlotUnlockLevel;
            PromotionUnlockLevel = promotionUnlockLevel;
            SignaturePassiveUnlockLevel = signaturePassiveUnlockLevel;
        }

        public string Id { get; }

        public int HpPerLevel { get; }

        public int AttackEveryLevels { get; }

        public int DefenseEveryLevels { get; }

        public int ManaEveryLevels { get; }

        public int AdvancedSkillUnlockLevel { get; }

        public int TraitSlotUnlockLevel { get; }

        public int PromotionUnlockLevel { get; }

        public int SignaturePassiveUnlockLevel { get; }

        public int GetAttackGain(int level)
        {
            return AttackEveryLevels > 0 && level % AttackEveryLevels == 0 ? 1 : 0;
        }

        public int GetDefenseGain(int level)
        {
            return DefenseEveryLevels > 0 && level % DefenseEveryLevels == 0 ? 1 : 0;
        }

        public int GetManaGain(int level)
        {
            return ManaEveryLevels > 0 && level % ManaEveryLevels == 0 ? 1 : 0;
        }

        public int GetTraitSlotCount(int level)
        {
            if (level >= SignaturePassiveUnlockLevel)
            {
                return 2;
            }

            return level >= TraitSlotUnlockLevel ? 1 : 0;
        }
    }

    public static class UnitClassCatalog
    {
        private static readonly IReadOnlyDictionary<string, UnitClassDefinition> Classes =
            new Dictionary<string, UnitClassDefinition>
            {
                ["commander"] = new UnitClassDefinition("commander", "role.commander", AiProfileType.Protector, 1, 1, 1),
                ["guardian"] = new UnitClassDefinition("guardian", "role.guardian", AiProfileType.Protector, 0, 2, 0),
                ["ranger"] = new UnitClassDefinition("ranger", "role.ranger", AiProfileType.Aggressor, 1, 0, 0),
                ["scout"] = new UnitClassDefinition("scout", "role.scout", AiProfileType.Aggressor, 1, 0, 0),
                ["raider"] = new UnitClassDefinition("raider", "role.raider", AiProfileType.Aggressor, 1, 0, 0),
                ["lord"] = new UnitClassDefinition("lord", "class.lord", AiProfileType.Protector, 2, 1, 2),
                ["warlord"] = new UnitClassDefinition("warlord", "class.warlord", AiProfileType.Protector, 2, 1, 1),
                ["sleeping_dragon"] = new UnitClassDefinition("sleeping_dragon", "class.sleeping_dragon", AiProfileType.Support, 1, 1, 3),
                ["tactician_general"] = new UnitClassDefinition("tactician_general", "class.tactician_general", AiProfileType.Support, 1, 2, 2),
                ["saint_blade"] = new UnitClassDefinition("saint_blade", "class.saint_blade", AiProfileType.Protector, 1, 2, 0),
                ["halberdier_general"] = new UnitClassDefinition("halberdier_general", "class.halberdier_general", AiProfileType.Aggressor, 2, 1, 0),
                ["vanguard_general"] = new UnitClassDefinition("vanguard_general", "class.vanguard_general", AiProfileType.Aggressor, 2, 1, 0),
                ["fortress_general"] = new UnitClassDefinition("fortress_general", "class.fortress_general", AiProfileType.Protector, 0, 3, 0),
                ["master_bow"] = new UnitClassDefinition("master_bow", "class.master_bow", AiProfileType.Support, 2, 0, 1),
                ["pinning_bow"] = new UnitClassDefinition("pinning_bow", "class.pinning_bow", AiProfileType.Support, 1, 0, 2),
                ["white_horse_general"] = new UnitClassDefinition("white_horse_general", "class.white_horse_general", AiProfileType.Aggressor, 2, 0, 1),
                ["dragon_lancer"] = new UnitClassDefinition("dragon_lancer", "class.dragon_lancer", AiProfileType.Aggressor, 1, 1, 0),
                ["storm_raider"] = new UnitClassDefinition("storm_raider", "class.storm_raider", AiProfileType.Aggressor, 2, 0, 0),
                ["western_lancer"] = new UnitClassDefinition("western_lancer", "class.western_lancer", AiProfileType.Aggressor, 1, 1, 0),
                ["unit"] = new UnitClassDefinition("unit", "ui.role.short.unknown", AiProfileType.Standard, 0, 0, 0),
            };

        public static string GetDefaultClassId(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Commander:
                    return "commander";
                case UnitRole.Guardian:
                    return "guardian";
                case UnitRole.Ranger:
                    return "ranger";
                case UnitRole.Scout:
                    return "scout";
                case UnitRole.Raider:
                    return "raider";
                default:
                    return "unit";
            }
        }

        public static UnitClassDefinition Get(string classId)
        {
            if (!string.IsNullOrWhiteSpace(classId) && Classes.TryGetValue(classId, out UnitClassDefinition definition))
            {
                return definition;
            }

            return Classes["unit"];
        }
    }

    public static class GrowthProfileCatalog
    {
        private static readonly IReadOnlyDictionary<string, GrowthProfileDefinition> Profiles =
            new Dictionary<string, GrowthProfileDefinition>
            {
                ["commander"] = new GrowthProfileDefinition("commander", 2, 2, 3, 2),
                ["guardian"] = new GrowthProfileDefinition("guardian", 3, 3, 2, 3),
                ["ranger"] = new GrowthProfileDefinition("ranger", 2, 2, 4, 2),
                ["scout"] = new GrowthProfileDefinition("scout", 2, 2, 4, 2),
                ["raider"] = new GrowthProfileDefinition("raider", 2, 2, 3, 3),
                ["lord"] = new GrowthProfileDefinition("lord", 3, 2, 2, 2, promotionUnlockLevel: 99),
                ["warlord"] = new GrowthProfileDefinition("warlord", 4, 2, 3, 3, promotionUnlockLevel: 99),
                ["sleeping_dragon"] = new GrowthProfileDefinition("sleeping_dragon", 2, 3, 3, 2, promotionUnlockLevel: 99),
                ["tactician_general"] = new GrowthProfileDefinition("tactician_general", 3, 3, 2, 2, promotionUnlockLevel: 99),
                ["saint_blade"] = new GrowthProfileDefinition("saint_blade", 4, 2, 2, 3, promotionUnlockLevel: 99),
                ["halberdier_general"] = new GrowthProfileDefinition("halberdier_general", 4, 2, 3, 3, promotionUnlockLevel: 99),
                ["vanguard_general"] = new GrowthProfileDefinition("vanguard_general", 4, 2, 2, 4, promotionUnlockLevel: 99),
                ["fortress_general"] = new GrowthProfileDefinition("fortress_general", 5, 3, 2, 4, promotionUnlockLevel: 99),
                ["master_bow"] = new GrowthProfileDefinition("master_bow", 3, 2, 3, 2, promotionUnlockLevel: 99),
                ["pinning_bow"] = new GrowthProfileDefinition("pinning_bow", 3, 2, 4, 2, promotionUnlockLevel: 99),
                ["white_horse_general"] = new GrowthProfileDefinition("white_horse_general", 3, 2, 3, 2, promotionUnlockLevel: 99),
                ["dragon_lancer"] = new GrowthProfileDefinition("dragon_lancer", 4, 2, 3, 3, promotionUnlockLevel: 99),
                ["storm_raider"] = new GrowthProfileDefinition("storm_raider", 4, 2, 3, 3, promotionUnlockLevel: 99),
                ["western_lancer"] = new GrowthProfileDefinition("western_lancer", 4, 2, 3, 3, promotionUnlockLevel: 99),
                ["unit"] = new GrowthProfileDefinition("unit", 2, 3, 3, 3),
            };

        public static GrowthProfileDefinition Get(string growthProfileId)
        {
            if (!string.IsNullOrWhiteSpace(growthProfileId) && Profiles.TryGetValue(growthProfileId, out GrowthProfileDefinition definition))
            {
                return definition;
            }

            return Profiles["unit"];
        }
    }
}
