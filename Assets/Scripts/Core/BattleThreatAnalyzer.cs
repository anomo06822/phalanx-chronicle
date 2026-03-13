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
            return AnalyzeProjected(context, focusUnit, focusUnit != null ? focusUnit.Position : new GridPosition(0, 0)).ToSummary();
        }

        public static BattleThreatProjection AnalyzeProjected(
            BattleContext context,
            UnitRuntimeState focusUnit,
            GridPosition projectedPosition,
            IReadOnlyCollection<string> defeatedUnitIds = null)
        {
            if (context == null || focusUnit == null || !focusUnit.IsAlive)
            {
                return new BattleThreatProjection(0, 0, new List<string>());
            }

            UnitFaction opposingFaction = focusUnit.Faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player;
            RangeCalculator rangeCalculator = new RangeCalculator();
            HashSet<string> defeatedIds = defeatedUnitIds != null
                ? new HashSet<string>(defeatedUnitIds.Where(id => !string.IsNullOrWhiteSpace(id)))
                : new HashSet<string>();
            List<string> threateningUnitIds = new List<string>();
            int maxProjectedDamage = 0;

            foreach (UnitRuntimeState opposingUnit in context.GetUnits(opposingFaction))
            {
                if (defeatedIds.Contains(opposingUnit.Id))
                {
                    continue;
                }

                int bestDamage = EstimateThreatDamage(context, rangeCalculator, opposingUnit, focusUnit, projectedPosition, defeatedIds);
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

            return new BattleThreatProjection(
                threateningUnitIds.Count,
                maxProjectedDamage,
                threateningUnitIds);
        }

        private static int EstimateThreatDamage(
            BattleContext context,
            RangeCalculator rangeCalculator,
            UnitRuntimeState opposingUnit,
            UnitRuntimeState focusUnit,
            GridPosition projectedPosition,
            ISet<string> defeatedUnitIds)
        {
            if (opposingUnit == null || !opposingUnit.IsAlive || opposingUnit.HasActed || defeatedUnitIds.Contains(opposingUnit.Id))
            {
                return 0;
            }

            int bestDamage = 0;
            IReadOnlyList<GridPosition> projectedOrigins = rangeCalculator.GetMoveRange(context, opposingUnit);
            foreach (GridPosition origin in projectedOrigins)
            {
                if (origin.ManhattanDistance(projectedPosition) <= PassiveSkillRules.GetAttackRange(opposingUnit))
                {
                    int damage = BattlePreviewCalculator.EstimateAttackDamage(context, opposingUnit, origin, focusUnit, projectedPosition);
                    if (damage > bestDamage)
                    {
                        bestDamage = damage;
                    }
                }

                if (!opposingUnit.CanUseSkill || !ActiveSkillRules.IsOffensiveSkill(opposingUnit.ActiveSkill))
                {
                    continue;
                }

                int skillDamage = EstimateSkillDamageAgainstUnit(context, opposingUnit, origin, focusUnit, projectedPosition, defeatedUnitIds);
                if (skillDamage > bestDamage)
                {
                    bestDamage = skillDamage;
                }
            }

            return bestDamage;
        }

        private static int EstimateSkillDamageAgainstUnit(
            BattleContext context,
            UnitRuntimeState caster,
            GridPosition origin,
            UnitRuntimeState focusUnit,
            GridPosition projectedPosition,
            ISet<string> defeatedUnitIds)
        {
            int range = ActiveSkillRules.GetRange(caster);
            switch (caster.ActiveSkill)
            {
                case ActiveSkillType.PowerStrike:
                case ActiveSkillType.DragonPierce:
                case ActiveSkillType.WhiteHorseRescue:
                case ActiveSkillType.PinningShot:
                    return origin.ManhattanDistance(projectedPosition) <= range
                        ? BattlePreviewCalculator.EstimateAttackDamage(
                            context,
                            caster,
                            origin,
                            focusUnit,
                            projectedPosition,
                            GetSkillFlatBonus(caster),
                            caster.ActiveSkill == ActiveSkillType.DragonPierce
                                ? ActiveSkillRules.GetDragonPierceIgnoredDefense(caster)
                                : caster.ActiveSkill == ActiveSkillType.WhiteHorseRescue
                                    ? ActiveSkillRules.GetWhiteHorseRescueIgnoredDefense(caster)
                                : 0)
                        : 0;
                case ActiveSkillType.Volley:
                case ActiveSkillType.SkyVolley:
                case ActiveSkillType.FireStratagem:
                case ActiveSkillType.EightTrigramInferno:
                    return CanProjectedAreaSkillHitFocus(context, caster, origin, projectedPosition, focusUnit, defeatedUnitIds)
                        ? BattlePreviewCalculator.EstimateAttackDamage(
                            context,
                            caster,
                            origin,
                            focusUnit,
                            projectedPosition,
                            GetSkillFlatBonus(caster))
                        : 0;
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.AzureDragonSlash:
                case ActiveSkillType.WesternStampede:
                case ActiveSkillType.CrimsonCrescent:
                case ActiveSkillType.StormbreakCharge:
                    return CanProjectedLineSkillHitFocus(context, caster, origin, projectedPosition, focusUnit, defeatedUnitIds)
                        ? BattlePreviewCalculator.EstimateAttackDamage(
                            context,
                            caster,
                            origin,
                            focusUnit,
                            projectedPosition,
                            GetSkillFlatBonus(caster))
                        : 0;
                case ActiveSkillType.StonewallChallenge:
                case ActiveSkillType.DustDevilSweep:
                    return origin.ManhattanDistance(projectedPosition) <= range
                        ? BattlePreviewCalculator.EstimateAttackDamage(
                            context,
                            caster,
                            origin,
                            focusUnit,
                            projectedPosition,
                            GetSkillFlatBonus(caster))
                        : 0;
                default:
                    return 0;
            }
        }

        private static bool CanProjectedAreaSkillHitFocus(
            BattleContext context,
            UnitRuntimeState caster,
            GridPosition origin,
            GridPosition projectedPosition,
            UnitRuntimeState focusUnit,
            ISet<string> defeatedUnitIds)
        {
            foreach (GridPosition primaryTargetPosition in GetProjectedPrimaryTargetPositions(context, caster, focusUnit, projectedPosition, defeatedUnitIds))
            {
                if (origin.ManhattanDistance(primaryTargetPosition) > ActiveSkillRules.GetRange(caster))
                {
                    continue;
                }

                if (BattlePreviewCalculator.GetVolleyAreaPositions(primaryTargetPosition).Contains(projectedPosition))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanProjectedLineSkillHitFocus(
            BattleContext context,
            UnitRuntimeState caster,
            GridPosition origin,
            GridPosition projectedPosition,
            UnitRuntimeState focusUnit,
            ISet<string> defeatedUnitIds)
        {
            foreach (GridPosition primaryTargetPosition in GetProjectedPrimaryTargetPositions(context, caster, focusUnit, projectedPosition, defeatedUnitIds))
            {
                if (origin.ManhattanDistance(primaryTargetPosition) > ActiveSkillRules.GetRange(caster))
                {
                    continue;
                }

                if (BattlePreviewCalculator.GetGreenDragonSlashAreaPositions(origin, primaryTargetPosition).Contains(projectedPosition))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<GridPosition> GetProjectedPrimaryTargetPositions(
            BattleContext context,
            UnitRuntimeState caster,
            UnitRuntimeState focusUnit,
            GridPosition projectedPosition,
            ISet<string> defeatedUnitIds)
        {
            if (context == null || caster == null || focusUnit == null)
            {
                return new List<GridPosition>();
            }

            UnitFaction targetFaction = caster.Faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player;
            return context.GetUnits(targetFaction)
                .Where(unit => unit.IsAlive && !defeatedUnitIds.Contains(unit.Id))
                .Where(unit => unit.Id == focusUnit.Id || unit.Position != projectedPosition)
                .Select(unit => unit.Id == focusUnit.Id ? projectedPosition : unit.Position)
                .Distinct()
                .OrderBy(position => position.Y)
                .ThenBy(position => position.X)
                .ToList();
        }

        private static int GetSkillFlatBonus(UnitRuntimeState caster)
        {
            switch (caster.ActiveSkill)
            {
                case ActiveSkillType.PowerStrike:
                    return ActiveSkillRules.GetPowerStrikeBonus(caster);
                case ActiveSkillType.DragonPierce:
                    return ActiveSkillRules.GetDragonPierceBonus(caster);
                case ActiveSkillType.WhiteHorseRescue:
                    return ActiveSkillRules.GetWhiteHorseRescueBonus(caster);
                case ActiveSkillType.PinningShot:
                    return ActiveSkillRules.GetPinningShotBonus(caster);
                case ActiveSkillType.Volley:
                    return ActiveSkillRules.GetVolleyBonus(caster);
                case ActiveSkillType.SkyVolley:
                    return ActiveSkillRules.GetSkyVolleyBonus(caster);
                case ActiveSkillType.CrimsonCrescent:
                    return ActiveSkillRules.GetCrimsonCrescentBonus(caster);
                case ActiveSkillType.GreenDragonSlash:
                    return ActiveSkillRules.GetGreenDragonSlashBonus(caster);
                case ActiveSkillType.AzureDragonSlash:
                    return ActiveSkillRules.GetAzureDragonSlashBonus(caster);
                case ActiveSkillType.WesternStampede:
                    return ActiveSkillRules.GetWesternStampedeBonus(caster);
                case ActiveSkillType.StonewallChallenge:
                    return ActiveSkillRules.GetStonewallChallengeBonus(caster);
                case ActiveSkillType.StormbreakCharge:
                    return ActiveSkillRules.GetStormbreakChargeBonus(caster);
                case ActiveSkillType.DustDevilSweep:
                    return ActiveSkillRules.GetDustDevilSweepBonus(caster);
                case ActiveSkillType.FireStratagem:
                    return ActiveSkillRules.GetFireStratagemBonus(caster);
                case ActiveSkillType.EightTrigramInferno:
                    return ActiveSkillRules.GetEightTrigramInfernoBonus(caster);
                default:
                    return 0;
            }
        }
    }
}
