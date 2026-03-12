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
        private EnemyTacticalPlan cachedEnemyTurnPlan;

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

        public IReadOnlyList<GridPosition> GetMoveDestinations(string unitId)
        {
            UnitRuntimeState unit = Context.GetUnit(unitId);
            return CanControlUnit(unit) ? rangeCalculator.GetMoveDestinations(Context, unit) : new List<GridPosition>();
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

        public IReadOnlyList<UnitRuntimeState> GetSkillAffectedTargets(string casterUnitId, string primaryTargetId)
        {
            UnitRuntimeState caster = Context.GetUnit(casterUnitId);
            UnitRuntimeState primaryTarget = Context.GetUnit(primaryTargetId);
            return CanControlUnit(caster) && primaryTarget != null
                ? skillSystem.GetSkillAffectedUnits(Context, caster, primaryTarget)
                : new List<UnitRuntimeState>();
        }

        public IReadOnlyList<GridPosition> GetSkillAffectedPositions(string casterUnitId, string primaryTargetId)
        {
            UnitRuntimeState caster = Context.GetUnit(casterUnitId);
            UnitRuntimeState primaryTarget = Context.GetUnit(primaryTargetId);
            return CanControlUnit(caster) && primaryTarget != null
                ? skillSystem.GetSkillAffectedPositions(Context, caster, primaryTarget)
                : new List<GridPosition>();
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

        public BattlePathPreview GetMovePreview(string unitId, GridPosition destination)
        {
            UnitRuntimeState unit = Context.GetUnit(unitId);
            if (!CanControlUnit(unit))
            {
                return null;
            }

            if (!rangeCalculator.TryBuildMovePath(Context, unit, destination, out IReadOnlyList<GridPosition> path, out int moveCost))
            {
                return null;
            }

            return new BattlePathPreview(
                destination,
                path,
                moveCost,
                rangeCalculator.GetAttackableTargets(Context, unit, destination).Count,
                skillSystem.GetSkillTargets(Context, unit, destination).Count);
        }

        public BattleIntentPreview PreviewMoveIntent(string unitId, GridPosition destination)
        {
            UnitRuntimeState unit = Context.GetUnit(unitId);
            if (!CanControlUnit(unit))
            {
                return CreateInvalidPreview(BattleIntentActionKind.Move, unit, destination, string.Empty);
            }

            if (!rangeCalculator.TryBuildMovePath(Context, unit, destination, out IReadOnlyList<GridPosition> path, out int moveCost))
            {
                return CreateInvalidPreview(BattleIntentActionKind.Move, unit, destination, "Destination blocked");
            }

            return new BattleIntentPreview(
                BattleIntentActionKind.Move,
                unit.Id,
                unit.Position,
                destination,
                path,
                moveCost,
                string.Empty,
                new List<BattleIntentEffectPreview>(),
                BattleThreatAnalyzer.AnalyzeProjected(Context, unit, destination),
                "Move " + moveCost,
                "Reposition",
                0,
                true,
                string.Empty);
        }

        public bool TryBuildMovePath(string unitId, GridPosition destination, out IReadOnlyList<GridPosition> path, out int moveCost)
        {
            UnitRuntimeState unit = Context.GetUnit(unitId);
            if (!CanControlUnit(unit))
            {
                path = new List<GridPosition>();
                moveCost = 0;
                return false;
            }

            return rangeCalculator.TryBuildMovePath(Context, unit, destination, out path, out moveCost);
        }

        public bool TryUndoMoveUnit(string unitId, GridPosition origin)
        {
            UnitRuntimeState unit = Context.GetUnit(unitId);
            if (!CanControlUnit(unit) || !unit.HasMovedThisTurn)
            {
                return false;
            }

            if (!Context.IsInside(origin) || !Context.IsWalkable(origin))
            {
                return false;
            }

            UnitRuntimeState occupant = Context.GetUnitAt(origin);
            if (occupant != null && occupant.Id != unitId)
            {
                return false;
            }

            Context.UndoMoveUnit(unitId, origin);
            return true;
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

        public BattleQuickAttackPreview GetQuickAttackPreview(string attackerUnitId, string targetUnitId)
        {
            UnitRuntimeState attacker = Context.GetUnit(attackerUnitId);
            UnitRuntimeState target = Context.GetUnit(targetUnitId);
            if (!CanControlUnit(attacker) || target == null || !target.IsAlive || attacker.Faction == target.Faction)
            {
                return null;
            }

            if (!TryFindAttackDestination(attackerUnitId, targetUnitId, out GridPosition destination))
            {
                return null;
            }

            if (!rangeCalculator.TryBuildMovePath(Context, attacker, destination, out IReadOnlyList<GridPosition> path, out int moveCost))
            {
                return null;
            }

            int effectiveAttack = attacker.Attack +
                                  PassiveSkillRules.GetPersonalAttackBonus(attacker) +
                                  PassiveSkillRules.GetAttackBonus(Context, attacker, destination) +
                                  SupportRules.GetAttackBonus(Context, attacker, destination) +
                                  PassiveSkillRules.GetDamageBonus(attacker, destination) +
                                  StatusEffectRules.GetAttackModifier(attacker);
            int effectiveDefense = target.Defense +
                                   PassiveSkillRules.GetDefenseBonus(target) +
                                   SupportRules.GetDefenseBonus(Context, target) +
                                   TerrainRules.GetDefenseBonus(Context.GetTerrainAt(target.Position)) +
                                   StatusEffectRules.GetDefenseModifier(target) -
                                   PassiveSkillRules.GetIgnoredDefense(attacker);
            if (effectiveDefense < 0)
            {
                effectiveDefense = 0;
            }

            int damage = BattlePreviewCalculator.EstimateAttackDamage(Context, attacker, destination, target);
            int remainingHp = target.CurrentHp - damage;
            return new BattleQuickAttackPreview(
                attackerUnitId,
                targetUnitId,
                destination,
                path,
                moveCost,
                damage,
                remainingHp < 0 ? 0 : remainingHp,
                damage >= target.CurrentHp,
                effectiveAttack,
                effectiveDefense);
        }

        public BattleIntentPreview PreviewAttackIntent(string attackerUnitId, string targetUnitId, GridPosition? destinationOverride = null)
        {
            UnitRuntimeState attacker = Context.GetUnit(attackerUnitId);
            UnitRuntimeState target = Context.GetUnit(targetUnitId);
            GridPosition destination = destinationOverride ?? (attacker != null ? attacker.Position : new GridPosition(0, 0));
            if (!CanControlUnit(attacker) || target == null || !target.IsAlive || attacker.Faction == target.Faction)
            {
                return CreateInvalidPreview(BattleIntentActionKind.Attack, attacker, destination, "No valid target");
            }

            if (!TryResolveProjectedOrigin(attacker, destination, out IReadOnlyList<GridPosition> path, out int moveCost))
            {
                return CreateInvalidPreview(BattleIntentActionKind.Attack, attacker, destination, "Destination blocked");
            }

            int attackRange = PassiveSkillRules.GetAttackRange(attacker);
            if (destination.ManhattanDistance(target.Position) <= 0 || destination.ManhattanDistance(target.Position) > attackRange)
            {
                return CreateInvalidPreview(BattleIntentActionKind.Attack, attacker, destination, "Target out of range");
            }

            int damage = BattlePreviewCalculator.EstimateAttackDamage(Context, attacker, destination, target);
            BattleIntentEffectPreview effect = new BattleIntentEffectPreview(
                target.Id,
                damage,
                0,
                target.CurrentHp - damage < 0 ? 0 : target.CurrentHp - damage,
                damage >= target.CurrentHp,
                new List<SkillStatusApplication>(),
                isPrimaryTarget: true);

            return new BattleIntentPreview(
                BattleIntentActionKind.Attack,
                attacker.Id,
                attacker.Position,
                destination,
                path,
                moveCost,
                target.Id,
                new[] { effect },
                BattleThreatAnalyzer.AnalyzeProjected(Context, attacker, destination, effect.Lethal ? new[] { target.Id } : null),
                "Range " + attackRange,
                "Single target",
                0,
                true,
                string.Empty);
        }

        public BattleIntentPreview PreviewSkillIntent(string casterUnitId, string targetUnitId, GridPosition? destinationOverride = null)
        {
            UnitRuntimeState caster = Context.GetUnit(casterUnitId);
            UnitRuntimeState primaryTarget = Context.GetUnit(targetUnitId);
            GridPosition destination = destinationOverride ?? (caster != null ? caster.Position : new GridPosition(0, 0));
            if (!CanControlUnit(caster) || primaryTarget == null || !primaryTarget.IsAlive || caster.ActiveSkill == ActiveSkillType.None)
            {
                return CreateInvalidPreview(BattleIntentActionKind.Skill, caster, destination, "No valid skill target");
            }

            if (!TryResolveProjectedOrigin(caster, destination, out IReadOnlyList<GridPosition> path, out int moveCost))
            {
                return CreateInvalidPreview(BattleIntentActionKind.Skill, caster, destination, "Destination blocked");
            }

            if (!skillSystem.GetSkillTargets(Context, caster, destination).Any(unit => unit.Id == primaryTarget.Id))
            {
                return CreateInvalidPreview(BattleIntentActionKind.Skill, caster, destination, "No valid skill target");
            }

            IReadOnlyList<UnitRuntimeState> affectedUnits = skillSystem.GetSkillAffectedUnits(Context, caster, destination, primaryTarget);
            List<BattleIntentEffectPreview> effects = affectedUnits
                .Select(unit => BuildSkillEffectPreview(caster, destination, primaryTarget, unit))
                .ToList();

            return new BattleIntentPreview(
                BattleIntentActionKind.Skill,
                caster.Id,
                caster.Position,
                destination,
                path,
                moveCost,
                primaryTarget.Id,
                effects,
                BattleThreatAnalyzer.AnalyzeProjected(Context, caster, destination, effects.Where(effect => effect.Lethal).Select(effect => effect.UnitId).ToList()),
                "Range " + ActiveSkillRules.GetRange(caster),
                BuildSkillAreaLabel(caster.ActiveSkill),
                ActiveSkillRules.GetManaCost(caster.ActiveSkill),
                true,
                string.Empty);
        }

        public BattleIntentPreview PreviewQuickAttackIntent(string attackerUnitId, string targetUnitId)
        {
            UnitRuntimeState attacker = Context.GetUnit(attackerUnitId);
            UnitRuntimeState target = Context.GetUnit(targetUnitId);
            BattleQuickAttackPreview preview = GetQuickAttackPreview(attackerUnitId, targetUnitId);
            if (preview == null || attacker == null || target == null)
            {
                return CreateInvalidPreview(BattleIntentActionKind.QuickAttack, attacker, attacker != null ? attacker.Position : new GridPosition(0, 0), "No quick attack path");
            }

            BattleIntentEffectPreview effect = new BattleIntentEffectPreview(
                target.Id,
                preview.ProjectedDamage,
                0,
                preview.DefenderRemainingHp,
                preview.IsLethal,
                new List<SkillStatusApplication>(),
                isPrimaryTarget: true);

            return new BattleIntentPreview(
                BattleIntentActionKind.QuickAttack,
                attacker.Id,
                attacker.Position,
                preview.Destination,
                preview.Path,
                preview.MoveCost,
                target.Id,
                new[] { effect },
                BattleThreatAnalyzer.AnalyzeProjected(Context, attacker, preview.Destination, effect.Lethal ? new[] { target.Id } : null),
                "Range " + PassiveSkillRules.GetAttackRange(attacker),
                "Single target",
                0,
                true,
                string.Empty);
        }

        public TurnSide EndCurrentTurn()
        {
            cachedEnemyTurnPlan = null;
            return turnManager.EndTurn(Context);
        }

        public bool SpawnUnit(UnitSpawnData spawnData)
        {
            return Context.AddUnit(spawnData) != null;
        }

        public void SetBattleOutcome(TurnSide winningSide)
        {
            Context.SetBattleOutcome(winningSide);
        }

        public AiDecision BuildEnemyDecision(string unitId)
        {
            UnitRuntimeState enemy = Context.GetUnit(unitId);
            EnemyTacticalPlan plan = cachedEnemyTurnPlan ?? aiController.CreateTacticalPlan(Context);
            return aiController.Decide(Context, enemy, plan);
        }

        public UnitActionResult ResolveEnemyAction(string unitId)
        {
            UnitRuntimeState enemy = Context.GetUnit(unitId);
            if (!CanControlUnit(enemy) || enemy.HasActed)
            {
                return null;
            }

            GridPosition start = enemy.Position;
            EnemyTacticalPlan plan = cachedEnemyTurnPlan ?? aiController.CreateTacticalPlan(Context);
            AiDecision decision = aiController.Decide(Context, enemy, plan);
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

        public IReadOnlyList<string> BuildEnemyTurnOrder()
        {
            cachedEnemyTurnPlan = aiController.CreateTacticalPlan(Context);
            return cachedEnemyTurnPlan.OrderedUnitIds;
        }

        private bool TryResolveProjectedOrigin(UnitRuntimeState unit, GridPosition destination, out IReadOnlyList<GridPosition> path, out int moveCost)
        {
            if (unit == null)
            {
                path = new List<GridPosition>();
                moveCost = 0;
                return false;
            }

            if (destination == unit.Position)
            {
                path = new[] { unit.Position };
                moveCost = 0;
                return true;
            }

            return rangeCalculator.TryBuildMovePath(Context, unit, destination, out path, out moveCost);
        }

        private BattleIntentPreview CreateInvalidPreview(BattleIntentActionKind actionKind, UnitRuntimeState unit, GridPosition destination, string blockReason)
        {
            GridPosition origin = unit != null ? unit.Position : destination;
            return new BattleIntentPreview(
                actionKind,
                unit != null ? unit.Id : string.Empty,
                origin,
                destination,
                new[] { origin },
                0,
                string.Empty,
                new List<BattleIntentEffectPreview>(),
                new BattleThreatProjection(0, 0, new List<string>()),
                string.Empty,
                string.Empty,
                0,
                false,
                blockReason);
        }

        private BattleIntentEffectPreview BuildSkillEffectPreview(UnitRuntimeState caster, GridPosition origin, UnitRuntimeState primaryTarget, UnitRuntimeState target)
        {
            bool isHealing = ActiveSkillRules.IsSupportSkill(caster.ActiveSkill);
            int amount = EstimateSkillEffectAmount(caster, origin, primaryTarget, target);
            bool lethal = !isHealing && amount >= target.CurrentHp && amount > 0;
            int remainingHp = isHealing
                ? target.CurrentHp + amount > target.MaxHp ? target.MaxHp : target.CurrentHp + amount
                : target.CurrentHp - amount < 0 ? 0 : target.CurrentHp - amount;
            return new BattleIntentEffectPreview(
                target.Id,
                isHealing ? 0 : amount,
                isHealing ? amount : 0,
                remainingHp,
                lethal,
                BuildPreviewStatuses(caster, primaryTarget, target, !lethal),
                target.Id == primaryTarget.Id);
        }

        private int EstimateSkillEffectAmount(UnitRuntimeState caster, GridPosition origin, UnitRuntimeState primaryTarget, UnitRuntimeState target)
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
                    return BattlePreviewCalculator.EstimateAttackDamage(
                        Context,
                        caster,
                        origin,
                        target,
                        GetSkillFlatBonus(caster),
                        caster.ActiveSkill == ActiveSkillType.DragonPierce
                            ? ActiveSkillRules.GetDragonPierceIgnoredDefense(caster)
                            : 0);
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

        private static string BuildSkillAreaLabel(ActiveSkillType skillType)
        {
            switch (skillType)
            {
                case ActiveSkillType.RoyalAid:
                    return "Single ally";
                case ActiveSkillType.ImperialAid:
                case ActiveSkillType.GuardOrder:
                    return "Ally + adjacent";
                case ActiveSkillType.PowerStrike:
                case ActiveSkillType.PinningShot:
                case ActiveSkillType.DragonPierce:
                    return "Single foe";
                case ActiveSkillType.Volley:
                case ActiveSkillType.SkyVolley:
                case ActiveSkillType.FireStratagem:
                case ActiveSkillType.EightTrigramInferno:
                    return "Target + adjacent";
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.AzureDragonSlash:
                case ActiveSkillType.WesternStampede:
                    return "Line cleave";
                case ActiveSkillType.WarCry:
                case ActiveSkillType.LionWarCry:
                    return "Nearby foes";
                default:
                    return string.Empty;
            }
        }

        private static int GetSkillFlatBonus(UnitRuntimeState caster)
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
