using System.Linq;

namespace PhalanxChronicle.Core
{
    public static class StatusEffectRules
    {
        public static int GetAttackModifier(UnitRuntimeState unit)
        {
            int modifier = 0;
            if (unit.HasStatus(StatusEffectType.Inspired))
            {
                modifier += 2;
            }

            if (unit.HasStatus(StatusEffectType.Intimidated))
            {
                modifier -= 2;
            }

            if (unit.HasStatus(StatusEffectType.Taunted))
            {
                modifier -= 1;
            }

            return modifier;
        }

        public static int GetDefenseModifier(UnitRuntimeState unit)
        {
            int modifier = 0;
            if (unit.HasStatus(StatusEffectType.ShatteredArmor))
            {
                modifier -= 2;
            }

            if (unit.HasStatus(StatusEffectType.Guarded))
            {
                modifier += 2;
            }

            return modifier;
        }

        public static bool IsMovementBlocked(UnitRuntimeState unit)
        {
            return unit != null && unit.HasStatus(StatusEffectType.Rooted);
        }

        public static string BuildSummary(UnitRuntimeState unit)
        {
            if (unit == null || unit.StatusEffects.Count == 0)
            {
                return "none";
            }

            return string.Join(", ", unit.StatusEffects.Select(effect => effect.Type.ToString()));
        }
    }
}
