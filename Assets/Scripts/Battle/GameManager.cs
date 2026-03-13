using System;
using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Core;
using PhalanxChronicle.Data;
using PhalanxChronicle.Localization;
using PhalanxChronicle.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;

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
            Camera cameraComponent = Camera.main;
            if (cameraComponent == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraComponent = cameraObject.AddComponent<Camera>();
            }

            cameraComponent.orthographic = true;
            PixelPerfectCamera pixelPerfect = cameraComponent.GetComponent<PixelPerfectCamera>();
            if (pixelPerfect == null)
            {
                pixelPerfect = cameraComponent.gameObject.AddComponent<PixelPerfectCamera>();
            }

            pixelPerfect.assetsPPU = 64;
            pixelPerfect.refResolutionX = 1280;
            pixelPerfect.refResolutionY = 720;
            pixelPerfect.cropFrameX = false;
            pixelPerfect.cropFrameY = false;
            pixelPerfect.upscaleRT = false;
            pixelPerfect.pixelSnapping = true;
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
            battleManager.ConfigureFirstBattleOnboarding(
                ShouldEnableFirstBattleOnboarding(stage.ScenarioId),
                HandleFirstBattleOnboardingResolved);
            battleManager.StartScenario(scenario);
        }

        private void ShowContinueOrNewGame()
        {
            CampaignOptionListModel model = new CampaignOptionListModel
            {
                Title = LocalizationService.Text(campaignDefinition.CampaignNameKey, campaignDefinition.CampaignNameFallback),
                Body = string.Join(
                    "\n\n",
                    LocalizationService.Text("campaign.save_found", "已找到自動存檔。"),
                    BuildCampaignProgressSnapshot(),
                    LocalizationService.Text("campaign.launch.continue_hint", "可直接延續目前戰役，或重新開始一次帶引導的首戰流程。")),
                Options = BuildContinueOptionEntries(),
                PrimaryActionLabel = LocalizationService.Text("ui.button.continue", "繼續"),
                SecondaryActionLabel = LocalizationService.Text("ui.button.new_game", "重新開局"),
            };

            battleManager.ShowCampaignOptionList(model, null, ContinueCampaign, StartNewCampaign);
        }

        private void StartNewCampaign()
        {
            InitializeCampaignSystems();
            campaignSaveData = campaignSaveRepository.CreateNew(campaignDefinition);
            campaignDirector = new CampaignDirector(campaignDefinition, campaignSaveData.Progress);
            ShowFirstLaunchIntro();
        }

        private void ContinueCampaign()
        {
            ShowCampHub(LocalizationService.Text(campaignDefinition.CampaignOverviewKey, campaignDefinition.CampaignOverviewFallback));
        }

        private void ShowFirstLaunchIntro()
        {
            if (campaignSaveData == null || campaignDirector == null)
            {
                return;
            }

            campaignSaveData.Progress.MarkFirstLaunchIntroSeen();
            campaignSaveRepository.Save(campaignSaveData);
            int recommendedStageIndex = Mathf.Max(0, campaignDirector.GetRecommendedStageIndex());

            CampaignOptionListModel model = new CampaignOptionListModel
            {
                Title = LocalizationService.Text(campaignDefinition.CampaignNameKey, campaignDefinition.CampaignNameFallback),
                Body = string.Join(
                    "\n\n",
                    LocalizationService.Text(campaignDefinition.CampaignOverviewKey, campaignDefinition.CampaignOverviewFallback),
                    LocalizationService.Text("campaign.launch.session_length", "建議首輪遊玩時間：15 到 20 分鐘。"),
                    LocalizationService.Text("campaign.launch.demo_path", "最佳起手路線：第一章簡報 → 廣宗之戰 → 結算 → 軍營。")),
                Options = BuildFirstLaunchOptionEntries(recommendedStageIndex),
                PrimaryActionLabel = LocalizationService.Text("ui.button.start_demo", "開始首場戰鬥"),
                SecondaryActionLabel = LocalizationService.Text("ui.button.open_camp", "進入軍營"),
            };

            battleManager.ShowCampaignOptionList(
                model,
                null,
                () => ShowStageBriefing(recommendedStageIndex),
                () => ShowCampHub(LocalizationService.Text(campaignDefinition.CampaignOverviewKey, campaignDefinition.CampaignOverviewFallback)));
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
                Eyebrow = LocalizationService.Text("campaign.shell.eyebrow", "戰役路線"),
                Title = LocalizationService.Text(
                    campaignDirector.Definition.CampaignNameKey,
                    campaignDirector.Definition.CampaignNameFallback),
                Body = LocalizationService.Text(campaignDirector.Definition.CampaignOverviewKey, campaignDirector.Definition.CampaignOverviewFallback),
                ProgressLabel = BuildCampaignProgressSnapshot(),
                HighlightLabel = recommendedStageIndex >= 0
                    ? BuildRecommendedStageSummary(recommendedStageIndex)
                    : LocalizationService.Text("campaign.stage.no_recommendation", "所有已解鎖章節都能出戰，選擇你想推進的戰線。"),
                DeckTitle = LocalizationService.Text("campaign.deck.stage_select", "前線章節"),
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

            BattleScenarioData scenario = campaignProgressionService.PrepareScenario(
                BattleScenarioCatalog.CreateScenario(stage.ScenarioId),
                campaignSaveData);
            ItemDefinition rewardItem = ItemCatalog.Get(scenario.RewardBundle.RewardItemId);
            List<string> detailLines = new List<string>
            {
                LocalizationService.Format("campaign.stage.recommended_level", "建議等級 {0}", scenario.RecommendedLevel),
                LocalizationService.Format("campaign.stage.variant", "戰場變體：{0}", FormatScenarioVariantTag(scenario.ScenarioVariantTag)),
                BuildStageBattlefieldLabel(stage.ScenarioId),
            };
            foreach (BonusRewardDefinition bonusReward in scenario.BonusRewards.Where(reward =>
                         reward != null &&
                         !campaignSaveData.Progress.IsBonusRewardClaimed(reward.RewardId)).Take(2))
            {
                ItemDefinition bonusItem = ItemCatalog.Get(bonusReward.RewardItemId);
                detailLines.Add(LocalizationService.Format(
                    "campaign.stage.bonus_objective",
                    "次要目標：{0} -> {1}",
                    LocalizationService.Text(bonusReward.ObjectiveKey, bonusReward.ObjectiveFallback),
                    bonusItem != null
                        ? LocalizationService.Text(bonusItem.NameKey, bonusItem.NameFallback)
                        : bonusReward.RewardItemId));
            }
            string rewardPreview = rewardItem != null && !campaignSaveData.Progress.IsRewardClaimed(stage.ScenarioId)
                ? LocalizationService.Format("campaign.stage.reward_preview", "首通寶物：{0}", LocalizationService.Text(rewardItem.NameKey, rewardItem.NameFallback))
                : LocalizationService.Text("campaign.replay.reward_info", "此章節首通獎勵已領取，回放仍可獲得戰鬥經驗。");

            CampaignInterludeModel model = new CampaignInterludeModel
            {
                Eyebrow = LocalizationService.Text("campaign.interlude.eyebrow", "戰前簡報"),
                Title = LocalizationService.Text(stage.ChapterTitleKey, stage.ChapterTitleFallback),
                Body = LocalizationService.Text(stage.InterludeIntroKey, stage.InterludeIntroFallback),
                ProgressLabel = BuildCampaignProgressSnapshot(),
                DeckTitle = LocalizationService.Text("campaign.interlude.deck_title", "作戰摘要"),
                DeckBody = LocalizationService.Text("campaign.interlude.deck_body", "出陣前先確認地形、節奏與首通收益。"),
                DetailLines = detailLines,
                HighlightLine = rewardPreview,
                TerrainLabel = BuildStageBattlefieldLabel(stage.ScenarioId),
                RiskLabel = BuildStageRiskLabel(stage.ScenarioId),
                RewardLabel = rewardPreview,
                RewardIconItemId = rewardItem != null ? rewardItem.ItemId : string.Empty,
                PrimaryActionLabel = LocalizationService.Text("ui.button.begin_battle", "開始戰鬥"),
                SecondaryActionLabel = LocalizationService.Text("ui.button.back_to_camp", "返回軍營"),
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
            if (recommendedStageIndex >= 0)
            {
                CampaignOptionEntryModel recommendedEntry = BuildRecommendedStageEntry(recommendedStageIndex);
                if (recommendedEntry != null)
                {
                    options.Add(recommendedEntry);
                }
            }

            foreach (CampaignUnitState unit in campaignSaveData.Units.OrderBy(unit => unit.UnitId))
            {
                options.Add(new CampaignOptionEntryModel
                {
                    OptionId = "unit:" + unit.UnitId,
                    IconGlyph = "將",
                    Section = LocalizationService.Text("campaign.section.squad", "部隊整備"),
                    Title = GetUnitDisplayName(unit.UnitId),
                    Status = BuildCampUnitStatus(unit),
                    MetricLine = BuildCampUnitMetricLine(unit),
                    Description = BuildCampUnitEquipmentSummary(unit),
                    BadgeText = CanPromote(unit)
                        ? LocalizationService.Text("ui.unlock.promotion", "可升階")
                        : string.Empty,
                    RecommendedReason = CanPromote(unit)
                        ? LocalizationService.Text("campaign.camp.promote_reason", "這名角色目前已可分支轉職，優先處理能立即拉高戰力。")
                        : string.Empty,
                    SortWeight = 30,
                    IsEnabled = true,
                    IsEmphasized = CanPromote(unit),
                });
            }

            options.Add(new CampaignOptionEntryModel
            {
                OptionId = "shop",
                IconGlyph = "商",
                Section = LocalizationService.Text("campaign.section.services", "軍營功能"),
                Title = LocalizationService.Text("camp.shop.title", "軍需官"),
                Status = LocalizationService.Format(
                    "camp.resources",
                    "軍資 {0}  聲望 {1}",
                    campaignSaveData.Inventory.Supplies,
                    campaignSaveData.Inventory.Renown),
                MetricLine = LocalizationService.Format(
                    "camp.shop.metric.camp",
                    "可購買 {0}  |  候選裝備 {1}",
                    campaignProgressionService.GetShopOffers().Count(offer =>
                        campaignSaveData.Inventory.Renown >= offer.RequiredRenown &&
                        campaignSaveData.Inventory.Supplies >= offer.SuppliesCost),
                    campaignProgressionService.GetShopOffers().Count()),
                Description = LocalizationService.Text("camp.shop.desc", "為下一場推進預先補齊更好的武器、護具與坐騎。"),
                BadgeText = campaignProgressionService.GetShopOffers().Any(offer =>
                    campaignSaveData.Inventory.Renown >= offer.RequiredRenown &&
                    campaignSaveData.Inventory.Supplies >= offer.SuppliesCost)
                    ? LocalizationService.Text("campaign.badge.affordable", "可購買")
                    : string.Empty,
                RecommendedReason = campaignProgressionService.GetShopOffers().Any(offer =>
                    campaignSaveData.Inventory.Renown >= offer.RequiredRenown &&
                    campaignSaveData.Inventory.Supplies >= offer.SuppliesCost)
                    ? LocalizationService.Text("campaign.shop.recommended_reason", "你目前的聲望與軍資，已足夠完成至少一項強化。")
                    : string.Empty,
                AvailabilityReason = campaignProgressionService.GetShopOffers().Any(offer =>
                    campaignSaveData.Inventory.Renown >= offer.RequiredRenown &&
                    campaignSaveData.Inventory.Supplies >= offer.SuppliesCost)
                    ? string.Empty
                    : LocalizationService.Text("campaign.shop.availability_reason", "暫時還沒有可直接購買的升級，先推進主線或累積軍資。"),
                SortWeight = 20,
                IsEnabled = true,
                IsEmphasized = campaignProgressionService.GetShopOffers().Any(offer =>
                    campaignSaveData.Inventory.Renown >= offer.RequiredRenown &&
                    campaignSaveData.Inventory.Supplies >= offer.SuppliesCost),
            });
            options.Add(new CampaignOptionEntryModel
            {
                OptionId = "inventory",
                IconGlyph = "藏",
                Section = LocalizationService.Text("campaign.section.services", "軍營功能"),
                Title = LocalizationService.Text("camp.inventory.title", "倉庫"),
                Status = LocalizationService.Format(
                    "camp.inventory.summary",
                    "寶物 {0}  |  存放裝備 {1}",
                    GetSpecialGoodCount(),
                    GetStoredGearCount()),
                MetricLine = LocalizationService.Format(
                    "camp.inventory.metric",
                    "收藏 {0} 項  |  裝備檢視 {1}",
                    campaignSaveData.Inventory.Entries.Count,
                    GetStoredGearCount()),
                Description = LocalizationService.Text("camp.inventory.desc", "檢視整場戰役累積的寶物，以及所有已持有裝備。"),
                SortWeight = 21,
                IsEnabled = true,
            });

            CampaignOptionListModel model = new CampaignOptionListModel
            {
                Eyebrow = LocalizationService.Text("campaign.camp.eyebrow", "行軍營地"),
                Title = LocalizationService.Text("camp.title", "軍營"),
                Body = currentCampMessage,
                ProgressLabel = BuildCampaignProgressSnapshot(),
                HighlightLabel = recommendedStageIndex >= 0
                    ? BuildRecommendedStageSummary(recommendedStageIndex)
                    : string.Empty,
                DeckTitle = LocalizationService.Text("campaign.deck.camp", "營地操作"),
                Options = options,
                PrimaryActionLabel = recommendedStageIndex >= 0
                    ? LocalizationService.Text("ui.button.next_battle", "前往下一戰")
                    : string.Empty,
                SecondaryActionLabel = LocalizationService.Text("ui.button.chapters", "章節選單"),
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
                        IconGlyph = "進",
                        Title = LocalizationService.Format(
                            "camp.promote.to",
                            "轉職為 {0}",
                            LocalizationService.Text(UnitClassCatalog.Get(promotion.TargetClassId).DisplayNameKey, promotion.TargetClassId)),
                        Status = LocalizationService.Text("camp.promote.branch", "升階分支"),
                        MetricLine = BuildPromotionMetricLine(promotion),
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
                    IconGlyph = BuildItemGlyph(item),
                    IconItemId = item.ItemId,
                    Title = LocalizationService.Text(item.NameKey, item.NameFallback),
                    Status = string.Equals(unit.EquipmentLoadout.WeaponId, item.ItemId, StringComparison.Ordinal)
                        ? LocalizationService.Text("camp.equip.current", "已裝備")
                        : LocalizationService.Format("camp.equip.available", "可用 {0}", campaignProgressionService.GetAvailableEquipmentCount(campaignSaveData, item.ItemId, unit.UnitId)),
                    MetricLine = BuildItemMetricLine(item, campaignProgressionService.GetAvailableEquipmentCount(campaignSaveData, item.ItemId, unit.UnitId)),
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
                    IconGlyph = BuildItemGlyph(item),
                    IconItemId = item.ItemId,
                    Title = LocalizationService.Text(item.NameKey, item.NameFallback),
                    Status = string.Equals(unit.EquipmentLoadout.ArmorId, item.ItemId, StringComparison.Ordinal)
                        ? LocalizationService.Text("camp.equip.current", "已裝備")
                        : LocalizationService.Format("camp.equip.available", "可用 {0}", campaignProgressionService.GetAvailableEquipmentCount(campaignSaveData, item.ItemId, unit.UnitId)),
                    MetricLine = BuildItemMetricLine(item, campaignProgressionService.GetAvailableEquipmentCount(campaignSaveData, item.ItemId, unit.UnitId)),
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
                    IconGlyph = BuildItemGlyph(item),
                    IconItemId = item.ItemId,
                    Title = LocalizationService.Text(item.NameKey, item.NameFallback),
                    Status = string.Equals(unit.EquipmentLoadout.MountId, item.ItemId, StringComparison.Ordinal)
                        ? LocalizationService.Text("camp.equip.current", "已裝備")
                        : LocalizationService.Format("camp.equip.available", "可用 {0}", campaignProgressionService.GetAvailableEquipmentCount(campaignSaveData, item.ItemId, unit.UnitId)),
                    MetricLine = BuildItemMetricLine(item, campaignProgressionService.GetAvailableEquipmentCount(campaignSaveData, item.ItemId, unit.UnitId)),
                    Description = BuildItemSummary(item),
                    IsEnabled = true,
                    IsEmphasized = string.Equals(unit.EquipmentLoadout.MountId, item.ItemId, StringComparison.Ordinal),
                });
            }

            CampaignOptionListModel model = new CampaignOptionListModel
            {
                Eyebrow = LocalizationService.Text("campaign.unit_management.eyebrow", "武將整備"),
                Title = GetUnitDisplayName(unit.UnitId),
                Body = BuildUnitManagementBody(unit, message),
                ProgressLabel = BuildCampaignProgressSnapshot(),
                HighlightLabel = BuildCampUnitStatus(unit),
                DeckTitle = LocalizationService.Text("campaign.unit_management.deck", "裝備與升階"),
                Options = options,
                PrimaryActionLabel = LocalizationService.Text("ui.button.back_to_camp", "返回軍營"),
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
                    ShowUnitManagement(unitId, LocalizationService.Text("camp.promote.success", "升階完成。"));
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
                ShowUnitManagement(unitId, LocalizationService.Text("camp.equip.success", "裝備已更新。"));
            }
        }

        private void ShowShop(string message = null)
        {
            List<CampaignOptionEntryModel> options = campaignProgressionService.GetShopOffers()
                .Select(offer => BuildShopEntry(offer))
                .ToList();

            CampaignOptionListModel model = new CampaignOptionListModel
            {
                Eyebrow = LocalizationService.Text("campaign.shop.eyebrow", "營地補給"),
                Title = LocalizationService.Text("camp.shop.title", "軍需官"),
                Body = string.Join(
                    "\n",
                    LocalizationService.Format("camp.resources", "軍資 {0}  聲望 {1}", campaignSaveData.Inventory.Supplies, campaignSaveData.Inventory.Renown),
                    string.IsNullOrWhiteSpace(message) ? LocalizationService.Text("camp.shop.desc", "為下一場推進預先補齊更好的武器、護具與坐騎。") : message),
                ProgressLabel = BuildCampaignProgressSnapshot(),
                HighlightLabel = LocalizationService.Format("camp.resources", "軍資 {0}  聲望 {1}", campaignSaveData.Inventory.Supplies, campaignSaveData.Inventory.Renown),
                DeckTitle = LocalizationService.Text("campaign.shop.deck", "軍需清單"),
                Options = options,
                PrimaryActionLabel = LocalizationService.Text("ui.button.back_to_camp", "返回軍營"),
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
                IconGlyph = BuildItemGlyph(item),
                IconItemId = item != null ? item.ItemId : string.Empty,
                Title = LocalizationService.Text(item.NameKey, item.NameFallback),
                Status = renownUnlocked
                    ? LocalizationService.Format("camp.shop.cost", "花費 {0} 軍資", offer.SuppliesCost)
                    : LocalizationService.Format("camp.shop.renown_gate", "需要聲望 {0}", offer.RequiredRenown),
                MetricLine = LocalizationService.Format("camp.shop.metric", "持有 {0}  |  聲望需求 {1}", campaignSaveData.Inventory.GetQuantity(offer.ItemId), offer.RequiredRenown),
                Description = BuildItemSummary(item) + " | " +
                              LocalizationService.Format("camp.shop.owned", "持有 {0}", campaignSaveData.Inventory.GetQuantity(offer.ItemId)),
                AvailabilityReason = renownUnlocked && canAfford
                    ? string.Empty
                    : renownUnlocked
                        ? LocalizationService.Text("camp.shop.availability.supplies", "軍資不足，暫時無法購買。")
                        : LocalizationService.Text("camp.shop.availability.renown", "聲望尚未達標，先推進主線即可解鎖。"),
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
                ShowShop(LocalizationService.Text("camp.shop.success", "購買完成。"));
            }
        }

        private void ShowInventory(string message = null)
        {
            List<CampaignOptionEntryModel> options = campaignSaveData.Inventory.Entries
                .OrderByDescending(entry =>
                {
                    ItemDefinition definition = ItemCatalog.Get(entry.ItemId);
                    return definition != null && definition.IsTreasure ? 1 : 0;
                })
                .ThenBy(entry => entry.ItemId, StringComparer.Ordinal)
                .Select(entry =>
                {
                    ItemDefinition definition = ItemCatalog.Get(entry.ItemId);
                    return new CampaignOptionEntryModel
                    {
                        OptionId = string.Empty,
                        IconGlyph = BuildItemGlyph(definition),
                        IconItemId = definition != null ? definition.ItemId : string.Empty,
                        Title = definition != null ? LocalizationService.Text(definition.NameKey, definition.NameFallback) : entry.ItemId,
                        Status = LocalizationService.Format("camp.inventory.count", "x{0}", entry.Quantity),
                        MetricLine = BuildInventoryMetricLine(definition, entry.Quantity),
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
                    IconGlyph = "藏",
                    Title = LocalizationService.Text("camp.inventory.empty.title", "倉庫空無一物"),
                    Status = string.Empty,
                    Description = LocalizationService.Text("camp.inventory.empty.desc", "目前還沒有存放任何物品。"),
                    IsEnabled = false,
                });
            }

            CampaignOptionListModel model = new CampaignOptionListModel
            {
                Eyebrow = LocalizationService.Text("campaign.inventory.eyebrow", "戰役倉儲"),
                Title = LocalizationService.Text("camp.inventory.title", "倉庫"),
                Body = string.IsNullOrWhiteSpace(message)
                    ? LocalizationService.Text("camp.inventory.desc", "檢視整場戰役累積的寶物，以及所有已持有裝備。")
                    : message,
                ProgressLabel = BuildCampaignProgressSnapshot(),
                HighlightLabel = LocalizationService.Format("camp.inventory.summary", "寶物 {0}  |  存放裝備 {1}", GetSpecialGoodCount(), GetStoredGearCount()),
                DeckTitle = LocalizationService.Text("campaign.inventory.deck", "藏品與裝備"),
                Options = options,
                PrimaryActionLabel = LocalizationService.Text("ui.button.back_to_camp", "返回軍營"),
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
            BattleScenarioData scenario = campaignProgressionService.PrepareScenario(
                BattleScenarioCatalog.CreateScenario(stage.ScenarioId),
                campaignSaveData);
            ItemDefinition rewardItem = ItemCatalog.Get(scenario.RewardBundle.RewardItemId);

            string statusKey;
            string statusFallback;
            if (!unlocked)
            {
                statusKey = "campaign.stage.locked";
                statusFallback = "未解鎖";
            }
            else if (recommended && !cleared)
            {
                statusKey = "campaign.stage.recommended";
                statusFallback = "下一戰";
            }
            else if (cleared)
            {
                statusKey = "campaign.stage.cleared";
                statusFallback = "已通關";
            }
            else
            {
                statusKey = "campaign.stage.unlocked";
                statusFallback = "可出戰";
            }

            return new CampaignStageEntryModel
            {
                StageIndex = stageIndex,
                Title = LocalizationService.Text(stage.ChapterTitleKey, stage.ChapterTitleFallback),
                Status = LocalizationService.Text(statusKey, statusFallback),
                Description = LocalizationService.Text(stage.InterludeIntroKey, stage.InterludeIntroFallback),
                BattlefieldLabel = BuildStageBattlefieldLabel(stage.ScenarioId),
                RewardLabel = rewardItem != null
                    ? LocalizationService.Format("campaign.stage.reward_preview", "首通寶物：{0}", LocalizationService.Text(rewardItem.NameKey, rewardItem.NameFallback))
                    : string.Empty,
                RewardIconItemId = rewardItem != null ? rewardItem.ItemId : string.Empty,
                DurationLabel = BuildStageDurationLabel(scenario),
                IsUnlocked = unlocked,
                IsCleared = cleared,
                IsRecommended = recommended,
                BadgeText = recommended
                    ? LocalizationService.Text("campaign.badge.main_story", "主線")
                    : cleared
                        ? LocalizationService.Text("campaign.badge.cleared", "已通關")
                        : unlocked
                            ? LocalizationService.Text("campaign.badge.unlocked", "可出戰")
                            : LocalizationService.Text("campaign.badge.locked", "未解鎖"),
                PriorityBadge = recommended
                    ? LocalizationService.Text("campaign.badge.priority_story", "主線優先")
                    : cleared
                        ? LocalizationService.Text("campaign.badge.replay", "回放")
                        : unlocked
                            ? LocalizationService.Text("campaign.badge.open_front", "已開戰線")
                            : string.Empty,
                RecommendedReason = recommended
                    ? LocalizationService.Text("campaign.stage.recommended_reason", "這是目前最快回到主線節奏的路線。")
                    : cleared
                        ? LocalizationService.Text("campaign.stage.replay_reason", "可用來補經驗，或測試新的編隊與裝備搭配。")
                        : string.Empty,
                PreviewThemeId = stage.ScenarioId,
                SortWeight = recommended ? 0 : unlocked ? 10 + stageIndex : 100 + stageIndex,
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
            string feedbackPrompt = string.Equals(result.ScenarioId, BattleScenarioCatalog.GuangzongScenarioId, StringComparison.Ordinal)
                ? BuildFirstBattleFeedbackPrompt()
                : string.Empty;
            ShowCampHub(string.Join("\n\n", new[] { narrative, battleSummary, rewardSummary, feedbackPrompt }.Where(text => !string.IsNullOrWhiteSpace(text))));
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

            List<string> parts = new List<string>();
            if (resolution.GrantedStageReward)
            {
                parts.Add(LocalizationService.Format("campaign.reward.supplies", "軍資 +{0}", resolution.GrantedSupplies));
                parts.Add(LocalizationService.Format("campaign.reward.renown", "聲望 +{0}", resolution.GrantedRenown));
                if (!string.IsNullOrWhiteSpace(resolution.GrantedItemId))
                {
                    ItemDefinition item = ItemCatalog.Get(resolution.GrantedItemId);
                    parts.Add(LocalizationService.Format(
                        "campaign.reward.item",
                        "寶物：{0}",
                        item != null ? LocalizationService.Text(item.NameKey, item.NameFallback) : resolution.GrantedItemId));
                }
            }

            if (resolution.GrantedBonusRewardLines != null && resolution.GrantedBonusRewardLines.Count > 0)
            {
                parts.AddRange(resolution.GrantedBonusRewardLines);
            }

            if (resolution.RecruitedUnitIds != null && resolution.RecruitedUnitIds.Count > 0)
            {
                parts.Add(LocalizationService.Format(
                    "campaign.reward.recruit",
                    "加入麾下：{0}",
                    string.Join("、", resolution.RecruitedUnitIds.Select(GetUnitDisplayName))));
            }

            if (parts.Count == 0)
            {
                return LocalizationService.Text("campaign.replay.reward_info", "此章節首通獎勵已領取，回放仍可獲得戰鬥經驗。");
            }

            return string.Join(" | ", parts);
        }

        private string BuildCampaignProgressSnapshot()
        {
            return string.Join(
                "\n",
                new[]
                {
                    LocalizationService.Format("campaign.snapshot.chapters", "已解鎖章節 {0}/{1}", campaignDirector.Progress.UnlockedStageIndex + 1, campaignDirector.StageCount),
                    BuildLastBattleSnapshot(),
                    LocalizationService.Format("camp.resources", "軍資 {0}  聲望 {1}", campaignSaveData.Inventory.Supplies, campaignSaveData.Inventory.Renown),
                }.Where(text => !string.IsNullOrWhiteSpace(text)));
        }

        private string BuildCampBody(string message, int recommendedStageIndex)
        {
            string recommendedSummary = recommendedStageIndex >= 0
                ? BuildRecommendedStageSummary(recommendedStageIndex)
                : string.Empty;
            return string.Join(
                "\n\n",
                new[]
                {
                    message,
                    recommendedSummary,
                    LocalizationService.Format("camp.resources", "軍資 {0}  聲望 {1}", campaignSaveData.Inventory.Supplies, campaignSaveData.Inventory.Renown),
                }.Where(text => !string.IsNullOrWhiteSpace(text)));
        }

        private string BuildCampUnitStatus(CampaignUnitState unit)
        {
            string status = LocalizationService.Format(
                "camp.unit.status",
                "{0}  等級 {1}",
                LocalizationService.Text(UnitClassCatalog.Get(unit.ClassId).DisplayNameKey, unit.ClassId),
                unit.Level);
            return CanPromote(unit)
                ? status + " | " + LocalizationService.Text("ui.unlock.promotion", "可升階")
                : status;
        }

        private string BuildCampUnitEquipmentSummary(CampaignUnitState unit)
        {
            ItemDefinition weapon = ItemCatalog.Get(unit.EquipmentLoadout.WeaponId);
            ItemDefinition armor = ItemCatalog.Get(unit.EquipmentLoadout.ArmorId);
            ItemDefinition mount = ItemCatalog.Get(unit.EquipmentLoadout.MountId);
            return LocalizationService.Format(
                "camp.unit.equipment",
                "武器：{0} | 護具：{1} | 坐騎：{2}",
                weapon != null ? LocalizationService.Text(weapon.NameKey, weapon.NameFallback) : LocalizationService.Text("camp.equip.none", "無"),
                armor != null ? LocalizationService.Text(armor.NameKey, armor.NameFallback) : LocalizationService.Text("camp.equip.none", "無"),
                mount != null ? LocalizationService.Text(mount.NameKey, mount.NameFallback) : LocalizationService.Text("camp.equip.none", "無"));
        }

        private string BuildUnitManagementBody(CampaignUnitState unit, string message)
        {
            ItemDefinition mount = ItemCatalog.Get(unit.EquipmentLoadout.MountId);
            int moveValue = unit.MoveRange + (mount != null ? mount.MoveBonus : 0);
            List<string> parts = new List<string>
            {
                LocalizationService.Format(
                    "camp.unit.detail",
                    "{0}  等級 {1}  經驗 {2}/{3}\n攻 {4}  防 {5}  生命 {6}  士氣 {7}  移動 {8}",
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
                    "目前已可轉職。下方高亮的升階卡片，就是這名角色的轉職入口。"));
            }
            else if (unit != null && unit.HasPromoted)
            {
                parts.Add(LocalizationService.Text(
                    "camp.promote.done_hint",
                    "這名角色已完成升階，之後不能再更換分支。"));
            }
            else if (unit != null)
            {
                parts.Add(LocalizationService.Format(
                    "camp.promote.locked_hint",
                    "升階會在等級 10 解鎖。目前等級：{0}。",
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
                "+{0} 生命  +{1} 攻  +{2} 防  +{3} 士氣",
                definition.HpBonus,
                definition.AttackBonus,
                definition.DefenseBonus,
                definition.ManaBonus);
            return string.Join(
                "\n",
                LocalizationService.Text("camp.promote.tap_hint", "點擊後立即完成升階。"),
                statSummary,
                LocalizationService.Text("ui.label.passive", "被動戰法") + "：" + passiveName,
                LocalizationService.Text("ui.label.active", "主動戰技") + "：" + activeName);
        }

        private string BuildItemSummary(ItemDefinition item)
        {
            List<string> statParts = new List<string>();
            if (item.AttackBonus != 0)
            {
                statParts.Add(LocalizationService.Format("camp.item.attack", "攻擊 +{0}", item.AttackBonus));
            }

            if (item.DefenseBonus != 0)
            {
                statParts.Add(LocalizationService.Format("camp.item.defense", "防禦 +{0}", item.DefenseBonus));
            }

            if (item.HpBonus != 0)
            {
                statParts.Add(LocalizationService.Format("camp.item.hp", "生命 +{0}", item.HpBonus));
            }

            if (item.MoveBonus != 0)
            {
                statParts.Add(LocalizationService.Format("camp.item.move", "移動 +{0}", item.MoveBonus));
            }

            string description = LocalizationService.Text(item.DescriptionKey, item.DescriptionFallback);
            string treasureEffect = BuildTreasureEffectSummary(item);
            string summary = statParts.Count == 0 ? description : string.Join("  ", statParts) + " | " + description;
            return string.IsNullOrWhiteSpace(treasureEffect) ? summary : summary + " | " + treasureEffect;
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

        private bool ShouldEnableFirstBattleOnboarding(string scenarioId)
        {
            return string.Equals(scenarioId, BattleScenarioCatalog.GuangzongScenarioId, StringComparison.Ordinal) &&
                   campaignSaveData != null &&
                   !campaignSaveData.Progress.HasCompletedFirstBattleOnboarding &&
                   !campaignSaveData.Progress.HasSkippedOnboarding;
        }

        private string BuildCampUnitMetricLine(CampaignUnitState unit)
        {
            if (unit == null)
            {
                return string.Empty;
            }

            return LocalizationService.Format(
                "camp.unit.metric",
                "經驗 {0}/{1}  |  移動 {2}  |  射程 {3}",
                unit.CurrentExp,
                ExperienceSystem.GetRequiredExpForLevel(unit.Level),
                unit.MoveRange,
                unit.AttackRange);
        }

        private string BuildPromotionMetricLine(PromotionDefinition definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            return LocalizationService.Format(
                "camp.promote.metric",
                "目標兵種：{0}  |  被動與主動戰技同步更新",
                LocalizationService.Text(UnitClassCatalog.Get(definition.TargetClassId).DisplayNameKey, definition.TargetClassId));
        }

        private string BuildItemMetricLine(ItemDefinition item, int availableCount)
        {
            if (item == null)
            {
                return string.Empty;
            }

            string category = item.Category == ItemCategory.Weapon
                ? "武器"
                : item.Category == ItemCategory.Armor
                    ? "護具"
                    : item.Category == ItemCategory.Mount
                        ? "坐騎"
                        : "寶物";
            return LocalizationService.Format("camp.item.metric", "{0}  |  可用 {1}", category, availableCount);
        }

        private string BuildInventoryMetricLine(ItemDefinition definition, int quantity)
        {
            if (definition == null)
            {
                return LocalizationService.Format("camp.inventory.count", "x{0}", quantity);
            }

            string category = definition.IsTreasure
                ? "寶物"
                : definition.Category == ItemCategory.Weapon
                    ? "武器"
                    : definition.Category == ItemCategory.Armor
                        ? "護具"
                        : definition.Category == ItemCategory.Mount
                            ? "坐騎"
                            : "物資";
            return LocalizationService.Format("camp.inventory.metric_line", "{0}  |  持有 {1}", category, quantity);
        }

        private static string BuildItemGlyph(ItemDefinition item)
        {
            if (item == null)
            {
                return "備";
            }

            switch (item.Category)
            {
                case ItemCategory.Weapon:
                    return "刃";
                case ItemCategory.Armor:
                    return "甲";
                case ItemCategory.Mount:
                    return "騎";
                case ItemCategory.SpecialGood:
                    return item.IsTreasure ? "寶" : "資";
                default:
                    return "備";
            }
        }

        private void HandleFirstBattleOnboardingResolved(bool skipped)
        {
            if (campaignSaveData == null)
            {
                return;
            }

            if (skipped)
            {
                campaignSaveData.Progress.MarkFirstBattleOnboardingSkipped();
            }
            else
            {
                campaignSaveData.Progress.MarkFirstBattleOnboardingCompleted();
            }

            campaignSaveRepository.Save(campaignSaveData);
        }

        private List<CampaignOptionEntryModel> BuildFirstLaunchOptionEntries(int recommendedStageIndex)
        {
            CampaignStageDefinition stage = recommendedStageIndex >= 0 ? campaignDirector.GetStage(recommendedStageIndex) : null;
            return new List<CampaignOptionEntryModel>
            {
                new CampaignOptionEntryModel
                {
                    IconGlyph = "薦",
                    Section = LocalizationService.Text("campaign.section.recommended", "推薦路徑"),
                    Title = stage != null ? LocalizationService.Text(stage.ChapterTitleKey, stage.ChapterTitleFallback) : LocalizationService.Text("ui.button.begin_battle", "開始戰鬥"),
                    Status = LocalizationService.Text("campaign.badge.demo", "引導示範"),
                    MetricLine = stage != null ? BuildStageBattlefieldLabel(stage.ScenarioId) : string.Empty,
                    Description = LocalizationService.Text("campaign.launch.demo_card", "從廣宗簡報與首戰開始，最快理解整套戰役節奏。"),
                    BadgeText = LocalizationService.Text("campaign.badge.recommended", "推薦"),
                    RecommendedReason = LocalizationService.Text("campaign.launch.demo_reason", "這條路線會一次教會你移動、戰鬥、目標、結算與回營流程。"),
                    SortWeight = 0,
                    IsEnabled = false,
                    IsEmphasized = true,
                },
                new CampaignOptionEntryModel
                {
                    IconGlyph = "覽",
                    Section = LocalizationService.Text("campaign.section.overview", "你會體驗到"),
                    Title = LocalizationService.Text("campaign.launch.value_title", "串連式的三國戰棋戰役"),
                    Status = LocalizationService.Text("campaign.launch.value_status", "劇情、戰術、養成同步推進"),
                    MetricLine = LocalizationService.Text("campaign.launch.value_metric", "九章主線  |  名將招募  |  戰利品回營"),
                    Description = LocalizationService.Text("campaign.launch.value_body", "你將指揮劉備軍穿過相連戰役，招募名將並把戰利品帶回營地整備。"),
                    SortWeight = 10,
                    IsEnabled = false,
                },
                new CampaignOptionEntryModel
                {
                    IconGlyph = "控",
                    Section = LocalizationService.Text("campaign.section.controls", "操作方式"),
                    Title = LocalizationService.Text("campaign.launch.controls_title", "滑鼠即可操作"),
                    Status = LocalizationService.Text("campaign.launch.controls_status", "點選武將 → 移動 → 攻擊 / 技能 / 待命"),
                    MetricLine = LocalizationService.Text("campaign.launch.controls_metric", "藍格移動  |  紅格攻擊  |  下方指令板"),
                    Description = LocalizationService.Text("campaign.launch.controls_body", "藍格代表移動範圍，紅格是攻擊影響範圍，指令板會告訴你行動可不可用。"),
                    SortWeight = 20,
                    IsEnabled = false,
                },
            };
        }

        private List<CampaignOptionEntryModel> BuildContinueOptionEntries()
        {
            int recommendedStageIndex = campaignDirector.GetRecommendedStageIndex();
            CampaignStageDefinition stage = recommendedStageIndex >= 0 ? campaignDirector.GetStage(recommendedStageIndex) : null;
            return new List<CampaignOptionEntryModel>
            {
                new CampaignOptionEntryModel
                {
                    IconGlyph = "進",
                    Section = LocalizationService.Text("campaign.section.progress", "戰役快照"),
                    Title = LocalizationService.Text("campaign.launch.progress_title", "目前戰役進度"),
                    Status = LocalizationService.Text("campaign.launch.progress_status", "自動存檔可直接續戰"),
                    MetricLine = LocalizationService.Format(
                        "campaign.launch.progress_metric",
                        "已解鎖章節 {0}/{1}  |  軍資 {2}  聲望 {3}",
                        campaignDirector.Progress.UnlockedStageIndex + 1,
                        campaignDirector.StageCount,
                        campaignSaveData.Inventory.Supplies,
                        campaignSaveData.Inventory.Renown),
                    Description = stage != null
                        ? LocalizationService.Format("campaign.launch.progress_body", "推薦下一戰：{0}", LocalizationService.Text(stage.ChapterTitleKey, stage.ChapterTitleFallback))
                        : LocalizationService.Text("campaign.launch.progress_fallback", "回到上次的軍營狀態，直接選擇下一步行動。"),
                    BadgeText = stage != null ? LocalizationService.Text("campaign.badge.recommended", "推薦") : string.Empty,
                    RecommendedReason = stage != null ? LocalizationService.Text("campaign.stage.recommended_reason", "這是目前最快回到主線節奏的路線。") : string.Empty,
                    SortWeight = 0,
                    IsEnabled = false,
                },
                new CampaignOptionEntryModel
                {
                    IconGlyph = "控",
                    Section = LocalizationService.Text("campaign.section.controls", "操作方式"),
                    Title = LocalizationService.Text("campaign.launch.controls_title", "滑鼠即可操作"),
                    Status = LocalizationService.Text("campaign.launch.controls_status", "點選武將 → 移動 → 攻擊 / 技能 / 待命"),
                    MetricLine = LocalizationService.Text("campaign.launch.controls_metric", "藍格移動  |  紅格攻擊  |  下方指令板"),
                    Description = LocalizationService.Text("campaign.launch.controls_body", "藍格代表移動範圍，紅格是攻擊影響範圍，指令板會告訴你行動可不可用。"),
                    SortWeight = 10,
                    IsEnabled = false,
                },
            };
        }

        private CampaignOptionEntryModel BuildRecommendedStageEntry(int stageIndex)
        {
            CampaignStageDefinition stage = campaignDirector.GetStage(stageIndex);
            if (stage == null)
            {
                return null;
            }

            BattleScenarioData scenario = campaignProgressionService.PrepareScenario(
                BattleScenarioCatalog.CreateScenario(stage.ScenarioId),
                campaignSaveData);
            ItemDefinition rewardItem = ItemCatalog.Get(scenario.RewardBundle.RewardItemId);
            string rewardPreview = rewardItem != null && !campaignSaveData.Progress.IsRewardClaimed(stage.ScenarioId)
                ? LocalizationService.Format("campaign.stage.reward_preview", "首通寶物：{0}", LocalizationService.Text(rewardItem.NameKey, rewardItem.NameFallback))
                : LocalizationService.Text("campaign.replay.reward_info", "此章節首通獎勵已領取，回放仍可獲得戰鬥經驗。");

            return new CampaignOptionEntryModel
            {
                OptionId = string.Empty,
                IconGlyph = "薦",
                Section = LocalizationService.Text("campaign.section.recommended", "推薦路徑"),
                Title = LocalizationService.Text(stage.ChapterTitleKey, stage.ChapterTitleFallback),
                Status = LocalizationService.Format(
                    "campaign.camp.recommended_status",
                    "建議等級 {0} | {1}",
                    scenario.RecommendedLevel,
                    FormatScenarioVariantTag(scenario.ScenarioVariantTag)),
                MetricLine = BuildStageBattlefieldLabel(stage.ScenarioId) + " | " + BuildStageDurationLabel(scenario),
                Description = rewardPreview,
                BadgeText = LocalizationService.Text("campaign.badge.recommended", "推薦"),
                RecommendedReason = LocalizationService.Text("campaign.camp.recommended_reason", "可直接使用下方主按鈕，立刻銜接下一場主線戰鬥。"),
                SortWeight = 0,
                IsEnabled = false,
                IsEmphasized = true,
            };
        }

        private string BuildRecommendedStageSummary(int stageIndex)
        {
            CampaignStageDefinition stage = stageIndex >= 0 ? campaignDirector.GetStage(stageIndex) : null;
            if (stage == null)
            {
                return string.Empty;
            }

            BattleScenarioData scenario = campaignProgressionService.PrepareScenario(
                BattleScenarioCatalog.CreateScenario(stage.ScenarioId),
                campaignSaveData);
            return LocalizationService.Format(
                "campaign.camp.recommended_summary",
                "推薦下一戰：{0} | 建議等級 {1} | {2}",
                LocalizationService.Text(stage.ChapterTitleKey, stage.ChapterTitleFallback),
                scenario.RecommendedLevel,
                FormatScenarioVariantTag(scenario.ScenarioVariantTag));
        }

        private string BuildStageBattlefieldLabel(string scenarioId)
        {
            switch (scenarioId)
            {
                case BattleScenarioCatalog.GuangzongScenarioId:
                    return LocalizationService.Text("campaign.stage.theme.guangzong", "戰場定位：三路平原突破");
                case BattleScenarioCatalog.BowangpoScenarioId:
                    return LocalizationService.Text("campaign.stage.theme.bowangpo", "戰場定位：林地伏擊與火線");
                case BattleScenarioCatalog.ChangbanScenarioId:
                    return LocalizationService.Text("campaign.stage.theme.changban", "戰場定位：水線斷後戰");
                case BattleScenarioCatalog.JiangxiaScenarioId:
                    return LocalizationService.Text("campaign.stage.theme.jiangxia", "戰場定位：三橋渡河強攻");
                case BattleScenarioCatalog.JiamengPassScenarioId:
                    return LocalizationService.Text("campaign.stage.theme.jiameng", "戰場定位：關隘門線守勢");
                case BattleScenarioCatalog.BaishuiScenarioId:
                    return LocalizationService.Text("campaign.stage.theme.baishui", "戰場定位：雙渡口與斷橋壓迫");
                case BattleScenarioCatalog.MianzhuScenarioId:
                    return LocalizationService.Text("campaign.stage.theme.mianzhu", "戰場定位：雙層門線與火場突破");
                case BattleScenarioCatalog.LuochengScenarioId:
                    return LocalizationService.Text("campaign.stage.theme.luocheng", "戰場定位：破門後的城街絞殺");
                case BattleScenarioCatalog.YangpingScenarioId:
                    return LocalizationService.Text("campaign.stage.theme.yangping", "戰場定位：山隘與落石陷阱");
                case BattleScenarioCatalog.TiandangScenarioId:
                    return LocalizationService.Text("campaign.stage.theme.tiandang", "戰場定位：雙信標夜襲與側翼警報");
                case BattleScenarioCatalog.HanshuiScenarioId:
                    return LocalizationService.Text("campaign.stage.theme.hanshui", "戰場定位：河岸反擊戰");
                case BattleScenarioCatalog.DingjunScenarioId:
                    return LocalizationService.Text("campaign.stage.theme.dingjun", "戰場定位：山脊突擊");
                default:
                    return LocalizationService.Text("campaign.stage.theme.default", "戰場定位：綜合正面壓力");
            }
        }

        private string BuildStageRiskLabel(string scenarioId)
        {
            switch (scenarioId)
            {
                case BattleScenarioCatalog.GuangzongScenarioId:
                    return LocalizationService.Text("campaign.stage.risk.guangzong", "風險：中央火線與後段兩翼包夾。");
                case BattleScenarioCatalog.JiangxiaScenarioId:
                    return LocalizationService.Text("campaign.stage.risk.jiangxia", "風險：橋頭卡位與兩岸交叉火力。");
                case BattleScenarioCatalog.BaishuiScenarioId:
                    return LocalizationService.Text("campaign.stage.risk.baishui", "風險：下橋一斷就會被後追騎兵逼出上路。");
                case BattleScenarioCatalog.MianzhuScenarioId:
                    return LocalizationService.Text("campaign.stage.risk.mianzhu", "風險：破門後中央會立刻變成火線殺區。");
                case BattleScenarioCatalog.LuochengScenarioId:
                    return LocalizationService.Text("campaign.stage.risk.luocheng", "風險：城門殺區之後還有第二層街巷防線。");
                case BattleScenarioCatalog.TiandangScenarioId:
                    return LocalizationService.Text("campaign.stage.risk.tiandang", "風險：若信標沒先壓掉，敵軍側翼會被整段打開。");
                case BattleScenarioCatalog.ChangbanScenarioId:
                    return LocalizationService.Text("campaign.stage.risk.changban", "風險：保護後隊的同時敵軍壓力會越滾越快。");
                default:
                    return LocalizationService.Text("campaign.stage.risk.default", "風險：若過早散陣，敵軍會集中壓上。");
            }
        }

        private string BuildStageDurationLabel(BattleScenarioData scenario)
        {
            if (scenario == null || scenario.Stage == null)
            {
                return string.Empty;
            }

            int footprint = scenario.Stage.Width * scenario.Stage.Height;
            if (footprint >= 280)
            {
                return LocalizationService.Text("campaign.stage.duration.long", "預估戰鬥長度：16 到 22 回合");
            }

            if (footprint >= 200)
            {
                return LocalizationService.Text("campaign.stage.duration.medium", "預估戰鬥長度：12 到 18 回合");
            }

            return LocalizationService.Text("campaign.stage.duration.short", "預估戰鬥長度：8 到 14 回合");
        }

        private string BuildLastBattleSnapshot()
        {
            BattleResultSummary summary = campaignSaveData != null ? campaignSaveData.Progress.LastBattleResult : null;
            if (summary == null || string.IsNullOrWhiteSpace(summary.ScenarioId))
            {
                return string.Empty;
            }

            BattleScenarioData scenario = BattleScenarioCatalog.CreateScenario(summary.ScenarioId);
            string scenarioName = scenario != null
                ? LocalizationService.Text(scenario.ScenarioNameKey, scenario.ScenarioName)
                : summary.ScenarioId;
            return LocalizationService.Format("campaign.snapshot.last_battle", "Last battle: {0}", scenarioName);
        }

        private string BuildFirstBattleFeedbackPrompt()
        {
            return string.Join(
                "\n",
                LocalizationService.Text("campaign.feedback.header", "Share Demo Feedback"),
                LocalizationService.Text("campaign.feedback.new_player", "New players: Did you understand what to click and what the first objective was?"),
                LocalizationService.Text("campaign.feedback.strategy", "Strategy players: Were attack, skill, and threat outcomes readable before you committed?"),
                LocalizationService.Text("campaign.feedback.stakeholder", "Collaborators: Did the intro, first battle, and return to camp clearly communicate the product pitch?"));
        }

        private int GetSpecialGoodCount()
        {
            return campaignSaveData.Inventory.Entries.Count(entry =>
            {
                ItemDefinition definition = ItemCatalog.Get(entry.ItemId);
                return definition != null && definition.IsTreasure;
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

        private static string BuildTreasureEffectSummary(ItemDefinition item)
        {
            if (item == null || !item.IsTreasure)
            {
                return string.Empty;
            }

            switch (item.TreasureEffect)
            {
                case TreasureEffectType.SkillDamageBonus:
                    return LocalizationService.Text("treasure.effect.skill_damage", "Treasure Effect: skill damage +2");
                case TreasureEffectType.StatusDurationBonus:
                    return LocalizationService.Text("treasure.effect.status_duration", "Treasure Effect: applied status duration +1");
                case TreasureEffectType.GuardOnLowHp:
                    return LocalizationService.Text("treasure.effect.guard_low_hp", "Treasure Effect: gain Guarded at low HP");
                case TreasureEffectType.IgnoreHazardTick:
                    return LocalizationService.Text("treasure.effect.ignore_hazard", "Treasure Effect: ignores hazard end-turn damage");
                case TreasureEffectType.FortHealingBonus:
                    return LocalizationService.Text("treasure.effect.fort_heal", "Treasure Effect: fort healing +2");
                case TreasureEffectType.MovePlusOneOnFirstThreeTurns:
                    return LocalizationService.Text("treasure.effect.first_turn_move", "Treasure Effect: MOVE +1 during the first 3 turns");
                default:
                    return string.Empty;
            }
        }

        private static string FormatScenarioVariantTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || string.Equals(tag, "Normal", StringComparison.Ordinal))
            {
                return LocalizationService.Text("campaign.variant.normal", "Normal");
            }

            const string ReplayPrefix = "Replay ";
            if (tag.StartsWith(ReplayPrefix, StringComparison.Ordinal))
            {
                return LocalizationService.Format(
                    "campaign.variant.replay",
                    "Replay {0}",
                    tag.Substring(ReplayPrefix.Length));
            }

            return tag;
        }
    }
}
