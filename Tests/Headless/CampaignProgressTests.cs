using PhalanxChronicle.Core;
using Xunit;

namespace PhalanxChronicle.Headless.Tests
{
    public sealed class CampaignProgressTests
    {
        [Fact]
        public void NewProgress_DefaultsFirstSessionFlagsToFalse()
        {
            CampaignProgress progress = new CampaignProgress();

            Assert.False(progress.HasSeenFirstLaunchIntro);
            Assert.False(progress.HasCompletedFirstBattleOnboarding);
            Assert.False(progress.HasSkippedOnboarding);
        }

        [Fact]
        public void FirstSessionMarkers_UpdateProgressFlags()
        {
            CampaignProgress progress = new CampaignProgress();

            progress.MarkFirstLaunchIntroSeen();
            progress.MarkFirstBattleOnboardingCompleted();
            progress.MarkFirstBattleOnboardingSkipped();

            Assert.True(progress.HasSeenFirstLaunchIntro);
            Assert.True(progress.HasCompletedFirstBattleOnboarding);
            Assert.True(progress.HasSkippedOnboarding);
        }

        [Fact]
        public void CampaignSaveData_DefaultsToVersionFive()
        {
            CampaignSaveData save = new CampaignSaveData("test-campaign", null, null, null);

            Assert.Equal(5, save.Version);
        }
    }
}
