using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    public sealed class BattleScenarioData
    {
        public BattleScenarioData(
            string scenarioId,
            string scenarioName,
            string scenarioNameKey,
            StageDefinitionData stage,
            IReadOnlyList<ScenarioTrigger> triggers,
            int recommendedLevel = 1,
            int victoryExpReward = 45,
            int defeatExpReward = 20,
            RewardBundle rewardBundle = null,
            int replayDifficultyTier = 0,
            string scenarioVariantTag = "")
        {
            ScenarioId = scenarioId;
            ScenarioName = scenarioName;
            ScenarioNameKey = scenarioNameKey;
            Stage = stage;
            Triggers = triggers ?? new List<ScenarioTrigger>();
            RecommendedLevel = recommendedLevel < 1 ? 1 : recommendedLevel;
            VictoryExpReward = victoryExpReward < 0 ? 0 : victoryExpReward;
            DefeatExpReward = defeatExpReward < 0 ? 0 : defeatExpReward;
            RewardBundle = rewardBundle ?? new RewardBundle(0, 0);
            ReplayDifficultyTier = replayDifficultyTier < 0 ? 0 : replayDifficultyTier;
            ScenarioVariantTag = scenarioVariantTag ?? string.Empty;
        }

        public string ScenarioId { get; }

        public string ScenarioName { get; }

        public string ScenarioNameKey { get; }

        public StageDefinitionData Stage { get; }

        public IReadOnlyList<ScenarioTrigger> Triggers { get; }

        public int RecommendedLevel { get; }

        public int VictoryExpReward { get; }

        public int DefeatExpReward { get; }

        public RewardBundle RewardBundle { get; }

        public int ReplayDifficultyTier { get; }

        public string ScenarioVariantTag { get; }
    }
}
