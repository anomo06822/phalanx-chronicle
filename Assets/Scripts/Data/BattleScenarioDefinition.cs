using PhalanxChronicle.Core;
using UnityEngine;

namespace PhalanxChronicle.Data
{
    [CreateAssetMenu(menuName = "Phalanx Chronicle/Battle Scenario", fileName = "BattleScenario")]
    public sealed class BattleScenarioDefinition : ScriptableObject
    {
        [SerializeField] private string scenarioId = BattleScenarioCatalog.GuangzongScenarioId;

        public string ScenarioId => scenarioId;

        public BattleScenarioData ToData()
        {
            return BattleScenarioCatalog.CreateScenario(scenarioId);
        }

        public static BattleScenarioDefinition CreateDefault()
        {
            BattleScenarioDefinition definition = CreateInstance<BattleScenarioDefinition>();
            definition.scenarioId = BattleScenarioCatalog.GuangzongScenarioId;
            return definition;
        }

        public static BattleScenarioDefinition CreateRuntime(string id)
        {
            BattleScenarioDefinition definition = CreateInstance<BattleScenarioDefinition>();
            definition.scenarioId = string.IsNullOrEmpty(id) ? BattleScenarioCatalog.GuangzongScenarioId : id;
            return definition;
        }
    }
}
