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
        private readonly Dictionary<string, int> scenarioClearCounts;

        public CampaignProgress(
            int unlockedStageIndex = 0,
            IReadOnlyList<string> clearedScenarioIds = null,
            BattleResultSummary lastBattleResult = null,
            IReadOnlyList<string> claimedRewardScenarioIds = null,
            IReadOnlyDictionary<string, int> scenarioClearCounts = null,
            bool hasSeenFirstLaunchIntro = false,
            bool hasCompletedFirstBattleOnboarding = false,
            bool hasSkippedOnboarding = false)
        {
            UnlockedStageIndex = Math.Max(0, unlockedStageIndex);
            this.clearedScenarioIds = new HashSet<string>(clearedScenarioIds ?? Array.Empty<string>());
            this.claimedRewardScenarioIds = new HashSet<string>(claimedRewardScenarioIds ?? Array.Empty<string>());
            this.scenarioClearCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            if (scenarioClearCounts != null)
            {
                foreach (KeyValuePair<string, int> entry in scenarioClearCounts)
                {
                    if (string.IsNullOrWhiteSpace(entry.Key))
                    {
                        continue;
                    }

                    this.scenarioClearCounts[entry.Key] = Math.Max(0, entry.Value);
                }
            }

            foreach (string scenarioId in this.clearedScenarioIds)
            {
                if (!this.scenarioClearCounts.ContainsKey(scenarioId))
                {
                    this.scenarioClearCounts[scenarioId] = 1;
                }
            }

            LastBattleResult = lastBattleResult;
            HasSeenFirstLaunchIntro = hasSeenFirstLaunchIntro;
            HasCompletedFirstBattleOnboarding = hasCompletedFirstBattleOnboarding;
            HasSkippedOnboarding = hasSkippedOnboarding;
        }

        public int UnlockedStageIndex { get; private set; }

        public IReadOnlyList<string> ClearedScenarioIds => clearedScenarioIds.OrderBy(id => id).ToList();

        public IReadOnlyList<string> ClaimedRewardScenarioIds => claimedRewardScenarioIds.OrderBy(id => id).ToList();

        public IReadOnlyDictionary<string, int> ScenarioClearCounts => new Dictionary<string, int>(scenarioClearCounts);

        public BattleResultSummary LastBattleResult { get; private set; }

        public bool HasSeenFirstLaunchIntro { get; private set; }

        public bool HasCompletedFirstBattleOnboarding { get; private set; }

        public bool HasSkippedOnboarding { get; private set; }

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
            scenarioClearCounts[scenarioId] = GetClearCount(scenarioId) + 1;
        }

        public int GetClearCount(string scenarioId)
        {
            return !string.IsNullOrWhiteSpace(scenarioId) && scenarioClearCounts.TryGetValue(scenarioId, out int count)
                ? count
                : 0;
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

        public void MarkFirstLaunchIntroSeen()
        {
            HasSeenFirstLaunchIntro = true;
        }

        public void MarkFirstBattleOnboardingCompleted()
        {
            HasCompletedFirstBattleOnboarding = true;
        }

        public void MarkFirstBattleOnboardingSkipped()
        {
            HasSkippedOnboarding = true;
        }
    }
}
