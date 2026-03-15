using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhalanxChronicle.Core;
using UnityEngine;

namespace PhalanxChronicle.Battle
{
    public sealed class CampaignSaveSlotSummary
    {
        public CampaignSaveSlotSummary(int slotIndex, string filePath, bool hasSave, CampaignSaveData saveData)
        {
            SlotIndex = slotIndex;
            FilePath = filePath ?? string.Empty;
            HasSave = hasSave;
            SaveData = saveData;
        }

        public int SlotIndex { get; }

        public string FilePath { get; }

        public bool HasSave { get; }

        public CampaignSaveData SaveData { get; }
    }

    public sealed class CampaignSaveRepository
    {
        public const int SlotCount = 3;
        public const string LegacyFileName = "phalanx-chronicle-save.json";

        private const string SlotFileNameFormat = "phalanx-chronicle-save-slot{0}.json";

        private readonly CampaignProgressionService progressionService;
        private readonly string savePath;

        public CampaignSaveRepository(CampaignProgressionService progressionService, string fileName = LegacyFileName)
            : this(progressionService, fileName, null)
        {
        }

        private CampaignSaveRepository(CampaignProgressionService progressionService, string fileName, string persistentDataPath)
        {
            this.progressionService = progressionService ?? throw new ArgumentNullException(nameof(progressionService));
            savePath = Path.Combine(ResolvePersistentDataPath(persistentDataPath), fileName);
        }

        public string SavePath => savePath;

        public static CampaignSaveRepository CreateForSlot(CampaignProgressionService progressionService, int slotIndex)
        {
            return new CampaignSaveRepository(progressionService, GetSlotFileName(slotIndex));
        }

        public static IReadOnlyList<CampaignSaveSlotSummary> GetSlotSummaries(
            CampaignProgressionService progressionService,
            string expectedCampaignId)
        {
            if (progressionService == null)
            {
                throw new ArgumentNullException(nameof(progressionService));
            }

            List<CampaignSaveSlotSummary> summaries = new List<CampaignSaveSlotSummary>(SlotCount);
            for (int slotIndex = 1; slotIndex <= SlotCount; slotIndex++)
            {
                CampaignSaveRepository repository = CreateForSlot(progressionService, slotIndex);
                bool hasSave = repository.TryLoad(expectedCampaignId, out CampaignSaveData saveData);
                summaries.Add(new CampaignSaveSlotSummary(slotIndex, repository.SavePath, hasSave, saveData));
            }

            return summaries;
        }

        public static bool TryMigrateLegacySaveToSlotOne()
        {
            string legacyPath = Path.Combine(ResolvePersistentDataPath(null), LegacyFileName);
            string slotOnePath = GetSlotSavePath(1);
            if (!File.Exists(legacyPath) || File.Exists(slotOnePath))
            {
                return false;
            }

            try
            {
                string directoryPath = Path.GetDirectoryName(slotOnePath);
                if (!string.IsNullOrWhiteSpace(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.Move(legacyPath, slotOnePath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to migrate legacy campaign save from {legacyPath} to {slotOnePath}: {exception.Message}");
                return false;
            }
        }

        public static string GetSlotFileName(int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Slot index must be between 1 and {SlotCount}.");
            }

            return string.Format(SlotFileNameFormat, slotIndex);
        }

        public static string GetSlotSavePath(int slotIndex)
        {
            return Path.Combine(ResolvePersistentDataPath(null), GetSlotFileName(slotIndex));
        }

        public bool TryLoad(string expectedCampaignId, out CampaignSaveData saveData)
        {
            saveData = null;
            if (!File.Exists(savePath))
            {
                return false;
            }

            try
            {
                CampaignSaveFileDto dto = JsonUtility.FromJson<CampaignSaveFileDto>(File.ReadAllText(savePath));
                if (dto == null ||
                    (dto.version != 1 && dto.version != 2 && dto.version != 3 && dto.version != 4 && dto.version != 5) ||
                    (!string.IsNullOrWhiteSpace(expectedCampaignId) && dto.campaignId != expectedCampaignId))
                {
                    return false;
                }

                saveData = ToModel(dto);
                bool normalized = progressionService.NormalizeSave(saveData);
                if (normalized || dto.version < 5)
                {
                    Save(saveData);
                }

                return saveData != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load campaign save from {savePath}: {exception.Message}");
                return false;
            }
        }

        public CampaignSaveData CreateNew(CampaignDefinition definition)
        {
            CampaignSaveData saveData = progressionService.CreateNewSave(definition);
            Save(saveData);
            return saveData;
        }

        public void Save(CampaignSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            try
            {
                string directoryPath = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrWhiteSpace(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllText(savePath, JsonUtility.ToJson(ToDto(saveData), true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to save campaign progress to {savePath}: {exception.Message}");
            }
        }

        public void Clear()
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        }

        private static string ResolvePersistentDataPath(string persistentDataPath)
        {
            return string.IsNullOrWhiteSpace(persistentDataPath)
                ? Application.persistentDataPath
                : persistentDataPath;
        }

        private static CampaignSaveData ToModel(CampaignSaveFileDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            CampaignProgress progress = new CampaignProgress(
                dto.progress != null ? dto.progress.unlockedStageIndex : 0,
                dto.progress != null ? dto.progress.clearedScenarioIds : Array.Empty<string>(),
                dto.progress != null && dto.progress.lastBattleResult != null
                    ? new BattleResultSummary(
                        dto.progress.lastBattleResult.scenarioId,
                        (TurnSide)dto.progress.lastBattleResult.winningSide,
                        dto.progress.lastBattleResult.roundCount,
                        dto.progress.lastBattleResult.survivingUnitIds ?? new List<string>(),
                        dto.progress.lastBattleResult.achievedScenarioFlags ?? new List<string>(),
                        dto.progress.lastBattleResult.triggeredDuelIds ?? new List<string>())
                    : null,
                dto.progress != null ? dto.progress.claimedRewardScenarioIds : Array.Empty<string>(),
                dto.progress != null ? dto.progress.claimedBonusRewardIds : Array.Empty<string>(),
                dto.progress != null && dto.progress.scenarioClearCounts != null
                    ? dto.progress.scenarioClearCounts
                        .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.scenarioId))
                        .GroupBy(entry => entry.scenarioId)
                        .ToDictionary(group => group.Key, group => group.Max(entry => entry.clearCount))
                    : null,
                dto.progress != null && dto.progress.hasSeenFirstLaunchIntro,
                dto.progress != null && dto.progress.hasCompletedFirstBattleOnboarding,
                dto.progress != null && dto.progress.hasSkippedOnboarding);

            CampaignInventoryState inventory = new CampaignInventoryState(
                dto.inventory != null ? dto.inventory.supplies : 0,
                dto.inventory != null ? dto.inventory.renown : 0,
                dto.inventory != null && dto.inventory.items != null
                    ? dto.inventory.items.Select(item => new InventoryItemEntry(item.itemId, item.quantity)).ToList()
                    : new List<InventoryItemEntry>());

            List<CampaignUnitState> units = dto.units != null
                ? dto.units.Select(unit => new CampaignUnitState(
                    unit.unitId,
                    unit.displayName,
                    unit.displayNameKey,
                    (UnitRole)unit.role,
                    unit.roleNameKey,
                    (PassiveSkillType)unit.passiveSkill,
                    unit.passiveSkillNameKey,
                    unit.passiveSkillDescriptionKey,
                    (ActiveSkillType)unit.activeSkill,
                    unit.activeSkillNameKey,
                    unit.activeSkillDescriptionKey,
                    unit.maxHp,
                    unit.attack,
                    unit.defense,
                    unit.moveRange,
                    unit.attackRange,
                    unit.maxMana,
                    unit.classId,
                    unit.growthProfileId,
                    (AiProfileType)unit.aiProfile,
                    new EquipmentLoadout(unit.weaponId, unit.armorId, unit.mountId),
                    unit.level,
                    unit.currentExp,
                    new BondState(unit.supportLevel, unit.sharedBattles),
                    unit.hasPromoted))
                .ToList()
                : new List<CampaignUnitState>();

            return new CampaignSaveData(dto.campaignId, progress, inventory, units, dto.version < 5 ? 5 : dto.version);
        }

        private static CampaignSaveFileDto ToDto(CampaignSaveData saveData)
        {
            return new CampaignSaveFileDto
            {
                version = saveData.Version,
                campaignId = saveData.CampaignId,
                progress = new CampaignProgressDto
                {
                    unlockedStageIndex = saveData.Progress.UnlockedStageIndex,
                    clearedScenarioIds = saveData.Progress.ClearedScenarioIds.ToList(),
                    claimedRewardScenarioIds = saveData.Progress.ClaimedRewardScenarioIds.ToList(),
                    claimedBonusRewardIds = saveData.Progress.ClaimedBonusRewardIds.ToList(),
                    scenarioClearCounts = saveData.Progress.ScenarioClearCounts
                        .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                        .Select(entry => new ScenarioClearCountDto
                        {
                            scenarioId = entry.Key,
                            clearCount = entry.Value,
                        })
                        .ToList(),
                    hasSeenFirstLaunchIntro = saveData.Progress.HasSeenFirstLaunchIntro,
                    hasCompletedFirstBattleOnboarding = saveData.Progress.HasCompletedFirstBattleOnboarding,
                    hasSkippedOnboarding = saveData.Progress.HasSkippedOnboarding,
                    lastBattleResult = saveData.Progress.LastBattleResult == null
                        ? null
                        : new BattleResultSummaryDto
                        {
                            scenarioId = saveData.Progress.LastBattleResult.ScenarioId,
                            winningSide = (int)saveData.Progress.LastBattleResult.WinningSide,
                            roundCount = saveData.Progress.LastBattleResult.RoundCount,
                            survivingUnitIds = saveData.Progress.LastBattleResult.SurvivingUnitIds.ToList(),
                            achievedScenarioFlags = saveData.Progress.LastBattleResult.AchievedScenarioFlags.ToList(),
                            triggeredDuelIds = saveData.Progress.LastBattleResult.TriggeredDuelIds.ToList(),
                        },
                },
                inventory = new CampaignInventoryDto
                {
                    supplies = saveData.Inventory.Supplies,
                    renown = saveData.Inventory.Renown,
                    items = saveData.Inventory.Entries
                        .Select(item => new InventoryItemEntryDto { itemId = item.ItemId, quantity = item.Quantity })
                        .ToList(),
                },
                units = saveData.Units
                    .Select(unit => new CampaignUnitDto
                    {
                        unitId = unit.UnitId,
                        displayName = unit.DisplayName,
                        displayNameKey = unit.DisplayNameKey,
                        role = (int)unit.Role,
                        roleNameKey = unit.RoleNameKey,
                        passiveSkill = (int)unit.PassiveSkill,
                        passiveSkillNameKey = unit.PassiveSkillNameKey,
                        passiveSkillDescriptionKey = unit.PassiveSkillDescriptionKey,
                        activeSkill = (int)unit.ActiveSkill,
                        activeSkillNameKey = unit.ActiveSkillNameKey,
                        activeSkillDescriptionKey = unit.ActiveSkillDescriptionKey,
                        maxHp = unit.MaxHp,
                        attack = unit.Attack,
                        defense = unit.Defense,
                        moveRange = unit.MoveRange,
                        attackRange = unit.AttackRange,
                        maxMana = unit.MaxMana,
                        classId = unit.ClassId,
                        growthProfileId = unit.GrowthProfileId,
                        aiProfile = (int)unit.AiProfile,
                        weaponId = unit.EquipmentLoadout.WeaponId,
                        armorId = unit.EquipmentLoadout.ArmorId,
                        mountId = unit.EquipmentLoadout.MountId,
                        level = unit.Level,
                        currentExp = unit.CurrentExp,
                        supportLevel = unit.BondState.SupportLevel,
                        sharedBattles = unit.BondState.SharedBattles,
                        hasPromoted = unit.HasPromoted,
                    })
                    .ToList(),
            };
        }

        [Serializable]
        private sealed class CampaignSaveFileDto
        {
            public int version;
            public string campaignId;
            public CampaignProgressDto progress;
            public CampaignInventoryDto inventory;
            public List<CampaignUnitDto> units;
        }

        [Serializable]
        private sealed class CampaignProgressDto
        {
            public int unlockedStageIndex;
            public List<string> clearedScenarioIds;
            public List<string> claimedRewardScenarioIds;
            public List<string> claimedBonusRewardIds;
            public List<ScenarioClearCountDto> scenarioClearCounts;
            public bool hasSeenFirstLaunchIntro;
            public bool hasCompletedFirstBattleOnboarding;
            public bool hasSkippedOnboarding;
            public BattleResultSummaryDto lastBattleResult;
        }

        [Serializable]
        private sealed class ScenarioClearCountDto
        {
            public string scenarioId;
            public int clearCount;
        }

        [Serializable]
        private sealed class BattleResultSummaryDto
        {
            public string scenarioId;
            public int winningSide;
            public int roundCount;
            public List<string> survivingUnitIds;
            public List<string> achievedScenarioFlags;
            public List<string> triggeredDuelIds;
        }

        [Serializable]
        private sealed class CampaignInventoryDto
        {
            public int supplies;
            public int renown;
            public List<InventoryItemEntryDto> items;
        }

        [Serializable]
        private sealed class InventoryItemEntryDto
        {
            public string itemId;
            public int quantity;
        }

        [Serializable]
        private sealed class CampaignUnitDto
        {
            public string unitId;
            public string displayName;
            public string displayNameKey;
            public int role;
            public string roleNameKey;
            public int passiveSkill;
            public string passiveSkillNameKey;
            public string passiveSkillDescriptionKey;
            public int activeSkill;
            public string activeSkillNameKey;
            public string activeSkillDescriptionKey;
            public int maxHp;
            public int attack;
            public int defense;
            public int moveRange;
            public int attackRange;
            public int maxMana;
            public string classId;
            public string growthProfileId;
            public int aiProfile;
            public string weaponId;
            public string armorId;
            public string mountId;
            public int level;
            public int currentExp;
            public int supportLevel;
            public int sharedBattles;
            public bool hasPromoted;
        }
    }
}
