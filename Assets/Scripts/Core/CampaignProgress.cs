using System;
using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    [Serializable]
    public sealed class CampaignProgress
    {
        private readonly HashSet<string> clearedScenarioIds;
        private readonly HashSet<string> claimedRewardScenarioIds;

        public CampaignProgress(
            int unlockedStageIndex = 0,
            IReadOnlyList<string> clearedScenarioIds = null,
            BattleResultSummary lastBattleResult = null,
            IReadOnlyList<string> claimedRewardScenarioIds = null)
        {
            UnlockedStageIndex = Math.Max(0, unlockedStageIndex);
            this.clearedScenarioIds = new HashSet<string>(clearedScenarioIds ?? Array.Empty<string>());
            this.claimedRewardScenarioIds = new HashSet<string>(claimedRewardScenarioIds ?? Array.Empty<string>());
            LastBattleResult = lastBattleResult;
        }

        public int UnlockedStageIndex { get; private set; }

        public IReadOnlyList<string> ClearedScenarioIds => clearedScenarioIds.OrderBy(id => id).ToList();

        public IReadOnlyList<string> ClaimedRewardScenarioIds => claimedRewardScenarioIds.OrderBy(id => id).ToList();

        public BattleResultSummary LastBattleResult { get; private set; }

        public bool IsCleared(string scenarioId)
        {
            return !string.IsNullOrEmpty(scenarioId) && clearedScenarioIds.Contains(scenarioId);
        }

        public void UnlockThrough(int stageIndex)
        {
            UnlockedStageIndex = Math.Max(UnlockedStageIndex, Math.Max(0, stageIndex));
        }

        public void MarkCleared(string scenarioId)
        {
            if (string.IsNullOrEmpty(scenarioId))
            {
                return;
            }

            clearedScenarioIds.Add(scenarioId);
        }

        public bool IsRewardClaimed(string scenarioId)
        {
            return !string.IsNullOrEmpty(scenarioId) && claimedRewardScenarioIds.Contains(scenarioId);
        }

        public void MarkRewardClaimed(string scenarioId)
        {
            if (string.IsNullOrEmpty(scenarioId))
            {
                return;
            }

            claimedRewardScenarioIds.Add(scenarioId);
        }

        public void SetLastBattleResult(BattleResultSummary summary)
        {
            LastBattleResult = summary;
        }
    }
}
