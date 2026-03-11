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
                TerrainType terrainType = context.GetTerrainAt(unit.Position);
                bool hadBleeding = unit.HasStatus(StatusEffectType.Bleeding);
                unit.AdvanceOwnTurnEnd();

                if (hadBleeding && unit.IsAlive)
                {
                    unit.ApplyDamage(2);
                }

                if (unit.IsAlive)
                {
                    int terrainDamage = TerrainRules.GetEndTurnDamage(terrainType);
                    if (terrainDamage > 0)
                    {
                        unit.ApplyDamage(terrainDamage);
                    }
                }

                if (unit.IsAlive)
                {
                    int terrainHealing = TerrainRules.GetEndTurnHealing(terrainType);
                    if (terrainHealing > 0)
                    {
                        unit.ApplyHealing(terrainHealing);
                    }

                    unit.RestoreMana(SupportRules.GetManaRecovery(context, unit));
                }

                if (!unit.IsAlive)
                {
                    context.RemoveUnit(unit.Id);
                }
            }

            TurnSide next = context.CurrentTurnSide == TurnSide.Player ? TurnSide.Enemy : TurnSide.Player;
            if (context.CurrentTurnSide == TurnSide.Enemy)
            {
                context.AdvanceRound();
            }

            BeginTurn(context, next);
            context.EvaluateBattleOutcome();
            return next;
        }
    }
}
