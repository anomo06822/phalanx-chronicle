using System;
using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    [Serializable]
    public sealed class InventoryItemEntry
    {
        public InventoryItemEntry(string itemId, int quantity)
        {
            ItemId = itemId ?? string.Empty;
            Quantity = quantity < 0 ? 0 : quantity;
        }

        public string ItemId { get; }

        public int Quantity { get; }
    }

    [Serializable]
    public sealed class CampaignInventoryState
    {
        private readonly Dictionary<string, int> itemQuantities;

        public CampaignInventoryState(
            int supplies = 0,
            int renown = 0,
            IReadOnlyList<InventoryItemEntry> items = null)
        {
            Supplies = supplies < 0 ? 0 : supplies;
            Renown = renown < 0 ? 0 : renown;
            itemQuantities = new Dictionary<string, int>(StringComparer.Ordinal);
            if (items == null)
            {
                return;
            }

            foreach (InventoryItemEntry entry in items)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId) || entry.Quantity <= 0)
                {
                    continue;
                }

                itemQuantities[entry.ItemId] = GetQuantity(entry.ItemId) + entry.Quantity;
            }
        }

        public int Supplies { get; private set; }

        public int Renown { get; private set; }

        public IReadOnlyList<InventoryItemEntry> Entries => itemQuantities
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new InventoryItemEntry(pair.Key, pair.Value))
            .ToList();

        public int GetQuantity(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && itemQuantities.TryGetValue(itemId, out int quantity)
                ? quantity
                : 0;
        }

        public void AddSupplies(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Supplies += amount;
        }

        public bool TrySpendSupplies(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (Supplies < amount)
            {
                return false;
            }

            Supplies -= amount;
            return true;
        }

        public void AddRenown(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Renown += amount;
        }

        public void AddItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return;
            }

            itemQuantities[itemId] = GetQuantity(itemId) + quantity;
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return false;
            }

            int currentQuantity = GetQuantity(itemId);
            if (currentQuantity < quantity)
            {
                return false;
            }

            if (currentQuantity == quantity)
            {
                itemQuantities.Remove(itemId);
            }
            else
            {
                itemQuantities[itemId] = currentQuantity - quantity;
            }

            return true;
        }
    }

    [Serializable]
    public sealed class CampaignUnitState
    {
        public CampaignUnitState(
            string unitId,
            string displayName,
            string displayNameKey,
            UnitRole role,
            string roleNameKey,
            PassiveSkillType passiveSkill,
            string passiveSkillNameKey,
            string passiveSkillDescriptionKey,
            ActiveSkillType activeSkill,
            string activeSkillNameKey,
            string activeSkillDescriptionKey,
            int maxHp,
            int attack,
            int defense,
            int moveRange,
            int attackRange,
            int maxMana,
            string classId,
            string growthProfileId,
            AiProfileType aiProfile,
            EquipmentLoadout equipmentLoadout,
            int level = 1,
            int currentExp = 0,
            BondState bondState = null,
            bool hasPromoted = false)
        {
            UnitId = unitId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            DisplayNameKey = displayNameKey ?? string.Empty;
            Role = role;
            RoleNameKey = roleNameKey ?? string.Empty;
            PassiveSkill = passiveSkill;
            PassiveSkillNameKey = passiveSkillNameKey ?? string.Empty;
            PassiveSkillDescriptionKey = passiveSkillDescriptionKey ?? string.Empty;
            ActiveSkill = activeSkill;
            ActiveSkillNameKey = activeSkillNameKey ?? string.Empty;
            ActiveSkillDescriptionKey = activeSkillDescriptionKey ?? string.Empty;
            MaxHp = maxHp < 1 ? 1 : maxHp;
            Attack = attack < 1 ? 1 : attack;
            Defense = defense < 0 ? 0 : defense;
            MoveRange = moveRange < 1 ? 1 : moveRange;
            AttackRange = attackRange < 1 ? 1 : attackRange;
            MaxMana = maxMana < 0 ? 0 : maxMana;
            ClassId = classId ?? string.Empty;
            GrowthProfileId = growthProfileId ?? string.Empty;
            AiProfile = aiProfile;
            EquipmentLoadout = equipmentLoadout ?? EquipmentLoadout.Empty;
            Level = level < 1 ? 1 : level;
            CurrentExp = currentExp < 0 ? 0 : currentExp;
            BondState = bondState ?? new BondState();
            HasPromoted = hasPromoted;
        }

        public string UnitId { get; }

        public string DisplayName { get; private set; }

        public string DisplayNameKey { get; private set; }

        public UnitRole Role { get; }

        public string RoleNameKey { get; private set; }

        public PassiveSkillType PassiveSkill { get; private set; }

        public string PassiveSkillNameKey { get; private set; }

        public string PassiveSkillDescriptionKey { get; private set; }

        public ActiveSkillType ActiveSkill { get; private set; }

        public string ActiveSkillNameKey { get; private set; }

        public string ActiveSkillDescriptionKey { get; private set; }

        public int MaxHp { get; private set; }

        public int Attack { get; private set; }

        public int Defense { get; private set; }

        public int MoveRange { get; private set; }

        public int AttackRange { get; private set; }

        public int MaxMana { get; private set; }

        public string ClassId { get; private set; }

        public string GrowthProfileId { get; private set; }

        public AiProfileType AiProfile { get; private set; }

        public EquipmentLoadout EquipmentLoadout { get; private set; }

        public int Level { get; private set; }

        public int CurrentExp { get; private set; }

        public BondState BondState { get; private set; }

        public bool HasPromoted { get; private set; }

        public void EquipWeapon(string itemId)
        {
            EquipmentLoadout = new EquipmentLoadout(itemId, EquipmentLoadout.ArmorId, EquipmentLoadout.MountId);
        }

        public void EquipArmor(string itemId)
        {
            EquipmentLoadout = new EquipmentLoadout(EquipmentLoadout.WeaponId, itemId, EquipmentLoadout.MountId);
        }

        public void EquipMount(string itemId)
        {
            EquipmentLoadout = new EquipmentLoadout(EquipmentLoadout.WeaponId, EquipmentLoadout.ArmorId, itemId);
        }

        public void SetActiveSkill(ActiveSkillType activeSkill, string activeSkillNameKey, string activeSkillDescriptionKey)
        {
            ActiveSkill = activeSkill;
            ActiveSkillNameKey = activeSkillNameKey ?? string.Empty;
            ActiveSkillDescriptionKey = activeSkillDescriptionKey ?? string.Empty;
        }

        public void SyncFromBattle(UnitRuntimeState runtimeState, int hpBonus, int attackBonus, int defenseBonus)
        {
            if (runtimeState == null)
            {
                return;
            }

            MaxHp = Math.Max(1, runtimeState.MaxHp - hpBonus);
            Attack = Math.Max(1, runtimeState.Attack - attackBonus);
            Defense = Math.Max(0, runtimeState.Defense - defenseBonus);
            MaxMana = Math.Max(0, runtimeState.MaxMana);
            ClassId = runtimeState.ClassId ?? ClassId;
            GrowthProfileId = runtimeState.GrowthProfileId ?? GrowthProfileId;
            AiProfile = runtimeState.AiProfile;
            Level = runtimeState.Level;
            CurrentExp = runtimeState.CurrentExp;
            BondState = new BondState(runtimeState.BondState.SupportLevel, runtimeState.BondState.SharedBattles);
        }

        public void ApplyPromotion(PromotionDefinition definition)
        {
            if (definition == null || HasPromoted)
            {
                return;
            }

            MaxHp += definition.HpBonus;
            Attack += definition.AttackBonus;
            Defense += definition.DefenseBonus;
            MaxMana += definition.ManaBonus;
            ClassId = definition.TargetClassId;
            GrowthProfileId = definition.TargetGrowthProfileId;
            PassiveSkill = definition.PassiveSkill;
            PassiveSkillNameKey = definition.PassiveSkillNameKey;
            PassiveSkillDescriptionKey = definition.PassiveSkillDescriptionKey;
            ActiveSkill = definition.ActiveSkill;
            ActiveSkillNameKey = definition.ActiveSkillNameKey;
            ActiveSkillDescriptionKey = definition.ActiveSkillDescriptionKey;
            HasPromoted = true;
        }
    }

    [Serializable]
    public sealed class CampaignSaveData
    {
        private readonly List<CampaignUnitState> units;

        public CampaignSaveData(
            string campaignId,
            CampaignProgress progress,
            CampaignInventoryState inventory,
            IReadOnlyList<CampaignUnitState> units,
            int version = 5)
        {
            CampaignId = campaignId ?? string.Empty;
            Progress = progress ?? new CampaignProgress();
            Inventory = inventory ?? new CampaignInventoryState();
            this.units = units != null ? new List<CampaignUnitState>(units) : new List<CampaignUnitState>();
            Version = version < 1 ? 1 : version;
        }

        public string CampaignId { get; }

        public CampaignProgress Progress { get; }

        public CampaignInventoryState Inventory { get; }

        public IReadOnlyList<CampaignUnitState> Units => units;

        public int Version { get; }

        public CampaignUnitState GetUnit(string unitId)
        {
            return units.FirstOrDefault(unit => unit.UnitId == unitId);
        }

        public bool TryAddUnit(CampaignUnitState unitState)
        {
            if (unitState == null || string.IsNullOrWhiteSpace(unitState.UnitId) || GetUnit(unitState.UnitId) != null)
            {
                return false;
            }

            units.Add(unitState);
            return true;
        }
    }
}
