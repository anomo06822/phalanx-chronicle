namespace PhalanxChronicle.Core
{
    public static class ActiveSkillRules
    {
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
                case ActiveSkillType.AzureDragonSlash:
                    return PassiveSkillRules.GetAttackRange(unit);
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
                   skillType == ActiveSkillType.PinningShot;
        }

        public static int GetPowerStrikeBonus()
        {
            return 4;
        }

        public static int GetVolleyBonus()
        {
            return 1;
        }

        public static int GetSkyVolleyBonus()
        {
            return 2;
        }

        public static int GetGreenDragonSlashBonus()
        {
            return 2;
        }

        public static int GetAzureDragonSlashBonus()
        {
            return 4;
        }

        public static int GetRoyalAidAmount()
        {
            return 8;
        }

        public static int GetImperialAidSplashAmount()
        {
            return 4;
        }

        public static int GetGuardOrderHealAmount()
        {
            return 4;
        }

        public static int GetPinningShotBonus()
        {
            return 2;
        }

        public static int GetCooldown(ActiveSkillType skillType)
        {
            switch (skillType)
            {
                case ActiveSkillType.RoyalAid:
                    return 2;
                case ActiveSkillType.PowerStrike:
                    return 1;
                case ActiveSkillType.Volley:
                    return 2;
                case ActiveSkillType.GreenDragonSlash:
                    return 2;
                case ActiveSkillType.WarCry:
                    return 2;
                case ActiveSkillType.ImperialAid:
                    return 3;
                case ActiveSkillType.AzureDragonSlash:
                    return 2;
                case ActiveSkillType.LionWarCry:
                    return 2;
                case ActiveSkillType.SkyVolley:
                    return 2;
                case ActiveSkillType.GuardOrder:
                    return 2;
                case ActiveSkillType.PinningShot:
                    return 2;
                default:
                    return 0;
            }
        }

        public static int GetInspiredDuration()
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

        public static int GetIntimidatedDuration()
        {
            return 1;
        }
    }
}
