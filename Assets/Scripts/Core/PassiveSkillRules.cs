using System.Linq;

namespace PhalanxChronicle.Core
{
    public static class PassiveSkillRules
    {
        public static int GetMoveRange(UnitRuntimeState unit)
        {
            if (unit == null)
            {
                return 0;
            }

            if (unit.PassiveSkill == PassiveSkillType.RapidMarch || unit.PassiveSkill == PassiveSkillType.GaleStride)
            {
                return unit.MoveRange + (unit.SignaturePassiveUnlocked ? 2 : 1);
            }

            return unit.MoveRange;
        }

        public static int GetAttackRange(UnitRuntimeState unit)
        {
            int bonus = 0;
            if (unit.PassiveSkill == PassiveSkillType.LongShot)
            {
                bonus = unit.SignaturePassiveUnlocked ? 2 : 1;
            }
            else if (unit.PassiveSkill == PassiveSkillType.Deadeye)
            {
                bonus = unit.SignaturePassiveUnlocked ? 3 : 2;
            }

            return unit.AttackRange + bonus;
        }

        public static int GetAttackBonus(BattleContext context, UnitRuntimeState attacker)
        {
            return GetAttackBonus(context, attacker, attacker.Position);
        }

        public static int GetAttackBonus(BattleContext context, UnitRuntimeState attacker, GridPosition attackerPosition)
        {
            return context.GetUnits(attacker.Faction)
                .Where(unit => unit.Id != attacker.Id &&
                               (unit.PassiveSkill == PassiveSkillType.CommandAura || unit.PassiveSkill == PassiveSkillType.BenevolentCommand) &&
                               unit.Position.ManhattanDistance(attackerPosition) == 1)
                .Select(unit =>
                    unit.PassiveSkill == PassiveSkillType.BenevolentCommand
                        ? (unit.SignaturePassiveUnlocked ? 4 : 3)
                        : (unit.SignaturePassiveUnlocked ? 3 : 2))
                .DefaultIfEmpty(0)
                .Max();
        }

        public static int GetDefenseBonus(UnitRuntimeState defender)
        {
            if (defender.PassiveSkill == PassiveSkillType.ShieldWall)
            {
                return defender.SignaturePassiveUnlocked ? 3 : 2;
            }

            if (defender.PassiveSkill == PassiveSkillType.Fortress)
            {
                return defender.HasMovedThisTurn
                    ? 1
                    : defender.SignaturePassiveUnlocked ? 4 : 3;
            }

            return defender.PassiveSkill == PassiveSkillType.DragonGuard
                ? (defender.SignaturePassiveUnlocked ? 4 : 3)
                : 0;
        }

        public static int GetIgnoredDefense(UnitRuntimeState attacker)
        {
            if (attacker.PassiveSkill == PassiveSkillType.ArmorBreak)
            {
                return attacker.SignaturePassiveUnlocked ? 3 : 2;
            }

            return attacker.PassiveSkill == PassiveSkillType.DragonGuard
                ? (attacker.SignaturePassiveUnlocked ? 4 : 3)
                : 0;
        }

        public static int GetDamageBonus(UnitRuntimeState attacker)
        {
            return attacker == null ? 0 : GetDamageBonus(attacker, attacker.Position);
        }

        public static int GetDamageBonus(UnitRuntimeState attacker, GridPosition attackOrigin)
        {
            if (attacker == null)
            {
                return 0;
            }

            if (attacker.PassiveSkill == PassiveSkillType.ThunderVanguard)
            {
                return attacker.HasMovedThisTurn || attackOrigin != attacker.Position
                    ? (attacker.SignaturePassiveUnlocked ? 4 : 3)
                    : 0;
            }

            if (attacker.PassiveSkill != PassiveSkillType.Vanguard)
            {
                return 0;
            }

            return attacker.HasMovedThisTurn || attackOrigin != attacker.Position
                ? (attacker.SignaturePassiveUnlocked ? 3 : 2)
                : 0;
        }

        public static int GetPersonalAttackBonus(UnitRuntimeState attacker)
        {
            return attacker != null && attacker.PassiveSkill == PassiveSkillType.Deadeye
                ? (attacker.SignaturePassiveUnlocked ? 2 : 1)
                : 0;
        }
    }
}
