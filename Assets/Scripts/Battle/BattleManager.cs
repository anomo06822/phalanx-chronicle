using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Battle.Effects;
using PhalanxChronicle.Battle.Grid;
using PhalanxChronicle.Battle.States;
using PhalanxChronicle.Battle.Units;
using PhalanxChronicle.Core;
using PhalanxChronicle.Data;
using PhalanxChronicle.Localization;
using PhalanxChronicle.Presentation;
using PhalanxChronicle.UI;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace PhalanxChronicle.Battle
{
    public sealed class BattleManager : MonoBehaviour
    {
        private readonly Dictionary<string, Unit> unitViews = new Dictionary<string, Unit>();
        private readonly Dictionary<Type, IBattleState> states = new Dictionary<Type, IBattleState>();

        private BattleScenarioDefinition scenarioDefinition;
        private BattleScenarioData scenarioData;
        private ScenarioDirector scenarioDirector;
        private Action<BattleResultSummary> battleCompletedHandler;
        private BattleSimulation simulation;
        private GridManager gridManager;
        private BattleHUD battleHUD;
        private BattleActionDockView actionMenuPanel;
        private readonly BattleActionSequencer actionSequencer = new BattleActionSequencer();
        private readonly BattleHudModelBuilder hudModelBuilder = new BattleHudModelBuilder();
        private BattlePresentationController presentationController;
        private IBattleState currentState;
        private string selectedUnitId;
        private GridPosition selectedUnitOrigin;
        private bool hasSelectedUnitOrigin;
        private CombatResult pendingCombatResult;
        private DuelResult pendingDuelResult;
        private SkillResult pendingSkillResult;
        private bool initialized;
        private GameObject unitRoot;
        private Type pendingDialogueResumeState;
        private string currentTurnText = string.Empty;
        private string currentInstructionText = string.Empty;
        private bool autoStartScenario = true;
        private bool battleRewardsGranted;
        private string battleResultSummaryText = string.Empty;
        private readonly List<string> battleResultRewardLines = new List<string>();
        private readonly List<string> battleResultSpecialLines = new List<string>();
        private readonly List<string> battleResultUnitLines = new List<string>();
        private bool enableFirstBattleOnboarding;
        private Action<bool> firstBattleOnboardingResolvedHandler;
        private FirstBattleOnboardingController onboardingController;
        private BattleDecisionContext currentDecisionContext = new BattleDecisionContext();
        private BattleOverviewModel currentOverviewModel = new BattleOverviewModel();
        private bool isAutoModeEnabled;
        private bool isAutoModeRunning;
        private bool isAutoModePausedByOverlay;
        private bool isAutoConfirmVisible;

        public bool IsDialogueVisible => battleHUD != null && battleHUD.IsDialogueVisible;

        public bool IsRerollVisible => battleHUD != null && battleHUD.IsRerollVisible;

        public bool IsResultVisible => battleHUD != null && battleHUD.IsResultVisible;

        public bool IsCampaignOverlayVisible => battleHUD != null && battleHUD.IsCampaignOverlayVisible;

        public bool IsOnboardingVisible => battleHUD != null && battleHUD.IsOnboardingVisible;

        public bool IsAutoModeEnabled => isAutoModeEnabled;

        public bool IsActionMenuVisible => actionMenuPanel != null && actionMenuPanel.IsVisible;

        public string CurrentActionMenuModeText => actionMenuPanel != null ? actionMenuPanel.CurrentModeText : string.Empty;

        public bool IsActionMenuBackEnabled => actionMenuPanel != null && actionMenuPanel.IsBackEnabled;

        public bool IsActionMenuSkillEnabled => actionMenuPanel != null && actionMenuPanel.IsSkillEnabled;

        public string CurrentObjectiveText => battleHUD != null ? battleHUD.CurrentObjectiveText : string.Empty;

        public BattleSimulation Simulation => simulation;

        public BattleScenarioData CurrentScenarioData => scenarioData;

        public void ConfigureFirstBattleOnboarding(bool enabled, Action<bool> onResolved = null)
        {
            enableFirstBattleOnboarding = enabled;
            firstBattleOnboardingResolvedHandler = onResolved;
        }

        public void Initialize(BattleScenarioDefinition scenario, bool autoStart = true, Action<BattleResultSummary> onBattleCompleted = null)
        {
            scenarioDefinition = scenario != null ? scenario : BattleScenarioDefinition.CreateDefault();
            autoStartScenario = autoStart;
            battleCompletedHandler = onBattleCompleted;
        }

        public void SetBattleCompletedHandler(Action<BattleResultSummary> handler)
        {
            battleCompletedHandler = handler;
        }

        public void StartScenario(BattleScenarioData data)
        {
            if (data == null)
            {
                return;
            }

            scenarioDefinition = BattleScenarioDefinition.CreateRuntime(data.ScenarioId);
            autoStartScenario = true;
            if (!initialized)
            {
                return;
            }

            HideCampaignOverlay();
            LoadScenario(data);
            ChangeState<BattleStartState>();
        }

        public void ShowCampaignStageSelect(CampaignStageSelectModel model, Action<int> onStageSelected)
        {
            battleHUD.ShowCampaignStageSelect(model, onStageSelected);
            SyncUnitInfoVisibility();
        }

        public void ShowCampaignInterlude(CampaignInterludeModel model, Action onPrimary, Action onSecondary = null)
        {
            battleHUD.ShowCampaignInterlude(model, onPrimary, onSecondary);
            SyncUnitInfoVisibility();
        }

        public void ShowCampaignOptionList(CampaignOptionListModel model, Action<string> onOptionSelected, Action onPrimary, Action onSecondary = null)
        {
            battleHUD.ShowCampaignOptionList(model, onOptionSelected, onPrimary, onSecondary);
            SyncUnitInfoVisibility();
        }

        public void HideCampaignOverlay()
        {
            battleHUD.HideCampaignOverlay();
            SyncUnitInfoVisibility();
        }

        public void ConfirmBattleResult()
        {
            if (simulation == null || scenarioData == null)
            {
                return;
            }

            battleHUD.HideResult();
            SyncUnitInfoVisibility();
            battleCompletedHandler?.Invoke(BuildBattleResultSummary());
        }

        public void ChangeState<TState>() where TState : IBattleState
        {
            ChangeState(typeof(TState));
        }

        public void EnsureTurn(TurnSide side)
        {
            if (simulation.Context.CurrentTurnSide != side)
            {
                simulation.EndCurrentTurn();
            }
        }

        public void SetTurnLabel(string text)
        {
            currentTurnText = text;
            UpdateHudModels();
        }

        public void SetLog(string text)
        {
            currentInstructionText = text;
            UpdateHudModels();
        }

        public void SetEndTurnEnabled(bool enabled)
        {
            battleHUD.SetEndTurnEnabled(enabled && !isAutoModeRunning);
        }

        public void SetRerollEnabled(bool enabled)
        {
            battleHUD.SetRerollEnabled(enabled);
        }

        public bool ShouldEnterAutoModeFromPlayerState()
        {
            if (!isAutoModeEnabled ||
                isAutoModeRunning ||
                simulation == null ||
                simulation.Context == null ||
                simulation.Context.BattleEnded ||
                simulation.Context.CurrentTurnSide != TurnSide.Player)
            {
                return false;
            }

            if (ShouldPauseAutoMode())
            {
                isAutoModePausedByOverlay = true;
                RefreshAutoModeUi();
                return false;
            }

            isAutoModePausedByOverlay = false;
            RefreshAutoModeUi();
            return true;
        }

        public void ShowResult(string text)
        {
            battleHUD.ShowResult(BuildBattleResultModel(text));
            SyncUnitInfoVisibility();
        }

        public void ShowCurrentScenarioDialogue()
        {
            ScenarioDialogueLine line = scenarioDirector != null ? scenarioDirector.PeekDialogue() : null;
            if (line == null)
            {
                HideScenarioDialogue();
                ResumeAfterScenarioDialogue();
                return;
            }

            battleHUD.ShowDialogue(
                LocalizationService.Text(line.SpeakerNameKey, line.SpeakerFallback),
                LocalizationService.Text(line.TextKey, line.TextFallback));
            SyncUnitInfoVisibility();
        }

        public void AdvanceScenarioDialogue()
        {
            if (scenarioDirector == null || !scenarioDirector.AdvanceDialogue())
            {
                HideScenarioDialogue();
                ResumeAfterScenarioDialogue();
                return;
            }

            ShowCurrentScenarioDialogue();
        }

        public void HideScenarioDialogue()
        {
            battleHUD.HideDialogue();
            SyncUnitInfoVisibility();
        }

        public bool HasScenarioFlag(string flagName)
        {
            return scenarioDirector != null && scenarioDirector.HasFlag(flagName);
        }

        public bool ProcessScenarioCheckpointAndEnterDialogue(ScenarioCheckpoint checkpoint, Type resumeStateType)
        {
            ProcessScenarioCheckpoint(checkpoint);
            return TryEnterScenarioDialogue(resumeStateType);
        }

        public bool AreAllPlayerUnitsDone()
        {
            return simulation.AreAllUnitsDone(UnitFaction.Player);
        }

        public bool CanSelectUnit(Unit unitView)
        {
            if (unitView == null || unitView.RuntimeState == null)
            {
                return false;
            }

            UnitRuntimeState runtimeState = unitView.RuntimeState;
            return simulation.Context.CurrentTurnSide == TurnSide.Player &&
                   runtimeState.Faction == UnitFaction.Player &&
                   runtimeState.IsAlive &&
                   !runtimeState.HasActed;
        }

        public void SelectUnit(string unitId)
        {
            selectedUnitId = unitId;
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected != null)
            {
                onboardingController?.MarkSelectedUnit();
                selectedUnitOrigin = selected.Position;
                hasSelectedUnitOrigin = true;
            }

            RefreshAllVisuals();
        }

        public bool IsSelectedUnit(string unitId)
        {
            return !string.IsNullOrEmpty(selectedUnitId) && selectedUnitId == unitId;
        }

        public void ClearSelectionAndHighlights()
        {
            selectedUnitId = null;
            hasSelectedUnitOrigin = false;
            ClearTargetPreview();
            if (gridManager != null)
            {
                gridManager.ClearHighlights();
            }

            RefreshAllVisuals();
        }

        public void ShowMoveRangeForSelection()
        {
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null)
            {
                return;
            }

            gridManager.ClearHighlights();
            gridManager.ShowMoveRange(simulation.GetMoveDestinations(selected.Id));
            gridManager.HighlightSelectedCell(selected.Position);
            RefreshAllVisuals();
        }

        public void PreviewMoveDestination(GridPosition position, bool isHovered)
        {
            if (!isHovered)
            {
                RestoreMoveSelectionPreview();
                return;
            }

            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null)
            {
                return;
            }

            BattleIntentPreview preview = simulation.PreviewMoveIntent(selected.Id, position);
            if (preview == null || !preview.CanCommit || preview.Path.Count <= 1)
            {
                RestoreMoveSelectionPreview();
                return;
            }

            ApplyMovePreview(selected, preview);
            BuildAndBindDecisionContext(
                BattleDecisionContextSource.MoveHover,
                selected,
                null,
                preview,
                preview.BlockReason);
        }

        public void ShowAttackRangeForSelection()
        {
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null)
            {
                return;
            }

            gridManager.ClearHighlights();
            gridManager.ShowAttackRange(simulation.GetAttackRange(selected.Id));
            gridManager.HighlightSelectedCell(selected.Position);
            RefreshAllVisuals();
        }

        public void ShowSkillRangeForSelection()
        {
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null)
            {
                return;
            }

            gridManager.ClearHighlights();
            gridManager.ShowSkillRange(simulation.GetSkillRange(selected.Id));
            gridManager.HighlightSelectedCell(selected.Position);
            RefreshAllVisuals();
        }

        public void ShowActionMenu()
        {
            BattleHudDecisionContextModel decisionContextModel = BuildAndBindDecisionContext(
                BattleDecisionContextSource.ActionMenu,
                GetSelectedUnit(),
                null,
                null,
                string.Empty);
            actionMenuPanel.Show(
                decisionContextModel.ActionMenuModel,
                HandleAttackRequested,
                HandleSkillRequested,
                HandleWaitRequested,
                HandleBackRequested);
            SyncUnitInfoVisibility();
        }

        public void HideActionMenu()
        {
            actionMenuPanel.Hide();
            SyncUnitInfoVisibility();
        }

        public bool HasAttackTargetsForSelection()
        {
            UnitRuntimeState selected = GetSelectedUnit();
            return selected != null && simulation.GetAttackableTargets(selected.Id).Count > 0;
        }

        public bool HasSkillTargetsForSelection()
        {
            UnitRuntimeState selected = GetSelectedUnit();
            int manaCost = selected == null ? 0 : ActiveSkillRules.GetManaCost(selected.ActiveSkill);
            return selected != null
                && selected.CanUseSkill
                && selected.CurrentMana >= manaCost
                && simulation.GetSkillTargets(selected.Id).Count > 0;
        }

        public bool TryMoveSelection(GridPosition position)
        {
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null)
            {
                return false;
            }

            bool moved = simulation.TryMoveUnit(selected.Id, position);
            if (moved)
            {
                onboardingController?.MarkMovedUnit();
                onboardingController?.DismissObjectiveUpdate();
                RefreshAllVisuals();
            }

            return moved;
        }

        public bool HasSelectionMoved()
        {
            UnitRuntimeState selected = GetSelectedUnit();
            return hasSelectedUnitOrigin && selected != null && selected.Position != selectedUnitOrigin;
        }

        public bool IsSelectionAtPosition(GridPosition position)
        {
            UnitRuntimeState selected = GetSelectedUnit();
            return selected != null && selected.Position == position;
        }

        public bool TrySwitchSelectionTo(Unit unitView)
        {
            if (unitView == null || !CanSelectUnit(unitView) || IsSelectedUnit(unitView.UnitId) || HasSelectionMoved())
            {
                return false;
            }

            pendingCombatResult = null;
            pendingSkillResult = null;
            ClearTargetPreview();
            SelectUnit(unitView.UnitId);
            ShowMoveRangeForSelection();
            return true;
        }

        public bool TryUndoSelectionMove()
        {
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null || !hasSelectedUnitOrigin || selected.Position == selectedUnitOrigin)
            {
                return false;
            }

            bool undone = simulation.TryUndoMoveUnit(selected.Id, selectedUnitOrigin);
            if (undone)
            {
                RefreshAllVisuals();
            }

            return undone;
        }

        public bool TryAttackSelection(string targetUnitId)
        {
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null)
            {
                return false;
            }

            pendingDuelResult = null;
            pendingSkillResult = null;
            DuelSceneDefinition duelScene = scenarioDirector != null
                ? scenarioDirector.TryMatchDuel(selected.Id, targetUnitId, simulation.Context)
                : null;
            if (duelScene != null)
            {
                pendingCombatResult = null;
                pendingDuelResult = DuelSystem.Resolve(simulation.Context, duelScene);
                if (pendingDuelResult != null)
                {
                    scenarioDirector.RecordDuelTriggered(pendingDuelResult.DuelId, pendingDuelResult.SetFlags);
                    return true;
                }
            }

            pendingCombatResult = simulation.TryAttack(selected.Id, targetUnitId);
            return pendingCombatResult != null;
        }

        public bool TryQuickAttackSelection(string targetUnitId)
        {
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null)
            {
                return false;
            }

            if (!simulation.TryFindAttackDestination(selected.Id, targetUnitId, out GridPosition destination))
            {
                return false;
            }

            if (destination != selected.Position)
            {
                bool moved = simulation.TryMoveUnit(selected.Id, destination);
                if (!moved)
                {
                    return false;
                }

                RefreshAllVisuals();
            }

            return TryAttackSelection(targetUnitId);
        }

        public bool TryUseSkillSelection(string targetUnitId)
        {
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null)
            {
                return false;
            }

            pendingDuelResult = null;
            pendingCombatResult = null;
            pendingSkillResult = simulation.TryUseSkill(selected.Id, targetUnitId);
            return pendingSkillResult != null;
        }

        public CombatResult ConsumePendingCombatResult()
        {
            CombatResult result = pendingCombatResult;
            pendingCombatResult = null;
            return result;
        }

        public SkillResult ConsumePendingSkillResult()
        {
            SkillResult result = pendingSkillResult;
            pendingSkillResult = null;
            return result;
        }

        public DuelResult ConsumePendingDuelResult()
        {
            DuelResult result = pendingDuelResult;
            pendingDuelResult = null;
            return result;
        }

        public IEnumerator ExecutePendingPlayerAction()
        {
            DuelResult duelResult = ConsumePendingDuelResult();
            if (duelResult != null)
            {
                yield return PlayDuelSequence(duelResult, TurnSide.Player);
                onboardingController?.MarkPlayerActionResolved(true);
                onboardingController?.DismissObjectiveUpdate();
                ResolvePlayerAction(BuildDuelLog(duelResult));
                yield break;
            }

            CombatResult combatResult = ConsumePendingCombatResult();
            if (combatResult != null)
            {
                yield return PlayCombatSequence(combatResult, TurnSide.Player);
                onboardingController?.MarkPlayerActionResolved(true);
                onboardingController?.DismissObjectiveUpdate();
                ResolvePlayerAction(BuildCombatLog(combatResult));
                yield break;
            }

            SkillResult skillResult = ConsumePendingSkillResult();
            if (skillResult != null)
            {
                yield return PlaySkillSequence(skillResult, TurnSide.Player);
                onboardingController?.MarkPlayerActionResolved(ActiveSkillRules.IsOffensiveSkill(skillResult.SkillType));
                onboardingController?.DismissObjectiveUpdate();
                ResolvePlayerAction(BuildSkillLog(skillResult));
                yield break;
            }

            ChangeState<UnitSelectionState>();
        }

        public void WaitWithSelection()
        {
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected != null)
            {
                simulation.Wait(selected.Id);
                onboardingController?.MarkPlayerActionResolved(false);
                onboardingController?.DismissObjectiveUpdate();
                RefreshAllVisuals();
            }
        }

        public string GetSelectedUnitDisplayName()
        {
            UnitRuntimeState selected = GetSelectedUnit();
            return selected != null ? GetUnitDisplayName(selected.Id) : string.Empty;
        }

        public string GetSelectedActiveSkillDisplayName()
        {
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null)
            {
                return LocalizationService.Text("ui.button.skill", "Skill");
            }

            string skillName = LocalizationService.Text(selected.ActiveSkillNameKey, selected.ActiveSkill.ToString());
            return skillName;
        }

        public string GetActionMenuInstructionText()
        {
            return HasSelectionMoved()
                ? LocalizationService.Text("ui.log.choose_action_moved", "移動後可選擇攻擊、技能、待命，或是撤回移動。")
                : LocalizationService.Text("ui.log.choose_action_hold", "原地可選擇攻擊、技能或待命。");
        }

        public IEnumerator RunAutoBattleLoop()
        {
            if (!isAutoModeEnabled ||
                simulation == null ||
                simulation.Context == null ||
                simulation.Context.CurrentTurnSide != TurnSide.Player)
            {
                isAutoModeRunning = false;
                isAutoModePausedByOverlay = false;
                RefreshAutoModeUi();
                yield break;
            }

            if (ShouldPauseAutoMode())
            {
                isAutoModeRunning = false;
                isAutoModePausedByOverlay = true;
                RefreshAutoModeUi();
                ChangeState<UnitSelectionState>();
                yield break;
            }

            IReadOnlyList<string> orderedPlayerIds = simulation.BuildPlayerAutoTurnOrder();
            if (orderedPlayerIds.Count == 0)
            {
                isAutoModeRunning = false;
                isAutoModePausedByOverlay = false;
                RefreshAutoModeUi();
                ChangeState<EnemyTurnState>();
                yield break;
            }

            isAutoModeRunning = true;
            isAutoModePausedByOverlay = false;
            HideActionMenu();
            ClearSelectionAndHighlights();
            SetEndTurnEnabled(false);
            RefreshAutoModeUi();

            UnitActionResult actionResult = simulation.ResolvePlayerAutoAction(orderedPlayerIds[0]);
            if (actionResult == null)
            {
                isAutoModeRunning = false;
                RefreshAutoModeUi();
                ChangeState<UnitSelectionState>();
                yield break;
            }

            Unit actorView = GetUnitView(actionResult.UnitId);
            if (actorView != null && actionResult.EndPosition != actionResult.StartPosition)
            {
                Vector3 startWorld = gridManager.GetWorldPosition(actionResult.StartPosition) + new Vector3(0f, 0f, -0.5f);
                Vector3 endWorld = gridManager.GetWorldPosition(actionResult.EndPosition) + new Vector3(0f, 0f, -0.5f);
                yield return actorView.AnimateMove(startWorld, endWorld, 0.22f);
            }

            string logText;
            if (actionResult.PerformedAttack)
            {
                yield return PlayCombatSequence(actionResult.CombatResult, TurnSide.Player);
                logText = BuildCombatLog(actionResult.CombatResult);
            }
            else if (actionResult.PerformedSkill)
            {
                yield return PlaySkillSequence(actionResult.SkillResult, TurnSide.Player);
                logText = BuildSkillLog(actionResult.SkillResult);
            }
            else
            {
                RefreshAllVisuals();
                logText = BuildAutoActionLog(actionResult);
            }

            yield return new WaitForSeconds(actionSequencer.GetPostActionHold(actionResult));

            isAutoModeRunning = false;
            RefreshAutoModeUi();
            ResolveAutoPlayerAction(logText);
        }

        public void ResolvePlayerAction(string logMessage)
        {
            SetLog(logMessage);
            PushBattleFeedEntry(logMessage);
            HideActionMenu();
            ClearDecisionContext();
            ClearSelectionAndHighlights();

            Type nextStateType = AreAllPlayerUnitsDone() ? typeof(EnemyTurnState) : typeof(UnitSelectionState);
            if (HandlePostActionScenarioFlow(nextStateType))
            {
                return;
            }

            SetEndTurnEnabled(true);
            ChangeState(nextStateType);
        }

        public void ResolveAutoPlayerAction(string logMessage)
        {
            SetLog(logMessage);
            PushBattleFeedEntry(logMessage);
            HideActionMenu();
            ClearDecisionContext();
            ClearSelectionAndHighlights();

            Type nextStateType;
            if (AreAllPlayerUnitsDone())
            {
                nextStateType = typeof(EnemyTurnState);
            }
            else if (isAutoModeEnabled && !ShouldPauseAutoMode())
            {
                nextStateType = typeof(PlayerAutoTurnState);
            }
            else
            {
                isAutoModePausedByOverlay = isAutoModeEnabled;
                nextStateType = typeof(UnitSelectionState);
            }

            if (HandlePostActionScenarioFlow(nextStateType))
            {
                return;
            }

            SetEndTurnEnabled(true);
            ChangeState(nextStateType);
        }

        public Coroutine StartManagedCoroutine(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        public IEnumerator ExecuteEnemyTurnSequence()
        {
            EnsureTurn(TurnSide.Enemy);
            onboardingController?.MarkEnemyTurnSeen();
            ClearSelectionAndHighlights();
            HideActionMenu();
            SetTurnLabel(LocalizationService.Text("ui.turn.enemy", "Turn: Enemy Phase"));
            SetEndTurnEnabled(false);
            SetLog(LocalizationService.Text("ui.log.enemy_acting", "Enemy forces are acting."));

            IReadOnlyList<string> orderedEnemyIds = simulation.BuildEnemyTurnOrder();
            foreach (string enemyId in orderedEnemyIds)
            {
                UnitRuntimeState enemy = simulation.Context.GetUnit(enemyId);
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                UnitActionResult actionResult = simulation.ResolveEnemyAction(enemy.Id);
                if (actionResult == null)
                {
                    continue;
                }

                Unit actorView = GetUnitView(actionResult.UnitId);
                if (actorView != null && actionResult.EndPosition != actionResult.StartPosition)
                {
                    Vector3 startWorld = gridManager.GetWorldPosition(actionResult.StartPosition) + new Vector3(0f, 0f, -0.5f);
                    Vector3 endWorld = gridManager.GetWorldPosition(actionResult.EndPosition) + new Vector3(0f, 0f, -0.5f);
                    yield return actorView.AnimateMove(startWorld, endWorld, 0.22f);
                }

                if (actionResult.PerformedAttack)
                {
                    yield return PlayCombatSequence(actionResult.CombatResult, TurnSide.Enemy);
                    string logText = BuildCombatLog(actionResult.CombatResult);
                    SetLog(logText);
                    PushBattleFeedEntry(logText);
                }
                else if (actionResult.PerformedSkill)
                {
                    yield return PlaySkillSequence(actionResult.SkillResult, TurnSide.Enemy);
                    string logText = BuildSkillLog(actionResult.SkillResult);
                    SetLog(logText);
                    PushBattleFeedEntry(logText);
                }
                else
                {
                    RefreshAllVisuals();
                    if (actionResult.EndPosition != actionResult.StartPosition)
                    {
                        string logText = LocalizationService.Format("ui.log.enemy_advanced", "{0} advanced.", GetUnitDisplayName(enemy.Id));
                        SetLog(logText);
                        PushBattleFeedEntry(logText);
                    }
                    else
                    {
                        string logText = LocalizationService.Format("ui.log.enemy_held", "{0} held position.", GetUnitDisplayName(enemy.Id));
                        SetLog(logText);
                        PushBattleFeedEntry(logText);
                    }
                }

                yield return new WaitForSeconds(actionSequencer.GetPostActionHold(actionResult));

                if (HandlePostActionScenarioFlow(typeof(EnemyTurnState)))
                {
                    yield break;
                }
            }

            simulation.EndCurrentTurn();
            ClearDecisionContext();
            ChangeState<PlayerTurnStartState>();
        }

        public void RefreshAllVisuals()
        {
            if (simulation == null || gridManager == null)
            {
                return;
            }

            EnsureUnitViews();
            bool suppressUnitInfo = ShouldSuppressUnitInfo();
            foreach (KeyValuePair<string, Unit> entry in unitViews)
            {
                UnitRuntimeState runtimeState = simulation.Context.GetUnit(entry.Key);
                if (runtimeState == null)
                {
                    continue;
                }

                entry.Value.SetInfoSuppressed(suppressUnitInfo);
                entry.Value.SetSelected(entry.Key == selectedUnitId);
                entry.Value.Sync(gridManager.GetWorldPosition(runtimeState.Position));
            }

            UpdateHudModels();
        }

        private void Awake()
        {
            presentationController = new BattlePresentationController(actionSequencer, hudModelBuilder);
            BuildRuntimeObjects();
        }

        private void Start()
        {
            if (initialized)
            {
                return;
            }

            RegisterStates();
            initialized = true;

            if (!autoStartScenario)
            {
                return;
            }

            if (scenarioDefinition == null)
            {
                scenarioDefinition = BattleScenarioDefinition.CreateDefault();
            }

            LoadScenario(scenarioDefinition.ToData());
            ChangeState<BattleStartState>();
        }

        private void BuildRuntimeObjects()
        {
            GameObject gridRoot = new GameObject("GridManager");
            gridRoot.transform.SetParent(transform, false);
            gridManager = gridRoot.AddComponent<GridManager>();

            GameObject canvasObject = new GameObject("BattleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject hudObject = new GameObject("BattleHUD");
            hudObject.transform.SetParent(canvasObject.transform, false);
            battleHUD = hudObject.AddComponent<BattleHUD>();
            battleHUD.Initialize(
                canvasObject.transform,
                HandleEndTurnRequested,
                HandleRerollRequested,
                HandleAutoModeRequested,
                HandleDialogueAdvanceRequested,
                HandleResultAdvanceRequested,
                HandleOnboardingSkipRequested,
                HandleAutoModeConfirmed,
                HandleAutoModeCancelled,
                hudModelBuilder);
            RefreshAutoModeUi();

            GameObject actionMenuObject = new GameObject("ActionMenuPanel");
            actionMenuObject.transform.SetParent(canvasObject.transform, false);
            actionMenuPanel = actionMenuObject.AddComponent<ActionMenuPanel>();
            actionMenuPanel.Initialize(canvasObject.transform);
        }

        private void CreateUnits()
        {
            if (unitRoot == null)
            {
                unitRoot = new GameObject("Units");
                unitRoot.transform.SetParent(transform, false);
            }

            unitViews.Clear();
            for (int index = unitRoot.transform.childCount - 1; index >= 0; index--)
            {
                Destroy(unitRoot.transform.GetChild(index).gameObject);
            }

            EnsureUnitViews();
            RefreshAllVisuals();
        }

        private void EnsureUnitViews()
        {
            if (simulation == null || unitRoot == null)
            {
                return;
            }

            foreach (UnitRuntimeState runtimeState in simulation.Context.Units)
            {
                if (unitViews.ContainsKey(runtimeState.Id))
                {
                    continue;
                }

                GameObject unitObject = new GameObject(runtimeState.Id);
                unitObject.transform.SetParent(unitRoot.transform, false);
                Unit unitView = unitObject.AddComponent<Unit>();
                unitView.Initialize(runtimeState, OnUnitClicked, OnUnitHoverChanged);
                unitViews[runtimeState.Id] = unitView;
            }
        }

        private void ConfigureCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            StageVisualDefinition stageVisual = StageVisualCatalog.GetDefinition(simulation.Context.StageNameKey);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.orthographic = true;
            mainCamera.backgroundColor = stageVisual.SkyTopColor;
            float largeBoardFactor = Mathf.Clamp01((Mathf.Max(simulation.Context.Width, simulation.Context.Height) - 12f) / 6f);
            float boardWidth = simulation.Context.Width + Mathf.Lerp(2f, 1.4f, largeBoardFactor);
            float boardHeight = simulation.Context.Height + Mathf.Lerp(2.1f, 1.5f, largeBoardFactor);
            float aspect = Mathf.Max(0.1f, mainCamera.aspect);
            float leftHudReserve = Mathf.Lerp(0.2f, 0.17f, largeBoardFactor);
            float rightHudReserve = Mathf.Lerp(0.21f, 0.17f, largeBoardFactor);
            float topHudReserve = Mathf.Lerp(0.12f, 0.09f, largeBoardFactor);
            float bottomHudReserve = Mathf.Lerp(0.08f, 0.07f, largeBoardFactor);
            float usableWidth = Mathf.Max(0.2f, 1f - leftHudReserve - rightHudReserve);
            float usableHeight = Mathf.Max(0.2f, 1f - topHudReserve - bottomHudReserve);
            float orthographicSizeForHeight = (boardHeight * 0.5f) / usableHeight;
            float orthographicSizeForWidth = (boardWidth * 0.5f) / (aspect * usableWidth);
            float orthographicSize = Mathf.Max(orthographicSizeForHeight, orthographicSizeForWidth);
            orthographicSize = Mathf.Ceil(orthographicSize * 16f) / 16f;
            float verticalOffset = (topHudReserve - bottomHudReserve) * orthographicSize * 0.24f;

            PixelPerfectCamera pixelPerfectCamera = mainCamera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfectCamera != null)
            {
                pixelPerfectCamera.assetsPPU = 64;
            }

            mainCamera.transform.position = SnapToPixelGrid(new Vector3(0f, verticalOffset, -10f), 64f);
            mainCamera.orthographicSize = orthographicSize;
        }

        private static Vector3 SnapToPixelGrid(Vector3 position, float assetsPpu)
        {
            if (assetsPpu <= 0f)
            {
                return position;
            }

            float pixel = 1f / assetsPpu;
            return new Vector3(
                Mathf.Round(position.x / pixel) * pixel,
                Mathf.Round(position.y / pixel) * pixel,
                position.z);
        }

        private void RegisterStates()
        {
            states[typeof(BattleStartState)] = new BattleStartState(this);
            states[typeof(PlayerTurnStartState)] = new PlayerTurnStartState(this);
            states[typeof(UnitSelectionState)] = new UnitSelectionState(this);
            states[typeof(UnitMoveSelectState)] = new UnitMoveSelectState(this);
            states[typeof(UnitActionMenuState)] = new UnitActionMenuState(this);
            states[typeof(UnitTargetSelectState)] = new UnitTargetSelectState(this);
            states[typeof(UnitSkillTargetState)] = new UnitSkillTargetState(this);
            states[typeof(UnitActionExecuteState)] = new UnitActionExecuteState(this);
            states[typeof(EnemyTurnState)] = new EnemyTurnState(this);
            states[typeof(PlayerAutoTurnState)] = new PlayerAutoTurnState(this);
            states[typeof(ScenarioDialogueState)] = new ScenarioDialogueState(this);
            states[typeof(BattleVictoryState)] = new BattleVictoryState(this);
            states[typeof(BattleDefeatState)] = new BattleDefeatState(this);
        }

        private void OnUnitClicked(Unit unitView)
        {
            if (IsInteractionLocked())
            {
                return;
            }

            currentState?.OnUnitClicked(unitView);
        }

        private void OnUnitHoverChanged(Unit unitView, bool isHovered)
        {
            if (IsInteractionLocked())
            {
                return;
            }

            currentState?.OnUnitHovered(unitView, isHovered);
        }

        private void OnCellHoverChanged(GridCellView cellView, bool isHovered)
        {
            if (IsInteractionLocked())
            {
                return;
            }

            currentState?.OnCellHovered(cellView.Position, isHovered);
        }

        private void OnCellClicked(GridCellView cellView)
        {
            if (IsInteractionLocked())
            {
                return;
            }

            currentState?.OnCellClicked(cellView.Position);
        }

        private void HandleAttackRequested()
        {
            if (IsInteractionLocked())
            {
                return;
            }

            currentState?.OnAttackRequested();
        }

        private void HandleSkillRequested()
        {
            if (IsInteractionLocked())
            {
                return;
            }

            currentState?.OnSkillRequested();
        }

        private void HandleWaitRequested()
        {
            if (IsInteractionLocked())
            {
                return;
            }

            currentState?.OnWaitRequested();
        }

        private void HandleBackRequested()
        {
            if (IsInteractionLocked())
            {
                return;
            }

            currentState?.OnBackRequested();
        }

        private void HandleEndTurnRequested()
        {
            if (IsInteractionLocked())
            {
                return;
            }

            currentState?.OnEndTurnRequested();
        }

        private void HandleAutoModeRequested()
        {
            if (battleHUD == null || simulation == null || simulation.Context == null || simulation.Context.BattleEnded)
            {
                return;
            }

            if (isAutoModeEnabled)
            {
                DisableAutoMode(LocalizationService.Text("ui.log.auto_disabled", "AI auto mode stopped."));
                return;
            }

            isAutoConfirmVisible = true;
            RefreshAutoModeUi();
            battleHUD.ShowConfirmDialog(new BattleConfirmDialogModel
            {
                Title = LocalizationService.Text("ui.auto.confirm.title", "啟用 AI 自動模式？"),
                Body = LocalizationService.Text("ui.auto.confirm.body", "啟用後，AI 會接手我軍並自動完成戰鬥。遇到劇情對話或新手引導時會暫停，直到你繼續。"),
                ConfirmLabel = LocalizationService.Text("ui.auto.confirm.confirm", "啟用 AI"),
                CancelLabel = LocalizationService.Text("ui.button.cancel", "取消"),
            });
            SyncUnitInfoVisibility();
        }

        private void HandleAutoModeConfirmed()
        {
            if (!isAutoConfirmVisible || battleHUD == null)
            {
                return;
            }

            isAutoConfirmVisible = false;
            isAutoModeEnabled = true;
            isAutoModePausedByOverlay = false;
            battleHUD.HideConfirmDialog();
            RefreshAutoModeUi();
            SetLog(LocalizationService.Text("ui.log.auto_enabled", "AI auto mode engaged."));
            SyncUnitInfoVisibility();

            if (simulation == null || simulation.Context == null || simulation.Context.CurrentTurnSide != TurnSide.Player)
            {
                return;
            }

            if (IsPlayerManualControlState())
            {
                PrepareAutoModeTakeover();
                ChangeState<UnitSelectionState>();
            }
        }

        private void HandleAutoModeCancelled()
        {
            if (!isAutoConfirmVisible)
            {
                return;
            }

            isAutoConfirmVisible = false;
            battleHUD.HideConfirmDialog();
            RefreshAutoModeUi();
            SyncUnitInfoVisibility();
        }

        private void HandleRerollRequested()
        {
            if (IsInteractionLocked() || scenarioData == null || !scenarioData.Stage.IsRandomMap)
            {
                return;
            }

            LoadScenario(scenarioDefinition.ToData());
            ChangeState<BattleStartState>();
        }

        private void HandleDialogueAdvanceRequested()
        {
            currentState?.OnConfirmRequested();
        }

        private void HandleResultAdvanceRequested()
        {
            currentState?.OnConfirmRequested();
        }

        private void DisableAutoMode(string logMessage)
        {
            bool wasEnabled = isAutoModeEnabled || isAutoConfirmVisible;
            isAutoModeEnabled = false;
            isAutoModePausedByOverlay = false;
            isAutoConfirmVisible = false;
            if (battleHUD != null)
            {
                battleHUD.HideConfirmDialog();
            }

            RefreshAutoModeUi();
            SyncUnitInfoVisibility();

            if (!string.IsNullOrWhiteSpace(logMessage) && wasEnabled)
            {
                SetLog(logMessage);
            }

            if (!isAutoModeRunning &&
                simulation != null &&
                simulation.Context != null &&
                simulation.Context.CurrentTurnSide == TurnSide.Player &&
                currentState is PlayerAutoTurnState)
            {
                ChangeState<UnitSelectionState>();
            }
        }

        private void ResetAutoModeState()
        {
            isAutoModeEnabled = false;
            isAutoModeRunning = false;
            isAutoModePausedByOverlay = false;
            isAutoConfirmVisible = false;
            if (battleHUD != null)
            {
                battleHUD.HideConfirmDialog();
            }

            RefreshAutoModeUi();
        }

        private void RefreshAutoModeUi()
        {
            if (battleHUD == null)
            {
                return;
            }

            battleHUD.SetAutoModeState(isAutoModeEnabled);
        }

        private bool ShouldPauseAutoMode()
        {
            return isAutoConfirmVisible ||
                   currentState is ScenarioDialogueState ||
                   // Onboarding can remain active between scripted steps without rendering a visible prompt.
                   // Auto mode should only pause for a prompt the player can actually respond to.
                   BuildCurrentOnboardingModel() != null ||
                   battleHUD != null && battleHUD.HasAutoModePauseOverlay;
        }

        private bool IsPlayerManualControlState()
        {
            return currentState is PlayerTurnStartState ||
                   currentState is UnitSelectionState ||
                   currentState is UnitMoveSelectState ||
                   currentState is UnitActionMenuState ||
                   currentState is UnitTargetSelectState ||
                   currentState is UnitSkillTargetState;
        }

        private void PrepareAutoModeTakeover()
        {
            pendingCombatResult = null;
            pendingDuelResult = null;
            pendingSkillResult = null;
            HideActionMenu();
            ClearDecisionContext();
            ClearSelectionAndHighlights();
        }

        private void TryResumeAutoModeAfterPause()
        {
            if (!isAutoModeEnabled ||
                isAutoModeRunning ||
                simulation == null ||
                simulation.Context == null ||
                simulation.Context.CurrentTurnSide != TurnSide.Player ||
                !IsPlayerManualControlState())
            {
                return;
            }

            if (ShouldPauseAutoMode())
            {
                isAutoModePausedByOverlay = true;
                RefreshAutoModeUi();
                return;
            }

            PrepareAutoModeTakeover();
            ChangeState<UnitSelectionState>();
        }

        private string BuildAutoActionLog(UnitActionResult actionResult)
        {
            if (actionResult == null)
            {
                return LocalizationService.Text("ui.log.auto_waiting", "AI auto mode is waiting for the next opening.");
            }

            string unitName = GetUnitDisplayName(actionResult.UnitId);
            return actionResult.EndPosition != actionResult.StartPosition
                ? LocalizationService.Format("ui.log.auto_advanced", "{0} advanced under AI command.", unitName)
                : LocalizationService.Format("ui.log.auto_held", "{0} held position under AI command.", unitName);
        }

        private UnitRuntimeState GetSelectedUnit()
        {
            return string.IsNullOrEmpty(selectedUnitId) ? null : simulation.Context.GetUnit(selectedUnitId);
        }

        private Unit GetUnitView(string unitId)
        {
            return unitViews.TryGetValue(unitId, out Unit unitView) ? unitView : null;
        }

        private BattleHudDecisionContextModel UpdateHudModels()
        {
            if (battleHUD == null || simulation == null)
            {
                return new BattleHudDecisionContextModel();
            }

            BattleThreatProjection threatProjection = null;
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected != null)
            {
                threatProjection = BattleThreatAnalyzer.AnalyzeProjected(simulation.Context, selected, selected.Position);
            }

            currentOverviewModel = BuildOverviewModel();
            battleHUD.BindOverview(currentOverviewModel);
            battleHUD.BindSelectedUnit(BuildSelectedUnitModel(selected, threatProjection));
            battleHUD.BindRoster(
                BuildRosterEntries(UnitFaction.Player, threatProjection),
                BuildRosterEntries(UnitFaction.Enemy, threatProjection),
                HandleHudUnitRequested);
            BattleHudDecisionContextModel decisionContextModel = BuildDecisionContextModel();
            battleHUD.BindDecisionContextModel(decisionContextModel);
            RefreshAutoModeUi();
            RefreshOnboardingPrompt();
            return decisionContextModel;
        }

        private IEnumerator PlaySkillSequence(SkillResult skillResult, TurnSide actingSide)
        {
            yield return presentationController.PlaySkillSequence(
                simulation,
                battleHUD,
                GetUnitView,
                skillResult,
                actingSide,
                GetSkillBarkText,
                RefreshAllVisuals);
        }

        private IEnumerator PlayDuelSequence(DuelResult duelResult, TurnSide actingSide)
        {
            yield return presentationController.PlayDuelSequence(
                simulation,
                battleHUD,
                GetUnitView,
                duelResult,
                actingSide,
                RefreshAllVisuals);
        }

        private IEnumerator PlayCombatSequence(CombatResult combatResult, TurnSide actingSide)
        {
            yield return presentationController.PlayCombatSequence(
                simulation,
                battleHUD,
                GetUnitView,
                combatResult,
                actingSide,
                RefreshAllVisuals);
        }

        private string BuildCombatLog(CombatResult combatResult)
        {
            return hudModelBuilder.BuildCombatLog(simulation, combatResult);
        }

        private string BuildDuelLog(DuelResult duelResult)
        {
            return hudModelBuilder.BuildDuelLog(simulation, duelResult);
        }

        private string BuildSkillLog(SkillResult skillResult)
        {
            return hudModelBuilder.BuildSkillLog(simulation, skillResult);
        }

        private BattleOverviewModel BuildOverviewModel()
        {
            return hudModelBuilder.BuildOverviewModel(
                simulation,
                scenarioData,
                scenarioDirector,
                currentTurnText,
                currentInstructionText);
        }

        private BattleSelectedUnitModel BuildSelectedUnitModel(UnitRuntimeState selected, BattleThreatProjection threatProjection)
        {
            return hudModelBuilder.BuildSelectedUnitModel(simulation, selected, threatProjection);
        }

        private IReadOnlyList<BattleRosterEntryModel> BuildRosterEntries(UnitFaction faction, BattleThreatProjection threatProjection)
        {
            return hudModelBuilder.BuildRosterEntries(simulation, faction, selectedUnitId, threatProjection);
        }

        private void HandleHudUnitRequested(string unitId)
        {
            if (IsInteractionLocked())
            {
                return;
            }

            Unit unitView = GetUnitView(unitId);
            if (unitView == null)
            {
                return;
            }

            currentState?.OnUnitClicked(unitView);
        }

        public void PreviewAttackTarget(Unit unitView, bool isHovered)
        {
            if (!isHovered)
            {
                ClearTargetPreview();
                return;
            }

            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null || unitView == null || unitView.RuntimeState == null)
            {
                ClearTargetPreview();
                return;
            }

            UnitRuntimeState target = unitView.RuntimeState;
            if (target.Faction == selected.Faction || !simulation.GetAttackableTargets(selected.Id).Any(candidate => candidate.Id == target.Id))
            {
                ClearTargetPreview();
                return;
            }

            BattleIntentPreview preview = simulation.PreviewAttackIntent(selected.Id, target.Id);
            if (preview == null || !preview.CanCommit)
            {
                ClearTargetPreview();
                return;
            }

            BuildAndBindDecisionContext(
                BattleDecisionContextSource.AttackHover,
                selected,
                target,
                preview,
                preview.BlockReason,
                GetDuelPreviewHint(selected.Id, target.Id));
        }

        public void PreviewQuickAttackTarget(Unit unitView, bool isHovered)
        {
            if (!isHovered)
            {
                RestoreMoveSelectionPreview();
                return;
            }

            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null || unitView == null || unitView.RuntimeState == null)
            {
                RestoreMoveSelectionPreview();
                return;
            }

            UnitRuntimeState target = unitView.RuntimeState;
            if (target.Faction == selected.Faction)
            {
                RestoreMoveSelectionPreview();
                return;
            }

            BattleIntentPreview preview = simulation.PreviewQuickAttackIntent(selected.Id, target.Id);
            if (preview == null || !preview.CanCommit)
            {
                RestoreMoveSelectionPreview();
                return;
            }

            ApplyQuickAttackPreview(selected, target, preview);
            BuildAndBindDecisionContext(
                BattleDecisionContextSource.AttackHover,
                selected,
                target,
                preview,
                preview.BlockReason,
                GetDuelPreviewHint(selected.Id, target.Id));
        }

        public void PreviewSkillTarget(Unit unitView, bool isHovered)
        {
            if (!isHovered)
            {
                ClearTargetPreview();
                ShowSkillRangeForSelection();
                return;
            }

            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null || unitView == null || unitView.RuntimeState == null)
            {
                ClearTargetPreview();
                ShowSkillRangeForSelection();
                return;
            }

            UnitRuntimeState target = unitView.RuntimeState;
            if (!simulation.GetSkillTargets(selected.Id).Any(candidate => candidate.Id == target.Id))
            {
                ClearTargetPreview();
                ShowSkillRangeForSelection();
                return;
            }

            BattleIntentPreview preview = simulation.PreviewSkillIntent(selected.Id, target.Id);
            if (preview == null || !preview.CanCommit)
            {
                ClearTargetPreview();
                ShowSkillRangeForSelection();
                return;
            }

            BuildAndBindDecisionContext(
                BattleDecisionContextSource.SkillHover,
                selected,
                target,
                preview,
                preview.BlockReason);
            RefreshSkillTargetPreview(selected, target);
        }

        public void ClearTargetPreview()
        {
            ClearDecisionContext();
        }

        private void RestoreMoveSelectionPreview()
        {
            ClearTargetPreview();
            ShowMoveRangeForSelection();
        }

        private void ApplyMovePreview(UnitRuntimeState selected, BattleIntentPreview preview)
        {
            if (selected == null || preview == null || gridManager == null)
            {
                return;
            }

            gridManager.ClearHighlights();
            gridManager.ShowMoveRange(simulation.GetMoveDestinations(selected.Id));
            gridManager.HighlightSelectedCell(selected.Position);
            gridManager.ShowPathPreview(preview.Path.Skip(1));
            gridManager.HighlightPreviewDestination(preview.Destination);
        }

        private void ApplyQuickAttackPreview(UnitRuntimeState selected, UnitRuntimeState target, BattleIntentPreview preview)
        {
            if (selected == null || target == null || preview == null || gridManager == null)
            {
                return;
            }

            ApplyMovePreview(selected, preview);
            gridManager.ShowAttackRange(new[] { target.Position });
        }

        private void RefreshSkillTargetPreview(UnitRuntimeState caster, UnitRuntimeState target)
        {
            if (caster == null || target == null)
            {
                return;
            }

            if (gridManager == null || simulation == null)
            {
                return;
            }

            IReadOnlyList<UnitRuntimeState> affectedUnits = simulation.GetSkillAffectedTargets(caster.Id, target.Id);
            IReadOnlyList<GridPosition> affectedPositions = simulation.GetSkillAffectedPositions(caster.Id, target.Id);
            gridManager.ClearHighlights();
            gridManager.ShowSkillRange(simulation.GetSkillRange(caster.Id));
            gridManager.HighlightSelectedCell(caster.Position);

            if (affectedPositions.Count > 0)
            {
                gridManager.ShowAttackRange(affectedPositions);
            }
        }

        private void PushBattleFeedEntry(string text)
        {
            battleHUD.PushFeedEntry(text);
        }

        private string GetUnitDisplayName(string unitId)
        {
            UnitRuntimeState unit = simulation.Context.GetUnit(unitId);
            return unit != null
                ? LocalizationService.Text(unit.DisplayNameKey, unit.DisplayName)
                : unitId;
        }

        private List<string> EvaluateAchievedBonusRewardLines(BattleResultSummary summary)
        {
            List<string> lines = new List<string>();
            if (summary == null || summary.WinningSide != TurnSide.Player || scenarioData?.BonusRewards == null)
            {
                return lines;
            }

            foreach (BonusRewardDefinition reward in scenarioData.BonusRewards.Where(reward => reward != null))
            {
                if (!reward.IsSatisfied(summary))
                {
                    continue;
                }

                ItemDefinition itemDefinition = ItemCatalog.Get(reward.RewardItemId);
                lines.Add(LocalizationService.Format(
                    "ui.result.special_line",
                    "{0} -> {1}",
                    LocalizationService.Text(reward.SummaryKey, reward.SummaryFallback),
                    itemDefinition != null
                        ? LocalizationService.Text(itemDefinition.NameKey, itemDefinition.NameFallback)
                        : reward.RewardItemId));
            }

            return lines;
        }

        private string GetSkillBarkText(string unitId, ActiveSkillType skillType)
        {
            string unitSpecificKey = "skill_bark." + unitId + "." + skillType;
            string unitSpecific = LocalizationService.Text(unitSpecificKey, unitSpecificKey);
            if (!string.Equals(unitSpecific, unitSpecificKey, StringComparison.Ordinal))
            {
                return unitSpecific;
            }

            string genericKey = "skill_bark.generic." + skillType;
            string generic = LocalizationService.Text(genericKey, genericKey);
            return string.Equals(generic, genericKey, StringComparison.Ordinal) ? string.Empty : generic;
        }

        private string GetDuelPreviewHint(string attackerUnitId, string defenderUnitId)
        {
            DuelSceneDefinition duelScene = scenarioDirector != null
                ? scenarioDirector.TryMatchDuel(attackerUnitId, defenderUnitId, simulation != null ? simulation.Context : null)
                : null;
            return duelScene != null
                ? LocalizationService.Text("ui.duel.preview_ready", "一騎可觸發")
                : string.Empty;
        }

        private BattleResultSummary BuildBattleResultSummary()
        {
            IReadOnlyList<string> survivingUnitIds = simulation.Context.GetUnits(UnitFaction.Player)
                .Select(unit => unit.Id)
                .ToList();

            return new BattleResultSummary(
                scenarioData != null ? scenarioData.ScenarioId : string.Empty,
                simulation.Context.WinningSide,
                simulation.Context.RoundNumber,
                survivingUnitIds,
                scenarioDirector != null ? scenarioDirector.ActiveFlags : Array.Empty<string>(),
                scenarioDirector != null ? scenarioDirector.TriggeredDuelIds : Array.Empty<string>());
        }

        private void ChangeState(Type stateType)
        {
            if (!states.TryGetValue(stateType, out IBattleState nextState))
            {
                throw new InvalidOperationException($"State {stateType.Name} is not registered.");
            }

            currentState?.Exit();
            currentState = nextState;
            currentState.Enter();
            RefreshOnboardingPrompt();
        }

        private void LoadScenario(BattleScenarioData data)
        {
            StopAllCoroutines();
            currentState?.Exit();
            currentState = null;
            ResetAutoModeState();
            selectedUnitId = null;
            pendingCombatResult = null;
            pendingSkillResult = null;
            pendingDialogueResumeState = null;
            currentTurnText = LocalizationService.Text("ui.turn.player", "Turn: Player Phase");
            currentInstructionText = string.Empty;
            battleRewardsGranted = false;
            battleResultSummaryText = string.Empty;
            onboardingController = enableFirstBattleOnboarding &&
                                   data != null &&
                                   string.Equals(data.ScenarioId, FirstBattleOnboardingController.GuangzongScenarioId, StringComparison.Ordinal)
                ? new FirstBattleOnboardingController(string.Empty)
                : null;

            ClearDecisionContext();
            battleHUD.HideResult();
            battleHUD.HideDialogue();
            battleHUD.HideCampaignOverlay();
            battleHUD.HideOnboarding();
            battleHUD.HideConfirmDialog();

            scenarioData = data;
            simulation = new BattleSimulation(scenarioData.Stage);
            scenarioDirector = new ScenarioDirector(scenarioData);
            gridManager.BuildGrid(simulation.Context, OnCellClicked, OnCellHoverChanged);
            CreateUnits();
            ConfigureCamera();
            battleHUD.SetRerollEnabled(simulation.Context.IsRandomMap);
            RefreshAllVisuals();
        }

        private BattleResultModel BuildBattleResultModel(string title)
        {
            GrantBattleRewards();
            return hudModelBuilder.BuildBattleResultModel(
                simulation,
                scenarioData,
                scenarioDirector,
                title,
                battleResultRewardLines,
                battleResultUnitLines,
                battleResultSpecialLines);
        }

        private void GrantBattleRewards()
        {
            if (battleRewardsGranted || simulation == null || scenarioData == null)
            {
                return;
            }

            int objectiveReward = ExperienceSystem.GetObjectiveReward(scenarioData, simulation.Context.WinningSide == TurnSide.Player);
            foreach (UnitRuntimeState unit in simulation.Context.GetUnits(UnitFaction.Player, false))
            {
                unit.BondState.RegisterBattle();
                int totalReward = objectiveReward + ExperienceSystem.GetParticipationReward(unit);
                unit.AddBonusExperience(totalReward);
            }

            battleRewardsGranted = true;
            battleResultRewardLines.Clear();
            battleResultSpecialLines.Clear();
            battleResultUnitLines.Clear();
            BuildBattleRewardSections(objectiveReward, battleResultRewardLines, battleResultSpecialLines, battleResultUnitLines);
            battleResultSummaryText = BuildBattleRewardSummary(objectiveReward);
        }

        private string BuildBattleRewardSummary(int objectiveReward)
        {
            List<string> lines = new List<string>();
            lines.AddRange(battleResultRewardLines);
            lines.AddRange(battleResultSpecialLines);
            lines.AddRange(battleResultUnitLines);
            return string.Join("\n", lines);
        }

        private void BuildBattleRewardSections(int objectiveReward, List<string> rewardLines, List<string> specialLines, List<string> unitLines)
        {
            if (rewardLines == null || specialLines == null || unitLines == null)
            {
                return;
            }

            rewardLines.Add(LocalizationService.Text("ui.result.section.rewards", "Battle Rewards"));
            rewardLines.Add(LocalizationService.Format("ui.result.objective_exp", "Objective reward +{0} EXP", objectiveReward));

            if (simulation.Context.WinningSide == TurnSide.Player &&
                scenarioData != null &&
                scenarioData.RewardBundle != null &&
                scenarioData.RewardBundle.HasAnyReward)
            {
                rewardLines.Add(LocalizationService.Format(
                    "ui.result.rewards",
                    "Rewards: Supplies {0}  Renown {1}",
                    scenarioData.RewardBundle.Supplies,
                    scenarioData.RewardBundle.Renown));
            }

            BattleResultSummary summary = BuildBattleResultSummary();
            List<string> achievedBonusLines = EvaluateAchievedBonusRewardLines(summary);
            if (achievedBonusLines.Count > 0)
            {
                specialLines.Add(LocalizationService.Text("ui.result.section.special", "Special Achievements"));
                specialLines.AddRange(achievedBonusLines);
            }

            unitLines.Add(LocalizationService.Text("ui.result.section.units", "Officer Progress"));
            foreach (UnitRuntimeState unit in simulation.Context.GetUnits(UnitFaction.Player, false).OrderBy(unit => unit.Id))
            {
                unitLines.Add(BuildUnitBattleRewardSummary(unit, objectiveReward));
            }
        }

        private string BuildUnitBattleRewardSummary(UnitRuntimeState unit, int objectiveReward)
        {
            int totalGain = unit.BattleExpEarned + objectiveReward + ExperienceSystem.GetParticipationReward(unit);
            string summary = LocalizationService.Format(
                "ui.result.unit_summary",
                "{0}  等級 {1}  經驗 +{2}  ({3}/{4})",
                GetUnitDisplayName(unit.Id),
                unit.Level,
                totalGain,
                unit.CurrentExp,
                unit.NextLevelExp);

            List<string> unlocks = new List<string>();
            GrowthProfileDefinition growthProfile = GrowthProfileCatalog.Get(unit.GrowthProfileId);
            if (unit.BattleStartLevel < growthProfile.AdvancedSkillUnlockLevel && unit.Level >= growthProfile.AdvancedSkillUnlockLevel)
            {
                unlocks.Add(LocalizationService.Text("ui.unlock.skill_mastery", "Skill Mastery"));
            }

            if (unit.BattleStartLevel < growthProfile.TraitSlotUnlockLevel && unit.Level >= growthProfile.TraitSlotUnlockLevel)
            {
                unlocks.Add(LocalizationService.Text("ui.unlock.trait_slot", "Trait Slot"));
            }

            if (unit.BattleStartLevel < growthProfile.PromotionUnlockLevel && unit.Level >= growthProfile.PromotionUnlockLevel)
            {
                unlocks.Add(LocalizationService.Text("ui.unlock.promotion", "Promotion Ready"));
            }

            if (unit.BattleStartLevel < growthProfile.SignaturePassiveUnlockLevel && unit.Level >= growthProfile.SignaturePassiveUnlockLevel)
            {
                unlocks.Add(LocalizationService.Text("ui.unlock.signature", "Signature Passive"));
            }

            return unlocks.Count == 0
                ? summary
                : summary + " | " + string.Join(", ", unlocks);
        }

        private void HandleOnboardingSkipRequested()
        {
            if (onboardingController == null || !onboardingController.IsActive)
            {
                return;
            }

            onboardingController.Skip();
            battleHUD.HideOnboarding();
            SyncUnitInfoVisibility();
            firstBattleOnboardingResolvedHandler?.Invoke(true);
            firstBattleOnboardingResolvedHandler = null;
            TryResumeAutoModeAfterPause();
        }

        private void RefreshOnboardingPrompt()
        {
            if (battleHUD == null)
            {
                return;
            }

            BattleOnboardingModel model = BuildCurrentOnboardingModel();
            if (model == null)
            {
                battleHUD.HideOnboarding();
                SyncUnitInfoVisibility();
                return;
            }

            battleHUD.ShowOnboarding(model);
            SyncUnitInfoVisibility();
        }

        private BattleOnboardingModel BuildCurrentOnboardingModel()
        {
            if (onboardingController == null || !onboardingController.IsActive || IsInteractionLocked())
            {
                return null;
            }

            return onboardingController.BuildModel(
                !string.IsNullOrEmpty(selectedUnitId),
                HasSelectionMoved(),
                HasAttackTargetsForSelection(),
                AnyPlayerUnitCanAttack(),
                AreAllPlayerUnitsDone());
        }

        private bool AnyPlayerUnitCanAttack()
        {
            if (simulation == null)
            {
                return false;
            }

            return simulation.Context.GetUnits(UnitFaction.Player)
                .Any(unit => !unit.HasActed && simulation.GetAttackableTargets(unit.Id).Count > 0);
        }

        private string GetCurrentPrimaryObjectiveText()
        {
            ObjectiveState objective = scenarioDirector != null ? scenarioDirector.CurrentObjective : null;
            return objective == null
                ? string.Empty
                : LocalizationService.Text(objective.PrimaryObjectiveKey, objective.PrimaryObjectiveFallback);
        }

        private bool IsInteractionLocked()
        {
            return isAutoModeRunning ||
                   isAutoConfirmVisible ||
                   currentState is ScenarioDialogueState ||
                   battleHUD != null && battleHUD.HasBlockingInteractionOverlay;
        }

        private bool ShouldSuppressUnitInfo()
        {
            return (battleHUD != null && battleHUD.ShouldSuppressUnitInfo()) || IsActionMenuVisible;
        }

        private void SyncUnitInfoVisibility()
        {
            bool suppressUnitInfo = ShouldSuppressUnitInfo();
            foreach (Unit unitView in unitViews.Values)
            {
                if (unitView != null)
                {
                    unitView.SetInfoSuppressed(suppressUnitInfo);
                }
            }
        }

        private ScenarioEvaluationResult ProcessScenarioCheckpoint(ScenarioCheckpoint checkpoint)
        {
            ScenarioEvaluationResult result = scenarioDirector != null
                ? scenarioDirector.Evaluate(checkpoint, simulation.Context)
                : new ScenarioEvaluationResult(Array.Empty<string>(), false, false, false, false);

            UpdateHudModels();
            onboardingController?.UpdateObjective(GetCurrentPrimaryObjectiveText());
            RefreshOnboardingPrompt();
            if (result.BattlefieldChanged)
            {
                gridManager.BuildGrid(simulation.Context, OnCellClicked, OnCellHoverChanged);
                ConfigureCamera();
                RefreshAllVisuals();
            }
            else if (result.SpawnedUnitIds.Count > 0)
            {
                RefreshAllVisuals();
            }

            return result;
        }

        private bool TryEnterScenarioDialogue(Type resumeStateType)
        {
            if (scenarioDirector == null || !scenarioDirector.HasPendingDialogue)
            {
                return false;
            }

            isAutoModePausedByOverlay = isAutoModeEnabled;
            RefreshAutoModeUi();
            pendingDialogueResumeState = resumeStateType;
            ChangeState<ScenarioDialogueState>();
            return true;
        }

        private void ResumeAfterScenarioDialogue()
        {
            Type resumeStateType = pendingDialogueResumeState ?? typeof(UnitSelectionState);
            pendingDialogueResumeState = null;
            ChangeState(resumeStateType);
        }

        private bool HandlePostActionScenarioFlow(Type defaultResumeStateType)
        {
            ProcessScenarioCheckpoint(ScenarioCheckpoint.ActionResolved);
            if (simulation.Context.BattleEnded)
            {
                isAutoModeRunning = false;
                isAutoModePausedByOverlay = false;
                RefreshAutoModeUi();
                onboardingController?.MarkBattleCompleted();
                if (onboardingController != null && !onboardingController.WasSkipped)
                {
                    firstBattleOnboardingResolvedHandler?.Invoke(false);
                    firstBattleOnboardingResolvedHandler = null;
                }

                ProcessScenarioCheckpoint(ScenarioCheckpoint.PreBattleOutcome);
                Type terminalState = simulation.Context.WinningSide == TurnSide.Player
                    ? typeof(BattleVictoryState)
                    : typeof(BattleDefeatState);
                if (TryEnterScenarioDialogue(terminalState))
                {
                    return true;
                }

                ClearDecisionContext();
                ChangeState(terminalState);
                return true;
            }

            return TryEnterScenarioDialogue(defaultResumeStateType);
        }

        private BattleHudDecisionContextModel BindDecisionContext(BattleDecisionContext context)
        {
            currentDecisionContext = context ?? new BattleDecisionContext();
            return UpdateHudModels();
        }

        private void ClearDecisionContext()
        {
            currentDecisionContext = new BattleDecisionContext();
            if (battleHUD == null)
            {
                return;
            }

            if (simulation == null)
            {
                battleHUD.ClearContext();
                return;
            }

            UpdateHudModels();
        }

        private BattleHudDecisionContextModel BuildAndBindDecisionContext(
            BattleDecisionContextSource sourceState,
            UnitRuntimeState actor,
            UnitRuntimeState target,
            BattleIntentPreview preview,
            string rationale,
            string specialHintText = "")
        {
            return BindDecisionContext(
                new BattleDecisionContext
                {
                    SourceState = sourceState,
                    ActorUnitId = actor != null ? actor.Id : string.Empty,
                    TargetUnitId = target != null ? target.Id : string.Empty,
                    Rationale = rationale ?? string.Empty,
                    Preview = preview,
                    SpecialHintText = specialHintText ?? string.Empty,
                });
        }

        private BattleHudDecisionContextModel BuildDecisionContextModel()
        {
            return hudModelBuilder.BuildDecisionContextModel(
                simulation,
                currentDecisionContext,
                GetSelectedUnit(),
                HasSelectionMoved(),
                currentOverviewModel,
                battleHUD != null ? battleHUD.FeedEntries : Array.Empty<string>());
        }
    }
}
