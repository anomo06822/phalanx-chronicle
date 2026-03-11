using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class AIController
    {
        private const float DamageWeight = 4f;
        private const float SkillDamageWeight = 4.5f;
        private const float HealingWeight = 3.5f;
        private const float KillBonus = 32f;
        private const float StatusBonus = 7f;
        private const float AdditionalTargetBonus = 8f;
        private const float SkillCommitmentPenalty = 5f;
        private const float ThreatDamageWeight = 0.8f;
        private const float ThreatUnitPenalty = 2.5f;
        private const float LethalThreatPenalty = 10f;
        private const float PressureBaseScore = 20f;
        private const float PressureDistancePenalty = 2.5f;

        private readonly RangeCalculator rangeCalculator;
        private readonly SkillSystem skillSystem;

        public AIController(RangeCalculator rangeCalculator, SkillSystem skillSystem)
        {
            this.rangeCalculator = rangeCalculator;
            this.skillSystem = skillSystem;
        }

        public AiDecision Decide(BattleContext context, UnitRuntimeState enemyUnit)
        {
            IReadOnlyList<UnitRuntimeState> playerUnits = context.GetUnits(UnitFaction.Player);
            if (enemyUnit == null || !enemyUnit.IsAlive || enemyUnit.HasActed || playerUnits.Count == 0)
            {
                return new AiDecision(enemyUnit != null ? enemyUnit.Position : new GridPosition(0, 0), AiActionType.None, null);
            }

            AiCandidate bestCandidate = BuildCandidates(context, enemyUnit)
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Risk)
                .ThenByDescending(candidate => candidate.ActionPriority)
                .ThenBy(candidate => candidate.TargetHp)
                .ThenBy(candidate => candidate.NearestOpponentDistance)
                .ThenBy(candidate => candidate.MoveDistance)
                .ThenBy(candidate => candidate.Destination.Y)
                .ThenBy(candidate => candidate.Destination.X)
                .First();

            return new AiDecision(bestCandidate.Destination, bestCandidate.ActionType, bestCandidate.TargetUnitId);
        }

        private IEnumerable<AiCandidate> BuildCandidates(BattleContext context, UnitRuntimeState enemyUnit)
        {
            IReadOnlyList<GridPosition> reachableCells = rangeCalculator.GetMoveRange(context, enemyUnit);
            foreach (GridPosition destination in reachableCells)
            {
                yield return CreateIdleCandidate(context, enemyUnit, destination);

                foreach (UnitRuntimeState attackTarget in rangeCalculator.GetAttackableTargets(context, enemyUnit, destination))
                {
                    yield return CreateAttackCandidate(context, enemyUnit, destination, attackTarget);
                }

                foreach (UnitRuntimeState skillTarget in skillSystem.GetSkillTargets(context, enemyUnit, destination))
                {
                    yield return CreateSkillCandidate(context, enemyUnit, destination, skillTarget);
                }
            }
        }

        private AiCandidate CreateIdleCandidate(BattleContext context, UnitRuntimeState enemyUnit, GridPosition destination)
        {
            int nearestOpponentDistance = CalculateNearestOpponentDistance(context, enemyUnit.Faction, destination, null);
            float pressure = CalculatePressureScore(nearestOpponentDistance) + GetProfilePressureBonus(context, enemyUnit, destination);
            float risk = EstimateExposure(context, enemyUnit, destination, null) * GetRiskMultiplier(enemyUnit.AiProfile);
            return new AiCandidate(
                destination,
                AiActionType.None,
                null,
                pressure - risk,
                risk,
                int.MaxValue,
                nearestOpponentDistance,
                enemyUnit.Position.ManhattanDistance(destination));
        }

        private AiCandidate CreateAttackCandidate(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target)
        {
            int rawDamage = BattlePreviewCalculator.EstimateAttackDamage(context, enemyUnit, destination, target);
            bool targetDies = rawDamage >= target.CurrentHp;
            int realizedDamage = rawDamage > target.CurrentHp ? target.CurrentHp : rawDamage;
            HashSet<string> defeatedUnitIds = targetDies ? new HashSet<string> { target.Id } : null;
            int nearestOpponentDistance = CalculateNearestOpponentDistance(context, enemyUnit.Faction, destination, defeatedUnitIds);
            float pressure = CalculatePressureScore(nearestOpponentDistance) + GetProfilePressureBonus(context, enemyUnit, destination);
            float risk = EstimateExposure(context, enemyUnit, destination, defeatedUnitIds) * GetRiskMultiplier(enemyUnit.AiProfile);
            float reward = 6f + (realizedDamage * DamageWeight) + (targetDies ? KillBonus : 0f) + GetActionBias(enemyUnit.AiProfile, false, targetDies);

            return new AiCandidate(
                destination,
                AiActionType.Attack,
                target.Id,
                reward + pressure - risk,
                risk,
                target.CurrentHp,
                nearestOpponentDistance,
                enemyUnit.Position.ManhattanDistance(destination));
        }

        private AiCandidate CreateSkillCandidate(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target)
        {
            SkillEvaluation evaluation = EvaluateSkill(context, enemyUnit, destination, target);
            int nearestOpponentDistance = CalculateNearestOpponentDistance(context, enemyUnit.Faction, destination, evaluation.DefeatedUnitIds);
            float pressure = CalculatePressureScore(nearestOpponentDistance) + GetProfilePressureBonus(context, enemyUnit, destination);
            float risk = EstimateExposure(context, enemyUnit, destination, evaluation.DefeatedUnitIds) * GetRiskMultiplier(enemyUnit.AiProfile);
            return new AiCandidate(
                destination,
                AiActionType.Skill,
                target.Id,
                evaluation.Reward + GetActionBias(enemyUnit.AiProfile, true, evaluation.DefeatedUnitIds.Count > 0) + pressure - risk,
                risk,
                target.CurrentHp,
                nearestOpponentDistance,
                enemyUnit.Position.ManhattanDistance(destination));
        }

        private SkillEvaluation EvaluateSkill(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target)
        {
            switch (enemyUnit.ActiveSkill)
            {
                case ActiveSkillType.RoyalAid:
                case ActiveSkillType.ImperialAid:
                    return EvaluateRoyalAid(target);
                case ActiveSkillType.GuardOrder:
                    return EvaluateGuardOrder(context, enemyUnit, target);
                case ActiveSkillType.PowerStrike:
                    return EvaluatePowerStrike(context, enemyUnit, destination, target);
                case ActiveSkillType.PinningShot:
                    return EvaluatePinningShot(context, enemyUnit, destination, target);
                case ActiveSkillType.Volley:
                case ActiveSkillType.SkyVolley:
                    return EvaluateVolley(context, enemyUnit, destination, target);
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.AzureDragonSlash:
                    return EvaluateGreenDragonSlash(context, enemyUnit, destination, target);
                case ActiveSkillType.WarCry:
                case ActiveSkillType.LionWarCry:
                    return EvaluateWarCry(context, enemyUnit, destination);
                default:
                    return new SkillEvaluation(0f, new HashSet<string>());
            }
        }

        private static SkillEvaluation EvaluateRoyalAid(UnitRuntimeState target)
        {
            int healedAmount = BattlePreviewCalculator.EstimateHealing(target, ActiveSkillRules.GetRoyalAidAmount());
            float reward = (healedAmount * HealingWeight) + (target.HasStatus(StatusEffectType.Inspired) ? 4f : 10f);
            if (target.CurrentHp <= target.MaxHp / 3)
            {
                reward += 8f;
            }

            return new SkillEvaluation(reward - 3f, new HashSet<string>());
        }

        private static SkillEvaluation EvaluateGuardOrder(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            UnitRuntimeState target)
        {
            IReadOnlyList<UnitRuntimeState> affectedUnits = context.GetUnits(enemyUnit.Faction)
                .Where(unit => unit.Id == target.Id || unit.Position.ManhattanDistance(target.Position) == 1)
                .OrderBy(unit => unit.Id == target.Id ? 0 : 1)
                .ThenBy(unit => unit.Id)
                .ToList();

            int healedAmount = BattlePreviewCalculator.EstimateHealing(target, ActiveSkillRules.GetGuardOrderHealAmount());
            int freshGuardApplications = affectedUnits.Count(unit => !unit.HasStatus(StatusEffectType.Guarded));
            float reward = (healedAmount * HealingWeight) +
                           (freshGuardApplications * (StatusBonus + 2f)) +
                           ((affectedUnits.Count - 1) * 2f) -
                           2f;
            if (target.CurrentHp <= target.MaxHp / 2)
            {
                reward += 5f;
            }

            if (healedAmount == 0 && freshGuardApplications == 0)
            {
                reward -= 10f;
            }

            return new SkillEvaluation(reward, new HashSet<string>());
        }

        private static SkillEvaluation EvaluatePowerStrike(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target)
        {
            int rawDamage = BattlePreviewCalculator.EstimateAttackDamage(
                context,
                enemyUnit,
                destination,
                target,
                ActiveSkillRules.GetPowerStrikeBonus());
            bool targetDies = rawDamage >= target.CurrentHp;
            int realizedDamage = rawDamage > target.CurrentHp ? target.CurrentHp : rawDamage;
            float reward = 5f + (realizedDamage * SkillDamageWeight) + (targetDies ? KillBonus : 0f);
            if (!targetDies && !target.HasStatus(StatusEffectType.ShatteredArmor))
            {
                reward += StatusBonus;
            }

            HashSet<string> defeatedUnitIds = targetDies ? new HashSet<string> { target.Id } : new HashSet<string>();
            return new SkillEvaluation(reward - SkillCommitmentPenalty, defeatedUnitIds);
        }

        private static SkillEvaluation EvaluatePinningShot(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target)
        {
            int rawDamage = BattlePreviewCalculator.EstimateAttackDamage(
                context,
                enemyUnit,
                destination,
                target,
                ActiveSkillRules.GetPinningShotBonus());
            bool targetDies = rawDamage >= target.CurrentHp;
            int realizedDamage = rawDamage > target.CurrentHp ? target.CurrentHp : rawDamage;
            float reward = 4f + (realizedDamage * SkillDamageWeight) + (targetDies ? KillBonus : 0f);
            if (!targetDies && !target.HasStatus(StatusEffectType.Rooted))
            {
                reward += StatusBonus + 2f;
            }

            HashSet<string> defeatedUnitIds = targetDies ? new HashSet<string> { target.Id } : new HashSet<string>();
            return new SkillEvaluation(reward - SkillCommitmentPenalty, defeatedUnitIds);
        }

        private static SkillEvaluation EvaluateVolley(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState primaryTarget)
        {
            IReadOnlyList<UnitRuntimeState> affectedUnits = BattlePreviewCalculator.GetVolleyTargets(context, primaryTarget);
            int totalDamage = 0;
            int defeatedCount = 0;
            int statusApplications = 0;
            HashSet<string> defeatedUnitIds = new HashSet<string>();

            foreach (UnitRuntimeState affectedUnit in affectedUnits)
            {
                int rawDamage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    enemyUnit,
                    destination,
                    affectedUnit,
                    ActiveSkillRules.GetVolleyBonus());
                int realizedDamage = rawDamage > affectedUnit.CurrentHp ? affectedUnit.CurrentHp : rawDamage;
                totalDamage += realizedDamage;

                if (rawDamage >= affectedUnit.CurrentHp)
                {
                    defeatedCount++;
                    defeatedUnitIds.Add(affectedUnit.Id);
                }
                else if (!affectedUnit.HasStatus(StatusEffectType.ShatteredArmor))
                {
                    statusApplications++;
                }
            }

            float reward = 4f +
                           (totalDamage * DamageWeight) +
                           (defeatedCount * KillBonus) +
                           (statusApplications * StatusBonus) +
                           ((affectedUnits.Count - 1) * AdditionalTargetBonus) -
                           SkillCommitmentPenalty;
            return new SkillEvaluation(reward, defeatedUnitIds);
        }

        private static SkillEvaluation EvaluateGreenDragonSlash(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState primaryTarget)
        {
            IReadOnlyList<UnitRuntimeState> affectedUnits = BattlePreviewCalculator.GetGreenDragonSlashTargets(
                context,
                destination,
                primaryTarget);
            int totalDamage = 0;
            int defeatedCount = 0;
            HashSet<string> defeatedUnitIds = new HashSet<string>();

            foreach (UnitRuntimeState affectedUnit in affectedUnits)
            {
                int rawDamage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    enemyUnit,
                    destination,
                    affectedUnit,
                    ActiveSkillRules.GetGreenDragonSlashBonus());
                int realizedDamage = rawDamage > affectedUnit.CurrentHp ? affectedUnit.CurrentHp : rawDamage;
                totalDamage += realizedDamage;

                if (rawDamage >= affectedUnit.CurrentHp)
                {
                    defeatedCount++;
                    defeatedUnitIds.Add(affectedUnit.Id);
                }
            }

            float reward = 6f +
                           (totalDamage * SkillDamageWeight) +
                           (defeatedCount * KillBonus) +
                           ((affectedUnits.Count - 1) * AdditionalTargetBonus) -
                           SkillCommitmentPenalty;
            return new SkillEvaluation(reward, defeatedUnitIds);
        }

        private static SkillEvaluation EvaluateWarCry(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination)
        {
            IReadOnlyList<UnitRuntimeState> affectedUnits = context.GetUnits(UnitFaction.Player)
                .Where(unit => destination.ManhattanDistance(unit.Position) <= ActiveSkillRules.GetRange(enemyUnit))
                .OrderBy(unit => destination.ManhattanDistance(unit.Position))
                .ThenBy(unit => unit.Id)
                .ToList();

            int freshApplications = affectedUnits.Count(unit => !unit.HasStatus(StatusEffectType.Intimidated));
            int lethalThreats = affectedUnits.Count(unit =>
                BattlePreviewCalculator.EstimateAttackDamage(context, unit, unit.Position, enemyUnit) >= enemyUnit.CurrentHp);

            float reward = (freshApplications * StatusBonus) + (affectedUnits.Count * 3f) + (lethalThreats * 12f) - SkillCommitmentPenalty;
            if (affectedUnits.Count < 2 && lethalThreats == 0)
            {
                reward -= 12f;
            }

            return new SkillEvaluation(reward, new HashSet<string>());
        }

        private float EstimateExposure(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            ISet<string> defeatedUnitIds)
        {
            UnitFaction opposingFaction = enemyUnit.Faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player;
            float totalThreatDamage = 0f;
            int threateningUnits = 0;

            foreach (UnitRuntimeState opposingUnit in context.GetUnits(opposingFaction))
            {
                if (defeatedUnitIds != null && defeatedUnitIds.Contains(opposingUnit.Id))
                {
                    continue;
                }

                int threatDamage = EstimateThreatDamage(context, opposingUnit, enemyUnit, destination, defeatedUnitIds);
                if (threatDamage <= 0)
                {
                    continue;
                }

                threateningUnits++;
                totalThreatDamage += threatDamage;
            }

            float risk = (totalThreatDamage * ThreatDamageWeight) + (threateningUnits * ThreatUnitPenalty);
            if (totalThreatDamage >= enemyUnit.CurrentHp)
            {
                risk += LethalThreatPenalty;
            }

            return risk;
        }

        private int EstimateThreatDamage(
            BattleContext context,
            UnitRuntimeState opposingUnit,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            ISet<string> defeatedUnitIds)
        {
            int bestDamage = 0;
            IReadOnlyList<GridPosition> moveOrigins = GetProjectedMoveRange(context, opposingUnit, enemyUnit.Position, destination, defeatedUnitIds);
            foreach (GridPosition origin in moveOrigins)
            {
                int distance = origin.ManhattanDistance(destination);
                int attackRange = PassiveSkillRules.GetAttackRange(opposingUnit);
                if (distance > 0 && distance <= attackRange)
                {
                    int attackDamage = BattlePreviewCalculator.EstimateAttackDamage(context, opposingUnit, origin, enemyUnit);
                    if (attackDamage > bestDamage)
                    {
                        bestDamage = attackDamage;
                    }
                }

                if (!opposingUnit.CanUseSkill || !ActiveSkillRules.IsOffensiveSkill(opposingUnit.ActiveSkill))
                {
                    continue;
                }

                int skillRange = ActiveSkillRules.GetRange(opposingUnit);
                if (distance <= 0 || distance > skillRange)
                {
                    continue;
                }

                int bonusDamage;
                switch (opposingUnit.ActiveSkill)
                {
                    case ActiveSkillType.PowerStrike:
                        bonusDamage = ActiveSkillRules.GetPowerStrikeBonus();
                        break;
                    case ActiveSkillType.PinningShot:
                        bonusDamage = ActiveSkillRules.GetPinningShotBonus();
                        break;
                    case ActiveSkillType.GreenDragonSlash:
                        bonusDamage = ActiveSkillRules.GetGreenDragonSlashBonus();
                        break;
                    case ActiveSkillType.AzureDragonSlash:
                        bonusDamage = ActiveSkillRules.GetAzureDragonSlashBonus();
                        break;
                    case ActiveSkillType.SkyVolley:
                        bonusDamage = ActiveSkillRules.GetSkyVolleyBonus();
                        break;
                    default:
                        bonusDamage = ActiveSkillRules.GetVolleyBonus();
                        break;
                }

                int skillDamage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    opposingUnit,
                    origin,
                    enemyUnit,
                    bonusDamage);
                if (skillDamage > bestDamage)
                {
                    bestDamage = skillDamage;
                }
            }

            return bestDamage;
        }

        private static float CalculatePressureScore(int nearestOpponentDistance)
        {
            return nearestOpponentDistance < 0
                ? PressureBaseScore
                : PressureBaseScore - (nearestOpponentDistance * PressureDistancePenalty);
        }

        private static float GetRiskMultiplier(AiProfileType aiProfile)
        {
            switch (aiProfile)
            {
                case AiProfileType.Aggressor:
                    return 0.9f;
                case AiProfileType.Protector:
                    return 1.1f;
                case AiProfileType.Support:
                    return 1.2f;
                case AiProfileType.Boss:
                    return 0.82f;
                default:
                    return 1f;
            }
        }

        private static float GetActionBias(AiProfileType aiProfile, bool skillAction, bool lethal)
        {
            switch (aiProfile)
            {
                case AiProfileType.Aggressor:
                    return skillAction ? 4f : lethal ? 6f : 2f;
                case AiProfileType.Protector:
                    return skillAction ? 2f : lethal ? 1f : 0f;
                case AiProfileType.Support:
                    return skillAction ? 6f : -1f;
                case AiProfileType.Boss:
                    return skillAction ? 5f : 4f;
                default:
                    return 0f;
            }
        }

        private static float GetProfilePressureBonus(BattleContext context, UnitRuntimeState enemyUnit, GridPosition destination)
        {
            switch (enemyUnit.AiProfile)
            {
                case AiProfileType.Aggressor:
                    return 4f;
                case AiProfileType.Protector:
                    return context.GetUnits(enemyUnit.Faction)
                        .Count(ally => ally.Id != enemyUnit.Id && ally.Position.ManhattanDistance(destination) <= 2) * 1.5f;
                case AiProfileType.Support:
                    return context.GetUnits(enemyUnit.Faction)
                        .Count(ally => ally.Id != enemyUnit.Id && ally.CurrentHp < ally.MaxHp) * 2f;
                case AiProfileType.Boss:
                    return 6f;
                default:
                    return 0f;
            }
        }

        private static int CalculateNearestOpponentDistance(
            BattleContext context,
            UnitFaction faction,
            GridPosition destination,
            ISet<string> defeatedUnitIds)
        {
            UnitFaction opposingFaction = faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player;
            int nearestDistance = -1;
            foreach (UnitRuntimeState opposingUnit in context.GetUnits(opposingFaction))
            {
                if (defeatedUnitIds != null && defeatedUnitIds.Contains(opposingUnit.Id))
                {
                    continue;
                }

                int distance = destination.ManhattanDistance(opposingUnit.Position);
                if (nearestDistance < 0 || distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }

            return nearestDistance;
        }

        private static IReadOnlyList<GridPosition> GetProjectedMoveRange(
            BattleContext context,
            UnitRuntimeState unit,
            GridPosition releasedPosition,
            GridPosition occupiedPosition,
            ISet<string> defeatedUnitIds)
        {
            int moveRange = PassiveSkillRules.GetMoveRange(unit);
            Dictionary<GridPosition, int> distances = new Dictionary<GridPosition, int>();
            Queue<GridPosition> frontier = new Queue<GridPosition>();
            frontier.Enqueue(unit.Position);
            distances[unit.Position] = 0;

            while (frontier.Count > 0)
            {
                GridPosition current = frontier.Dequeue();
                int nextDistance = distances[current] + 1;
                if (nextDistance > moveRange)
                {
                    continue;
                }

                foreach (GridPosition neighbor in current.GetOrthogonalNeighbors())
                {
                    if (!context.IsInside(neighbor) || distances.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    if (context.GetCell(neighbor).IsBlocked)
                    {
                        continue;
                    }

                    if (IsProjectedOccupied(context, unit, neighbor, releasedPosition, occupiedPosition, defeatedUnitIds))
                    {
                        continue;
                    }

                    distances[neighbor] = nextDistance;
                    frontier.Enqueue(neighbor);
                }
            }

            return distances.Keys
                .OrderBy(position => position.Y)
                .ThenBy(position => position.X)
                .ToList();
        }

        private static bool IsProjectedOccupied(
            BattleContext context,
            UnitRuntimeState movingUnit,
            GridPosition position,
            GridPosition releasedPosition,
            GridPosition occupiedPosition,
            ISet<string> defeatedUnitIds)
        {
            if (position == movingUnit.Position)
            {
                return false;
            }

            if (position == occupiedPosition)
            {
                return true;
            }

            if (releasedPosition != occupiedPosition && position == releasedPosition)
            {
                return false;
            }

            UnitRuntimeState occupant = context.GetUnitAt(position);
            if (occupant == null)
            {
                return false;
            }

            if (defeatedUnitIds != null && defeatedUnitIds.Contains(occupant.Id))
            {
                return false;
            }

            return occupant.Id != movingUnit.Id;
        }

        private sealed class SkillEvaluation
        {
            public SkillEvaluation(float reward, ISet<string> defeatedUnitIds)
            {
                Reward = reward;
                DefeatedUnitIds = defeatedUnitIds ?? new HashSet<string>();
            }

            public float Reward { get; }

            public ISet<string> DefeatedUnitIds { get; }
        }

        private sealed class AiCandidate
        {
            public AiCandidate(
                GridPosition destination,
                AiActionType actionType,
                string targetUnitId,
                float score,
                float risk,
                int targetHp,
                int nearestOpponentDistance,
                int moveDistance)
            {
                Destination = destination;
                ActionType = actionType;
                TargetUnitId = targetUnitId;
                Score = score;
                Risk = risk;
                TargetHp = targetHp;
                NearestOpponentDistance = nearestOpponentDistance;
                MoveDistance = moveDistance;
            }

            public GridPosition Destination { get; }

            public AiActionType ActionType { get; }

            public string TargetUnitId { get; }

            public float Score { get; }

            public float Risk { get; }

            public int TargetHp { get; }

            public int NearestOpponentDistance { get; }

            public int MoveDistance { get; }

            public int ActionPriority
            {
                get
                {
                    switch (ActionType)
                    {
                        case AiActionType.Attack:
                            return 2;
                        case AiActionType.Skill:
                            return 1;
                        default:
                            return 0;
                    }
                }
            }
        }
    }
}
