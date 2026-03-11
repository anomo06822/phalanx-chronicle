using System;

namespace PhalanxChronicle.Core
{
    [Serializable]
    public sealed class UnitDefinitionData
    {
        public UnitDefinitionData(
            string id,
            string displayName,
            string displayNameKey,
            UnitFaction faction,
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
            int attackRange)
            : this(
                id,
                displayName,
                displayNameKey,
                faction,
                role,
                roleNameKey,
                passiveSkill,
                passiveSkillNameKey,
                passiveSkillDescriptionKey,
                activeSkill,
                activeSkillNameKey,
                activeSkillDescriptionKey,
                maxHp,
                attack,
                defense,
                moveRange,
                attackRange,
                20,
                null,
                null,
                AiProfileType.Default,
                null,
                1,
                0,
                null)
        {
        }

        public UnitDefinitionData(
            string id,
            string displayName,
            string displayNameKey,
            UnitFaction faction,
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
            int maxMana)
            : this(
                id,
                displayName,
                displayNameKey,
                faction,
                role,
                roleNameKey,
                passiveSkill,
                passiveSkillNameKey,
                passiveSkillDescriptionKey,
                activeSkill,
                activeSkillNameKey,
                activeSkillDescriptionKey,
                maxHp,
                attack,
                defense,
                moveRange,
                attackRange,
                maxMana,
                null,
                null,
                AiProfileType.Default,
                null,
                1,
                0,
                null)
        {
        }

        public UnitDefinitionData(
            string id,
            string displayName,
            string displayNameKey,
            UnitFaction faction,
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
            EquipmentLoadout equipmentLoadout,
            int startingLevel,
            int startingExp,
            BondState bondState,
            bool progressionResolved = false)
        {
            Id = id;
            DisplayName = displayName;
            DisplayNameKey = displayNameKey;
            Faction = faction;
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
            ClassId = string.IsNullOrWhiteSpace(classId) ? UnitClassCatalog.GetDefaultClassId(role) : classId;
            GrowthProfileId = string.IsNullOrWhiteSpace(growthProfileId) ? ClassId : growthProfileId;
            AiProfile = aiProfile == AiProfileType.Default ? UnitClassCatalog.Get(ClassId).DefaultAiProfile : aiProfile;
            EquipmentLoadout = equipmentLoadout ?? EquipmentLoadout.Empty;
            StartingLevel = startingLevel < 1 ? 1 : startingLevel;
            StartingExp = startingExp < 0 ? 0 : startingExp;
            BondState = bondState ?? new BondState();
            ProgressionResolved = progressionResolved;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string DisplayNameKey { get; }

        public UnitFaction Faction { get; }

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

        public int StartingLevel { get; }

        public int StartingExp { get; }

        public BondState BondState { get; }

        public bool ProgressionResolved { get; }
    }
}
