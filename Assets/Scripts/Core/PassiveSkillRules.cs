using System.Linq;

namespace PhalanxChronicle.Core
{
    public static class PassiveSkillRules
    {
        public static int GetMoveRange(UnitRuntimeState unit)
        {
            return unit.MoveRange + (unit.PassiveSkill == PassiveSkillType.RapidMarch ? 1 : 0);
        }

        public static int GetAttackRange(UnitRuntimeState unit)
        {
            return unit.AttackRange + (unit.PassiveSkill == PassiveSkillType.LongShot ? 1 : 0);
        }

        public static int GetAttackBonus(BattleContext context, UnitRuntimeState attacker)
        {
            return GetAttackBonus(context, attacker, attacker.Position);
        }

        public static int GetAttackBonus(BattleContext context, UnitRuntimeState attacker, GridPosition attackerPosition)
        {
            int bonus = 0;
            bool hasCommanderAura = context.GetUnits(attacker.Faction)
                .Any(unit => unit.Id != attacker.Id &&
                             unit.PassiveSkill == PassiveSkillType.CommandAura &&
                             unit.Position.ManhattanDistance(attackerPosition) == 1);

            if (hasCommanderAura)
            {
                bonus += 2;
            }

            return bonus;
        }

        public static int GetDefenseBonus(UnitRuntimeState defender)
        {
            return defender.PassiveSkill == PassiveSkillType.ShieldWall ? 2 : 0;
        }

        public static int GetIgnoredDefense(UnitRuntimeState attacker)
        {
            return attacker.PassiveSkill == PassiveSkillType.ArmorBreak ? 2 : 0;
        }

        public static int GetDamageBonus(UnitRuntimeState attacker)
        {
            return attacker == null ? 0 : GetDamageBonus(attacker, attacker.Position);
        }

        public static int GetDamageBonus(UnitRuntimeState attacker, GridPosition attackOrigin)
        {
            if (attacker == null || attacker.PassiveSkill != PassiveSkillType.Vanguard)
            {
                return 0;
            }

            return attacker.HasMovedThisTurn || attackOrigin != attacker.Position ? 2 : 0;
        }
    }
}
