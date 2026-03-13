using System;
using System.Collections;
using PhalanxChronicle.Battle.Units;
using PhalanxChronicle.Core;
using PhalanxChronicle.Localization;

namespace PhalanxChronicle.Battle.States
{
    public sealed class BattleStartState : BattleStateBase
    {
        public BattleStartState(BattleManager battleManager) : base(battleManager)
        {
        }

        public override string Name => nameof(BattleStartState);

        public override void Enter()
        {
            BattleManager.ClearSelectionAndHighlights();
            BattleManager.HideActionMenu();
            BattleManager.RefreshAllVisuals();
            if (BattleManager.ProcessScenarioCheckpointAndEnterDialogue(ScenarioCheckpoint.BattleStart, typeof(PlayerTurnStartState)))
            {
                return;
            }

            BattleManager.ChangeState<PlayerTurnStartState>();
        }
    }

    public sealed class PlayerTurnStartState : BattleStateBase
    {
        public PlayerTurnStartState(BattleManager battleManager) : base(battleManager)
        {
        }

        public override string Name => nameof(PlayerTurnStartState);

        public override void Enter()
        {
            BattleManager.EnsureTurn(TurnSide.Player);
            BattleManager.SetTurnLabel(LocalizationService.Text("ui.turn.player", "Turn: Player Phase"));
            BattleManager.SetLog(LocalizationService.Text("ui.log.select_player", "Select a blue officer to act."));
            BattleManager.SetEndTurnEnabled(true);
            if (BattleManager.ProcessScenarioCheckpointAndEnterDialogue(ScenarioCheckpoint.PlayerTurnStart, typeof(UnitSelectionState)))
            {
                return;
            }

            if (BattleManager.ShouldEnterAutoModeFromPlayerState())
            {
                BattleManager.ChangeState<PlayerAutoTurnState>();
                return;
            }

            BattleManager.ChangeState<UnitSelectionState>();
        }
    }

    public sealed class UnitSelectionState : BattleStateBase
    {
        public UnitSelectionState(BattleManager battleManager) : base(battleManager)
        {
        }

        public override string Name => nameof(UnitSelectionState);

        public override void Enter()
        {
            BattleManager.HideActionMenu();
            BattleManager.ClearSelectionAndHighlights();

            if (BattleManager.AreAllPlayerUnitsDone())
            {
                BattleManager.ChangeState<EnemyTurnState>();
                return;
            }

            if (BattleManager.ShouldEnterAutoModeFromPlayerState())
            {
                BattleManager.ChangeState<PlayerAutoTurnState>();
                return;
            }

            BattleManager.SetLog(LocalizationService.Text("ui.log.select_to_move", "Select a blue officer to move."));
        }

        public override void OnUnitClicked(Unit unitView)
        {
            if (!BattleManager.CanSelectUnit(unitView))
            {
                return;
            }

            BattleManager.SelectUnit(unitView.UnitId);
            BattleManager.ShowMoveRangeForSelection();
            BattleManager.ChangeState<UnitMoveSelectState>();
        }

        public override void OnEndTurnRequested()
        {
            BattleManager.ChangeState<EnemyTurnState>();
        }
    }

    public sealed class UnitMoveSelectState : BattleStateBase
    {
        public UnitMoveSelectState(BattleManager battleManager) : base(battleManager)
        {
        }

        public override string Name => nameof(UnitMoveSelectState);

        public override void Enter()
        {
            BattleManager.SetLog(LocalizationService.Text("ui.log.choose_destination", "Choose a destination, click the unit to act in place, or click an enemy in range to attack."));
            BattleManager.SetEndTurnEnabled(false);
            BattleManager.ShowMoveRangeForSelection();
        }

        public override void OnUnitClicked(Unit unitView)
        {
            if (unitView != null &&
                unitView.RuntimeState != null &&
                unitView.RuntimeState.Faction == UnitFaction.Enemy &&
                BattleManager.TryQuickAttackSelection(unitView.UnitId))
            {
                BattleManager.ChangeState<UnitActionExecuteState>();
                return;
            }

            if (BattleManager.TrySwitchSelectionTo(unitView))
            {
                return;
            }

            if (!BattleManager.IsSelectedUnit(unitView.UnitId))
            {
                return;
            }

            BattleManager.ChangeState<UnitActionMenuState>();
        }

        public override void OnCellClicked(GridPosition position)
        {
            if (BattleManager.IsSelectionAtPosition(position))
            {
                BattleManager.ChangeState<UnitActionMenuState>();
                return;
            }

            if (!BattleManager.TryMoveSelection(position))
            {
                return;
            }

            BattleManager.ChangeState<UnitActionMenuState>();
        }

        public override void OnCellHovered(GridPosition position, bool isHovered)
        {
            BattleManager.PreviewMoveDestination(position, isHovered);
        }

        public override void OnUnitHovered(Unit unitView, bool isHovered)
        {
            BattleManager.PreviewQuickAttackTarget(unitView, isHovered);
        }
    }

    public sealed class UnitActionMenuState : BattleStateBase
    {
        public UnitActionMenuState(BattleManager battleManager) : base(battleManager)
        {
        }

        public override string Name => nameof(UnitActionMenuState);

        public override void Enter()
        {
            BattleManager.SetLog(BattleManager.GetActionMenuInstructionText());
            BattleManager.ShowActionMenu();
        }

        public override void Exit()
        {
            BattleManager.HideActionMenu();
        }

        public override void OnAttackRequested()
        {
            if (!BattleManager.HasAttackTargetsForSelection())
            {
                return;
            }

            BattleManager.ChangeState<UnitTargetSelectState>();
        }

        public override void OnSkillRequested()
        {
            if (!BattleManager.HasSkillTargetsForSelection())
            {
                return;
            }

            BattleManager.ChangeState<UnitSkillTargetState>();
        }

        public override void OnWaitRequested()
        {
            BattleManager.WaitWithSelection();
            string unitName = BattleManager.GetSelectedUnitDisplayName();
            BattleManager.ResolvePlayerAction(LocalizationService.Format("ui.log.unit_waited", "{0} held position.", unitName));
        }

        public override void OnBackRequested()
        {
            if (!BattleManager.TryUndoSelectionMove())
            {
                return;
            }

            BattleManager.ChangeState<UnitMoveSelectState>();
        }

        public override void OnUnitClicked(Unit unitView)
        {
            if (unitView == null || unitView.RuntimeState == null)
            {
                return;
            }

            if (BattleManager.TrySwitchSelectionTo(unitView))
            {
                BattleManager.ChangeState<UnitMoveSelectState>();
                return;
            }

            if (BattleManager.IsSelectedUnit(unitView.UnitId))
            {
                if (BattleManager.TryUndoSelectionMove())
                {
                    BattleManager.ChangeState<UnitMoveSelectState>();
                    return;
                }

                return;
            }

            if (unitView.RuntimeState.Faction != UnitFaction.Enemy)
            {
                return;
            }

            if (!BattleManager.TryQuickAttackSelection(unitView.UnitId))
            {
                return;
            }

            BattleManager.ChangeState<UnitActionExecuteState>();
        }
    }

    public sealed class UnitTargetSelectState : BattleStateBase
    {
        public UnitTargetSelectState(BattleManager battleManager) : base(battleManager)
        {
        }

        public override string Name => nameof(UnitTargetSelectState);

        public override void Enter()
        {
            BattleManager.SetLog(LocalizationService.Text("ui.log.select_target_cancel", "Select an enemy target, or click the acting unit to cancel."));
            BattleManager.ShowAttackRangeForSelection();
        }

        public override void Exit()
        {
            BattleManager.ClearTargetPreview();
        }

        public override void OnUnitClicked(Unit unitView)
        {
            if (unitView != null && BattleManager.IsSelectedUnit(unitView.UnitId))
            {
                BattleManager.ChangeState<UnitActionMenuState>();
                return;
            }

            if (!BattleManager.TryAttackSelection(unitView.UnitId))
            {
                if (BattleManager.TrySwitchSelectionTo(unitView))
                {
                    BattleManager.ChangeState<UnitMoveSelectState>();
                }

                return;
            }

            BattleManager.ChangeState<UnitActionExecuteState>();
        }

        public override void OnUnitHovered(Unit unitView, bool isHovered)
        {
            BattleManager.PreviewAttackTarget(unitView, isHovered);
        }
    }

    public sealed class UnitSkillTargetState : BattleStateBase
    {
        public UnitSkillTargetState(BattleManager battleManager) : base(battleManager)
        {
        }

        public override string Name => nameof(UnitSkillTargetState);

        public override void Enter()
        {
            BattleManager.SetLog(LocalizationService.Text("ui.log.select_skill_target_cancel", "Select a skill target, or click the acting unit to cancel."));
            BattleManager.ShowSkillRangeForSelection();
        }

        public override void Exit()
        {
            BattleManager.ClearTargetPreview();
        }

        public override void OnUnitClicked(Unit unitView)
        {
            if (unitView != null && BattleManager.IsSelectedUnit(unitView.UnitId))
            {
                BattleManager.ChangeState<UnitActionMenuState>();
                return;
            }

            if (!BattleManager.TryUseSkillSelection(unitView.UnitId))
            {
                if (BattleManager.TrySwitchSelectionTo(unitView))
                {
                    BattleManager.ChangeState<UnitMoveSelectState>();
                }

                return;
            }

            BattleManager.ChangeState<UnitActionExecuteState>();
        }

        public override void OnUnitHovered(Unit unitView, bool isHovered)
        {
            BattleManager.PreviewSkillTarget(unitView, isHovered);
        }
    }

    public sealed class UnitActionExecuteState : BattleStateBase
    {
        public UnitActionExecuteState(BattleManager battleManager) : base(battleManager)
        {
        }

        public override string Name => nameof(UnitActionExecuteState);

        public override void Enter()
        {
            BattleManager.StartManagedCoroutine(RunPlayerAction());
        }

        private IEnumerator RunPlayerAction()
        {
            yield return BattleManager.ExecutePendingPlayerAction();
        }
    }

    public sealed class EnemyTurnState : BattleStateBase
    {
        public EnemyTurnState(BattleManager battleManager) : base(battleManager)
        {
        }

        public override string Name => nameof(EnemyTurnState);

        public override void Enter()
        {
            if (BattleManager.ProcessScenarioCheckpointAndEnterDialogue(ScenarioCheckpoint.EnemyTurnStart, typeof(EnemyTurnState)))
            {
                return;
            }

            BattleManager.StartManagedCoroutine(RunEnemyTurn());
        }

        private IEnumerator RunEnemyTurn()
        {
            yield return BattleManager.ExecuteEnemyTurnSequence();
        }
    }

    public sealed class PlayerAutoTurnState : BattleStateBase
    {
        public PlayerAutoTurnState(BattleManager battleManager) : base(battleManager)
        {
        }

        public override string Name => nameof(PlayerAutoTurnState);

        public override void Enter()
        {
            BattleManager.StartManagedCoroutine(RunAutoTurn());
        }

        private IEnumerator RunAutoTurn()
        {
            yield return BattleManager.RunAutoBattleLoop();
        }
    }

    public sealed class ScenarioDialogueState : BattleStateBase
    {
        public ScenarioDialogueState(BattleManager battleManager) : base(battleManager)
        {
        }

        public override string Name => nameof(ScenarioDialogueState);

        public override void Enter()
        {
            BattleManager.HideActionMenu();
            BattleManager.ShowCurrentScenarioDialogue();
        }

        public override void Exit()
        {
            BattleManager.HideScenarioDialogue();
        }

        public override void OnConfirmRequested()
        {
            BattleManager.AdvanceScenarioDialogue();
        }
    }

    public sealed class BattleVictoryState : BattleStateBase
    {
        public BattleVictoryState(BattleManager battleManager) : base(battleManager)
        {
        }

        public override string Name => nameof(BattleVictoryState);

        public override void Enter()
        {
            BattleManager.HideActionMenu();
            BattleManager.ClearSelectionAndHighlights();
            BattleManager.SetTurnLabel(LocalizationService.Text("ui.turn.end", "Turn: Battle End"));
            BattleManager.SetLog(LocalizationService.Text("ui.log.victory", "All enemies defeated."));
            BattleManager.ShowResult(LocalizationService.Text("ui.result.victory", "Victory"));
            BattleManager.SetEndTurnEnabled(false);
        }

        public override void OnConfirmRequested()
        {
            BattleManager.ConfirmBattleResult();
        }
    }

    public sealed class BattleDefeatState : BattleStateBase
    {
        public BattleDefeatState(BattleManager battleManager) : base(battleManager)
        {
        }

        public override string Name => nameof(BattleDefeatState);

        public override void Enter()
        {
            BattleManager.HideActionMenu();
            BattleManager.ClearSelectionAndHighlights();
            BattleManager.SetTurnLabel(LocalizationService.Text("ui.turn.end", "Turn: Battle End"));
            BattleManager.SetLog(LocalizationService.Text("ui.log.defeat", "All player units have fallen."));
            BattleManager.ShowResult(LocalizationService.Text("ui.result.defeat", "Defeat"));
            BattleManager.SetEndTurnEnabled(false);
        }

        public override void OnConfirmRequested()
        {
            BattleManager.ConfirmBattleResult();
        }
    }
}
