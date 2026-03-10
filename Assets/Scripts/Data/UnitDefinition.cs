using PhalanxChronicle.Core;
using UnityEngine;

namespace PhalanxChronicle.Data
{
    [CreateAssetMenu(menuName = "Phalanx Chronicle/Unit Definition", fileName = "UnitDefinition")]
    public sealed class UnitDefinition : ScriptableObject
    {
        [SerializeField] private string unitId = "unit";
        [SerializeField] private string displayName = "Unit";
        [SerializeField] private string displayNameKey = "unit.name";
        [SerializeField] private UnitFaction faction = UnitFaction.Player;
        [SerializeField] private UnitRole role = UnitRole.Commander;
        [SerializeField] private string roleNameKey = "role.commander";
        [SerializeField] private PassiveSkillType passiveSkill = PassiveSkillType.None;
        [SerializeField] private string passiveSkillNameKey = "skill.none.name";
        [SerializeField] private string passiveSkillDescriptionKey = "skill.none.desc";
        [SerializeField] private ActiveSkillType activeSkill = ActiveSkillType.None;
        [SerializeField] private string activeSkillNameKey = "active.none.name";
        [SerializeField] private string activeSkillDescriptionKey = "active.none.desc";
        [SerializeField] private int maxHp = 30;
        [SerializeField] private int attack = 10;
        [SerializeField] private int defense = 5;
        [SerializeField] private int moveRange = 3;
        [SerializeField] private int attackRange = 1;
        [SerializeField] private int maxMana = 20;

        public string DisplayName => displayName;

        public UnitFaction Faction => faction;

        public UnitDefinitionData ToData()
        {
            return new UnitDefinitionData(
                unitId,
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
                maxMana);
        }

        public static UnitDefinition CreateRuntime(
            string unitId,
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
            int maxMana = 20)
        {
            UnitDefinition definition = CreateInstance<UnitDefinition>();
            definition.unitId = unitId;
            definition.displayName = displayName;
            definition.displayNameKey = displayNameKey;
            definition.faction = faction;
            definition.role = role;
            definition.roleNameKey = roleNameKey;
            definition.passiveSkill = passiveSkill;
            definition.passiveSkillNameKey = passiveSkillNameKey;
            definition.passiveSkillDescriptionKey = passiveSkillDescriptionKey;
            definition.activeSkill = activeSkill;
            definition.activeSkillNameKey = activeSkillNameKey;
            definition.activeSkillDescriptionKey = activeSkillDescriptionKey;
            definition.maxHp = maxHp;
            definition.attack = attack;
            definition.defense = defense;
            definition.moveRange = moveRange;
            definition.attackRange = attackRange;
            definition.maxMana = maxMana;
            return definition;
        }
    }
}
