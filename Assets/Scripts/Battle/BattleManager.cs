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

        public bool IsDialogueVisible => battleHUD != null && battleHUD.IsDialogueVisible;

        public bool IsRerollVisible => battleHUD != null && battleHUD.IsRerollVisible;

        public bool IsActionMenuVisible => actionMenuPanel != null && actionMenuPanel.IsVisible;

        public string CurrentActionMenuModeText => actionMenuPanel != null ? actionMenuPanel.CurrentModeText : string.Empty;

        public bool IsActionMenuBackEnabled => actionMenuPanel != null && actionMenuPanel.IsBackEnabled;

        public bool IsActionMenuSkillEnabled => actionMenuPanel != null && actionMenuPanel.IsSkillEnabled;

        public string CurrentObjectiveText => battleHUD != null ? battleHUD.CurrentObjectiveText : string.Empty;

        public BattleSimulation Simulation => simulation;

        public void Initialize(BattleScenarioDefinition scenario)
        {
            scenarioDefinition = scenario != null ? scenario : BattleScenarioDefinition.CreateDefault();
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
            battleHUD.ShowResult(text);
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
            gridManager.ShowAttackRange(simulation.GetProjectedAttackRange(selected.Id));
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
            if (selected.CurrentSkillCooldown > 0)
            {
                return skillName + "\n" + LocalizationService.Format("ui.cooldown.value", "CD {0}", selected.CurrentSkillCooldown);
            }

            return skillName;
        }

        public string GetActionMenuInstructionText()
        {
            return HasSelectionMoved()
                ? LocalizationService.Text("ui.log.choose_action_moved", "Choose Attack, Skill, Wait, or Back after moving.")
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
            int skillManaCost = ActiveSkillRules.GetManaCost(selected.ActiveSkill);
            bool moved = HasSelectionMoved();
            string skillName = LocalizationService.Text(selected.ActiveSkillNameKey, selected.ActiveSkill.ToString());
            string skillDetail;

            if (selected.ActiveSkill == ActiveSkillType.None)
            {
                skillDetail = LocalizationService.Text("ui.action_menu.skill.none", "No active skill");
            }
            else if (selected.CurrentSkillCooldown > 0)
            {
                skillDetail = LocalizationService.Format(
                    "ui.action_menu.skill.cooldown",
                    "Cooldown {0}",
                    selected.CurrentSkillCooldown);
            }
            else if (selected.CurrentMana < skillManaCost)
            {
                skillDetail = LocalizationService.Format(
                    "ui.action_menu.skill.no_mana",
                    "Not enough mana ({0}/{1})",
                    selected.CurrentMana,
                    selected.MaxMana);
            }
            else if (canUseSkill)
            {
                skillDetail = LocalizationService.Format(
                    "ui.action_menu.skill.ready",
                    "Skill ready") + " | " + LocalizationService.Format("ui.action_menu.skill.cost", "Cost {0} MP", skillManaCost);
            }
            else
            {
                skillDetail = LocalizationService.Text("ui.action_menu.skill.unavailable", "No valid target");
            }

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
                SkillDetail = skillDetail,
                WaitDetail = LocalizationService.Text("ui.action_menu.wait.detail", "End this unit's action"),
                CanBack = moved,
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

            if (scenarioDefinition == null)
            {
                scenarioDefinition = BattleScenarioDefinition.CreateDefault();
            }

            RegisterStates();
            LoadScenario(scenarioDefinition.ToData());
            initialized = true;
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

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject hudObject = new GameObject("BattleHUD");
            hudObject.transform.SetParent(canvasObject.transform, false);
            battleHUD = hudObject.AddComponent<BattleHUD>();
            battleHUD.Initialize(canvasObject.transform, HandleEndTurnRequested, HandleRerollRequested, HandleDialogueAdvanceRequested);

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

            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.orthographic = true;
            mainCamera.backgroundColor = new Color(0.31f, 0.34f, 0.28f, 1f);
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.orthographicSize = Mathf.Max(simulation.Context.Width, simulation.Context.Height) * 0.68f;
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
            battleHUD.BindForecast(BuildSkillResultForecastModel(skillResult));

            if (casterView != null)
            {
                yield return SkillVisualEffects.PlayCasterEffect(skillResult.SkillType, casterView, primaryTargetView);
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
                    yield return SkillVisualEffects.PlayTargetEffect(skillResult.SkillType, targetView, effect.UnitId == skillResult.PrimaryTargetUnitId);
                    FloatingText.Spawn("+" + effect.Amount, targetView.GetAnchorPosition(0.98f), new Color(0.54f, 1f, 0.62f, 1f));
                    yield return targetView.AnimatePulse(new Color(0.7f, 1f, 0.78f, 1f));
                }
                else if (effect.Amount > 0)
                {
                    yield return SkillVisualEffects.PlayTargetEffect(skillResult.SkillType, targetView, effect.UnitId == skillResult.PrimaryTargetUnitId);
                    FloatingText.Spawn("-" + effect.Amount, targetView.GetAnchorPosition(0.98f), new Color(1f, 0.89f, 0.4f, 1f));
                    yield return targetView.AnimateHit();

                    if (effect.UnitDied)
                    {
                        FloatingText.Spawn(LocalizationService.Text("ui.combat.popup_ko", "KO"), targetView.GetAnchorPosition(1.24f), new Color(1f, 0.56f, 0.42f, 1f));
                    }
                }
                else
                {
                    yield return SkillVisualEffects.PlayTargetEffect(skillResult.SkillType, targetView, effect.UnitId == skillResult.PrimaryTargetUnitId);
                    yield return targetView.AnimatePulse(new Color(0.75f, 0.72f, 1f, 1f));
                }

                if (effect.AppliedStatus != StatusEffectType.None)
                {
                    FloatingText.Spawn(GetStatusDisplayName(effect.AppliedStatus), targetView.GetAnchorPosition(1.18f), new Color(0.76f, 0.96f, 1f, 1f));
                }

                yield return new WaitForSeconds(0.05f);
            }

            yield return new WaitForSeconds(0.18f);
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
            int skillReadyUnits = context.Units.Count(unit => unit.Faction == UnitFaction.Player && unit.IsAlive && !unit.HasActed && unit.CanUseSkill);

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
            string cooldownText = selected.ActiveSkill == ActiveSkillType.None
                ? LocalizationService.Format("ui.label.cooldown_value", "Cooldown: {0}", LocalizationService.Text("ui.cooldown.none", "-"))
                : LocalizationService.Format(
                    "ui.label.cooldown_value",
                    "Cooldown: {0}",
                    selected.CurrentSkillCooldown > 0
                        ? LocalizationService.Format("ui.cooldown.value", "CD {0}", selected.CurrentSkillCooldown)
                        : LocalizationService.Text("ui.cooldown.ready", "Ready"));

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
                RoleLabel = LocalizationService.Text(selected.RoleNameKey, selected.Role.ToString()),
                PositionLabel = LocalizationService.Format("ui.position.value", "Position ({0}, {1})", selected.Position.X, selected.Position.Y),
                Faction = selected.Faction,
                CurrentHp = selected.CurrentHp,
                MaxHp = selected.MaxHp,
                CurrentMana = selected.CurrentMana,
                MaxMana = selected.MaxMana,
                Attack = selected.Attack + PassiveSkillRules.GetAttackBonus(simulation.Context, selected) + StatusEffectRules.GetAttackModifier(selected),
                Defense = selected.Defense + PassiveSkillRules.GetDefenseBonus(selected) + StatusEffectRules.GetDefenseModifier(selected),
                MoveRange = PassiveSkillRules.GetMoveRange(selected),
                AttackRange = PassiveSkillRules.GetAttackRange(selected),
                WeaponTypeLabel = LocalizationService.Text(loadoutProfile.WeaponTypeKey, loadoutProfile.WeaponTypeFallback),
                WeaponName = LocalizationService.Text(loadoutProfile.WeaponNameKey, loadoutProfile.WeaponNameFallback),
                WeaponDescription = LocalizationService.Text(loadoutProfile.WeaponDescriptionKey, loadoutProfile.WeaponDescriptionFallback),
                WeaponAccentColor = loadoutProfile.AccentColor,
                PassiveName = LocalizationService.Text(selected.PassiveSkillNameKey, selected.PassiveSkill.ToString()),
                PassiveDescription = LocalizationService.Text(selected.PassiveSkillDescriptionKey, selected.PassiveSkill.ToString()),
                ActiveName = LocalizationService.Text(selected.ActiveSkillNameKey, selected.ActiveSkill.ToString()),
                ActiveDescription = LocalizationService.Text(selected.ActiveSkillDescriptionKey, selected.ActiveSkill.ToString()),
                CooldownLabel = cooldownText,
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
                    PositionLabel = LocalizationService.Format("ui.position.compact", "({0},{1})", unit.Position.X, unit.Position.Y),
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
            gridManager.ClearHighlights();
            gridManager.ShowSkillRange(simulation.GetSkillRange(caster.Id));
            gridManager.HighlightSelectedCell(caster.Position);

            if (affectedUnits.Count > 1)
            {
                gridManager.ShowAttackRange(affectedUnits.Select(unit => unit.Position));
            }
        }

        private BattleForecastModel BuildAttackPreviewModel(UnitRuntimeState attacker, UnitRuntimeState defender)
        {
            int damage = BattlePreviewCalculator.EstimateAttackDamage(simulation.Context, attacker, attacker.Position, defender);
            bool defenderFalls = damage >= defender.CurrentHp;
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
                    attacker.Attack + PassiveSkillRules.GetAttackBonus(simulation.Context, attacker) + StatusEffectRules.GetAttackModifier(attacker),
                    defender.Defense + PassiveSkillRules.GetDefenseBonus(defender) + StatusEffectRules.GetDefenseModifier(defender)),
                Footer = BuildAttackForecastFooter(attacker, defender),
                AccentColor = attacker.Faction == UnitFaction.Player ? new Color(0.28f, 0.58f, 0.98f, 1f) : new Color(0.92f, 0.36f, 0.28f, 1f),
            };
        }

        private BattleForecastModel BuildSkillPreviewModel(UnitRuntimeState caster, UnitRuntimeState primaryTarget)
        {
            string skillName = GetSkillDisplayName(caster.Id, caster.ActiveSkill);
            string title = LocalizationService.Format("ui.forecast.skill.title", "{0} -> {1}", skillName, GetUnitDisplayName(primaryTarget.Id));
            string manaText = LocalizationService.Format("ui.skill.mana_cost", "Cost {0} MP", ActiveSkillRules.GetManaCost(caster.ActiveSkill));
            string summary;
            string detail;
            string footer = string.Empty;
            Color accent = caster.Faction == UnitFaction.Player ? new Color(0.27f, 0.65f, 0.98f, 1f) : new Color(0.92f, 0.36f, 0.28f, 1f);

            switch (caster.ActiveSkill)
            {
                case ActiveSkillType.RoyalAid:
                    int healAmount = BattlePreviewCalculator.EstimateHealing(primaryTarget, ActiveSkillRules.GetRoyalAidAmount());
                    summary = LocalizationService.Format("ui.forecast.skill.heal", "Heal {0} HP", healAmount);
                    detail = LocalizationService.Format("ui.forecast.skill.status", "Applies {0}", GetStatusDisplayName(StatusEffectType.Inspired));
                    footer = manaText;
                    accent = new Color(0.34f, 0.82f, 0.58f, 1f);
                    break;
                case ActiveSkillType.PowerStrike:
                    int powerStrikeDamage = BattlePreviewCalculator.EstimateAttackDamage(
                        simulation.Context,
                        caster,
                        caster.Position,
                        primaryTarget,
                        ActiveSkillRules.GetPowerStrikeBonus());
                    bool powerStrikeKo = powerStrikeDamage >= primaryTarget.CurrentHp;
                    summary = powerStrikeKo
                        ? LocalizationService.Format("ui.forecast.attack.ko", "Projected damage {0} | KO", powerStrikeDamage)
                        : LocalizationService.Format("ui.forecast.attack.damage", "Projected damage {0} | {1} HP left", powerStrikeDamage, Mathf.Max(0, primaryTarget.CurrentHp - powerStrikeDamage));
                    detail = powerStrikeKo
                        ? LocalizationService.Text("ui.forecast.skill.power_strike_ko", "The target falls before armor can shatter.")
                        : LocalizationService.Format("ui.forecast.skill.status", "Applies {0}", GetStatusDisplayName(StatusEffectType.ShatteredArmor));
                    footer = manaText;
                    break;
                case ActiveSkillType.Volley:
                    IReadOnlyList<UnitRuntimeState> volleyTargets = BattlePreviewCalculator.GetVolleyTargets(simulation.Context, primaryTarget);
                    int volleyPrimaryDamage = BattlePreviewCalculator.EstimateAttackDamage(
                        simulation.Context,
                        caster,
                        caster.Position,
                        primaryTarget,
                        ActiveSkillRules.GetVolleyBonus());
                    summary = LocalizationService.Format(
                        "ui.forecast.skill.volley",
                        "Primary {0} | Splash {1} | {2} targets",
                        volleyPrimaryDamage,
                        Mathf.Max(0, volleyTargets.Count - 1),
                        volleyTargets.Count);
                    detail = LocalizationService.Format(
                        "ui.forecast.skill.targets",
                        "Affects {0}",
                        string.Join(", ", volleyTargets.Select(unit => GetUnitDisplayName(unit.Id))));
                    footer = LocalizationService.Format("ui.forecast.skill.status", "Applies {0} to surviving targets", GetStatusDisplayName(StatusEffectType.ShatteredArmor)) + " | " + manaText;
                    break;
                case ActiveSkillType.GreenDragonSlash:
                    IReadOnlyList<UnitRuntimeState> slashTargets = BattlePreviewCalculator.GetGreenDragonSlashTargets(simulation.Context, caster.Position, primaryTarget);
                    int slashDamage = BattlePreviewCalculator.EstimateAttackDamage(
                        simulation.Context,
                        caster,
                        caster.Position,
                        primaryTarget,
                        ActiveSkillRules.GetGreenDragonSlashBonus());
                    summary = LocalizationService.Format(
                        "ui.forecast.skill.green_dragon",
                        "Primary {0} | Cleave {1}",
                        slashDamage,
                        slashTargets.Count);
                    detail = LocalizationService.Format(
                        "ui.forecast.skill.targets",
                        "Affects {0}",
                        string.Join(", ", slashTargets.Select(unit => GetUnitDisplayName(unit.Id))));
                    footer = manaText;
                    break;
                case ActiveSkillType.WarCry:
                    IReadOnlyList<UnitRuntimeState> warCryTargets = simulation.Context.GetUnits(primaryTarget.Faction)
                        .Where(unit => caster.Position.ManhattanDistance(unit.Position) <= ActiveSkillRules.GetRange(caster))
                        .OrderBy(unit => caster.Position.ManhattanDistance(unit.Position))
                        .ThenBy(unit => unit.Id)
                        .ToList();
                    summary = LocalizationService.Format("ui.forecast.skill.war_cry", "Affects {0} nearby foes", warCryTargets.Count);
                    detail = LocalizationService.Format("ui.forecast.skill.targets", "Affects {0}", string.Join(", ", warCryTargets.Select(unit => GetUnitDisplayName(unit.Id))));
                    footer = LocalizationService.Format("ui.forecast.skill.status", "Applies {0}", GetStatusDisplayName(StatusEffectType.Intimidated)) + " | " + manaText;
                    break;
                default:
                    summary = LocalizationService.Text("ui.forecast.skill.none", "No forecast available.");
                    detail = string.Empty;
                    break;
            }

            return new BattleForecastModel
            {
                Header = LocalizationService.Text("ui.forecast.skill.header", "Skill Forecast"),
                Title = title,
                Summary = summary,
                Detail = detail,
                Footer = footer,
                AccentColor = accent,
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
                Footer = BuildCombatLog(combatResult),
                AccentColor = new Color(0.96f, 0.42f, 0.26f, 1f),
            };
        }

        private BattleForecastModel BuildSkillResultForecastModel(SkillResult skillResult)
        {
            string casterName = GetUnitDisplayName(skillResult.CasterUnitId);
            string skillName = GetSkillDisplayName(skillResult.CasterUnitId, skillResult.SkillType);
            return new BattleForecastModel
            {
                Header = LocalizationService.Text("ui.forecast.result.header", "Battle Result"),
                Title = LocalizationService.Format("ui.skill.banner", "{0} uses {1}", casterName, skillName),
                Summary = BuildSkillBannerDetail(skillResult),
                Detail = skillResult.Effects.Count > 1
                    ? LocalizationService.Format("ui.forecast.skill.targets", "Affects {0}", string.Join(", ", skillResult.Effects.Select(effect => GetUnitDisplayName(effect.UnitId))))
                    : string.Empty,
                Footer = BuildSkillLog(skillResult),
                AccentColor = new Color(0.32f, 0.72f, 0.98f, 1f),
            };
        }

        private string BuildAttackForecastFooter(UnitRuntimeState attacker, UnitRuntimeState defender)
        {
            List<string> notes = new List<string>();
            if (PassiveSkillRules.GetAttackBonus(simulation.Context, attacker) > 0)
            {
                notes.Add(LocalizationService.Text("skill.command_aura.name", "Command Aura"));
            }

            if (PassiveSkillRules.GetDamageBonus(attacker) > 0)
            {
                notes.Add(LocalizationService.Text("skill.vanguard.name", "Vanguard"));
            }

            if (PassiveSkillRules.GetIgnoredDefense(attacker) > 0)
            {
                notes.Add(LocalizationService.Text("skill.armor_break.name", "Armor Break"));
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

            if (skillResult.Effects[0].IsHealing)
            {
                int total = skillResult.Effects.Sum(effect => effect.Amount);
                return LocalizationService.Format("ui.skill.detail.heal", "+{0} HP", total);
            }

            if (skillResult.Effects.All(effect => !effect.IsHealing && effect.Amount <= 0 && effect.AppliedStatus != StatusEffectType.None))
            {
                return LocalizationService.Format("ui.skill.detail.status", "{0} units afflicted", skillResult.Effects.Count);
            }

            if (skillResult.Effects.Count == 1)
            {
                return LocalizationService.Format("ui.skill.detail.damage", "-{0} HP", skillResult.Effects[0].Amount);
            }

            return LocalizationService.Format("ui.skill.detail.multi", "{0} targets hit", skillResult.Effects.Count);
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
                return LocalizationService.Text("ui.cooldown.none", "-");
            }

            if (unit.CurrentSkillCooldown > 0)
            {
                return LocalizationService.Format("ui.cooldown.value", "CD {0}", unit.CurrentSkillCooldown);
            }

            return unit.HasActed
                ? LocalizationService.Text("ui.roster.skill_spent", "Spent")
                : LocalizationService.Text("ui.roster.skill_ready", "Skill Ready");
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

            battleHUD.ClearForecast();
            battleHUD.HideResult();
            battleHUD.HideDialogue();

            scenarioData = data;
            simulation = new BattleSimulation(scenarioData.Stage);
            scenarioDirector = new ScenarioDirector(scenarioData);
            gridManager.BuildGrid(simulation.Context.Width, simulation.Context.Height, simulation.Context.BlockedCells, OnCellClicked);
            CreateUnits();
            ConfigureCamera();
            battleHUD.SetRerollEnabled(simulation.Context.IsRandomMap);
            RefreshAllVisuals();
        }

        private bool IsInteractionLocked()
        {
            return currentState is ScenarioDialogueState;
        }

        private ScenarioEvaluationResult ProcessScenarioCheckpoint(ScenarioCheckpoint checkpoint)
        {
            ScenarioEvaluationResult result = scenarioDirector != null
                ? scenarioDirector.Evaluate(checkpoint, simulation.Context)
                : new ScenarioEvaluationResult(Array.Empty<string>(), false, false, false);

            UpdateHudModels();
            if (result.SpawnedUnitIds.Count > 0)
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
