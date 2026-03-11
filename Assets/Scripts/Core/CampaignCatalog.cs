using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    public static class CampaignCatalog
    {
        public const string LiuBeiLegendCampaignId = "campaign.liu_bei_legend";

        public static CampaignDefinition CreateLiuBeiLegend()
        {
            return new CampaignDefinition(
                LiuBeiLegendCampaignId,
                "campaign.liu_bei_legend.name",
                "Liu Bei Chronicle",
                "campaign.liu_bei_legend.overview",
                "Lead Liu Bei's growing host through six linked Three Kingdoms battles, recruit new heroes along the road, then replay any cleared chapter from the war map.",
                new List<CampaignStageDefinition>
                {
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.GuangzongScenarioId,
                        "campaign.chapter.1.title",
                        "Chapter I: Guangzong",
                        "campaign.chapter.1.intro",
                        "Yellow Turban fires spread through Guangzong. Liu Bei pushes into the breach before the rebel brothers can seal the road.",
                        "campaign.chapter.1.outro",
                        "With Guangzong broken, Liu Bei's name begins to travel beyond the local militia ranks."),
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.BowangpoScenarioId,
                        "campaign.chapter.2.title",
                        "Chapter II: Bowangpo",
                        "campaign.chapter.2.intro",
                        "At Bowangpo, Zhuge Liang draws the pursuing Wei vanguard into a narrow pass and turns the terrain into a weapon.",
                        "campaign.chapter.2.outro",
                        "Bowangpo's trap burns across the hills, and Zhuge Liang formally steps into Liu Bei's war council."),
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.ChangbanScenarioId,
                        "campaign.chapter.3.title",
                        "Chapter III: Changban Rearguard",
                        "campaign.chapter.3.intro",
                        "Cao Cao's pursuit closes at Changban. Liu Bei must hold the crossing long enough for the retreating column to escape.",
                        "campaign.chapter.3.outro",
                        "The Changban rearguard survives, and Zhao Yun joins Liu Bei's banner for the campaigns ahead."),
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.JiamengPassScenarioId,
                        "campaign.chapter.4.title",
                        "Chapter IV: Jiameng Pass",
                        "campaign.chapter.4.intro",
                        "Jiameng Pass becomes a long mountain stand-off. Break the gate line and force the defenders' commandant into the open.",
                        "campaign.chapter.4.outro",
                        "Jiameng Pass opens, and Ma Chao rides in under Liu Bei's colors once the ridge finally breaks."),
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.HanshuiScenarioId,
                        "campaign.chapter.5.title",
                        "Chapter V: Hanshui",
                        "campaign.chapter.5.intro",
                        "Hold the Han camp through the river assault, then counterattack across the Hanshui line with Zhao Yun and Huang Zhong at the front.",
                        "campaign.chapter.5.outro",
                        "Hanshui swings in Liu Bei's favor, setting the table for the decisive strike deeper into Hanzhong."),
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.DingjunScenarioId,
                        "campaign.chapter.6.title",
                        "Chapter VI: Dingjun Mountain",
                        "campaign.chapter.6.intro",
                        "The Hanzhong front demands a decisive blow. Break the Wei forward camp and force Xiahou Yuan into open battle on Dingjun Mountain.",
                        "campaign.chapter.6.outro",
                        "Dingjun Mountain falls. Liu Bei's banner now stands at the edge of a new kingdom."),
                });
        }
    }
}
