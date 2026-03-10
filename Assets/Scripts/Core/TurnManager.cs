namespace PhalanxChronicle.Core
{
    public sealed class TurnManager
    {
        public void BeginTurn(BattleContext context, TurnSide turnSide)
        {
            context.SetCurrentTurn(turnSide);
            UnitFaction faction = turnSide == TurnSide.Player ? UnitFaction.Player : UnitFaction.Enemy;
            foreach (UnitRuntimeState unit in context.GetUnits(faction))
            {
                unit.ResetTurn();
            }
        }

        public TurnSide EndTurn(BattleContext context)
        {
            UnitFaction endingFaction = context.CurrentTurnSide == TurnSide.Player ? UnitFaction.Player : UnitFaction.Enemy;
            foreach (UnitRuntimeState unit in context.GetUnits(endingFaction))
            {
                unit.AdvanceOwnTurnEnd();
            }

            TurnSide next = context.CurrentTurnSide == TurnSide.Player ? TurnSide.Enemy : TurnSide.Player;
            BeginTurn(context, next);
            return next;
        }
    }
}
