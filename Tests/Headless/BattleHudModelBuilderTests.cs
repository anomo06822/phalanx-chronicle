using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Battle;
using PhalanxChronicle.Battle.Effects;
using PhalanxChronicle.Core;
using PhalanxChronicle.UI;
using Xunit;

namespace PhalanxChronicle.Headless.Tests
{
    public sealed class BattleHudModelBuilderTests
    {
        [Fact]
        public void ForecastBuilder_MovePreview_PopulatesMoveThreatAndCommitFields()
        {
            BattleSimulation simulation = CreateSimulation();
            BattleForecastModelBuilder builder = new BattleForecastModelBuilder();

            BattleIntentPreview preview = simulation.PreviewMoveIntent("player-1", new GridPosition(1, 1));
            BattleForecastModel model = builder.BuildIntentForecastModel(simulation, preview);

            Assert.Equal(BattleForecastMode.MovePreview, model.Mode);
            Assert.NotEmpty(model.OutcomeFacts);
            Assert.Equal("2", model.OutcomeFacts[0].Value);
            Assert.False(string.IsNullOrWhiteSpace(model.PrimaryLine));
            Assert.NotNull(model.RiskChip);
            Assert.NotNull(model.CommitChip);
        }

        [Fact]
        public void ActionMenuBuilder_AttackAction_ProducesMetricsOutcomeAndRiskChip()
        {
            BattleSimulation simulation = CreateAdjacentCombatSimulation(enemyHpOverride: 7);
            BattleActionMenuModelBuilder builder = new BattleActionMenuModelBuilder();

            BattleActionMenuModel model = builder.BuildActionMenuModel(simulation, simulation.Context.GetUnit("player-1"), moved: false);
            BattleActionDescriptor attack = model.Actions.Single(action => action.Type == BattleActionDescriptorType.Attack);

            Assert.Equal(BattleActionDescriptorPriority.Primary, attack.Priority);
            Assert.True(attack.IsEnabled);
            Assert.True(attack.Lethal);
            Assert.True(attack.MetricChips.Count >= 3);
            Assert.Contains(attack.MetricChips, chip => chip.Text.Contains("射程"));
            Assert.False(string.IsNullOrWhiteSpace(attack.OutcomeLine));
            Assert.NotNull(attack.RiskChip);
        }

        [Fact]
        public void RosterBuilder_OrdersSelectedPlayerBeforeDoneAndMarksThreateningEnemy()
        {
            BattleSimulation simulation = CreateSimulation();
            BattleRosterModelBuilder builder = new BattleRosterModelBuilder();
            simulation.Context.GetUnit("player-2").MarkActed();
            BattleThreatProjection selectedProjection = BattleThreatAnalyzer.AnalyzeProjected(simulation.Context, simulation.Context.GetUnit("player-1"), simulation.Context.GetUnit("player-1").Position);

            IReadOnlyList<BattleRosterEntryModel> playerRoster = builder.BuildRosterEntries(simulation, UnitFaction.Player, "player-1", selectedProjection);
            IReadOnlyList<BattleRosterEntryModel> enemyRoster = builder.BuildRosterEntries(simulation, UnitFaction.Enemy, "player-1", selectedProjection);

            Assert.Equal("player-1", playerRoster[0].UnitId);
            Assert.Equal(BattleRosterTag.Exposed, playerRoster.Last().PrimaryTag);
            Assert.Contains(BattleRosterTag.Done, playerRoster.Last().Tags);
            Assert.True(enemyRoster[0].IsThreateningSelection);
            Assert.Contains(BattleRosterTag.Threatening, enemyRoster[0].Tags);
        }

        [Fact]
        public void EnemyLowSignalActions_DoNotTakeOverRibbon()
        {
            BattleActionSequencer sequencer = new BattleActionSequencer();
            CombatResult combatResult = new CombatResult("enemy-1", "player-1", 2, 18, false, 0, 0);
            SkillResult skillResult = new SkillResult(
                "enemy-buffer",
                ActiveSkillType.WarCry,
                "player-1",
                new List<SkillEffectResult>
                {
                    new SkillEffectResult("player-1", 0, 20, false, false),
                },
                0,
                0);

            Assert.False(sequencer.ShouldTakeOverRibbon(TurnSide.Enemy, combatResult));
            Assert.False(sequencer.ShouldTakeOverRibbon(TurnSide.Enemy, skillResult));
            Assert.True(sequencer.ShouldTakeOverRibbon(TurnSide.Player, combatResult));
        }

        private static BattleSimulation CreateSimulation()
        {
            UnitDefinitionData playerDefinition = CreateDefinition("player-1", "Hero", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.None, attack: 10);
            UnitDefinitionData playerTwoDefinition = CreateDefinition("player-2", "Knight", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None);
            UnitDefinitionData enemyOne = CreateDefinition("enemy-1", "Bandit A", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, attack: 8, defense: 3);
            UnitDefinitionData enemyTwo = CreateDefinition("enemy-2", "Bandit B", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, attack: 8, defense: 3);

            StageDefinitionData stage = new StageDefinitionData(
                "Test Stage",
                "stage.test",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(playerDefinition, new GridPosition(0, 0)),
                    new UnitSpawnData(playerTwoDefinition, new GridPosition(1, 0)),
                    new UnitSpawnData(enemyOne, new GridPosition(1, 1)),
                    new UnitSpawnData(enemyTwo, new GridPosition(3, 2)),
                },
                new List<GridPosition>());

            return new BattleSimulation(stage);
        }

        private static BattleSimulation CreateAdjacentCombatSimulation(int enemyHpOverride = 20)
        {
            UnitDefinitionData playerDefinition = CreateDefinition("player-1", "Hero", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.None, attack: 10);
            UnitDefinitionData enemyDefinition = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: enemyHpOverride, attack: 8, defense: 3);

            StageDefinitionData stage = new StageDefinitionData(
                "Adjacent Combat",
                "stage.adjacent",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(playerDefinition, new GridPosition(0, 0)),
                    new UnitSpawnData(enemyDefinition, new GridPosition(1, 0)),
                },
                new List<GridPosition>());

            return new BattleSimulation(stage);
        }

        private static UnitDefinitionData CreateDefinition(
            string id,
            string displayName,
            UnitFaction faction,
            UnitRole role,
            PassiveSkillType passiveSkill,
            ActiveSkillType activeSkill,
            int maxHp = 30,
            int attack = 10,
            int defense = 5,
            int moveRange = 3,
            int attackRange = 1,
            int maxMana = 20,
            int startingLevel = 1,
            string classId = null,
            string growthProfileId = null,
            AiProfileType aiProfile = AiProfileType.Default,
            bool progressionResolved = false)
        {
            return new UnitDefinitionData(
                id,
                displayName,
                "unit." + id,
                faction,
                role,
                "role." + role.ToString().ToLowerInvariant(),
                passiveSkill,
                "skill." + passiveSkill.ToString().ToLowerInvariant() + ".name",
                "skill." + passiveSkill.ToString().ToLowerInvariant() + ".desc",
                activeSkill,
                "skill." + activeSkill.ToString().ToLowerInvariant() + ".name",
                "skill." + activeSkill.ToString().ToLowerInvariant() + ".desc",
                maxHp,
                attack,
                defense,
                moveRange,
                attackRange,
                maxMana,
                classId,
                growthProfileId,
                aiProfile,
                null,
                startingLevel,
                0,
                null,
                progressionResolved);
        }
    }
}
