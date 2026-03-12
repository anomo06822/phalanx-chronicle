using PhalanxChronicle.Core;
using UnityEngine;
using System.Collections.Generic;

namespace PhalanxChronicle.UI
{
    public enum BattleActionMenuMode
    {
        HoldPosition,
        AfterMove,
    }

    public sealed class BattleActionMenuModel
    {
        public BattleActionMenuMode Mode { get; set; }

        public string ModeLabel { get; set; } = string.Empty;

        public bool CanAttack { get; set; }

        public string AttackDetail { get; set; } = string.Empty;

        public string SkillName { get; set; } = string.Empty;

        public bool CanUseSkill { get; set; }

        public string SkillDetail { get; set; } = string.Empty;

        public bool CanWait { get; set; } = true;

        public string WaitDetail { get; set; } = string.Empty;

        public bool CanBack { get; set; }

        public string BackLabel { get; set; } = string.Empty;

        public string BackDetail { get; set; } = string.Empty;
    }

    public sealed class BattleOverviewModel
    {
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
    }

    public sealed class BattleSelectedUnitModel
    {
        public bool HasSelection { get; set; }

        public string UnitId { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public UnitRole Role { get; set; }

        public string RoleLabel { get; set; } = string.Empty;

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
    }

    public sealed class BattleForecastModel
    {
        public string Header { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string Detail { get; set; } = string.Empty;

        public string Footer { get; set; } = string.Empty;

        public Color AccentColor { get; set; } = Color.white;
    }

    public sealed class CampaignStageSelectModel
    {
        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public IReadOnlyList<CampaignStageEntryModel> Stages { get; set; } = new List<CampaignStageEntryModel>();
    }

    public sealed class CampaignStageEntryModel
    {
        public int StageIndex { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsUnlocked { get; set; }

        public bool IsCleared { get; set; }

        public bool IsRecommended { get; set; }
    }

    public sealed class CampaignInterludeModel
    {
        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string PrimaryActionLabel { get; set; } = string.Empty;

        public string SecondaryActionLabel { get; set; } = string.Empty;
    }

    public sealed class CampaignOptionListModel
    {
        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public IReadOnlyList<CampaignOptionEntryModel> Options { get; set; } = new List<CampaignOptionEntryModel>();

        public string PrimaryActionLabel { get; set; } = string.Empty;

        public string SecondaryActionLabel { get; set; } = string.Empty;
    }

    public sealed class CampaignOptionEntryModel
    {
        public string OptionId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public bool IsEmphasized { get; set; }

        public bool IsPromotionOption { get; set; }
    }
}
