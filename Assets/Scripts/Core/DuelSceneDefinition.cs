using System;
using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class StatusEffectDurationDefinition
    {
        public StatusEffectDurationDefinition(StatusEffectType type, int duration)
        {
            Type = type;
            Duration = duration < 0 ? 0 : duration;
        }

        public StatusEffectType Type { get; }

        public int Duration { get; }
    }

    public sealed class DuelOutcomeDefinition
    {
        public DuelOutcomeDefinition(
            bool defeatTarget = false,
            int flatDamageToTarget = 0,
            IReadOnlyList<StatusEffectDurationDefinition> applyStatusesToCaster = null,
            IReadOnlyList<StatusEffectDurationDefinition> applyStatusesToTarget = null,
            IReadOnlyList<StatusEffectDurationDefinition> applyStatusesToNearbyEnemies = null,
            IReadOnlyList<string> setFlags = null,
            int nearbyEnemyRadius = 2)
        {
            DefeatTarget = defeatTarget;
            FlatDamageToTarget = flatDamageToTarget < 0 ? 0 : flatDamageToTarget;
            ApplyStatusesToCaster = applyStatusesToCaster ?? Array.Empty<StatusEffectDurationDefinition>();
            ApplyStatusesToTarget = applyStatusesToTarget ?? Array.Empty<StatusEffectDurationDefinition>();
            ApplyStatusesToNearbyEnemies = applyStatusesToNearbyEnemies ?? Array.Empty<StatusEffectDurationDefinition>();
            SetFlags = setFlags ?? Array.Empty<string>();
            NearbyEnemyRadius = nearbyEnemyRadius < 1 ? 1 : nearbyEnemyRadius;
        }

        public bool DefeatTarget { get; }

        public int FlatDamageToTarget { get; }

        public IReadOnlyList<StatusEffectDurationDefinition> ApplyStatusesToCaster { get; }

        public IReadOnlyList<StatusEffectDurationDefinition> ApplyStatusesToTarget { get; }

        public IReadOnlyList<StatusEffectDurationDefinition> ApplyStatusesToNearbyEnemies { get; }

        public IReadOnlyList<string> SetFlags { get; }

        public int NearbyEnemyRadius { get; }
    }

    public sealed class DuelSceneDefinition
    {
        public DuelSceneDefinition(
            string duelId,
            string attackerUnitId,
            string defenderUnitId,
            string titleKey,
            string titleFallback,
            IReadOnlyList<string> requiredFlags = null,
            IReadOnlyList<string> excludedFlags = null,
            int? minimumRoundNumber = null,
            int targetHpPercentAtMost = 100,
            bool playerInitiatedOnly = true,
            string animationProfileId = "",
            DuelOutcomeDefinition outcome = null)
        {
            DuelId = duelId ?? string.Empty;
            AttackerUnitId = attackerUnitId ?? string.Empty;
            DefenderUnitId = defenderUnitId ?? string.Empty;
            TitleKey = titleKey ?? string.Empty;
            TitleFallback = titleFallback ?? string.Empty;
            RequiredFlags = requiredFlags ?? Array.Empty<string>();
            ExcludedFlags = excludedFlags ?? Array.Empty<string>();
            MinimumRoundNumber = minimumRoundNumber;
            TargetHpPercentAtMost = targetHpPercentAtMost <= 0 ? 100 : Math.Min(100, targetHpPercentAtMost);
            PlayerInitiatedOnly = playerInitiatedOnly;
            AnimationProfileId = animationProfileId ?? string.Empty;
            Outcome = outcome ?? new DuelOutcomeDefinition();
        }

        public string DuelId { get; }

        public string AttackerUnitId { get; }

        public string DefenderUnitId { get; }

        public string TitleKey { get; }

        public string TitleFallback { get; }

        public IReadOnlyList<string> RequiredFlags { get; }

        public IReadOnlyList<string> ExcludedFlags { get; }

        public int? MinimumRoundNumber { get; }

        public int TargetHpPercentAtMost { get; }

        public bool PlayerInitiatedOnly { get; }

        public string AnimationProfileId { get; }

        public DuelOutcomeDefinition Outcome { get; }

        public bool Matches(string attackerUnitId, string defenderUnitId, BattleContext context, ISet<string> activeFlags, ISet<string> triggeredDuelIds, bool playerInitiated)
        {
            if (context == null ||
                string.IsNullOrWhiteSpace(DuelId) ||
                !string.Equals(AttackerUnitId, attackerUnitId, StringComparison.Ordinal) ||
                !string.Equals(DefenderUnitId, defenderUnitId, StringComparison.Ordinal) ||
                (PlayerInitiatedOnly && !playerInitiated) ||
                (triggeredDuelIds != null && triggeredDuelIds.Contains(DuelId)))
            {
                return false;
            }

            if (MinimumRoundNumber.HasValue && context.RoundNumber < MinimumRoundNumber.Value)
            {
                return false;
            }

            if (RequiredFlags.Any(flag => activeFlags == null || !activeFlags.Contains(flag)) ||
                ExcludedFlags.Any(flag => activeFlags != null && activeFlags.Contains(flag)))
            {
                return false;
            }

            UnitRuntimeState attacker = context.GetUnit(attackerUnitId);
            UnitRuntimeState defender = context.GetUnit(defenderUnitId);
            if (attacker == null || defender == null || !attacker.IsAlive || !defender.IsAlive)
            {
                return false;
            }

            int hpPercent = defender.MaxHp <= 0
                ? 100
                : (int)Math.Ceiling((defender.CurrentHp * 100f) / defender.MaxHp);
            return hpPercent <= TargetHpPercentAtMost;
        }
    }
}
