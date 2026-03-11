using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    public sealed class ScenarioEvaluationResult
    {
        public ScenarioEvaluationResult(
            IReadOnlyList<string> spawnedUnitIds,
            bool objectiveChanged,
            bool battleOutcomeChanged,
            bool dialogueQueued,
            bool battlefieldChanged)
        {
            SpawnedUnitIds = spawnedUnitIds ?? new List<string>();
            ObjectiveChanged = objectiveChanged;
            BattleOutcomeChanged = battleOutcomeChanged;
            DialogueQueued = dialogueQueued;
            BattlefieldChanged = battlefieldChanged;
        }

        public IReadOnlyList<string> SpawnedUnitIds { get; }

        public bool ObjectiveChanged { get; }

        public bool BattleOutcomeChanged { get; }

        public bool DialogueQueued { get; }

        public bool BattlefieldChanged { get; }
    }
}
