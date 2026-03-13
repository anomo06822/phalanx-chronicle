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

            return combatResult.Damage <= 2
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
            if (actingSide != TurnSide.Enemy)
            {
                return true;
            }

            return GetImpactTier(skillResult) != BattlePresentationImpactTier.LowSignal;
        }

        public bool ShouldShowCombatForecast(TurnSide actingSide, CombatResult combatResult)
        {
            if (actingSide != TurnSide.Enemy)
            {
                return true;
            }

            return GetImpactTier(combatResult) != BattlePresentationImpactTier.LowSignal;
        }

        public bool ShouldTakeOverRibbon(TurnSide actingSide, CombatResult combatResult)
        {
            return ShouldShowCombatForecast(actingSide, combatResult);
        }

        public bool ShouldShowSkillForecast(TurnSide actingSide, SkillResult skillResult)
        {
            if (actingSide != TurnSide.Enemy)
            {
                return true;
            }

            return GetImpactTier(skillResult) != BattlePresentationImpactTier.LowSignal;
        }

        public bool ShouldTakeOverRibbon(TurnSide actingSide, SkillResult skillResult)
        {
            return ShouldShowSkillForecast(actingSide, skillResult);
        }

        public bool ShouldBatchSkillResult(TurnSide actingSide, SkillResult skillResult)
        {
            if (actingSide != TurnSide.Enemy)
            {
                return false;
            }

            if (skillResult == null || skillResult.Effects == null || skillResult.Effects.Count <= 1)
            {
                return false;
            }

            return GetImpactTier(skillResult) == BattlePresentationImpactTier.LowSignal;
        }

        public float GetInterEffectDelay(TurnSide actingSide, SkillResult skillResult)
        {
            BattlePresentationProfile profile = GetProfile(actingSide);
            if (ShouldBatchSkillResult(actingSide, skillResult) ||
                (actingSide == TurnSide.Enemy && skillResult != null && skillResult.Effects.Count >= 3) ||
                GetImpactTier(skillResult) == BattlePresentationImpactTier.LowSignal)
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
                    return profile.PostCombatHold + 0.02f;
                case BattlePresentationImpactTier.LowSignal:
                    return actingSide == TurnSide.Enemy ? 0.02f : profile.PostCombatHold * 0.7f;
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
                    return profile.PostSkillHold + 0.02f;
                case BattlePresentationImpactTier.LowSignal:
                    return actingSide == TurnSide.Enemy ? 0.02f : profile.PostSkillHold * 0.7f;
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
                    return profile.PostActionHold + 0.02f;
                case BattlePresentationImpactTier.LowSignal:
                    return 0.1f;
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
    }
}
