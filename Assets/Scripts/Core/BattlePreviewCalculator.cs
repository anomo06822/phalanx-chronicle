using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public static class BattlePreviewCalculator
    {
        public static int EstimateAttackDamage(
            BattleContext context,
            UnitRuntimeState attacker,
            GridPosition attackerPosition,
            UnitRuntimeState defender,
            int flatAttackBonus = 0)
        {
            if (context == null || attacker == null || defender == null)
            {
                return 0;
            }

            int effectiveAttack = attacker.Attack +
                                  PassiveSkillRules.GetAttackBonus(context, attacker, attackerPosition) +
                                  StatusEffectRules.GetAttackModifier(attacker) +
                                  flatAttackBonus;
            int effectiveDefense = defender.Defense +
                                   PassiveSkillRules.GetDefenseBonus(defender) +
                                   StatusEffectRules.GetDefenseModifier(defender) -
                                   PassiveSkillRules.GetIgnoredDefense(attacker);
            if (effectiveDefense < 0)
            {
                effectiveDefense = 0;
            }

            int damage = effectiveAttack - effectiveDefense;
            return damage < 1 ? 1 : damage;
        }

        public static int EstimateHealing(UnitRuntimeState target, int amount)
        {
            if (target == null || !target.IsAlive || amount <= 0)
            {
                return 0;
            }

            int missingHp = target.MaxHp - target.CurrentHp;
            return missingHp <= amount ? missingHp : amount;
        }

        public static IReadOnlyList<UnitRuntimeState> GetVolleyTargets(BattleContext context, UnitRuntimeState primaryTarget)
        {
            if (context == null || primaryTarget == null)
            {
                return new List<UnitRuntimeState>();
            }

            return context.GetUnits(primaryTarget.Faction)
                .Where(unit => unit.Position.ManhattanDistance(primaryTarget.Position) <= 1)
                .OrderBy(unit => unit.Position.ManhattanDistance(primaryTarget.Position))
                .ThenBy(unit => unit.Id)
                .ToList();
        }
    }
}
