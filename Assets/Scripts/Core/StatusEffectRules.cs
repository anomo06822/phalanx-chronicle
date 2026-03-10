using System.Linq;

namespace PhalanxChronicle.Core
{
    public static class StatusEffectRules
    {
        public static int GetAttackModifier(UnitRuntimeState unit)
        {
            return unit.HasStatus(StatusEffectType.Inspired) ? 2 : 0;
        }

        public static int GetDefenseModifier(UnitRuntimeState unit)
        {
            return unit.HasStatus(StatusEffectType.ShatteredArmor) ? -2 : 0;
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
