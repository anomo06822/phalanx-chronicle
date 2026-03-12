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

            return normalized ||
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

            if (definition.Category == ItemCategory.Mount)
            {
                unitState.EquipMount(itemId);
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
                   string.Equals(unitState.EquipmentLoadout.ArmorId, itemId, StringComparison.Ordinal) ||
                   string.Equals(unitState.EquipmentLoadout.MountId, itemId, StringComparison.Ordinal);
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
