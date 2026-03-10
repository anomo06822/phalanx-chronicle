using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class BattleSimulation
    {
        private readonly RangeCalculator rangeCalculator;
        private readonly MoveSystem moveSystem;
        private readonly CombatSystem combatSystem;
        private readonly SkillSystem skillSystem;
        private readonly AIController aiController;
        private readonly TurnManager turnManager;

        public BattleSimulation(StageDefinitionData stage)
        {
            Context = new BattleContext(stage);
            rangeCalculator = new RangeCalculator();
            moveSystem = new MoveSystem(rangeCalculator);
            combatSystem = new CombatSystem(rangeCalculator);
            skillSystem = new SkillSystem();
            aiController = new AIController(rangeCalculator, skillSystem);
            turnManager = new TurnManager();
            turnManager.BeginTurn(Context, TurnSide.Player);
            Context.EvaluateBattleOutcome();
        }

        public BattleContext Context { get; }

        public IReadOnlyList<UnitRuntimeState> GetUnits(UnitFaction faction)
        {
            return Context.GetUnits(faction);
        }

        public IReadOnlyList<GridPosition> GetMoveRange(string unitId)
        {
            UnitRuntimeState unit = Context.GetUnit(unitId);
            return CanControlUnit(unit) ? rangeCalculator.GetMoveRange(Context, unit) : new List<GridPosition>();
        }

        public IReadOnlyList<UnitRuntimeState> GetAttackableTargets(string unitId)
        {
            UnitRuntimeState unit = Context.GetUnit(unitId);
            return CanControlUnit(unit) ? rangeCalculator.GetAttackableTargets(Context, unit) : new List<UnitRuntimeState>();
        }

        public IReadOnlyList<GridPosition> GetAttackRange(string unitId)
        {
            UnitRuntimeState unit = Context.GetUnit(unitId);
            int attackRange = unit == null ? 0 : PassiveSkillRules.GetAttackRange(unit);
            return unit == null
                ? new List<GridPosition>()
                : rangeCalculator.GetAttackRange(Context, unit.Position, attackRange);
        }

        public IReadOnlyList<GridPosition> GetProjectedAttackRange(string unitId)
        {
            UnitRuntimeState unit = Context.GetUnit(unitId);
            return CanControlUnit(unit) ? rangeCalculator.GetProjectedAttackRange(Context, unit) : new List<GridPosition>();
        }

        public IReadOnlyList<UnitRuntimeState> GetSkillTargets(string unitId)
        {
            UnitRuntimeState unit = Context.GetUnit(unitId);
            return CanControlUnit(unit) ? skillSystem.GetSkillTargets(Context, unit) : new List<UnitRuntimeState>();
        }

        public IReadOnlyList<GridPosition> GetSkillRange(string unitId)
        {
            UnitRuntimeState unit = Context.GetUnit(unitId);
            return CanControlUnit(unit) ? skillSystem.GetSkillRange(Context, unit) : new List<GridPosition>();
        }

        public bool TryMoveUnit(string unitId, GridPosition destination)
        {
            UnitRuntimeState unit = Context.GetUnit(unitId);
            if (!CanControlUnit(unit))
            {
                return false;
            }

            return moveSystem.TryMove(Context, unit, destination);
        }

        public CombatResult TryAttack(string attackerUnitId, string defenderUnitId)
        {
            UnitRuntimeState attacker = Context.GetUnit(attackerUnitId);
            UnitRuntimeState defender = Context.GetUnit(defenderUnitId);
            if (!CanControlUnit(attacker))
            {
                return null;
            }

            return combatSystem.TryAttack(Context, attacker, defender);
        }

        public SkillResult TryUseSkill(string casterUnitId, string targetUnitId)
        {
            UnitRuntimeState caster = Context.GetUnit(casterUnitId);
            UnitRuntimeState target = Context.GetUnit(targetUnitId);
            if (!CanControlUnit(caster))
            {
                return null;
            }

            return skillSystem.TryUseSkill(Context, caster, target);
        }

        public void Wait(string unitId)
        {
            UnitRuntimeState unit = Context.GetUnit(unitId);
            if (!CanControlUnit(unit))
            {
                return;
            }

            unit.MarkActed();
        }

        public bool AreAllUnitsDone(UnitFaction faction)
        {
            return Context.GetUnits(faction).All(unit => unit.HasActed);
        }

        public bool TryFindAttackDestination(string attackerUnitId, string targetUnitId, out GridPosition destination)
        {
            destination = new GridPosition(0, 0);

            UnitRuntimeState attacker = Context.GetUnit(attackerUnitId);
            UnitRuntimeState target = Context.GetUnit(targetUnitId);
            if (!CanControlUnit(attacker) || target == null || !target.IsAlive || attacker.Faction == target.Faction)
            {
                return false;
            }

            GridPosition? bestDestination = rangeCalculator.GetMoveRange(Context, attacker)
                .Where(position => rangeCalculator.GetAttackableTargets(Context, attacker, position).Any(candidate => candidate.Id == targetUnitId))
                .OrderBy(position => position.ManhattanDistance(attacker.Position))
                .ThenBy(position => position.ManhattanDistance(target.Position))
                .ThenBy(position => position.Y)
                .ThenBy(position => position.X)
                .Cast<GridPosition?>()
                .FirstOrDefault();

            if (!bestDestination.HasValue)
            {
                return false;
            }

            destination = bestDestination.Value;
            return true;
        }

        public TurnSide EndCurrentTurn()
        {
            return turnManager.EndTurn(Context);
        }

        public AiDecision BuildEnemyDecision(string unitId)
        {
            UnitRuntimeState enemy = Context.GetUnit(unitId);
            return aiController.Decide(Context, enemy);
        }

        public UnitActionResult ResolveEnemyAction(string unitId)
        {
            UnitRuntimeState enemy = Context.GetUnit(unitId);
            if (!CanControlUnit(enemy))
            {
                return null;
            }

            GridPosition start = enemy.Position;
            AiDecision decision = aiController.Decide(Context, enemy);
            if (decision.Destination != enemy.Position)
            {
                moveSystem.TryMove(Context, enemy, decision.Destination);
            }

            CombatResult combatResult = null;
            SkillResult skillResult = null;
            if (decision.ActionType == AiActionType.Skill && decision.HasTarget)
            {
                skillResult = skillSystem.TryUseSkill(Context, enemy, Context.GetUnit(decision.TargetUnitId));
            }
            else if (decision.ActionType == AiActionType.Attack && decision.HasTarget)
            {
                combatResult = combatSystem.TryAttack(Context, enemy, Context.GetUnit(decision.TargetUnitId));
            }

            if (combatResult == null && skillResult == null && !enemy.HasActed)
            {
                enemy.MarkActed();
            }

            return new UnitActionResult(enemy.Id, start, enemy.Position, combatResult, skillResult);
        }

        private bool CanControlUnit(UnitRuntimeState unit)
        {
            if (unit == null || !unit.IsAlive)
            {
                return false;
            }

            return Context.CurrentTurnSide == TurnSide.Player
                ? unit.Faction == UnitFaction.Player
                : unit.Faction == UnitFaction.Enemy;
        }
    }
}
