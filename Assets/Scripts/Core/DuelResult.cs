using System;
using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    public sealed class DuelResult
    {
        public DuelResult(
            string duelId,
            string titleKey,
            string titleFallback,
            string animationProfileId,
            string attackerUnitId,
            string defenderUnitId,
            SkillEffectResult defenderEffect,
            IReadOnlyList<SkillStatusApplication> casterStatuses = null,
            IReadOnlyList<SkillEffectResult> nearbyEnemyEffects = null,
            IReadOnlyList<string> setFlags = null,
            int attackerExpGained = 0,
            int attackerLevelsGained = 0)
        {
            DuelId = duelId ?? string.Empty;
            TitleKey = titleKey ?? string.Empty;
            TitleFallback = titleFallback ?? string.Empty;
            AnimationProfileId = animationProfileId ?? string.Empty;
            AttackerUnitId = attackerUnitId ?? string.Empty;
            DefenderUnitId = defenderUnitId ?? string.Empty;
            DefenderEffect = defenderEffect;
            CasterStatuses = casterStatuses ?? Array.Empty<SkillStatusApplication>();
            NearbyEnemyEffects = nearbyEnemyEffects ?? Array.Empty<SkillEffectResult>();
            SetFlags = setFlags ?? Array.Empty<string>();
            AttackerExpGained = attackerExpGained < 0 ? 0 : attackerExpGained;
            AttackerLevelsGained = attackerLevelsGained < 0 ? 0 : attackerLevelsGained;
        }

        public string DuelId { get; }

        public string TitleKey { get; }

        public string TitleFallback { get; }

        public string AnimationProfileId { get; }

        public string AttackerUnitId { get; }

        public string DefenderUnitId { get; }

        public SkillEffectResult DefenderEffect { get; }

        public IReadOnlyList<SkillStatusApplication> CasterStatuses { get; }

        public IReadOnlyList<SkillEffectResult> NearbyEnemyEffects { get; }

        public IReadOnlyList<string> SetFlags { get; }

        public int AttackerExpGained { get; }

        public int AttackerLevelsGained { get; }

        public bool DefenderDefeated => DefenderEffect != null && DefenderEffect.UnitDied;
    }
}
