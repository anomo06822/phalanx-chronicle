using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public enum BattleIntentActionKind
    {
        None,
        Move,
        Attack,
        Skill,
        QuickAttack,
    }

    public sealed class BattleIntentEffectPreview
    {
        public BattleIntentEffectPreview(
            string unitId,
            int predictedDamage,
            int predictedHealing,
            int remainingHp,
            bool lethal,
            IReadOnlyList<SkillStatusApplication> predictedStatuses,
            bool isPrimaryTarget)
        {
            UnitId = unitId ?? string.Empty;
            PredictedDamage = predictedDamage;
            PredictedHealing = predictedHealing;
            RemainingHp = remainingHp;
            Lethal = lethal;
            PredictedStatuses = predictedStatuses != null
                ? predictedStatuses
                    .Where(status => status != null && status.Type != StatusEffectType.None)
                    .ToList()
                : new List<SkillStatusApplication>();
            IsPrimaryTarget = isPrimaryTarget;
        }

        public string UnitId { get; }

        public int PredictedDamage { get; }

        public int PredictedHealing { get; }

        public int RemainingHp { get; }

        public bool Lethal { get; }

        public IReadOnlyList<SkillStatusApplication> PredictedStatuses { get; }

        public bool IsPrimaryTarget { get; }
    }

    public sealed class BattleIntentPreview
    {
        public BattleIntentPreview(
            BattleIntentActionKind actionKind,
            string actorUnitId,
            GridPosition origin,
            GridPosition destination,
            IReadOnlyList<GridPosition> path,
            int moveCost,
            string primaryTargetId,
            IReadOnlyList<BattleIntentEffectPreview> effects,
            BattleThreatProjection threatAfterAction,
            string rangeLabel,
            string areaLabel,
            int manaCost,
            bool canCommit,
            string blockReason)
        {
            ActionKind = actionKind;
            ActorUnitId = actorUnitId ?? string.Empty;
            Origin = origin;
            Destination = destination;
            Path = path ?? new List<GridPosition>();
            MoveCost = moveCost;
            PrimaryTargetId = primaryTargetId ?? string.Empty;
            Effects = effects ?? new List<BattleIntentEffectPreview>();
            ThreatAfterAction = threatAfterAction ?? new BattleThreatProjection(0, 0, new List<string>());
            RangeLabel = rangeLabel ?? string.Empty;
            AreaLabel = areaLabel ?? string.Empty;
            ManaCost = manaCost;
            CanCommit = canCommit;
            BlockReason = blockReason ?? string.Empty;
        }

        public BattleIntentActionKind ActionKind { get; }

        public string ActorUnitId { get; }

        public GridPosition Origin { get; }

        public GridPosition Destination { get; }

        public IReadOnlyList<GridPosition> Path { get; }

        public int MoveCost { get; }

        public string PrimaryTargetId { get; }

        public IReadOnlyList<BattleIntentEffectPreview> Effects { get; }

        public IReadOnlyList<string> AffectedTargetIds => Effects.Select(effect => effect.UnitId).Distinct().ToList();

        public int PredictedDamage => Effects.Sum(effect => effect.PredictedDamage);

        public int PredictedHealing => Effects.Sum(effect => effect.PredictedHealing);

        public IReadOnlyList<SkillStatusApplication> PredictedStatuses => Effects
            .SelectMany(effect => effect.PredictedStatuses)
            .Where(status => status != null && status.Type != StatusEffectType.None)
            .GroupBy(status => status.Type)
            .Select(group => group.OrderByDescending(status => status.Duration).First())
            .ToList();

        public IReadOnlyList<string> LethalTargetIds => Effects
            .Where(effect => effect.Lethal)
            .Select(effect => effect.UnitId)
            .Distinct()
            .ToList();

        public BattleThreatProjection ThreatAfterAction { get; }

        public string RangeLabel { get; }

        public string AreaLabel { get; }

        public int ManaCost { get; }

        public bool CanCommit { get; }

        public string BlockReason { get; }
    }
}
