using System.Linq;
using PhalanxChronicle.Core;

namespace PhalanxChronicle.Battle.Effects
{
    public enum BattlePresentationImpactTier
    {
        LowSignal,
        Standard,
        Critical,
    }

    public sealed class BattleActionSequencer
    {
        private const int LowSignalDamageThreshold = 2;
        private const int EnemyBatchEffectThreshold = 3;
        private const float CriticalHoldBonus = 0.02f;
        private const float EnemyLowSignalResolveHold = 0.02f;
        private const float EnemyLowSignalActionHold = 0.1f;
        private const float PlayerLowSignalHoldScale = 0.7f;

        public BattlePresentationProfile GetProfile(TurnSide actingSide)
        {
            return actingSide == TurnSide.Enemy
                ? BattlePresentationProfile.EnemyFastResolve
                : BattlePresentationProfile.PlayerReadable;
        }

        public BattlePresentationImpactTier GetImpactTier(CombatResult combatResult)
        {
            if (combatResult == null)
            {
                return BattlePresentationImpactTier.LowSignal;
            }

            if (combatResult.DefenderDied || combatResult.AttackerLevelsGained > 0)
            {
                return BattlePresentationImpactTier.Critical;
            }

            return combatResult.Damage <= LowSignalDamageThreshold
                ? BattlePresentationImpactTier.LowSignal
                : BattlePresentationImpactTier.Standard;
        }

        public BattlePresentationImpactTier GetImpactTier(SkillResult skillResult)
        {
            if (skillResult == null || skillResult.Effects == null || skillResult.Effects.Count == 0)
            {
                return BattlePresentationImpactTier.LowSignal;
            }

            if (skillResult.CasterLevelsGained > 0 ||
                skillResult.Effects.Any(effect => effect.UnitDied || effect.AppliedStatuses.Any(status => status.WasApplied)))
            {
                return BattlePresentationImpactTier.Critical;
            }

            return skillResult.Effects.Any(effect => effect.Amount > 0)
                ? BattlePresentationImpactTier.Standard
                : BattlePresentationImpactTier.LowSignal;
        }

        public bool ShouldShowSkillBark(TurnSide actingSide, SkillResult skillResult)
        {
            return !IsEnemyLowSignal(actingSide, GetImpactTier(skillResult));
        }

        public bool ShouldShowCombatForecast(TurnSide actingSide, CombatResult combatResult)
        {
            return !IsEnemyLowSignal(actingSide, GetImpactTier(combatResult));
        }

        public bool ShouldTakeOverRibbon(TurnSide actingSide, CombatResult combatResult)
        {
            return ShouldShowCombatForecast(actingSide, combatResult);
        }

        public bool ShouldShowSkillForecast(TurnSide actingSide, SkillResult skillResult)
        {
            return !IsEnemyLowSignal(actingSide, GetImpactTier(skillResult));
        }

        public bool ShouldTakeOverRibbon(TurnSide actingSide, SkillResult skillResult)
        {
            return ShouldShowSkillForecast(actingSide, skillResult);
        }

        public bool ShouldBatchSkillResult(TurnSide actingSide, SkillResult skillResult)
        {
            if (skillResult == null || skillResult.Effects == null || skillResult.Effects.Count <= 1)
            {
                return false;
            }

            return IsEnemyLowSignal(actingSide, GetImpactTier(skillResult));
        }

        public float GetInterEffectDelay(TurnSide actingSide, SkillResult skillResult)
        {
            BattlePresentationProfile profile = GetProfile(actingSide);
            if (ShouldBatchSkillResult(actingSide, skillResult) ||
                ShouldUseEnemyBatchPolicy(actingSide, skillResult) ||
                IsEnemyLowSignal(actingSide, GetImpactTier(skillResult)))
            {
                return 0f;
            }

            return profile.InterEffectDelay;
        }

        public float GetPostCombatHold(TurnSide actingSide, CombatResult combatResult)
        {
            BattlePresentationProfile profile = GetProfile(actingSide);
            switch (GetImpactTier(combatResult))
            {
                case BattlePresentationImpactTier.Critical:
                    return profile.PostCombatHold + CriticalHoldBonus;
                case BattlePresentationImpactTier.LowSignal:
                    return actingSide == TurnSide.Enemy ? EnemyLowSignalResolveHold : profile.PostCombatHold * PlayerLowSignalHoldScale;
                default:
                    return profile.PostCombatHold;
            }
        }

        public float GetPostSkillHold(TurnSide actingSide, SkillResult skillResult)
        {
            BattlePresentationProfile profile = GetProfile(actingSide);
            switch (GetImpactTier(skillResult))
            {
                case BattlePresentationImpactTier.Critical:
                    return profile.PostSkillHold + CriticalHoldBonus;
                case BattlePresentationImpactTier.LowSignal:
                    return actingSide == TurnSide.Enemy ? EnemyLowSignalResolveHold : profile.PostSkillHold * PlayerLowSignalHoldScale;
                default:
                    return profile.PostSkillHold;
            }
        }

        public float GetPostActionHold(UnitActionResult actionResult)
        {
            BattlePresentationProfile profile = BattlePresentationProfile.EnemyFastResolve;
            if (actionResult == null)
            {
                return profile.PostActionHold;
            }

            BattlePresentationImpactTier tier = actionResult.PerformedAttack
                ? GetImpactTier(actionResult.CombatResult)
                : actionResult.PerformedSkill
                    ? GetImpactTier(actionResult.SkillResult)
                    : BattlePresentationImpactTier.LowSignal;

            switch (tier)
            {
                case BattlePresentationImpactTier.Critical:
                    return profile.PostActionHold + CriticalHoldBonus;
                case BattlePresentationImpactTier.LowSignal:
                    return EnemyLowSignalActionHold;
                default:
                    return profile.PostActionHold;
            }
        }

        public BattlePresentationProfile GetDuelProfile(TurnSide actingSide)
        {
            return actingSide == TurnSide.Enemy
                ? BattlePresentationProfile.EnemyFastResolve
                : BattlePresentationProfile.PlayerReadable;
        }

        public float GetPostDuelHold(TurnSide actingSide, DuelResult duelResult)
        {
            BattlePresentationProfile profile = GetDuelProfile(actingSide);
            if (duelResult == null)
            {
                return profile.PostCombatHold;
            }

            return duelResult.DefenderDefeated
                ? profile.PostCombatHold + 0.08f
                : profile.PostCombatHold + 0.02f;
        }

        private static bool IsEnemyLowSignal(TurnSide actingSide, BattlePresentationImpactTier impactTier)
        {
            return actingSide == TurnSide.Enemy && impactTier == BattlePresentationImpactTier.LowSignal;
        }

        private static bool ShouldUseEnemyBatchPolicy(TurnSide actingSide, SkillResult skillResult)
        {
            return actingSide == TurnSide.Enemy &&
                   skillResult != null &&
                   skillResult.Effects != null &&
                   skillResult.Effects.Count >= EnemyBatchEffectThreshold;
        }
    }
}
