using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    public sealed class BattleQuickAttackPreview
    {
        public BattleQuickAttackPreview(
            string attackerUnitId,
            string targetUnitId,
            GridPosition destination,
            IReadOnlyList<GridPosition> path,
            int moveCost,
            int projectedDamage,
            int defenderRemainingHp,
            bool isLethal,
            int effectiveAttack,
            int effectiveDefense)
        {
            AttackerUnitId = attackerUnitId ?? string.Empty;
            TargetUnitId = targetUnitId ?? string.Empty;
            Destination = destination;
            Path = path ?? new List<GridPosition>();
            MoveCost = moveCost;
            ProjectedDamage = projectedDamage;
            DefenderRemainingHp = defenderRemainingHp;
            IsLethal = isLethal;
            EffectiveAttack = effectiveAttack;
            EffectiveDefense = effectiveDefense;
        }

        public string AttackerUnitId { get; }

        public string TargetUnitId { get; }

        public GridPosition Destination { get; }

        public IReadOnlyList<GridPosition> Path { get; }

        public int MoveCost { get; }

        public int ProjectedDamage { get; }

        public int DefenderRemainingHp { get; }

        public bool IsLethal { get; }

        public int EffectiveAttack { get; }

        public int EffectiveDefense { get; }
    }
}
