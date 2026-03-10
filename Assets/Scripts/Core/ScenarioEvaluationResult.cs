using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    public sealed class ScenarioEvaluationResult
    {
        public ScenarioEvaluationResult(
            IReadOnlyList<string> spawnedUnitIds,
            bool objectiveChanged,
            bool battleOutcomeChanged,
            bool dialogueQueued)
        {
            SpawnedUnitIds = spawnedUnitIds ?? new List<string>();
            ObjectiveChanged = objectiveChanged;
            BattleOutcomeChanged = battleOutcomeChanged;
            DialogueQueued = dialogueQueued;
        }

        public IReadOnlyList<string> SpawnedUnitIds { get; }

        public bool ObjectiveChanged { get; }

        public bool BattleOutcomeChanged { get; }

        public bool DialogueQueued { get; }
    }
}
