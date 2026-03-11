using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhalanxChronicle.Core;
using UnityEngine;

namespace PhalanxChronicle.Battle
{
    public sealed class CampaignSaveRepository
    {
        private readonly CampaignProgressionService progressionService;
        private readonly string savePath;

        public CampaignSaveRepository(CampaignProgressionService progressionService, string fileName = "phalanx-chronicle-save.json")
        {
            this.progressionService = progressionService ?? throw new ArgumentNullException(nameof(progressionService));
            savePath = Path.Combine(Application.persistentDataPath, fileName);
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
                if (dto == null || dto.version != 1 || (!string.IsNullOrWhiteSpace(expectedCampaignId) && dto.campaignId != expectedCampaignId))
                {
                    return false;
                }

                saveData = ToModel(dto);
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
                        dto.progress.lastBattleResult.survivingUnitIds ?? new List<string>())
                    : null,
                dto.progress != null ? dto.progress.claimedRewardScenarioIds : Array.Empty<string>());

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
                    new EquipmentLoadout(unit.weaponId, unit.armorId),
                    unit.level,
                    unit.currentExp,
                    new BondState(unit.supportLevel, unit.sharedBattles),
                    unit.hasPromoted))
                .ToList()
                : new List<CampaignUnitState>();

            return new CampaignSaveData(dto.campaignId, progress, inventory, units, dto.version);
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
                    lastBattleResult = saveData.Progress.LastBattleResult == null
                        ? null
                        : new BattleResultSummaryDto
                        {
                            scenarioId = saveData.Progress.LastBattleResult.ScenarioId,
                            winningSide = (int)saveData.Progress.LastBattleResult.WinningSide,
                            roundCount = saveData.Progress.LastBattleResult.RoundCount,
                            survivingUnitIds = saveData.Progress.LastBattleResult.SurvivingUnitIds.ToList(),
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
            public BattleResultSummaryDto lastBattleResult;
        }

        [Serializable]
        private sealed class BattleResultSummaryDto
        {
            public string scenarioId;
            public int winningSide;
            public int roundCount;
            public List<string> survivingUnitIds;
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
            public int level;
            public int currentExp;
            public int supportLevel;
            public int sharedBattles;
            public bool hasPromoted;
        }
    }
}
