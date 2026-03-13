using System;
using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class AIController
    {
        private const float DamageWeight = 4f;
        private const float SkillDamageWeight = 4.5f;
        private const float HealingWeight = 3.5f;
        private const float KillBonus = 32f;
        private const float StatusBonus = 7f;
        private const float AdditionalTargetBonus = 8f;
        private const float SkillCommitmentPenalty = 5f;
        private const float ThreatDamageWeight = 0.8f;
        private const float ThreatUnitPenalty = 2.5f;
        private const float LethalThreatPenalty = 10f;
        private const float PressureBaseScore = 20f;
        private const float PressureDistancePenalty = 2.5f;
        private const float FocusFireBonus = 10f;
        private const float ChokeHoldBonus = 12f;
        private const float ProtectorScreenBonus = 8f;
        private const float SupportSafetyPenalty = 10f;
        private const float LowValueSkillPenalty = 18f;
        private const float BossHoldLinePenalty = 18f;
        private const float BossHoldLineBonus = 10f;

        private readonly RangeCalculator rangeCalculator;
        private readonly SkillSystem skillSystem;

        public AIController(RangeCalculator rangeCalculator, SkillSystem skillSystem)
        {
            this.rangeCalculator = rangeCalculator;
            this.skillSystem = skillSystem;
        }

        public AiDecision Decide(BattleContext context, UnitRuntimeState enemyUnit)
        {
            return Decide(context, enemyUnit, CreateTacticalPlan(context));
        }

        internal AiDecision Decide(BattleContext context, UnitRuntimeState enemyUnit, EnemyTacticalPlan plan)
        {
            IReadOnlyList<UnitRuntimeState> playerUnits = context.GetUnits(UnitFaction.Player);
            if (enemyUnit == null || !enemyUnit.IsAlive || enemyUnit.HasActed || playerUnits.Count == 0)
            {
                return new AiDecision(enemyUnit != null ? enemyUnit.Position : new GridPosition(0, 0), AiActionType.None, null);
            }

            AiCandidate bestCandidate = BuildCandidates(context, enemyUnit, plan)
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Risk)
                .ThenByDescending(candidate => candidate.ActionPriority)
                .ThenBy(candidate => candidate.TargetHp)
                .ThenBy(candidate => candidate.NearestOpponentDistance)
                .ThenBy(candidate => candidate.MoveDistance)
                .ThenBy(candidate => candidate.Destination.Y)
                .ThenBy(candidate => candidate.Destination.X)
                .First();

            return new AiDecision(bestCandidate.Destination, bestCandidate.ActionType, bestCandidate.TargetUnitId);
        }

        internal EnemyTacticalPlan CreateTacticalPlan(BattleContext context)
        {
            List<UnitRuntimeState> enemies = context.GetUnits(UnitFaction.Enemy).ToList();
            if (enemies.Count == 0)
            {
                return new EnemyTacticalPlan(string.Empty, new HashSet<string>(), new HashSet<GridPosition>(), new List<string>(), false);
            }

            string focusTargetId = SelectFocusTarget(context, enemies);
            HashSet<string> protectedUnitIds = SelectProtectedUnitIds(context, enemies);
            HashSet<GridPosition> chokeTiles = GetStageChokeTiles(context);
            List<string> orderedUnitIds = BuildEnemyTurnOrder(context, enemies, focusTargetId, protectedUnitIds, chokeTiles);
            return new EnemyTacticalPlan(focusTargetId, protectedUnitIds, chokeTiles, orderedUnitIds, enemies.Count >= 2);
        }

        private IEnumerable<AiCandidate> BuildCandidates(BattleContext context, UnitRuntimeState enemyUnit, EnemyTacticalPlan plan)
        {
            IReadOnlyList<GridPosition> reachableCells = rangeCalculator.GetMoveRange(context, enemyUnit);
            foreach (GridPosition destination in reachableCells)
            {
                yield return CreateIdleCandidate(context, enemyUnit, destination, plan);

                foreach (UnitRuntimeState attackTarget in rangeCalculator.GetAttackableTargets(context, enemyUnit, destination))
                {
                    yield return CreateAttackCandidate(context, enemyUnit, destination, attackTarget, plan);
                }

                foreach (UnitRuntimeState skillTarget in skillSystem.GetSkillTargets(context, enemyUnit, destination))
                {
                    yield return CreateSkillCandidate(context, enemyUnit, destination, skillTarget, plan);
                }
            }
        }

        private AiCandidate CreateIdleCandidate(BattleContext context, UnitRuntimeState enemyUnit, GridPosition destination, EnemyTacticalPlan plan)
        {
            int nearestOpponentDistance = CalculateNearestOpponentDistance(context, enemyUnit.Faction, destination, null);
            float pressure = CalculatePressureScore(nearestOpponentDistance) + GetProfilePressureBonus(context, enemyUnit, destination);
            float risk = EstimateExposure(context, enemyUnit, destination, null) * GetRiskMultiplier(enemyUnit.AiProfile);
            float tacticalBonus = GetTacticalBonus(context, enemyUnit, destination, AiActionType.None, null, false, plan);
            return new AiCandidate(
                destination,
                AiActionType.None,
                null,
                pressure + tacticalBonus - risk,
                risk,
                int.MaxValue,
                nearestOpponentDistance,
                enemyUnit.Position.ManhattanDistance(destination));
        }

        private AiCandidate CreateAttackCandidate(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target,
            EnemyTacticalPlan plan)
        {
            int rawDamage = BattlePreviewCalculator.EstimateAttackDamage(context, enemyUnit, destination, target);
            bool targetDies = rawDamage >= target.CurrentHp;
            int realizedDamage = rawDamage > target.CurrentHp ? target.CurrentHp : rawDamage;
            HashSet<string> defeatedUnitIds = targetDies ? new HashSet<string> { target.Id } : null;
            int nearestOpponentDistance = CalculateNearestOpponentDistance(context, enemyUnit.Faction, destination, defeatedUnitIds);
            float pressure = CalculatePressureScore(nearestOpponentDistance) + GetProfilePressureBonus(context, enemyUnit, destination);
            float risk = EstimateExposure(context, enemyUnit, destination, defeatedUnitIds) * GetRiskMultiplier(enemyUnit.AiProfile);
            float reward = 6f + (realizedDamage * DamageWeight) + (targetDies ? KillBonus : 0f) + GetActionBias(enemyUnit.AiProfile, false, targetDies);
            float tacticalBonus = GetTacticalBonus(context, enemyUnit, destination, AiActionType.Attack, target, targetDies, plan);

            return new AiCandidate(
                destination,
                AiActionType.Attack,
                target.Id,
                reward + pressure + tacticalBonus - risk,
                risk,
                target.CurrentHp,
                nearestOpponentDistance,
                enemyUnit.Position.ManhattanDistance(destination));
        }

        private AiCandidate CreateSkillCandidate(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target,
            EnemyTacticalPlan plan)
        {
            SkillEvaluation evaluation = EvaluateSkill(context, enemyUnit, destination, target);
            int nearestOpponentDistance = CalculateNearestOpponentDistance(context, enemyUnit.Faction, destination, evaluation.DefeatedUnitIds);
            float pressure = CalculatePressureScore(nearestOpponentDistance) + GetProfilePressureBonus(context, enemyUnit, destination);
            float risk = EstimateExposure(context, enemyUnit, destination, evaluation.DefeatedUnitIds) * GetRiskMultiplier(enemyUnit.AiProfile);
            bool targetDies = evaluation.DefeatedUnitIds.Contains(target.Id);
            float tacticalBonus = GetTacticalBonus(context, enemyUnit, destination, AiActionType.Skill, target, targetDies, plan);
            tacticalBonus += GetSkillValueAdjustment(context, enemyUnit, destination, target, targetDies);
            return new AiCandidate(
                destination,
                AiActionType.Skill,
                target.Id,
                evaluation.Reward + GetActionBias(enemyUnit.AiProfile, true, evaluation.DefeatedUnitIds.Count > 0) + pressure + tacticalBonus - risk,
                risk,
                target.CurrentHp,
                nearestOpponentDistance,
                enemyUnit.Position.ManhattanDistance(destination));
        }

        private List<string> BuildEnemyTurnOrder(
            BattleContext context,
            IReadOnlyList<UnitRuntimeState> enemies,
            string focusTargetId,
            HashSet<string> protectedUnitIds,
            HashSet<GridPosition> chokeTiles)
        {
            List<UnitRuntimeState> orderedEnemies = enemies
                .Where(unit => unit.IsAlive && !unit.HasActed)
                .OrderByDescending(unit => CanLikelySecureKill(context, unit, focusTargetId))
                .ThenByDescending(unit => unit.AiProfile == AiProfileType.Protector && CanReachChokeTile(context, unit, chokeTiles))
                .ThenByDescending(unit => unit.AiProfile == AiProfileType.Protector && IsNearProtectedAlly(context, unit.Position, protectedUnitIds))
                .ThenBy(unit => GetPriority(unit.AiProfile))
                .ThenBy(unit => unit.Position.ManhattanDistance(GetEnemyFrontlineAnchor(context)))
                .ThenBy(unit => unit.Id)
                .ToList();

            return orderedEnemies.Select(unit => unit.Id).ToList();
        }

        private string SelectFocusTarget(BattleContext context, IReadOnlyList<UnitRuntimeState> enemies)
        {
            UnitRuntimeState bestTarget = null;
            float bestScore = float.MinValue;

            foreach (UnitRuntimeState playerUnit in context.GetUnits(UnitFaction.Player))
            {
                int threateningEnemies = enemies.Count(enemy => CanThreatenTarget(context, enemy, playerUnit));
                int nearestEnemyDistance = enemies.Count == 0
                    ? 0
                    : enemies.Min(enemy => enemy.Position.ManhattanDistance(playerUnit.Position));
                float score = (threateningEnemies * 12f) +
                              ((playerUnit.MaxHp - playerUnit.CurrentHp) * 0.7f) +
                              (Math.Max(0, 6 - nearestEnemyDistance) * 1.8f) +
                              GetRoleFocusBonus(playerUnit.Role);

                if (playerUnit.CurrentHp <= (int)Math.Ceiling(playerUnit.MaxHp * 0.5f))
                {
                    score += 6f;
                }

                if (threateningEnemies >= 2)
                {
                    score += 7f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = playerUnit;
                }
            }

            return bestTarget != null ? bestTarget.Id : string.Empty;
        }

        private static HashSet<string> SelectProtectedUnitIds(BattleContext context, IReadOnlyList<UnitRuntimeState> enemies)
        {
            HashSet<string> protectedUnits = new HashSet<string>();
            foreach (UnitRuntimeState unit in enemies)
            {
                if (!unit.IsAlive)
                {
                    continue;
                }

                if (unit.AiProfile == AiProfileType.Support || unit.AiProfile == AiProfileType.Boss || unit.CurrentHp <= (int)Math.Ceiling(unit.MaxHp * 0.55f))
                {
                    protectedUnits.Add(unit.Id);
                }
            }

            return protectedUnits;
        }

        private static HashSet<GridPosition> GetStageChokeTiles(BattleContext context)
        {
            HashSet<GridPosition> chokeTiles = new HashSet<GridPosition>();
            switch (context.StageNameKey)
            {
                case "stage.guangzong":
                    AddChokeTiles(chokeTiles, new[]
                    {
                        new GridPosition(8, 6),
                        new GridPosition(8, 7),
                        new GridPosition(9, 5),
                        new GridPosition(9, 8),
                        new GridPosition(12, 6),
                        new GridPosition(12, 7),
                    });
                    break;
                case "stage.jiangxia_ferry":
                    AddChokeTiles(chokeTiles, new[]
                    {
                        new GridPosition(11, 2),
                        new GridPosition(11, 6),
                        new GridPosition(11, 7),
                        new GridPosition(11, 11),
                        new GridPosition(8, 6),
                        new GridPosition(9, 6),
                        new GridPosition(8, 7),
                        new GridPosition(9, 7),
                    });
                    break;
                case "stage.luocheng_siege":
                    AddChokeTiles(chokeTiles, new[]
                    {
                        new GridPosition(8, 7),
                        new GridPosition(9, 7),
                        new GridPosition(8, 8),
                        new GridPosition(9, 8),
                        new GridPosition(8, 10),
                        new GridPosition(9, 10),
                        new GridPosition(8, 11),
                        new GridPosition(9, 11),
                    });
                    break;
            }

            return chokeTiles;
        }

        private float GetTacticalBonus(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            AiActionType actionType,
            UnitRuntimeState target,
            bool targetDies,
            EnemyTacticalPlan plan)
        {
            if (plan == null)
            {
                return 0f;
            }

            float bonus = 0f;
            if (plan.EnableCoordinatedFocus && target != null && target.Id == plan.FocusTargetId)
            {
                bonus += enemyUnit.AiProfile == AiProfileType.Aggressor ? FocusFireBonus + 2f : FocusFireBonus;
            }

            switch (enemyUnit.AiProfile)
            {
                case AiProfileType.Support:
                    bonus += GetSupportTacticalBonus(context, enemyUnit, destination, actionType, target, targetDies, plan);
                    break;
                case AiProfileType.Protector:
                    bonus += GetProtectorTacticalBonus(context, enemyUnit, destination, actionType, target, plan);
                    break;
                case AiProfileType.Aggressor:
                    bonus += GetAggressorTacticalBonus(context, enemyUnit, destination, target, plan);
                    break;
                case AiProfileType.Boss:
                    bonus += GetBossTacticalBonus(context, enemyUnit, destination, actionType, target, targetDies, plan);
                    break;
            }

            return bonus;
        }

        private float GetSkillValueAdjustment(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target,
            bool targetDies)
        {
            if (!enemyUnit.CanUseSkill)
            {
                return 0f;
            }

            if (enemyUnit.AiProfile != AiProfileType.Support)
            {
                return 0f;
            }

            if (ActiveSkillRules.IsOffensiveSkill(enemyUnit.ActiveSkill))
            {
                int affectedUnits = GetProjectedSkillAffectedCount(context, enemyUnit, destination, target);
                return affectedUnits >= 2 || targetDies
                    ? (affectedUnits - 1) * 3f
                    : -LowValueSkillPenalty;
            }

            if (ActiveSkillRules.IsSupportSkill(enemyUnit.ActiveSkill))
            {
                return IsSupportSkillWorthwhile(context, enemyUnit, target) ? 8f : -10f;
            }

            return 0f;
        }

        private float GetSupportTacticalBonus(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            AiActionType actionType,
            UnitRuntimeState target,
            bool targetDies,
            EnemyTacticalPlan plan)
        {
            float bonus = 0f;
            int nearestOpponentDistance = CalculateNearestOpponentDistance(context, enemyUnit.Faction, destination, null);
            if (nearestOpponentDistance <= 1)
            {
                bonus -= SupportSafetyPenalty;
            }

            if (CountNearbyProtectors(context, enemyUnit, destination) > 0)
            {
                bonus += 5f;
            }

            if (IsNearProtectedAlly(context, destination, plan.ProtectedUnitIds))
            {
                bonus += 4f;
            }

            if (actionType == AiActionType.Skill && target != null)
            {
                bonus += GetSkillValueAdjustment(context, enemyUnit, destination, target, targetDies);
            }

            return bonus;
        }

        private float GetProtectorTacticalBonus(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            AiActionType actionType,
            UnitRuntimeState target,
            EnemyTacticalPlan plan)
        {
            float bonus = 0f;
            if (plan.ChokeTiles.Contains(destination))
            {
                bonus += actionType == AiActionType.None ? ChokeHoldBonus : ChokeHoldBonus - 3f;
            }

            if (IsNearProtectedAlly(context, destination, plan.ProtectedUnitIds))
            {
                bonus += ProtectorScreenBonus;
            }

            if (plan.EnableCoordinatedFocus && target != null && target.Id == plan.FocusTargetId)
            {
                bonus += 4f;
            }

            return bonus;
        }

        private float GetAggressorTacticalBonus(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target,
            EnemyTacticalPlan plan)
        {
            if (target == null)
            {
                return 0f;
            }

            int alliedPressure = context.GetUnits(enemyUnit.Faction)
                .Count(ally => ally.Id != enemyUnit.Id && ally.IsAlive && ally.Position.ManhattanDistance(target.Position) <= 2);

            float bonus = alliedPressure * 2.5f;
            if (plan.EnableCoordinatedFocus && target.Id == plan.FocusTargetId)
            {
                bonus += FocusFireBonus + 2f;
            }

            if (destination.ManhattanDistance(target.Position) <= 1 && alliedPressure > 0)
            {
                bonus += 3f;
            }

            return bonus;
        }

        private float GetBossTacticalBonus(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            AiActionType actionType,
            UnitRuntimeState target,
            bool targetDies,
            EnemyTacticalPlan plan)
        {
            float bonus = 0f;
            if (ShouldBossHoldLine(context, enemyUnit) && BreaksBossHoldLine(context, destination))
            {
                bonus -= BossHoldLinePenalty;
            }
            else if (ShouldBossHoldLine(context, enemyUnit))
            {
                bonus += BossHoldLineBonus;
            }

            if (target != null && target.CurrentHp <= (int)Math.Ceiling(target.MaxHp * 0.5f))
            {
                bonus += 6f;
            }

            if (plan.EnableCoordinatedFocus && target != null && target.Id == plan.FocusTargetId)
            {
                bonus += 6f;
            }

            if (actionType != AiActionType.None && targetDies)
            {
                bonus += 4f;
            }

            return bonus;
        }

        private SkillEvaluation EvaluateSkill(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target)
        {
            switch (enemyUnit.ActiveSkill)
            {
                case ActiveSkillType.RoyalAid:
                case ActiveSkillType.ImperialAid:
                    return EvaluateRoyalAid(enemyUnit, target);
                case ActiveSkillType.GuardOrder:
                case ActiveSkillType.KingsBanner:
                    return EvaluateGuardOrder(context, enemyUnit, target);
                case ActiveSkillType.FeatherFormation:
                    return EvaluateFeatherFormation(context, enemyUnit, target);
                case ActiveSkillType.PowerStrike:
                    return EvaluatePowerStrike(context, enemyUnit, destination, target);
                case ActiveSkillType.DragonPierce:
                case ActiveSkillType.WhiteHorseRescue:
                    return EvaluateDragonPierce(context, enemyUnit, destination, target);
                case ActiveSkillType.PinningShot:
                    return EvaluatePinningShot(context, enemyUnit, destination, target);
                case ActiveSkillType.Volley:
                case ActiveSkillType.SkyVolley:
                case ActiveSkillType.FireStratagem:
                case ActiveSkillType.EightTrigramInferno:
                    return EvaluateVolley(context, enemyUnit, destination, target);
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.AzureDragonSlash:
                case ActiveSkillType.WesternStampede:
                case ActiveSkillType.CrimsonCrescent:
                case ActiveSkillType.StormbreakCharge:
                    return EvaluateGreenDragonSlash(context, enemyUnit, destination, target);
                case ActiveSkillType.WarCry:
                case ActiveSkillType.LionWarCry:
                case ActiveSkillType.StonewallChallenge:
                case ActiveSkillType.DustDevilSweep:
                    return EvaluateWarCry(context, enemyUnit, destination);
                default:
                    return new SkillEvaluation(0f, new HashSet<string>());
            }
        }

        private static SkillEvaluation EvaluateRoyalAid(UnitRuntimeState caster, UnitRuntimeState target)
        {
            int healedAmount = BattlePreviewCalculator.EstimateHealing(target, ActiveSkillRules.GetRoyalAidAmount(caster));
            float reward = (healedAmount * HealingWeight) + (target.HasStatus(StatusEffectType.Inspired) ? 4f : 10f);
            if (target.CurrentHp <= target.MaxHp / 3)
            {
                reward += 8f;
            }

            if (!target.HasStatus(StatusEffectType.Guarded))
            {
                reward += 4f;
            }

            return new SkillEvaluation(reward - 3f, new HashSet<string>());
        }

        private static SkillEvaluation EvaluateGuardOrder(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            UnitRuntimeState target)
        {
            IReadOnlyList<UnitRuntimeState> affectedUnits = context.GetUnits(enemyUnit.Faction)
                .Where(unit => unit.Id == target.Id || unit.Position.ManhattanDistance(target.Position) == 1)
                .OrderBy(unit => unit.Id == target.Id ? 0 : 1)
                .ThenBy(unit => unit.Id)
                .ToList();

            int healedAmount = BattlePreviewCalculator.EstimateHealing(
                target,
                enemyUnit.ActiveSkill == ActiveSkillType.KingsBanner
                    ? ActiveSkillRules.GetKingsBannerHealAmount(enemyUnit)
                    : ActiveSkillRules.GetGuardOrderHealAmount(enemyUnit));
            int freshGuardApplications = affectedUnits.Count(unit => !unit.HasStatus(StatusEffectType.Guarded));
            int freshInspiredApplications = enemyUnit.ActiveSkill == ActiveSkillType.KingsBanner
                ? affectedUnits.Count(unit => !unit.HasStatus(StatusEffectType.Inspired))
                : 0;
            float reward = (healedAmount * HealingWeight) +
                           (freshGuardApplications * (StatusBonus + 2f)) +
                           (freshInspiredApplications * StatusBonus) +
                           ((affectedUnits.Count - 1) * 2f) -
                           2f;
            if (ActiveSkillRules.IsMastered(enemyUnit) && !target.HasStatus(StatusEffectType.Inspired))
            {
                reward += StatusBonus;
            }

            if (target.CurrentHp <= target.MaxHp / 2)
            {
                reward += 5f;
            }

            if (healedAmount == 0 && freshGuardApplications == 0)
            {
                reward -= 10f;
            }

            return new SkillEvaluation(reward, new HashSet<string>());
        }

        private static SkillEvaluation EvaluateFeatherFormation(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            UnitRuntimeState target)
        {
            IReadOnlyList<UnitRuntimeState> affectedUnits = context.GetUnits(enemyUnit.Faction)
                .Where(unit => unit.Id == target.Id || unit.Position.ManhattanDistance(target.Position) == 1)
                .OrderBy(unit => unit.Id == target.Id ? 0 : 1)
                .ThenBy(unit => unit.CurrentHp)
                .ThenBy(unit => unit.Id)
                .ToList();

            UnitRuntimeState healTarget = context.GetUnits(enemyUnit.Faction)
                .OrderBy(unit => unit.CurrentHp)
                .ThenBy(unit => unit.Id)
                .FirstOrDefault();
            int healedAmount = healTarget != null
                ? BattlePreviewCalculator.EstimateHealing(healTarget, ActiveSkillRules.GetFeatherFormationHealAmount(enemyUnit))
                : 0;
            int freshGuardApplications = affectedUnits.Count(unit => !unit.HasStatus(StatusEffectType.Guarded));
            int inspiredApplications = affectedUnits.Count(unit => (unit.Id == target.Id || ActiveSkillRules.IsMastered(enemyUnit)) && !unit.HasStatus(StatusEffectType.Inspired));
            float reward = (healedAmount * HealingWeight) +
                           (freshGuardApplications * (StatusBonus + 1f)) +
                           (inspiredApplications * StatusBonus) +
                           ((affectedUnits.Count - 1) * 2f) -
                           1f;
            if (healTarget != null && healTarget.CurrentHp <= healTarget.MaxHp / 2)
            {
                reward += 5f;
            }

            return new SkillEvaluation(reward, new HashSet<string>());
        }

        private static SkillEvaluation EvaluatePowerStrike(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target)
        {
            int rawDamage = BattlePreviewCalculator.EstimateAttackDamage(
                context,
                enemyUnit,
                destination,
                target,
                ActiveSkillRules.GetPowerStrikeBonus(enemyUnit));
            bool targetDies = rawDamage >= target.CurrentHp;
            int realizedDamage = rawDamage > target.CurrentHp ? target.CurrentHp : rawDamage;
            float reward = 5f + (realizedDamage * SkillDamageWeight) + (targetDies ? KillBonus : 0f);
            if (!targetDies && !target.HasStatus(StatusEffectType.ShatteredArmor))
            {
                reward += StatusBonus;
            }

            if (!targetDies && !target.HasStatus(StatusEffectType.Bleeding))
            {
                reward += 3f;
            }

            HashSet<string> defeatedUnitIds = targetDies ? new HashSet<string> { target.Id } : new HashSet<string>();
            return new SkillEvaluation(reward - SkillCommitmentPenalty, defeatedUnitIds);
        }

        private static SkillEvaluation EvaluatePinningShot(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target)
        {
            int rawDamage = BattlePreviewCalculator.EstimateAttackDamage(
                context,
                enemyUnit,
                destination,
                target,
                ActiveSkillRules.GetPinningShotBonus(enemyUnit));
            bool targetDies = rawDamage >= target.CurrentHp;
            int realizedDamage = rawDamage > target.CurrentHp ? target.CurrentHp : rawDamage;
            float reward = 4f + (realizedDamage * SkillDamageWeight) + (targetDies ? KillBonus : 0f);
            if (!targetDies && !target.HasStatus(StatusEffectType.Rooted))
            {
                reward += StatusBonus + 2f;
            }

            HashSet<string> defeatedUnitIds = targetDies ? new HashSet<string> { target.Id } : new HashSet<string>();
            return new SkillEvaluation(reward - SkillCommitmentPenalty, defeatedUnitIds);
        }

        private static SkillEvaluation EvaluateDragonPierce(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target)
        {
            int rawDamage = BattlePreviewCalculator.EstimateAttackDamage(
                context,
                enemyUnit,
                destination,
                target,
                enemyUnit.ActiveSkill == ActiveSkillType.WhiteHorseRescue
                    ? ActiveSkillRules.GetWhiteHorseRescueBonus(enemyUnit)
                    : ActiveSkillRules.GetDragonPierceBonus(enemyUnit),
                enemyUnit.ActiveSkill == ActiveSkillType.WhiteHorseRescue
                    ? ActiveSkillRules.GetWhiteHorseRescueIgnoredDefense(enemyUnit)
                    : ActiveSkillRules.GetDragonPierceIgnoredDefense(enemyUnit));
            bool targetDies = rawDamage >= target.CurrentHp;
            int realizedDamage = rawDamage > target.CurrentHp ? target.CurrentHp : rawDamage;
            int guardValue = 1;
            if (context.GetUnits(enemyUnit.Faction)
                    .Any(unit => unit.Id != enemyUnit.Id && unit.Position.ManhattanDistance(destination) == 1 && !unit.HasStatus(StatusEffectType.Guarded)))
            {
                guardValue++;
            }

            float reward = 6f +
                           (realizedDamage * SkillDamageWeight) +
                           (targetDies ? KillBonus : 0f) +
                           (guardValue * (StatusBonus + 1f)) -
                           SkillCommitmentPenalty;
            return new SkillEvaluation(reward, targetDies ? new HashSet<string> { target.Id } : new HashSet<string>());
        }

        private static SkillEvaluation EvaluateVolley(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState primaryTarget)
        {
            IReadOnlyList<UnitRuntimeState> affectedUnits = BattlePreviewCalculator.GetVolleyTargets(context, primaryTarget);
            int totalDamage = 0;
            int defeatedCount = 0;
            int statusApplications = 0;
            HashSet<string> defeatedUnitIds = new HashSet<string>();
            int bonusDamage = GetAreaSkillBonus(enemyUnit);

            foreach (UnitRuntimeState affectedUnit in affectedUnits)
            {
                int rawDamage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    enemyUnit,
                    destination,
                    affectedUnit,
                    bonusDamage);
                int realizedDamage = rawDamage > affectedUnit.CurrentHp ? affectedUnit.CurrentHp : rawDamage;
                totalDamage += realizedDamage;

                if (rawDamage >= affectedUnit.CurrentHp)
                {
                    defeatedCount++;
                    defeatedUnitIds.Add(affectedUnit.Id);
                }
                else if (enemyUnit.ActiveSkill == ActiveSkillType.FireStratagem)
                {
                    if ((affectedUnit.Id == primaryTarget.Id || ActiveSkillRules.IsMastered(enemyUnit)) &&
                        !affectedUnit.HasStatus(StatusEffectType.Intimidated))
                    {
                        statusApplications++;
                    }
                }
                else if (enemyUnit.ActiveSkill == ActiveSkillType.EightTrigramInferno)
                {
                    if (!affectedUnit.HasStatus(StatusEffectType.Intimidated))
                    {
                        statusApplications++;
                    }

                    if (!affectedUnit.HasStatus(StatusEffectType.ShatteredArmor))
                    {
                        statusApplications++;
                    }
                }
                else if (!affectedUnit.HasStatus(StatusEffectType.ShatteredArmor))
                {
                    statusApplications++;
                }
            }

            float reward = 4f +
                           (totalDamage * DamageWeight) +
                           (defeatedCount * KillBonus) +
                           (statusApplications * StatusBonus) +
                           ((affectedUnits.Count - 1) * AdditionalTargetBonus) -
                           SkillCommitmentPenalty;
            return new SkillEvaluation(reward, defeatedUnitIds);
        }

        private static SkillEvaluation EvaluateGreenDragonSlash(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState primaryTarget)
        {
            IReadOnlyList<UnitRuntimeState> affectedUnits = BattlePreviewCalculator.GetGreenDragonSlashTargets(
                context,
                destination,
                primaryTarget);
            int totalDamage = 0;
            int defeatedCount = 0;
            HashSet<string> defeatedUnitIds = new HashSet<string>();

            foreach (UnitRuntimeState affectedUnit in affectedUnits)
            {
                int rawDamage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    enemyUnit,
                    destination,
                    affectedUnit,
                    enemyUnit.ActiveSkill == ActiveSkillType.AzureDragonSlash
                        ? ActiveSkillRules.GetAzureDragonSlashBonus(enemyUnit)
                        : enemyUnit.ActiveSkill == ActiveSkillType.CrimsonCrescent
                            ? ActiveSkillRules.GetCrimsonCrescentBonus(enemyUnit)
                        : enemyUnit.ActiveSkill == ActiveSkillType.WesternStampede
                            ? ActiveSkillRules.GetWesternStampedeBonus(enemyUnit)
                        : enemyUnit.ActiveSkill == ActiveSkillType.StormbreakCharge
                            ? ActiveSkillRules.GetStormbreakChargeBonus(enemyUnit)
                        : ActiveSkillRules.GetGreenDragonSlashBonus(enemyUnit));
                int realizedDamage = rawDamage > affectedUnit.CurrentHp ? affectedUnit.CurrentHp : rawDamage;
                totalDamage += realizedDamage;

                if (rawDamage >= affectedUnit.CurrentHp)
                {
                    defeatedCount++;
                    defeatedUnitIds.Add(affectedUnit.Id);
                }
            }

            float reward = 6f +
                           (totalDamage * SkillDamageWeight) +
                           (defeatedCount * KillBonus) +
                           ((affectedUnits.Count - 1) * AdditionalTargetBonus) +
                           ((enemyUnit.ActiveSkill == ActiveSkillType.WesternStampede || enemyUnit.ActiveSkill == ActiveSkillType.StormbreakCharge)
                               ? affectedUnits.Count(unit => unit.CurrentHp > 0 && !unit.HasStatus(StatusEffectType.Intimidated)) * StatusBonus
                               : 0f) +
                           (enemyUnit.ActiveSkill == ActiveSkillType.CrimsonCrescent
                               ? affectedUnits.Count(unit => unit.CurrentHp > 0 && !unit.HasStatus(StatusEffectType.ShatteredArmor)) * StatusBonus
                               : 0f) -
                           SkillCommitmentPenalty;
            return new SkillEvaluation(reward, defeatedUnitIds);
        }

        private static SkillEvaluation EvaluateWarCry(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination)
        {
            IReadOnlyList<UnitRuntimeState> affectedUnits = context.GetUnits(UnitFaction.Player)
                .Where(unit => destination.ManhattanDistance(unit.Position) <= ActiveSkillRules.GetRange(enemyUnit))
                .OrderBy(unit => destination.ManhattanDistance(unit.Position))
                .ThenBy(unit => unit.Id)
                .ToList();

            int freshApplications = affectedUnits.Count(unit => !unit.HasStatus(StatusEffectType.Intimidated));
            int lethalThreats = affectedUnits.Count(unit =>
                BattlePreviewCalculator.EstimateAttackDamage(context, unit, unit.Position, enemyUnit) >= enemyUnit.CurrentHp);

            float reward;
            if (enemyUnit.ActiveSkill == ActiveSkillType.StonewallChallenge || enemyUnit.ActiveSkill == ActiveSkillType.DustDevilSweep)
            {
                int totalDamage = 0;
                int defeatedCount = 0;
                foreach (UnitRuntimeState target in affectedUnits)
                {
                    int rawDamage = BattlePreviewCalculator.EstimateAttackDamage(
                        context,
                        enemyUnit,
                        destination,
                        target,
                        enemyUnit.ActiveSkill == ActiveSkillType.StonewallChallenge
                            ? ActiveSkillRules.GetStonewallChallengeBonus(enemyUnit)
                            : ActiveSkillRules.GetDustDevilSweepBonus(enemyUnit));
                    totalDamage += rawDamage > target.CurrentHp ? target.CurrentHp : rawDamage;
                    if (rawDamage >= target.CurrentHp)
                    {
                        defeatedCount++;
                    }
                }

                reward = (totalDamage * SkillDamageWeight) +
                         (defeatedCount * KillBonus) +
                         (freshApplications * StatusBonus) +
                         (affectedUnits.Count * 3f) -
                         SkillCommitmentPenalty;
                if (enemyUnit.ActiveSkill == ActiveSkillType.StonewallChallenge && !enemyUnit.HasStatus(StatusEffectType.Guarded))
                {
                    reward += StatusBonus;
                }

                if (enemyUnit.ActiveSkill == ActiveSkillType.DustDevilSweep && affectedUnits.Count >= 2 && !enemyUnit.HasStatus(StatusEffectType.Inspired))
                {
                    reward += StatusBonus;
                }
            }
            else
            {
                reward = (freshApplications * StatusBonus) + (affectedUnits.Count * 3f) + (lethalThreats * 12f) - SkillCommitmentPenalty;
            }
            if (affectedUnits.Count < 2 && lethalThreats == 0)
            {
                reward -= 12f;
            }

            return new SkillEvaluation(reward, new HashSet<string>());
        }

        private static void AddChokeTiles(HashSet<GridPosition> chokeTiles, IEnumerable<GridPosition> tiles)
        {
            foreach (GridPosition tile in tiles)
            {
                chokeTiles.Add(tile);
            }
        }

        private static float GetRoleFocusBonus(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Commander:
                    return 7f;
                case UnitRole.Ranger:
                    return 5f;
                case UnitRole.Scout:
                case UnitRole.Raider:
                    return 4f;
                default:
                    return 2f;
            }
        }

        private static EnemyTacticalPriority GetPriority(AiProfileType aiProfile)
        {
            switch (aiProfile)
            {
                case AiProfileType.Support:
                    return EnemyTacticalPriority.Support;
                case AiProfileType.Protector:
                    return EnemyTacticalPriority.Protector;
                case AiProfileType.Boss:
                    return EnemyTacticalPriority.Boss;
                case AiProfileType.Aggressor:
                default:
                    return EnemyTacticalPriority.Aggressor;
            }
        }

        private bool CanThreatenTarget(BattleContext context, UnitRuntimeState enemyUnit, UnitRuntimeState target)
        {
            if (enemyUnit == null || target == null || !enemyUnit.IsAlive || !target.IsAlive)
            {
                return false;
            }

            IReadOnlyList<GridPosition> moveRange = rangeCalculator.GetMoveRange(context, enemyUnit);
            int attackRange = PassiveSkillRules.GetAttackRange(enemyUnit);
            int skillRange = enemyUnit.CanUseSkill ? ActiveSkillRules.GetRange(enemyUnit) : 0;
            foreach (GridPosition origin in moveRange)
            {
                int distance = origin.ManhattanDistance(target.Position);
                if (distance > 0 && distance <= attackRange)
                {
                    return true;
                }

                if (enemyUnit.CanUseSkill && ActiveSkillRules.IsOffensiveSkill(enemyUnit.ActiveSkill) && distance > 0 && distance <= skillRange)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanLikelySecureKill(BattleContext context, UnitRuntimeState enemyUnit, string focusTargetId)
        {
            if (enemyUnit == null || !enemyUnit.IsAlive)
            {
                return false;
            }

            UnitRuntimeState preferredTarget = string.IsNullOrWhiteSpace(focusTargetId) ? null : context.GetUnit(focusTargetId);
            if (preferredTarget != null && EstimatePotentialDamageAgainstTarget(context, enemyUnit, preferredTarget) >= preferredTarget.CurrentHp)
            {
                return true;
            }

            return context.GetUnits(UnitFaction.Player)
                .Any(playerUnit => EstimatePotentialDamageAgainstTarget(context, enemyUnit, playerUnit) >= playerUnit.CurrentHp);
        }

        private int EstimatePotentialDamageAgainstTarget(BattleContext context, UnitRuntimeState enemyUnit, UnitRuntimeState target)
        {
            if (enemyUnit == null || target == null || !enemyUnit.IsAlive || !target.IsAlive)
            {
                return 0;
            }

            int bestDamage = 0;
            IReadOnlyList<GridPosition> moveRange = rangeCalculator.GetMoveRange(context, enemyUnit);
            int attackRange = PassiveSkillRules.GetAttackRange(enemyUnit);
            int skillRange = enemyUnit.CanUseSkill ? ActiveSkillRules.GetRange(enemyUnit) : 0;
            foreach (GridPosition origin in moveRange)
            {
                int distance = origin.ManhattanDistance(target.Position);
                if (distance > 0 && distance <= attackRange)
                {
                    bestDamage = Math.Max(bestDamage, BattlePreviewCalculator.EstimateAttackDamage(context, enemyUnit, origin, target));
                }

                if (!enemyUnit.CanUseSkill || !ActiveSkillRules.IsOffensiveSkill(enemyUnit.ActiveSkill) || distance <= 0 || distance > skillRange)
                {
                    continue;
                }

                bestDamage = Math.Max(
                    bestDamage,
                        BattlePreviewCalculator.EstimateAttackDamage(
                            context,
                            enemyUnit,
                            origin,
                            target,
                            GetOffensiveSkillBonus(enemyUnit),
                            enemyUnit.ActiveSkill == ActiveSkillType.DragonPierce
                                ? ActiveSkillRules.GetDragonPierceIgnoredDefense(enemyUnit)
                                : enemyUnit.ActiveSkill == ActiveSkillType.WhiteHorseRescue
                                    ? ActiveSkillRules.GetWhiteHorseRescueIgnoredDefense(enemyUnit)
                                : 0));
            }

            return bestDamage;
        }

        private static GridPosition GetEnemyFrontlineAnchor(BattleContext context)
        {
            IReadOnlyList<UnitRuntimeState> players = context.GetUnits(UnitFaction.Player);
            if (players.Count == 0)
            {
                return new GridPosition(0, 0);
            }

            int x = 0;
            int y = 0;
            foreach (UnitRuntimeState unit in players)
            {
                x += unit.Position.X;
                y += unit.Position.Y;
            }

            return new GridPosition(x / players.Count, y / players.Count);
        }

        private static bool CanReachChokeTile(BattleContext context, UnitRuntimeState enemyUnit, HashSet<GridPosition> chokeTiles)
        {
            if (chokeTiles == null || chokeTiles.Count == 0)
            {
                return false;
            }

            return chokeTiles.Any(tile => tile.ManhattanDistance(enemyUnit.Position) <= PassiveSkillRules.GetMoveRange(enemyUnit) + 1);
        }

        private static bool IsNearProtectedAlly(BattleContext context, GridPosition destination, HashSet<string> protectedUnitIds)
        {
            if (protectedUnitIds == null || protectedUnitIds.Count == 0)
            {
                return false;
            }

            foreach (string protectedUnitId in protectedUnitIds)
            {
                UnitRuntimeState protectedUnit = context.GetUnit(protectedUnitId);
                if (protectedUnit == null || !protectedUnit.IsAlive)
                {
                    continue;
                }

                if (destination.ManhattanDistance(protectedUnit.Position) <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountNearbyProtectors(BattleContext context, UnitRuntimeState enemyUnit, GridPosition destination)
        {
            return context.GetUnits(enemyUnit.Faction)
                .Count(ally =>
                    ally.Id != enemyUnit.Id &&
                    ally.IsAlive &&
                    (ally.AiProfile == AiProfileType.Protector || ally.AiProfile == AiProfileType.Boss) &&
                    ally.Position.ManhattanDistance(destination) <= 2);
        }

        private int GetProjectedSkillAffectedCount(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            UnitRuntimeState target)
        {
            switch (enemyUnit.ActiveSkill)
            {
                case ActiveSkillType.Volley:
                case ActiveSkillType.SkyVolley:
                case ActiveSkillType.FireStratagem:
                case ActiveSkillType.EightTrigramInferno:
                    return BattlePreviewCalculator.GetVolleyTargets(context, target).Count;
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.AzureDragonSlash:
                case ActiveSkillType.WesternStampede:
                case ActiveSkillType.CrimsonCrescent:
                case ActiveSkillType.StormbreakCharge:
                    return BattlePreviewCalculator.GetGreenDragonSlashTargets(context, destination, target).Count;
                case ActiveSkillType.WarCry:
                case ActiveSkillType.LionWarCry:
                case ActiveSkillType.StonewallChallenge:
                case ActiveSkillType.DustDevilSweep:
                    return context.GetUnits(UnitFaction.Player)
                        .Count(unit => destination.ManhattanDistance(unit.Position) <= ActiveSkillRules.GetRange(enemyUnit));
                case ActiveSkillType.ImperialAid:
                case ActiveSkillType.GuardOrder:
                case ActiveSkillType.KingsBanner:
                case ActiveSkillType.FeatherFormation:
                    return context.GetUnits(enemyUnit.Faction)
                        .Count(unit => unit.Id == target.Id || unit.Position.ManhattanDistance(target.Position) <= 1);
                default:
                    return 1;
            }
        }

        private static bool IsSupportSkillWorthwhile(BattleContext context, UnitRuntimeState enemyUnit, UnitRuntimeState target)
        {
            switch (enemyUnit.ActiveSkill)
            {
                case ActiveSkillType.RoyalAid:
                case ActiveSkillType.ImperialAid:
                    return target.CurrentHp < target.MaxHp || !target.HasStatus(StatusEffectType.Inspired);
                case ActiveSkillType.GuardOrder:
                case ActiveSkillType.KingsBanner:
                    return target.CurrentHp < target.MaxHp ||
                           !target.HasStatus(StatusEffectType.Guarded) ||
                           context.GetUnits(enemyUnit.Faction).Any(unit =>
                               unit.Position.ManhattanDistance(target.Position) == 1 &&
                               !unit.HasStatus(StatusEffectType.Guarded));
                case ActiveSkillType.FeatherFormation:
                    return context.GetUnits(enemyUnit.Faction).Any(unit => unit.CurrentHp < unit.MaxHp) ||
                           context.GetUnits(enemyUnit.Faction).Any(unit =>
                               (unit.Id == target.Id || unit.Position.ManhattanDistance(target.Position) == 1) &&
                               !unit.HasStatus(StatusEffectType.Guarded));
                default:
                    return true;
            }
        }

        private static bool ShouldBossHoldLine(BattleContext context, UnitRuntimeState enemyUnit)
        {
            if (enemyUnit == null || enemyUnit.AiProfile != AiProfileType.Boss)
            {
                return false;
            }

            switch (context.StageNameKey)
            {
                case "stage.guangzong":
                    return context.GetUnit("enemy-zhang-liang") == null &&
                           (context.RoundNumber < 3 ||
                            context.GetUnit("enemy-yellow_turban_raider") != null ||
                            context.GetUnit("enemy-armored_zealot") != null);
                default:
                    return false;
            }
        }

        private static bool BreaksBossHoldLine(BattleContext context, GridPosition destination)
        {
            switch (context.StageNameKey)
            {
                case "stage.guangzong":
                    return destination.X < 12;
                default:
                    return false;
            }
        }

        private float EstimateExposure(
            BattleContext context,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            ISet<string> defeatedUnitIds)
        {
            UnitFaction opposingFaction = enemyUnit.Faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player;
            float totalThreatDamage = 0f;
            int threateningUnits = 0;

            foreach (UnitRuntimeState opposingUnit in context.GetUnits(opposingFaction))
            {
                if (defeatedUnitIds != null && defeatedUnitIds.Contains(opposingUnit.Id))
                {
                    continue;
                }

                int threatDamage = EstimateThreatDamage(context, opposingUnit, enemyUnit, destination, defeatedUnitIds);
                if (threatDamage <= 0)
                {
                    continue;
                }

                threateningUnits++;
                totalThreatDamage += threatDamage;
            }

            float risk = (totalThreatDamage * ThreatDamageWeight) + (threateningUnits * ThreatUnitPenalty);
            if (totalThreatDamage >= enemyUnit.CurrentHp)
            {
                risk += LethalThreatPenalty;
            }

            return risk;
        }

        private int EstimateThreatDamage(
            BattleContext context,
            UnitRuntimeState opposingUnit,
            UnitRuntimeState enemyUnit,
            GridPosition destination,
            ISet<string> defeatedUnitIds)
        {
            int bestDamage = 0;
            IReadOnlyList<GridPosition> moveOrigins = GetProjectedMoveRange(context, opposingUnit, enemyUnit.Position, destination, defeatedUnitIds);
            foreach (GridPosition origin in moveOrigins)
            {
                int distance = origin.ManhattanDistance(destination);
                int attackRange = PassiveSkillRules.GetAttackRange(opposingUnit);
                if (distance > 0 && distance <= attackRange)
                {
                    int attackDamage = BattlePreviewCalculator.EstimateAttackDamage(context, opposingUnit, origin, enemyUnit);
                    if (attackDamage > bestDamage)
                    {
                        bestDamage = attackDamage;
                    }
                }

                if (!opposingUnit.CanUseSkill || !ActiveSkillRules.IsOffensiveSkill(opposingUnit.ActiveSkill))
                {
                    continue;
                }

                int skillRange = ActiveSkillRules.GetRange(opposingUnit);
                if (distance <= 0 || distance > skillRange)
                {
                    continue;
                }

                int bonusDamage = GetOffensiveSkillBonus(opposingUnit);

                int skillDamage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    opposingUnit,
                    origin,
                    enemyUnit,
                    bonusDamage,
                    opposingUnit.ActiveSkill == ActiveSkillType.DragonPierce
                        ? ActiveSkillRules.GetDragonPierceIgnoredDefense(opposingUnit)
                        : 0);
                if (skillDamage > bestDamage)
                {
                    bestDamage = skillDamage;
                }
            }

            return bestDamage;
        }

        private static int GetAreaSkillBonus(UnitRuntimeState unit)
        {
            switch (unit.ActiveSkill)
            {
                case ActiveSkillType.SkyVolley:
                    return ActiveSkillRules.GetSkyVolleyBonus(unit);
                case ActiveSkillType.FireStratagem:
                    return ActiveSkillRules.GetFireStratagemBonus(unit);
                case ActiveSkillType.EightTrigramInferno:
                    return ActiveSkillRules.GetEightTrigramInfernoBonus(unit);
                default:
                    return ActiveSkillRules.GetVolleyBonus(unit);
            }
        }

        private static int GetOffensiveSkillBonus(UnitRuntimeState unit)
        {
            switch (unit.ActiveSkill)
            {
                case ActiveSkillType.PowerStrike:
                    return ActiveSkillRules.GetPowerStrikeBonus(unit);
                case ActiveSkillType.DragonPierce:
                    return ActiveSkillRules.GetDragonPierceBonus(unit);
                case ActiveSkillType.WhiteHorseRescue:
                    return ActiveSkillRules.GetWhiteHorseRescueBonus(unit);
                case ActiveSkillType.PinningShot:
                    return ActiveSkillRules.GetPinningShotBonus(unit);
                case ActiveSkillType.CrimsonCrescent:
                    return ActiveSkillRules.GetCrimsonCrescentBonus(unit);
                case ActiveSkillType.GreenDragonSlash:
                    return ActiveSkillRules.GetGreenDragonSlashBonus(unit);
                case ActiveSkillType.AzureDragonSlash:
                    return ActiveSkillRules.GetAzureDragonSlashBonus(unit);
                case ActiveSkillType.WesternStampede:
                    return ActiveSkillRules.GetWesternStampedeBonus(unit);
                case ActiveSkillType.StonewallChallenge:
                    return ActiveSkillRules.GetStonewallChallengeBonus(unit);
                case ActiveSkillType.StormbreakCharge:
                    return ActiveSkillRules.GetStormbreakChargeBonus(unit);
                case ActiveSkillType.DustDevilSweep:
                    return ActiveSkillRules.GetDustDevilSweepBonus(unit);
                case ActiveSkillType.FireStratagem:
                    return ActiveSkillRules.GetFireStratagemBonus(unit);
                case ActiveSkillType.EightTrigramInferno:
                    return ActiveSkillRules.GetEightTrigramInfernoBonus(unit);
                case ActiveSkillType.SkyVolley:
                    return ActiveSkillRules.GetSkyVolleyBonus(unit);
                case ActiveSkillType.Volley:
                    return ActiveSkillRules.GetVolleyBonus(unit);
                default:
                    return 0;
            }
        }

        private static float CalculatePressureScore(int nearestOpponentDistance)
        {
            return nearestOpponentDistance < 0
                ? PressureBaseScore
                : PressureBaseScore - (nearestOpponentDistance * PressureDistancePenalty);
        }

        private static float GetRiskMultiplier(AiProfileType aiProfile)
        {
            switch (aiProfile)
            {
                case AiProfileType.Aggressor:
                    return 0.9f;
                case AiProfileType.Protector:
                    return 1.1f;
                case AiProfileType.Support:
                    return 1.2f;
                case AiProfileType.Boss:
                    return 0.82f;
                default:
                    return 1f;
            }
        }

        private static float GetActionBias(AiProfileType aiProfile, bool skillAction, bool lethal)
        {
            switch (aiProfile)
            {
                case AiProfileType.Aggressor:
                    return skillAction ? 4f : lethal ? 6f : 2f;
                case AiProfileType.Protector:
                    return skillAction ? 2f : lethal ? 1f : 0f;
                case AiProfileType.Support:
                    return skillAction ? 6f : -1f;
                case AiProfileType.Boss:
                    return skillAction ? 5f : 4f;
                default:
                    return 0f;
            }
        }

        private static float GetProfilePressureBonus(BattleContext context, UnitRuntimeState enemyUnit, GridPosition destination)
        {
            switch (enemyUnit.AiProfile)
            {
                case AiProfileType.Aggressor:
                    return 4f;
                case AiProfileType.Protector:
                    return context.GetUnits(enemyUnit.Faction)
                        .Count(ally => ally.Id != enemyUnit.Id && ally.Position.ManhattanDistance(destination) <= 2) * 1.5f;
                case AiProfileType.Support:
                    return context.GetUnits(enemyUnit.Faction)
                        .Count(ally => ally.Id != enemyUnit.Id && ally.CurrentHp < ally.MaxHp) * 2f;
                case AiProfileType.Boss:
                    return 6f;
                default:
                    return 0f;
            }
        }

        private static int CalculateNearestOpponentDistance(
            BattleContext context,
            UnitFaction faction,
            GridPosition destination,
            ISet<string> defeatedUnitIds)
        {
            UnitFaction opposingFaction = faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player;
            int nearestDistance = -1;
            foreach (UnitRuntimeState opposingUnit in context.GetUnits(opposingFaction))
            {
                if (defeatedUnitIds != null && defeatedUnitIds.Contains(opposingUnit.Id))
                {
                    continue;
                }

                int distance = destination.ManhattanDistance(opposingUnit.Position);
                if (nearestDistance < 0 || distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }

            return nearestDistance;
        }

        private static IReadOnlyList<GridPosition> GetProjectedMoveRange(
            BattleContext context,
            UnitRuntimeState unit,
            GridPosition releasedPosition,
            GridPosition occupiedPosition,
            ISet<string> defeatedUnitIds)
        {
            int moveRange = PassiveSkillRules.GetMoveRange(unit) + EquipmentEffectRules.GetMoveBonus(context, unit);
            Dictionary<GridPosition, int> distances = new Dictionary<GridPosition, int>();
            Queue<GridPosition> frontier = new Queue<GridPosition>();
            frontier.Enqueue(unit.Position);
            distances[unit.Position] = 0;

            while (frontier.Count > 0)
            {
                GridPosition current = frontier.Dequeue();
                int nextDistance = distances[current] + 1;
                if (nextDistance > moveRange)
                {
                    continue;
                }

                foreach (GridPosition neighbor in current.GetOrthogonalNeighbors())
                {
                    if (!context.IsInside(neighbor) || distances.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    if (context.GetCell(neighbor).IsBlocked)
                    {
                        continue;
                    }

                    if (IsProjectedOccupied(context, unit, neighbor, releasedPosition, occupiedPosition, defeatedUnitIds))
                    {
                        continue;
                    }

                    distances[neighbor] = nextDistance;
                    frontier.Enqueue(neighbor);
                }
            }

            return distances.Keys
                .OrderBy(position => position.Y)
                .ThenBy(position => position.X)
                .ToList();
        }

        private static bool IsProjectedOccupied(
            BattleContext context,
            UnitRuntimeState movingUnit,
            GridPosition position,
            GridPosition releasedPosition,
            GridPosition occupiedPosition,
            ISet<string> defeatedUnitIds)
        {
            if (position == movingUnit.Position)
            {
                return false;
            }

            if (position == occupiedPosition)
            {
                return true;
            }

            if (releasedPosition != occupiedPosition && position == releasedPosition)
            {
                return false;
            }

            UnitRuntimeState occupant = context.GetUnitAt(position);
            if (occupant == null)
            {
                return false;
            }

            if (defeatedUnitIds != null && defeatedUnitIds.Contains(occupant.Id))
            {
                return false;
            }

            return occupant.Id != movingUnit.Id;
        }

        private sealed class SkillEvaluation
        {
            public SkillEvaluation(float reward, ISet<string> defeatedUnitIds)
            {
                Reward = reward;
                DefeatedUnitIds = defeatedUnitIds ?? new HashSet<string>();
            }

            public float Reward { get; }

            public ISet<string> DefeatedUnitIds { get; }
        }

        private sealed class AiCandidate
        {
            public AiCandidate(
                GridPosition destination,
                AiActionType actionType,
                string targetUnitId,
                float score,
                float risk,
                int targetHp,
                int nearestOpponentDistance,
                int moveDistance)
            {
                Destination = destination;
                ActionType = actionType;
                TargetUnitId = targetUnitId;
                Score = score;
                Risk = risk;
                TargetHp = targetHp;
                NearestOpponentDistance = nearestOpponentDistance;
                MoveDistance = moveDistance;
            }

            public GridPosition Destination { get; }

            public AiActionType ActionType { get; }

            public string TargetUnitId { get; }

            public float Score { get; }

            public float Risk { get; }

            public int TargetHp { get; }

            public int NearestOpponentDistance { get; }

            public int MoveDistance { get; }

            public int ActionPriority
            {
                get
                {
                    switch (ActionType)
                    {
                        case AiActionType.Attack:
                            return 2;
                        case AiActionType.Skill:
                            return 1;
                        default:
                            return 0;
                    }
                }
            }
        }
    }
}
