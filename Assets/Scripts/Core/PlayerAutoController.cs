using System;
using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    internal sealed class PlayerAutoController
    {
        private const float AttackDamageWeight = 4f;
        private const float SkillDamageWeight = 4.6f;
        private const float HealingWeight = 3.5f;
        private const float KillBonus = 32f;
        private const float StatusBonus = 6f;
        private const float AdditionalTargetBonus = 8f;
        private const float WoundedAllyBonus = 10f;
        private const float MovePressureBase = 16f;
        private const float MoveDistancePenalty = 2.5f;
        private const float ThreatDamageWeight = 0.9f;
        private const float ThreatUnitPenalty = 2.5f;
        private const float LethalThreatPenalty = 12f;
        private const float IdlePenalty = 10f;
        private const float AllyFormationBonus = 2.5f;

        public AiDecision Decide(BattleSimulation simulation, UnitRuntimeState playerUnit)
        {
            PlayerAutoCandidate bestCandidate = BuildBestCandidate(simulation, playerUnit);
            if (bestCandidate == null)
            {
                GridPosition fallback = playerUnit != null ? playerUnit.Position : new GridPosition(0, 0);
                return new AiDecision(fallback, AiActionType.None, null);
            }

            return new AiDecision(bestCandidate.Destination, bestCandidate.ActionType, bestCandidate.TargetUnitId);
        }

        public IReadOnlyList<string> BuildTurnOrder(BattleSimulation simulation)
        {
            if (simulation == null || simulation.Context == null || simulation.Context.CurrentTurnSide != TurnSide.Player)
            {
                return Array.Empty<string>();
            }

            return simulation.Context.GetUnits(UnitFaction.Player)
                .Where(unit => unit.IsAlive && !unit.HasActed)
                .Select(unit => BuildBestCandidate(simulation, unit))
                .Where(candidate => candidate != null)
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Risk)
                .ThenByDescending(candidate => candidate.ActionPriority)
                .ThenBy(candidate => candidate.MoveDistance)
                .ThenBy(candidate => candidate.TargetHp)
                .ThenBy(candidate => candidate.UnitId, StringComparer.Ordinal)
                .Select(candidate => candidate.UnitId)
                .ToList();
        }

        private PlayerAutoCandidate BuildBestCandidate(BattleSimulation simulation, UnitRuntimeState playerUnit)
        {
            if (simulation == null ||
                simulation.Context == null ||
                playerUnit == null ||
                !playerUnit.IsAlive ||
                playerUnit.HasActed ||
                playerUnit.Faction != UnitFaction.Player ||
                simulation.Context.CurrentTurnSide != TurnSide.Player)
            {
                return null;
            }

            List<PlayerAutoCandidate> candidates = new List<PlayerAutoCandidate>();
            foreach (GridPosition destination in GetReachableDestinations(simulation, playerUnit))
            {
                candidates.Add(BuildMoveCandidate(simulation, playerUnit, destination));
                candidates.AddRange(BuildAttackCandidates(simulation, playerUnit, destination));
                candidates.AddRange(BuildSkillCandidates(simulation, playerUnit, destination));
            }

            return candidates
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Risk)
                .ThenByDescending(candidate => candidate.ActionPriority)
                .ThenBy(candidate => candidate.TargetHp)
                .ThenBy(candidate => candidate.MoveDistance)
                .ThenBy(candidate => candidate.Destination.Y)
                .ThenBy(candidate => candidate.Destination.X)
                .FirstOrDefault();
        }

        private IEnumerable<GridPosition> GetReachableDestinations(BattleSimulation simulation, UnitRuntimeState playerUnit)
        {
            HashSet<GridPosition> destinations = new HashSet<GridPosition>(simulation.GetMoveRange(playerUnit.Id))
            {
                playerUnit.Position,
            };

            return destinations
                .OrderBy(position => position.Y)
                .ThenBy(position => position.X);
        }

        private PlayerAutoCandidate BuildMoveCandidate(BattleSimulation simulation, UnitRuntimeState playerUnit, GridPosition destination)
        {
            BattleContext context = simulation.Context;
            UnitFaction opposingFaction = playerUnit.Faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player;
            IReadOnlyList<UnitRuntimeState> opposingUnits = context.GetUnits(opposingFaction);
            IReadOnlyList<UnitRuntimeState> alliedUnits = context.GetUnits(playerUnit.Faction);

            BattleThreatProjection threat = BattleThreatAnalyzer.AnalyzeProjected(context, playerUnit, destination);
            float risk = CalculateRisk(playerUnit, threat, 0);
            float score = -risk;

            if (ActiveSkillRules.IsSupportSkill(playerUnit.ActiveSkill))
            {
                UnitRuntimeState woundedAlly = alliedUnits
                    .Where(unit => unit.IsAlive && unit.Id != playerUnit.Id)
                    .OrderBy(unit => unit.CurrentHp)
                    .ThenBy(unit => unit.Id, StringComparer.Ordinal)
                    .FirstOrDefault();
                int allyDistance = woundedAlly != null
                    ? destination.ManhattanDistance(woundedAlly.Position)
                    : 0;
                score += CalculatePressure(allyDistance);
                score += CountAdjacentAllies(alliedUnits, playerUnit.Id, destination) * AllyFormationBonus;
                if (woundedAlly != null && woundedAlly.CurrentHp <= (int)Math.Ceiling(woundedAlly.MaxHp * 0.55f))
                {
                    score += WoundedAllyBonus;
                }
            }
            else
            {
                UnitRuntimeState nearestEnemy = opposingUnits
                    .Where(unit => unit.IsAlive)
                    .OrderBy(unit => destination.ManhattanDistance(unit.Position))
                    .ThenBy(unit => unit.CurrentHp)
                    .ThenBy(unit => unit.Id, StringComparer.Ordinal)
                    .FirstOrDefault();
                int enemyDistance = nearestEnemy != null
                    ? destination.ManhattanDistance(nearestEnemy.Position)
                    : 0;
                score += CalculatePressure(enemyDistance);
                if (playerUnit.AiProfile == AiProfileType.Protector)
                {
                    score += CountAdjacentAllies(alliedUnits, playerUnit.Id, destination) * AllyFormationBonus;
                }
            }

            if (destination == playerUnit.Position)
            {
                score -= IdlePenalty;
            }

            return new PlayerAutoCandidate(
                playerUnit.Id,
                destination,
                AiActionType.None,
                null,
                score,
                risk,
                int.MaxValue,
                0,
                playerUnit.Position.ManhattanDistance(destination));
        }

        private IEnumerable<PlayerAutoCandidate> BuildAttackCandidates(BattleSimulation simulation, UnitRuntimeState playerUnit, GridPosition destination)
        {
            foreach (UnitRuntimeState target in simulation.Context.GetUnits(UnitFaction.Enemy))
            {
                if (!target.IsAlive)
                {
                    continue;
                }

                BattleIntentPreview preview = simulation.PreviewAttackIntent(playerUnit.Id, target.Id, destination);
                if (preview == null || !preview.CanCommit)
                {
                    continue;
                }

                int projectedSelfHealing = GetProjectedSelfHealing(preview, playerUnit.Id);
                float risk = CalculateRisk(playerUnit, preview.ThreatAfterAction, projectedSelfHealing);
                int killCount = preview.LethalTargetIds.Count;
                int statusCount = preview.PredictedStatuses.Count;
                float reward = (preview.PredictedDamage * AttackDamageWeight) +
                               (killCount * KillBonus) +
                               (statusCount * StatusBonus) +
                               GetTargetPriorityBonus(target);

                yield return new PlayerAutoCandidate(
                    playerUnit.Id,
                    destination,
                    AiActionType.Attack,
                    target.Id,
                    reward - risk,
                    risk,
                    target.CurrentHp,
                    2 + (killCount > 0 ? 1 : 0),
                    playerUnit.Position.ManhattanDistance(destination));
            }
        }

        private IEnumerable<PlayerAutoCandidate> BuildSkillCandidates(BattleSimulation simulation, UnitRuntimeState playerUnit, GridPosition destination)
        {
            if (!playerUnit.CanUseSkill || !playerUnit.HasEnoughMana(ActiveSkillRules.GetManaCost(playerUnit)))
            {
                yield break;
            }

            UnitFaction targetFaction = ActiveSkillRules.IsSupportSkill(playerUnit.ActiveSkill)
                ? playerUnit.Faction
                : UnitFaction.Enemy;
            foreach (UnitRuntimeState target in simulation.Context.GetUnits(targetFaction))
            {
                if (!target.IsAlive)
                {
                    continue;
                }

                BattleIntentPreview preview = simulation.PreviewSkillIntent(playerUnit.Id, target.Id, destination);
                if (preview == null || !preview.CanCommit)
                {
                    continue;
                }

                int projectedSelfHealing = GetProjectedSelfHealing(preview, playerUnit.Id);
                float risk = CalculateRisk(playerUnit, preview.ThreatAfterAction, projectedSelfHealing);
                int targetHp = target.CurrentHp;

                if (ActiveSkillRules.IsSupportSkill(playerUnit.ActiveSkill))
                {
                    int effectiveHealing = preview.Effects.Sum(effect =>
                    {
                        UnitRuntimeState affectedUnit = simulation.Context.GetUnit(effect.UnitId);
                        if (affectedUnit == null || effect.PredictedHealing <= 0)
                        {
                            return 0;
                        }

                        return Math.Min(effect.PredictedHealing, affectedUnit.MaxHp - affectedUnit.CurrentHp);
                    });
                    int woundedPriorityCount = preview.Effects.Count(effect =>
                    {
                        UnitRuntimeState affectedUnit = simulation.Context.GetUnit(effect.UnitId);
                        return affectedUnit != null &&
                               affectedUnit.CurrentHp <= (int)Math.Ceiling(affectedUnit.MaxHp * 0.55f);
                    });
                    int statusCount = preview.PredictedStatuses.Count;
                    float reward = (effectiveHealing * HealingWeight) +
                                   (woundedPriorityCount * WoundedAllyBonus) +
                                   (statusCount * StatusBonus) +
                                   ((preview.AffectedTargetIds.Count - 1) * AdditionalTargetBonus);

                    yield return new PlayerAutoCandidate(
                        playerUnit.Id,
                        destination,
                        AiActionType.Skill,
                        target.Id,
                        reward - risk,
                        risk,
                        targetHp,
                        3,
                        playerUnit.Position.ManhattanDistance(destination));
                    continue;
                }

                int killCount = preview.LethalTargetIds.Count;
                int statusBonusCount = preview.PredictedStatuses.Count;
                float offensiveReward = (preview.PredictedDamage * SkillDamageWeight) +
                                        (killCount * KillBonus) +
                                        (statusBonusCount * StatusBonus) +
                                        ((preview.AffectedTargetIds.Count - 1) * AdditionalTargetBonus) +
                                        GetTargetPriorityBonus(target);

                yield return new PlayerAutoCandidate(
                    playerUnit.Id,
                    destination,
                    AiActionType.Skill,
                    target.Id,
                    offensiveReward - risk,
                    risk,
                    targetHp,
                    4 + (killCount > 0 ? 1 : 0),
                    playerUnit.Position.ManhattanDistance(destination));
            }
        }

        private static float CalculatePressure(int distance)
        {
            return MovePressureBase - (distance * MoveDistancePenalty);
        }

        private static int CountAdjacentAllies(IEnumerable<UnitRuntimeState> alliedUnits, string actorUnitId, GridPosition destination)
        {
            if (alliedUnits == null)
            {
                return 0;
            }

            return alliedUnits.Count(unit =>
                unit.IsAlive &&
                unit.Id != actorUnitId &&
                unit.Position.ManhattanDistance(destination) == 1);
        }

        private static float GetTargetPriorityBonus(UnitRuntimeState target)
        {
            if (target == null)
            {
                return 0f;
            }

            float lowHpBonus = (target.MaxHp - target.CurrentHp) * 0.6f;
            float roleBonus = target.AiProfile == AiProfileType.Boss ? 6f : 0f;
            return lowHpBonus + roleBonus;
        }

        private static int GetProjectedSelfHealing(BattleIntentPreview preview, string actorUnitId)
        {
            if (preview == null || string.IsNullOrWhiteSpace(actorUnitId))
            {
                return 0;
            }

            BattleIntentEffectPreview effect = preview.Effects.FirstOrDefault(candidate => candidate.UnitId == actorUnitId);
            return effect != null ? effect.PredictedHealing : 0;
        }

        private static float CalculateRisk(UnitRuntimeState actor, BattleThreatProjection threat, int projectedSelfHealing)
        {
            if (actor == null || threat == null)
            {
                return 0f;
            }

            int projectedHp = actor.CurrentHp + projectedSelfHealing;
            if (projectedHp > actor.MaxHp)
            {
                projectedHp = actor.MaxHp;
            }

            float risk = (threat.MaxProjectedDamage * ThreatDamageWeight) +
                         (threat.ThreateningEnemyCount * ThreatUnitPenalty);
            if (threat.IsLethalRisk(projectedHp))
            {
                risk += LethalThreatPenalty;
            }

            return risk * GetRiskMultiplier(actor.AiProfile);
        }

        private static float GetRiskMultiplier(AiProfileType profile)
        {
            switch (profile)
            {
                case AiProfileType.Support:
                    return 1.2f;
                case AiProfileType.Protector:
                    return 1f;
                case AiProfileType.Aggressor:
                    return 0.9f;
                case AiProfileType.Boss:
                    return 0.95f;
                default:
                    return 1f;
            }
        }

        private sealed class PlayerAutoCandidate
        {
            public PlayerAutoCandidate(
                string unitId,
                GridPosition destination,
                AiActionType actionType,
                string targetUnitId,
                float score,
                float risk,
                int targetHp,
                int actionPriority,
                int moveDistance)
            {
                UnitId = unitId;
                Destination = destination;
                ActionType = actionType;
                TargetUnitId = targetUnitId;
                Score = score;
                Risk = risk;
                TargetHp = targetHp;
                ActionPriority = actionPriority;
                MoveDistance = moveDistance;
            }

            public string UnitId { get; }

            public GridPosition Destination { get; }

            public AiActionType ActionType { get; }

            public string TargetUnitId { get; }

            public float Score { get; }

            public float Risk { get; }

            public int TargetHp { get; }

            public int ActionPriority { get; }

            public int MoveDistance { get; }
        }
    }
}
