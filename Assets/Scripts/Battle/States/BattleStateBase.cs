using PhalanxChronicle.Battle.Units;
using PhalanxChronicle.Core;

namespace PhalanxChronicle.Battle.States
{
    public abstract class BattleStateBase : IBattleState
    {
        protected BattleStateBase(BattleManager battleManager)
        {
            BattleManager = battleManager;
        }

        protected BattleManager BattleManager { get; }

        public abstract string Name { get; }

        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void OnUnitClicked(Unit unitView)
        {
        }

        public virtual void OnCellClicked(GridPosition position)
        {
        }

        public virtual void OnAttackRequested()
        {
        }

        public virtual void OnSkillRequested()
        {
        }

        public virtual void OnWaitRequested()
        {
        }

        public virtual void OnEndTurnRequested()
        {
        }
    }
}
