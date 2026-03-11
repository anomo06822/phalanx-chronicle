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
            Assert.Equal(1, save.Version);
            Assert.Equal("vermilion-jian", save.GetUnit("player-liu-bei").EquipmentLoadout.WeaponId);
            Assert.Equal("commander-travel-cloak", save.GetUnit("player-liu-bei").EquipmentLoadout.ArmorId);
            Assert.Equal(1, save.Inventory.GetQuantity("vermilion-jian"));
            Assert.Equal(2, save.Inventory.GetQuantity("iron-crescent-glaive"));
            Assert.Equal(2, save.Inventory.GetQuantity("guardian-scale-vest"));
            Assert.Equal(1, save.Inventory.GetQuantity("featherback-war-bow"));
            Assert.Equal(1, save.Inventory.GetQuantity("ranger-hunt-coat"));
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

            Assert.Equal(150, firstClearScenario.RewardBundle.Supplies);
            Assert.Equal(2, firstClearScenario.RewardBundle.Renown);
            Assert.Equal("bowang-fire-token", firstClearScenario.RewardBundle.RewardItemId);
            Assert.Contains("player-zhuge-liang", firstClearScenario.RewardBundle.RecruitUnitIds);
            Assert.Equal(0, replayScenario.RewardBundle.Supplies);
            Assert.Equal(0, replayScenario.RewardBundle.Renown);
            Assert.Equal(string.Empty, replayScenario.RewardBundle.RewardItemId);
            Assert.Empty(replayScenario.RewardBundle.RecruitUnitIds);
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
            Assert.Equal(1, save.Inventory.GetQuantity("wind-feather-fan"));
            Assert.Equal(1, save.Inventory.GetQuantity("strategist-robe"));
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
            Assert.True(service.TryEquipWeapon(save, "player-liu-bei", "tempered-jian"));
            Assert.True(service.TryEquipArmor(save, "player-liu-bei", "commander-lamellar"));

            BattleScenarioData scenario = service.PrepareScenario(CreateSingleDuelScenario(), save);
            UnitDefinitionData liuBei = scenario.Stage.UnitSpawns.Single(spawn => spawn.Definition.Id == "player-liu-bei").Definition;

            Assert.Equal(10, liuBei.Attack);
            Assert.Equal(6, liuBei.Defense);
            Assert.Equal(32, liuBei.MaxHp);
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
            Assert.Equal(ActiveSkillType.GuardOrder, liuBei.ActiveSkill);
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
