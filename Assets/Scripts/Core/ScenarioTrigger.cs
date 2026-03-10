using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class ScenarioTrigger
    {
        public ScenarioTrigger(
            string id,
            ScenarioCheckpoint checkpoint,
            IReadOnlyList<ScenarioDirective> directives,
            IReadOnlyList<string> requiredDefeatedUnitIds = null,
            IReadOnlyList<string> requiredAliveUnitIds = null,
            IReadOnlyList<string> requiredFlags = null,
            IReadOnlyList<string> excludedFlags = null,
            int? minimumRoundNumber = null,
            bool requiresBattleEnded = false,
            TurnSide? requiredWinningSide = null,
            string exclusivityGroupId = null,
            bool fireOnce = true)
        {
            Id = id;
            Checkpoint = checkpoint;
            Directives = directives ?? new List<ScenarioDirective>();
            RequiredDefeatedUnitIds = requiredDefeatedUnitIds ?? new List<string>();
            RequiredAliveUnitIds = requiredAliveUnitIds ?? new List<string>();
            RequiredFlags = requiredFlags ?? new List<string>();
            ExcludedFlags = excludedFlags ?? new List<string>();
            MinimumRoundNumber = minimumRoundNumber;
            RequiresBattleEnded = requiresBattleEnded;
            RequiredWinningSide = requiredWinningSide;
            ExclusivityGroupId = exclusivityGroupId;
            FireOnce = fireOnce;
        }

        public string Id { get; }

        public ScenarioCheckpoint Checkpoint { get; }

        public IReadOnlyList<ScenarioDirective> Directives { get; }

        public IReadOnlyList<string> RequiredDefeatedUnitIds { get; }

        public IReadOnlyList<string> RequiredAliveUnitIds { get; }

        public IReadOnlyList<string> RequiredFlags { get; }

        public IReadOnlyList<string> ExcludedFlags { get; }

        public int? MinimumRoundNumber { get; }

        public bool RequiresBattleEnded { get; }

        public TurnSide? RequiredWinningSide { get; }

        public string ExclusivityGroupId { get; }

        public bool FireOnce { get; }

        public bool Matches(BattleContext context, ScenarioCheckpoint checkpoint, ISet<string> activeFlags)
        {
            if (context == null || Checkpoint != checkpoint)
            {
                return false;
            }

            if (MinimumRoundNumber.HasValue && context.RoundNumber < MinimumRoundNumber.Value)
            {
                return false;
            }

            if (RequiresBattleEnded && !context.BattleEnded)
            {
                return false;
            }

            if (RequiredWinningSide.HasValue && (!context.BattleEnded || context.WinningSide != RequiredWinningSide.Value))
            {
                return false;
            }

            if (RequiredFlags.Any(flag => activeFlags == null || !activeFlags.Contains(flag)))
            {
                return false;
            }

            if (ExcludedFlags.Any(flag => activeFlags != null && activeFlags.Contains(flag)))
            {
                return false;
            }

            if (RequiredDefeatedUnitIds.Any(unitId =>
                {
                    UnitRuntimeState unit = context.GetUnit(unitId);
                    return unit == null || unit.IsAlive;
                }))
            {
                return false;
            }

            if (RequiredAliveUnitIds.Any(unitId =>
                {
                    UnitRuntimeState unit = context.GetUnit(unitId);
                    return unit == null || !unit.IsAlive;
                }))
            {
                return false;
            }

            return true;
        }
    }
}
