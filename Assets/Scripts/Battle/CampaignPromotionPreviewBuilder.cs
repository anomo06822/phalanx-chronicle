using System;
using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Core;
using PhalanxChronicle.Localization;
using PhalanxChronicle.UI;

namespace PhalanxChronicle.Battle
{
    internal sealed class CampaignPromotionPreviewBuilder
    {
        private const int PromotionUnlockLevel = 10;

        public PromotionPreviewModel BuildPreview(CampaignUnitState unit, IReadOnlyList<PromotionDefinition> options, bool isExpanded)
        {
            PromotionPreviewModel model = new PromotionPreviewModel();
            if (unit == null)
            {
                return model;
            }

            bool hasOptions = options != null && options.Count > 0;
            bool inMasteryStage = IsInMasteryStage(unit, hasOptions);
            bool inPromotionStage = unit.Level >= PromotionUnlockLevel;
            string currentClassName = GetClassName(unit.ClassId);

            model.CurrentStageLabel = inMasteryStage
                ? LocalizationService.Text("camp.progression.stage.mastery", "精通")
                : inPromotionStage
                    ? LocalizationService.Text("camp.progression.stage.promotion", "升階")
                    : LocalizationService.Text("camp.progression.stage.base", "初階");

            model.NextStageLabel = inMasteryStage
                ? LocalizationService.Text("camp.progression.next.completed", "下一階段：已完成三段培養")
                : inPromotionStage
                    ? LocalizationService.Format(
                        "camp.progression.next.mastery",
                        "下一階段：精通（Lv{0}）",
                        GetSignatureUnlockLevel(unit))
                    : LocalizationService.Format(
                        "camp.progression.next.promotion",
                        "下一階段：升階（Lv{0}）",
                        PromotionUnlockLevel);

            if (!hasOptions)
            {
                model.PrimarySummary = inMasteryStage
                    ? LocalizationService.Text("camp.progression.preview.no_branch_mastered", "這名角色已進入精通階段，會沿既有職系持續放大定位。")
                    : inPromotionStage
                        ? LocalizationService.Text("camp.progression.preview.no_branch_mid", "這名角色沒有分支轉職，會沿既有職系持續累積戰力。")
                        : LocalizationService.Text("camp.progression.preview.no_branch_base", "這名角色沒有分支轉職，會沿既有職系穩定成長。");
                model.SecondarySummary = LocalizationService.Format(
                    "camp.progression.preview.current_class",
                    "目前職階：{0}",
                    currentClassName);
            }
            else if (unit.HasPromoted)
            {
                model.PrimarySummary = inMasteryStage
                    ? LocalizationService.Text("camp.progression.preview.promoted_mastered", "已進入精通階段，專屬被動與技能節奏都已定型。")
                    : LocalizationService.Format(
                        "camp.progression.preview.promoted",
                        "已升階為 {0}。",
                        currentClassName);
                model.SecondarySummary = inMasteryStage
                    ? LocalizationService.Format(
                        "camp.progression.preview.current_class",
                        "目前職階：{0}",
                        currentClassName)
                    : LocalizationService.Format(
                        "camp.progression.preview.mastery_pending",
                        "Lv{0} 起會進入精通階段，強化專屬被動。",
                        GetSignatureUnlockLevel(unit));
            }
            else if (unit.Level >= PromotionUnlockLevel)
            {
                model.PrimarySummary = LocalizationService.Format(
                    "camp.progression.preview.ready",
                    "目前已達升階階段，待從 {0} 條分支中擇一。",
                    options.Count);
                model.SecondarySummary = LocalizationService.Text(
                    "camp.progression.preview.ready_detail",
                    "升階後會同步更新被動與主動戰技。");
            }
            else
            {
                model.PrimarySummary = LocalizationService.Format(
                    "camp.progression.preview.expected",
                    "預計升階：共 {0} 條分支可選。",
                    options.Count);
                model.SecondarySummary = LocalizationService.Text(
                    "camp.progression.preview.expected_detail",
                    "先完成初階養成，再決定最適合的升階方向。");
            }

            model.ToggleLabel = isExpanded
                ? LocalizationService.Text("camp.progression.toggle.hide", "收合三段介紹")
                : LocalizationService.Text("camp.progression.toggle.show", "查看三段介紹");

            return model;
        }

        public IReadOnlyList<ProgressionStageIntroModel> BuildStageIntro(CampaignUnitState unit, IReadOnlyList<PromotionDefinition> options)
        {
            if (unit == null)
            {
                return Array.Empty<ProgressionStageIntroModel>();
            }

            bool hasOptions = options != null && options.Count > 0;
            bool inMasteryStage = IsInMasteryStage(unit, hasOptions);
            bool inPromotionStage = unit.Level >= PromotionUnlockLevel;
            int advancedSkillLevel = 3;
            int traitSlotLevel = 5;
            int signatureUnlockLevel = GetSignatureUnlockLevel(unit);

            return new[]
            {
                new ProgressionStageIntroModel
                {
                    Title = LocalizationService.Text("camp.progression.stage.base", "初階"),
                    LevelRangeLabel = LocalizationService.Text("camp.progression.range.base", "Lv1-Lv9"),
                    Summary = LocalizationService.Text("camp.progression.summary.base", "先完成角色定位與基礎養成，讓職業輪廓穩定成形。"),
                    UnlocksLabel = LocalizationService.Format(
                        "camp.progression.unlocks.base",
                        "Lv{0}：進階技能 | Lv{1}：特質槽",
                        advancedSkillLevel,
                        traitSlotLevel),
                    StatusBadge = !inPromotionStage
                        ? LocalizationService.Text("camp.progression.badge.current", "目前階段")
                        : LocalizationService.Text("camp.progression.badge.completed", "已完成"),
                    IsReached = unit.Level >= 1,
                },
                new ProgressionStageIntroModel
                {
                    Title = LocalizationService.Text("camp.progression.stage.promotion", "升階"),
                    LevelRangeLabel = LocalizationService.Text("camp.progression.range.promotion", "Lv10-Lv14"),
                    Summary = hasOptions
                        ? LocalizationService.Text("camp.progression.summary.promotion", "選擇升階分支，重塑被動與主動戰技的節奏。")
                        : LocalizationService.Text("camp.progression.summary.promotion_linear", "這名角色沒有分支轉職，會延續既有職系繼續成熟。"),
                    UnlocksLabel = hasOptions
                        ? LocalizationService.Format(
                            "camp.progression.unlocks.promotion",
                            "Lv{0}：可從 {1} 條分支中擇一升階",
                            PromotionUnlockLevel,
                            options.Count)
                        : LocalizationService.Format(
                            "camp.progression.unlocks.promotion_linear",
                            "Lv{0}：延續既有職系並提高整體數值",
                            PromotionUnlockLevel),
                    StatusBadge = unit.Level < PromotionUnlockLevel
                        ? LocalizationService.Text("camp.progression.badge.next", "下一階段")
                        : unit.HasPromoted || !hasOptions
                            ? (!inMasteryStage
                                ? LocalizationService.Text("camp.progression.badge.current", "目前階段")
                                : LocalizationService.Text("camp.progression.badge.completed", "已完成"))
                            : LocalizationService.Text("camp.progression.badge.pending", "待選分支"),
                    IsReached = unit.Level >= PromotionUnlockLevel,
                },
                new ProgressionStageIntroModel
                {
                    Title = LocalizationService.Text("camp.progression.stage.mastery", "精通"),
                    LevelRangeLabel = LocalizationService.Text("camp.progression.range.mastery", "Lv15+"),
                    Summary = LocalizationService.Text("camp.progression.summary.mastery", "專屬被動會進一步強化，角色定位在這一段完全定型。"),
                    UnlocksLabel = LocalizationService.Format(
                        "camp.progression.unlocks.mastery",
                        "Lv{0}：專屬被動強化，既有戰技完成精修",
                        signatureUnlockLevel),
                    StatusBadge = inMasteryStage
                        ? LocalizationService.Text("camp.progression.badge.current", "目前階段")
                        : unit.Level >= signatureUnlockLevel && !unit.HasPromoted && hasOptions
                            ? LocalizationService.Text("camp.progression.badge.pending", "待完成升階")
                            : inPromotionStage
                                ? LocalizationService.Text("camp.progression.badge.next", "下一階段")
                                : LocalizationService.Text("camp.progression.badge.locked", "未達成"),
                    IsReached = inMasteryStage,
                },
            };
        }

        public PromotionComparisonModel BuildPromotionComparison(CampaignUnitState unit, PromotionDefinition definition)
        {
            if (unit == null || definition == null)
            {
                return new PromotionComparisonModel();
            }

            string currentPassiveName = ResolveSkillName(unit.PassiveSkillNameKey, unit.PassiveSkill == PassiveSkillType.None ? "camp.promote.skill.none" : string.Empty, unit.PassiveSkill == PassiveSkillType.None ? "無" : unit.PassiveSkill.ToString());
            string targetPassiveName = ResolveSkillName(definition.PassiveSkillNameKey, definition.PassiveSkill == PassiveSkillType.None ? "camp.promote.skill.none" : string.Empty, definition.PassiveSkill == PassiveSkillType.None ? "無" : definition.PassiveSkill.ToString());
            string currentPassiveDesc = ResolveText(unit.PassiveSkillDescriptionKey);
            string targetPassiveDesc = ResolveText(definition.PassiveSkillDescriptionKey);

            string currentActiveName = ResolveSkillName(unit.ActiveSkillNameKey, unit.ActiveSkill == ActiveSkillType.None ? "camp.promote.skill.none" : string.Empty, unit.ActiveSkill == ActiveSkillType.None ? "無" : unit.ActiveSkill.ToString());
            string targetActiveName = ResolveSkillName(definition.ActiveSkillNameKey, definition.ActiveSkill == ActiveSkillType.None ? "camp.promote.skill.none" : string.Empty, definition.ActiveSkill == ActiveSkillType.None ? "無" : definition.ActiveSkill.ToString());
            string currentActiveDesc = ResolveText(unit.ActiveSkillDescriptionKey);
            string targetActiveDesc = ResolveText(definition.ActiveSkillDescriptionKey);

            return new PromotionComparisonModel
            {
                StatDeltaLabel = LocalizationService.Format(
                    "camp.promote.summary",
                    "+{0} 生命  +{1} 攻  +{2} 防  +{3} 士氣",
                    definition.HpBonus,
                    definition.AttackBonus,
                    definition.DefenseBonus,
                    definition.ManaBonus),
                PassiveCurrentName = currentPassiveName,
                PassiveTargetName = targetPassiveName,
                PassiveChangeLabel = BuildSkillChangeLabel(currentPassiveName, targetPassiveName, currentPassiveDesc, targetPassiveDesc),
                PassiveDetail = targetPassiveDesc,
                ActiveCurrentName = currentActiveName,
                ActiveTargetName = targetActiveName,
                ActiveChangeLabel = BuildSkillChangeLabel(currentActiveName, targetActiveName, currentActiveDesc, targetActiveDesc),
                ActiveDetail = targetActiveDesc,
                MasteryPreview = BuildMasteryPreview(definition.ActiveSkill),
            };
        }

        public PromotionComparisonModel BuildSelectedPromotionSummary(CampaignUnitState unit, PromotionDefinition definition)
        {
            if (unit == null || definition == null)
            {
                return new PromotionComparisonModel();
            }

            string passiveName = ResolveSkillName(unit.PassiveSkillNameKey, unit.PassiveSkill == PassiveSkillType.None ? "camp.promote.skill.none" : string.Empty, unit.PassiveSkill == PassiveSkillType.None ? "無" : unit.PassiveSkill.ToString());
            string passiveDesc = ResolveText(unit.PassiveSkillDescriptionKey);
            string activeName = ResolveSkillName(unit.ActiveSkillNameKey, unit.ActiveSkill == ActiveSkillType.None ? "camp.promote.skill.none" : string.Empty, unit.ActiveSkill == ActiveSkillType.None ? "無" : unit.ActiveSkill.ToString());
            string activeDesc = ResolveText(unit.ActiveSkillDescriptionKey);

            return new PromotionComparisonModel
            {
                StatDeltaLabel = LocalizationService.Format(
                    "camp.promote.summary",
                    "+{0} 生命  +{1} 攻  +{2} 防  +{3} 士氣",
                    definition.HpBonus,
                    definition.AttackBonus,
                    definition.DefenseBonus,
                    definition.ManaBonus),
                PassiveCurrentName = passiveName,
                PassiveTargetName = passiveName,
                PassiveDetail = passiveDesc,
                ActiveCurrentName = activeName,
                ActiveTargetName = activeName,
                ActiveDetail = activeDesc,
                MasteryPreview = BuildMasteryPreview(unit.ActiveSkill),
            };
        }

        public PromotionDefinition ResolveSelectedPromotion(CampaignUnitState unit, IReadOnlyList<PromotionDefinition> options)
        {
            if (unit == null || options == null || options.Count == 0)
            {
                return null;
            }

            PromotionDefinition match = options.FirstOrDefault(option =>
                string.Equals(option.TargetClassId, unit.ClassId, StringComparison.Ordinal) ||
                string.Equals(option.TargetGrowthProfileId, unit.GrowthProfileId, StringComparison.Ordinal));

            if (match != null)
            {
                return match;
            }

            return options.FirstOrDefault(option =>
                option.PassiveSkill == unit.PassiveSkill &&
                option.ActiveSkill == unit.ActiveSkill);
        }

        public string BuildProgressionBodySummary(CampaignUnitState unit, IReadOnlyList<PromotionDefinition> options)
        {
            if (unit == null)
            {
                return string.Empty;
            }

            bool hasOptions = options != null && options.Count > 0;
            if (!hasOptions)
            {
                return IsInMasteryStage(unit, false)
                    ? LocalizationService.Text("camp.progression.body.no_branch_mastered", "成長進度：已進入精通階段，會沿既有職系持續成長。")
                    : unit.Level >= PromotionUnlockLevel
                        ? LocalizationService.Text("camp.progression.body.no_branch_mid", "成長進度：這名角色沒有分支轉職，會沿既有職系穩定成熟。")
                        : LocalizationService.Text("camp.progression.body.no_branch_base", "成長進度：先完成初階養成，後續仍會沿現有職系推進。");
            }

            if (unit.HasPromoted)
            {
                return IsInMasteryStage(unit, true)
                    ? LocalizationService.Text("camp.progression.body.mastered", "升階進度：已進入精通階段。")
                    : LocalizationService.Format(
                        "camp.progression.body.promoted",
                        "升階進度：已升階為 {0}，下一階段是 Lv{1} 精通。",
                        GetClassName(unit.ClassId),
                        GetSignatureUnlockLevel(unit));
            }

            if (unit.Level >= PromotionUnlockLevel)
            {
                return LocalizationService.Format(
                    "camp.progression.body.ready",
                    "升階進度：已達升階門檻，可從 {0} 條分支中擇一。",
                    options.Count);
            }

            return LocalizationService.Format(
                "camp.progression.body.expected",
                "升階進度：Lv{0} 會解鎖升階，共 {1} 條分支可選。",
                PromotionUnlockLevel,
                options.Count);
        }

        private static bool IsInMasteryStage(CampaignUnitState unit, bool hasOptions)
        {
            return unit != null &&
                   unit.Level >= GetSignatureUnlockLevel(unit) &&
                   (unit.HasPromoted || !hasOptions);
        }

        private static int GetSignatureUnlockLevel(CampaignUnitState unit)
        {
            if (unit == null)
            {
                return 15;
            }

            GrowthProfileDefinition growthProfile = GrowthProfileCatalog.Get(unit.GrowthProfileId);
            return growthProfile != null ? growthProfile.SignaturePassiveUnlockLevel : 15;
        }

        private static string ResolveSkillName(string key, string fallbackKey, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                return LocalizationService.Text(key, fallback);
            }

            if (!string.IsNullOrWhiteSpace(fallbackKey))
            {
                return LocalizationService.Text(fallbackKey, fallback);
            }

            return fallback;
        }

        private static string ResolveText(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : LocalizationService.Text(key, string.Empty);
        }

        private static string GetClassName(string classId)
        {
            UnitClassDefinition definition = UnitClassCatalog.Get(classId);
            if (definition == null)
            {
                return classId ?? string.Empty;
            }

            return LocalizationService.Text(definition.DisplayNameKey, classId);
        }

        private static string BuildSkillChangeLabel(string currentName, string targetName, string currentDescription, string targetDescription)
        {
            string none = LocalizationService.Text("camp.promote.skill.none", "無");
            if (string.Equals(currentName, none, StringComparison.Ordinal) &&
                !string.Equals(targetName, none, StringComparison.Ordinal))
            {
                return LocalizationService.Text("camp.promote.compare.added", "新增");
            }

            if (!string.Equals(currentName, none, StringComparison.Ordinal) &&
                string.Equals(targetName, none, StringComparison.Ordinal))
            {
                return LocalizationService.Text("camp.promote.compare.removed", "移除");
            }

            if (string.Equals(currentName, targetName, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(targetDescription) &&
                !string.Equals(currentDescription, targetDescription, StringComparison.Ordinal))
            {
                return LocalizationService.Text("camp.promote.compare.enhanced", "強化");
            }

            return string.Empty;
        }

        private static string BuildMasteryPreview(ActiveSkillType skillType)
        {
            string deltaKey = GetMasteryDeltaKey(skillType);
            if (string.IsNullOrWhiteSpace(deltaKey))
            {
                return LocalizationService.Text("camp.progression.mastery.generic", "精通後會進一步放大這套戰技的節奏。");
            }

            return LocalizationService.Format(
                "camp.progression.mastery.preview",
                "精通預告：{0}",
                LocalizationService.Text(deltaKey, string.Empty));
        }

        private static string GetMasteryDeltaKey(ActiveSkillType skillType)
        {
            switch (skillType)
            {
                case ActiveSkillType.RoyalAid:
                    return "ui.mastery.delta.royal_aid";
                case ActiveSkillType.ImperialAid:
                    return "ui.mastery.delta.imperial_aid";
                case ActiveSkillType.GuardOrder:
                    return "ui.mastery.delta.guard_order";
                case ActiveSkillType.KingsBanner:
                    return "ui.mastery.delta.kings_banner";
                case ActiveSkillType.PowerStrike:
                    return "ui.mastery.delta.power_strike";
                case ActiveSkillType.DragonPierce:
                    return "ui.mastery.delta.dragon_pierce";
                case ActiveSkillType.WhiteHorseRescue:
                    return "ui.mastery.delta.white_horse_rescue";
                case ActiveSkillType.Volley:
                    return "ui.mastery.delta.volley";
                case ActiveSkillType.SkyVolley:
                    return "ui.mastery.delta.sky_volley";
                case ActiveSkillType.CrimsonCrescent:
                    return "ui.mastery.delta.crimson_crescent";
                case ActiveSkillType.GreenDragonSlash:
                    return "ui.mastery.delta.green_dragon_slash";
                case ActiveSkillType.AzureDragonSlash:
                    return "ui.mastery.delta.azure_dragon_slash";
                case ActiveSkillType.WesternStampede:
                    return "ui.mastery.delta.western_stampede";
                case ActiveSkillType.StormbreakCharge:
                    return "ui.mastery.delta.stormbreak_charge";
                case ActiveSkillType.WarCry:
                    return "ui.mastery.delta.war_cry";
                case ActiveSkillType.LionWarCry:
                    return "ui.mastery.delta.lion_war_cry";
                case ActiveSkillType.StonewallChallenge:
                    return "ui.mastery.delta.stonewall_challenge";
                case ActiveSkillType.DustDevilSweep:
                    return "ui.mastery.delta.dust_devil_sweep";
                case ActiveSkillType.PinningShot:
                    return "ui.mastery.delta.pinning_shot";
                case ActiveSkillType.FireStratagem:
                    return "ui.mastery.delta.fire_stratagem";
                case ActiveSkillType.EightTrigramInferno:
                    return "ui.mastery.delta.eight_trigram_inferno";
                case ActiveSkillType.FeatherFormation:
                    return "ui.mastery.delta.feather_formation";
                default:
                    return string.Empty;
            }
        }
    }
}
