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
            IReadOnlyList<ScenarioTrigger> triggers)
        {
            ScenarioId = scenarioId;
            ScenarioName = scenarioName;
            ScenarioNameKey = scenarioNameKey;
            Stage = stage;
            Triggers = triggers ?? new List<ScenarioTrigger>();
        }

        public string ScenarioId { get; }

        public string ScenarioName { get; }

        public string ScenarioNameKey { get; }

        public StageDefinitionData Stage { get; }

        public IReadOnlyList<ScenarioTrigger> Triggers { get; }
    }
}
