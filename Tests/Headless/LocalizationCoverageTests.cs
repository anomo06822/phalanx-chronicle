using System.Collections.Generic;
using System.Reflection;
using PhalanxChronicle.Localization;
using Xunit;

namespace PhalanxChronicle.Headless.Tests
{
    public sealed class LocalizationCoverageTests
    {
        private static readonly string[] RequiredTraditionalChineseKeys =
        {
            "campaign.liu_bei_legend.overview",
            "campaign.chapter.7.title",
            "campaign.chapter.7.intro",
            "campaign.chapter.7.outro",
            "campaign.chapter.8.title",
            "campaign.chapter.8.intro",
            "campaign.chapter.8.outro",
            "campaign.chapter.9.title",
            "campaign.chapter.9.intro",
            "campaign.chapter.9.outro",
            "campaign.chapter.10.title",
            "campaign.chapter.10.intro",
            "campaign.chapter.10.outro",
            "campaign.chapter.11.title",
            "campaign.chapter.11.intro",
            "campaign.chapter.11.outro",
            "campaign.chapter.12.title",
            "campaign.chapter.12.intro",
            "campaign.chapter.12.outro",
            "stage.jiangxia_ferry",
            "stage.baishui_pass_raid",
            "stage.mianzhu_breakthrough",
            "stage.luocheng_siege",
            "stage.yangping_pass",
            "stage.tiandang_raid",
            "scenario.jiangxia_ferry",
            "scenario.baishui_pass_raid",
            "scenario.mianzhu_breakthrough",
            "scenario.luocheng_siege",
            "scenario.yangping_pass",
            "scenario.tiandang_raid",
            "objective.jiangxia.opening",
            "objective.jiangxia.final",
            "objective.jiangxia.failure",
            "objective.baishui.opening",
            "objective.baishui.final",
            "objective.baishui.failure",
            "objective.mianzhu.opening",
            "objective.mianzhu.final",
            "objective.mianzhu.failure",
            "objective.luocheng.opening",
            "objective.luocheng.final",
            "objective.luocheng.failure",
            "objective.yangping.opening",
            "objective.yangping.final",
            "objective.yangping.failure",
            "objective.tiandang.opening",
            "objective.tiandang.final",
            "objective.tiandang.failure",
            "dialogue.jiangxia.opening.1",
            "dialogue.jiangxia.opening.2",
            "dialogue.jiangxia.opening.3",
            "dialogue.jiangxia.mid.1",
            "dialogue.jiangxia.mid.2",
            "dialogue.jiangxia.mid.3",
            "dialogue.jiangxia.victory.1",
            "dialogue.jiangxia.victory.2",
            "dialogue.baishui.opening.1",
            "dialogue.baishui.opening.2",
            "dialogue.baishui.opening.3",
            "dialogue.baishui.mid.1",
            "dialogue.baishui.mid.2",
            "dialogue.baishui.mid.3",
            "dialogue.baishui.victory.1",
            "dialogue.baishui.victory.2",
            "dialogue.baishui.defeat.1",
            "dialogue.mianzhu.opening.1",
            "dialogue.mianzhu.opening.2",
            "dialogue.mianzhu.opening.3",
            "dialogue.mianzhu.mid.1",
            "dialogue.mianzhu.mid.2",
            "dialogue.mianzhu.mid.3",
            "dialogue.mianzhu.victory.1",
            "dialogue.mianzhu.victory.2",
            "dialogue.mianzhu.defeat.1",
            "dialogue.luocheng.opening.1",
            "dialogue.luocheng.opening.2",
            "dialogue.luocheng.opening.3",
            "dialogue.luocheng.mid.1",
            "dialogue.luocheng.mid.2",
            "dialogue.luocheng.mid.3",
            "dialogue.yangping.opening.1",
            "dialogue.yangping.opening.2",
            "dialogue.yangping.opening.3",
            "dialogue.yangping.mid.1",
            "dialogue.yangping.mid.2",
            "dialogue.yangping.mid.3",
            "dialogue.tiandang.opening.1",
            "dialogue.tiandang.opening.2",
            "dialogue.tiandang.opening.3",
            "dialogue.tiandang.mid.secured.1",
            "dialogue.tiandang.mid.secured.2",
            "dialogue.tiandang.mid.alarm.1",
            "dialogue.tiandang.mid.alarm.2",
            "dialogue.tiandang.mid.alarm.3",
            "dialogue.tiandang.victory.1",
            "dialogue.tiandang.victory.2",
            "dialogue.tiandang.defeat.1",
            "item.bowang_fire_token.name",
            "item.bowang_fire_token.desc",
            "item.baishui_signal_spear.name",
            "item.baishui_signal_spear.desc",
            "item.dragon_rider_spear.name",
            "item.dragon_rider_spear.desc",
            "item.hanshui_command_seal.name",
            "item.hanshui_command_seal.desc",
            "item.jiameng_oath_banner.name",
            "item.jiameng_oath_banner.desc",
            "item.jiangxia_river_reins.name",
            "item.jiangxia_river_reins.desc",
            "item.luocheng_breach_hammer.name",
            "item.luocheng_breach_hammer.desc",
            "item.mianzhu_feather_sigil.name",
            "item.mianzhu_feather_sigil.desc",
            "item.raider_scale_vest.name",
            "item.raider_scale_vest.desc",
            "item.raider_war_harness.name",
            "item.raider_war_harness.desc",
            "item.scout_travel_mail.name",
            "item.scout_travel_mail.desc",
            "item.scout_war_cloak.name",
            "item.scout_war_cloak.desc",
            "item.storm_lance.name",
            "item.storm_lance.desc",
            "item.strategist_robe.name",
            "item.strategist_robe.desc",
            "item.western_lance.name",
            "item.western_lance.desc",
            "item.white_dragon_spear.name",
            "item.white_dragon_spear.desc",
            "item.wind_feather_fan.name",
            "item.wind_feather_fan.desc",
            "item.yangping_stone_route.name",
            "item.yangping_stone_route.desc",
            "item.tiandang_falcon_badge.name",
            "item.tiandang_falcon_badge.desc",
            "skill.kings_banner.name",
            "skill.kings_banner.desc",
            "skill.crimson_crescent.name",
            "skill.crimson_crescent.desc",
            "skill.stonewall_challenge.name",
            "skill.stonewall_challenge.desc",
            "skill.feather_formation.name",
            "skill.feather_formation.desc",
            "skill.white_horse_rescue.name",
            "skill.white_horse_rescue.desc",
            "skill.stormbreak_charge.name",
            "skill.stormbreak_charge.desc",
            "skill.dust_devil_sweep.name",
            "skill.dust_devil_sweep.desc",
            "ui.mastery.delta.kings_banner",
            "ui.mastery.delta.crimson_crescent",
            "ui.mastery.delta.stonewall_challenge",
            "ui.mastery.delta.feather_formation",
            "ui.mastery.delta.white_horse_rescue",
            "ui.mastery.delta.stormbreak_charge",
            "ui.mastery.delta.dust_devil_sweep",
            "campaign.stage.theme.baishui",
            "campaign.stage.theme.mianzhu",
            "campaign.stage.theme.tiandang",
            "campaign.stage.risk.baishui",
            "campaign.stage.risk.mianzhu",
            "campaign.stage.risk.tiandang",
        };

        [Fact]
        public void TraditionalChineseTable_CoversExpandedCampaignKeys()
        {
            IReadOnlyDictionary<string, string> table = GetLocaleTable(GameLocale.TraditionalChinese);

            foreach (string key in RequiredTraditionalChineseKeys)
            {
                Assert.True(table.ContainsKey(key), $"Missing zh-TW localization for '{key}'.");
            }
        }

        [Fact]
        public void TraditionalChineseText_UsesUpdatedCampaignAndScenarioLabels()
        {
            GameLocale originalLocale = LocalizationService.CurrentLocale;

            try
            {
                LocalizationService.SetLocale(GameLocale.TraditionalChinese);

                Assert.Equal("第四章：江夏渡口", LocalizationService.Text("campaign.chapter.4.title"));
                Assert.Equal("第五章：葭萌關", LocalizationService.Text("campaign.chapter.5.title"));
                Assert.Equal("第六章：白水關奔襲", LocalizationService.Text("campaign.chapter.6.title"));
                Assert.Equal("第七章：綿竹關破陣", LocalizationService.Text("campaign.chapter.7.title"));
                Assert.Equal("第八章：雒城攻城戰", LocalizationService.Text("campaign.chapter.8.title"));
                Assert.Equal("第九章：陽平關", LocalizationService.Text("campaign.chapter.9.title"));
                Assert.Equal("第十章：天蕩山奇襲", LocalizationService.Text("campaign.chapter.10.title"));
                Assert.Equal("第十一章：漢水之戰", LocalizationService.Text("campaign.chapter.11.title"));
                Assert.Equal("第十二章：定軍山", LocalizationService.Text("campaign.chapter.12.title"));
                Assert.Equal("江夏渡口", LocalizationService.Text("scenario.jiangxia_ferry"));
                Assert.Equal("白水關奔襲", LocalizationService.Text("scenario.baishui_pass_raid"));
                Assert.Equal("綿竹關破陣", LocalizationService.Text("stage.mianzhu_breakthrough"));
                Assert.Equal("雒城攻城戰", LocalizationService.Text("stage.luocheng_siege"));
                Assert.Equal("天蕩山奇襲", LocalizationService.Text("stage.tiandang_raid"));
                Assert.Equal("白龍槍", LocalizationService.Text("item.white_dragon_spear.name"));
                Assert.Equal("王旗號令", LocalizationService.Text("skill.kings_banner.name"));
                Assert.Equal("江夏渡線太寬，硬衝只會白白折兵。先守住中橋，等兩側渡橋被截斷。", LocalizationService.Text("dialogue.jiangxia.opening.1"));
            }
            finally
            {
                LocalizationService.SetLocale(originalLocale);
            }
        }

        private static IReadOnlyDictionary<string, string> GetLocaleTable(GameLocale locale)
        {
            FieldInfo tablesField = typeof(LocalizationService).GetField("Tables", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(tablesField);

            IReadOnlyDictionary<GameLocale, IReadOnlyDictionary<string, string>> tables =
                Assert.IsAssignableFrom<IReadOnlyDictionary<GameLocale, IReadOnlyDictionary<string, string>>>(tablesField.GetValue(null));

            return Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(tables[locale]);
        }
    }
}
