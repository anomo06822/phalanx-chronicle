using PhalanxChronicle.Core;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField] private string classId = string.Empty;
        [SerializeField] private string growthProfileId = string.Empty;
        [SerializeField] private AiProfileType aiProfile = AiProfileType.Default;
        [SerializeField] private string weaponId = string.Empty;
        [FormerlySerializedAs("accessoryId")]
        [SerializeField] private string armorId = string.Empty;
        [SerializeField] private int startingLevel = 1;
        [SerializeField] private int startingExp;

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
                maxMana,
                classId,
                growthProfileId,
                aiProfile,
                new EquipmentLoadout(weaponId, armorId),
                startingLevel,
                startingExp,
                new BondState());
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
            int maxMana = 20,
            string classId = "",
            string growthProfileId = "",
            AiProfileType aiProfile = AiProfileType.Default,
            string weaponId = "",
            string armorId = "",
            int startingLevel = 1,
            int startingExp = 0)
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
            definition.classId = classId;
            definition.growthProfileId = growthProfileId;
            definition.aiProfile = aiProfile;
            definition.weaponId = weaponId;
            definition.armorId = armorId;
            definition.startingLevel = startingLevel;
            definition.startingExp = startingExp;
            return definition;
        }
    }
}
