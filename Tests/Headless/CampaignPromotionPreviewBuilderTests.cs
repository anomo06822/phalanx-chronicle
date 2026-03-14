using System.Linq;
using PhalanxChronicle.Battle;
using PhalanxChronicle.Core;
using PhalanxChronicle.Localization;
using PhalanxChronicle.UI;
using Xunit;

namespace PhalanxChronicle.Headless.Tests
{
    public sealed class CampaignPromotionPreviewBuilderTests
    {
        [Fact]
        public void BuildPreview_Level8_ShowsExpectedPromotionAndLv10Unlock()
        {
            GameLocale originalLocale = LocalizationService.CurrentLocale;
            LocalizationService.SetLocale(GameLocale.TraditionalChinese);

            try
            {
                CampaignPromotionPreviewBuilder builder = new CampaignPromotionPreviewBuilder();
                CampaignUnitState liuBei = CreateLiuBei(level: 8);
                PromotionPreviewModel preview = builder.BuildPreview(liuBei, PromotionCatalog.GetOptions(liuBei.UnitId), false);

                Assert.Equal("初階", preview.CurrentStageLabel);
                Assert.Contains("Lv10", preview.NextStageLabel);
                Assert.Contains("2", preview.PrimarySummary);
                Assert.Equal("查看三段介紹", preview.ToggleLabel);
            }
            finally
            {
                LocalizationService.SetLocale(originalLocale);
            }
        }

        [Fact]
        public void BuildPromotionComparison_Level10_ProvidesBeforeAfterForEveryBranch()
        {
            GameLocale originalLocale = LocalizationService.CurrentLocale;
            LocalizationService.SetLocale(GameLocale.TraditionalChinese);

            try
            {
                CampaignPromotionPreviewBuilder builder = new CampaignPromotionPreviewBuilder();
                CampaignUnitState zhugeLiang = CreateZhugeLiang(level: 10);
                PromotionDefinition[] options = PromotionCatalog.GetOptions(zhugeLiang.UnitId).ToArray();

                Assert.Equal(2, options.Length);

                PromotionComparisonModel[] comparisons = options
                    .Select(option => builder.BuildPromotionComparison(zhugeLiang, option))
                    .ToArray();

                Assert.All(comparisons, comparison =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(comparison.StatDeltaLabel));
                    Assert.False(string.IsNullOrWhiteSpace(comparison.PassiveCurrentName));
                    Assert.False(string.IsNullOrWhiteSpace(comparison.PassiveTargetName));
                    Assert.False(string.IsNullOrWhiteSpace(comparison.ActiveCurrentName));
                    Assert.False(string.IsNullOrWhiteSpace(comparison.ActiveTargetName));
                    Assert.False(string.IsNullOrWhiteSpace(comparison.MasteryPreview));
                });
            }
            finally
            {
                LocalizationService.SetLocale(originalLocale);
            }
        }

        [Fact]
        public void BuildSelectedPromotionSummary_Level12_UsesCurrentBranchAndKeepsMasteryPreview()
        {
            GameLocale originalLocale = LocalizationService.CurrentLocale;
            LocalizationService.SetLocale(GameLocale.TraditionalChinese);

            try
            {
                CampaignPromotionPreviewBuilder builder = new CampaignPromotionPreviewBuilder();
                CampaignUnitState tacticianZhuge = CreatePromotedZhugeLiang(level: 12);
                PromotionDefinition selected = builder.ResolveSelectedPromotion(tacticianZhuge, PromotionCatalog.GetOptions(tacticianZhuge.UnitId));
                PromotionComparisonModel comparison = builder.BuildSelectedPromotionSummary(tacticianZhuge, selected);
                PromotionPreviewModel preview = builder.BuildPreview(tacticianZhuge, PromotionCatalog.GetOptions(tacticianZhuge.UnitId), true);

                Assert.NotNull(selected);
                Assert.Equal("升階", preview.CurrentStageLabel);
                Assert.Contains("Lv15", preview.NextStageLabel);
                Assert.Equal(comparison.ActiveCurrentName, comparison.ActiveTargetName);
                Assert.False(string.IsNullOrWhiteSpace(comparison.MasteryPreview));
            }
            finally
            {
                LocalizationService.SetLocale(originalLocale);
            }
        }

        [Fact]
        public void BuildStageIntro_Level15_MarksMasteryAsCurrent()
        {
            GameLocale originalLocale = LocalizationService.CurrentLocale;
            LocalizationService.SetLocale(GameLocale.TraditionalChinese);

            try
            {
                CampaignPromotionPreviewBuilder builder = new CampaignPromotionPreviewBuilder();
                CampaignUnitState promotedZhuge = CreatePromotedZhugeLiang(level: 15);
                ProgressionStageIntroModel masteryStage = builder.BuildStageIntro(promotedZhuge, PromotionCatalog.GetOptions(promotedZhuge.UnitId))
                    .Single(stage => stage.Title == "精通");

                Assert.True(masteryStage.IsReached);
                Assert.Equal("目前階段", masteryStage.StatusBadge);
            }
            finally
            {
                LocalizationService.SetLocale(originalLocale);
            }
        }

        [Fact]
        public void BuildPreview_WithoutPromotionBranches_StillBuildsThreeStageIntro()
        {
            GameLocale originalLocale = LocalizationService.CurrentLocale;
            LocalizationService.SetLocale(GameLocale.TraditionalChinese);

            try
            {
                CampaignPromotionPreviewBuilder builder = new CampaignPromotionPreviewBuilder();
                CampaignUnitState genericCommander = CreateGenericCommander(level: 6);
                PromotionPreviewModel preview = builder.BuildPreview(genericCommander, PromotionCatalog.GetOptions(genericCommander.UnitId), false);
                ProgressionStageIntroModel[] stageIntro = builder.BuildStageIntro(genericCommander, PromotionCatalog.GetOptions(genericCommander.UnitId)).ToArray();

                Assert.False(string.IsNullOrWhiteSpace(preview.PrimarySummary));
                Assert.Equal(3, stageIntro.Length);
                Assert.Contains(stageIntro, stage => stage.Title == "初階");
                Assert.Contains(stageIntro, stage => stage.Title == "升階");
                Assert.Contains(stageIntro, stage => stage.Title == "精通");
            }
            finally
            {
                LocalizationService.SetLocale(originalLocale);
            }
        }

        private static CampaignUnitState CreateLiuBei(int level)
        {
            return new CampaignUnitState(
                "player-liu-bei",
                "Liu Bei",
                "unit.player_liu_bei",
                UnitRole.Commander,
                "role.commander",
                PassiveSkillType.BenevolentCommand,
                "skill.benevolent_command.name",
                "skill.benevolent_command.desc",
                ActiveSkillType.ImperialAid,
                "skill.imperial_aid.name",
                "skill.imperial_aid.desc",
                29,
                9,
                5,
                3,
                1,
                22,
                "commander",
                "commander",
                AiProfileType.Support,
                EquipmentLoadout.Empty,
                level: level);
        }

        private static CampaignUnitState CreateZhugeLiang(int level)
        {
            return new CampaignUnitState(
                "player-zhuge-liang",
                "Zhuge Liang",
                "unit.player_zhuge_liang",
                UnitRole.Commander,
                "role.commander",
                PassiveSkillType.CommandAura,
                "skill.command_aura.name",
                "skill.command_aura.desc",
                ActiveSkillType.FireStratagem,
                "skill.fire_stratagem.name",
                "skill.fire_stratagem.desc",
                28,
                8,
                4,
                3,
                1,
                20,
                "commander",
                "commander",
                AiProfileType.Support,
                EquipmentLoadout.Empty,
                level: level);
        }

        private static CampaignUnitState CreatePromotedZhugeLiang(int level)
        {
            return new CampaignUnitState(
                "player-zhuge-liang",
                "Zhuge Liang",
                "unit.player_zhuge_liang",
                UnitRole.Commander,
                "role.commander",
                PassiveSkillType.CommandAura,
                "skill.command_aura.name",
                "skill.command_aura.desc",
                ActiveSkillType.FeatherFormation,
                "skill.feather_formation.name",
                "skill.feather_formation.desc",
                31,
                9,
                5,
                3,
                1,
                23,
                "tactician_general",
                "tactician_general",
                AiProfileType.Support,
                EquipmentLoadout.Empty,
                level: level,
                hasPromoted: true);
        }

        private static CampaignUnitState CreateGenericCommander(int level)
        {
            return new CampaignUnitState(
                "player-generic",
                "Generic Commander",
                "unit.player_zhuge_liang",
                UnitRole.Commander,
                "role.commander",
                PassiveSkillType.CommandAura,
                "skill.command_aura.name",
                "skill.command_aura.desc",
                ActiveSkillType.RoyalAid,
                "skill.royal_aid.name",
                "skill.royal_aid.desc",
                26,
                8,
                4,
                3,
                1,
                18,
                "commander",
                "commander",
                AiProfileType.Support,
                EquipmentLoadout.Empty,
                level: level);
        }
    }
}
