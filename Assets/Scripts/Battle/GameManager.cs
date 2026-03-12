using System;
using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Core;
using PhalanxChronicle.Data;
using PhalanxChronicle.Localization;
using PhalanxChronicle.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhalanxChronicle.Battle
{
    public sealed class GameManager : MonoBehaviour
    {
        private static GameManager instance;

        [SerializeField] private BattleScenarioDefinition scenarioDefinition;
        [SerializeField] private GameLocale initialLocale = GameLocale.TraditionalChinese;

        private BattleManager battleManager;
        private CampaignDefinition campaignDefinition;
        private CampaignDirector campaignDirector;
        private CampaignProgressionService campaignProgressionService;
        private CampaignSaveRepository campaignSaveRepository;
        private CampaignSaveData campaignSaveData;
        private string currentCampMessage = string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (FindObjectOfType<GameManager>() != null)
            {
                return;
            }

            GameObject gameManagerObject = new GameObject("GameManager");
            gameManagerObject.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            LocalizationService.SetLocale(initialLocale);
            EnsureCamera();
            EnsureEventSystem();
            EnsureBattleManager();
        }

        private void Start()
        {
            if (scenarioDefinition != null)
            {
                return;
            }

            InitializeCampaignSystems();
            if (campaignSaveRepository.TryLoad(campaignDefinition.CampaignId, out CampaignSaveData loadedSave))
            {
                campaignSaveData = loadedSave;
                campaignDirector = new CampaignDirector(campaignDefinition, campaignSaveData.Progress);
                ShowContinueOrNewGame();
                return;
            }

            StartNewCampaign();
        }

        private void InitializeCampaignSystems()
        {
            if (campaignDefinition != null)
            {
                return;
            }

            campaignDefinition = CampaignCatalog.CreateLiuBeiLegend();
            campaignProgressionService = new CampaignProgressionService();
            campaignSaveRepository = new CampaignSaveRepository(campaignProgressionService);
        }

        private void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera cameraComponent = cameraObject.AddComponent<Camera>();
            cameraComponent.orthographic = true;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private void EnsureBattleManager()
        {
            if (GetComponentInChildren<BattleManager>() != null)
            {
                battleManager = GetComponentInChildren<BattleManager>();
                battleManager.SetBattleCompletedHandler(HandleBattleCompleted);
                return;
            }

            GameObject battleManagerObject = new GameObject("BattleManager");
            battleManagerObject.transform.SetParent(transform, false);
            battleManager = battleManagerObject.AddComponent<BattleManager>();
            bool autoStartBattle = scenarioDefinition != null;
            battleManager.Initialize(
                scenarioDefinition != null ? scenarioDefinition : BattleScenarioDefinition.CreateDefault(),
                autoStart: autoStartBattle,
                onBattleCompleted: autoStartBattle ? null : HandleBattleCompleted);
        }

        public void StartCampaignStage(int stageIndex)
        {
            if (campaignDirector == null || campaignSaveData == null)
            {
                return;
            }

            CampaignStageDefinition stage = campaignDirector.GetStage(stageIndex);
            if (stage == null || !campaignDirector.IsStageUnlocked(stageIndex))
            {
                return;
            }

            BattleScenarioData scenario = campaignProgressionService.PrepareScenario(
                BattleScenarioCatalog.CreateScenario(stage.ScenarioId),
                campaignSaveData);
            battleManager.StartScenario(scenario);
        }

        private void ShowContinueOrNewGame()
        {
            CampaignOptionListModel model = new CampaignOptionListModel
            {
                Title = LocalizationService.Text(campaignDefinition.CampaignNameKey, campaignDefinition.CampaignNameFallback),
                Body = string.Join(
                    "\n",
                    LocalizationService.Text("campaign.save_found", "Auto-save found."),
                    BuildCampaignProgressSnapshot()),
                PrimaryActionLabel = LocalizationService.Text("ui.button.continue", "Continue"),
                SecondaryActionLabel = LocalizationService.Text("ui.button.new_game", "New Game"),
            };

            battleManager.ShowCampaignOptionList(model, null, ContinueCampaign, StartNewCampaign);
        }

        private void StartNewCampaign()
        {
            InitializeCampaignSystems();
            campaignSaveData = campaignSaveRepository.CreateNew(campaignDefinition);
            campaignDirector = new CampaignDirector(campaignDefinition, campaignSaveData.Progress);
            ShowCampHub(LocalizationService.Text(campaignDefinition.CampaignOverviewKey, campaignDefinition.CampaignOverviewFallback));
        }

        private void ContinueCampaign()
        {
            ShowCampHub(LocalizationService.Text(campaignDefinition.CampaignOverviewKey, campaignDefinition.CampaignOverviewFallback));
        }

        private void ShowCampaignStageSelect()
        {
            if (campaignDirector == null || battleManager == null)
            {
                return;
            }

            int recommendedStageIndex = campaignDirector.GetRecommendedStageIndex();
            CampaignStageSelectModel model = new CampaignStageSelectModel
            {
                Title = LocalizationService.Text(
                    campaignDirector.Definition.CampaignNameKey,
                    campaignDirector.Definition.CampaignNameFallback),
                Body = BuildCampaignProgressSnapshot(),
                Stages = Enumerable.Range(0, campaignDirector.StageCount)
                    .Select(index => BuildStageEntryModel(index, recommendedStageIndex))
                    .ToList(),
            };

            battleManager.ShowCampaignStageSelect(model, ShowStageBriefing);
        }

        private void ShowStageBriefing(int stageIndex)
        {
            CampaignStageDefinition stage = campaignDirector != null ? campaignDirector.GetStage(stageIndex) : null;
            if (stage == null || !campaignDirector.IsStageUnlocked(stageIndex))
            {
                return;
            }

            BattleScenarioData scenario = BattleScenarioCatalog.CreateScenario(stage.ScenarioId);
            CampaignInterludeModel model = new CampaignInterludeModel
            {
                Title = LocalizationService.Text(stage.ChapterTitleKey, stage.ChapterTitleFallback),
                Body = LocalizationService.Text(stage.InterludeIntroKey, stage.InterludeIntroFallback) + "\n\n" +
                       LocalizationService.Format("campaign.stage.recommended_level", "Recommended Level {0}", scenario.RecommendedLevel),
                PrimaryActionLabel = LocalizationService.Text("ui.button.begin_battle", "Begin Battle"),
                SecondaryActionLabel = LocalizationService.Text("ui.button.back_to_camp", "Back to Camp"),
            };

            battleManager.ShowCampaignInterlude(
                model,
                () => StartCampaignStage(stageIndex),
                () => ShowCampHub(currentCampMessage));
        }

        private void ShowCampHub(string message = null)
        {
            if (campaignSaveData == null)
            {
                return;
            }

            currentCampMessage = string.IsNullOrWhiteSpace(message)
                ? LocalizationService.Text(campaignDefinition.CampaignOverviewKey, campaignDefinition.CampaignOverviewFallback)
                : message;

            int recommendedStageIndex = campaignDirector.GetRecommendedStageIndex();
            List<CampaignOptionEntryModel> options = new List<CampaignOptionEntryModel>();
            foreach (CampaignUnitState unit in campaignSaveData.Units.OrderBy(unit => unit.UnitId))
            {
                options.Add(new CampaignOptionEntryModel
                {
                    OptionId = "unit:" + unit.UnitId,
                    Title = GetUnitDisplayName(unit.UnitId),
                    Status = BuildCampUnitStatus(unit),
                    Description = BuildCampUnitEquipmentSummary(unit),
                    IsEnabled = true,
                    IsEmphasized = CanPromote(unit),
                });
            }

            options.Add(new CampaignOptionEntryModel
            {
                OptionId = "shop",
                Title = LocalizationService.Text("camp.shop.title", "Quartermaster"),
                Status = LocalizationService.Format(
                    "camp.resources",
                    "Supplies {0}  Renown {1}",
                    campaignSaveData.Inventory.Supplies,
                    campaignSaveData.Inventory.Renown),
                Description = LocalizationService.Text("camp.shop.desc", "Buy stronger weapons and armor for the next push."),
                IsEnabled = true,
                IsEmphasized = campaignProgressionService.GetShopOffers().Any(offer =>
                    campaignSaveData.Inventory.Renown >= offer.RequiredRenown &&
                    campaignSaveData.Inventory.Supplies >= offer.SuppliesCost),
            });
            options.Add(new CampaignOptionEntryModel
            {
                OptionId = "inventory",
                Title = LocalizationService.Text("camp.inventory.title", "Warehouse"),
                Status = LocalizationService.Format(
                    "camp.inventory.summary",
                    "Special Goods {0}  Stored Gear {1}",
                    GetSpecialGoodCount(),
                    GetStoredGearCount()),
                Description = LocalizationService.Text("camp.inventory.desc", "Review story rewards and every owned piece of equipment."),
                IsEnabled = true,
            });

            CampaignOptionListModel model = new CampaignOptionListModel
            {
                Title = LocalizationService.Text("camp.title", "War Camp"),
                Body = BuildCampBody(currentCampMessage),
                Options = options,
                PrimaryActionLabel = recommendedStageIndex >= 0
                    ? LocalizationService.Text("ui.button.next_battle", "Next Battle")
                    : string.Empty,
                SecondaryActionLabel = LocalizationService.Text("ui.button.chapters", "Chapters"),
            };

            battleManager.ShowCampaignOptionList(
                model,
                HandleCampOptionSelected,
                recommendedStageIndex >= 0 ? (Action)(() => ShowStageBriefing(recommendedStageIndex)) : null,
                ShowCampaignStageSelect);
        }

        private void HandleCampOptionSelected(string optionId)
        {
            if (string.IsNullOrWhiteSpace(optionId))
            {
                return;
            }

            if (optionId.StartsWith("unit:", StringComparison.Ordinal))
            {
                ShowUnitManagement(optionId.Substring("unit:".Length));
                return;
            }

            if (optionId == "shop")
            {
                ShowShop();
                return;
            }

            if (optionId == "inventory")
            {
                ShowInventory();
            }
        }

        private void ShowUnitManagement(string unitId, string message = null)
        {
            CampaignUnitState unit = campaignSaveData != null ? campaignSaveData.GetUnit(unitId) : null;
            if (unit == null)
            {
                return;
            }

            List<CampaignOptionEntryModel> options = new List<CampaignOptionEntryModel>();
            if (CanPromote(unit))
            {
                foreach (PromotionDefinition promotion in PromotionCatalog.GetOptions(unit.UnitId))
                {
                    options.Add(new CampaignOptionEntryModel
                    {
                        OptionId = "promote|" + unit.UnitId + "|" + promotion.PromotionId,
                        Title = LocalizationService.Format(
                            "camp.promote.to",
                            "Promote to {0}",
                            LocalizationService.Text(UnitClassCatalog.Get(promotion.TargetClassId).DisplayNameKey, promotion.TargetClassId)),
                        Status = LocalizationService.Text("camp.promote.branch", "Promotion Branch"),
                        Description = BuildPromotionSummary(promotion),
                        IsEnabled = true,
                        IsEmphasized = true,
                        IsPromotionOption = true,
                    });
                }
            }

            foreach (ItemDefinition item in campaignProgressionService.GetEquippableItems(campaignSaveData, unit.UnitId, ItemCategory.Weapon))
            {
                options.Add(new CampaignOptionEntryModel
                {
                    OptionId = "weapon|" + unit.UnitId + "|" + item.ItemId,
                    Title = LocalizationService.Text(item.NameKey, item.NameFallback),
                    Status = string.Equals(unit.EquipmentLoadout.WeaponId, item.ItemId, StringComparison.Ordinal)
                        ? LocalizationService.Text("camp.equip.current", "Equipped")
                        : LocalizationService.Format("camp.equip.available", "Available {0}", campaignProgressionService.GetAvailableEquipmentCount(campaignSaveData, item.ItemId, unit.UnitId)),
                    Description = BuildItemSummary(item),
                    IsEnabled = true,
                    IsEmphasized = string.Equals(unit.EquipmentLoadout.WeaponId, item.ItemId, StringComparison.Ordinal),
                });
            }

            foreach (ItemDefinition item in campaignProgressionService.GetEquippableItems(campaignSaveData, unit.UnitId, ItemCategory.Armor))
            {
                options.Add(new CampaignOptionEntryModel
                {
                    OptionId = "armor|" + unit.UnitId + "|" + item.ItemId,
                    Title = LocalizationService.Text(item.NameKey, item.NameFallback),
                    Status = string.Equals(unit.EquipmentLoadout.ArmorId, item.ItemId, StringComparison.Ordinal)
                        ? LocalizationService.Text("camp.equip.current", "Equipped")
                        : LocalizationService.Format("camp.equip.available", "Available {0}", campaignProgressionService.GetAvailableEquipmentCount(campaignSaveData, item.ItemId, unit.UnitId)),
                    Description = BuildItemSummary(item),
                    IsEnabled = true,
                    IsEmphasized = string.Equals(unit.EquipmentLoadout.ArmorId, item.ItemId, StringComparison.Ordinal),
                });
            }

            foreach (ItemDefinition item in campaignProgressionService.GetEquippableItems(campaignSaveData, unit.UnitId, ItemCategory.Mount))
            {
                options.Add(new CampaignOptionEntryModel
                {
                    OptionId = "mount|" + unit.UnitId + "|" + item.ItemId,
                    Title = LocalizationService.Text(item.NameKey, item.NameFallback),
                    Status = string.Equals(unit.EquipmentLoadout.MountId, item.ItemId, StringComparison.Ordinal)
                        ? LocalizationService.Text("camp.equip.current", "Equipped")
                        : LocalizationService.Format("camp.equip.available", "Available {0}", campaignProgressionService.GetAvailableEquipmentCount(campaignSaveData, item.ItemId, unit.UnitId)),
                    Description = BuildItemSummary(item),
                    IsEnabled = true,
                    IsEmphasized = string.Equals(unit.EquipmentLoadout.MountId, item.ItemId, StringComparison.Ordinal),
                });
            }

            CampaignOptionListModel model = new CampaignOptionListModel
            {
                Title = GetUnitDisplayName(unit.UnitId),
                Body = BuildUnitManagementBody(unit, message),
                Options = options,
                PrimaryActionLabel = LocalizationService.Text("ui.button.back_to_camp", "Back to Camp"),
                SecondaryActionLabel = string.Empty,
            };

            battleManager.ShowCampaignOptionList(model, HandleUnitManagementOptionSelected, () => ShowCampHub(currentCampMessage));
        }

        private void HandleUnitManagementOptionSelected(string optionId)
        {
            if (string.IsNullOrWhiteSpace(optionId))
            {
                return;
            }

            string[] segments = optionId.Split('|');
            if (segments.Length < 2)
            {
                return;
            }

            string unitId = segments[1];
            if (segments[0] == "promote")
            {
                string promotionId = segments.Length >= 3 ? segments[2] : null;
                if (campaignProgressionService.TryPromoteUnit(campaignSaveData, unitId, promotionId))
                {
                    campaignSaveRepository.Save(campaignSaveData);
                    ShowUnitManagement(unitId, LocalizationService.Text("camp.promote.success", "Promotion complete."));
                }

                return;
            }

            if (segments.Length < 3)
            {
                return;
            }

            string itemId = segments[2];
            bool equipped = segments[0] == "weapon"
                ? campaignProgressionService.TryEquipWeapon(campaignSaveData, unitId, itemId)
                : segments[0] == "armor"
                    ? campaignProgressionService.TryEquipArmor(campaignSaveData, unitId, itemId)
                    : campaignProgressionService.TryEquipMount(campaignSaveData, unitId, itemId);
            if (equipped)
            {
                campaignSaveRepository.Save(campaignSaveData);
                ShowUnitManagement(unitId, LocalizationService.Text("camp.equip.success", "Loadout updated."));
            }
        }

        private void ShowShop(string message = null)
        {
            List<CampaignOptionEntryModel> options = campaignProgressionService.GetShopOffers()
                .Select(offer => BuildShopEntry(offer))
                .ToList();

            CampaignOptionListModel model = new CampaignOptionListModel
            {
                Title = LocalizationService.Text("camp.shop.title", "Quartermaster"),
                Body = string.Join(
                    "\n",
                    LocalizationService.Format("camp.resources", "Supplies {0}  Renown {1}", campaignSaveData.Inventory.Supplies, campaignSaveData.Inventory.Renown),
                    string.IsNullOrWhiteSpace(message) ? LocalizationService.Text("camp.shop.desc", "Buy stronger weapons, armor, and mounts for the next push.") : message),
                Options = options,
                PrimaryActionLabel = LocalizationService.Text("ui.button.back_to_camp", "Back to Camp"),
                SecondaryActionLabel = string.Empty,
            };

            battleManager.ShowCampaignOptionList(model, HandleShopOptionSelected, () => ShowCampHub(currentCampMessage));
        }

        private CampaignOptionEntryModel BuildShopEntry(ShopOfferDefinition offer)
        {
            ItemDefinition item = ItemCatalog.Get(offer.ItemId);
            bool renownUnlocked = campaignSaveData.Inventory.Renown >= offer.RequiredRenown;
            bool canAfford = campaignSaveData.Inventory.Supplies >= offer.SuppliesCost;
            return new CampaignOptionEntryModel
            {
                OptionId = "buy|" + offer.ItemId,
                Title = LocalizationService.Text(item.NameKey, item.NameFallback),
                Status = renownUnlocked
                    ? LocalizationService.Format("camp.shop.cost", "Cost {0} Supplies", offer.SuppliesCost)
                    : LocalizationService.Format("camp.shop.renown_gate", "Needs Renown {0}", offer.RequiredRenown),
                Description = BuildItemSummary(item) + " | " +
                              LocalizationService.Format("camp.shop.owned", "Owned {0}", campaignSaveData.Inventory.GetQuantity(offer.ItemId)),
                IsEnabled = renownUnlocked && canAfford,
                IsEmphasized = renownUnlocked && canAfford,
            };
        }

        private void HandleShopOptionSelected(string optionId)
        {
            if (string.IsNullOrWhiteSpace(optionId) || !optionId.StartsWith("buy|", StringComparison.Ordinal))
            {
                return;
            }

            string itemId = optionId.Substring("buy|".Length);
            if (campaignProgressionService.TryPurchaseItem(campaignSaveData, itemId))
            {
                campaignSaveRepository.Save(campaignSaveData);
                ShowShop(LocalizationService.Text("camp.shop.success", "Purchase complete."));
            }
        }

        private void ShowInventory(string message = null)
        {
            List<CampaignOptionEntryModel> options = campaignSaveData.Inventory.Entries
                .OrderByDescending(entry =>
                {
                    ItemDefinition definition = ItemCatalog.Get(entry.ItemId);
                    return definition != null && definition.Category == ItemCategory.SpecialGood ? 1 : 0;
                })
                .ThenBy(entry => entry.ItemId, StringComparer.Ordinal)
                .Select(entry =>
                {
                    ItemDefinition definition = ItemCatalog.Get(entry.ItemId);
                    return new CampaignOptionEntryModel
                    {
                        OptionId = string.Empty,
                        Title = definition != null ? LocalizationService.Text(definition.NameKey, definition.NameFallback) : entry.ItemId,
                        Status = LocalizationService.Format("camp.inventory.count", "x{0}", entry.Quantity),
                        Description = definition != null ? LocalizationService.Text(definition.DescriptionKey, definition.DescriptionFallback) : string.Empty,
                        IsEnabled = false,
                    };
                })
                .ToList();
            if (options.Count == 0)
            {
                options.Add(new CampaignOptionEntryModel
                {
                    OptionId = string.Empty,
                    Title = LocalizationService.Text("camp.inventory.empty.title", "Warehouse Empty"),
                    Status = string.Empty,
                    Description = LocalizationService.Text("camp.inventory.empty.desc", "No stored items yet."),
                    IsEnabled = false,
                });
            }

            CampaignOptionListModel model = new CampaignOptionListModel
            {
                Title = LocalizationService.Text("camp.inventory.title", "Warehouse"),
                Body = string.IsNullOrWhiteSpace(message)
                    ? LocalizationService.Text("camp.inventory.desc", "Review story rewards and every owned piece of equipment.")
                    : message,
                Options = options,
                PrimaryActionLabel = LocalizationService.Text("ui.button.back_to_camp", "Back to Camp"),
                SecondaryActionLabel = string.Empty,
            };

            battleManager.ShowCampaignOptionList(model, null, () => ShowCampHub(currentCampMessage));
        }

        private CampaignStageEntryModel BuildStageEntryModel(int stageIndex, int recommendedStageIndex)
        {
            CampaignStageDefinition stage = campaignDirector.GetStage(stageIndex);
            bool unlocked = campaignDirector.IsStageUnlocked(stageIndex);
            bool cleared = campaignDirector.IsStageCleared(stageIndex);
            bool recommended = unlocked && stageIndex == recommendedStageIndex;

            string statusKey;
            string statusFallback;
            if (!unlocked)
            {
                statusKey = "campaign.stage.locked";
                statusFallback = "Locked";
            }
            else if (recommended && !cleared)
            {
                statusKey = "campaign.stage.recommended";
                statusFallback = "Next Battle";
            }
            else if (cleared)
            {
                statusKey = "campaign.stage.cleared";
                statusFallback = "Cleared";
            }
            else
            {
                statusKey = "campaign.stage.unlocked";
                statusFallback = "Unlocked";
            }

            return new CampaignStageEntryModel
            {
                StageIndex = stageIndex,
                Title = LocalizationService.Text(stage.ChapterTitleKey, stage.ChapterTitleFallback),
                Status = LocalizationService.Text(statusKey, statusFallback),
                Description = LocalizationService.Text(stage.InterludeIntroKey, stage.InterludeIntroFallback),
                IsUnlocked = unlocked,
                IsCleared = cleared,
                IsRecommended = recommended,
            };
        }

        private void HandleBattleCompleted(BattleResultSummary result)
        {
            if (campaignDirector == null || result == null || campaignSaveData == null)
            {
                return;
            }

            campaignDirector.RecordBattleResult(result);
            CampaignBattleResolution resolution = campaignProgressionService.FinalizeBattle(
                campaignSaveData,
                battleManager.CurrentScenarioData,
                result,
                battleManager.Simulation != null
                    ? battleManager.Simulation.Context.GetUnits(UnitFaction.Player, false)
                    : Array.Empty<UnitRuntimeState>());
            campaignSaveRepository.Save(campaignSaveData);

            int stageIndex = campaignDirector.GetStageIndex(result.ScenarioId);
            CampaignStageDefinition stage = stageIndex >= 0 ? campaignDirector.GetStage(stageIndex) : null;
            string narrative = result.WinningSide == TurnSide.Player && stage != null
                ? LocalizationService.Text(stage.InterludeOutroKey, stage.InterludeOutroFallback)
                : LocalizationService.Text("campaign.defeat.body", "The line has failed, but the campaign is not over. Regroup and choose your next move.");
            string battleSummary = BuildBattleSummary(result);
            string rewardSummary = BuildRewardSummary(resolution);
            ShowCampHub(string.Join("\n\n", new[] { narrative, battleSummary, rewardSummary }.Where(text => !string.IsNullOrWhiteSpace(text))));
        }

        private string BuildBattleSummary(BattleResultSummary result)
        {
            string survivors = result.SurvivingUnitIds != null && result.SurvivingUnitIds.Count > 0
                ? string.Join(
                    "、",
                    result.SurvivingUnitIds.Select(unitId => LocalizationService.Text("unit." + unitId.Replace('-', '_'), unitId)))
                : LocalizationService.Text("campaign.summary.none", "None");

            return LocalizationService.Format(
                "campaign.summary.result",
                "Rounds fought: {0}\nSurvivors: {1}",
                result.RoundCount,
                survivors);
        }

        private string BuildRewardSummary(CampaignBattleResolution resolution)
        {
            if (resolution == null)
            {
                return string.Empty;
            }

            if (!resolution.GrantedStageReward)
            {
                return LocalizationService.Text("campaign.replay.reward_info", "Replay rewards already claimed. Battle EXP still applied.");
            }

            List<string> parts = new List<string>
            {
                LocalizationService.Format("campaign.reward.supplies", "Supplies +{0}", resolution.GrantedSupplies),
                LocalizationService.Format("campaign.reward.renown", "Renown +{0}", resolution.GrantedRenown),
            };
            if (!string.IsNullOrWhiteSpace(resolution.GrantedItemId))
            {
                ItemDefinition item = ItemCatalog.Get(resolution.GrantedItemId);
                parts.Add(LocalizationService.Format(
                    "campaign.reward.item",
                    "Special Good: {0}",
                    item != null ? LocalizationService.Text(item.NameKey, item.NameFallback) : resolution.GrantedItemId));
            }

            if (resolution.RecruitedUnitIds != null && resolution.RecruitedUnitIds.Count > 0)
            {
                parts.Add(LocalizationService.Format(
                    "campaign.reward.recruit",
                    "Joined the banner: {0}",
                    string.Join("、", resolution.RecruitedUnitIds.Select(GetUnitDisplayName))));
            }

            return string.Join(" | ", parts);
        }

        private string BuildCampaignProgressSnapshot()
        {
            return string.Join(
                "\n",
                LocalizationService.Format("campaign.snapshot.chapters", "Unlocked Chapters {0}/{1}", campaignDirector.Progress.UnlockedStageIndex + 1, campaignDirector.StageCount),
                LocalizationService.Format("camp.resources", "Supplies {0}  Renown {1}", campaignSaveData.Inventory.Supplies, campaignSaveData.Inventory.Renown));
        }

        private string BuildCampBody(string message)
        {
            return string.Join(
                "\n\n",
                new[]
                {
                    message,
                    LocalizationService.Format("camp.resources", "Supplies {0}  Renown {1}", campaignSaveData.Inventory.Supplies, campaignSaveData.Inventory.Renown),
                }.Where(text => !string.IsNullOrWhiteSpace(text)));
        }

        private string BuildCampUnitStatus(CampaignUnitState unit)
        {
            string status = LocalizationService.Format(
                "camp.unit.status",
                "{0}  Lv {1}",
                LocalizationService.Text(UnitClassCatalog.Get(unit.ClassId).DisplayNameKey, unit.ClassId),
                unit.Level);
            return CanPromote(unit)
                ? status + " | " + LocalizationService.Text("ui.unlock.promotion", "Promotion Ready")
                : status;
        }

        private string BuildCampUnitEquipmentSummary(CampaignUnitState unit)
        {
            ItemDefinition weapon = ItemCatalog.Get(unit.EquipmentLoadout.WeaponId);
            ItemDefinition armor = ItemCatalog.Get(unit.EquipmentLoadout.ArmorId);
            ItemDefinition mount = ItemCatalog.Get(unit.EquipmentLoadout.MountId);
            return LocalizationService.Format(
                "camp.unit.equipment",
                "Weapon: {0} | Armor: {1} | Mount: {2}",
                weapon != null ? LocalizationService.Text(weapon.NameKey, weapon.NameFallback) : LocalizationService.Text("camp.equip.none", "None"),
                armor != null ? LocalizationService.Text(armor.NameKey, armor.NameFallback) : LocalizationService.Text("camp.equip.none", "None"),
                mount != null ? LocalizationService.Text(mount.NameKey, mount.NameFallback) : LocalizationService.Text("camp.equip.none", "None"));
        }

        private string BuildUnitManagementBody(CampaignUnitState unit, string message)
        {
            ItemDefinition mount = ItemCatalog.Get(unit.EquipmentLoadout.MountId);
            int moveValue = unit.MoveRange + (mount != null ? mount.MoveBonus : 0);
            List<string> parts = new List<string>
            {
                LocalizationService.Format(
                    "camp.unit.detail",
                    "{0}  Lv {1}  EXP {2}/{3}\nATK {4}  DEF {5}  HP {6}  MP {7}  MOVE {8}",
                    LocalizationService.Text(UnitClassCatalog.Get(unit.ClassId).DisplayNameKey, unit.ClassId),
                    unit.Level,
                    unit.CurrentExp,
                    ExperienceSystem.GetRequiredExpForLevel(unit.Level),
                    unit.Attack,
                    unit.Defense,
                    unit.MaxHp,
                    unit.MaxMana,
                    moveValue),
                BuildCampUnitEquipmentSummary(unit),
            };

            if (CanPromote(unit))
            {
                parts.Add(LocalizationService.Text(
                    "camp.promote.ready_hint",
                    "Promotion is available. The highlighted Promotion Branch cards below are the class change options."));
            }
            else if (unit != null && unit.HasPromoted)
            {
                parts.Add(LocalizationService.Text(
                    "camp.promote.done_hint",
                    "This unit has already promoted. Promotion cannot be changed again."));
            }
            else if (unit != null)
            {
                parts.Add(LocalizationService.Format(
                    "camp.promote.locked_hint",
                    "Promotion unlocks at Lv10. Current level: Lv {0}.",
                    unit.Level));
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                parts.Add(message);
            }

            return string.Join("\n\n", parts);
        }

        private string BuildPromotionSummary(PromotionDefinition definition)
        {
            string passiveName = LocalizationService.Text(definition.PassiveSkillNameKey, definition.PassiveSkill.ToString());
            string activeName = LocalizationService.Text(definition.ActiveSkillNameKey, definition.ActiveSkill.ToString());
            string statSummary = LocalizationService.Format(
                "camp.promote.summary",
                "+{0} HP  +{1} ATK  +{2} DEF  +{3} MP",
                definition.HpBonus,
                definition.AttackBonus,
                definition.DefenseBonus,
                definition.ManaBonus);
            return string.Join(
                "\n",
                LocalizationService.Text("camp.promote.tap_hint", "Tap to promote immediately."),
                statSummary,
                LocalizationService.Text("ui.label.passive", "Passive Doctrine") + ": " + passiveName,
                LocalizationService.Text("ui.label.active", "Battle Art") + ": " + activeName);
        }

        private string BuildItemSummary(ItemDefinition item)
        {
            List<string> statParts = new List<string>();
            if (item.AttackBonus != 0)
            {
                statParts.Add(LocalizationService.Format("camp.item.attack", "ATK +{0}", item.AttackBonus));
            }

            if (item.DefenseBonus != 0)
            {
                statParts.Add(LocalizationService.Format("camp.item.defense", "DEF +{0}", item.DefenseBonus));
            }

            if (item.HpBonus != 0)
            {
                statParts.Add(LocalizationService.Format("camp.item.hp", "HP +{0}", item.HpBonus));
            }

            if (item.MoveBonus != 0)
            {
                statParts.Add(LocalizationService.Format("camp.item.move", "MOVE +{0}", item.MoveBonus));
            }

            string description = LocalizationService.Text(item.DescriptionKey, item.DescriptionFallback);
            return statParts.Count == 0 ? description : string.Join("  ", statParts) + " | " + description;
        }

        private bool CanPromote(CampaignUnitState unit)
        {
            return unit != null &&
                   !unit.HasPromoted &&
                   unit.Level >= 10 &&
                   PromotionCatalog.GetOptions(unit.UnitId).Count > 0;
        }

        private string GetUnitDisplayName(string unitId)
        {
            CampaignUnitState unit = campaignSaveData != null ? campaignSaveData.GetUnit(unitId) : null;
            return unit != null
                ? LocalizationService.Text(unit.DisplayNameKey, unit.DisplayName)
                : LocalizationService.Text("unit." + unitId.Replace('-', '_'), unitId);
        }

        private int GetSpecialGoodCount()
        {
            return campaignSaveData.Inventory.Entries.Count(entry =>
            {
                ItemDefinition definition = ItemCatalog.Get(entry.ItemId);
                return definition != null && definition.Category == ItemCategory.SpecialGood;
            });
        }

        private int GetStoredGearCount()
        {
            return campaignSaveData.Inventory.Entries.Sum(entry =>
            {
                ItemDefinition definition = ItemCatalog.Get(entry.ItemId);
                return definition != null && definition.IsEquipable ? entry.Quantity : 0;
            });
        }
    }
}
