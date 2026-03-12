namespace PhalanxChronicle.Core
{
    public static class ActiveSkillRules
    {
        public static bool IsMastered(UnitRuntimeState unit)
        {
            return unit != null && unit.Level >= 10;
        }

        public static int GetRange(UnitRuntimeState unit)
        {
            switch (unit.ActiveSkill)
            {
                case ActiveSkillType.RoyalAid:
                case ActiveSkillType.ImperialAid:
                case ActiveSkillType.GuardOrder:
                    return 2;
                case ActiveSkillType.PinningShot:
                    return PassiveSkillRules.GetAttackRange(unit) + 1;
                case ActiveSkillType.PowerStrike:
                case ActiveSkillType.DragonPierce:
                case ActiveSkillType.AzureDragonSlash:
                case ActiveSkillType.WesternStampede:
                    return PassiveSkillRules.GetAttackRange(unit);
                case ActiveSkillType.FireStratagem:
                    return 3;
                case ActiveSkillType.EightTrigramInferno:
                    return 4;
                case ActiveSkillType.Volley:
                case ActiveSkillType.SkyVolley:
                    return 3;
                case ActiveSkillType.GreenDragonSlash:
                    return 1;
                case ActiveSkillType.WarCry:
                    return 1;
                case ActiveSkillType.LionWarCry:
                    return 2;
                default:
                    return 0;
            }
        }

        public static int GetManaCost(ActiveSkillType skillType)
        {
            switch (skillType)
            {
                case ActiveSkillType.RoyalAid:
                    return 2;
                case ActiveSkillType.PowerStrike:
                    return 3;
                case ActiveSkillType.Volley:
                    return 4;
                case ActiveSkillType.GreenDragonSlash:
                    return 4;
                case ActiveSkillType.WarCry:
                    return 3;
                case ActiveSkillType.ImperialAid:
                    return 4;
                case ActiveSkillType.AzureDragonSlash:
                    return 5;
                case ActiveSkillType.LionWarCry:
                    return 4;
                case ActiveSkillType.SkyVolley:
                    return 5;
                case ActiveSkillType.GuardOrder:
                    return 3;
                case ActiveSkillType.PinningShot:
                    return 4;
                case ActiveSkillType.FireStratagem:
                    return 4;
                case ActiveSkillType.EightTrigramInferno:
                    return 5;
                case ActiveSkillType.DragonPierce:
                    return 4;
                case ActiveSkillType.WesternStampede:
                    return 4;
                default:
                    return 0;
            }
        }

        public static int GetManaCost(UnitRuntimeState unit)
        {
            return GetManaCost(unit.ActiveSkill);
        }

        public static bool IsSupportSkill(ActiveSkillType skillType)
        {
            return skillType == ActiveSkillType.RoyalAid ||
                   skillType == ActiveSkillType.ImperialAid ||
                   skillType == ActiveSkillType.GuardOrder;
        }

        public static bool IsOffensiveSkill(ActiveSkillType skillType)
        {
            return skillType == ActiveSkillType.PowerStrike ||
                   skillType == ActiveSkillType.Volley ||
                   skillType == ActiveSkillType.GreenDragonSlash ||
                   skillType == ActiveSkillType.AzureDragonSlash ||
                   skillType == ActiveSkillType.SkyVolley ||
                   skillType == ActiveSkillType.PinningShot ||
                   skillType == ActiveSkillType.FireStratagem ||
                   skillType == ActiveSkillType.EightTrigramInferno ||
                   skillType == ActiveSkillType.DragonPierce ||
                   skillType == ActiveSkillType.WesternStampede;
        }

        public static int GetPowerStrikeBonus()
        {
            return 4;
        }

        public static int GetPowerStrikeBonus(UnitRuntimeState unit)
        {
            return (IsMastered(unit) ? 6 : GetPowerStrikeBonus()) + EquipmentEffectRules.GetSkillDamageBonus(unit);
        }

        public static int GetVolleyBonus()
        {
            return 1;
        }

        public static int GetVolleyBonus(UnitRuntimeState unit)
        {
            return (IsMastered(unit) ? 2 : GetVolleyBonus()) + EquipmentEffectRules.GetSkillDamageBonus(unit);
        }

        public static int GetSkyVolleyBonus()
        {
            return 2;
        }

        public static int GetSkyVolleyBonus(UnitRuntimeState unit)
        {
            return (IsMastered(unit) ? 4 : GetSkyVolleyBonus()) + EquipmentEffectRules.GetSkillDamageBonus(unit);
        }

        public static int GetGreenDragonSlashBonus()
        {
            return 2;
        }

        public static int GetGreenDragonSlashBonus(UnitRuntimeState unit)
        {
            return (IsMastered(unit) ? 3 : GetGreenDragonSlashBonus()) + EquipmentEffectRules.GetSkillDamageBonus(unit);
        }

        public static int GetAzureDragonSlashBonus()
        {
            return 4;
        }

        public static int GetAzureDragonSlashBonus(UnitRuntimeState unit)
        {
            return (IsMastered(unit) ? 5 : GetAzureDragonSlashBonus()) + EquipmentEffectRules.GetSkillDamageBonus(unit);
        }

        public static int GetDragonPierceBonus()
        {
            return 3;
        }

        public static int GetDragonPierceBonus(UnitRuntimeState unit)
        {
            return (IsMastered(unit) ? 5 : GetDragonPierceBonus()) + EquipmentEffectRules.GetSkillDamageBonus(unit);
        }

        public static int GetDragonPierceIgnoredDefense(UnitRuntimeState unit)
        {
            return IsMastered(unit) ? 3 : 2;
        }

        public static int GetWesternStampedeBonus()
        {
            return 2;
        }

        public static int GetWesternStampedeBonus(UnitRuntimeState unit)
        {
            return (IsMastered(unit) ? 3 : GetWesternStampedeBonus()) + EquipmentEffectRules.GetSkillDamageBonus(unit);
        }

        public static int GetRoyalAidAmount()
        {
            return 8;
        }

        public static int GetRoyalAidAmount(UnitRuntimeState unit)
        {
            return IsMastered(unit) ? 10 : GetRoyalAidAmount();
        }

        public static int GetImperialAidSplashAmount()
        {
            return 4;
        }

        public static int GetImperialAidSplashAmount(UnitRuntimeState unit)
        {
            return IsMastered(unit) ? 6 : GetImperialAidSplashAmount();
        }

        public static int GetGuardOrderHealAmount()
        {
            return 4;
        }

        public static int GetGuardOrderHealAmount(UnitRuntimeState unit)
        {
            return IsMastered(unit) ? 6 : GetGuardOrderHealAmount();
        }

        public static int GetPinningShotBonus()
        {
            return 2;
        }

        public static int GetPinningShotBonus(UnitRuntimeState unit)
        {
            return (IsMastered(unit) ? 4 : GetPinningShotBonus()) + EquipmentEffectRules.GetSkillDamageBonus(unit);
        }

        public static int GetFireStratagemBonus()
        {
            return 1;
        }

        public static int GetFireStratagemBonus(UnitRuntimeState unit)
        {
            return (IsMastered(unit) ? 3 : GetFireStratagemBonus()) + EquipmentEffectRules.GetSkillDamageBonus(unit);
        }

        public static int GetEightTrigramInfernoBonus()
        {
            return 3;
        }

        public static int GetEightTrigramInfernoBonus(UnitRuntimeState unit)
        {
            return (IsMastered(unit) ? 5 : GetEightTrigramInfernoBonus()) + EquipmentEffectRules.GetSkillDamageBonus(unit);
        }

        public static int GetCooldown(ActiveSkillType skillType)
        {
            return 0;
        }

        public static int GetInspiredDuration()
        {
            return 1;
        }

        public static int GetGuardedDuration()
        {
            return 1;
        }

        public static int GetShatteredArmorDuration()
        {
            return 1;
        }

        public static int GetShatteredArmorDuration(ActiveSkillType skillType)
        {
            return skillType == ActiveSkillType.SkyVolley ? 2 : 1;
        }

        public static int GetShatteredArmorDuration(ActiveSkillType skillType, UnitRuntimeState unit)
        {
            if (!IsMastered(unit))
            {
                return GetShatteredArmorDuration(skillType);
            }

            switch (skillType)
            {
                case ActiveSkillType.PowerStrike:
                    return 2;
                case ActiveSkillType.Volley:
                    return 2;
                case ActiveSkillType.SkyVolley:
                    return 3;
                case ActiveSkillType.AzureDragonSlash:
                    return 2;
                case ActiveSkillType.EightTrigramInferno:
                    return 2;
                default:
                    return GetShatteredArmorDuration(skillType);
            }
        }

        public static int GetIntimidatedDuration()
        {
            return 1;
        }

        public static int GetIntimidatedDuration(ActiveSkillType skillType, UnitRuntimeState unit)
        {
            if (!IsMastered(unit))
            {
                return GetIntimidatedDuration();
            }

            switch (skillType)
            {
                case ActiveSkillType.WarCry:
                case ActiveSkillType.LionWarCry:
                case ActiveSkillType.WesternStampede:
                    return 2;
                default:
                    return GetIntimidatedDuration();
            }
        }

        public static int GetRootedDuration(UnitRuntimeState unit)
        {
            return IsMastered(unit) ? 2 : 1;
        }
    }
}
