using System;
using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class CampaignRosterBuilder
    {
        private static readonly IReadOnlyList<string> StartingUnitIds = new[]
        {
            "player-liu-bei",
            "player-guan-yu",
            "player-zhang-fei",
            "player-huang-zhong",
        };

        private static readonly IReadOnlyDictionary<string, CampaignUnitTemplate> Templates =
            new Dictionary<string, CampaignUnitTemplate>(StringComparer.Ordinal)
            {
                ["player-liu-bei"] = new CampaignUnitTemplate(
                    "player-liu-bei",
                    "Liu Bei",
                    "unit.player_liu_bei",
                    UnitRole.Commander,
                    "role.commander",
                    PassiveSkillType.CommandAura,
                    "skill.command_aura.name",
                    "skill.command_aura.desc",
                    ActiveSkillType.RoyalAid,
                    "skill.royal_aid.name",
                    "skill.royal_aid.desc",
                    30,
                    9,
                    5,
                    3,
                    1,
                    20,
                    "commander",
                    "commander",
                    AiProfileType.Protector,
                    new EquipmentLoadout("vermilion-jian", "commander-travel-cloak")),
                ["player-guan-yu"] = new CampaignUnitTemplate(
                    "player-guan-yu",
                    "Guan Yu",
                    "unit.player_guan_yu",
                    UnitRole.Guardian,
                    "role.guardian",
                    PassiveSkillType.ArmorBreak,
                    "skill.armor_break.name",
                    "skill.armor_break.desc",
                    ActiveSkillType.GreenDragonSlash,
                    "skill.green_dragon_slash.name",
                    "skill.green_dragon_slash.desc",
                    34,
                    12,
                    5,
                    3,
                    1,
                    20,
                    "guardian",
                    "guardian",
                    AiProfileType.Protector,
                    new EquipmentLoadout("iron-crescent-glaive", "guardian-scale-vest")),
                ["player-zhang-fei"] = new CampaignUnitTemplate(
                    "player-zhang-fei",
                    "Zhang Fei",
                    "unit.player_zhang_fei",
                    UnitRole.Guardian,
                    "role.guardian",
                    PassiveSkillType.Vanguard,
                    "skill.vanguard.name",
                    "skill.vanguard.desc",
                    ActiveSkillType.WarCry,
                    "skill.war_cry.name",
                    "skill.war_cry.desc",
                    36,
                    11,
                    6,
                    3,
                    1,
                    20,
                    "guardian",
                    "guardian",
                    AiProfileType.Aggressor,
                    new EquipmentLoadout("iron-crescent-glaive", "guardian-scale-vest")),
                ["player-huang-zhong"] = new CampaignUnitTemplate(
                    "player-huang-zhong",
                    "Huang Zhong",
                    "unit.player_huang_zhong",
                    UnitRole.Ranger,
                    "role.ranger",
                    PassiveSkillType.LongShot,
                    "skill.long_shot.name",
                    "skill.long_shot.desc",
                    ActiveSkillType.Volley,
                    "skill.volley.name",
                    "skill.volley.desc",
                    28,
                    10,
                    3,
                    3,
                    2,
                    20,
                    "ranger",
                    "ranger",
                    AiProfileType.Support,
                    new EquipmentLoadout("featherback-war-bow", "ranger-hunt-coat")),
                ["player-zhuge-liang"] = new CampaignUnitTemplate(
                    "player-zhuge-liang",
                    "Zhuge Liang",
                    "unit.player_zhuge_liang",
                    UnitRole.Commander,
                    "role.commander",
                    PassiveSkillType.CommandAura,
                    "skill.command_aura.name",
                    "skill.command_aura.desc",
                    ActiveSkillType.FireStratagem,
                    "skill.fire_stratagem.name",
                    "skill.fire_stratagem.desc",
                    26,
                    8,
                    3,
                    3,
                    1,
                    26,
                    "commander",
                    "commander",
                    AiProfileType.Support,
                    new EquipmentLoadout("wind-feather-fan", "strategist-robe")),
                ["player-zhao-yun"] = new CampaignUnitTemplate(
                    "player-zhao-yun",
                    "Zhao Yun",
                    "unit.player_zhao_yun",
                    UnitRole.Scout,
                    "role.scout",
                    PassiveSkillType.RapidMarch,
                    "skill.rapid_march.name",
                    "skill.rapid_march.desc",
                    ActiveSkillType.DragonPierce,
                    "skill.dragon_pierce.name",
                    "skill.dragon_pierce.desc",
                    31,
                    11,
                    4,
                    4,
                    1,
                    18,
                    "scout",
                    "scout",
                    AiProfileType.Aggressor,
                    new EquipmentLoadout("white-dragon-spear", "scout-travel-mail")),
                ["player-ma-chao"] = new CampaignUnitTemplate(
                    "player-ma-chao",
                    "Ma Chao",
                    "unit.player_ma_chao",
                    UnitRole.Raider,
                    "role.raider",
                    PassiveSkillType.Vanguard,
                    "skill.vanguard.name",
                    "skill.vanguard.desc",
                    ActiveSkillType.WesternStampede,
                    "skill.western_stampede.name",
                    "skill.western_stampede.desc",
                    33,
                    12,
                    4,
                    4,
                    1,
                    18,
                    "raider",
                    "raider",
                    AiProfileType.Aggressor,
                    new EquipmentLoadout("western-lance", "raider-scale-vest")),
            };

        public CampaignSaveData CreateNewSave(CampaignDefinition definition)
        {
            List<CampaignUnitState> units = StartingUnitIds
                .Select(unitId => Templates[unitId])
                .Select(template => template.ToState())
                .ToList();

            CampaignInventoryState inventory = new CampaignInventoryState();
            foreach (CampaignUnitState unit in units)
            {
                inventory.AddItem(unit.EquipmentLoadout.WeaponId);
                inventory.AddItem(unit.EquipmentLoadout.ArmorId);
                inventory.AddItem(unit.EquipmentLoadout.MountId);
            }

            return new CampaignSaveData(
                definition != null ? definition.CampaignId : CampaignCatalog.LiuBeiLegendCampaignId,
                new CampaignProgress(),
                inventory,
                units,
                3);
        }

        public BattleScenarioData BuildScenario(BattleScenarioData baseScenario, CampaignSaveData saveData, bool includeStageReward)
        {
            if (baseScenario == null || saveData == null)
            {
                return baseScenario;
            }

            List<UnitSpawnData> spawns = new List<UnitSpawnData>();
            foreach (UnitSpawnData spawn in baseScenario.Stage.UnitSpawns)
            {
                if (spawn == null || spawn.Definition == null || spawn.Definition.Faction != UnitFaction.Player)
                {
                    spawns.Add(spawn);
                    continue;
                }

                CampaignUnitState unitState = saveData.GetUnit(spawn.Definition.Id);
                spawns.Add(unitState == null
                    ? spawn
                    : new UnitSpawnData(BuildDefinition(unitState), spawn.StartPosition));
            }

            StageDefinitionData stage = new StageDefinitionData(
                baseScenario.Stage.StageName,
                baseScenario.Stage.StageNameKey,
                baseScenario.Stage.Width,
                baseScenario.Stage.Height,
                spawns,
                baseScenario.Stage.BlockedCells.ToList(),
                baseScenario.Stage.TerrainTiles.ToList(),
                baseScenario.Stage.IsRandomMap,
                baseScenario.Stage.MapSeed);

            RewardBundle rewardBundle = includeStageReward
                ? baseScenario.RewardBundle
                : new RewardBundle(0, 0, string.Empty, Array.Empty<string>());

            return new BattleScenarioData(
                baseScenario.ScenarioId,
                baseScenario.ScenarioName,
                baseScenario.ScenarioNameKey,
                stage,
                baseScenario.Triggers.ToList(),
                baseScenario.RecommendedLevel,
                baseScenario.VictoryExpReward,
                baseScenario.DefeatExpReward,
                rewardBundle,
                baseScenario.ReplayDifficultyTier,
                baseScenario.ScenarioVariantTag);
        }

        public IReadOnlyList<string> AddRecruitsIfMissing(CampaignSaveData saveData, RewardBundle rewardBundle)
        {
            if (saveData == null || rewardBundle == null || rewardBundle.RecruitUnitIds.Count == 0)
            {
                return Array.Empty<string>();
            }

            List<string> recruitedUnitIds = new List<string>();
            foreach (string recruitUnitId in rewardBundle.RecruitUnitIds)
            {
                if (string.IsNullOrWhiteSpace(recruitUnitId) ||
                    !Templates.TryGetValue(recruitUnitId, out CampaignUnitTemplate template) ||
                    !saveData.TryAddUnit(template.ToState()))
                {
                    continue;
                }

                AddStartingEquipment(saveData.Inventory, template.EquipmentLoadout);
                recruitedUnitIds.Add(recruitUnitId);
            }

            return recruitedUnitIds;
        }

        private static void AddStartingEquipment(CampaignInventoryState inventory, EquipmentLoadout loadout)
        {
            if (inventory == null || loadout == null)
            {
                return;
            }

            inventory.AddItem(loadout.WeaponId);
            inventory.AddItem(loadout.ArmorId);
            inventory.AddItem(loadout.MountId);
        }

        private static UnitDefinitionData BuildDefinition(CampaignUnitState unitState)
        {
            ItemDefinition weapon = ItemCatalog.Get(unitState.EquipmentLoadout.WeaponId);
            ItemDefinition armor = ItemCatalog.Get(unitState.EquipmentLoadout.ArmorId);
            ItemDefinition mount = ItemCatalog.Get(unitState.EquipmentLoadout.MountId);
            int hpBonus = GetHpBonus(weapon) + GetHpBonus(armor);
            int attackBonus = GetAttackBonus(weapon) + GetAttackBonus(armor);
            int defenseBonus = GetDefenseBonus(weapon) + GetDefenseBonus(armor);
            int moveBonus = GetMoveBonus(mount);

            return new UnitDefinitionData(
                unitState.UnitId,
                unitState.DisplayName,
                unitState.DisplayNameKey,
                UnitFaction.Player,
                unitState.Role,
                unitState.RoleNameKey,
                unitState.PassiveSkill,
                unitState.PassiveSkillNameKey,
                unitState.PassiveSkillDescriptionKey,
                unitState.ActiveSkill,
                unitState.ActiveSkillNameKey,
                unitState.ActiveSkillDescriptionKey,
                unitState.MaxHp + hpBonus,
                unitState.Attack + attackBonus,
                unitState.Defense + defenseBonus,
                unitState.MoveRange + moveBonus,
                unitState.AttackRange,
                unitState.MaxMana,
                unitState.ClassId,
                unitState.GrowthProfileId,
                unitState.AiProfile,
                unitState.EquipmentLoadout,
                unitState.Level,
                unitState.CurrentExp,
                new BondState(unitState.BondState.SupportLevel, unitState.BondState.SharedBattles),
                true);
        }

        private static int GetAttackBonus(ItemDefinition definition)
        {
            return definition != null ? definition.AttackBonus : 0;
        }

        private static int GetDefenseBonus(ItemDefinition definition)
        {
            return definition != null ? definition.DefenseBonus : 0;
        }

        private static int GetHpBonus(ItemDefinition definition)
        {
            return definition != null ? definition.HpBonus : 0;
        }

        private static int GetMoveBonus(ItemDefinition definition)
        {
            return definition != null ? definition.MoveBonus : 0;
        }

        private sealed class CampaignUnitTemplate
        {
            public CampaignUnitTemplate(
                string unitId,
                string displayName,
                string displayNameKey,
                UnitRole role,
                string roleNameKey,
                PassiveSkillType passiveSkill,
                string passiveSkillNameKey,
                string passiveSkillDescriptionKey,
                ActiveSkillType activeSkill,
                string activeSkillNameKey,
                string activeSkillDescriptionKey,
                int maxHp,
                int attack,
                int defense,
                int moveRange,
                int attackRange,
                int maxMana,
                string classId,
                string growthProfileId,
                AiProfileType aiProfile,
                EquipmentLoadout equipmentLoadout)
            {
                UnitId = unitId;
                DisplayName = displayName;
                DisplayNameKey = displayNameKey;
                Role = role;
                RoleNameKey = roleNameKey;
                PassiveSkill = passiveSkill;
                PassiveSkillNameKey = passiveSkillNameKey;
                PassiveSkillDescriptionKey = passiveSkillDescriptionKey;
                ActiveSkill = activeSkill;
                ActiveSkillNameKey = activeSkillNameKey;
                ActiveSkillDescriptionKey = activeSkillDescriptionKey;
                MaxHp = maxHp;
                Attack = attack;
                Defense = defense;
                MoveRange = moveRange;
                AttackRange = attackRange;
                MaxMana = maxMana;
                ClassId = classId;
                GrowthProfileId = growthProfileId;
                AiProfile = aiProfile;
                EquipmentLoadout = equipmentLoadout;
            }

            public string UnitId { get; }

            public string DisplayName { get; }

            public string DisplayNameKey { get; }

            public UnitRole Role { get; }

            public string RoleNameKey { get; }

            public PassiveSkillType PassiveSkill { get; }

            public string PassiveSkillNameKey { get; }

            public string PassiveSkillDescriptionKey { get; }

            public ActiveSkillType ActiveSkill { get; }

            public string ActiveSkillNameKey { get; }

            public string ActiveSkillDescriptionKey { get; }

            public int MaxHp { get; }

            public int Attack { get; }

            public int Defense { get; }

            public int MoveRange { get; }

            public int AttackRange { get; }

            public int MaxMana { get; }

            public string ClassId { get; }

            public string GrowthProfileId { get; }

            public AiProfileType AiProfile { get; }

            public EquipmentLoadout EquipmentLoadout { get; }

            public CampaignUnitState ToState()
            {
                return new CampaignUnitState(
                    UnitId,
                    DisplayName,
                    DisplayNameKey,
                    Role,
                    RoleNameKey,
                    PassiveSkill,
                    PassiveSkillNameKey,
                    PassiveSkillDescriptionKey,
                    ActiveSkill,
                    ActiveSkillNameKey,
                    ActiveSkillDescriptionKey,
                    MaxHp,
                    Attack,
                    Defense,
                    MoveRange,
                    AttackRange,
                    MaxMana,
                    ClassId,
                    GrowthProfileId,
                    AiProfile,
                    EquipmentLoadout,
                    1,
                    0,
                    new BondState(),
                    false);
            }
        }
    }
}
