using System;
using System.Linq;
using PhalanxChronicle.Core;
using Xunit;

namespace PhalanxChronicle.Headless.Tests
{
    public sealed class BonusRewardAndDuelTests
    {
        [Fact]
        public void ScenarioCatalog_WiresBonusRewardsAndDuelsToConfiguredBattles()
        {
            BattleScenarioData guangzong = BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.GuangzongScenarioId);
            BattleScenarioData changban = BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.ChangbanScenarioId);
            BattleScenarioData jiameng = BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.JiamengPassScenarioId);
            BattleScenarioData hanshui = BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.HanshuiScenarioId);

            Assert.Contains(guangzong.BonusRewards, reward => reward.RewardItemId == "guangzong-rally-seal");
            Assert.Empty(guangzong.DuelScenes);

            Assert.Contains(changban.BonusRewards, reward => reward.RewardItemId == "changban-white-plume");
            Assert.Contains(changban.DuelScenes, duel => duel.DuelId == "duel.changban.zhao_yun");
            Assert.Contains(changban.Stage.UnitSpawns, spawn => spawn.Definition.Id == "player-zhao-yun");

            Assert.Contains(jiameng.BonusRewards, reward => reward.RewardItemId == "jiameng-iron-girth");
            Assert.Contains(jiameng.DuelScenes, duel => duel.DuelId == "duel.jiameng.ma_chao");
            Assert.Contains(jiameng.Stage.UnitSpawns, spawn => spawn.Definition.Id == "player-ma-chao");

            Assert.DoesNotContain(hanshui.BonusRewards, reward => reward != null);
            Assert.Contains(hanshui.DuelScenes, duel => duel.DuelId == "duel.hanshui.huang_zhong");
        }

        [Fact]
        public void FinalizeBattle_MissedBonusRewardCanBeClaimedOnReplayOnlyOnce()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignSaveData save = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());
            BattleScenarioData firstClearScenario = service.PrepareScenario(
                BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.ChangbanScenarioId),
                save);
            BattleSimulation firstClearSimulation = new BattleSimulation(firstClearScenario.Stage);
            string[] survivors = firstClearSimulation.Context.GetUnits(UnitFaction.Player, false).Select(unit => unit.Id).ToArray();

            BattleResultSummary missedSummary = new BattleResultSummary(
                BattleScenarioCatalog.ChangbanScenarioId,
                TurnSide.Player,
                5,
                survivors);

            CampaignBattleResolution firstResolution = service.FinalizeBattle(
                save,
                firstClearScenario,
                missedSummary,
                firstClearSimulation.Context.GetUnits(UnitFaction.Player, false));

            Assert.True(firstResolution.GrantedStageReward);
            Assert.Empty(firstResolution.GrantedBonusItemIds);
            Assert.False(save.Progress.IsBonusRewardClaimed("bonus.changban.white_plume"));
            Assert.Equal(0, save.Inventory.GetQuantity("changban-white-plume"));

            BattleScenarioData replayScenario = service.PrepareScenario(
                BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.ChangbanScenarioId),
                save);
            BattleSimulation replaySimulation = new BattleSimulation(replayScenario.Stage);
            BattleResultSummary replaySummary = new BattleResultSummary(
                BattleScenarioCatalog.ChangbanScenarioId,
                TurnSide.Player,
                6,
                replaySimulation.Context.GetUnits(UnitFaction.Player, false).Select(unit => unit.Id).ToArray(),
                Array.Empty<string>(),
                new[] { "duel.changban.zhao_yun" });

            CampaignBattleResolution secondResolution = service.FinalizeBattle(
                save,
                replayScenario,
                replaySummary,
                replaySimulation.Context.GetUnits(UnitFaction.Player, false));
            CampaignBattleResolution thirdResolution = service.FinalizeBattle(
                save,
                replayScenario,
                replaySummary,
                replaySimulation.Context.GetUnits(UnitFaction.Player, false));

            Assert.Contains("changban-white-plume", secondResolution.GrantedBonusItemIds);
            Assert.NotEmpty(secondResolution.GrantedBonusRewardLines);
            Assert.True(save.Progress.IsBonusRewardClaimed("bonus.changban.white_plume"));
            Assert.Equal(1, save.Inventory.GetQuantity("changban-white-plume"));
            Assert.Empty(thirdResolution.GrantedBonusItemIds);
            Assert.Equal(1, save.Inventory.GetQuantity("changban-white-plume"));
        }

        [Fact]
        public void ScenarioDirector_MatchesDuelOnlyWhenSceneConditionsAreMet()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.ChangbanScenarioId);
            ScenarioDirector director = new ScenarioDirector(scenario);
            BattleContext context = new BattleContext(scenario.Stage);
            UnitRuntimeState defender = context.GetUnit("enemy-pursuit_commander");

            Assert.Null(director.TryMatchDuel("player-zhao-yun", "enemy-pursuit_commander", context));

            context.AdvanceRound();
            context.AdvanceRound();
            defender.ApplyDamage(16);

            DuelSceneDefinition matched = director.TryMatchDuel("player-zhao-yun", "enemy-pursuit_commander", context);

            Assert.NotNull(matched);
            Assert.Equal("duel.changban.zhao_yun", matched.DuelId);
            Assert.Null(director.TryMatchDuel("player-liu-bei", "enemy-pursuit_commander", context));
        }
    }
}
