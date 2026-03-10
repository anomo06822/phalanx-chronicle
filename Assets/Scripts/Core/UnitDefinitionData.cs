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
    }
}
