using PhalanxChronicle.Core;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace PhalanxChronicle.UI
{
    public sealed class HudFactModel
    {
        public string Label { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public Color AccentColor { get; set; } = Color.clear;
    }

    public sealed class HudChipModel
    {
        public string Text { get; set; } = string.Empty;

        public Color BackgroundColor { get; set; } = new Color(0.16f, 0.14f, 0.11f, 0.95f);

        public Color TextColor { get; set; } = Color.white;
    }

    public enum BattleActionMenuMode
    {
        HoldPosition,
        AfterMove,
    }

    public enum BattleActionDescriptorType
    {
        Attack,
        Skill,
        Wait,
        Back,
    }

    public enum BattleActionDescriptorPriority
    {
        Primary,
        Secondary,
    }

    public enum BattleForecastMode
    {
        Neutral,
        MovePreview,
        ActionPreview,
        ResultConfirm,
    }

    public enum BattleDecisionContextSource
    {
        None,
        Neutral,
        MoveHover,
        AttackHover,
        SkillHover,
        ActionMenu,
    }

    public enum BattleRosterTag
    {
        Ready,
        Done,
        SkillReady,
        Threatening,
        LowHp,
        Exposed,
    }

    public sealed class BattleDecisionContext
    {
        public BattleDecisionContextSource SourceState { get; set; } = BattleDecisionContextSource.None;

        public string ActorUnitId { get; set; } = string.Empty;

        public string TargetUnitId { get; set; } = string.Empty;

        public string Rationale { get; set; } = string.Empty;

        public BattleIntentPreview Preview { get; set; }

        public string SpecialHintText { get; set; } = string.Empty;
    }

    public sealed class BattleHudDecisionContextModel
    {
        public BattleForecastModel ForecastModel { get; set; } = new BattleForecastModel();

        public BattleActionMenuModel ActionMenuModel { get; set; } = new BattleActionMenuModel();

        public BattleDecisionContextSource SourceState { get; set; } = BattleDecisionContextSource.None;

        public string Rationale { get; set; } = string.Empty;
    }

    public sealed class BattleActionDescriptor
    {
        public BattleActionDescriptorType Type { get; set; }

        public string Label { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }

        public string Reason { get; set; } = string.Empty;

        public int ManaCost { get; set; }

        public string Range { get; set; } = string.Empty;

        public string Area { get; set; } = string.Empty;

        public int BestDamage { get; set; }

        public IReadOnlyList<string> PredictedStatuses { get; set; } = new List<string>();

        public string ThreatAfterAction { get; set; } = string.Empty;

        public bool Lethal { get; set; }

        public IReadOnlyList<HudChipModel> MetricChips { get; set; } = new List<HudChipModel>();

        public string OutcomeLine { get; set; } = string.Empty;

        public HudChipModel RiskChip { get; set; }

        public BattleActionDescriptorPriority Priority { get; set; } = BattleActionDescriptorPriority.Secondary;
    }

    public sealed class BattleActionMenuModel
    {
        public BattleActionMenuMode Mode { get; set; }

        public string ModeLabel { get; set; } = string.Empty;

        public string ContextHint { get; set; } = string.Empty;

        public IReadOnlyList<BattleActionDescriptor> Actions { get; set; } = new List<BattleActionDescriptor>();
    }

    public sealed class BattleOverviewModel
    {
        public string HeaderEyebrow { get; set; } = string.Empty;

        public string StageLabel { get; set; } = string.Empty;

        public string SeedLabel { get; set; } = string.Empty;

        public string PhaseLabel { get; set; } = string.Empty;

        public string TurnLabel { get; set; } = string.Empty;

        public string PlayerAliveLabel { get; set; } = string.Empty;

        public string EnemyAliveLabel { get; set; } = string.Empty;

        public string ReadyLabel { get; set; } = string.Empty;

        public string SkillReadyLabel { get; set; } = string.Empty;

        public string ObjectivePrimary { get; set; } = string.Empty;

        public string ObjectiveFailure { get; set; } = string.Empty;

        public string InstructionText { get; set; } = string.Empty;

        public string SecondaryInstructionText { get; set; } = string.Empty;

        public IReadOnlyList<string> SecondaryObjectiveLines { get; set; } = new List<string>();

        public IReadOnlyList<HudFactModel> HeaderFacts { get; set; } = new List<HudFactModel>();

        public IReadOnlyList<HudFactModel> ObjectiveFacts { get; set; } = new List<HudFactModel>();
    }

    public sealed class BattleSelectedUnitModel
    {
        public bool HasSelection { get; set; }

        public string UnitId { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public UnitRole Role { get; set; }

        public string RoleLabel { get; set; } = string.Empty;

        public string IdentitySubtitle { get; set; } = string.Empty;

        public string PositionLabel { get; set; } = string.Empty;

        public string TerrainName { get; set; } = string.Empty;

        public string TerrainEffectSummary { get; set; } = string.Empty;

        public UnitFaction Faction { get; set; }

        public int CurrentHp { get; set; }

        public int MaxHp { get; set; }

        public int CurrentMana { get; set; }

        public int MaxMana { get; set; }

        public int Level { get; set; }

        public int CurrentExp { get; set; }

        public int NextLevelExp { get; set; }

        public int Attack { get; set; }

        public int Defense { get; set; }

        public int MoveRange { get; set; }

        public int AttackRange { get; set; }

        public string WeaponTypeLabel { get; set; } = string.Empty;

        public string WeaponName { get; set; } = string.Empty;

        public string WeaponDescription { get; set; } = string.Empty;

        public string ArmorSummary { get; set; } = string.Empty;

        public Color WeaponAccentColor { get; set; } = Color.white;

        public string PassiveName { get; set; } = string.Empty;

        public string PassiveDescription { get; set; } = string.Empty;

        public string ActiveName { get; set; } = string.Empty;

        public string ActiveDescription { get; set; } = string.Empty;

        public string CooldownLabel { get; set; } = string.Empty;

        public string StatusSummary { get; set; } = string.Empty;

        public string ActionSummary { get; set; } = string.Empty;

        public string ThreatSummary { get; set; } = string.Empty;

        public string ThreatDetail { get; set; } = string.Empty;

        public string ProjectedRiskLabel { get; set; } = string.Empty;

        public string EquipmentSummary { get; set; } = string.Empty;

        public string MountSummary { get; set; } = string.Empty;

        public string MountDeltaLabel { get; set; } = string.Empty;

        public BattleThreatProjection ThreatProjection { get; set; }

        public IReadOnlyList<HudChipModel> IdentityChips { get; set; } = new List<HudChipModel>();

        public IReadOnlyList<HudFactModel> VitalFacts { get; set; } = new List<HudFactModel>();

        public IReadOnlyList<HudFactModel> CombatFacts { get; set; } = new List<HudFactModel>();

        public IReadOnlyList<HudChipModel> StatusPills { get; set; } = new List<HudChipModel>();

        public IReadOnlyList<HudFactModel> IdentityFacts { get; set; } = new List<HudFactModel>();

        public IReadOnlyList<HudFactModel> PrimaryFacts { get; set; } = new List<HudFactModel>();

        public IReadOnlyList<HudChipModel> PrimaryChips { get; set; } = new List<HudChipModel>();

        public HudChipModel ThreatChip { get; set; }

        public string ThreatLine { get; set; } = string.Empty;

        public string DetailHeader { get; set; } = string.Empty;

        public IReadOnlyList<string> DetailLines { get; set; } = new List<string>();
    }

    public sealed class BattleRosterEntryModel
    {
        public string UnitId { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string RoleShortLabel { get; set; } = string.Empty;

        public string PositionLabel { get; set; } = string.Empty;

        public string StatusLabel { get; set; } = string.Empty;

        public string SkillLabel { get; set; } = string.Empty;

        public UnitFaction Faction { get; set; }

        public int CurrentHp { get; set; }

        public int MaxHp { get; set; }

        public bool IsAlive { get; set; }

        public bool HasActed { get; set; }

        public bool CanUseSkill { get; set; }

        public bool IsSelected { get; set; }

        public bool IsThreateningSelection { get; set; }

        public bool IsLowHp { get; set; }

        public bool IsSkillReady { get; set; }

        public bool IsExposed { get; set; }

        public IReadOnlyList<BattleRosterTag> Tags { get; set; } = new List<BattleRosterTag>();

        public BattleRosterTag? PrimaryTag { get; set; }

        public BattleRosterTag? SecondaryTag { get; set; }
    }

    public sealed class BattleForecastModel
    {
        public BattleForecastMode Mode { get; set; } = BattleForecastMode.Neutral;

        public string Header { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string OutcomeSummary { get; set; } = string.Empty;

        public string PrimaryEffect { get; set; } = string.Empty;

        public IReadOnlyList<string> SecondaryEffects { get; set; } = new List<string>();

        public string RiskSummary { get; set; } = string.Empty;

        public string CommitRecommendation { get; set; } = string.Empty;

        public Color AccentColor { get; set; } = Color.white;

        public IReadOnlyList<HudFactModel> OutcomeFacts { get; set; } = new List<HudFactModel>();

        public string PrimaryLine { get; set; } = string.Empty;

        public IReadOnlyList<string> SecondaryLines { get; set; } = new List<string>();

        public HudChipModel RiskChip { get; set; }

        public HudChipModel CommitChip { get; set; }
    }

    public sealed class BattleResultModel
    {
        public string Title { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public IReadOnlyList<string> RewardLines { get; set; } = new List<string>();

        public IReadOnlyList<string> SpecialLines { get; set; } = new List<string>();

        public IReadOnlyList<BattleRewardEntryModel> RewardEntries { get; set; } = new List<BattleRewardEntryModel>();

        public IReadOnlyList<string> UnitLines { get; set; } = new List<string>();
    }

    public sealed class BattleRewardEntryModel
    {
        public string Label { get; set; } = string.Empty;

        public string IconItemId { get; set; } = string.Empty;

        public string AccentRole { get; set; } = string.Empty;

        public bool IsPrimaryReward { get; set; }
    }

    public sealed class CampaignStageSelectModel
    {
        public string Eyebrow { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string ProgressLabel { get; set; } = string.Empty;

        public string HighlightLabel { get; set; } = string.Empty;

        public string DeckTitle { get; set; } = string.Empty;

        public IReadOnlyList<CampaignStageEntryModel> Stages { get; set; } = new List<CampaignStageEntryModel>();
    }

    public sealed class CampaignStageEntryModel
    {
        public int StageIndex { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string BattlefieldLabel { get; set; } = string.Empty;

        public string RewardLabel { get; set; } = string.Empty;

        public string RewardIconItemId { get; set; } = string.Empty;

        public string DurationLabel { get; set; } = string.Empty;

        public bool IsUnlocked { get; set; }

        public bool IsCleared { get; set; }

        public bool IsRecommended { get; set; }

        public string BadgeText { get; set; } = string.Empty;

        public string PriorityBadge { get; set; } = string.Empty;

        public string RecommendedReason { get; set; } = string.Empty;

        public string PreviewThemeId { get; set; } = string.Empty;

        public int SortWeight { get; set; }
    }

    public sealed class CampaignInterludeModel
    {
        public string Eyebrow { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string ProgressLabel { get; set; } = string.Empty;

        public string DeckTitle { get; set; } = string.Empty;

        public string DeckBody { get; set; } = string.Empty;

        public IReadOnlyList<string> DetailLines { get; set; } = new List<string>();

        public string HighlightLine { get; set; } = string.Empty;

        public string TerrainLabel { get; set; } = string.Empty;

        public string RiskLabel { get; set; } = string.Empty;

        public string RewardLabel { get; set; } = string.Empty;

        public string RewardIconItemId { get; set; } = string.Empty;

        public string PrimaryActionLabel { get; set; } = string.Empty;

        public string SecondaryActionLabel { get; set; } = string.Empty;
    }

    public sealed class CampaignOptionListModel
    {
        public string Eyebrow { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string ProgressLabel { get; set; } = string.Empty;

        public string HighlightLabel { get; set; } = string.Empty;

        public string DeckTitle { get; set; } = string.Empty;

        public IReadOnlyList<CampaignOptionEntryModel> Options { get; set; } = new List<CampaignOptionEntryModel>();

        public string PrimaryActionLabel { get; set; } = string.Empty;

        public string SecondaryActionLabel { get; set; } = string.Empty;
    }

    public sealed class CampaignOptionEntryModel
    {
        public string OptionId { get; set; } = string.Empty;

        public string Section { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public IReadOnlyList<string> DetailLines { get; set; } = Array.Empty<string>();

        public string IconGlyph { get; set; } = string.Empty;

        public string IconItemId { get; set; } = string.Empty;

        public string MetricLine { get; set; } = string.Empty;

        public string BadgeText { get; set; } = string.Empty;

        public string RecommendedReason { get; set; } = string.Empty;

        public string AvailabilityReason { get; set; } = string.Empty;

        public int SortWeight { get; set; }

        public bool IsEnabled { get; set; } = true;

        public bool IsEmphasized { get; set; }

        public bool IsPromotionOption { get; set; }

        public PromotionPreviewModel PromotionPreview { get; set; }

        public IReadOnlyList<PromotionComparisonModel> PromotionComparisons { get; set; } = Array.Empty<PromotionComparisonModel>();

        public IReadOnlyList<ProgressionStageIntroModel> StageIntro { get; set; } = Array.Empty<ProgressionStageIntroModel>();

        public bool IsStageIntroExpanded { get; set; }
    }

    public sealed class PromotionPreviewModel
    {
        public string CurrentStageLabel { get; set; } = string.Empty;

        public string NextStageLabel { get; set; } = string.Empty;

        public string PrimarySummary { get; set; } = string.Empty;

        public string SecondarySummary { get; set; } = string.Empty;

        public string ToggleLabel { get; set; } = string.Empty;
    }

    public sealed class PromotionComparisonModel
    {
        public string StatDeltaLabel { get; set; } = string.Empty;

        public string PassiveCurrentName { get; set; } = string.Empty;

        public string PassiveTargetName { get; set; } = string.Empty;

        public string PassiveChangeLabel { get; set; } = string.Empty;

        public string PassiveDetail { get; set; } = string.Empty;

        public string ActiveCurrentName { get; set; } = string.Empty;

        public string ActiveTargetName { get; set; } = string.Empty;

        public string ActiveChangeLabel { get; set; } = string.Empty;

        public string ActiveDetail { get; set; } = string.Empty;

        public string MasteryPreview { get; set; } = string.Empty;
    }

    public sealed class ProgressionStageIntroModel
    {
        public string Title { get; set; } = string.Empty;

        public string LevelRangeLabel { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string UnlocksLabel { get; set; } = string.Empty;

        public string StatusBadge { get; set; } = string.Empty;

        public bool IsReached { get; set; }
    }

    public sealed class CampaignEquipmentDeckModel
    {
        public string Eyebrow { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string ProgressLabel { get; set; } = string.Empty;

        public string HighlightLabel { get; set; } = string.Empty;

        public string DeckTitle { get; set; } = string.Empty;

        public string PreviewMessage { get; set; } = string.Empty;

        public IReadOnlyList<CampaignOptionEntryModel> PromotionOptions { get; set; } = new List<CampaignOptionEntryModel>();

        public IReadOnlyList<CampaignEquipmentSlotCardModel> SlotCards { get; set; } = new List<CampaignEquipmentSlotCardModel>();

        public ItemCategory SelectedSlotCategory { get; set; } = ItemCategory.Weapon;

        public IReadOnlyList<CampaignEquipmentChoiceSectionModel> ChoiceSections { get; set; } = new List<CampaignEquipmentChoiceSectionModel>();

        public string PrimaryActionLabel { get; set; } = string.Empty;

        public string SecondaryActionLabel { get; set; } = string.Empty;
    }

    public sealed class CampaignEquipmentSlotCardModel
    {
        public string OptionId { get; set; } = string.Empty;

        public ItemCategory Category { get; set; } = ItemCategory.Weapon;

        public string SlotLabel { get; set; } = string.Empty;

        public string ItemId { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public string SummaryLine { get; set; } = string.Empty;

        public bool IsTreasure { get; set; }

        public bool IsSelected { get; set; }

        public bool IsEmpty { get; set; }
    }

    public sealed class CampaignEquipmentChoiceSectionModel
    {
        public string Title { get; set; } = string.Empty;

        public IReadOnlyList<CampaignEquipmentChoiceModel> Choices { get; set; } = new List<CampaignEquipmentChoiceModel>();
    }

    public sealed class CampaignEquipmentChoiceModel
    {
        public string OptionId { get; set; } = string.Empty;

        public string ItemId { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public ItemCategory Category { get; set; } = ItemCategory.Weapon;

        public EquipmentChoiceStateKind StateKind { get; set; } = EquipmentChoiceStateKind.Available;

        public string EquippedByUnitId { get; set; } = string.Empty;

        public IReadOnlyList<HudChipModel> Badges { get; set; } = new List<HudChipModel>();

        public string CompareSummary { get; set; } = string.Empty;

        public string EffectSummary { get; set; } = string.Empty;

        public string HintLine { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public bool IsEmphasized { get; set; }

        public int SortWeight { get; set; }
    }

    public sealed class BattleConfirmDialogModel
    {
        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string ConfirmLabel { get; set; } = string.Empty;

        public string CancelLabel { get; set; } = string.Empty;
    }

    public sealed class BattleOnboardingModel
    {
        public string ProgressLabel { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string HintText { get; set; } = string.Empty;

        public string SkipLabel { get; set; } = string.Empty;

        public bool CanSkip { get; set; } = true;
    }
}
