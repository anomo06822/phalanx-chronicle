using System;
using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    [Serializable]
    public sealed class CampaignDefinition
    {
        public CampaignDefinition(
            string campaignId,
            string campaignNameKey,
            string campaignNameFallback,
            string campaignOverviewKey,
            string campaignOverviewFallback,
            IReadOnlyList<CampaignStageDefinition> stages)
        {
            CampaignId = campaignId ?? string.Empty;
            CampaignNameKey = campaignNameKey ?? string.Empty;
            CampaignNameFallback = campaignNameFallback ?? string.Empty;
            CampaignOverviewKey = campaignOverviewKey ?? string.Empty;
            CampaignOverviewFallback = campaignOverviewFallback ?? string.Empty;
            Stages = stages ?? Array.Empty<CampaignStageDefinition>();
        }

        public string CampaignId { get; }

        public string CampaignNameKey { get; }

        public string CampaignNameFallback { get; }

        public string CampaignOverviewKey { get; }

        public string CampaignOverviewFallback { get; }

        public IReadOnlyList<CampaignStageDefinition> Stages { get; }
    }

    [Serializable]
    public sealed class CampaignStageDefinition
    {
        public CampaignStageDefinition(
            string scenarioId,
            string chapterTitleKey,
            string chapterTitleFallback,
            string interludeIntroKey,
            string interludeIntroFallback,
            string interludeOutroKey,
            string interludeOutroFallback)
        {
            ScenarioId = scenarioId ?? string.Empty;
            ChapterTitleKey = chapterTitleKey ?? string.Empty;
            ChapterTitleFallback = chapterTitleFallback ?? string.Empty;
            InterludeIntroKey = interludeIntroKey ?? string.Empty;
            InterludeIntroFallback = interludeIntroFallback ?? string.Empty;
            InterludeOutroKey = interludeOutroKey ?? string.Empty;
            InterludeOutroFallback = interludeOutroFallback ?? string.Empty;
        }

        public string ScenarioId { get; }

        public string ChapterTitleKey { get; }

        public string ChapterTitleFallback { get; }

        public string InterludeIntroKey { get; }

        public string InterludeIntroFallback { get; }

        public string InterludeOutroKey { get; }

        public string InterludeOutroFallback { get; }
    }
}
