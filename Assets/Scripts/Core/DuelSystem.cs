using System;
using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public static class DuelSystem
    {
        public static DuelResult Resolve(BattleContext context, DuelSceneDefinition duelScene)
        {
            if (context == null || duelScene == null)
            {
                return null;
            }

            UnitRuntimeState attacker = context.GetUnit(duelScene.AttackerUnitId);
            UnitRuntimeState defender = context.GetUnit(duelScene.DefenderUnitId);
            if (attacker == null || defender == null || !attacker.IsAlive || !defender.IsAlive || attacker.HasActed)
            {
                return null;
            }

            DuelOutcomeDefinition outcome = duelScene.Outcome ?? new DuelOutcomeDefinition();
            int damage = outcome.DefeatTarget
                ? defender.CurrentHp
                : Math.Min(defender.CurrentHp, outcome.FlatDamageToTarget);
            defender.ApplyDamage(damage);
            bool defenderDied = !defender.IsAlive;
            if (defenderDied)
            {
                context.RemoveUnit(defender.Id);
            }

            IReadOnlyList<SkillStatusApplication> casterStatuses = ApplyStatuses(attacker, outcome.ApplyStatusesToCaster);
            IReadOnlyList<SkillStatusApplication> targetStatuses = defenderDied
                ? Array.Empty<SkillStatusApplication>()
                : ApplyStatuses(defender, outcome.ApplyStatusesToTarget);
            List<SkillEffectResult> nearbyEffects = ApplyNearbyEnemyStatuses(context, defender, outcome);

            int experience = ExperienceSystem.CalculateDamageReward(
                attacker,
                defender,
                damage,
                attacker.RegisterContribution("duel:" + defender.Id));
            if (defenderDied)
            {
                experience += ExperienceSystem.CalculateKillBonus(attacker, defender);
            }

            foreach (SkillStatusApplication status in targetStatuses.Where(status => status != null && status.WasApplied))
            {
                experience += ExperienceSystem.CalculateStatusReward(
                    attacker,
                    defender,
                    status.Type,
                    attacker.RegisterContribution("duel-status:" + defender.Id + ":" + status.Type));
            }

            foreach (SkillEffectResult nearbyEffect in nearbyEffects)
            {
                UnitRuntimeState nearbyTarget = context.GetUnit(nearbyEffect.UnitId);
                if (nearbyTarget == null)
                {
                    continue;
                }

                foreach (SkillStatusApplication status in nearbyEffect.AppliedStatuses.Where(status => status.WasApplied))
                {
                    experience += ExperienceSystem.CalculateStatusReward(
                        attacker,
                        nearbyTarget,
                        status.Type,
                        attacker.RegisterContribution("duel-status:" + nearbyTarget.Id + ":" + status.Type));
                }
            }

            int levelsGained = attacker.AddExperience(experience);
            attacker.MarkActed();
            context.EvaluateBattleOutcome();

            SkillEffectResult defenderEffect = new SkillEffectResult(
                defender.Id,
                damage,
                defender.CurrentHp,
                defenderDied,
                false,
                targetStatuses);

            return new DuelResult(
                duelScene.DuelId,
                duelScene.TitleKey,
                duelScene.TitleFallback,
                duelScene.AnimationProfileId,
                attacker.Id,
                defender.Id,
                defenderEffect,
                casterStatuses,
                nearbyEffects,
                outcome.SetFlags,
                experience,
                levelsGained);
        }

        private static List<SkillEffectResult> ApplyNearbyEnemyStatuses(BattleContext context, UnitRuntimeState primaryTarget, DuelOutcomeDefinition outcome)
        {
            List<SkillEffectResult> results = new List<SkillEffectResult>();
            if (context == null ||
                primaryTarget == null ||
                !primaryTarget.IsAlive ||
                outcome == null ||
                outcome.ApplyStatusesToNearbyEnemies == null ||
                outcome.ApplyStatusesToNearbyEnemies.Count == 0)
            {
                return results;
            }

            IReadOnlyList<UnitRuntimeState> nearbyEnemies = context.GetUnits(primaryTarget.Faction)
                .Where(unit => unit.IsAlive &&
                               unit.Id != primaryTarget.Id &&
                               unit.Position.ManhattanDistance(primaryTarget.Position) <= outcome.NearbyEnemyRadius)
                .ToList();
            foreach (UnitRuntimeState enemy in nearbyEnemies)
            {
                IReadOnlyList<SkillStatusApplication> statuses = ApplyStatuses(enemy, outcome.ApplyStatusesToNearbyEnemies);
                if (statuses.Count == 0)
                {
                    continue;
                }

                results.Add(new SkillEffectResult(enemy.Id, 0, enemy.CurrentHp, false, false, statuses));
            }

            return results;
        }

        private static IReadOnlyList<SkillStatusApplication> ApplyStatuses(UnitRuntimeState target, IReadOnlyList<StatusEffectDurationDefinition> definitions)
        {
            List<SkillStatusApplication> applications = new List<SkillStatusApplication>();
            if (target == null || definitions == null)
            {
                return applications;
            }

            foreach (StatusEffectDurationDefinition definition in definitions.Where(definition => definition != null && definition.Type != StatusEffectType.None && definition.Duration > 0))
            {
                bool applied = target.AddOrRefreshStatus(definition.Type, definition.Duration);
                applications.Add(new SkillStatusApplication(definition.Type, definition.Duration, applied));
            }

            return applications;
        }
    }
}
