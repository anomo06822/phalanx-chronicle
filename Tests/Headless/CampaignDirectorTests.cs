using PhalanxChronicle.Core;
using Xunit;

namespace PhalanxChronicle.Headless.Tests
{
    public sealed class CampaignDirectorTests
    {
        [Fact]
        public void Campaign_DefaultProgressUnlocksOnlyFirstStage()
        {
            CampaignDirector director = new CampaignDirector(CampaignCatalog.CreateLiuBeiLegend());

            Assert.True(director.IsStageUnlocked(0));
            Assert.False(director.IsStageUnlocked(1));
            Assert.False(director.IsStageUnlocked(2));
            Assert.False(director.IsStageUnlocked(3));
            Assert.False(director.IsStageUnlocked(4));
            Assert.False(director.IsStageUnlocked(5));
            Assert.False(director.IsStageUnlocked(6));
            Assert.False(director.IsStageUnlocked(7));
            Assert.False(director.IsStageUnlocked(8));
            Assert.Equal(0, director.GetRecommendedStageIndex());
        }

        [Fact]
        public void Campaign_PlayerVictoryUnlocksNextStageAndKeepsClearedStagesReplayable()
        {
            CampaignDirector director = new CampaignDirector(CampaignCatalog.CreateLiuBeiLegend());

            director.RecordBattleResult(new BattleResultSummary(
                BattleScenarioCatalog.GuangzongScenarioId,
                TurnSide.Player,
                3,
                new[] { "player-liu-bei", "player-guan-yu" }));

            Assert.True(director.IsStageCleared(0));
            Assert.True(director.IsStageUnlocked(0));
            Assert.True(director.IsStageUnlocked(1));
            Assert.False(director.IsStageUnlocked(2));
            Assert.False(director.IsStageUnlocked(3));
            Assert.False(director.IsStageUnlocked(8));
            Assert.Equal(1, director.GetRecommendedStageIndex());
            Assert.Equal(1, director.Progress.GetClearCount(BattleScenarioCatalog.GuangzongScenarioId));
        }

        [Fact]
        public void Campaign_ReplayVictoriesIncrementScenarioClearCountWithoutUnlockRegression()
        {
            CampaignDirector director = new CampaignDirector(CampaignCatalog.CreateLiuBeiLegend());

            director.RecordBattleResult(new BattleResultSummary(
                BattleScenarioCatalog.GuangzongScenarioId,
                TurnSide.Player,
                3,
                new[] { "player-liu-bei" }));
            director.RecordBattleResult(new BattleResultSummary(
                BattleScenarioCatalog.GuangzongScenarioId,
                TurnSide.Player,
                2,
                new[] { "player-liu-bei" }));

            Assert.True(director.IsStageUnlocked(1));
            Assert.False(director.IsStageUnlocked(2));
            Assert.Equal(2, director.Progress.GetClearCount(BattleScenarioCatalog.GuangzongScenarioId));
        }

        [Fact]
        public void Campaign_DefeatDoesNotUnlockAdditionalStages()
        {
            CampaignDirector director = new CampaignDirector(CampaignCatalog.CreateLiuBeiLegend());

            director.RecordBattleResult(new BattleResultSummary(
                BattleScenarioCatalog.GuangzongScenarioId,
                TurnSide.Enemy,
                2,
                new string[0]));

            Assert.False(director.IsStageCleared(0));
            Assert.True(director.IsStageUnlocked(0));
            Assert.False(director.IsStageUnlocked(1));
            Assert.False(director.IsStageUnlocked(8));
            Assert.Equal(0, director.GetRecommendedStageIndex());
        }

        [Fact]
        public void Campaign_ClaimedRewards_DoNotAffectUnlockOrReplayAccess()
        {
            CampaignProgress progress = new CampaignProgress();
            CampaignDirector director = new CampaignDirector(CampaignCatalog.CreateLiuBeiLegend(), progress);

            director.RecordBattleResult(new BattleResultSummary(
                BattleScenarioCatalog.GuangzongScenarioId,
                TurnSide.Player,
                3,
                new[] { "player-liu-bei", "player-guan-yu" }));
            progress.MarkRewardClaimed(BattleScenarioCatalog.GuangzongScenarioId);

            Assert.True(director.IsStageCleared(0));
            Assert.True(director.IsStageUnlocked(0));
            Assert.True(director.IsStageUnlocked(1));
            Assert.True(progress.IsRewardClaimed(BattleScenarioCatalog.GuangzongScenarioId));
        }

        [Fact]
        public void Campaign_ClearingAllNineStagesUnlocksFullWarMap()
        {
            CampaignDirector director = new CampaignDirector(CampaignCatalog.CreateLiuBeiLegend());

            director.RecordBattleResult(new BattleResultSummary(BattleScenarioCatalog.GuangzongScenarioId, TurnSide.Player, 3, new[] { "player-liu-bei" }));
            director.RecordBattleResult(new BattleResultSummary(BattleScenarioCatalog.BowangpoScenarioId, TurnSide.Player, 3, new[] { "player-liu-bei" }));
            director.RecordBattleResult(new BattleResultSummary(BattleScenarioCatalog.ChangbanScenarioId, TurnSide.Player, 4, new[] { "player-liu-bei" }));
            director.RecordBattleResult(new BattleResultSummary(BattleScenarioCatalog.JiangxiaScenarioId, TurnSide.Player, 4, new[] { "player-liu-bei" }));
            director.RecordBattleResult(new BattleResultSummary(BattleScenarioCatalog.JiamengPassScenarioId, TurnSide.Player, 5, new[] { "player-liu-bei" }));
            director.RecordBattleResult(new BattleResultSummary(BattleScenarioCatalog.LuochengScenarioId, TurnSide.Player, 5, new[] { "player-liu-bei" }));
            director.RecordBattleResult(new BattleResultSummary(BattleScenarioCatalog.YangpingScenarioId, TurnSide.Player, 5, new[] { "player-liu-bei" }));
            director.RecordBattleResult(new BattleResultSummary(BattleScenarioCatalog.HanshuiScenarioId, TurnSide.Player, 6, new[] { "player-liu-bei" }));
            director.RecordBattleResult(new BattleResultSummary(BattleScenarioCatalog.DingjunScenarioId, TurnSide.Player, 7, new[] { "player-liu-bei" }));

            Assert.Equal(8, director.Progress.UnlockedStageIndex);
            Assert.True(director.IsStageUnlocked(8));
            Assert.True(director.IsStageCleared(8));
            Assert.Equal(8, director.GetRecommendedStageIndex());
        }
    }
}
