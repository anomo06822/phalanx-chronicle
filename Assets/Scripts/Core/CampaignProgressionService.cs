using System;
using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Localization;

namespace PhalanxChronicle.Core
{
    public sealed class CampaignBattleResolution
    {
        public CampaignBattleResolution(
            bool grantedStageReward,
            int grantedSupplies,
            int grantedRenown,
            string grantedItemId,
            IReadOnlyList<string> recruitedUnitIds = null,
            IReadOnlyList<string> grantedBonusItemIds = null,
            IReadOnlyList<string> grantedBonusRewardLines = null)
        {
            GrantedStageReward = grantedStageReward;
            GrantedSupplies = grantedSupplies < 0 ? 0 : grantedSupplies;
            GrantedRenown = grantedRenown < 0 ? 0 : grantedRenown;
            GrantedItemId = grantedItemId ?? string.Empty;
            RecruitedUnitIds = recruitedUnitIds ?? Array.Empty<string>();
            GrantedBonusItemIds = grantedBonusItemIds ?? Array.Empty<string>();
            GrantedBonusRewardLines = grantedBonusRewardLines ?? Array.Empty<string>();
        }

        public bool GrantedStageReward { get; }

        public int GrantedSupplies { get; }

        public int GrantedRenown { get; }

        public string GrantedItemId { get; }

        public IReadOnlyList<string> RecruitedUnitIds { get; }

        public IReadOnlyList<string> GrantedBonusItemIds { get; }

        public IReadOnlyList<string> GrantedBonusRewardLines { get; }
    }

    public sealed class CampaignProgressionService
    {
        private readonly CampaignRosterBuilder rosterBuilder;

        public CampaignProgressionService(CampaignRosterBuilder rosterBuilder = null)
        {
            this.rosterBuilder = rosterBuilder ?? new CampaignRosterBuilder();
        }

        public CampaignSaveData CreateNewSave(CampaignDefinition definition)
        {
            return rosterBuilder.CreateNewSave(definition);
        }

        public bool NormalizeSave(CampaignSaveData saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            HashSet<string> unitIdsBefore = saveData.Units
                .Select(unit => unit.UnitId)
                .ToHashSet(StringComparer.Ordinal);
            Dictionary<string, ActiveSkillType> activeSkillsBefore = saveData.Units
                .ToDictionary(unit => unit.UnitId, unit => unit.ActiveSkill, StringComparer.Ordinal);

            bool normalized = CampaignSaveNormalizer.Normalize(saveData);
            BackfillMissingRewardRecruits(saveData);
            bool backfilledUnlockProgress = BackfillUnlockProgress(saveData);

            return normalized ||
                   backfilledUnlockProgress ||
                   !unitIdsBefore.SetEquals(saveData.Units.Select(unit => unit.UnitId)) ||
                   saveData.Units.Any(unit => activeSkillsBefore.TryGetValue(unit.UnitId, out ActiveSkillType skill) && skill != unit.ActiveSkill);
        }

        public BattleScenarioData PrepareScenario(BattleScenarioData scenario, CampaignSaveData saveData)
        {
            if (scenario == null || saveData == null)
            {
                return scenario;
            }

            NormalizeSave(saveData);
            bool includeStageReward = !saveData.Progress.IsRewardClaimed(scenario.ScenarioId);
            BattleScenarioData preparedScenario = rosterBuilder.BuildScenario(scenario, saveData, includeStageReward);
            int replayTier = CalculateReplayTier(preparedScenario, saveData);
            return ApplyReplayDifficulty(preparedScenario, replayTier);
        }

        public CampaignBattleResolution FinalizeBattle(
            CampaignSaveData saveData,
            BattleScenarioData scenario,
            BattleResultSummary summary,
            IReadOnlyList<UnitRuntimeState> playerUnits)
        {
            if (saveData == null || summary == null)
            {
                return new CampaignBattleResolution(false, 0, 0, string.Empty);
            }

            NormalizeSave(saveData);
            saveData.Progress.SetLastBattleResult(summary);

            bool grantedStageReward = summary.WinningSide == TurnSide.Player &&
                                      scenario != null &&
                                      scenario.RewardBundle != null &&
                                      scenario.RewardBundle.HasAnyReward &&
                                      !saveData.Progress.IsRewardClaimed(summary.ScenarioId);
            IReadOnlyList<string> recruitedUnitIds = Array.Empty<string>();
            List<string> grantedBonusItemIds = new List<string>();
            List<string> grantedBonusRewardLines = new List<string>();
            if (grantedStageReward)
            {
                saveData.Inventory.AddSupplies(scenario.RewardBundle.Supplies);
                saveData.Inventory.AddRenown(scenario.RewardBundle.Renown);
                if (!string.IsNullOrWhiteSpace(scenario.RewardBundle.RewardItemId))
                {
                    saveData.Inventory.AddItem(scenario.RewardBundle.RewardItemId);
                }

                recruitedUnitIds = rosterBuilder.AddRecruitsIfMissing(saveData, scenario.RewardBundle);
                saveData.Progress.MarkRewardClaimed(summary.ScenarioId);
            }

            if (summary.WinningSide == TurnSide.Player && scenario?.BonusRewards != null)
            {
                foreach (BonusRewardDefinition bonusReward in scenario.BonusRewards.Where(reward => reward != null))
                {
                    if (string.IsNullOrWhiteSpace(bonusReward.RewardId) ||
                        saveData.Progress.IsBonusRewardClaimed(bonusReward.RewardId) ||
                        !bonusReward.IsSatisfied(summary))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(bonusReward.RewardItemId))
                    {
                        saveData.Inventory.AddItem(bonusReward.RewardItemId);
                        grantedBonusItemIds.Add(bonusReward.RewardItemId);
                        ItemDefinition itemDefinition = ItemCatalog.Get(bonusReward.RewardItemId);
                        grantedBonusRewardLines.Add(LocalizationService.Format(
                            "campaign.reward.bonus_item",
                            "條件寶物：{0}",
                            itemDefinition != null
                                ? LocalizationService.Text(itemDefinition.NameKey, itemDefinition.NameFallback)
                                : bonusReward.RewardItemId));
                    }

                    saveData.Progress.MarkBonusRewardClaimed(bonusReward.RewardId);
                }
            }

            if (playerUnits != null)
            {
                foreach (UnitRuntimeState runtimeState in playerUnits.Where(unit => unit != null && unit.Faction == UnitFaction.Player))
                {
                    CampaignUnitState unitState = saveData.GetUnit(runtimeState.Id);
                    if (unitState == null)
                    {
                        unitState = AddPersistentUnitFromBattle(saveData, runtimeState);
                    }

                    if (unitState == null)
                    {
                        continue;
                    }

                    ItemDefinition weapon = ItemCatalog.Get(unitState.EquipmentLoadout.WeaponId);
                    ItemDefinition armor = ItemCatalog.Get(unitState.EquipmentLoadout.ArmorId);
                    unitState.SyncFromBattle(
                        runtimeState,
                        GetHpBonus(weapon) + GetHpBonus(armor),
                        GetAttackBonus(weapon) + GetAttackBonus(armor),
                        GetDefenseBonus(weapon) + GetDefenseBonus(armor));
                }
            }

            return new CampaignBattleResolution(
                grantedStageReward,
                grantedStageReward && scenario != null ? scenario.RewardBundle.Supplies : 0,
                grantedStageReward && scenario != null ? scenario.RewardBundle.Renown : 0,
                grantedStageReward && scenario != null ? scenario.RewardBundle.RewardItemId : string.Empty,
                recruitedUnitIds,
                grantedBonusItemIds,
                grantedBonusRewardLines);
        }

        public IReadOnlyList<ShopOfferDefinition> GetShopOffers()
        {
            return ShopCatalog.All;
        }

        public IReadOnlyList<ItemDefinition> GetEquippableItems(CampaignSaveData saveData, string unitId, ItemCategory category)
        {
            CampaignUnitState unitState = saveData != null ? saveData.GetUnit(unitId) : null;
            if (unitState == null)
            {
                return Array.Empty<ItemDefinition>();
            }

            List<ItemDefinition> definitions = new List<ItemDefinition>();
            foreach (InventoryItemEntry entry in saveData.Inventory.Entries)
            {
                ItemDefinition definition = ItemCatalog.Get(entry.ItemId);
                if (definition == null || definition.Category != category || !definition.CanEquip(unitState.Role))
                {
                    continue;
                }

                if (GetAvailableEquipmentCount(saveData, entry.ItemId, unitId) > 0 || IsCurrentlyEquipped(unitState, entry.ItemId))
                {
                    definitions.Add(definition);
                }
            }

            return definitions.OrderBy(item => item.ItemId, StringComparer.Ordinal).ToList();
        }

        public IReadOnlyList<EquipmentChoiceDefinition> GetEquipmentChoices(CampaignSaveData saveData, string unitId, ItemCategory category)
        {
            CampaignUnitState unitState = saveData != null ? saveData.GetUnit(unitId) : null;
            if (unitState == null)
            {
                return Array.Empty<EquipmentChoiceDefinition>();
            }

            string currentItemId = GetEquippedItemId(unitState, category);
            List<EquipmentChoiceDefinition> choices = new List<EquipmentChoiceDefinition>();
            if (!string.IsNullOrWhiteSpace(currentItemId))
            {
                choices.Add(new EquipmentChoiceDefinition(currentItemId, category, EquipmentChoiceStateKind.Current));
            }

            foreach (InventoryItemEntry entry in saveData.Inventory.Entries)
            {
                ItemDefinition definition = ItemCatalog.Get(entry.ItemId);
                if (definition == null ||
                    definition.Category != category ||
                    !definition.CanEquip(unitState.Role) ||
                    string.Equals(entry.ItemId, currentItemId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (GetAvailableEquipmentCount(saveData, entry.ItemId, unitId) > 0)
                {
                    choices.Add(new EquipmentChoiceDefinition(entry.ItemId, category, EquipmentChoiceStateKind.Available));
                }
            }

            foreach (CampaignUnitState otherUnit in saveData.Units.Where(unit => unit.UnitId != unitId))
            {
                string equippedItemId = GetEquippedItemId(otherUnit, category);
                if (string.IsNullOrWhiteSpace(equippedItemId) ||
                    string.Equals(equippedItemId, currentItemId, StringComparison.Ordinal))
                {
                    continue;
                }

                ItemDefinition definition = ItemCatalog.Get(equippedItemId);
                if (definition == null ||
                    !definition.CanEquip(unitState.Role) ||
                    GetAvailableEquipmentCount(saveData, equippedItemId, unitId) > 0)
                {
                    continue;
                }

                choices.Add(new EquipmentChoiceDefinition(
                    equippedItemId,
                    category,
                    EquipmentChoiceStateKind.EquippedByOther,
                    otherUnit.UnitId));
            }

            return choices
                .OrderBy(choice => choice.StateKind)
                .ThenByDescending(choice =>
                {
                    ItemDefinition definition = ItemCatalog.Get(choice.ItemId);
                    return definition != null &&
                           string.Equals(definition.RecommendedOwnerUnitId, unitId, StringComparison.Ordinal)
                        ? 1
                        : 0;
                })
                .ThenByDescending(choice =>
                {
                    ItemDefinition definition = ItemCatalog.Get(choice.ItemId);
                    return definition != null && definition.IsTreasure ? 1 : 0;
                })
                .ThenBy(choice =>
                {
                    ItemDefinition definition = ItemCatalog.Get(choice.ItemId);
                    return definition != null ? definition.ItemId : choice.ItemId;
                }, StringComparer.Ordinal)
                .ThenBy(choice => choice.EquippedByUnitId, StringComparer.Ordinal)
                .ToList();
        }

        public int GetEquippedItemCount(CampaignSaveData saveData, string itemId)
        {
            if (saveData == null || string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            return saveData.Units.Count(unit =>
                string.Equals(unit.EquipmentLoadout.WeaponId, itemId, StringComparison.Ordinal) ||
                string.Equals(unit.EquipmentLoadout.ArmorId, itemId, StringComparison.Ordinal) ||
                string.Equals(unit.EquipmentLoadout.MountId, itemId, StringComparison.Ordinal));
        }

        public IReadOnlyList<string> GetUnitsEquippingItem(CampaignSaveData saveData, string itemId, ItemCategory? category = null)
        {
            if (saveData == null || string.IsNullOrWhiteSpace(itemId))
            {
                return Array.Empty<string>();
            }

            return saveData.Units
                .Where(unit => IsItemEquippedInCategory(unit, itemId, category))
                .Select(unit => unit.UnitId)
                .ToList();
        }

        public int GetAvailableEquipmentCount(CampaignSaveData saveData, string itemId, string unitIdToIgnore = null)
        {
            if (saveData == null || string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            int equippedCopies = saveData.Units.Count(unit =>
                unit.UnitId != unitIdToIgnore &&
                (string.Equals(unit.EquipmentLoadout.WeaponId, itemId, StringComparison.Ordinal) ||
                 string.Equals(unit.EquipmentLoadout.ArmorId, itemId, StringComparison.Ordinal) ||
                 string.Equals(unit.EquipmentLoadout.MountId, itemId, StringComparison.Ordinal)));
            return Math.Max(0, saveData.Inventory.GetQuantity(itemId) - equippedCopies);
        }

        public bool TryPurchaseItem(CampaignSaveData saveData, string itemId)
        {
            if (saveData == null)
            {
                return false;
            }

            ShopOfferDefinition offer = ShopCatalog.Get(itemId);
            if (offer == null || saveData.Inventory.Renown < offer.RequiredRenown)
            {
                return false;
            }

            if (!saveData.Inventory.TrySpendSupplies(offer.SuppliesCost))
            {
                return false;
            }

            saveData.Inventory.AddItem(offer.ItemId);
            return true;
        }

        public bool TryEquipWeapon(CampaignSaveData saveData, string unitId, string itemId)
        {
            return TryEquipItem(saveData, unitId, itemId, ItemCategory.Weapon);
        }

        public bool TryEquipArmor(CampaignSaveData saveData, string unitId, string itemId)
        {
            return TryEquipItem(saveData, unitId, itemId, ItemCategory.Armor);
        }

        public bool TryEquipMount(CampaignSaveData saveData, string unitId, string itemId)
        {
            return TryEquipItem(saveData, unitId, itemId, ItemCategory.Mount);
        }

        public bool TryTransferEquipment(CampaignSaveData saveData, string fromUnitId, string toUnitId, string itemId, ItemCategory category)
        {
            CampaignUnitState fromUnit = saveData != null ? saveData.GetUnit(fromUnitId) : null;
            CampaignUnitState toUnit = saveData != null ? saveData.GetUnit(toUnitId) : null;
            if (fromUnit == null ||
                toUnit == null ||
                string.Equals(fromUnit.UnitId, toUnit.UnitId, StringComparison.Ordinal))
            {
                return false;
            }

            ItemDefinition definition = ItemCatalog.Get(itemId);
            if (definition == null ||
                !definition.IsEquipable ||
                definition.Category != category ||
                !definition.CanEquip(toUnit.Role) ||
                !string.Equals(GetEquippedItemId(fromUnit, category), itemId, StringComparison.Ordinal) ||
                string.Equals(GetEquippedItemId(toUnit, category), itemId, StringComparison.Ordinal))
            {
                return false;
            }

            UnequipItem(fromUnit, category);
            EquipItem(toUnit, itemId, category);
            return true;
        }

        public bool TryUnequipItem(CampaignSaveData saveData, string unitId, ItemCategory category)
        {
            CampaignUnitState unitState = saveData != null ? saveData.GetUnit(unitId) : null;
            if (unitState == null || string.IsNullOrWhiteSpace(GetEquippedItemId(unitState, category)))
            {
                return false;
            }

            UnequipItem(unitState, category);
            return true;
        }

        private bool TryEquipItem(CampaignSaveData saveData, string unitId, string itemId, ItemCategory category)
        {
            CampaignUnitState unitState = saveData != null ? saveData.GetUnit(unitId) : null;
            if (unitState == null)
            {
                return false;
            }

            ItemDefinition definition = ItemCatalog.Get(itemId);
            if (definition == null || !definition.IsEquipable || definition.Category != category || !definition.CanEquip(unitState.Role))
            {
                return false;
            }

            if (!IsCurrentlyEquipped(unitState, itemId) && GetAvailableEquipmentCount(saveData, itemId, unitId) <= 0)
            {
                return false;
            }

            EquipItem(unitState, itemId, category);
            return true;
        }

        public bool TryPromoteUnit(CampaignSaveData saveData, string unitId)
        {
            return TryPromoteUnit(saveData, unitId, null);
        }

        public bool TryPromoteUnit(CampaignSaveData saveData, string unitId, string promotionId)
        {
            CampaignUnitState unitState = saveData != null ? saveData.GetUnit(unitId) : null;
            PromotionDefinition definition = string.IsNullOrWhiteSpace(promotionId)
                ? PromotionCatalog.Get(unitId)
                : PromotionCatalog.GetOptions(unitId).FirstOrDefault(option => string.Equals(option.PromotionId, promotionId, StringComparison.Ordinal));
            if (unitState == null || definition == null || unitState.HasPromoted || unitState.Level < 10)
            {
                return false;
            }

            unitState.ApplyPromotion(definition);
            return true;
        }

        private static bool IsCurrentlyEquipped(CampaignUnitState unitState, string itemId)
        {
            return string.Equals(unitState.EquipmentLoadout.WeaponId, itemId, StringComparison.Ordinal) ||
                   string.Equals(unitState.EquipmentLoadout.ArmorId, itemId, StringComparison.Ordinal) ||
                   string.Equals(unitState.EquipmentLoadout.MountId, itemId, StringComparison.Ordinal);
        }

        private static bool IsItemEquippedInCategory(CampaignUnitState unitState, string itemId, ItemCategory? category)
        {
            if (unitState == null || string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            if (category == null)
            {
                return IsCurrentlyEquipped(unitState, itemId);
            }

            return string.Equals(GetEquippedItemId(unitState, category.Value), itemId, StringComparison.Ordinal);
        }

        private static string GetEquippedItemId(CampaignUnitState unitState, ItemCategory category)
        {
            if (unitState == null)
            {
                return string.Empty;
            }

            switch (category)
            {
                case ItemCategory.Weapon:
                    return unitState.EquipmentLoadout.WeaponId;
                case ItemCategory.Armor:
                    return unitState.EquipmentLoadout.ArmorId;
                case ItemCategory.Mount:
                    return unitState.EquipmentLoadout.MountId;
                default:
                    return string.Empty;
            }
        }

        private static void EquipItem(CampaignUnitState unitState, string itemId, ItemCategory category)
        {
            if (unitState == null)
            {
                return;
            }

            switch (category)
            {
                case ItemCategory.Weapon:
                    unitState.EquipWeapon(itemId);
                    break;
                case ItemCategory.Armor:
                    unitState.EquipArmor(itemId);
                    break;
                case ItemCategory.Mount:
                    unitState.EquipMount(itemId);
                    break;
            }
        }

        private static void UnequipItem(CampaignUnitState unitState, ItemCategory category)
        {
            EquipItem(unitState, string.Empty, category);
        }

        private static int GetAttackBonus(ItemDefinition definition)
        {
            return definition != null ? definition.AttackBonus : 0;
        }

        private static int GetDefenseBonus(ItemDefinition definition)
        {
            return definition != null ? definition.DefenseBonus : 0;
        }

        private static int GetHpBonus(ItemDefinition definition)
        {
            return definition != null ? definition.HpBonus : 0;
        }

        private static int CalculateReplayTier(BattleScenarioData scenario, CampaignSaveData saveData)
        {
            if (scenario == null || saveData == null)
            {
                return 0;
            }

            int clearCountTier = Math.Max(0, saveData.Progress.GetClearCount(scenario.ScenarioId) - 1);
            int averageLevel = saveData.Units.Count == 0
                ? 1
                : (int)Math.Floor(saveData.Units.Average(unit => unit.Level));
            int levelTier = Math.Max(0, (averageLevel - scenario.RecommendedLevel) / 2);
            return Math.Min(5, clearCountTier + levelTier);
        }

        private static bool BackfillUnlockProgress(CampaignSaveData saveData)
        {
            if (saveData == null)
            {
                return false;
            }

            CampaignDefinition definition = ResolveCampaignDefinition(saveData.CampaignId);
            if (definition == null || definition.Stages == null || definition.Stages.Count == 0)
            {
                return false;
            }

            int highestUnlockedStageIndex = 0;
            for (int stageIndex = 0; stageIndex < definition.Stages.Count; stageIndex++)
            {
                CampaignStageDefinition stage = definition.Stages[stageIndex];
                if (stage == null || !saveData.Progress.IsCleared(stage.ScenarioId))
                {
                    continue;
                }

                highestUnlockedStageIndex = Math.Max(
                    highestUnlockedStageIndex,
                    Math.Min(stageIndex + 1, definition.Stages.Count - 1));
            }

            if (highestUnlockedStageIndex <= saveData.Progress.UnlockedStageIndex)
            {
                return false;
            }

            saveData.Progress.UnlockThrough(highestUnlockedStageIndex);
            return true;
        }

        private static CampaignDefinition ResolveCampaignDefinition(string campaignId)
        {
            return string.Equals(campaignId, CampaignCatalog.LiuBeiLegendCampaignId, StringComparison.Ordinal)
                ? CampaignCatalog.CreateLiuBeiLegend()
                : null;
        }

        private static BattleScenarioData ApplyReplayDifficulty(BattleScenarioData scenario, int replayTier)
        {
            if (scenario == null)
            {
                return null;
            }

            string variantTag = replayTier <= 0 ? "Normal" : "Replay " + ToRomanNumeral(replayTier);
            if (replayTier <= 0)
            {
                return new BattleScenarioData(
                    scenario.ScenarioId,
                    scenario.ScenarioName,
                    scenario.ScenarioNameKey,
                    scenario.Stage,
                    scenario.Triggers,
                    scenario.RecommendedLevel,
                    scenario.VictoryExpReward,
                    scenario.DefeatExpReward,
                    scenario.RewardBundle,
                    scenario.BonusRewards,
                    scenario.DuelScenes,
                    0,
                    variantTag);
            }

            StageDefinitionData scaledStage = new StageDefinitionData(
                scenario.Stage.StageName,
                scenario.Stage.StageNameKey,
                scenario.Stage.Width,
                scenario.Stage.Height,
                scenario.Stage.UnitSpawns.Select(spawn => ScaleSpawn(spawn, replayTier)).ToList(),
                scenario.Stage.BlockedCells.ToList(),
                scenario.Stage.TerrainTiles.ToList(),
                scenario.Stage.IsRandomMap,
                scenario.Stage.MapSeed);

            List<ScenarioTrigger> triggers = scenario.Triggers
                .Select(trigger => ScaleTrigger(trigger, replayTier))
                .ToList();

            return new BattleScenarioData(
                scenario.ScenarioId,
                scenario.ScenarioName,
                scenario.ScenarioNameKey,
                scaledStage,
                triggers,
                scenario.RecommendedLevel,
                scenario.VictoryExpReward,
                scenario.DefeatExpReward,
                scenario.RewardBundle,
                scenario.BonusRewards,
                scenario.DuelScenes,
                replayTier,
                variantTag);
        }

        private static ScenarioTrigger ScaleTrigger(ScenarioTrigger trigger, int replayTier)
        {
            if (trigger == null)
            {
                return null;
            }

            int? minimumRoundNumber = trigger.MinimumRoundNumber;
            if (replayTier >= 2 &&
                minimumRoundNumber.HasValue &&
                trigger.Directives.Any(directive =>
                    directive != null &&
                    (directive.Type == ScenarioDirectiveType.SpawnUnits || directive.Type == ScenarioDirectiveType.ApplyBattlefieldMutation)))
            {
                minimumRoundNumber = Math.Max(1, minimumRoundNumber.Value - 1);
            }

            return new ScenarioTrigger(
                trigger.Id,
                trigger.Checkpoint,
                trigger.Directives.Select(directive => ScaleDirective(directive, replayTier)).ToList(),
                trigger.RequiredDefeatedUnitIds.ToList(),
                trigger.RequiredAliveUnitIds.ToList(),
                trigger.RequiredFlags.ToList(),
                trigger.ExcludedFlags.ToList(),
                minimumRoundNumber,
                trigger.RequiresBattleEnded,
                trigger.RequiredWinningSide,
                trigger.ExclusivityGroupId,
                trigger.FireOnce);
        }

        private static ScenarioDirective ScaleDirective(ScenarioDirective directive, int replayTier)
        {
            if (directive == null)
            {
                return null;
            }

            switch (directive.Type)
            {
                case ScenarioDirectiveType.SpawnUnits:
                    return ScenarioDirective.SpawnUnits(directive.UnitSpawns.Select(spawn => ScaleSpawn(spawn, replayTier)).ToList());
                case ScenarioDirectiveType.QueueDialogue:
                    return ScenarioDirective.QueueDialogue(directive.DialogueLines);
                case ScenarioDirectiveType.UpdateObjective:
                    return ScenarioDirective.UpdateObjective(directive.ObjectiveState);
                case ScenarioDirectiveType.SetBattleOutcome:
                    return directive.WinningSide.HasValue
                        ? ScenarioDirective.SetBattleOutcome(directive.WinningSide.Value)
                        : null;
                case ScenarioDirectiveType.SetFlag:
                    return ScenarioDirective.SetFlag(directive.FlagName);
                case ScenarioDirectiveType.ApplyBattlefieldMutation:
                    return ScenarioDirective.ApplyBattlefieldMutation(directive.BattlefieldMutation);
                default:
                    return directive;
            }
        }

        private static UnitSpawnData ScaleSpawn(UnitSpawnData spawn, int replayTier)
        {
            if (spawn == null || spawn.Definition == null || spawn.Definition.Faction != UnitFaction.Enemy || replayTier <= 0)
            {
                return spawn;
            }

            UnitDefinitionData scaledDefinition = ScaleEnemyDefinition(spawn.Definition, replayTier);
            return new UnitSpawnData(scaledDefinition, spawn.StartPosition);
        }

        private static UnitDefinitionData ScaleEnemyDefinition(UnitDefinitionData definition, int replayTier)
        {
            int bonusHp = replayTier * 2;
            int bonusAttack = IsEliteEnemy(definition) ? replayTier : 0;
            int bonusDefense = replayTier / 2;
            ActiveSkillType activeSkill = replayTier >= 4 ? UpgradeBossSkill(definition.ActiveSkill, definition.AiProfile) : definition.ActiveSkill;
            (string nameKey, string descKey) = GetSkillMetadata(activeSkill, definition.ActiveSkillNameKey, definition.ActiveSkillDescriptionKey);

            return new UnitDefinitionData(
                definition.Id,
                definition.DisplayName,
                definition.DisplayNameKey,
                definition.Faction,
                definition.Role,
                definition.RoleNameKey,
                definition.PassiveSkill,
                definition.PassiveSkillNameKey,
                definition.PassiveSkillDescriptionKey,
                activeSkill,
                nameKey,
                descKey,
                definition.MaxHp + bonusHp,
                definition.Attack + bonusAttack,
                definition.Defense + bonusDefense,
                definition.MoveRange,
                definition.AttackRange,
                definition.MaxMana,
                definition.ClassId,
                definition.GrowthProfileId,
                definition.AiProfile,
                definition.EquipmentLoadout,
                definition.StartingLevel,
                definition.StartingExp,
                definition.BondState,
                definition.ProgressionResolved);
        }

        private static bool IsEliteEnemy(UnitDefinitionData definition)
        {
            return definition != null &&
                   (definition.AiProfile == AiProfileType.Boss || definition.Role == UnitRole.Commander);
        }

        private static ActiveSkillType UpgradeBossSkill(ActiveSkillType skillType, AiProfileType aiProfile)
        {
            if (aiProfile != AiProfileType.Boss)
            {
                return skillType;
            }

            switch (skillType)
            {
                case ActiveSkillType.PowerStrike:
                    return ActiveSkillType.AzureDragonSlash;
                case ActiveSkillType.Volley:
                    return ActiveSkillType.SkyVolley;
                case ActiveSkillType.WarCry:
                    return ActiveSkillType.LionWarCry;
                case ActiveSkillType.FireStratagem:
                    return ActiveSkillType.EightTrigramInferno;
                default:
                    return skillType;
            }
        }

        private static (string NameKey, string DescriptionKey) GetSkillMetadata(
            ActiveSkillType skillType,
            string fallbackNameKey,
            string fallbackDescriptionKey)
        {
            switch (skillType)
            {
                case ActiveSkillType.AzureDragonSlash:
                    return ("skill.azure_dragon_slash.name", "skill.azure_dragon_slash.desc");
                case ActiveSkillType.SkyVolley:
                    return ("skill.sky_volley.name", "skill.sky_volley.desc");
                case ActiveSkillType.LionWarCry:
                    return ("skill.lion_war_cry.name", "skill.lion_war_cry.desc");
                case ActiveSkillType.EightTrigramInferno:
                    return ("skill.eight_trigram_inferno.name", "skill.eight_trigram_inferno.desc");
                case ActiveSkillType.DragonPierce:
                    return ("skill.dragon_pierce.name", "skill.dragon_pierce.desc");
                case ActiveSkillType.WesternStampede:
                    return ("skill.western_stampede.name", "skill.western_stampede.desc");
                case ActiveSkillType.KingsBanner:
                    return ("skill.kings_banner.name", "skill.kings_banner.desc");
                case ActiveSkillType.CrimsonCrescent:
                    return ("skill.crimson_crescent.name", "skill.crimson_crescent.desc");
                case ActiveSkillType.StonewallChallenge:
                    return ("skill.stonewall_challenge.name", "skill.stonewall_challenge.desc");
                case ActiveSkillType.FeatherFormation:
                    return ("skill.feather_formation.name", "skill.feather_formation.desc");
                case ActiveSkillType.WhiteHorseRescue:
                    return ("skill.white_horse_rescue.name", "skill.white_horse_rescue.desc");
                case ActiveSkillType.StormbreakCharge:
                    return ("skill.stormbreak_charge.name", "skill.stormbreak_charge.desc");
                case ActiveSkillType.DustDevilSweep:
                    return ("skill.dust_devil_sweep.name", "skill.dust_devil_sweep.desc");
                default:
                    return (fallbackNameKey, fallbackDescriptionKey);
            }
        }

        private static string ToRomanNumeral(int value)
        {
            switch (value)
            {
                case 1:
                    return "I";
                case 2:
                    return "II";
                case 3:
                    return "III";
                case 4:
                    return "IV";
                case 5:
                    return "V";
                default:
                    return value.ToString();
            }
        }

        private void BackfillMissingRewardRecruits(CampaignSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            foreach (string scenarioId in saveData.Progress.ClaimedRewardScenarioIds
                         .Concat(saveData.Progress.ClearedScenarioIds)
                         .Where(id => !string.IsNullOrWhiteSpace(id))
                         .Distinct(StringComparer.Ordinal))
            {
                BattleScenarioData scenario = BattleScenarioCatalog.CreateScenario(scenarioId);
                if (scenario?.RewardBundle?.RecruitUnitIds == null || scenario.RewardBundle.RecruitUnitIds.Count == 0)
                {
                    continue;
                }

                rosterBuilder.AddRecruitsIfMissing(saveData, scenario.RewardBundle);
            }
        }

        private static CampaignUnitState AddPersistentUnitFromBattle(CampaignSaveData saveData, UnitRuntimeState runtimeState)
        {
            if (saveData == null ||
                runtimeState == null ||
                runtimeState.Faction != UnitFaction.Player ||
                string.IsNullOrWhiteSpace(runtimeState.Id) ||
                !runtimeState.Id.StartsWith("player-", StringComparison.Ordinal))
            {
                return null;
            }

            CampaignUnitState unitState = new CampaignUnitState(
                runtimeState.Id,
                runtimeState.DisplayName,
                runtimeState.DisplayNameKey,
                runtimeState.Role,
                runtimeState.RoleNameKey,
                runtimeState.PassiveSkill,
                runtimeState.PassiveSkillNameKey,
                runtimeState.PassiveSkillDescriptionKey,
                runtimeState.ActiveSkill,
                runtimeState.ActiveSkillNameKey,
                runtimeState.ActiveSkillDescriptionKey,
                runtimeState.MaxHp,
                runtimeState.Attack,
                runtimeState.Defense,
                runtimeState.MoveRange,
                runtimeState.AttackRange,
                runtimeState.MaxMana,
                runtimeState.ClassId,
                runtimeState.GrowthProfileId,
                runtimeState.AiProfile,
                runtimeState.EquipmentLoadout,
                runtimeState.Level,
                runtimeState.CurrentExp,
                new BondState(runtimeState.BondState.SupportLevel, runtimeState.BondState.SharedBattles),
                HasPromoted(runtimeState.Id, runtimeState.ClassId));
            if (!saveData.TryAddUnit(unitState))
            {
                return saveData.GetUnit(runtimeState.Id);
            }

            AddLoadoutToInventory(saveData.Inventory, runtimeState.EquipmentLoadout);
            return unitState;
        }

        private static void AddLoadoutToInventory(CampaignInventoryState inventory, EquipmentLoadout loadout)
        {
            if (inventory == null || loadout == null)
            {
                return;
            }

            inventory.AddItem(loadout.WeaponId);
            inventory.AddItem(loadout.ArmorId);
            inventory.AddItem(loadout.MountId);
        }

        private static bool HasPromoted(string unitId, string classId)
        {
            return PromotionCatalog.GetOptions(unitId)
                .Any(option => string.Equals(option.TargetClassId, classId, StringComparison.Ordinal));
        }
    }
}
