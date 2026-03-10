namespace PhalanxChronicle.Core
{
    public sealed class UnitActionResult
    {
        public UnitActionResult(
            string unitId,
            GridPosition startPosition,
            GridPosition endPosition,
            CombatResult combatResult,
            SkillResult skillResult)
        {
            UnitId = unitId;
            StartPosition = startPosition;
            EndPosition = endPosition;
            CombatResult = combatResult;
            SkillResult = skillResult;
        }

        public string UnitId { get; }

        public GridPosition StartPosition { get; }

        public GridPosition EndPosition { get; }

        public CombatResult CombatResult { get; }

        public SkillResult SkillResult { get; }

        public bool PerformedAttack => CombatResult != null;

        public bool PerformedSkill => SkillResult != null;
    }
}
