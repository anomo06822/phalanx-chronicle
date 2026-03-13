using System;
using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    [Serializable]
    public sealed class BattleResultSummary
    {
        public BattleResultSummary(
            string scenarioId,
            TurnSide winningSide,
            int roundCount,
            IReadOnlyList<string> survivingUnitIds,
            IReadOnlyList<string> achievedScenarioFlags = null,
            IReadOnlyList<string> triggeredDuelIds = null)
        {
            ScenarioId = scenarioId ?? string.Empty;
            WinningSide = winningSide;
            RoundCount = roundCount;
            SurvivingUnitIds = survivingUnitIds ?? Array.Empty<string>();
            AchievedScenarioFlags = achievedScenarioFlags ?? Array.Empty<string>();
            TriggeredDuelIds = triggeredDuelIds ?? Array.Empty<string>();
        }

        public string ScenarioId { get; }

        public TurnSide WinningSide { get; }

        public int RoundCount { get; }

        public IReadOnlyList<string> SurvivingUnitIds { get; }

        public IReadOnlyList<string> AchievedScenarioFlags { get; }

        public IReadOnlyList<string> TriggeredDuelIds { get; }
    }
}
