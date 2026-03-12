using System;

namespace PhalanxChronicle.Core
{
    public static class CampaignSaveNormalizer
    {
        public static bool Normalize(CampaignSaveData saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            bool changed = false;
            CampaignUnitState zhugeLiang = saveData.GetUnit("player-zhuge-liang");
            if (zhugeLiang != null)
            {
                if (string.Equals(zhugeLiang.ClassId, "sleeping_dragon", StringComparison.Ordinal) &&
                    zhugeLiang.ActiveSkill == ActiveSkillType.ImperialAid)
                {
                    zhugeLiang.SetActiveSkill(
                        ActiveSkillType.EightTrigramInferno,
                        "skill.eight_trigram_inferno.name",
                        "skill.eight_trigram_inferno.desc");
                    changed = true;
                }
                else if (string.Equals(zhugeLiang.ClassId, "commander", StringComparison.Ordinal) &&
                         zhugeLiang.ActiveSkill == ActiveSkillType.RoyalAid)
                {
                    zhugeLiang.SetActiveSkill(
                        ActiveSkillType.FireStratagem,
                        "skill.fire_stratagem.name",
                        "skill.fire_stratagem.desc");
                    changed = true;
                }
            }

            CampaignUnitState zhaoYun = saveData.GetUnit("player-zhao-yun");
            if (zhaoYun != null &&
                (string.Equals(zhaoYun.ClassId, "scout", StringComparison.Ordinal) ||
                 string.Equals(zhaoYun.ClassId, "white_horse_general", StringComparison.Ordinal)) &&
                zhaoYun.ActiveSkill == ActiveSkillType.PowerStrike)
            {
                zhaoYun.SetActiveSkill(
                    ActiveSkillType.DragonPierce,
                    "skill.dragon_pierce.name",
                    "skill.dragon_pierce.desc");
                changed = true;
            }

            CampaignUnitState maChao = saveData.GetUnit("player-ma-chao");
            if (maChao != null &&
                (string.Equals(maChao.ClassId, "raider", StringComparison.Ordinal) ||
                 string.Equals(maChao.ClassId, "storm_raider", StringComparison.Ordinal)) &&
                maChao.ActiveSkill == ActiveSkillType.PowerStrike)
            {
                maChao.SetActiveSkill(
                    ActiveSkillType.WesternStampede,
                    "skill.western_stampede.name",
                    "skill.western_stampede.desc");
                changed = true;
            }

            return changed;
        }
    }
}
