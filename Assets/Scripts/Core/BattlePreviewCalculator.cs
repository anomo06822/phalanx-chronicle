using System;
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
            int flatAttackBonus = 0,
            int ignoredDefenseBonus = 0)
        {
            return EstimateAttackDamage(
                context,
                attacker,
                attackerPosition,
                defender,
                defender != null ? defender.Position : new GridPosition(0, 0),
                flatAttackBonus,
                ignoredDefenseBonus);
        }

        public static int EstimateAttackDamage(
            BattleContext context,
            UnitRuntimeState attacker,
            GridPosition attackerPosition,
            UnitRuntimeState defender,
            GridPosition defenderPosition,
            int flatAttackBonus = 0,
            int ignoredDefenseBonus = 0)
        {
            if (context == null || attacker == null || defender == null)
            {
                return 0;
            }

            int effectiveAttack = attacker.Attack +
                                  PassiveSkillRules.GetPersonalAttackBonus(attacker) +
                                  PassiveSkillRules.GetAttackBonus(context, attacker, attackerPosition) +
                                  SupportRules.GetAttackBonus(context, attacker, attackerPosition) +
                                  PassiveSkillRules.GetDamageBonus(attacker, attackerPosition) +
                                  StatusEffectRules.GetAttackModifier(attacker) +
                                  flatAttackBonus;
            int effectiveDefense = defender.Defense +
                                   PassiveSkillRules.GetDefenseBonus(defender) +
                                   SupportRules.GetDefenseBonus(context, defender) +
                                   TerrainRules.GetDefenseBonus(context.GetTerrainAt(defenderPosition)) +
                                   StatusEffectRules.GetDefenseModifier(defender) -
                                   PassiveSkillRules.GetIgnoredDefense(attacker) -
                                   ignoredDefenseBonus;
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

        public static IReadOnlyList<GridPosition> GetVolleyAreaPositions(GridPosition primaryTargetPosition)
        {
            return primaryTargetPosition
                .GetOrthogonalNeighbors()
                .Append(primaryTargetPosition)
                .Distinct()
                .OrderBy(position => position.Y)
                .ThenBy(position => position.X)
                .ToList();
        }

        public static IReadOnlyList<UnitRuntimeState> GetGreenDragonSlashTargets(
            BattleContext context,
            GridPosition attackerPosition,
            UnitRuntimeState primaryTarget)
        {
            if (context == null || primaryTarget == null)
            {
                return new List<UnitRuntimeState>();
            }

            List<UnitRuntimeState> targets = new List<UnitRuntimeState> { primaryTarget };
            int distanceX = primaryTarget.Position.X - attackerPosition.X;
            int distanceY = primaryTarget.Position.Y - attackerPosition.Y;
            if (Math.Abs(distanceX) + Math.Abs(distanceY) != 1)
            {
                return targets;
            }

            GridPosition secondaryPosition = new GridPosition(
                primaryTarget.Position.X + distanceX,
                primaryTarget.Position.Y + distanceY);
            UnitRuntimeState secondaryTarget = context.GetUnitAt(secondaryPosition);
            if (secondaryTarget != null &&
                secondaryTarget.IsAlive &&
                secondaryTarget.Faction == primaryTarget.Faction)
            {
                targets.Add(secondaryTarget);
            }

            return targets;
        }

        public static IReadOnlyList<GridPosition> GetGreenDragonSlashAreaPositions(
            GridPosition attackerPosition,
            GridPosition primaryTargetPosition)
        {
            List<GridPosition> positions = new List<GridPosition> { primaryTargetPosition };
            int distanceX = primaryTargetPosition.X - attackerPosition.X;
            int distanceY = primaryTargetPosition.Y - attackerPosition.Y;
            if (Math.Abs(distanceX) + Math.Abs(distanceY) != 1)
            {
                return positions;
            }

            positions.Add(new GridPosition(primaryTargetPosition.X + distanceX, primaryTargetPosition.Y + distanceY));
            return positions;
        }
    }
}
