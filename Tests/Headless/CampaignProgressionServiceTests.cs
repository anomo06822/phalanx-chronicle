using System;
using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Core;
using Xunit;

namespace PhalanxChronicle.Headless.Tests
{
    public sealed class CampaignProgressionServiceTests
    {
        [Fact]
        public void CreateNewSave_SeedsRosterAndStoryEquipment()
        {
            CampaignProgressionService service = new CampaignProgressionService();

            CampaignSaveData save = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());

            Assert.Equal(CampaignCatalog.LiuBeiLegendCampaignId, save.CampaignId);
            Assert.Equal(4, save.Units.Count);
            Assert.Equal(4, save.Version);
            Assert.Equal("vermilion-jian", save.GetUnit("player-liu-bei").EquipmentLoadout.WeaponId);
            Assert.Equal("commander-travel-cloak", save.GetUnit("player-liu-bei").EquipmentLoadout.ArmorId);
            Assert.Equal(string.Empty, save.GetUnit("player-liu-bei").EquipmentLoadout.MountId);
            Assert.Equal(1, save.Inventory.GetQuantity("vermilion-jian"));
            Assert.Equal(2, save.Inventory.GetQuantity("iron-crescent-glaive"));
            Assert.Equal(2, save.Inventory.GetQuantity("guardian-scale-vest"));
            Assert.Equal(1, save.Inventory.GetQuantity("featherback-war-bow"));
            Assert.Equal(1, save.Inventory.GetQuantity("ranger-hunt-coat"));
            Assert.Equal(0, save.Progress.GetClearCount(BattleScenarioCatalog.GuangzongScenarioId));
        }

        [Fact]
        public void PrepareScenario_StripsFirstClearRewardsAfterTheyAreClaimed()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignSaveData save = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());
            BattleScenarioData baseScenario = BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.BowangpoScenarioId);

            BattleScenarioData firstClearScenario = service.PrepareScenario(baseScenario, save);
            save.Progress.MarkRewardClaimed(BattleScenarioCatalog.BowangpoScenarioId);
            BattleScenarioData replayScenario = service.PrepareScenario(baseScenario, save);
            ItemDefinition firstClearReward = ItemCatalog.Get(firstClearScenario.RewardBundle.RewardItemId);

            Assert.Equal(150, firstClearScenario.RewardBundle.Supplies);
            Assert.Equal(2, firstClearScenario.RewardBundle.Renown);
            Assert.Equal("bowang-fire-token", firstClearScenario.RewardBundle.RewardItemId);
            Assert.Contains("player-zhuge-liang", firstClearScenario.RewardBundle.RecruitUnitIds);
            Assert.NotNull(firstClearReward);
            Assert.True(firstClearReward.IsTreasure);
            Assert.Equal(TreasureEffectType.SkillDamageBonus, firstClearReward.TreasureEffect);
            Assert.Equal(0, replayScenario.RewardBundle.Supplies);
            Assert.Equal(0, replayScenario.RewardBundle.Renown);
            Assert.Equal(string.Empty, replayScenario.RewardBundle.RewardItemId);
            Assert.Empty(replayScenario.RewardBundle.RecruitUnitIds);
        }

        [Fact]
        public void PrepareScenario_AppliesReplayTierFromClearCountAndRosterLevels()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignSaveData save = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());
            foreach (CampaignUnitState unit in save.Units)
            {
                unit.SyncFromBattle(CreateResolvedRuntime(unit, 9), 0, 0, 0);
            }

            save.Progress.MarkCleared(BattleScenarioCatalog.GuangzongScenarioId);
            save.Progress.MarkCleared(BattleScenarioCatalog.GuangzongScenarioId);
            save.Progress.MarkCleared(BattleScenarioCatalog.GuangzongScenarioId);

            BattleScenarioData prepared = service.PrepareScenario(
                BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.GuangzongScenarioId),
                save);
            UnitDefinitionData zhangBao = prepared.Stage.UnitSpawns.Single(spawn => spawn.Definition.Id == "enemy-zhang-bao").Definition;

            Assert.Equal(5, prepared.ReplayDifficultyTier);
            Assert.Equal("Replay V", prepared.ScenarioVariantTag);
            Assert.Equal(42, zhangBao.MaxHp);
            Assert.Equal(15, zhangBao.Attack);
            Assert.Equal(6, zhangBao.Defense);
        }

        [Fact]
        public void FinalizeBattle_GrantsStageRewardOnlyOnce_AndSyncsProgression()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignSaveData save = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());
            BattleScenarioData scenario = service.PrepareScenario(
                BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.GuangzongScenarioId),
                save);
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            UnitRuntimeState liuBei = simulation.Context.GetUnit("player-liu-bei");
            liuBei.AddBonusExperience(1000);
            BattleResultSummary summary = new BattleResultSummary(
                BattleScenarioCatalog.GuangzongScenarioId,
                TurnSide.Player,
                4,
                save.Units.Select(unit => unit.UnitId).ToList());

            CampaignBattleResolution firstResolution = service.FinalizeBattle(
                save,
                scenario,
                summary,
                simulation.Context.GetUnits(UnitFaction.Player, false));
            CampaignBattleResolution replayResolution = service.FinalizeBattle(
                save,
                scenario,
                summary,
                simulation.Context.GetUnits(UnitFaction.Player, false));

            Assert.True(firstResolution.GrantedStageReward);
            Assert.Equal(120, firstResolution.GrantedSupplies);
            Assert.Equal(1, firstResolution.GrantedRenown);
            Assert.Equal("yellow-turban-signet", firstResolution.GrantedItemId);
            Assert.Equal(120, save.Inventory.Supplies);
            Assert.Equal(1, save.Inventory.Renown);
            Assert.Equal(1, save.Inventory.GetQuantity("yellow-turban-signet"));
            Assert.True(ItemCatalog.Get("yellow-turban-signet").IsTreasure);
            Assert.Equal("player-liu-bei", ItemCatalog.Get("yellow-turban-signet").RecommendedOwnerUnitId);
            Assert.True(save.Progress.IsRewardClaimed(BattleScenarioCatalog.GuangzongScenarioId));
            Assert.True(save.GetUnit("player-liu-bei").Level > 1);
            Assert.False(replayResolution.GrantedStageReward);
            Assert.Equal(120, save.Inventory.Supplies);
            Assert.Equal(1, save.Inventory.Renown);
            Assert.Equal(1, save.Inventory.GetQuantity("yellow-turban-signet"));
        }

        [Fact]
        public void FinalizeBattle_RecruitsNamedHeroesOnlyOnceAndAddsTheirGear()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignSaveData save = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());
            BattleScenarioData scenario = service.PrepareScenario(
                BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.BowangpoScenarioId),
                save);
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            BattleResultSummary summary = new BattleResultSummary(
                BattleScenarioCatalog.BowangpoScenarioId,
                TurnSide.Player,
                4,
                simulation.Context.GetUnits(UnitFaction.Player, false).Select(unit => unit.Id).ToList());

            CampaignBattleResolution firstResolution = service.FinalizeBattle(
                save,
                scenario,
                summary,
                simulation.Context.GetUnits(UnitFaction.Player, false));
            CampaignBattleResolution secondResolution = service.FinalizeBattle(
                save,
                scenario,
                summary,
                simulation.Context.GetUnits(UnitFaction.Player, false));

            Assert.Contains("player-zhuge-liang", firstResolution.RecruitedUnitIds);
            Assert.Empty(secondResolution.RecruitedUnitIds);
            Assert.NotNull(save.GetUnit("player-zhuge-liang"));
            Assert.Equal(ActiveSkillType.FireStratagem, save.GetUnit("player-zhuge-liang").ActiveSkill);
            Assert.Equal(1, save.Inventory.GetQuantity("wind-feather-fan"));
            Assert.Equal(1, save.Inventory.GetQuantity("strategist-robe"));
        }

        [Fact]
        public void NormalizeSave_BackfillsMissingRewardRecruitsFromLegacyProgress()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignSaveData save = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());
            save.Progress.MarkCleared(BattleScenarioCatalog.ChangbanScenarioId);
            save.Progress.MarkRewardClaimed(BattleScenarioCatalog.ChangbanScenarioId);
            save.Progress.MarkCleared(BattleScenarioCatalog.JiamengPassScenarioId);
            save.Progress.MarkRewardClaimed(BattleScenarioCatalog.JiamengPassScenarioId);

            bool normalized = service.NormalizeSave(save);
            bool normalizedAgain = service.NormalizeSave(save);

            Assert.True(normalized);
            Assert.False(normalizedAgain);
            Assert.NotNull(save.GetUnit("player-zhao-yun"));
            Assert.NotNull(save.GetUnit("player-ma-chao"));
            Assert.Equal(1, save.Inventory.GetQuantity("white-dragon-spear"));
            Assert.Equal(1, save.Inventory.GetQuantity("scout-travel-mail"));
            Assert.Equal(1, save.Inventory.GetQuantity("western-lance"));
            Assert.Equal(1, save.Inventory.GetQuantity("raider-scale-vest"));
        }

        [Fact]
        public void NormalizeSave_BackfillsUnlockedStageIndexFromClearedChapters()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignSaveData save = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());

            save.Progress.MarkCleared(BattleScenarioCatalog.GuangzongScenarioId);
            save.Progress.MarkCleared(BattleScenarioCatalog.BowangpoScenarioId);
            save.Progress.MarkCleared(BattleScenarioCatalog.ChangbanScenarioId);
            save.Progress.MarkCleared(BattleScenarioCatalog.JiangxiaScenarioId);
            save.Progress.MarkCleared(BattleScenarioCatalog.JiamengPassScenarioId);
            save.Progress.MarkCleared(BattleScenarioCatalog.BaishuiScenarioId);
            save.Progress.MarkCleared(BattleScenarioCatalog.MianzhuScenarioId);
            save.Progress.MarkCleared(BattleScenarioCatalog.LuochengScenarioId);
            save.Progress.MarkCleared(BattleScenarioCatalog.YangpingScenarioId);

            bool normalized = service.NormalizeSave(save);

            Assert.True(normalized);
            Assert.Equal(9, save.Progress.UnlockedStageIndex);
        }

        [Fact]
        public void NormalizeSave_UpdatesLegacyBranchSkills()
        {
            CampaignUnitState legacyZhaoYun = new CampaignUnitState(
                "player-zhao-yun",
                "Zhao Yun",
                "unit.player_zhao_yun",
                UnitRole.Scout,
                "role.scout",
                PassiveSkillType.RapidMarch,
                "skill.rapid_march.name",
                "skill.rapid_march.desc",
                ActiveSkillType.PowerStrike,
                "skill.power_strike.name",
                "skill.power_strike.desc",
                31,
                11,
                4,
                4,
                1,
                18,
                "scout",
                "scout",
                AiProfileType.Aggressor,
                EquipmentLoadout.Empty);
            CampaignUnitState legacyMaChao = new CampaignUnitState(
                "player-ma-chao",
                "Ma Chao",
                "unit.player_ma_chao",
                UnitRole.Raider,
                "role.raider",
                PassiveSkillType.Vanguard,
                "skill.vanguard.name",
                "skill.vanguard.desc",
                ActiveSkillType.PowerStrike,
                "skill.power_strike.name",
                "skill.power_strike.desc",
                33,
                12,
                4,
                4,
                1,
                18,
                "storm_raider",
                "storm_raider",
                AiProfileType.Aggressor,
                EquipmentLoadout.Empty,
                level: 10,
                hasPromoted: true);
            CampaignUnitState legacyTacticianZhuge = new CampaignUnitState(
                "player-zhuge-liang",
                "Zhuge Liang",
                "unit.player_zhuge_liang",
                UnitRole.Commander,
                "role.commander",
                PassiveSkillType.CommandAura,
                "skill.command_aura.name",
                "skill.command_aura.desc",
                ActiveSkillType.GuardOrder,
                "skill.guard_order.name",
                "skill.guard_order.desc",
                31,
                9,
                5,
                3,
                1,
                23,
                "tactician_general",
                "tactician_general",
                AiProfileType.Support,
                EquipmentLoadout.Empty,
                level: 10,
                hasPromoted: true);
            CampaignUnitState legacyWarlordLiuBei = new CampaignUnitState(
                "player-liu-bei",
                "Liu Bei",
                "unit.player_liu_bei",
                UnitRole.Commander,
                "role.commander",
                PassiveSkillType.CommandAura,
                "skill.command_aura.name",
                "skill.command_aura.desc",
                ActiveSkillType.GuardOrder,
                "skill.guard_order.name",
                "skill.guard_order.desc",
                34,
                11,
                6,
                3,
                1,
                21,
                "warlord",
                "warlord",
                AiProfileType.Support,
                EquipmentLoadout.Empty,
                level: 10,
                hasPromoted: true);

            CampaignSaveData save = new CampaignSaveData(
                "campaign",
                new CampaignProgress(),
                new CampaignInventoryState(),
                new[] { legacyZhaoYun, legacyMaChao, legacyTacticianZhuge, legacyWarlordLiuBei });

            CampaignSaveNormalizer.Normalize(save);

            Assert.Equal(ActiveSkillType.DragonPierce, legacyZhaoYun.ActiveSkill);
            Assert.Equal("skill.dragon_pierce.name", legacyZhaoYun.ActiveSkillNameKey);
            Assert.Equal(ActiveSkillType.StormbreakCharge, legacyMaChao.ActiveSkill);
            Assert.Equal("skill.stormbreak_charge.name", legacyMaChao.ActiveSkillNameKey);
            Assert.Equal(ActiveSkillType.FeatherFormation, legacyTacticianZhuge.ActiveSkill);
            Assert.Equal("skill.feather_formation.name", legacyTacticianZhuge.ActiveSkillNameKey);
            Assert.Equal(ActiveSkillType.KingsBanner, legacyWarlordLiuBei.ActiveSkill);
            Assert.Equal("skill.kings_banner.name", legacyWarlordLiuBei.ActiveSkillNameKey);
        }

        [Fact]
        public void PrepareScenario_PreservesAbsentRecruitProgressAcrossSkippedBattle()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignSaveData save = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());
            save.Progress.MarkCleared(BattleScenarioCatalog.ChangbanScenarioId);
            save.Progress.MarkRewardClaimed(BattleScenarioCatalog.ChangbanScenarioId);
            service.NormalizeSave(save);

            CampaignUnitState zhaoYun = save.GetUnit("player-zhao-yun");
            zhaoYun.SyncFromBattle(CreateResolvedRuntime(zhaoYun, 8), 0, 0, 0);
            save.Inventory.AddItem("field-horse");
            Assert.True(service.TryEquipMount(save, zhaoYun.UnitId, "field-horse"));

            BattleScenarioData replayScenario = service.PrepareScenario(
                BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.BowangpoScenarioId),
                save);
            BattleSimulation replaySimulation = new BattleSimulation(replayScenario.Stage);
            BattleResultSummary replaySummary = new BattleResultSummary(
                BattleScenarioCatalog.BowangpoScenarioId,
                TurnSide.Player,
                4,
                replaySimulation.Context.GetUnits(UnitFaction.Player, false).Select(unit => unit.Id).ToList());

            service.FinalizeBattle(
                save,
                replayScenario,
                replaySummary,
                replaySimulation.Context.GetUnits(UnitFaction.Player, false));

            BattleScenarioData jiamengScenario = service.PrepareScenario(
                BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.JiamengPassScenarioId),
                save);
            UnitDefinitionData zhaoYunDefinition = jiamengScenario.Stage.UnitSpawns
                .Single(spawn => spawn.Definition.Id == "player-zhao-yun")
                .Definition;

            Assert.Equal(8, zhaoYunDefinition.StartingLevel);
            Assert.Equal("field-horse", zhaoYunDefinition.EquipmentLoadout.MountId);
            Assert.Equal(5, zhaoYunDefinition.MoveRange);
        }

        [Fact]
        public void PurchaseAndEquip_RespectRenownCostAndSharedCopies()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignSaveData save = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());
            save.Inventory.AddSupplies(500);

            Assert.False(service.TryPurchaseItem(save, "crescent-glaive"));
            save.Inventory.AddRenown(1);
            Assert.True(service.TryPurchaseItem(save, "crescent-glaive"));
            Assert.Equal(420, save.Inventory.Supplies);
            Assert.Equal(1, save.Inventory.GetQuantity("crescent-glaive"));
            Assert.True(service.TryEquipWeapon(save, "player-guan-yu", "crescent-glaive"));
            Assert.False(service.TryEquipWeapon(save, "player-zhang-fei", "crescent-glaive"));

            save.Inventory.AddItem("tempered-jian");
            save.Inventory.AddItem("commander-lamellar");
            save.Inventory.AddItem("field-horse");
            Assert.True(service.TryEquipWeapon(save, "player-liu-bei", "tempered-jian"));
            Assert.True(service.TryEquipArmor(save, "player-liu-bei", "commander-lamellar"));
            Assert.True(service.TryEquipMount(save, "player-liu-bei", "field-horse"));
            Assert.False(service.TryEquipMount(save, "player-guan-yu", "field-horse"));

            BattleScenarioData scenario = service.PrepareScenario(CreateSingleDuelScenario(), save);
            UnitDefinitionData liuBei = scenario.Stage.UnitSpawns.Single(spawn => spawn.Definition.Id == "player-liu-bei").Definition;

            Assert.Equal(10, liuBei.Attack);
            Assert.Equal(6, liuBei.Defense);
            Assert.Equal(32, liuBei.MaxHp);
            Assert.Equal(4, liuBei.MoveRange);
        }

        [Fact]
        public void PrepareScenario_MountMoveBonusStacksWithRapidMarchPassive()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignUnitState rider = new CampaignUnitState(
                "player-zhao-yun",
                "Zhao Yun",
                "unit.player_zhao_yun",
                UnitRole.Scout,
                "role.scout",
                PassiveSkillType.RapidMarch,
                "skill.rapid_march.name",
                "skill.rapid_march.desc",
                ActiveSkillType.DragonPierce,
                "skill.dragon_pierce.name",
                "skill.dragon_pierce.desc",
                31,
                11,
                4,
                4,
                1,
                18,
                "scout",
                "scout",
                AiProfileType.Aggressor,
                new EquipmentLoadout("white-dragon-spear", "scout-travel-mail", "field-horse"));
            CampaignSaveData save = new CampaignSaveData(
                "campaign-test",
                new CampaignProgress(),
                new CampaignInventoryState(),
                new[] { rider },
                3);

            UnitDefinitionData placeholder = new UnitDefinitionData(
                "player-zhao-yun",
                "Zhao Yun",
                "unit.player_zhao_yun",
                UnitFaction.Player,
                UnitRole.Scout,
                "role.scout",
                PassiveSkillType.None,
                "skill.none.name",
                "skill.none.desc",
                ActiveSkillType.None,
                "active.none.name",
                "active.none.desc",
                1,
                1,
                0,
                1,
                1);
            BattleScenarioData scenario = new BattleScenarioData(
                "scenario.mount_stack",
                "Mount Stack",
                "scenario.mount_stack",
                new StageDefinitionData(
                    "Mount Stack",
                    "stage.mount_stack",
                    8,
                    8,
                    new[] { new UnitSpawnData(placeholder, new GridPosition(1, 1)) },
                    Array.Empty<GridPosition>()),
                Array.Empty<ScenarioTrigger>(),
                rewardBundle: new RewardBundle(0, 0));

            BattleScenarioData prepared = service.PrepareScenario(scenario, save);
            BattleSimulation simulation = new BattleSimulation(prepared.Stage);
            UnitRuntimeState zhaoYun = simulation.Context.GetUnit("player-zhao-yun");

            Assert.Equal(5, zhaoYun.MoveRange);
            Assert.Equal(6, PassiveSkillRules.GetMoveRange(zhaoYun));
        }

        [Fact]
        public void PromoteUnit_RequiresLevelTen_AndOnlyAppliesOnce()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignSaveData save = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());
            CampaignUnitState liuBei = save.GetUnit("player-liu-bei");

            Assert.False(service.TryPromoteUnit(save, liuBei.UnitId));

            liuBei.SyncFromBattle(CreateResolvedRuntime(liuBei, 10), 0, 0, 0);

            Assert.True(service.TryPromoteUnit(save, liuBei.UnitId));
            Assert.False(service.TryPromoteUnit(save, liuBei.UnitId));
            Assert.True(liuBei.HasPromoted);
            Assert.Equal("lord", liuBei.ClassId);
            Assert.Equal(PassiveSkillType.BenevolentCommand, liuBei.PassiveSkill);
            Assert.Equal(ActiveSkillType.ImperialAid, liuBei.ActiveSkill);
            Assert.Equal(33, liuBei.MaxHp);
            Assert.Equal(10, liuBei.Attack);
            Assert.Equal(6, liuBei.Defense);
            Assert.Equal(22, liuBei.MaxMana);
        }

        [Fact]
        public void PromoteUnit_CanSelectAlternateBranchByPromotionId()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignSaveData save = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());
            CampaignUnitState liuBei = save.GetUnit("player-liu-bei");

            liuBei.SyncFromBattle(CreateResolvedRuntime(liuBei, 10), 0, 0, 0);

            Assert.True(service.TryPromoteUnit(save, liuBei.UnitId, "warlord"));
            Assert.True(liuBei.HasPromoted);
            Assert.Equal("warlord", liuBei.ClassId);
            Assert.Equal(PassiveSkillType.CommandAura, liuBei.PassiveSkill);
            Assert.Equal(ActiveSkillType.KingsBanner, liuBei.ActiveSkill);
            Assert.Equal(34, liuBei.MaxHp);
            Assert.Equal(11, liuBei.Attack);
            Assert.Equal(6, liuBei.Defense);
            Assert.Equal(21, liuBei.MaxMana);
        }

        [Fact]
        public void PromoteUnit_AlternateArcherBranchCanUnlockPinningShot()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignSaveData save = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());
            CampaignUnitState huangZhong = save.GetUnit("player-huang-zhong");

            huangZhong.SyncFromBattle(CreateResolvedRuntime(huangZhong, 10), 0, 0, 0);

            Assert.True(service.TryPromoteUnit(save, huangZhong.UnitId, "pinning_bow"));
            Assert.True(huangZhong.HasPromoted);
            Assert.Equal("pinning_bow", huangZhong.ClassId);
            Assert.Equal(PassiveSkillType.LongShot, huangZhong.PassiveSkill);
            Assert.Equal(ActiveSkillType.PinningShot, huangZhong.ActiveSkill);
        }

        [Fact]
        public void CampaignSaveNormalizer_UpdatesLegacyZhugeSkills()
        {
            CampaignUnitState commanderZhuge = new CampaignUnitState(
                "player-zhuge-liang",
                "Zhuge Liang",
                "unit.player_zhuge_liang",
                UnitRole.Commander,
                "role.commander",
                PassiveSkillType.CommandAura,
                "skill.command_aura.name",
                "skill.command_aura.desc",
                ActiveSkillType.RoyalAid,
                "skill.royal_aid.name",
                "skill.royal_aid.desc",
                28,
                8,
                4,
                3,
                1,
                20,
                "commander",
                "commander",
                AiProfileType.Support,
                EquipmentLoadout.Empty);
            CampaignUnitState sleepingDragonZhuge = new CampaignUnitState(
                "player-zhuge-liang",
                "Zhuge Liang",
                "unit.player_zhuge_liang",
                UnitRole.Commander,
                "role.commander",
                PassiveSkillType.BenevolentCommand,
                "skill.benevolent_command.name",
                "skill.benevolent_command.desc",
                ActiveSkillType.ImperialAid,
                "skill.imperial_aid.name",
                "skill.imperial_aid.desc",
                30,
                9,
                5,
                3,
                1,
                24,
                "sleeping_dragon",
                "sleeping_dragon",
                AiProfileType.Support,
                EquipmentLoadout.Empty,
                level: 10,
                hasPromoted: true);

            CampaignSaveData commanderSave = new CampaignSaveData("campaign", new CampaignProgress(), new CampaignInventoryState(), new[] { commanderZhuge });
            CampaignSaveData sleepingDragonSave = new CampaignSaveData("campaign", new CampaignProgress(), new CampaignInventoryState(), new[] { sleepingDragonZhuge });

            CampaignSaveNormalizer.Normalize(commanderSave);
            CampaignSaveNormalizer.Normalize(sleepingDragonSave);

            Assert.Equal(ActiveSkillType.FireStratagem, commanderZhuge.ActiveSkill);
            Assert.Equal("skill.fire_stratagem.name", commanderZhuge.ActiveSkillNameKey);
            Assert.Equal(ActiveSkillType.EightTrigramInferno, sleepingDragonZhuge.ActiveSkill);
            Assert.Equal("skill.eight_trigram_inferno.name", sleepingDragonZhuge.ActiveSkillNameKey);
        }

        [Fact]
        public void CampaignSaveNormalizer_PreservesTacticianGeneralSupportBranch()
        {
            CampaignUnitState tacticianZhuge = new CampaignUnitState(
                "player-zhuge-liang",
                "Zhuge Liang",
                "unit.player_zhuge_liang",
                UnitRole.Commander,
                "role.commander",
                PassiveSkillType.CommandAura,
                "skill.command_aura.name",
                "skill.command_aura.desc",
                ActiveSkillType.GuardOrder,
                "skill.guard_order.name",
                "skill.guard_order.desc",
                31,
                9,
                5,
                3,
                1,
                23,
                "tactician_general",
                "tactician_general",
                AiProfileType.Support,
                EquipmentLoadout.Empty,
                level: 10,
                hasPromoted: true);
            CampaignSaveData save = new CampaignSaveData("campaign", new CampaignProgress(), new CampaignInventoryState(), new[] { tacticianZhuge });

            CampaignSaveNormalizer.Normalize(save);

            Assert.Equal(ActiveSkillType.FeatherFormation, tacticianZhuge.ActiveSkill);
            Assert.Equal("skill.feather_formation.name", tacticianZhuge.ActiveSkillNameKey);
        }

        [Fact]
        public void PromotionCatalog_ZhugeSleepingDragon_UsesEightTrigramInferno()
        {
            PromotionDefinition sleepingDragon = PromotionCatalog
                .GetOptions("player-zhuge-liang")
                .Single(definition => definition.PromotionId == "sleeping_dragon");
            PromotionDefinition tacticianGeneral = PromotionCatalog
                .GetOptions("player-zhuge-liang")
                .Single(definition => definition.PromotionId == "tactician_general");

            Assert.Equal(ActiveSkillType.EightTrigramInferno, sleepingDragon.ActiveSkill);
            Assert.Equal(ActiveSkillType.FeatherFormation, tacticianGeneral.ActiveSkill);
        }

        [Fact]
        public void PromotionCatalog_AllPromotionBranchesUseUniqueActiveSkills()
        {
            PromotionDefinition[] options = PromotionCatalog
                .GetOptions("player-liu-bei")
                .Concat(PromotionCatalog.GetOptions("player-guan-yu"))
                .Concat(PromotionCatalog.GetOptions("player-zhang-fei"))
                .Concat(PromotionCatalog.GetOptions("player-huang-zhong"))
                .Concat(PromotionCatalog.GetOptions("player-zhuge-liang"))
                .Concat(PromotionCatalog.GetOptions("player-zhao-yun"))
                .Concat(PromotionCatalog.GetOptions("player-ma-chao"))
                .ToArray();

            Assert.Equal(14, options.Length);
            Assert.Equal(14, options.Select(option => option.ActiveSkill).Distinct().Count());
        }

        private static BattleScenarioData CreateSingleDuelScenario()
        {
            UnitDefinitionData playerPlaceholder = new UnitDefinitionData(
                "player-liu-bei",
                "Liu Bei",
                "unit.player_liu_bei",
                UnitFaction.Player,
                UnitRole.Commander,
                "role.commander",
                PassiveSkillType.CommandAura,
                "skill.command_aura.name",
                "skill.command_aura.desc",
                ActiveSkillType.RoyalAid,
                "skill.royal_aid.name",
                "skill.royal_aid.desc",
                1,
                1,
                0,
                3,
                1);
            UnitDefinitionData enemyDefinition = new UnitDefinitionData(
                "enemy-bandit",
                "Bandit",
                "unit.enemy_bandit",
                UnitFaction.Enemy,
                UnitRole.Raider,
                "role.raider",
                PassiveSkillType.None,
                "skill.none.name",
                "skill.none.desc",
                ActiveSkillType.None,
                "active.none.name",
                "active.none.desc",
                20,
                7,
                3,
                3,
                1);
            StageDefinitionData stage = new StageDefinitionData(
                "Progression Test",
                "stage.progression_test",
                6,
                6,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(playerPlaceholder, new GridPosition(1, 1)),
                    new UnitSpawnData(enemyDefinition, new GridPosition(2, 1)),
                },
                Array.Empty<GridPosition>());

            return new BattleScenarioData(
                "scenario.progression_test",
                "Progression Test",
                "scenario.progression_test",
                stage,
                Array.Empty<ScenarioTrigger>(),
                rewardBundle: new RewardBundle(0, 0));
        }

        private static UnitRuntimeState CreateResolvedRuntime(CampaignUnitState unit, int level)
        {
            UnitDefinitionData definition = new UnitDefinitionData(
                unit.UnitId,
                unit.DisplayName,
                unit.DisplayNameKey,
                UnitFaction.Player,
                unit.Role,
                unit.RoleNameKey,
                unit.PassiveSkill,
                unit.PassiveSkillNameKey,
                unit.PassiveSkillDescriptionKey,
                unit.ActiveSkill,
                unit.ActiveSkillNameKey,
                unit.ActiveSkillDescriptionKey,
                unit.MaxHp,
                unit.Attack,
                unit.Defense,
                unit.MoveRange,
                unit.AttackRange,
                unit.MaxMana,
                unit.ClassId,
                unit.GrowthProfileId,
                unit.AiProfile,
                unit.EquipmentLoadout,
                level,
                0,
                new BondState(unit.BondState.SupportLevel, unit.BondState.SharedBattles),
                true);
            return new UnitRuntimeState(definition, new GridPosition(0, 0));
        }
    }
}
