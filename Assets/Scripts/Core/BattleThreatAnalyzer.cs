using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class BattleThreatSummary
    {
        public BattleThreatSummary(int threateningEnemyCount, int maxProjectedDamage, IReadOnlyList<string> threateningUnitIds)
        {
            ThreateningEnemyCount = threateningEnemyCount;
            MaxProjectedDamage = maxProjectedDamage;
            ThreateningUnitIds = threateningUnitIds ?? new List<string>();
        }

        public int ThreateningEnemyCount { get; }

        public int MaxProjectedDamage { get; }

        public IReadOnlyList<string> ThreateningUnitIds { get; }
    }

    public static class BattleThreatAnalyzer
    {
        public static BattleThreatSummary Analyze(BattleContext context, UnitRuntimeState focusUnit)
        {
            if (context == null || focusUnit == null || !focusUnit.IsAlive)
            {
                return new BattleThreatSummary(0, 0, new List<string>());
            }

            UnitFaction opposingFaction = focusUnit.Faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player;
            RangeCalculator rangeCalculator = new RangeCalculator();
            SkillSystem skillSystem = new SkillSystem();
            List<string> threateningUnitIds = new List<string>();
            int maxProjectedDamage = 0;

            foreach (UnitRuntimeState opposingUnit in context.GetUnits(opposingFaction))
            {
                int bestDamage = EstimateThreatDamage(context, rangeCalculator, skillSystem, opposingUnit, focusUnit);
                if (bestDamage <= 0)
                {
                    continue;
                }

                threateningUnitIds.Add(opposingUnit.Id);
                if (bestDamage > maxProjectedDamage)
                {
                    maxProjectedDamage = bestDamage;
                }
            }

            return new BattleThreatSummary(
                threateningUnitIds.Count,
                maxProjectedDamage,
                threateningUnitIds);
        }

        private static int EstimateThreatDamage(
            BattleContext context,
            RangeCalculator rangeCalculator,
            SkillSystem skillSystem,
            UnitRuntimeState opposingUnit,
            UnitRuntimeState focusUnit)
        {
            if (opposingUnit == null || !opposingUnit.IsAlive || opposingUnit.HasActed)
            {
                return 0;
            }

            int bestDamage = 0;
            IReadOnlyList<GridPosition> projectedOrigins = rangeCalculator.GetMoveRange(context, opposingUnit);
            foreach (GridPosition origin in projectedOrigins)
            {
                if (rangeCalculator.GetAttackableTargets(context, opposingUnit, origin).Any(target => target.Id == focusUnit.Id))
                {
                    int damage = BattlePreviewCalculator.EstimateAttackDamage(context, opposingUnit, origin, focusUnit);
                    if (damage > bestDamage)
                    {
                        bestDamage = damage;
                    }
                }

                if (!opposingUnit.CanUseSkill || !ActiveSkillRules.IsOffensiveSkill(opposingUnit.ActiveSkill))
                {
                    continue;
                }

                foreach (UnitRuntimeState primaryTarget in skillSystem.GetSkillTargets(context, opposingUnit, origin))
                {
                    int damage = EstimateSkillDamageAgainstUnit(context, opposingUnit, origin, primaryTarget, focusUnit);
                    if (damage > bestDamage)
                    {
                        bestDamage = damage;
                    }
                }
            }

            return bestDamage;
        }

        private static int EstimateSkillDamageAgainstUnit(
            BattleContext context,
            UnitRuntimeState caster,
            GridPosition origin,
            UnitRuntimeState primaryTarget,
            UnitRuntimeState focusUnit)
        {
            switch (caster.ActiveSkill)
            {
                case ActiveSkillType.PowerStrike:
                    return primaryTarget.Id == focusUnit.Id
                        ? BattlePreviewCalculator.EstimateAttackDamage(
                            context,
                            caster,
                            origin,
                            focusUnit,
                            ActiveSkillRules.GetPowerStrikeBonus())
                        : 0;
                case ActiveSkillType.Volley:
                    return BattlePreviewCalculator.GetVolleyTargets(context, primaryTarget).Any(unit => unit.Id == focusUnit.Id)
                        ? BattlePreviewCalculator.EstimateAttackDamage(
                            context,
                            caster,
                            origin,
                            focusUnit,
                            ActiveSkillRules.GetVolleyBonus())
                        : 0;
                case ActiveSkillType.GreenDragonSlash:
                    return BattlePreviewCalculator.GetGreenDragonSlashTargets(context, origin, primaryTarget).Any(unit => unit.Id == focusUnit.Id)
                        ? BattlePreviewCalculator.EstimateAttackDamage(
                            context,
                            caster,
                            origin,
                            focusUnit,
                            ActiveSkillRules.GetGreenDragonSlashBonus())
                        : 0;
                default:
                    return 0;
            }
        }
    }
}
