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
                string.Equals(zhaoYun.ClassId, "scout", StringComparison.Ordinal) &&
                zhaoYun.ActiveSkill == ActiveSkillType.PowerStrike)
            {
                zhaoYun.SetActiveSkill(
                    ActiveSkillType.DragonPierce,
                    "skill.dragon_pierce.name",
                    "skill.dragon_pierce.desc");
                changed = true;
            }
            else if (zhaoYun != null &&
                     string.Equals(zhaoYun.ClassId, "white_horse_general", StringComparison.Ordinal) &&
                     (zhaoYun.ActiveSkill == ActiveSkillType.PowerStrike || zhaoYun.ActiveSkill == ActiveSkillType.DragonPierce))
            {
                zhaoYun.SetActiveSkill(
                    ActiveSkillType.WhiteHorseRescue,
                    "skill.white_horse_rescue.name",
                    "skill.white_horse_rescue.desc");
                changed = true;
            }

            CampaignUnitState maChao = saveData.GetUnit("player-ma-chao");
            if (maChao != null &&
                string.Equals(maChao.ClassId, "raider", StringComparison.Ordinal) &&
                maChao.ActiveSkill == ActiveSkillType.PowerStrike)
            {
                maChao.SetActiveSkill(
                    ActiveSkillType.WesternStampede,
                    "skill.western_stampede.name",
                    "skill.western_stampede.desc");
                changed = true;
            }
            else if (maChao != null &&
                     string.Equals(maChao.ClassId, "storm_raider", StringComparison.Ordinal) &&
                     (maChao.ActiveSkill == ActiveSkillType.PowerStrike || maChao.ActiveSkill == ActiveSkillType.WesternStampede))
            {
                maChao.SetActiveSkill(
                    ActiveSkillType.StormbreakCharge,
                    "skill.stormbreak_charge.name",
                    "skill.stormbreak_charge.desc");
                changed = true;
            }
            else if (maChao != null &&
                     string.Equals(maChao.ClassId, "western_lancer", StringComparison.Ordinal) &&
                     maChao.ActiveSkill == ActiveSkillType.WarCry)
            {
                maChao.SetActiveSkill(
                    ActiveSkillType.DustDevilSweep,
                    "skill.dust_devil_sweep.name",
                    "skill.dust_devil_sweep.desc");
                changed = true;
            }

            CampaignUnitState liuBei = saveData.GetUnit("player-liu-bei");
            if (liuBei != null &&
                string.Equals(liuBei.ClassId, "warlord", StringComparison.Ordinal) &&
                liuBei.ActiveSkill == ActiveSkillType.GuardOrder)
            {
                liuBei.SetActiveSkill(
                    ActiveSkillType.KingsBanner,
                    "skill.kings_banner.name",
                    "skill.kings_banner.desc");
                changed = true;
            }

            CampaignUnitState guanYu = saveData.GetUnit("player-guan-yu");
            if (guanYu != null &&
                string.Equals(guanYu.ClassId, "halberdier_general", StringComparison.Ordinal) &&
                guanYu.ActiveSkill == ActiveSkillType.PowerStrike)
            {
                guanYu.SetActiveSkill(
                    ActiveSkillType.CrimsonCrescent,
                    "skill.crimson_crescent.name",
                    "skill.crimson_crescent.desc");
                changed = true;
            }

            CampaignUnitState zhangFei = saveData.GetUnit("player-zhang-fei");
            if (zhangFei != null &&
                string.Equals(zhangFei.ClassId, "fortress_general", StringComparison.Ordinal) &&
                zhangFei.ActiveSkill == ActiveSkillType.GuardOrder)
            {
                zhangFei.SetActiveSkill(
                    ActiveSkillType.StonewallChallenge,
                    "skill.stonewall_challenge.name",
                    "skill.stonewall_challenge.desc");
                changed = true;
            }

            CampaignUnitState tactician = saveData.GetUnit("player-zhuge-liang");
            if (tactician != null &&
                string.Equals(tactician.ClassId, "tactician_general", StringComparison.Ordinal) &&
                tactician.ActiveSkill == ActiveSkillType.GuardOrder)
            {
                tactician.SetActiveSkill(
                    ActiveSkillType.FeatherFormation,
                    "skill.feather_formation.name",
                    "skill.feather_formation.desc");
                changed = true;
            }

            return changed;
        }
    }
}
