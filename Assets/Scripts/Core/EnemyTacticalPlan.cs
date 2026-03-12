using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    internal enum EnemyTacticalPriority
    {
        Support = 0,
        Protector = 1,
        Aggressor = 2,
        Boss = 3,
    }

    internal sealed class EnemyTacticalPlan
    {
        public EnemyTacticalPlan(
            string focusTargetId,
            HashSet<string> protectedUnitIds,
            HashSet<GridPosition> chokeTiles,
            IReadOnlyList<string> orderedUnitIds)
        {
            FocusTargetId = focusTargetId ?? string.Empty;
            ProtectedUnitIds = protectedUnitIds ?? new HashSet<string>();
            ChokeTiles = chokeTiles ?? new HashSet<GridPosition>();
            OrderedUnitIds = orderedUnitIds ?? new List<string>();
        }

        public string FocusTargetId { get; }

        public HashSet<string> ProtectedUnitIds { get; }

        public HashSet<GridPosition> ChokeTiles { get; }

        public IReadOnlyList<string> OrderedUnitIds { get; }
    }
}
