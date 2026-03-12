using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    public sealed class BattlePathPreview
    {
        public BattlePathPreview(
            GridPosition destination,
            IReadOnlyList<GridPosition> path,
            int moveCost,
            int attackTargetCount,
            int skillTargetCount)
        {
            Destination = destination;
            Path = path ?? new List<GridPosition>();
            MoveCost = moveCost;
            AttackTargetCount = attackTargetCount;
            SkillTargetCount = skillTargetCount;
        }

        public GridPosition Destination { get; }

        public IReadOnlyList<GridPosition> Path { get; }

        public int MoveCost { get; }

        public int AttackTargetCount { get; }

        public int SkillTargetCount { get; }
    }
}
