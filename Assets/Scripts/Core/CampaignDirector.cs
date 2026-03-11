using System;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class CampaignDirector
    {
        public CampaignDirector(CampaignDefinition definition, CampaignProgress progress = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Progress = progress ?? new CampaignProgress();
        }

        public CampaignDefinition Definition { get; }

        public CampaignProgress Progress { get; }

        public int StageCount => Definition.Stages.Count;

        public CampaignStageDefinition GetStage(int stageIndex)
        {
            return stageIndex >= 0 && stageIndex < Definition.Stages.Count
                ? Definition.Stages[stageIndex]
                : null;
        }

        public int GetStageIndex(string scenarioId)
        {
            if (string.IsNullOrEmpty(scenarioId))
            {
                return -1;
            }

            for (int index = 0; index < Definition.Stages.Count; index++)
            {
                if (Definition.Stages[index].ScenarioId == scenarioId)
                {
                    return index;
                }
            }

            return -1;
        }

        public bool IsStageUnlocked(int stageIndex)
        {
            return stageIndex >= 0 &&
                   stageIndex < Definition.Stages.Count &&
                   stageIndex <= Progress.UnlockedStageIndex;
        }

        public bool IsStageCleared(int stageIndex)
        {
            CampaignStageDefinition stage = GetStage(stageIndex);
            return stage != null && Progress.IsCleared(stage.ScenarioId);
        }

        public int GetRecommendedStageIndex()
        {
            for (int index = 0; index <= Progress.UnlockedStageIndex && index < Definition.Stages.Count; index++)
            {
                if (!IsStageCleared(index))
                {
                    return index;
                }
            }

            return Math.Min(Progress.UnlockedStageIndex, Math.Max(0, Definition.Stages.Count - 1));
        }

        public int GetNextStageIndex(string scenarioId)
        {
            int currentIndex = GetStageIndex(scenarioId);
            int nextIndex = currentIndex + 1;
            return nextIndex >= 0 && nextIndex < Definition.Stages.Count ? nextIndex : -1;
        }

        public void RecordBattleResult(BattleResultSummary summary)
        {
            if (summary == null)
            {
                return;
            }

            Progress.SetLastBattleResult(summary);
            if (summary.WinningSide != TurnSide.Player)
            {
                return;
            }

            Progress.MarkCleared(summary.ScenarioId);

            int stageIndex = GetStageIndex(summary.ScenarioId);
            if (stageIndex < 0)
            {
                return;
            }

            Progress.UnlockThrough(Math.Min(stageIndex + 1, Math.Max(0, Definition.Stages.Count - 1)));
        }
    }
}
