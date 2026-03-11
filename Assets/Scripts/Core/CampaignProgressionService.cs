using System;
using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class CampaignBattleResolution
    {
        public CampaignBattleResolution(
            bool grantedStageReward,
            int grantedSupplies,
            int grantedRenown,
            string grantedItemId,
            IReadOnlyList<string> recruitedUnitIds = null)
        {
            GrantedStageReward = grantedStageReward;
            GrantedSupplies = grantedSupplies < 0 ? 0 : grantedSupplies;
            GrantedRenown = grantedRenown < 0 ? 0 : grantedRenown;
            GrantedItemId = grantedItemId ?? string.Empty;
            RecruitedUnitIds = recruitedUnitIds ?? Array.Empty<string>();
        }

        public bool GrantedStageReward { get; }

        public int GrantedSupplies { get; }

        public int GrantedRenown { get; }

        public string GrantedItemId { get; }

        public IReadOnlyList<string> RecruitedUnitIds { get; }
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

        public BattleScenarioData PrepareScenario(BattleScenarioData scenario, CampaignSaveData saveData)
        {
            if (scenario == null || saveData == null)
            {
                return scenario;
            }

            bool includeStageReward = !saveData.Progress.IsRewardClaimed(scenario.ScenarioId);
            return rosterBuilder.BuildScenario(scenario, saveData, includeStageReward);
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

            saveData.Progress.SetLastBattleResult(summary);

            bool grantedStageReward = summary.WinningSide == TurnSide.Player &&
                                      scenario != null &&
                                      scenario.RewardBundle != null &&
                                      scenario.RewardBundle.HasAnyReward &&
                                      !saveData.Progress.IsRewardClaimed(summary.ScenarioId);
            IReadOnlyList<string> recruitedUnitIds = Array.Empty<string>();
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

            if (playerUnits != null)
            {
                foreach (CampaignUnitState unitState in saveData.Units)
                {
                    UnitRuntimeState runtimeState = playerUnits.FirstOrDefault(unit => unit.Id == unitState.UnitId);
                    if (runtimeState == null)
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
                recruitedUnitIds);
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

        public int GetAvailableEquipmentCount(CampaignSaveData saveData, string itemId, string unitIdToIgnore = null)
        {
            if (saveData == null || string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            int equippedCopies = saveData.Units.Count(unit =>
                unit.UnitId != unitIdToIgnore &&
                (string.Equals(unit.EquipmentLoadout.WeaponId, itemId, StringComparison.Ordinal) ||
                 string.Equals(unit.EquipmentLoadout.ArmorId, itemId, StringComparison.Ordinal)));
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

            if (definition.Category == ItemCategory.Weapon)
            {
                unitState.EquipWeapon(itemId);
                return true;
            }

            if (definition.Category == ItemCategory.Armor)
            {
                unitState.EquipArmor(itemId);
                return true;
            }

            return false;
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
                   string.Equals(unitState.EquipmentLoadout.ArmorId, itemId, StringComparison.Ordinal);
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
    }
}
