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
            "stage.jiangxia_ferry",
            "stage.luocheng_siege",
            "stage.yangping_pass",
            "scenario.jiangxia_ferry",
            "scenario.luocheng_siege",
            "scenario.yangping_pass",
            "objective.jiangxia.opening",
            "objective.jiangxia.final",
            "objective.jiangxia.failure",
            "objective.luocheng.opening",
            "objective.luocheng.final",
            "objective.luocheng.failure",
            "objective.yangping.opening",
            "objective.yangping.final",
            "objective.yangping.failure",
            "dialogue.jiangxia.opening.1",
            "dialogue.jiangxia.opening.2",
            "dialogue.jiangxia.opening.3",
            "dialogue.jiangxia.mid.1",
            "dialogue.jiangxia.mid.2",
            "dialogue.jiangxia.mid.3",
            "dialogue.jiangxia.victory.1",
            "dialogue.jiangxia.victory.2",
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
            "item.bowang_fire_token.name",
            "item.bowang_fire_token.desc",
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
                Assert.Equal("第六章：雒城攻城戰", LocalizationService.Text("campaign.chapter.6.title"));
                Assert.Equal("第七章：陽平關", LocalizationService.Text("campaign.chapter.7.title"));
                Assert.Equal("第八章：漢水之戰", LocalizationService.Text("campaign.chapter.8.title"));
                Assert.Equal("第九章：定軍山", LocalizationService.Text("campaign.chapter.9.title"));
                Assert.Equal("江夏渡口", LocalizationService.Text("scenario.jiangxia_ferry"));
                Assert.Equal("雒城攻城戰", LocalizationService.Text("stage.luocheng_siege"));
                Assert.Equal("白龍槍", LocalizationService.Text("item.white_dragon_spear.name"));
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
