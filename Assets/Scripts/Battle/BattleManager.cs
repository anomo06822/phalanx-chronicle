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
        private ActionMenuPanel actionMenuPanel;
        private IBattleState currentState;
        private string selectedUnitId;
        private GridPosition selectedUnitOrigin;
        private bool hasSelectedUnitOrigin;
        private CombatResult pendingCombatResult;
        private SkillResult pendingSkillResult;
        private bool initialized;
        private GameObject unitRoot;
        private Type pendingDialogueResumeState;
        private string currentTurnText = string.Empty;
        private string currentInstructionText = string.Empty;
        private bool autoStartScenario = true;
        private bool battleRewardsGranted;
        private string battleResultSummaryText = string.Empty;

        public bool IsDialogueVisible => battleHUD != null && battleHUD.IsDialogueVisible;

        public bool IsRerollVisible => battleHUD != null && battleHUD.IsRerollVisible;

        public bool IsResultVisible => battleHUD != null && battleHUD.IsResultVisible;

        public bool IsCampaignOverlayVisible => battleHUD != null && battleHUD.IsCampaignOverlayVisible;

        public bool IsActionMenuVisible => actionMenuPanel != null && actionMenuPanel.IsVisible;

        public string CurrentActionMenuModeText => actionMenuPanel != null ? actionMenuPanel.CurrentModeText : string.Empty;

        public bool IsActionMenuBackEnabled => actionMenuPanel != null && actionMenuPanel.IsBackEnabled;

        public bool IsActionMenuSkillEnabled => actionMenuPanel != null && actionMenuPanel.IsSkillEnabled;

        public string CurrentObjectiveText => battleHUD != null ? battleHUD.CurrentObjectiveText : string.Empty;

        public BattleSimulation Simulation => simulation;

        public BattleScenarioData CurrentScenarioData => scenarioData;

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
        }

        public void ShowCampaignInterlude(CampaignInterludeModel model, Action onPrimary, Action onSecondary = null)
        {
            battleHUD.ShowCampaignInterlude(model, onPrimary, onSecondary);
        }

        public void ShowCampaignOptionList(CampaignOptionListModel model, Action<string> onOptionSelected, Action onPrimary, Action onSecondary = null)
        {
            battleHUD.ShowCampaignOptionList(model, onOptionSelected, onPrimary, onSecondary);
        }

        public void HideCampaignOverlay()
        {
            battleHUD.HideCampaignOverlay();
        }

        public void ConfirmBattleResult()
        {
            if (simulation == null || scenarioData == null)
            {
                return;
            }

            battleHUD.HideResult();
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
            battleHUD.SetEndTurnEnabled(enabled);
        }

        public void SetRerollEnabled(bool enabled)
        {
            battleHUD.SetRerollEnabled(enabled);
        }

        public void ShowResult(string text)
        {
            battleHUD.ShowResult(BuildBattleResultText(text));
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
            actionMenuPanel.Show(
                BuildActionMenuModel(),
                HandleAttackRequested,
                HandleSkillRequested,
                HandleWaitRequested,
                HandleBackRequested);
        }

        public void HideActionMenu()
        {
            actionMenuPanel.Hide();
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

            pendingSkillResult = null;
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

        public IEnumerator ExecutePendingPlayerAction()
        {
            CombatResult combatResult = ConsumePendingCombatResult();
            if (combatResult != null)
            {
                yield return PlayCombatSequence(combatResult);
                ResolvePlayerAction(BuildCombatLog(combatResult));
                yield break;
            }

            SkillResult skillResult = ConsumePendingSkillResult();
            if (skillResult != null)
            {
                yield return PlaySkillSequence(skillResult);
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
                ? LocalizationService.Text("ui.log.choose_action_moved", "Choose Attack, Skill, Wait, or Undo Move after moving.")
                : LocalizationService.Text("ui.log.choose_action_hold", "Choose Attack, Skill, or Wait without moving.");
        }

        private BattleActionMenuModel BuildActionMenuModel()
        {
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null)
            {
                return new BattleActionMenuModel();
            }

            bool canAttack = HasAttackTargetsForSelection();
            bool canUseSkill = HasSkillTargetsForSelection();
            bool moved = HasSelectionMoved();
            string skillName = LocalizationService.Text(selected.ActiveSkillNameKey, selected.ActiveSkill.ToString());

            return new BattleActionMenuModel
            {
                Mode = moved ? BattleActionMenuMode.AfterMove : BattleActionMenuMode.HoldPosition,
                ModeLabel = LocalizationService.Text(
                    moved ? "ui.action_menu.mode.moved" : "ui.action_menu.mode.hold",
                    moved ? "After Move" : "Hold Position"),
                CanAttack = canAttack,
                AttackDetail = LocalizationService.Text(
                    canAttack ? "ui.action_menu.attack.ready" : "ui.action_menu.attack.unavailable",
                    canAttack ? "Target in range" : "No target in range"),
                SkillName = skillName,
                CanUseSkill = canUseSkill,
                SkillDetail = BuildSkillActionMenuDetail(selected, canUseSkill),
                WaitDetail = LocalizationService.Text("ui.action_menu.wait.detail", "End this unit's action"),
                CanBack = moved,
                BackLabel = moved
                    ? LocalizationService.Text("ui.button.undo_move", "Undo Move")
                    : LocalizationService.Text("ui.button.back", "Back"),
                BackDetail = LocalizationService.Text(
                    moved ? "ui.action_menu.back.ready" : "ui.action_menu.back.unavailable",
                    moved ? "Return to the original tile" : "No move to undo"),
            };
        }

        public void ResolvePlayerAction(string logMessage)
        {
            SetLog(logMessage);
            PushBattleFeedEntry(logMessage);
            HideActionMenu();
            battleHUD.ClearForecast();
            ClearSelectionAndHighlights();

            Type nextStateType = AreAllPlayerUnitsDone() ? typeof(EnemyTurnState) : typeof(UnitSelectionState);
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
            ClearSelectionAndHighlights();
            HideActionMenu();
            SetTurnLabel(LocalizationService.Text("ui.turn.enemy", "Turn: Enemy Phase"));
            SetEndTurnEnabled(false);
            SetLog(LocalizationService.Text("ui.log.enemy_acting", "Enemy forces are acting."));

            List<UnitRuntimeState> enemies = simulation.GetUnits(UnitFaction.Enemy).ToList();
            foreach (UnitRuntimeState enemy in enemies)
            {
                if (!enemy.IsAlive)
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
                    yield return PlayCombatSequence(actionResult.CombatResult);
                    string logText = BuildCombatLog(actionResult.CombatResult);
                    SetLog(logText);
                    PushBattleFeedEntry(logText);
                }
                else if (actionResult.PerformedSkill)
                {
                    yield return PlaySkillSequence(actionResult.SkillResult);
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

                yield return new WaitForSeconds(0.35f);

                if (HandlePostActionScenarioFlow(typeof(EnemyTurnState)))
                {
                    yield break;
                }
            }

            simulation.EndCurrentTurn();
            battleHUD.ClearForecast();
            ChangeState<PlayerTurnStartState>();
        }

        public void RefreshAllVisuals()
        {
            if (simulation == null || gridManager == null)
            {
                return;
            }

            EnsureUnitViews();
            foreach (KeyValuePair<string, Unit> entry in unitViews)
            {
                UnitRuntimeState runtimeState = simulation.Context.GetUnit(entry.Key);
                if (runtimeState == null)
                {
                    continue;
                }

                entry.Value.SetSelected(entry.Key == selectedUnitId);
                entry.Value.Sync(gridManager.GetWorldPosition(runtimeState.Position));
            }

            UpdateHudModels();
        }

        private void Awake()
        {
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
                HandleDialogueAdvanceRequested,
                HandleResultAdvanceRequested);

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
            float boardWidth = simulation.Context.Width + 2f;
            float boardHeight = simulation.Context.Height + 2.1f;
            float aspect = Mathf.Max(0.1f, mainCamera.aspect);
            const float leftHudReserve = 0.23f;
            const float rightHudReserve = 0.22f;
            const float topHudReserve = 0.15f;
            const float bottomHudReserve = 0.06f;
            float usableWidth = Mathf.Max(0.2f, 1f - leftHudReserve - rightHudReserve);
            float usableHeight = Mathf.Max(0.2f, 1f - topHudReserve - bottomHudReserve);
            float orthographicSizeForHeight = (boardHeight * 0.5f) / usableHeight;
            float orthographicSizeForWidth = (boardWidth * 0.5f) / (aspect * usableWidth);
            float orthographicSize = Mathf.Max(orthographicSizeForHeight, orthographicSizeForWidth);
            float verticalOffset = (topHudReserve - bottomHudReserve) * orthographicSize * 0.45f;

            mainCamera.transform.position = new Vector3(0f, verticalOffset, -10f);
            mainCamera.orthographicSize = orthographicSize;
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

        private UnitRuntimeState GetSelectedUnit()
        {
            return string.IsNullOrEmpty(selectedUnitId) ? null : simulation.Context.GetUnit(selectedUnitId);
        }

        private Unit GetUnitView(string unitId)
        {
            return unitViews.TryGetValue(unitId, out Unit unitView) ? unitView : null;
        }

        private void UpdateHudModels()
        {
            if (battleHUD == null || simulation == null)
            {
                return;
            }

            BattleThreatSummary threatSummary = null;
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected != null)
            {
                threatSummary = BattleThreatAnalyzer.Analyze(simulation.Context, selected);
            }

            battleHUD.BindOverview(BuildOverviewModel());
            battleHUD.BindSelectedUnit(BuildSelectedUnitModel(selected, threatSummary));
            battleHUD.BindRoster(
                BuildRosterEntries(UnitFaction.Player, threatSummary),
                BuildRosterEntries(UnitFaction.Enemy, threatSummary),
                HandleHudUnitRequested);
        }

        private IEnumerator PlaySkillSequence(SkillResult skillResult)
        {
            if (skillResult == null)
            {
                yield break;
            }

            Unit casterView = GetUnitView(skillResult.CasterUnitId);
            Unit primaryTargetView = GetUnitView(skillResult.PrimaryTargetUnitId);
            UnitRuntimeState casterState = simulation.Context.GetUnit(skillResult.CasterUnitId);
            battleHUD.BindForecast(BuildSkillResultForecastModel(skillResult));

            if (casterView != null)
            {
                string barkText = GetSkillBarkText(skillResult.CasterUnitId, skillResult.SkillType);
                if (!string.IsNullOrWhiteSpace(barkText))
                {
                    FloatingText.Spawn(barkText, casterView.GetAnchorPosition(1.32f), new Color(1f, 0.92f, 0.72f, 1f));
                    yield return new WaitForSeconds(0.08f);
                }
            }

            if (casterView != null)
            {
                yield return SkillVisualEffects.PlayCasterEffect(skillResult.SkillType, casterView, primaryTargetView, casterState);
            }

            if (casterView != null && primaryTargetView != null && SkillVisualEffects.ShouldAnimateLunge(skillResult.SkillType))
            {
                yield return casterView.AnimateAttack(primaryTargetView.transform.position);
            }

            foreach (SkillEffectResult effect in skillResult.Effects)
            {
                Unit targetView = GetUnitView(effect.UnitId);
                if (targetView == null)
                {
                    continue;
                }

                if (effect.IsHealing)
                {
                    yield return SkillVisualEffects.PlayTargetEffect(skillResult.SkillType, targetView, effect.UnitId == skillResult.PrimaryTargetUnitId, casterState);
                    FloatingText.Spawn("+" + effect.Amount, targetView.GetAnchorPosition(0.98f), new Color(0.54f, 1f, 0.62f, 1f));
                    yield return targetView.AnimatePulse(new Color(0.7f, 1f, 0.78f, 1f));
                }
                else if (effect.Amount > 0)
                {
                    yield return SkillVisualEffects.PlayTargetEffect(skillResult.SkillType, targetView, effect.UnitId == skillResult.PrimaryTargetUnitId, casterState);
                    FloatingText.Spawn("-" + effect.Amount, targetView.GetAnchorPosition(0.98f), new Color(1f, 0.89f, 0.4f, 1f));
                    yield return targetView.AnimateHit();

                    if (effect.UnitDied)
                    {
                        FloatingText.Spawn(LocalizationService.Text("ui.combat.popup_ko", "KO"), targetView.GetAnchorPosition(1.24f), new Color(1f, 0.56f, 0.42f, 1f));
                    }
                }
                else
                {
                    yield return SkillVisualEffects.PlayTargetEffect(skillResult.SkillType, targetView, effect.UnitId == skillResult.PrimaryTargetUnitId, casterState);
                    yield return targetView.AnimatePulse(new Color(0.75f, 0.72f, 1f, 1f));
                }

                if (effect.AppliedStatuses.Count > 0)
                {
                    string statusFloatingText = BuildStatusFloatingText(effect.AppliedStatuses);
                    if (!string.IsNullOrWhiteSpace(statusFloatingText))
                    {
                        FloatingText.Spawn(statusFloatingText, targetView.GetAnchorPosition(1.18f), new Color(0.76f, 0.96f, 1f, 1f));
                    }
                }

                yield return new WaitForSeconds(0.02f);
            }

            if (casterView != null && skillResult.CasterExpGained > 0)
            {
                FloatingText.Spawn(FormatExpGainText(skillResult.CasterExpGained), casterView.GetAnchorPosition(1.2f), new Color(0.76f, 0.98f, 0.58f, 1f));
                if (skillResult.CasterLevelsGained > 0)
                {
                    FloatingText.Spawn(LocalizationService.Text("ui.exp.level_up", "LEVEL UP"), casterView.GetAnchorPosition(1.36f), new Color(0.98f, 0.9f, 0.52f, 1f));
                }
            }

            yield return new WaitForSeconds(0.12f);
            battleHUD.ClearForecast();
            RefreshAllVisuals();
        }

        private IEnumerator PlayCombatSequence(CombatResult combatResult)
        {
            if (combatResult == null)
            {
                yield break;
            }

            Unit attackerView = GetUnitView(combatResult.AttackerUnitId);
            Unit defenderView = GetUnitView(combatResult.DefenderUnitId);
            battleHUD.BindForecast(BuildCombatResultForecastModel(combatResult));

            if (attackerView != null && defenderView != null)
            {
                yield return attackerView.AnimateAttack(defenderView.transform.position);
                yield return PlaySlashEffect(defenderView.GetAnchorPosition(0.12f));
                FloatingText.Spawn("-" + combatResult.Damage, defenderView.GetAnchorPosition(0.98f), new Color(1f, 0.89f, 0.4f, 1f));
                yield return defenderView.AnimateHit();

                if (combatResult.DefenderDied)
                {
                    FloatingText.Spawn(LocalizationService.Text("ui.combat.popup_ko", "KO"), defenderView.GetAnchorPosition(1.24f), new Color(1f, 0.56f, 0.42f, 1f));
                }

                if (combatResult.AttackerExpGained > 0)
                {
                    FloatingText.Spawn(FormatExpGainText(combatResult.AttackerExpGained), attackerView.GetAnchorPosition(1.2f), new Color(0.76f, 0.98f, 0.58f, 1f));
                    if (combatResult.AttackerLevelsGained > 0)
                    {
                        FloatingText.Spawn(LocalizationService.Text("ui.exp.level_up", "LEVEL UP"), attackerView.GetAnchorPosition(1.36f), new Color(0.98f, 0.9f, 0.52f, 1f));
                    }
                }
            }

            yield return new WaitForSeconds(0.18f);
            battleHUD.ClearForecast();
            RefreshAllVisuals();
        }

        private IEnumerator PlaySlashEffect(Vector3 worldPosition)
        {
            GameObject slashObject = new GameObject("SlashEffect");
            slashObject.transform.position = new Vector3(worldPosition.x, worldPosition.y, -0.72f);
            slashObject.transform.rotation = Quaternion.Euler(0f, 0f, -18f);

            SpriteRenderer renderer = slashObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteLibrary.SlashSprite;
            renderer.sortingOrder = 55;

            float duration = 0.12f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float alpha = progress < 0.5f ? progress * 2f : (1f - progress) * 2f;
                renderer.color = new Color(1f, 0.95f, 0.78f, alpha);
                slashObject.transform.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.15f, progress);
                yield return null;
            }

            Destroy(slashObject);
        }

        private string BuildCombatLog(CombatResult combatResult)
        {
            string attackerName = GetUnitDisplayName(combatResult.AttackerUnitId);
            string defenderName = GetUnitDisplayName(combatResult.DefenderUnitId);
            return combatResult.DefenderDied
                ? LocalizationService.Format("ui.combat.log.ko", "{0} defeated {1}.", attackerName, defenderName)
                : LocalizationService.Format("ui.combat.log.damage", "{0} dealt {1} damage to {2}.", attackerName, combatResult.Damage, defenderName);
        }

        private string BuildSkillLog(SkillResult skillResult)
        {
            string casterName = GetUnitDisplayName(skillResult.CasterUnitId);
            string skillName = GetSkillDisplayName(skillResult.CasterUnitId, skillResult.SkillType);
            return LocalizationService.Format("ui.skill.log.use", "{0} used {1}.", casterName, skillName);
        }

        private BattleOverviewModel BuildOverviewModel()
        {
            ObjectiveState objective = scenarioDirector != null ? scenarioDirector.CurrentObjective : null;
            BattleContext context = simulation.Context;
            int playerAlive = context.GetUnits(UnitFaction.Player).Count;
            int enemyAlive = context.GetUnits(UnitFaction.Enemy).Count;
            int playerTotal = context.Units.Count(unit => unit.Faction == UnitFaction.Player);
            int enemyTotal = context.Units.Count(unit => unit.Faction == UnitFaction.Enemy);
            int readyUnits = context.Units.Count(unit => unit.Faction == UnitFaction.Player && unit.IsAlive && !unit.HasActed);
            int skillReadyUnits = context.Units.Count(unit =>
                unit.Faction == UnitFaction.Player &&
                unit.IsAlive &&
                !unit.HasActed &&
                unit.ActiveSkill != ActiveSkillType.None &&
                unit.HasEnoughMana(ActiveSkillRules.GetManaCost(unit)) &&
                simulation.GetSkillTargets(unit.Id).Count > 0);

            return new BattleOverviewModel
            {
                StageLabel = LocalizationService.Format("ui.stage", "Stage: {0}", LocalizationService.Text(context.StageNameKey, context.StageName)),
                SeedLabel = context.IsRandomMap
                    ? LocalizationService.Format("ui.seed.value", "Seed: {0}", context.MapSeed)
                    : LocalizationService.Text("ui.seed.fixed", "Seed: Fixed"),
                PhaseLabel = string.IsNullOrEmpty(currentTurnText)
                    ? LocalizationService.Text(
                        context.CurrentTurnSide == TurnSide.Player ? "ui.turn.player" : "ui.turn.enemy",
                        context.CurrentTurnSide == TurnSide.Player ? "Turn: Player Phase" : "Turn: Enemy Phase")
                    : currentTurnText,
                TurnLabel = LocalizationService.Format("ui.turn.count", "Turn {0}", context.TurnNumber),
                PlayerAliveLabel = LocalizationService.Format("ui.overview.player_force", "Allies {0}/{1}", playerAlive, playerTotal),
                EnemyAliveLabel = LocalizationService.Format("ui.overview.enemy_force", "Enemies {0}/{1}", enemyAlive, enemyTotal),
                ReadyLabel = LocalizationService.Format("ui.overview.ready_units", "Ready units {0}", readyUnits),
                SkillReadyLabel = LocalizationService.Format("ui.overview.skill_ready", "Skills ready {0}", skillReadyUnits),
                ObjectivePrimary = LocalizationService.Format(
                    "ui.objective.primary",
                    "Primary: {0}",
                    objective != null ? LocalizationService.Text(objective.PrimaryObjectiveKey, objective.PrimaryObjectiveFallback) : "-"),
                ObjectiveFailure = LocalizationService.Format(
                    "ui.objective.failure",
                    "Fail: {0}",
                    objective != null ? LocalizationService.Text(objective.FailureConditionKey, objective.FailureConditionFallback) : "-"),
                InstructionText = currentInstructionText,
            };
        }

        private BattleSelectedUnitModel BuildSelectedUnitModel(UnitRuntimeState selected, BattleThreatSummary threatSummary)
        {
            if (selected == null)
            {
                return new BattleSelectedUnitModel();
            }

            RoleLoadoutProfile loadoutProfile = RoleLoadoutCatalog.GetProfile(selected.Role);
            ItemDefinition weapon = ItemCatalog.Get(selected.EquipmentLoadout.WeaponId);
            ItemDefinition armor = ItemCatalog.Get(selected.EquipmentLoadout.ArmorId);
            ItemDefinition mount = ItemCatalog.Get(selected.EquipmentLoadout.MountId);
            TerrainType terrainType = simulation.Context.GetTerrainAt(selected.Position);
            bool hasSkillTargets = selected.ActiveSkill != ActiveSkillType.None &&
                                   selected.HasEnoughMana(ActiveSkillRules.GetManaCost(selected)) &&
                                   simulation.GetSkillTargets(selected.Id).Count > 0;
            string skillStatusText = BuildSkillAvailabilityLabel(selected, hasSkillTargets);
            string terrainName = GetTerrainDisplayName(terrainType);
            string terrainEffectSummary = BuildTerrainEffectSummary(selected, terrainType);

            string threatSummaryText = threatSummary == null || threatSummary.ThreateningEnemyCount == 0
                ? LocalizationService.Text("ui.threat.none", "No immediate enemy threat.")
                : LocalizationService.Format(
                    "ui.threat.summary",
                    "Threats {0} | Max incoming {1}",
                    threatSummary.ThreateningEnemyCount,
                    threatSummary.MaxProjectedDamage);
            string threatDetailText = threatSummary == null || threatSummary.ThreateningEnemyCount == 0
                ? string.Empty
                : LocalizationService.Format(
                    "ui.threat.detail",
                    "Threatened by: {0}",
                    string.Join(", ", threatSummary.ThreateningUnitIds.Select(GetUnitDisplayName)));

            return new BattleSelectedUnitModel
            {
                HasSelection = true,
                UnitId = selected.Id,
                DisplayName = GetUnitDisplayName(selected.Id),
                Role = selected.Role,
                RoleLabel = LocalizationService.Format(
                    "ui.selected.role_level",
                    "{0}  Lv {1}",
                    LocalizationService.Text(UnitClassCatalog.Get(selected.ClassId).DisplayNameKey, LocalizationService.Text(selected.RoleNameKey, selected.Role.ToString())),
                    selected.Level),
                PositionLabel = LocalizationService.Format(
                    "ui.position.value",
                    "Position ({0}, {1})",
                    selected.Position.X,
                    selected.Position.Y),
                TerrainName = terrainName,
                TerrainEffectSummary = terrainEffectSummary,
                Faction = selected.Faction,
                CurrentHp = selected.CurrentHp,
                MaxHp = selected.MaxHp,
                CurrentMana = selected.CurrentMana,
                MaxMana = selected.MaxMana,
                Level = selected.Level,
                CurrentExp = selected.CurrentExp,
                NextLevelExp = selected.NextLevelExp,
                Attack = selected.Attack + PassiveSkillRules.GetPersonalAttackBonus(selected) + PassiveSkillRules.GetAttackBonus(simulation.Context, selected) + SupportRules.GetAttackBonus(simulation.Context, selected, selected.Position) + StatusEffectRules.GetAttackModifier(selected),
                Defense = selected.Defense + PassiveSkillRules.GetDefenseBonus(selected) + SupportRules.GetDefenseBonus(simulation.Context, selected) + TerrainRules.GetDefenseBonus(terrainType) + StatusEffectRules.GetDefenseModifier(selected),
                MoveRange = PassiveSkillRules.GetMoveRange(selected),
                AttackRange = PassiveSkillRules.GetAttackRange(selected),
                WeaponTypeLabel = LocalizationService.Text(loadoutProfile.WeaponTypeKey, loadoutProfile.WeaponTypeFallback),
                WeaponName = weapon != null ? LocalizationService.Text(weapon.NameKey, weapon.NameFallback) : LocalizationService.Text(loadoutProfile.WeaponNameKey, loadoutProfile.WeaponNameFallback),
                WeaponDescription = weapon != null ? LocalizationService.Text(weapon.DescriptionKey, weapon.DescriptionFallback) : LocalizationService.Text(loadoutProfile.WeaponDescriptionKey, loadoutProfile.WeaponDescriptionFallback),
                ArmorSummary = BuildEquipmentSummary(armor, mount),
                WeaponAccentColor = loadoutProfile.AccentColor,
                PassiveName = LocalizationService.Text(selected.PassiveSkillNameKey, selected.PassiveSkill.ToString()),
                PassiveDescription = LocalizationService.Text(selected.PassiveSkillDescriptionKey, selected.PassiveSkill.ToString()),
                ActiveName = BuildMasteryTaggedSkillName(LocalizationService.Text(selected.ActiveSkillNameKey, selected.ActiveSkill.ToString()), selected),
                ActiveDescription = BuildSkillPanelDescription(selected),
                CooldownLabel = skillStatusText,
                StatusSummary = LocalizationService.Format("ui.label.status_value", "Status: {0}", BuildStatusSummary(selected)),
                ActionSummary = LocalizationService.Format(
                    "ui.label.action_state",
                    "Action state: {0}",
                    selected.HasActed
                        ? LocalizationService.Text("ui.status.done", "DONE")
                        : LocalizationService.Text("ui.status.ready", "READY")),
                ThreatSummary = threatSummaryText,
                ThreatDetail = threatDetailText,
            };
        }

        private string BuildEquipmentSummary(ItemDefinition armor, ItemDefinition mount)
        {
            List<string> sections = new List<string>();
            if (armor != null)
            {
                List<string> parts = new List<string>
                {
                    LocalizationService.Format("ui.label.armor_value", "Armor: {0}", LocalizationService.Text(armor.NameKey, armor.NameFallback)),
                };
                if (armor.DefenseBonus != 0)
                {
                    parts.Add(LocalizationService.Format("camp.item.defense", "DEF +{0}", armor.DefenseBonus));
                }

                if (armor.HpBonus != 0)
                {
                    parts.Add(LocalizationService.Format("camp.item.hp", "HP +{0}", armor.HpBonus));
                }

                sections.Add(string.Join("  ", parts));
            }

            if (mount != null)
            {
                List<string> parts = new List<string>
                {
                    LocalizationService.Format("ui.label.mount_value", "Mount: {0}", LocalizationService.Text(mount.NameKey, mount.NameFallback)),
                };
                if (mount.MoveBonus != 0)
                {
                    parts.Add(LocalizationService.Format("camp.item.move", "MOVE +{0}", mount.MoveBonus));
                }

                sections.Add(string.Join("  ", parts));
            }

            if (sections.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("\n", sections);
        }

        private IReadOnlyList<BattleRosterEntryModel> BuildRosterEntries(UnitFaction faction, BattleThreatSummary threatSummary)
        {
            HashSet<string> threateningIds = faction == UnitFaction.Enemy && threatSummary != null
                ? new HashSet<string>(threatSummary.ThreateningUnitIds)
                : new HashSet<string>();

            return simulation.Context.Units
                .Where(unit => unit.Faction == faction)
                .OrderBy(unit => unit.IsAlive ? (unit.HasActed ? 1 : 0) : 2)
                .ThenBy(unit => unit.Position.Y)
                .ThenBy(unit => unit.Position.X)
                .ThenBy(unit => unit.Id)
                .Select(unit => new BattleRosterEntryModel
                {
                    UnitId = unit.Id,
                    DisplayName = GetUnitDisplayName(unit.Id),
                    RoleShortLabel = GetRoleShortLabel(unit.Role),
                    PositionLabel = BuildRosterPositionLabel(unit),
                    StatusLabel = BuildRosterStatusLabel(unit),
                    SkillLabel = BuildRosterSkillLabel(unit),
                    Faction = unit.Faction,
                    CurrentHp = unit.CurrentHp,
                    MaxHp = unit.MaxHp,
                    IsAlive = unit.IsAlive,
                    HasActed = unit.HasActed,
                    CanUseSkill = unit.CanUseSkill,
                    IsSelected = unit.Id == selectedUnitId,
                    IsThreateningSelection = threateningIds.Contains(unit.Id),
                })
                .ToList();
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

            battleHUD.BindForecast(BuildAttackPreviewModel(selected, target));
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

            battleHUD.BindForecast(BuildSkillPreviewModel(selected, target));
            RefreshSkillTargetPreview(selected, target);
        }

        public void ClearTargetPreview()
        {
            if (battleHUD != null)
            {
                battleHUD.ClearForecast();
            }
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

        private BattleForecastModel BuildAttackPreviewModel(UnitRuntimeState attacker, UnitRuntimeState defender)
        {
            int damage = BattlePreviewCalculator.EstimateAttackDamage(simulation.Context, attacker, attacker.Position, defender);
            bool defenderFalls = damage >= defender.CurrentHp;
            int effectiveAttack = attacker.Attack +
                                  PassiveSkillRules.GetPersonalAttackBonus(attacker) +
                                  PassiveSkillRules.GetAttackBonus(simulation.Context, attacker) +
                                  SupportRules.GetAttackBonus(simulation.Context, attacker, attacker.Position) +
                                  PassiveSkillRules.GetDamageBonus(attacker) +
                                  StatusEffectRules.GetAttackModifier(attacker);
            int effectiveDefense = defender.Defense +
                                   PassiveSkillRules.GetDefenseBonus(defender) +
                                   SupportRules.GetDefenseBonus(simulation.Context, defender) +
                                   TerrainRules.GetDefenseBonus(simulation.Context.GetTerrainAt(defender.Position)) +
                                   StatusEffectRules.GetDefenseModifier(defender) -
                                   PassiveSkillRules.GetIgnoredDefense(attacker);
            if (effectiveDefense < 0)
            {
                effectiveDefense = 0;
            }

            return new BattleForecastModel
            {
                Header = LocalizationService.Text("ui.forecast.attack.header", "Attack Forecast"),
                Title = LocalizationService.Format("ui.forecast.attack.title", "{0} -> {1}", GetUnitDisplayName(attacker.Id), GetUnitDisplayName(defender.Id)),
                Summary = defenderFalls
                    ? LocalizationService.Format("ui.forecast.attack.ko", "Projected damage {0} | KO", damage)
                    : LocalizationService.Format("ui.forecast.attack.damage", "Projected damage {0} | {1} HP left", damage, Mathf.Max(0, defender.CurrentHp - damage)),
                Detail = LocalizationService.Format(
                    "ui.forecast.attack.detail",
                    "ATK {0} vs DEF {1}",
                    effectiveAttack,
                    effectiveDefense),
                Footer = BuildAttackForecastFooter(attacker, defender),
                AccentColor = attacker.Faction == UnitFaction.Player ? new Color(0.28f, 0.58f, 0.98f, 1f) : new Color(0.92f, 0.36f, 0.28f, 1f),
            };
        }

        private BattleForecastModel BuildSkillPreviewModel(UnitRuntimeState caster, UnitRuntimeState primaryTarget)
        {
            IReadOnlyList<UnitRuntimeState> affectedUnits = simulation.GetSkillAffectedTargets(caster.Id, primaryTarget.Id);
            string skillName = GetSkillDisplayName(caster.Id, caster.ActiveSkill);
            string title = LocalizationService.Format("ui.forecast.skill.title", "{0} -> {1}", skillName, GetUnitDisplayName(primaryTarget.Id));
            return new BattleForecastModel
            {
                Header = BuildSkillForecastHeader(caster),
                Title = title,
                Summary = BuildSkillPreviewSummary(caster, primaryTarget, affectedUnits),
                Detail = BuildSkillPreviewDetail(caster, primaryTarget, affectedUnits),
                Footer = BuildSkillPreviewFooter(caster),
                AccentColor = RoleLoadoutCatalog.GetSkillAccent(caster.ActiveSkill),
            };
        }

        private BattleForecastModel BuildCombatResultForecastModel(CombatResult combatResult)
        {
            string attackerName = GetUnitDisplayName(combatResult.AttackerUnitId);
            string defenderName = GetUnitDisplayName(combatResult.DefenderUnitId);
            return new BattleForecastModel
            {
                Header = LocalizationService.Text("ui.forecast.result.header", "Battle Result"),
                Title = LocalizationService.Format("ui.combat.banner", "{0} strikes {1}", attackerName, defenderName),
                Summary = combatResult.DefenderDied
                    ? LocalizationService.Format("ui.combat.ko", "-{0} HP   KO", combatResult.Damage)
                    : LocalizationService.Format("ui.combat.damage", "-{0} HP   {1} left", combatResult.Damage, combatResult.DefenderRemainingHp),
                Detail = string.Empty,
                Footer = BuildCombatLog(combatResult) + (combatResult.AttackerExpGained > 0 ? " | " + FormatExpGainText(combatResult.AttackerExpGained) : string.Empty),
                AccentColor = new Color(0.96f, 0.42f, 0.26f, 1f),
            };
        }

        private BattleForecastModel BuildSkillResultForecastModel(SkillResult skillResult)
        {
            string casterName = GetUnitDisplayName(skillResult.CasterUnitId);
            string skillName = GetSkillDisplayName(skillResult.CasterUnitId, skillResult.SkillType);
            UnitRuntimeState caster = simulation.Context.GetUnit(skillResult.CasterUnitId);
            return new BattleForecastModel
            {
                Header = caster != null ? BuildSkillForecastHeader(caster) : LocalizationService.Text("ui.forecast.result.header", "Battle Result"),
                Title = LocalizationService.Format("ui.skill.banner", "{0} uses {1}", casterName, skillName),
                Summary = BuildSkillBannerDetail(skillResult),
                Detail = string.Join("\n", skillResult.Effects.Select(BuildSkillEffectLine).Where(line => !string.IsNullOrWhiteSpace(line))),
                Footer = BuildSkillLog(skillResult) + (skillResult.CasterExpGained > 0 ? " | " + FormatExpGainText(skillResult.CasterExpGained) : string.Empty),
                AccentColor = RoleLoadoutCatalog.GetSkillAccent(skillResult.SkillType),
            };
        }

        private string BuildAttackForecastFooter(UnitRuntimeState attacker, UnitRuntimeState defender)
        {
            List<string> notes = new List<string>();
            if (PassiveSkillRules.GetPersonalAttackBonus(attacker) > 0)
            {
                notes.Add(LocalizationService.Text("skill.deadeye.name", "Deadeye"));
            }

            if (PassiveSkillRules.GetAttackBonus(simulation.Context, attacker) > 0)
            {
                notes.Add(attacker.PassiveSkill == PassiveSkillType.BenevolentCommand
                    ? LocalizationService.Text("skill.benevolent_command.name", "Benevolent Command")
                    : LocalizationService.Text("skill.command_aura.name", "Command Aura"));
            }

            if (PassiveSkillRules.GetDamageBonus(attacker) > 0)
            {
                notes.Add(attacker.PassiveSkill == PassiveSkillType.ThunderVanguard
                    ? LocalizationService.Text("skill.thunder_vanguard.name", "Thunder Vanguard")
                    : LocalizationService.Text("skill.vanguard.name", "Vanguard"));
            }

            if (PassiveSkillRules.GetIgnoredDefense(attacker) > 0)
            {
                notes.Add(attacker.PassiveSkill == PassiveSkillType.DragonGuard
                    ? LocalizationService.Text("skill.dragon_guard.name", "Dragon Guard")
                    : LocalizationService.Text("skill.armor_break.name", "Armor Break"));
            }

            if (StatusEffectRules.GetDefenseModifier(defender) < 0)
            {
                notes.Add(GetStatusDisplayName(StatusEffectType.ShatteredArmor));
            }

            return notes.Count == 0
                ? LocalizationService.Text("ui.forecast.no_modifier", "No extra combat modifiers.")
                : string.Join(" | ", notes);
        }

        private void PushBattleFeedEntry(string text)
        {
            battleHUD.PushFeedEntry(text);
        }

        private string BuildSkillBannerDetail(SkillResult skillResult)
        {
            if (skillResult.Effects == null || skillResult.Effects.Count == 0)
            {
                return string.Empty;
            }

            int totalHealing = skillResult.Effects.Where(effect => effect.IsHealing).Sum(effect => effect.Amount);
            int totalDamage = skillResult.Effects.Where(effect => !effect.IsHealing).Sum(effect => effect.Amount);
            int totalStatuses = skillResult.Effects.Sum(effect => effect.AppliedStatuses.Count(status => status.WasApplied));
            List<string> parts = new List<string>();

            if (totalHealing > 0)
            {
                parts.Add(LocalizationService.Format("ui.skill.detail.heal", "+{0} HP", totalHealing));
            }

            if (totalDamage > 0)
            {
                parts.Add(LocalizationService.Format("ui.skill.detail.damage", "-{0} HP", totalDamage));
            }

            if (skillResult.Effects.Count > 1)
            {
                parts.Add(LocalizationService.Format("ui.skill.detail.multi", "{0} targets hit", skillResult.Effects.Count));
            }

            if (totalStatuses > 0)
            {
                parts.Add(LocalizationService.Format("ui.skill.detail.status_count", "{0} status effects", totalStatuses));
            }

            return parts.Count == 0
                ? string.Empty
                : string.Join(" | ", parts);
        }

        private string BuildSkillForecastHeader(UnitRuntimeState caster)
        {
            string baseHeader = LocalizationService.Text("ui.forecast.skill.header", "Skill Forecast");
            return ActiveSkillRules.IsMastered(caster)
                ? LocalizationService.Format("ui.mastery.header", "{0} | {1}", baseHeader, BuildMasteryTag())
                : baseHeader;
        }

        private string BuildSkillPreviewSummary(UnitRuntimeState caster, UnitRuntimeState primaryTarget, IReadOnlyList<UnitRuntimeState> affectedUnits)
        {
            switch (caster.ActiveSkill)
            {
                case ActiveSkillType.RoyalAid:
                    return LocalizationService.Format(
                        "ui.forecast.skill.heal",
                        "Heal {0} HP",
                        BattlePreviewCalculator.EstimateHealing(primaryTarget, ActiveSkillRules.GetRoyalAidAmount(caster)));
                case ActiveSkillType.ImperialAid:
                    return LocalizationService.Format(
                        "ui.forecast.skill.volley",
                        "Primary {0} | Splash {1} | {2} targets",
                        BattlePreviewCalculator.EstimateHealing(primaryTarget, ActiveSkillRules.GetRoyalAidAmount(caster)),
                        Mathf.Max(0, affectedUnits.Count - 1),
                        affectedUnits.Count);
                case ActiveSkillType.GuardOrder:
                    return LocalizationService.Format(
                        "ui.forecast.skill.guard_order",
                        "Heal {0} | Guard {1} allies",
                        BattlePreviewCalculator.EstimateHealing(primaryTarget, ActiveSkillRules.GetGuardOrderHealAmount(caster)),
                        affectedUnits.Count);
                case ActiveSkillType.PowerStrike:
                case ActiveSkillType.PinningShot:
                case ActiveSkillType.DragonPierce:
                    int singleTargetDamage = EstimateSkillDamage(caster, primaryTarget);
                    return singleTargetDamage >= primaryTarget.CurrentHp
                        ? LocalizationService.Format("ui.forecast.attack.ko", "Projected damage {0} | KO", singleTargetDamage)
                        : LocalizationService.Format("ui.forecast.attack.damage", "Projected damage {0} | {1} HP left", singleTargetDamage, Mathf.Max(0, primaryTarget.CurrentHp - singleTargetDamage));
                case ActiveSkillType.Volley:
                case ActiveSkillType.SkyVolley:
                case ActiveSkillType.FireStratagem:
                case ActiveSkillType.EightTrigramInferno:
                    return LocalizationService.Format(
                        "ui.forecast.skill.volley",
                        "Primary {0} | Splash {1} | {2} targets",
                        EstimateSkillDamage(caster, primaryTarget),
                        Mathf.Max(0, affectedUnits.Count - 1),
                        affectedUnits.Count);
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.AzureDragonSlash:
                case ActiveSkillType.WesternStampede:
                    return LocalizationService.Format(
                        "ui.forecast.skill.green_dragon",
                        "Primary {0} | Cleave {1}",
                        EstimateSkillDamage(caster, primaryTarget),
                        affectedUnits.Count);
                case ActiveSkillType.WarCry:
                case ActiveSkillType.LionWarCry:
                    return LocalizationService.Format("ui.forecast.skill.war_cry", "Affects {0} nearby foes", affectedUnits.Count);
                default:
                    return LocalizationService.Text("ui.forecast.skill.none", "No forecast available.");
            }
        }

        private string BuildSkillPreviewDetail(UnitRuntimeState caster, UnitRuntimeState primaryTarget, IReadOnlyList<UnitRuntimeState> affectedUnits)
        {
            List<string> lines = affectedUnits
                .Select(target => BuildPreviewSkillEffectLine(caster, primaryTarget, target))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (caster.ActiveSkill == ActiveSkillType.DragonPierce)
            {
                lines.Add(BuildEffectLine(
                    GetUnitDisplayName(caster.Id),
                    0,
                    caster.CurrentHp,
                    false,
                    false,
                    new[] { CreatePreviewStatus(StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()) }));

                UnitRuntimeState ally = simulation.Context.GetUnits(caster.Faction)
                    .Where(unit => unit.Id != caster.Id && unit.Position.ManhattanDistance(caster.Position) == 1)
                    .OrderBy(unit => unit.CurrentHp)
                    .ThenBy(unit => unit.Id)
                    .FirstOrDefault();
                if (ally != null)
                {
                    lines.Add(BuildEffectLine(
                        GetUnitDisplayName(ally.Id),
                        0,
                        ally.CurrentHp,
                        false,
                        false,
                        new[] { CreatePreviewStatus(StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()) }));
                }
            }

            if (caster.ActiveSkill == ActiveSkillType.LionWarCry)
            {
                IReadOnlyList<SkillStatusApplication> casterStatuses = BuildPreviewStatuses(caster, primaryTarget, caster, true);
                lines.Add(BuildEffectLine(GetUnitDisplayName(caster.Id), 0, caster.CurrentHp, false, false, casterStatuses));
            }

            return string.Join("\n", lines);
        }

        private string BuildSkillPreviewFooter(UnitRuntimeState caster)
        {
            List<string> parts = new List<string>
            {
                LocalizationService.Format("ui.skill.mana_cost", "Cost {0} MP", ActiveSkillRules.GetManaCost(caster.ActiveSkill)),
                LocalizationService.Format("ui.skill.range", "Range {0}", ActiveSkillRules.GetRange(caster)),
                BuildSkillImpactDescriptor(caster.ActiveSkill),
                BuildSkillMasteryFooter(caster),
            };

            return string.Join(" | ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        private string BuildPreviewSkillEffectLine(UnitRuntimeState caster, UnitRuntimeState primaryTarget, UnitRuntimeState target)
        {
            bool isHealing = ActiveSkillRules.IsSupportSkill(caster.ActiveSkill);
            int amount = EstimateSkillEffectAmount(caster, primaryTarget, target);
            bool unitDied = !isHealing && amount >= target.CurrentHp && amount > 0;
            int remainingHp = isHealing
                ? Mathf.Min(target.MaxHp, target.CurrentHp + amount)
                : Mathf.Max(0, target.CurrentHp - amount);
            IReadOnlyList<SkillStatusApplication> statuses = BuildPreviewStatuses(caster, primaryTarget, target, !unitDied);
            return BuildEffectLine(GetUnitDisplayName(target.Id), amount, remainingHp, unitDied, isHealing, statuses);
        }

        private int EstimateSkillEffectAmount(UnitRuntimeState caster, UnitRuntimeState primaryTarget, UnitRuntimeState target)
        {
            switch (caster.ActiveSkill)
            {
                case ActiveSkillType.RoyalAid:
                    return BattlePreviewCalculator.EstimateHealing(target, ActiveSkillRules.GetRoyalAidAmount(caster));
                case ActiveSkillType.ImperialAid:
                    return BattlePreviewCalculator.EstimateHealing(
                        target,
                        target.Id == primaryTarget.Id
                            ? ActiveSkillRules.GetRoyalAidAmount(caster)
                            : ActiveSkillRules.GetImperialAidSplashAmount(caster));
                case ActiveSkillType.GuardOrder:
                    return target.Id == primaryTarget.Id
                        ? BattlePreviewCalculator.EstimateHealing(target, ActiveSkillRules.GetGuardOrderHealAmount(caster))
                        : 0;
                case ActiveSkillType.WarCry:
                case ActiveSkillType.LionWarCry:
                    return 0;
                default:
                    return EstimateSkillDamage(caster, target);
            }
        }

        private int EstimateSkillDamage(UnitRuntimeState caster, UnitRuntimeState target)
        {
            return BattlePreviewCalculator.EstimateAttackDamage(
                simulation.Context,
                caster,
                caster.Position,
                target,
                GetSkillFlatBonus(caster),
                caster.ActiveSkill == ActiveSkillType.DragonPierce
                    ? ActiveSkillRules.GetDragonPierceIgnoredDefense(caster)
                    : 0);
        }

        private int GetSkillFlatBonus(UnitRuntimeState caster)
        {
            switch (caster.ActiveSkill)
            {
                case ActiveSkillType.PowerStrike:
                    return ActiveSkillRules.GetPowerStrikeBonus(caster);
                case ActiveSkillType.DragonPierce:
                    return ActiveSkillRules.GetDragonPierceBonus(caster);
                case ActiveSkillType.PinningShot:
                    return ActiveSkillRules.GetPinningShotBonus(caster);
                case ActiveSkillType.Volley:
                    return ActiveSkillRules.GetVolleyBonus(caster);
                case ActiveSkillType.SkyVolley:
                    return ActiveSkillRules.GetSkyVolleyBonus(caster);
                case ActiveSkillType.GreenDragonSlash:
                    return ActiveSkillRules.GetGreenDragonSlashBonus(caster);
                case ActiveSkillType.AzureDragonSlash:
                    return ActiveSkillRules.GetAzureDragonSlashBonus(caster);
                case ActiveSkillType.WesternStampede:
                    return ActiveSkillRules.GetWesternStampedeBonus(caster);
                case ActiveSkillType.FireStratagem:
                    return ActiveSkillRules.GetFireStratagemBonus(caster);
                case ActiveSkillType.EightTrigramInferno:
                    return ActiveSkillRules.GetEightTrigramInfernoBonus(caster);
                default:
                    return 0;
            }
        }

        private IReadOnlyList<SkillStatusApplication> BuildPreviewStatuses(UnitRuntimeState caster, UnitRuntimeState primaryTarget, UnitRuntimeState target, bool targetSurvives)
        {
            List<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
            bool isPrimaryTarget = primaryTarget != null && target != null && target.Id == primaryTarget.Id;

            switch (caster.ActiveSkill)
            {
                case ActiveSkillType.RoyalAid:
                    statuses.Add(CreatePreviewStatus(StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration()));
                    statuses.Add(CreatePreviewStatus(StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()));
                    break;
                case ActiveSkillType.ImperialAid:
                    if (isPrimaryTarget)
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration()));
                        statuses.Add(CreatePreviewStatus(StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()));
                    }
                    else
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration()));
                        if (ActiveSkillRules.IsMastered(caster))
                        {
                            statuses.Add(CreatePreviewStatus(StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()));
                        }
                    }

                    break;
                case ActiveSkillType.GuardOrder:
                    statuses.Add(CreatePreviewStatus(StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()));
                    if (isPrimaryTarget && ActiveSkillRules.IsMastered(caster))
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration()));
                    }

                    break;
                case ActiveSkillType.PowerStrike:
                    if (targetSurvives)
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.PowerStrike, caster)));
                        statuses.Add(CreatePreviewStatus(StatusEffectType.Bleeding, 1));
                    }

                    break;
                case ActiveSkillType.Volley:
                    if (targetSurvives)
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.Volley, caster)));
                    }

                    break;
                case ActiveSkillType.SkyVolley:
                    if (targetSurvives)
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.SkyVolley, caster)));
                    }

                    break;
                case ActiveSkillType.PinningShot:
                    if (targetSurvives)
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.Rooted, ActiveSkillRules.GetRootedDuration(caster)));
                    }

                    break;
                case ActiveSkillType.WesternStampede:
                    if (targetSurvives)
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.Intimidated, ActiveSkillRules.GetIntimidatedDuration(ActiveSkillType.WesternStampede, caster)));
                    }

                    break;
                case ActiveSkillType.GreenDragonSlash:
                    if (targetSurvives && ActiveSkillRules.IsMastered(caster))
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.GreenDragonSlash, caster)));
                    }

                    break;
                case ActiveSkillType.AzureDragonSlash:
                    if (targetSurvives)
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.AzureDragonSlash, caster)));
                    }

                    break;
                case ActiveSkillType.WarCry:
                    statuses.Add(CreatePreviewStatus(StatusEffectType.Intimidated, ActiveSkillRules.GetIntimidatedDuration(ActiveSkillType.WarCry, caster)));
                    break;
                case ActiveSkillType.LionWarCry:
                    if (target.Id == caster.Id)
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration()));
                        if (ActiveSkillRules.IsMastered(caster))
                        {
                            statuses.Add(CreatePreviewStatus(StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()));
                        }
                    }
                    else
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.Intimidated, ActiveSkillRules.GetIntimidatedDuration(ActiveSkillType.LionWarCry, caster)));
                    }

                    break;
                case ActiveSkillType.FireStratagem:
                    if (targetSurvives && (isPrimaryTarget || ActiveSkillRules.IsMastered(caster)))
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.Intimidated, ActiveSkillRules.GetIntimidatedDuration(ActiveSkillType.FireStratagem, caster)));
                    }

                    break;
                case ActiveSkillType.EightTrigramInferno:
                    if (targetSurvives)
                    {
                        statuses.Add(CreatePreviewStatus(StatusEffectType.Intimidated, ActiveSkillRules.GetIntimidatedDuration(ActiveSkillType.EightTrigramInferno, caster)));
                        statuses.Add(CreatePreviewStatus(StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.EightTrigramInferno, caster)));
                    }

                    break;
            }

            return statuses;
        }

        private static SkillStatusApplication CreatePreviewStatus(StatusEffectType type, int duration)
        {
            return new SkillStatusApplication(type, duration, true);
        }

        private string BuildEffectLine(
            string targetName,
            int amount,
            int remainingHp,
            bool unitDied,
            bool isHealing,
            IReadOnlyList<SkillStatusApplication> statuses)
        {
            List<string> parts = new List<string>();
            if (amount > 0)
            {
                parts.Add(isHealing
                    ? LocalizationService.Format("ui.skill.detail.heal", "+{0} HP", amount)
                    : unitDied
                        ? LocalizationService.Format("ui.skill.effect.ko_line", "-{0} HP | KO", amount)
                        : LocalizationService.Format("ui.skill.effect.damage_line", "-{0} HP | {1} HP left", amount, remainingHp));
            }

            string statusText = BuildStatusSummaryText(statuses);
            if (!string.IsNullOrWhiteSpace(statusText))
            {
                parts.Add(statusText);
            }

            return parts.Count == 0
                ? targetName
                : LocalizationService.Format("ui.forecast.skill.effect_line", "{0}: {1}", targetName, string.Join(" | ", parts));
        }

        private string BuildSkillEffectLine(SkillEffectResult effect)
        {
            return BuildEffectLine(
                GetUnitDisplayName(effect.UnitId),
                effect.Amount,
                effect.RemainingHp,
                effect.UnitDied,
                effect.IsHealing,
                effect.AppliedStatuses.Where(status => status.WasApplied).ToList());
        }

        private string BuildStatusFloatingText(IReadOnlyList<SkillStatusApplication> statuses)
        {
            return string.Join(
                " / ",
                statuses
                    .Where(status => status.WasApplied)
                    .Select(status => GetStatusDisplayName(status.Type))
                    .Distinct());
        }

        private string BuildStatusSummaryText(IReadOnlyList<SkillStatusApplication> statuses)
        {
            if (statuses == null || statuses.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(", ", statuses.Select(FormatStatusApplication));
        }

        private string FormatStatusApplication(SkillStatusApplication status)
        {
            return LocalizationService.Format(
                "ui.status.duration",
                "{0} ({1}T)",
                GetStatusDisplayName(status.Type),
                status.Duration);
        }

        private string BuildMasteryTag()
        {
            return LocalizationService.Text("ui.mastery.tag", "Lv10 Mastery");
        }

        private string BuildSkillImpactDescriptor(ActiveSkillType skillType)
        {
            switch (skillType)
            {
                case ActiveSkillType.RoyalAid:
                    return LocalizationService.Text("ui.skill.impact.single_ally", "1 ally");
                case ActiveSkillType.ImperialAid:
                case ActiveSkillType.GuardOrder:
                    return LocalizationService.Text("ui.skill.impact.ally_adjacent", "1 ally + adjacent");
                case ActiveSkillType.PowerStrike:
                case ActiveSkillType.PinningShot:
                case ActiveSkillType.DragonPierce:
                    return LocalizationService.Text("ui.skill.impact.single_enemy", "1 foe");
                case ActiveSkillType.Volley:
                case ActiveSkillType.SkyVolley:
                case ActiveSkillType.FireStratagem:
                case ActiveSkillType.EightTrigramInferno:
                    return LocalizationService.Text("ui.skill.impact.enemy_adjacent", "1 foe + adjacent");
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.AzureDragonSlash:
                case ActiveSkillType.WesternStampede:
                    return LocalizationService.Text("ui.skill.impact.line", "up to 2 foes");
                case ActiveSkillType.WarCry:
                case ActiveSkillType.LionWarCry:
                    return LocalizationService.Text("ui.skill.impact.nearby", "nearby foes");
                default:
                    return string.Empty;
            }
        }

        private string BuildSkillMasteryFooter(UnitRuntimeState caster)
        {
            string deltaKey = GetSkillMasteryDeltaKey(caster.ActiveSkill);
            string deltaText = LocalizationService.Text(deltaKey, string.Empty);
            return ActiveSkillRules.IsMastered(caster)
                ? LocalizationService.Format("ui.mastery.active", "{0} active", BuildMasteryTag())
                : string.IsNullOrWhiteSpace(deltaText)
                    ? string.Empty
                    : LocalizationService.Format("ui.mastery.future", "Lv10: {0}", deltaText);
        }

        private static string GetSkillMasteryDeltaKey(ActiveSkillType skillType)
        {
            switch (skillType)
            {
                case ActiveSkillType.RoyalAid:
                    return "ui.mastery.delta.royal_aid";
                case ActiveSkillType.ImperialAid:
                    return "ui.mastery.delta.imperial_aid";
                case ActiveSkillType.GuardOrder:
                    return "ui.mastery.delta.guard_order";
                case ActiveSkillType.PowerStrike:
                    return "ui.mastery.delta.power_strike";
                case ActiveSkillType.DragonPierce:
                    return "ui.mastery.delta.dragon_pierce";
                case ActiveSkillType.Volley:
                    return "ui.mastery.delta.volley";
                case ActiveSkillType.SkyVolley:
                    return "ui.mastery.delta.sky_volley";
                case ActiveSkillType.GreenDragonSlash:
                    return "ui.mastery.delta.green_dragon_slash";
                case ActiveSkillType.AzureDragonSlash:
                    return "ui.mastery.delta.azure_dragon_slash";
                case ActiveSkillType.WesternStampede:
                    return "ui.mastery.delta.western_stampede";
                case ActiveSkillType.WarCry:
                    return "ui.mastery.delta.war_cry";
                case ActiveSkillType.LionWarCry:
                    return "ui.mastery.delta.lion_war_cry";
                case ActiveSkillType.PinningShot:
                    return "ui.mastery.delta.pinning_shot";
                case ActiveSkillType.FireStratagem:
                    return "ui.mastery.delta.fire_stratagem";
                case ActiveSkillType.EightTrigramInferno:
                    return "ui.mastery.delta.eight_trigram_inferno";
                default:
                    return string.Empty;
            }
        }

        private string BuildSkillActionMenuDetail(UnitRuntimeState unit, bool canUseSkill)
        {
            if (unit == null || unit.ActiveSkill == ActiveSkillType.None)
            {
                return LocalizationService.Text("ui.action_menu.skill.none", "No active skill");
            }

            List<string> parts = new List<string>();
            if (unit.CurrentMana < ActiveSkillRules.GetManaCost(unit.ActiveSkill))
            {
                parts.Add(LocalizationService.Format("ui.action_menu.skill.no_mana", "Not enough mana ({0}/{1})", unit.CurrentMana, unit.MaxMana));
            }
            else if (canUseSkill)
            {
                parts.Add(LocalizationService.Text("ui.action_menu.skill.ready", "Skill ready"));
            }
            else
            {
                parts.Add(LocalizationService.Text("ui.action_menu.skill.unavailable", "No valid target"));
            }

            parts.Add(LocalizationService.Format("ui.skill.mana_cost", "Cost {0} MP", ActiveSkillRules.GetManaCost(unit.ActiveSkill)));
            parts.Add(LocalizationService.Format("ui.skill.range", "Range {0}", ActiveSkillRules.GetRange(unit)));
            parts.Add(BuildSkillImpactDescriptor(unit.ActiveSkill));

            string masteryFooter = BuildSkillMasteryFooter(unit);
            if (!string.IsNullOrWhiteSpace(masteryFooter))
            {
                parts.Add(masteryFooter);
            }

            return string.Join(" | ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        private string BuildMasteryTaggedSkillName(string skillName, UnitRuntimeState unit)
        {
            return ActiveSkillRules.IsMastered(unit)
                ? skillName + "  " + BuildMasteryTag()
                : skillName;
        }

        private string BuildSkillPanelDescription(UnitRuntimeState unit)
        {
            string description = LocalizationService.Text(unit.ActiveSkillDescriptionKey, unit.ActiveSkill.ToString());
            string masteryFooter = BuildSkillMasteryFooter(unit);
            return string.IsNullOrWhiteSpace(masteryFooter)
                ? description
                : description + "\n" + masteryFooter;
        }

        private string BuildSkillAvailabilityLabel(UnitRuntimeState unit, bool hasTargets)
        {
            if (unit == null || unit.ActiveSkill == ActiveSkillType.None)
            {
                return LocalizationService.Format(
                    "ui.label.skill_status_value",
                    "Skill: {0}",
                    LocalizationService.Text("ui.action_menu.skill.none", "No active skill"));
            }

            string status = unit.CurrentMana < ActiveSkillRules.GetManaCost(unit.ActiveSkill)
                ? LocalizationService.Format("ui.action_menu.skill.no_mana", "Not enough mana ({0}/{1})", unit.CurrentMana, unit.MaxMana)
                : hasTargets
                    ? LocalizationService.Text("ui.action_menu.skill.ready", "Skill ready")
                    : LocalizationService.Text("ui.action_menu.skill.unavailable", "No valid target");
            return LocalizationService.Format("ui.label.skill_status_value", "Skill: {0}", status);
        }

        private string BuildRosterPositionLabel(UnitRuntimeState unit)
        {
            if (unit == null)
            {
                return LocalizationService.Text("ui.position.compact", "(0,0)");
            }

            ItemDefinition mount = ItemCatalog.Get(unit.EquipmentLoadout.MountId);
            int moveRange = PassiveSkillRules.GetMoveRange(unit);
            return mount != null && mount.MoveBonus > 0
                ? LocalizationService.Format(
                    "ui.position.compact_move_mount",
                    "({0},{1}) | M {2} | {3}",
                    unit.Position.X,
                    unit.Position.Y,
                    moveRange,
                    LocalizationService.Text(mount.NameKey, mount.NameFallback))
                : LocalizationService.Format(
                    "ui.position.compact_move",
                    "({0},{1}) | M {2}",
                    unit.Position.X,
                    unit.Position.Y,
                    moveRange);
        }

        private string BuildTerrainEffectSummary(UnitRuntimeState unit, TerrainType terrainType)
        {
            List<string> parts = new List<string>();
            if (terrainType == TerrainType.Forest || terrainType == TerrainType.Hazard)
            {
                parts.Add(LocalizationService.Format(
                    "ui.terrain.effect.move_cost",
                    "Move cost {0}",
                    TerrainRules.GetMoveCost(unit, terrainType)));
            }

            int defenseBonus = TerrainRules.GetDefenseBonus(terrainType);
            if (defenseBonus > 0)
            {
                parts.Add(LocalizationService.Format("ui.terrain.effect.defense_bonus", "Defense +{0}", defenseBonus));
            }

            int healing = TerrainRules.GetEndTurnHealing(terrainType);
            if (healing > 0)
            {
                parts.Add(LocalizationService.Format("ui.terrain.effect.heal", "End turn heal {0}", healing));
            }

            int damage = TerrainRules.GetEndTurnDamage(terrainType);
            if (damage > 0)
            {
                parts.Add(LocalizationService.Format("ui.terrain.effect.damage", "End turn damage {0}", damage));
            }

            return parts.Count == 0
                ? LocalizationService.Text("ui.terrain.effect.none", "No special effect")
                : string.Join(" | ", parts);
        }

        private string GetUnitDisplayName(string unitId)
        {
            UnitRuntimeState unit = simulation.Context.GetUnit(unitId);
            return unit != null
                ? LocalizationService.Text(unit.DisplayNameKey, unit.DisplayName)
                : unitId;
        }

        private string GetSkillDisplayName(string unitId, ActiveSkillType skillType)
        {
            UnitRuntimeState unit = simulation.Context.GetUnit(unitId);
            return unit != null && unit.ActiveSkill == skillType
                ? LocalizationService.Text(unit.ActiveSkillNameKey, skillType.ToString())
                : skillType.ToString();
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

        private string GetTerrainDisplayName(TerrainType terrainType)
        {
            return LocalizationService.Text(TerrainRules.GetNameKey(terrainType), terrainType.ToString());
        }

        private string FormatExpGainText(int experience)
        {
            return LocalizationService.Format("ui.exp.gain", "EXP +{0}", experience);
        }

        private string GetStatusDisplayName(StatusEffectType statusEffectType)
        {
            switch (statusEffectType)
            {
                case StatusEffectType.Inspired:
                    return LocalizationService.Text("status.inspired.name", "Inspired");
                case StatusEffectType.ShatteredArmor:
                    return LocalizationService.Text("status.shattered_armor.name", "Shattered Armor");
                case StatusEffectType.Intimidated:
                    return LocalizationService.Text("status.intimidated.name", "Intimidated");
                case StatusEffectType.Bleeding:
                    return LocalizationService.Text("status.bleeding.name", "Bleeding");
                case StatusEffectType.Rooted:
                    return LocalizationService.Text("status.rooted.name", "Rooted");
                case StatusEffectType.Guarded:
                    return LocalizationService.Text("status.guarded.name", "Guarded");
                case StatusEffectType.Taunted:
                    return LocalizationService.Text("status.taunted.name", "Taunted");
                default:
                    return statusEffectType.ToString();
            }
        }

        private string BuildStatusSummary(UnitRuntimeState unit)
        {
            if (unit == null || unit.StatusEffects.Count == 0)
            {
                return LocalizationService.Text("ui.status.none", "None");
            }

            return string.Join(", ", unit.StatusEffects.Select(effect => GetStatusDisplayName(effect.Type)));
        }

        private string BuildRosterStatusLabel(UnitRuntimeState unit)
        {
            if (!unit.IsAlive)
            {
                return LocalizationService.Text("ui.roster.defeated", "Defeated");
            }

            string actionState = unit.HasActed
                ? LocalizationService.Text("ui.roster.done", "Done")
                : LocalizationService.Text("ui.roster.ready", "Ready");
            if (unit.StatusEffects.Count == 0)
            {
                return actionState;
            }

            return actionState + " | " + BuildStatusSummary(unit);
        }

        private string BuildRosterSkillLabel(UnitRuntimeState unit)
        {
            if (!unit.IsAlive || unit.ActiveSkill == ActiveSkillType.None)
            {
                return LocalizationService.Text("ui.roster.skill_none", "-");
            }

            string manaSummary = LocalizationService.Format("ui.roster.skill_mana", "MP {0}/{1}", unit.CurrentMana, unit.MaxMana);
            string state = unit.CurrentMana < ActiveSkillRules.GetManaCost(unit.ActiveSkill)
                ? LocalizationService.Text("ui.roster.skill_low_mana", "Low MP")
                : unit.HasActed
                    ? LocalizationService.Text("ui.roster.skill_spent", "Spent")
                    : LocalizationService.Text("ui.roster.skill_ready", "Ready");

            return manaSummary + " | " + state;
        }

        private string GetRoleShortLabel(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Commander:
                    return LocalizationService.Text("ui.role.short.commander", "CMD");
                case UnitRole.Guardian:
                    return LocalizationService.Text("ui.role.short.guardian", "GDN");
                case UnitRole.Ranger:
                    return LocalizationService.Text("ui.role.short.ranger", "RNG");
                case UnitRole.Scout:
                    return LocalizationService.Text("ui.role.short.scout", "SCT");
                case UnitRole.Raider:
                    return LocalizationService.Text("ui.role.short.raider", "RDR");
                default:
                    return LocalizationService.Text("ui.role.short.unknown", "UNIT");
            }
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
                survivingUnitIds);
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
        }

        private void LoadScenario(BattleScenarioData data)
        {
            StopAllCoroutines();
            currentState?.Exit();
            currentState = null;
            selectedUnitId = null;
            pendingCombatResult = null;
            pendingSkillResult = null;
            pendingDialogueResumeState = null;
            currentTurnText = LocalizationService.Text("ui.turn.player", "Turn: Player Phase");
            currentInstructionText = string.Empty;
            battleRewardsGranted = false;
            battleResultSummaryText = string.Empty;

            battleHUD.ClearForecast();
            battleHUD.HideResult();
            battleHUD.HideDialogue();
            battleHUD.HideCampaignOverlay();

            scenarioData = data;
            simulation = new BattleSimulation(scenarioData.Stage);
            scenarioDirector = new ScenarioDirector(scenarioData);
            gridManager.BuildGrid(simulation.Context, OnCellClicked);
            CreateUnits();
            ConfigureCamera();
            battleHUD.SetRerollEnabled(simulation.Context.IsRandomMap);
            RefreshAllVisuals();
        }

        private string BuildBattleResultText(string text)
        {
            if (simulation == null || scenarioData == null || simulation.Context == null || !simulation.Context.BattleEnded)
            {
                return text;
            }

            GrantBattleRewards();
            return string.IsNullOrEmpty(battleResultSummaryText)
                ? text
                : text + "\n\n" + battleResultSummaryText;
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
            battleResultSummaryText = BuildBattleRewardSummary(objectiveReward);
        }

        private string BuildBattleRewardSummary(int objectiveReward)
        {
            List<string> lines = new List<string>
            {
                LocalizationService.Format("ui.result.objective_exp", "Objective reward +{0} EXP", objectiveReward),
            };

            if (simulation.Context.WinningSide == TurnSide.Player &&
                scenarioData != null &&
                scenarioData.RewardBundle != null &&
                scenarioData.RewardBundle.HasAnyReward)
            {
                lines.Add(LocalizationService.Format(
                    "ui.result.rewards",
                    "Rewards: Supplies {0}  Renown {1}",
                    scenarioData.RewardBundle.Supplies,
                    scenarioData.RewardBundle.Renown));
            }

            foreach (UnitRuntimeState unit in simulation.Context.GetUnits(UnitFaction.Player, false).OrderBy(unit => unit.Id))
            {
                lines.Add(BuildUnitBattleRewardSummary(unit, objectiveReward));
            }

            return string.Join("\n", lines);
        }

        private string BuildUnitBattleRewardSummary(UnitRuntimeState unit, int objectiveReward)
        {
            int totalGain = unit.BattleExpEarned + objectiveReward + ExperienceSystem.GetParticipationReward(unit);
            string summary = LocalizationService.Format(
                "ui.result.unit_summary",
                "{0}  Lv {1}  EXP +{2}  ({3}/{4})",
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

        private bool IsInteractionLocked()
        {
            return currentState is ScenarioDialogueState ||
                   battleHUD != null && (battleHUD.IsCampaignOverlayVisible || battleHUD.IsResultVisible);
        }

        private ScenarioEvaluationResult ProcessScenarioCheckpoint(ScenarioCheckpoint checkpoint)
        {
            ScenarioEvaluationResult result = scenarioDirector != null
                ? scenarioDirector.Evaluate(checkpoint, simulation.Context)
                : new ScenarioEvaluationResult(Array.Empty<string>(), false, false, false, false);

            UpdateHudModels();
            if (result.BattlefieldChanged)
            {
                gridManager.BuildGrid(simulation.Context, OnCellClicked);
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
                ProcessScenarioCheckpoint(ScenarioCheckpoint.PreBattleOutcome);
                Type terminalState = simulation.Context.WinningSide == TurnSide.Player
                    ? typeof(BattleVictoryState)
                    : typeof(BattleDefeatState);
                if (TryEnterScenarioDialogue(terminalState))
                {
                    return true;
                }

                battleHUD.ClearForecast();
                ChangeState(terminalState);
                return true;
            }

            return TryEnterScenarioDialogue(defaultResumeStateType);
        }
    }
}
