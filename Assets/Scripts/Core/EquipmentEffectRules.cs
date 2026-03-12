using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    public static class EquipmentEffectRules
    {
        public static int GetSkillDamageBonus(UnitRuntimeState unit)
        {
            return CountTreasureEffects(unit, TreasureEffectType.SkillDamageBonus) * 2;
        }

        public static int GetStatusDurationBonus(UnitRuntimeState unit)
        {
            return HasTreasureEffect(unit, TreasureEffectType.StatusDurationBonus) ? 1 : 0;
        }

        public static bool ShouldGainGuardAtLowHp(UnitRuntimeState unit)
        {
            return HasTreasureEffect(unit, TreasureEffectType.GuardOnLowHp);
        }

        public static bool IgnoresHazardTick(UnitRuntimeState unit)
        {
            return HasTreasureEffect(unit, TreasureEffectType.IgnoreHazardTick);
        }

        public static int GetFortHealingBonus(UnitRuntimeState unit)
        {
            return HasTreasureEffect(unit, TreasureEffectType.FortHealingBonus) ? 2 : 0;
        }

        public static int GetMoveBonus(BattleContext context, UnitRuntimeState unit)
        {
            return context != null &&
                   context.RoundNumber <= 3 &&
                   HasTreasureEffect(unit, TreasureEffectType.MovePlusOneOnFirstThreeTurns)
                ? 1
                : 0;
        }

        public static bool HasTreasureEffect(UnitRuntimeState unit, TreasureEffectType effectType)
        {
            return CountTreasureEffects(unit, effectType) > 0;
        }

        private static int CountTreasureEffects(UnitRuntimeState unit, TreasureEffectType effectType)
        {
            if (unit == null || unit.EquipmentLoadout == null || effectType == TreasureEffectType.None)
            {
                return 0;
            }

            int count = 0;
            foreach (ItemDefinition item in GetEquippedItems(unit.EquipmentLoadout))
            {
                if (item != null &&
                    item.IsTreasure &&
                    item.TreasureEffect == effectType)
                {
                    count++;
                }
            }

            return count;
        }

        private static IEnumerable<ItemDefinition> GetEquippedItems(EquipmentLoadout loadout)
        {
            yield return ItemCatalog.Get(loadout.WeaponId);
            yield return ItemCatalog.Get(loadout.ArmorId);
            yield return ItemCatalog.Get(loadout.MountId);
        }
    }
}
