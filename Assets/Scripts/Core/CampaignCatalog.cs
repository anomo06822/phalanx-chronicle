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
                "Lead Liu Bei's growing host through twelve linked Three Kingdoms battles, recruit new heroes along the road, claim legendary treasures, then replay cleared chapters at rising difficulty.",
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
                        BattleScenarioCatalog.JiangxiaScenarioId,
                        "campaign.chapter.4.title",
                        "Chapter IV: Jiangxia Ferry",
                        "campaign.chapter.4.intro",
                        "Jiangxia's crossing line spreads across three bridges. Sever the outer spans and crush the ferry defense through the middle lane.",
                        "campaign.chapter.4.outro",
                        "With Jiangxia's bridges broken and the center held, Liu Bei's road south no longer bends to the river."),
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.JiamengPassScenarioId,
                        "campaign.chapter.5.title",
                        "Chapter V: Jiameng Pass",
                        "campaign.chapter.5.intro",
                        "Jiameng Pass becomes a long mountain stand-off. Break the gate line and force the defenders' commandant into the open.",
                        "campaign.chapter.5.outro",
                        "Jiameng Pass opens, and Ma Chao rides in under Liu Bei's colors once the ridge finally breaks."),
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.BaishuiScenarioId,
                        "campaign.chapter.6.title",
                        "Chapter VI: Baishui Pass Raid",
                        "campaign.chapter.6.intro",
                        "Baishui Pass offers two crossings, but only one will remain. Break the outer line before the lower bridge is cut away behind you.",
                        "campaign.chapter.6.outro",
                        "Baishui breaks at speed, proving Liu Bei's vanguard can survive even when the road collapses mid-battle."),
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.MianzhuScenarioId,
                        "campaign.chapter.7.title",
                        "Chapter VII: Mianzhu Breakthrough",
                        "campaign.chapter.7.intro",
                        "Mianzhu's front line is layered behind a heavy gate. Break the first defense, then force the breach before the center lane turns into a fire trap.",
                        "campaign.chapter.7.outro",
                        "Mianzhu's gate falls, and the inland road opens one hard wall wider for Liu Bei's push."),
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.LuochengScenarioId,
                        "campaign.chapter.8.title",
                        "Chapter VIII: Luocheng Siege",
                        "campaign.chapter.8.intro",
                        "Luocheng's outer gate must fall before the city can be taken. Break the wall line, then drive through the breach into the inner street.",
                        "campaign.chapter.8.outro",
                        "Luocheng's breach widens Liu Bei's hold inland and adds a true city fight to the campaign ledger."),
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.YangpingScenarioId,
                        "campaign.chapter.9.title",
                        "Chapter IX: Yangping Pass",
                        "campaign.chapter.9.intro",
                        "Yangping Pass twists through two mountain lanes. When the slopes shift, seize the surviving flank before the defenders can reset.",
                        "campaign.chapter.9.outro",
                        "Yangping's rockfall changes the route, and Liu Bei's vanguard learns to turn the mountain's own violence into momentum."),
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.TiandangScenarioId,
                        "campaign.chapter.10.title",
                        "Chapter X: Tiandang Raid",
                        "campaign.chapter.10.intro",
                        "Tiandang's ridge is held together by signal beacons. Silence both towers before the camp can turn the whole slope against you.",
                        "campaign.chapter.10.outro",
                        "Tiandang's night raid strips the defenders of timing and clears the way for a heavier blow along the Hanzhong front."),
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.HanshuiScenarioId,
                        "campaign.chapter.11.title",
                        "Chapter XI: Hanshui",
                        "campaign.chapter.11.intro",
                        "Hold the Han camp through the river assault, then counterattack across the Hanshui line with Zhao Yun and Huang Zhong at the front.",
                        "campaign.chapter.11.outro",
                        "Hanshui swings in Liu Bei's favor, setting the table for the decisive strike deeper into Hanzhong."),
                    new CampaignStageDefinition(
                        BattleScenarioCatalog.DingjunScenarioId,
                        "campaign.chapter.12.title",
                        "Chapter XII: Dingjun Mountain",
                        "campaign.chapter.12.intro",
                        "The Hanzhong front demands a decisive blow. Break the Wei forward camp and force Xiahou Yuan into open battle on Dingjun Mountain.",
                        "campaign.chapter.12.outro",
                        "Dingjun Mountain falls. Liu Bei's banner now stands at the edge of a new kingdom."),
                });
        }
    }
}
