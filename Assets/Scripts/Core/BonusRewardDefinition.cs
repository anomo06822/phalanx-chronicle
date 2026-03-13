using System;
using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public enum BonusRewardProgressState
    {
        InProgress,
        Completed,
        Failed,
    }

    public sealed class BonusRewardDefinition
    {
        public BonusRewardDefinition(
            string rewardId,
            string rewardItemId,
            string objectiveKey,
            string objectiveFallback,
            string summaryKey,
            string summaryFallback,
            IReadOnlyList<string> requiredFlags = null,
            IReadOnlyList<string> excludedFlags = null,
            int? maxRoundNumber = null,
            IReadOnlyList<string> requiredAliveUnitIds = null,
            IReadOnlyList<string> requiredTriggeredDuelIds = null)
        {
            RewardId = rewardId ?? string.Empty;
            RewardItemId = rewardItemId ?? string.Empty;
            ObjectiveKey = objectiveKey ?? string.Empty;
            ObjectiveFallback = objectiveFallback ?? string.Empty;
            SummaryKey = summaryKey ?? string.Empty;
            SummaryFallback = summaryFallback ?? string.Empty;
            RequiredFlags = requiredFlags ?? Array.Empty<string>();
            ExcludedFlags = excludedFlags ?? Array.Empty<string>();
            MaxRoundNumber = maxRoundNumber;
            RequiredAliveUnitIds = requiredAliveUnitIds ?? Array.Empty<string>();
            RequiredTriggeredDuelIds = requiredTriggeredDuelIds ?? Array.Empty<string>();
        }

        public string RewardId { get; }

        public string RewardItemId { get; }

        public string ObjectiveKey { get; }

        public string ObjectiveFallback { get; }

        public string SummaryKey { get; }

        public string SummaryFallback { get; }

        public IReadOnlyList<string> RequiredFlags { get; }

        public IReadOnlyList<string> ExcludedFlags { get; }

        public int? MaxRoundNumber { get; }

        public IReadOnlyList<string> RequiredAliveUnitIds { get; }

        public IReadOnlyList<string> RequiredTriggeredDuelIds { get; }

        public bool IsSatisfied(BattleResultSummary summary)
        {
            if (summary == null)
            {
                return false;
            }

            HashSet<string> flags = new HashSet<string>(summary.AchievedScenarioFlags ?? Array.Empty<string>(), StringComparer.Ordinal);
            HashSet<string> survivingUnits = new HashSet<string>(summary.SurvivingUnitIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            HashSet<string> triggeredDuels = new HashSet<string>(summary.TriggeredDuelIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            return Matches(flags, survivingUnits, triggeredDuels, summary.RoundCount);
        }

        public BonusRewardProgressState EvaluateProgress(BattleContext context, IReadOnlyList<DuelSceneDefinition> duelScenes, ISet<string> activeFlags, ISet<string> triggeredDuelIds)
        {
            if (context == null)
            {
                return BonusRewardProgressState.InProgress;
            }

            HashSet<string> survivingUnits = context.GetUnits(UnitFaction.Player, false)
                .Where(unit => unit.IsAlive)
                .Select(unit => unit.Id)
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string> flags = activeFlags != null
                ? new HashSet<string>(activeFlags, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> duels = triggeredDuelIds != null
                ? new HashSet<string>(triggeredDuelIds, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            if (Matches(flags, survivingUnits, duels, context.RoundNumber))
            {
                return BonusRewardProgressState.Completed;
            }

            if (ExcludedFlags.Any(flags.Contains) ||
                RequiredAliveUnitIds.Any(unitId =>
                {
                    UnitRuntimeState unit = context.GetUnit(unitId);
                    return unit == null || !unit.IsAlive;
                }) ||
                (MaxRoundNumber.HasValue && context.RoundNumber > MaxRoundNumber.Value))
            {
                return BonusRewardProgressState.Failed;
            }

            if (RequiredTriggeredDuelIds.Any())
            {
                Dictionary<string, DuelSceneDefinition> duelLookup = (duelScenes ?? Array.Empty<DuelSceneDefinition>())
                    .Where(scene => scene != null && !string.IsNullOrWhiteSpace(scene.DuelId))
                    .GroupBy(scene => scene.DuelId, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

                foreach (string duelId in RequiredTriggeredDuelIds)
                {
                    if (string.IsNullOrWhiteSpace(duelId) || duels.Contains(duelId))
                    {
                        continue;
                    }

                    if (!duelLookup.TryGetValue(duelId, out DuelSceneDefinition duelScene))
                    {
                        continue;
                    }

                    UnitRuntimeState attacker = context.GetUnit(duelScene.AttackerUnitId);
                    UnitRuntimeState defender = context.GetUnit(duelScene.DefenderUnitId);
                    if (attacker == null || !attacker.IsAlive || defender == null || !defender.IsAlive)
                    {
                        return BonusRewardProgressState.Failed;
                    }
                }
            }

            return BonusRewardProgressState.InProgress;
        }

        private bool Matches(ISet<string> flags, ISet<string> survivingUnits, ISet<string> triggeredDuels, int roundCount)
        {
            if (RequiredFlags.Any(flag => string.IsNullOrWhiteSpace(flag) || flags == null || !flags.Contains(flag)))
            {
                return false;
            }

            if (ExcludedFlags.Any(flag => !string.IsNullOrWhiteSpace(flag) && flags != null && flags.Contains(flag)))
            {
                return false;
            }

            if (MaxRoundNumber.HasValue && roundCount > MaxRoundNumber.Value)
            {
                return false;
            }

            if (RequiredAliveUnitIds.Any(unitId => string.IsNullOrWhiteSpace(unitId) || survivingUnits == null || !survivingUnits.Contains(unitId)))
            {
                return false;
            }

            if (RequiredTriggeredDuelIds.Any(duelId => string.IsNullOrWhiteSpace(duelId) || triggeredDuels == null || !triggeredDuels.Contains(duelId)))
            {
                return false;
            }

            return true;
        }
    }
}
