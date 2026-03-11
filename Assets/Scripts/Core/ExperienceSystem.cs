using System;

namespace PhalanxChronicle.Core
{
    public static class ExperienceSystem
    {
        private static readonly ExpRewardRule DamageRule = new ExpRewardRule(5, 25, 20f);
        private static readonly ExpRewardRule HealingRule = new ExpRewardRule(5, 25, 18f);
        private static readonly ExpRewardRule StatusRule = new ExpRewardRule(6, 18, 0f);

        public const int MaxLevel = 20;

        public static int GetRequiredExpForLevel(int level)
        {
            return 100 + (Math.Max(1, level) - 1) * 20;
        }

        public static int CalculateDamageReward(UnitRuntimeState actor, UnitRuntimeState target, int actualDamage, int repeatCount)
        {
            if (actor == null || target == null || actualDamage <= 0 || target.MaxHp <= 0)
            {
                return 0;
            }

            float reward = DamageRule.Minimum + (DamageRule.Scale * actualDamage / target.MaxHp);
            reward = ApplyLevelScaling(actor, target, reward);
            reward = ApplyBossBonus(target, reward);
            reward = ApplyRepeatPenalty(reward, repeatCount);
            return ClampReward(reward, DamageRule);
        }

        public static int CalculateHealingReward(UnitRuntimeState actor, UnitRuntimeState target, int actualHealing, int repeatCount)
        {
            if (actor == null || target == null || actualHealing <= 0 || target.MaxHp <= 0)
            {
                return 0;
            }

            float reward = HealingRule.Minimum + (HealingRule.Scale * actualHealing / target.MaxHp);
            reward = ApplyRepeatPenalty(reward, repeatCount);
            return ClampReward(reward, HealingRule);
        }

        public static int CalculateStatusReward(UnitRuntimeState actor, UnitRuntimeState target, StatusEffectType status, int repeatCount)
        {
            if (actor == null || target == null || status == StatusEffectType.None)
            {
                return 0;
            }

            float reward = StatusRule.Minimum;
            if (status == StatusEffectType.ShatteredArmor || status == StatusEffectType.Rooted || status == StatusEffectType.Taunted)
            {
                reward += 4f;
            }

            reward = ApplyLevelScaling(actor, target, reward);
            reward = ApplyRepeatPenalty(reward, repeatCount);
            return ClampReward(reward, StatusRule);
        }

        public static int CalculateKillBonus(UnitRuntimeState actor, UnitRuntimeState target)
        {
            if (actor == null || target == null)
            {
                return 0;
            }

            float reward = 15f;
            int levelDelta = target.Level - actor.Level;
            if (levelDelta > 0)
            {
                reward += Math.Min(10f, levelDelta * 2f);
            }

            reward = ApplyBossBonus(target, reward);
            return (int)Math.Round(reward);
        }

        public static int GetObjectiveReward(BattleScenarioData scenario, bool victory)
        {
            if (scenario == null)
            {
                return victory ? 45 : 20;
            }

            return victory ? scenario.VictoryExpReward : scenario.DefeatExpReward;
        }

        public static int GetParticipationReward(UnitRuntimeState unit)
        {
            return unit != null && unit.BattleExpEarned <= 0 ? 10 : 0;
        }

        private static int ClampReward(float reward, ExpRewardRule rule)
        {
            int rounded = (int)Math.Round(reward);
            if (rounded < rule.Minimum)
            {
                return rule.Minimum;
            }

            return rounded > rule.Maximum ? rule.Maximum : rounded;
        }

        private static float ApplyRepeatPenalty(float reward, int repeatCount)
        {
            switch (repeatCount)
            {
                case 0:
                    return reward;
                case 1:
                    return reward * 0.75f;
                case 2:
                    return reward * 0.5f;
                default:
                    return reward * 0.35f;
            }
        }

        private static float ApplyLevelScaling(UnitRuntimeState actor, UnitRuntimeState target, float reward)
        {
            int levelDelta = target.Level - actor.Level;
            if (levelDelta > 0)
            {
                reward *= 1f + Math.Min(0.6f, levelDelta * 0.12f);
            }
            else if (levelDelta < -2)
            {
                reward *= Math.Max(0.4f, 1f + ((levelDelta + 2) * 0.15f));
            }

            return reward;
        }

        private static float ApplyBossBonus(UnitRuntimeState target, float reward)
        {
            return target != null && target.AiProfile == AiProfileType.Boss
                ? reward + 5f
                : reward;
        }
    }
}
