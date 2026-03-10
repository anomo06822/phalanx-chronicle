namespace PhalanxChronicle.Core
{
    public static class ActiveSkillRules
    {
        public static int GetRange(UnitRuntimeState unit)
        {
            switch (unit.ActiveSkill)
            {
                case ActiveSkillType.RoyalAid:
                    return 2;
                case ActiveSkillType.PowerStrike:
                    return PassiveSkillRules.GetAttackRange(unit);
                case ActiveSkillType.Volley:
                    return 3;
                default:
                    return 0;
            }
        }

        public static bool IsSupportSkill(ActiveSkillType skillType)
        {
            return skillType == ActiveSkillType.RoyalAid;
        }

        public static bool IsOffensiveSkill(ActiveSkillType skillType)
        {
            return skillType == ActiveSkillType.PowerStrike || skillType == ActiveSkillType.Volley;
        }

        public static int GetPowerStrikeBonus()
        {
            return 4;
        }

        public static int GetVolleyBonus()
        {
            return 1;
        }

        public static int GetRoyalAidAmount()
        {
            return 8;
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
    }
}
