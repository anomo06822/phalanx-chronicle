using PhalanxChronicle.Battle;
using Xunit;

namespace PhalanxChronicle.Headless.Tests
{
    public sealed class AppVersionComparerTests
    {
        [Theory]
        [InlineData("v0.1.1", "0.1.1", 0)]
        [InlineData("0.1.2", "0.1.1", 1)]
        [InlineData("0.2.0", "0.1.9", 1)]
        [InlineData("0.1.1", "0.1.2", -1)]
        public void Compare_HandlesSemverStyleTags(string left, string right, int expectedSign)
        {
            int result = AppVersionComparer.Compare(left, right);

            Assert.Equal(expectedSign, result == 0 ? 0 : result > 0 ? 1 : -1);
        }

        [Theory]
        [InlineData("", "0.1.1", false)]
        [InlineData("preview", "0.1.1", false)]
        [InlineData("v0.1.1-beta", "0.1.1", true)]
        [InlineData("1", "1.0.0", true)]
        [InlineData("1.2", "1.2.0", true)]
        public void Normalize_OnlyReturnsValuesForSupportedVersions(string version, string expected, bool hasValue)
        {
            string normalized = AppVersionComparer.Normalize(version);

            if (!hasValue)
            {
                Assert.Equal(string.Empty, normalized);
                return;
            }

            Assert.Equal(expected, normalized);
        }

        [Theory]
        [InlineData("0.1.1", "v0.1.2", "", true)]
        [InlineData("0.1.1", "0.1.1", "", false)]
        [InlineData("0.1.1", "preview", "", false)]
        [InlineData("0.1.1", "", "", false)]
        [InlineData("0.1.1", "v0.1.2", "0.1.2", false)]
        [InlineData("0.1.1", "v0.1.2", "0.1.1", true)]
        public void ShouldPrompt_AppliesDismissedTagRules(string currentVersion, string latestTag, string dismissedTag, bool expected)
        {
            bool shouldPrompt = AppVersionComparer.ShouldPrompt(currentVersion, latestTag, dismissedTag);

            Assert.Equal(expected, shouldPrompt);
        }
    }
}
