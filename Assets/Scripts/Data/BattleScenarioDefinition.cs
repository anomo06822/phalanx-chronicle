using PhalanxChronicle.Core;
using UnityEngine;

namespace PhalanxChronicle.Data
{
    [CreateAssetMenu(menuName = "Phalanx Chronicle/Battle Scenario", fileName = "BattleScenario")]
    public sealed class BattleScenarioDefinition : ScriptableObject
    {
        [SerializeField] private string scenarioId = BattleScenarioCatalog.JieqiaoFiresScenarioId;

        public BattleScenarioData ToData()
        {
            switch (scenarioId)
            {
                case BattleScenarioCatalog.JieqiaoFiresScenarioId:
                default:
                    return BattleScenarioCatalog.CreateJieqiaoFires();
            }
        }

        public static BattleScenarioDefinition CreateDefault()
        {
            BattleScenarioDefinition definition = CreateInstance<BattleScenarioDefinition>();
            definition.scenarioId = BattleScenarioCatalog.JieqiaoFiresScenarioId;
            return definition;
        }
    }
}
