using PhalanxChronicle.Battle.Units;
using PhalanxChronicle.Core;

namespace PhalanxChronicle.Battle.States
{
    public interface IBattleState
    {
        string Name { get; }

        void Enter();

        void Exit();

        void OnUnitClicked(Unit unitView);

        void OnCellClicked(GridPosition position);

        void OnAttackRequested();

        void OnSkillRequested();

        void OnWaitRequested();

        void OnEndTurnRequested();
    }
}
