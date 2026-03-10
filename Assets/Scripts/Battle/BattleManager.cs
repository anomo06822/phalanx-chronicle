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

        private StageDefinition stageDefinition;
        private BattleSimulation simulation;
        private GridManager gridManager;
        private BattleHUD battleHUD;
        private ActionMenuPanel actionMenuPanel;
        private IBattleState currentState;
        private string selectedUnitId;
        private CombatResult pendingCombatResult;
        private SkillResult pendingSkillResult;
        private bool initialized;
        private GameObject unitRoot;

        public void Initialize(StageDefinition stage)
        {
            stageDefinition = stage != null ? stage : StageDefinition.CreateDefault();
        }

        public void ChangeState<TState>() where TState : IBattleState
        {
            if (!states.TryGetValue(typeof(TState), out IBattleState nextState))
            {
                throw new InvalidOperationException($"State {typeof(TState).Name} is not registered.");
            }

            currentState?.Exit();
            currentState = nextState;
            currentState.Enter();
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
            battleHUD.SetTurn(text);
        }

        public void SetLog(string text)
        {
            battleHUD.SetLog(text);
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
            RefreshAllVisuals();
        }

        public bool IsSelectedUnit(string unitId)
        {
            return !string.IsNullOrEmpty(selectedUnitId) && selectedUnitId == unitId;
        }

        public void ClearSelectionAndHighlights()
        {
            selectedUnitId = null;
            gridManager.ClearHighlights();
            battleHUD.HideCombatBanner();
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
            gridManager.ShowMoveRange(simulation.GetMoveRange(selected.Id));
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

        public void ShowActionMenu(bool canAttack)
        {
            actionMenuPanel.Show(
                canAttack,
                GetSelectedActiveSkillDisplayName(),
                HasSkillTargetsForSelection(),
                HandleAttackRequested,
                HandleSkillRequested,
                HandleWaitRequested);
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
            return selected != null && selected.CanUseSkill && simulation.GetSkillTargets(selected.Id).Count > 0;
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

            if (combatResult == null)
            {
                ChangeState<UnitSelectionState>();
                yield break;
            }
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

        public void ResolvePlayerAction(string logMessage)
        {
            SetLog(logMessage);
            HideActionMenu();
            battleHUD.HideCombatBanner();

            if (simulation.Context.BattleEnded)
            {
                ChangeState(simulation.Context.WinningSide == TurnSide.Player ? typeof(BattleVictoryState) : typeof(BattleDefeatState));
                return;
            }

            ClearSelectionAndHighlights();
            if (AreAllPlayerUnitsDone())
            {
                ChangeState<EnemyTurnState>();
                return;
            }

            SetEndTurnEnabled(true);
            ChangeState<UnitSelectionState>();
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
                    SetLog(BuildCombatLog(actionResult.CombatResult));
                }
                else if (actionResult.PerformedSkill)
                {
                    yield return PlaySkillSequence(actionResult.SkillResult);
                    SetLog(BuildSkillLog(actionResult.SkillResult));
                }
                else
                {
                    RefreshAllVisuals();
                    if (actionResult.EndPosition != actionResult.StartPosition)
                    {
                        SetLog(LocalizationService.Format("ui.log.enemy_advanced", "{0} advanced.", GetUnitDisplayName(enemy.Id)));
                    }
                    else
                    {
                        SetLog(LocalizationService.Format("ui.log.enemy_held", "{0} held position.", GetUnitDisplayName(enemy.Id)));
                    }
                }

                yield return new WaitForSeconds(0.35f);

                if (simulation.Context.BattleEnded)
                {
                    battleHUD.HideCombatBanner();
                    ChangeState<BattleDefeatState>();
                    yield break;
                }
            }

            simulation.EndCurrentTurn();
            battleHUD.HideCombatBanner();
            ChangeState<PlayerTurnStartState>();
        }

        public void RefreshAllVisuals()
        {
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

            UpdateSelectedHud();
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

            if (stageDefinition == null)
            {
                stageDefinition = StageDefinition.CreateDefault();
            }

            RegisterStates();
            LoadStage(stageDefinition.ToData());
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
            battleHUD.Initialize(canvasObject.transform, HandleEndTurnRequested, HandleRerollRequested);

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

            foreach (UnitRuntimeState runtimeState in simulation.Context.Units)
            {
                GameObject unitObject = new GameObject(runtimeState.Id);
                unitObject.transform.SetParent(unitRoot.transform, false);
                Unit unitView = unitObject.AddComponent<Unit>();
                unitView.Initialize(runtimeState, OnUnitClicked);
                unitViews[runtimeState.Id] = unitView;
            }

            RefreshAllVisuals();
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
            mainCamera.backgroundColor = new Color(0.58f, 0.63f, 0.52f, 1f);
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
            states[typeof(BattleVictoryState)] = new BattleVictoryState(this);
            states[typeof(BattleDefeatState)] = new BattleDefeatState(this);
        }

        private void OnUnitClicked(Unit unitView)
        {
            currentState?.OnUnitClicked(unitView);
        }

        private void OnCellClicked(GridCellView cellView)
        {
            currentState?.OnCellClicked(cellView.Position);
        }

        private void HandleAttackRequested()
        {
            currentState?.OnAttackRequested();
        }

        private void HandleSkillRequested()
        {
            currentState?.OnSkillRequested();
        }

        private void HandleWaitRequested()
        {
            currentState?.OnWaitRequested();
        }

        private void HandleEndTurnRequested()
        {
            currentState?.OnEndTurnRequested();
        }

        private void HandleRerollRequested()
        {
            if (stageDefinition == null || !stageDefinition.UseRandomMap)
            {
                return;
            }

            LoadStage(stageDefinition.CreateRerolledData());
            ChangeState<BattleStartState>();
        }

        private UnitRuntimeState GetSelectedUnit()
        {
            return string.IsNullOrEmpty(selectedUnitId) ? null : simulation.Context.GetUnit(selectedUnitId);
        }

        private Unit GetUnitView(string unitId)
        {
            return unitViews.TryGetValue(unitId, out Unit unitView) ? unitView : null;
        }

        private void UpdateSelectedHud()
        {
            UnitRuntimeState selected = GetSelectedUnit();
            if (selected == null)
            {
                battleHUD.SetSelectedUnit(LocalizationService.Text("ui.selected.none", "Selected: None"));
                return;
            }

            string status = selected.HasActed
                ? LocalizationService.Text("ui.status.done", "DONE")
                : LocalizationService.Text("ui.status.ready", "READY");
            string passiveDescription = LocalizationService.Text(selected.PassiveSkillDescriptionKey, selected.PassiveSkill.ToString());
            string activeDescription = LocalizationService.Text(selected.ActiveSkillDescriptionKey, selected.ActiveSkill.ToString());
            string cooldownText = selected.ActiveSkill == ActiveSkillType.None
                ? LocalizationService.Text("ui.cooldown.none", "-")
                : (selected.CurrentSkillCooldown > 0
                    ? LocalizationService.Format("ui.cooldown.value", "CD {0}", selected.CurrentSkillCooldown)
                    : LocalizationService.Text("ui.cooldown.ready", "Ready"));
            string statusSummary = BuildStatusSummary(selected);
            battleHUD.SetSelectedUnit(LocalizationService.Format(
                "ui.selected.summary",
                "Selected: {0}\nRole {1}\nPassive {2}  Active {3}\nHP {4}/{5}  ATK {6}  DEF {7}  MOVE {8}  RANGE {9}  {10}",
                GetUnitDisplayName(selected.Id),
                LocalizationService.Text(selected.RoleNameKey, selected.Role.ToString()),
                LocalizationService.Text(selected.PassiveSkillNameKey, selected.PassiveSkill.ToString()),
                LocalizationService.Text(selected.ActiveSkillNameKey, selected.ActiveSkill.ToString()),
                selected.CurrentHp,
                selected.MaxHp,
                selected.Attack + PassiveSkillRules.GetAttackBonus(simulation.Context, selected) + StatusEffectRules.GetAttackModifier(selected),
                selected.Defense + PassiveSkillRules.GetDefenseBonus(selected) + StatusEffectRules.GetDefenseModifier(selected),
                PassiveSkillRules.GetMoveRange(selected),
                PassiveSkillRules.GetAttackRange(selected),
                status) +
                "\n" + LocalizationService.Text("ui.label.cooldown", "Cooldown") + ": " + cooldownText +
                "    " + LocalizationService.Text("ui.label.status", "Status") + ": " + statusSummary +
                "\n" + passiveDescription +
                "\n" + activeDescription);
        }

        private IEnumerator PlaySkillSequence(SkillResult skillResult)
        {
            if (skillResult == null)
            {
                yield break;
            }

            Unit casterView = GetUnitView(skillResult.CasterUnitId);
            Unit primaryTargetView = GetUnitView(skillResult.PrimaryTargetUnitId);
            string casterName = GetUnitDisplayName(skillResult.CasterUnitId);
            string skillName = GetSkillDisplayName(skillResult.CasterUnitId, skillResult.SkillType);
            string detail = BuildSkillBannerDetail(skillResult);

            battleHUD.ShowCombatBanner(
                LocalizationService.Format("ui.skill.banner", "{0} uses {1}", casterName, skillName),
                detail);

            if (casterView != null && primaryTargetView != null && ActiveSkillRules.IsOffensiveSkill(skillResult.SkillType))
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
                    FloatingText.Spawn("+" + effect.Amount, targetView.GetAnchorPosition(0.98f), new Color(0.54f, 1f, 0.62f, 1f));
                    yield return targetView.AnimatePulse(new Color(0.7f, 1f, 0.78f, 1f));
                }
                else
                {
                    yield return PlaySlashEffect(targetView.GetAnchorPosition(0.12f));
                    FloatingText.Spawn("-" + effect.Amount, targetView.GetAnchorPosition(0.98f), new Color(1f, 0.89f, 0.4f, 1f));
                    yield return targetView.AnimateHit();

                    if (effect.UnitDied)
                    {
                        FloatingText.Spawn(LocalizationService.Text("ui.combat.popup_ko", "KO"), targetView.GetAnchorPosition(1.24f), new Color(1f, 0.56f, 0.42f, 1f));
                    }
                }

                if (effect.AppliedStatus != StatusEffectType.None)
                {
                    FloatingText.Spawn(GetStatusDisplayName(effect.AppliedStatus), targetView.GetAnchorPosition(1.18f), new Color(0.76f, 0.96f, 1f, 1f));
                }

                yield return new WaitForSeconds(0.05f);
            }

            yield return new WaitForSeconds(0.18f);
            battleHUD.HideCombatBanner();
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
            string attackerName = GetUnitDisplayName(combatResult.AttackerUnitId);
            string defenderName = GetUnitDisplayName(combatResult.DefenderUnitId);
            string detail = combatResult.DefenderDied
                ? LocalizationService.Format("ui.combat.ko", "-{0} HP   KO", combatResult.Damage)
                : LocalizationService.Format("ui.combat.damage", "-{0} HP   {1} left", combatResult.Damage, combatResult.DefenderRemainingHp);

            battleHUD.ShowCombatBanner(
                LocalizationService.Format("ui.combat.banner", "{0} strikes {1}", attackerName, defenderName),
                detail);

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
            battleHUD.HideCombatBanner();
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

        private void ChangeState(Type stateType)
        {
            if (stateType == typeof(BattleVictoryState))
            {
                ChangeState<BattleVictoryState>();
            }
            else if (stateType == typeof(BattleDefeatState))
            {
                ChangeState<BattleDefeatState>();
            }
        }

        private void LoadStage(StageDefinitionData stageData)
        {
            StopAllCoroutines();
            currentState?.Exit();
            currentState = null;
            selectedUnitId = null;
            pendingCombatResult = null;
            pendingSkillResult = null;

            battleHUD.HideCombatBanner();
            battleHUD.HideResult();

            simulation = new BattleSimulation(stageData);
            gridManager.BuildGrid(simulation.Context.Width, simulation.Context.Height, simulation.Context.BlockedCells, OnCellClicked);
            CreateUnits();
            ConfigureCamera();
            battleHUD.SetStage(LocalizationService.Format("ui.stage", "Stage: {0}", LocalizationService.Text(simulation.Context.StageNameKey, simulation.Context.StageName)));
            battleHUD.SetMapSeed(
                simulation.Context.IsRandomMap
                    ? LocalizationService.Format("ui.seed.value", "Seed: {0}", simulation.Context.MapSeed)
                    : LocalizationService.Text("ui.seed.fixed", "Seed: Fixed"));
            battleHUD.SetRerollEnabled(stageDefinition != null && stageDefinition.UseRandomMap);
            RefreshAllVisuals();
        }
    }
}
