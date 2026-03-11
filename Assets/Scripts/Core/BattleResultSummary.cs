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
            IReadOnlyList<string> survivingUnitIds)
        {
            ScenarioId = scenarioId ?? string.Empty;
            WinningSide = winningSide;
            RoundCount = roundCount;
            SurvivingUnitIds = survivingUnitIds ?? Array.Empty<string>();
        }

        public string ScenarioId { get; }

        public TurnSide WinningSide { get; }

        public int RoundCount { get; }

        public IReadOnlyList<string> SurvivingUnitIds { get; }
    }
}
