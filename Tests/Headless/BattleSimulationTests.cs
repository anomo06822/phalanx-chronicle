using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Core;
using Xunit;

namespace PhalanxChronicle.Headless.Tests
{
    public sealed class BattleSimulationTests
    {
        [Fact]
        public void MoveRange_BlocksOccupiedCell()
        {
            BattleSimulation simulation = CreateSimulation();

            IReadOnlyList<GridPosition> positions = simulation.GetMoveRange("player-1");

            Assert.DoesNotContain(new GridPosition(1, 0), positions);
            Assert.Contains(new GridPosition(0, 1), positions);
        }

        [Fact]
        public void MoveRange_BlocksTerrainCell()
        {
            BattleSimulation simulation = CreateSimulation(blockedCells: new[] { new GridPosition(0, 1) });

            IReadOnlyList<GridPosition> positions = simulation.GetMoveRange("player-1");

            Assert.DoesNotContain(new GridPosition(0, 1), positions);
        }

        [Fact]
        public void MoveDestinations_ExcludeOriginForUiFlow()
        {
            BattleSimulation simulation = CreateSimulation();

            IReadOnlyList<GridPosition> destinations = simulation.GetMoveDestinations("player-1");

            Assert.DoesNotContain(new GridPosition(0, 0), destinations);
            Assert.Contains(new GridPosition(0, 1), destinations);
        }

        [Fact]
        public void Attack_UsesMinimumDamageFormula()
        {
            BattleSimulation simulation = CreateAdjacentCombatSimulation(
                enemyDefenseOverride: 99,
                playerAttackOverride: 1);

            CombatResult result = simulation.TryAttack("player-1", "enemy-1");

            Assert.NotNull(result);
            Assert.Equal(1, result.Damage);
            Assert.Equal(19, simulation.Context.GetUnit("enemy-1").CurrentHp);
        }

        [Fact]
        public void PersistentEquipmentBonuses_IncreaseMaterializedAttackDamage()
        {
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignSaveData baselineSave = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());
            CampaignSaveData equippedSave = service.CreateNewSave(CampaignCatalog.CreateLiuBeiLegend());
            equippedSave.Inventory.AddItem("tempered-jian");
            Assert.True(service.TryEquipWeapon(equippedSave, "player-liu-bei", "tempered-jian"));

            BattleScenarioData duelScenario = new BattleScenarioData(
                "scenario.duel",
                "Duel",
                "scenario.duel",
                new StageDefinitionData(
                    "Duel",
                    "stage.duel",
                    6,
                    6,
                    new List<UnitSpawnData>
                    {
                        new UnitSpawnData(CreateDefinition("player-liu-bei", "Liu Bei", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.RoyalAid), new GridPosition(1, 1)),
                        new UnitSpawnData(CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, attack: 8, defense: 3), new GridPosition(2, 1)),
                    },
                    new List<GridPosition>()),
                new List<ScenarioTrigger>(),
                rewardBundle: new RewardBundle(0, 0));

            BattleSimulation baselineSimulation = new BattleSimulation(service.PrepareScenario(duelScenario, baselineSave).Stage);
            BattleSimulation equippedSimulation = new BattleSimulation(service.PrepareScenario(duelScenario, equippedSave).Stage);

            CombatResult baselineResult = baselineSimulation.TryAttack("player-liu-bei", "enemy-1");
            CombatResult equippedResult = equippedSimulation.TryAttack("player-liu-bei", "enemy-1");

            Assert.NotNull(baselineResult);
            Assert.NotNull(equippedResult);
            Assert.Equal(6, baselineResult.Damage);
            Assert.Equal(7, equippedResult.Damage);
        }

        [Fact]
        public void Attack_RemovesDeadUnitAndMarksVictory()
        {
            BattleSimulation simulation = CreateSingleEnemySimulation(enemyHpOverride: 1);
            CombatResult result = simulation.TryAttack("player-1", "enemy-1");

            Assert.NotNull(result);
            Assert.True(simulation.Context.BattleEnded);
            Assert.Equal(TurnSide.Player, simulation.Context.WinningSide);
            Assert.Null(simulation.Context.GetUnitAt(new GridPosition(1, 0)));
        }

        [Fact]
        public void EnemyAi_MovesTowardClosestPlayerAndAttacksWhenPossible()
        {
            BattleSimulation simulation = CreateSimulation();

            simulation.EndCurrentTurn();

            UnitActionResult enemyAction = simulation.ResolveEnemyAction("enemy-1");

            Assert.NotNull(enemyAction);
            Assert.NotEqual(new GridPosition(2, 1), enemyAction.EndPosition);
            Assert.True(enemyAction.PerformedAttack);
            Assert.Equal("player-2", enemyAction.CombatResult.DefenderUnitId);
        }

        [Fact]
        public void ProjectedAttackRange_ShowsThreatAfterMoving()
        {
            UnitDefinitionData playerDefinition = CreateDefinition("player-1", "Hero", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.None, moveRange: 2, attackRange: 1);
            UnitDefinitionData enemyDefinition = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None);

            StageDefinitionData stage = new StageDefinitionData(
                "Projected Attack",
                "stage.projected_attack",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(playerDefinition, new GridPosition(0, 0)),
                    new UnitSpawnData(enemyDefinition, new GridPosition(6, 6)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            IReadOnlyList<GridPosition> projectedRange = simulation.GetProjectedAttackRange("player-1");

            Assert.Contains(new GridPosition(3, 0), projectedRange);
            Assert.Contains(new GridPosition(1, 2), projectedRange);
            Assert.DoesNotContain(new GridPosition(4, 0), projectedRange);
        }

        [Fact]
        public void FindAttackDestination_PicksReachableAttackCell()
        {
            BattleSimulation simulation = CreateSimulation();

            bool found = simulation.TryFindAttackDestination("player-1", "enemy-1", out GridPosition destination);

            Assert.True(found);
            Assert.Equal(new GridPosition(1, 1), destination);
        }

        [Fact]
        public void MovePreview_ReturnsShortestPathAndProjectedTargets()
        {
            BattleSimulation simulation = CreateSimulation();

            BattlePathPreview preview = simulation.GetMovePreview("player-1", new GridPosition(1, 1));

            Assert.NotNull(preview);
            Assert.Equal(new GridPosition(1, 1), preview.Destination);
            Assert.Equal(2, preview.MoveCost);
            Assert.Equal(
                new[]
                {
                    new GridPosition(0, 0),
                    new GridPosition(0, 1),
                    new GridPosition(1, 1),
                },
                preview.Path);
            Assert.True(preview.AttackTargetCount >= 1);
        }

        [Fact]
        public void QuickAttackPreview_UsesProjectedDestinationDamage()
        {
            BattleSimulation simulation = CreateSimulation();

            BattleQuickAttackPreview preview = simulation.GetQuickAttackPreview("player-1", "enemy-1");

            Assert.NotNull(preview);
            Assert.Equal(new GridPosition(1, 1), preview.Destination);
            Assert.Equal("enemy-1", preview.TargetUnitId);
            Assert.Equal(7, preview.ProjectedDamage);
            Assert.Equal(13, preview.DefenderRemainingHp);
            Assert.False(preview.IsLethal);
            Assert.Equal(
                new[]
                {
                    new GridPosition(0, 0),
                    new GridPosition(0, 1),
                    new GridPosition(1, 1),
                },
                preview.Path);
        }

        [Fact]
        public void PreviewMoveIntent_ReturnsPathAndProjectedThreat()
        {
            BattleSimulation simulation = CreateSimulation();

            BattleIntentPreview preview = simulation.PreviewMoveIntent("player-1", new GridPosition(1, 1));

            Assert.NotNull(preview);
            Assert.Equal(BattleIntentActionKind.Move, preview.ActionKind);
            Assert.True(preview.CanCommit);
            Assert.Equal(new GridPosition(1, 1), preview.Destination);
            Assert.Equal(2, preview.MoveCost);
            Assert.Equal(
                new[]
                {
                    new GridPosition(0, 0),
                    new GridPosition(0, 1),
                    new GridPosition(1, 1),
                },
                preview.Path);
            Assert.NotNull(preview.ThreatAfterAction);
            Assert.True(preview.ThreatAfterAction.ThreateningEnemyCount > 0);
        }

        [Fact]
        public void PreviewAttackIntent_MatchesLethalDamageProjection()
        {
            BattleSimulation simulation = CreateAdjacentCombatSimulation(enemyHpOverride: 7);

            BattleIntentPreview preview = simulation.PreviewAttackIntent("player-1", "enemy-1");

            Assert.NotNull(preview);
            Assert.Equal(BattleIntentActionKind.Attack, preview.ActionKind);
            Assert.True(preview.CanCommit);
            Assert.Equal("enemy-1", preview.PrimaryTargetId);
            Assert.Equal(7, preview.PredictedDamage);
            Assert.Contains("enemy-1", preview.LethalTargetIds);
            Assert.Equal(0, preview.ThreatAfterAction.ThreateningEnemyCount);
        }

        [Fact]
        public void PreviewQuickAttackIntent_UsesProjectedDestinationAndDamage()
        {
            BattleSimulation simulation = CreateSimulation();

            BattleIntentPreview preview = simulation.PreviewQuickAttackIntent("player-1", "enemy-1");

            Assert.NotNull(preview);
            Assert.Equal(BattleIntentActionKind.QuickAttack, preview.ActionKind);
            Assert.True(preview.CanCommit);
            Assert.Equal("enemy-1", preview.PrimaryTargetId);
            Assert.Equal(new GridPosition(1, 1), preview.Destination);
            Assert.Equal(7, preview.PredictedDamage);
            Assert.Equal(
                new[]
                {
                    new GridPosition(0, 0),
                    new GridPosition(0, 1),
                    new GridPosition(1, 1),
                },
                preview.Path);
        }

        [Fact]
        public void PreviewSkillIntent_IncludesPredictedStatuses()
        {
            UnitDefinitionData archer = CreateDefinition("player-archer", "Archer", UnitFaction.Player, UnitRole.Ranger, PassiveSkillType.None, ActiveSkillType.PinningShot, attack: 10, attackRange: 2);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 3);

            StageDefinitionData stage = new StageDefinitionData(
                "Pinning Shot Stage",
                "stage.pinning_shot_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(archer, new GridPosition(0, 0)),
                    new UnitSpawnData(enemy, new GridPosition(3, 0)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            BattleIntentPreview preview = simulation.PreviewSkillIntent("player-archer", "enemy-1");

            Assert.NotNull(preview);
            Assert.Equal(BattleIntentActionKind.Skill, preview.ActionKind);
            Assert.True(preview.CanCommit);
            Assert.Equal(ActiveSkillRules.GetManaCost(ActiveSkillType.PinningShot), preview.ManaCost);
            Assert.Contains(preview.PredictedStatuses, status => status.Type == StatusEffectType.Rooted);
            Assert.True(preview.PredictedDamage > 0);
        }

        [Fact]
        public void UndoMove_ReturnsUnitToOriginalCell()
        {
            BattleSimulation simulation = CreateSimulation();

            bool moved = simulation.TryMoveUnit("player-1", new GridPosition(1, 1));
            bool undone = simulation.TryUndoMoveUnit("player-1", new GridPosition(0, 0));

            Assert.True(moved);
            Assert.True(undone);
            Assert.Equal(new GridPosition(0, 0), simulation.Context.GetUnit("player-1").Position);
            Assert.False(simulation.Context.GetUnit("player-1").HasMovedThisTurn);
            Assert.Equal("player-1", simulation.Context.GetUnitAt(new GridPosition(0, 0)).Id);
            Assert.Null(simulation.Context.GetUnitAt(new GridPosition(1, 1)));
        }

        [Fact]
        public void EnemyAi_PrioritizesFinishingKillableTargetOverCloserTarget()
        {
            UnitDefinitionData closerTarget = CreateDefinition(
                "player-closer",
                "Closer",
                UnitFaction.Player,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 20,
                defense: 5);
            UnitDefinitionData killableTarget = CreateDefinition(
                "player-killable",
                "Killable",
                UnitFaction.Player,
                UnitRole.Ranger,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 4,
                defense: 0);
            UnitDefinitionData enemy = CreateDefinition(
                "enemy-ranger",
                "Enemy Ranger",
                UnitFaction.Enemy,
                UnitRole.Ranger,
                PassiveSkillType.LongShot,
                ActiveSkillType.None,
                attack: 6,
                attackRange: 1);

            StageDefinitionData stage = new StageDefinitionData(
                "Kill Priority",
                "stage.kill_priority",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(closerTarget, new GridPosition(2, 1)),
                    new UnitSpawnData(killableTarget, new GridPosition(1, 1)),
                    new UnitSpawnData(enemy, new GridPosition(3, 1)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.EndCurrentTurn();

            AiDecision decision = simulation.BuildEnemyDecision("enemy-ranger");

            Assert.Equal(AiActionType.Attack, decision.ActionType);
            Assert.Equal("player-killable", decision.TargetUnitId);
            Assert.Equal(new GridPosition(3, 1), decision.Destination);
        }

        [Fact]
        public void EnemyAi_UsesVolleyTargetThatHitsMoreUnits()
        {
            UnitDefinitionData isolatedTarget = CreateDefinition(
                "player-isolated",
                "Isolated",
                UnitFaction.Player,
                UnitRole.Ranger,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 5,
                defense: 0);
            UnitDefinitionData clusteredTarget = CreateDefinition(
                "player-clustered-a",
                "Cluster A",
                UnitFaction.Player,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 10,
                defense: 0);
            UnitDefinitionData clusteredNeighbor = CreateDefinition(
                "player-clustered-b",
                "Cluster B",
                UnitFaction.Player,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 10,
                defense: 0);
            UnitDefinitionData enemy = CreateDefinition(
                "enemy-volley",
                "Enemy Volley",
                UnitFaction.Enemy,
                UnitRole.Ranger,
                PassiveSkillType.None,
                ActiveSkillType.Volley,
                attack: 10);

            StageDefinitionData stage = new StageDefinitionData(
                "Volley Value",
                "stage.volley_value",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(isolatedTarget, new GridPosition(2, 1)),
                    new UnitSpawnData(clusteredTarget, new GridPosition(2, 3)),
                    new UnitSpawnData(clusteredNeighbor, new GridPosition(2, 4)),
                    new UnitSpawnData(enemy, new GridPosition(0, 2)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.EndCurrentTurn();

            AiDecision decision = simulation.BuildEnemyDecision("enemy-volley");

            Assert.Equal(AiActionType.Skill, decision.ActionType);
            Assert.Contains(decision.TargetUnitId, new[] { "player-clustered-a", "player-clustered-b" });
        }

        [Fact]
        public void EnemyAi_PrefersSaferAttackPositionWhenDamageIsEqual()
        {
            UnitDefinitionData frontliner = CreateDefinition(
                "player-frontliner",
                "Frontliner",
                UnitFaction.Player,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.None,
                moveRange: 1);
            UnitDefinitionData support = CreateDefinition(
                "player-support",
                "Support",
                UnitFaction.Player,
                UnitRole.Ranger,
                PassiveSkillType.None,
                ActiveSkillType.None,
                moveRange: 1);
            UnitDefinitionData enemy = CreateDefinition(
                "enemy-archer",
                "Enemy Archer",
                UnitFaction.Enemy,
                UnitRole.Ranger,
                PassiveSkillType.LongShot,
                ActiveSkillType.None,
                attack: 9,
                moveRange: 2,
                attackRange: 1);

            StageDefinitionData stage = new StageDefinitionData(
                "Risk Positioning",
                "stage.risk_positioning",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(frontliner, new GridPosition(1, 1)),
                    new UnitSpawnData(support, new GridPosition(1, 2)),
                    new UnitSpawnData(enemy, new GridPosition(4, 1)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.EndCurrentTurn();

            AiDecision decision = simulation.BuildEnemyDecision("enemy-archer");

            Assert.Equal(AiActionType.Attack, decision.ActionType);
            Assert.Equal("player-frontliner", decision.TargetUnitId);
            Assert.Equal(new GridPosition(3, 1), decision.Destination);
        }

        [Fact]
        public void PlayerAutoDecision_PrefersKillableTargetOverHigherHpEnemy()
        {
            UnitDefinitionData player = CreateDefinition(
                "player-ranger",
                "Player Ranger",
                UnitFaction.Player,
                UnitRole.Ranger,
                PassiveSkillType.None,
                ActiveSkillType.None,
                attack: 7,
                attackRange: 1,
                aiProfile: AiProfileType.Aggressor);
            UnitDefinitionData healthyTarget = CreateDefinition(
                "enemy-healthy",
                "Healthy Target",
                UnitFaction.Enemy,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 18,
                defense: 2);
            UnitDefinitionData killableTarget = CreateDefinition(
                "enemy-killable",
                "Killable Target",
                UnitFaction.Enemy,
                UnitRole.Raider,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 4,
                defense: 0);

            StageDefinitionData stage = new StageDefinitionData(
                "Player Kill Priority",
                "stage.player_kill_priority",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(player, new GridPosition(3, 2)),
                    new UnitSpawnData(healthyTarget, new GridPosition(2, 2)),
                    new UnitSpawnData(killableTarget, new GridPosition(4, 2)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            AiDecision decision = simulation.BuildPlayerAutoDecision("player-ranger");

            Assert.Equal(AiActionType.Attack, decision.ActionType);
            Assert.Equal("enemy-killable", decision.TargetUnitId);
            Assert.Equal(new GridPosition(3, 2), decision.Destination);
        }

        [Fact]
        public void PlayerAutoDecision_SupportPrefersHealingWoundedAlly()
        {
            UnitDefinitionData healer = CreateDefinition(
                "player-healer",
                "Player Healer",
                UnitFaction.Player,
                UnitRole.Commander,
                PassiveSkillType.CommandAura,
                ActiveSkillType.RoyalAid,
                attack: 8,
                aiProfile: AiProfileType.Support);
            UnitDefinitionData woundedAlly = CreateDefinition(
                "player-ally",
                "Wounded Ally",
                UnitFaction.Player,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 20,
                defense: 4,
                aiProfile: AiProfileType.Protector);
            UnitDefinitionData enemy = CreateDefinition(
                "enemy-1",
                "Enemy",
                UnitFaction.Enemy,
                UnitRole.Raider,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 20,
                attack: 8);

            StageDefinitionData stage = new StageDefinitionData(
                "Player Support Priority",
                "stage.player_support_priority",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(healer, new GridPosition(1, 1)),
                    new UnitSpawnData(woundedAlly, new GridPosition(2, 1)),
                    new UnitSpawnData(enemy, new GridPosition(6, 6)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.Context.GetUnit("player-ally").ApplyDamage(12);

            AiDecision decision = simulation.BuildPlayerAutoDecision("player-healer");

            Assert.Equal(AiActionType.Skill, decision.ActionType);
            Assert.Equal("player-ally", decision.TargetUnitId);
        }

        [Fact]
        public void ResolvePlayerAutoAction_MovesExecutesAndMarksUnitActed()
        {
            UnitDefinitionData player = CreateDefinition(
                "player-rider",
                "Player Rider",
                UnitFaction.Player,
                UnitRole.Raider,
                PassiveSkillType.RapidMarch,
                ActiveSkillType.None,
                attack: 9,
                moveRange: 2,
                attackRange: 1,
                aiProfile: AiProfileType.Aggressor);
            UnitDefinitionData enemy = CreateDefinition(
                "enemy-1",
                "Enemy",
                UnitFaction.Enemy,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 18,
                defense: 2);

            StageDefinitionData stage = new StageDefinitionData(
                "Player Auto Resolve",
                "stage.player_auto_resolve",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(player, new GridPosition(0, 0)),
                    new UnitSpawnData(enemy, new GridPosition(3, 0)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            UnitActionResult actionResult = simulation.ResolvePlayerAutoAction("player-rider");

            Assert.NotNull(actionResult);
            Assert.NotEqual(actionResult.StartPosition, actionResult.EndPosition);
            Assert.True(actionResult.PerformedAttack);
            Assert.True(simulation.Context.GetUnit("player-rider").HasActed);
        }

        [Fact]
        public void BuildPlayerAutoTurnOrder_ExcludesActedUnitsAndAllowsEnemyHandoff()
        {
            UnitDefinitionData playerA = CreateDefinition(
                "player-a",
                "Player A",
                UnitFaction.Player,
                UnitRole.Commander,
                PassiveSkillType.None,
                ActiveSkillType.None,
                moveRange: 2,
                aiProfile: AiProfileType.Protector);
            UnitDefinitionData playerB = CreateDefinition(
                "player-b",
                "Player B",
                UnitFaction.Player,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.None,
                moveRange: 2,
                aiProfile: AiProfileType.Protector);
            UnitDefinitionData enemy = CreateDefinition(
                "enemy-1",
                "Enemy",
                UnitFaction.Enemy,
                UnitRole.Raider,
                PassiveSkillType.None,
                ActiveSkillType.None,
                moveRange: 2);

            StageDefinitionData stage = new StageDefinitionData(
                "Player Auto Turn Order",
                "stage.player_auto_turn_order",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(playerA, new GridPosition(0, 0)),
                    new UnitSpawnData(playerB, new GridPosition(1, 0)),
                    new UnitSpawnData(enemy, new GridPosition(8, 8)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            IReadOnlyList<string> firstOrder = simulation.BuildPlayerAutoTurnOrder();
            Assert.Equal(2, firstOrder.Count);

            UnitActionResult firstAction = simulation.ResolvePlayerAutoAction(firstOrder[0]);
            Assert.NotNull(firstAction);

            IReadOnlyList<string> secondOrder = simulation.BuildPlayerAutoTurnOrder();
            Assert.Single(secondOrder);
            Assert.DoesNotContain(firstOrder[0], secondOrder);

            UnitActionResult secondAction = simulation.ResolvePlayerAutoAction(secondOrder[0]);
            Assert.NotNull(secondAction);
            Assert.True(simulation.AreAllUnitsDone(UnitFaction.Player));
            Assert.Equal(TurnSide.Enemy, simulation.EndCurrentTurn());
            Assert.Equal(TurnSide.Enemy, simulation.Context.CurrentTurnSide);
        }

        [Fact]
        public void EnemyAi_SupportAvoidsLowValueVolleyAgainstSingleTarget()
        {
            UnitDefinitionData player = CreateDefinition(
                "player-isolated",
                "Isolated",
                UnitFaction.Player,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 22,
                defense: 4);
            UnitDefinitionData enemy = CreateDefinition(
                "enemy-support",
                "Enemy Support",
                UnitFaction.Enemy,
                UnitRole.Ranger,
                PassiveSkillType.LongShot,
                ActiveSkillType.Volley,
                attack: 10,
                attackRange: 1,
                aiProfile: AiProfileType.Support);

            StageDefinitionData stage = new StageDefinitionData(
                "Single Target Volley",
                "stage.single_target_volley",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(player, new GridPosition(2, 2)),
                    new UnitSpawnData(enemy, new GridPosition(4, 2)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.EndCurrentTurn();

            AiDecision decision = simulation.BuildEnemyDecision("enemy-support");

            Assert.NotEqual(AiActionType.Skill, decision.ActionType);
        }

        [Fact]
        public void EnemyAi_ProtectorMovesToJiangxiaBridgeHeadInsteadOfDrifting()
        {
            UnitDefinitionData player = CreateDefinition(
                "player-front",
                "Front",
                UnitFaction.Player,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.None);
            UnitDefinitionData enemyProtector = CreateDefinition(
                "enemy-bridge-guard",
                "Bridge Guard",
                UnitFaction.Enemy,
                UnitRole.Guardian,
                PassiveSkillType.ShieldWall,
                ActiveSkillType.None,
                moveRange: 3,
                aiProfile: AiProfileType.Protector);
            UnitDefinitionData enemySupport = CreateDefinition(
                "enemy-bridge-bow",
                "Bridge Bow",
                UnitFaction.Enemy,
                UnitRole.Ranger,
                PassiveSkillType.LongShot,
                ActiveSkillType.Volley,
                attackRange: 2,
                aiProfile: AiProfileType.Support);

            List<GridPosition> blockedCells = new List<GridPosition>();
            for (int x = 7; x <= 10; x++)
            {
                for (int y = 0; y < 14; y++)
                {
                    if (y == 2 || y == 6 || y == 7 || y == 11)
                    {
                        continue;
                    }

                    blockedCells.Add(new GridPosition(x, y));
                }
            }

            StageDefinitionData stage = new StageDefinitionData(
                "Jiangxia Test",
                "stage.jiangxia_ferry",
                18,
                14,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(player, new GridPosition(4, 6)),
                    new UnitSpawnData(enemyProtector, new GridPosition(14, 6)),
                    new UnitSpawnData(enemySupport, new GridPosition(14, 5)),
                },
                blockedCells);

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.EndCurrentTurn();

            AiDecision decision = simulation.BuildEnemyDecision("enemy-bridge-guard");

            Assert.Equal(AiActionType.None, decision.ActionType);
            Assert.Equal(new GridPosition(11, 6), decision.Destination);
        }

        [Fact]
        public void EnemyAi_AggressorCoordinatesOnSharedFocusTarget()
        {
            UnitDefinitionData healthyTarget = CreateDefinition(
                "player-healthy",
                "Healthy",
                UnitFaction.Player,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 24,
                defense: 5);
            UnitDefinitionData woundedTarget = CreateDefinition(
                "player-wounded",
                "Wounded",
                UnitFaction.Player,
                UnitRole.Commander,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 20,
                defense: 3);
            UnitDefinitionData enemyAggressor = CreateDefinition(
                "enemy-aggressor",
                "Enemy Aggressor",
                UnitFaction.Enemy,
                UnitRole.Raider,
                PassiveSkillType.RapidMarch,
                ActiveSkillType.PowerStrike,
                attack: 10,
                moveRange: 4,
                aiProfile: AiProfileType.Aggressor);
            UnitDefinitionData enemySupport = CreateDefinition(
                "enemy-cover",
                "Enemy Cover",
                UnitFaction.Enemy,
                UnitRole.Ranger,
                PassiveSkillType.LongShot,
                ActiveSkillType.Volley,
                attackRange: 2,
                aiProfile: AiProfileType.Support);

            StageDefinitionData stage = new StageDefinitionData(
                "Focus Fire",
                "stage.focus_fire",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(healthyTarget, new GridPosition(2, 3)),
                    new UnitSpawnData(woundedTarget, new GridPosition(2, 5)),
                    new UnitSpawnData(enemyAggressor, new GridPosition(6, 4)),
                    new UnitSpawnData(enemySupport, new GridPosition(7, 5)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.Context.GetUnit("player-wounded").ApplyDamage(9);
            simulation.EndCurrentTurn();

            AiDecision decision = simulation.BuildEnemyDecision("enemy-aggressor");

            Assert.Equal("player-wounded", decision.TargetUnitId);
            Assert.NotEqual(AiActionType.None, decision.ActionType);
        }

        [Fact]
        public void EnemyAi_BossHoldsGuangzongInnerLineBeforeReinforcements()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateGuangzong();
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            simulation.EndCurrentTurn();

            AiDecision decision = simulation.BuildEnemyDecision("enemy-zhang-bao");

            Assert.True(decision.Destination.X >= 12, $"Expected Zhang Bao to hold the inner line, but moved to {decision.Destination}.");
        }

        [Fact]
        public void TurnSwitch_ResetsActedFlagForNextSide()
        {
            BattleSimulation simulation = CreateSimulation();

            simulation.Wait("player-1");
            simulation.Wait("player-2");
            TurnSide turnSide = simulation.EndCurrentTurn();

            Assert.Equal(TurnSide.Enemy, turnSide);
            Assert.All(simulation.GetUnits(UnitFaction.Enemy), unit => Assert.False(unit.HasActed));
        }

        [Fact]
        public void TurnSwitch_AdvancesTurnNumberWhenEnemyPhaseEnds()
        {
            BattleSimulation simulation = CreateSimulation();

            Assert.Equal(1, simulation.Context.TurnNumber);

            simulation.Wait("player-1");
            simulation.Wait("player-2");
            simulation.EndCurrentTurn();
            Assert.Equal(1, simulation.Context.TurnNumber);

            simulation.Wait("enemy-1");
            simulation.Wait("enemy-2");
            simulation.Wait("enemy-3");
            simulation.EndCurrentTurn();

            Assert.Equal(2, simulation.Context.TurnNumber);
        }

        [Fact]
        public void BattleThreatAnalyzer_FindsDirectAndSplashThreats()
        {
            UnitDefinitionData focus = CreateDefinition("player-focus", "Focus", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, defense: 5);
            UnitDefinitionData ally = CreateDefinition("player-ally", "Ally", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.None, defense: 5);
            UnitDefinitionData raider = CreateDefinition("enemy-raider", "Raider", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, attack: 9, moveRange: 3);
            UnitDefinitionData archer = CreateDefinition("enemy-archer", "Archer", UnitFaction.Enemy, UnitRole.Ranger, PassiveSkillType.None, ActiveSkillType.Volley, attack: 10, moveRange: 3);

            StageDefinitionData stage = new StageDefinitionData(
                "Threat Stage",
                "stage.threat_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(focus, new GridPosition(1, 1)),
                    new UnitSpawnData(ally, new GridPosition(1, 2)),
                    new UnitSpawnData(raider, new GridPosition(4, 1)),
                    new UnitSpawnData(archer, new GridPosition(4, 2)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            BattleThreatSummary threatSummary = BattleThreatAnalyzer.Analyze(simulation.Context, simulation.Context.GetUnit("player-focus"));

            Assert.Equal(2, threatSummary.ThreateningEnemyCount);
            Assert.Equal(6, threatSummary.MaxProjectedDamage);
            Assert.Contains("enemy-raider", threatSummary.ThreateningUnitIds);
            Assert.Contains("enemy-archer", threatSummary.ThreateningUnitIds);
        }

        [Fact]
        public void BattleThreatAnalyzer_ProjectedThreatChangesWithDestination()
        {
            BattleSimulation simulation = CreateSimulation();
            UnitRuntimeState focus = simulation.Context.GetUnit("player-1");

            BattleThreatProjection originThreat = BattleThreatAnalyzer.AnalyzeProjected(simulation.Context, focus, focus.Position);
            BattleThreatProjection advancedThreat = BattleThreatAnalyzer.AnalyzeProjected(simulation.Context, focus, new GridPosition(1, 1));

            Assert.NotNull(originThreat);
            Assert.NotNull(advancedThreat);
            Assert.True(advancedThreat.ThreateningEnemyCount >= originThreat.ThreateningEnemyCount);
            Assert.True(advancedThreat.MaxProjectedDamage >= originThreat.MaxProjectedDamage);
        }

        [Fact]
        public void PassiveSkill_LongShot_ExtendsAttackRange()
        {
            UnitDefinitionData rangerDefinition = CreateDefinition(
                "player-ranger",
                "Ranger",
                UnitFaction.Player,
                UnitRole.Ranger,
                PassiveSkillType.LongShot,
                ActiveSkillType.None,
                attackRange: 1);
            UnitDefinitionData enemyDefinition = CreateDefinition(
                "enemy-1",
                "Bandit",
                UnitFaction.Enemy,
                UnitRole.Raider,
                PassiveSkillType.None,
                ActiveSkillType.None);

            StageDefinitionData stage = new StageDefinitionData(
                "Skill Stage",
                "stage.skill_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(rangerDefinition, new GridPosition(0, 0)),
                    new UnitSpawnData(enemyDefinition, new GridPosition(2, 0)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            CombatResult result = simulation.TryAttack("player-ranger", "enemy-1");

            Assert.NotNull(result);
        }

        [Fact]
        public void Treasure_JiangxiaRiverReins_IgnoresHazardEndTurnDamage()
        {
            UnitDefinitionData rider = new UnitDefinitionData(
                "player-rider",
                "Rider",
                "unit.player_rider",
                UnitFaction.Player,
                UnitRole.Commander,
                "role.commander",
                PassiveSkillType.None,
                "skill.none.name",
                "skill.none.desc",
                ActiveSkillType.None,
                "active.none.name",
                "active.none.desc",
                30,
                10,
                5,
                3,
                1,
                20,
                "commander",
                "commander",
                AiProfileType.Default,
                new EquipmentLoadout(string.Empty, string.Empty, "jiangxia-river-reins"),
                1,
                0,
                null,
                true);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None);

            StageDefinitionData stage = new StageDefinitionData(
                "River Reins Hazard",
                "stage.river_reins_hazard",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(rider, new GridPosition(1, 1)),
                    new UnitSpawnData(enemy, new GridPosition(6, 6)),
                },
                new List<GridPosition>(),
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(1, 1), TerrainType.Hazard),
                });

            BattleSimulation simulation = new BattleSimulation(stage);

            simulation.Wait("player-rider");
            simulation.EndCurrentTurn();

            Assert.Equal(30, simulation.Context.GetUnit("player-rider").CurrentHp);
        }

        private static BattleSimulation CreateSimulation(
            int enemyHpOverride = 20,
            int enemyDefenseOverride = 3,
            int playerAttackOverride = 10,
            IEnumerable<GridPosition> blockedCells = null)
        {
            UnitDefinitionData playerDefinition = CreateDefinition("player-1", "Hero", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.None, attack: playerAttackOverride);
            UnitDefinitionData playerTwoDefinition = CreateDefinition("player-2", "Knight", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None);
            UnitDefinitionData enemyOne = CreateDefinition("enemy-1", "Bandit A", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: enemyHpOverride, attack: 8, defense: enemyDefenseOverride);
            UnitDefinitionData enemyTwo = CreateDefinition("enemy-2", "Bandit B", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: enemyHpOverride, attack: 8, defense: enemyDefenseOverride);
            UnitDefinitionData enemyThree = CreateDefinition("enemy-3", "Bandit C", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: enemyHpOverride, attack: 8, defense: enemyDefenseOverride);

            StageDefinitionData stage = new StageDefinitionData(
                "Test Stage",
                "stage.test",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(playerDefinition, new GridPosition(0, 0)),
                    new UnitSpawnData(playerTwoDefinition, new GridPosition(1, 0)),
                    new UnitSpawnData(enemyOne, new GridPosition(2, 1)),
                    new UnitSpawnData(enemyTwo, new GridPosition(3, 2)),
                    new UnitSpawnData(enemyThree, new GridPosition(4, 2)),
                },
                blockedCells != null ? blockedCells.ToList() : new List<GridPosition>());

            return new BattleSimulation(stage);
        }

        private static BattleSimulation CreateAdjacentCombatSimulation(
            int enemyHpOverride = 20,
            int enemyDefenseOverride = 3,
            int playerAttackOverride = 10)
        {
            UnitDefinitionData playerDefinition = CreateDefinition("player-1", "Hero", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.None, attack: playerAttackOverride);
            UnitDefinitionData enemyDefinition = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: enemyHpOverride, attack: 8, defense: enemyDefenseOverride);

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

        private static BattleSimulation CreateSingleEnemySimulation(int enemyHpOverride)
        {
            UnitDefinitionData playerDefinition = CreateDefinition("player-1", "Hero", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.None);
            UnitDefinitionData enemyDefinition = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: enemyHpOverride, attack: 8, defense: 3);

            StageDefinitionData stage = new StageDefinitionData(
                "Single Enemy",
                "stage.single_enemy",
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

        [Fact]
        public void ActiveSkill_RoyalAid_HealsAlly()
        {
            UnitDefinitionData healer = CreateDefinition("player-healer", "Healer", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.RoyalAid);
            UnitDefinitionData ally = CreateDefinition("player-ally", "Ally", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None);

            StageDefinitionData stage = new StageDefinitionData(
                "Heal Stage",
                "stage.heal_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(healer, new GridPosition(0, 0)),
                    new UnitSpawnData(ally, new GridPosition(1, 0)),
                    new UnitSpawnData(enemy, new GridPosition(5, 5)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.Context.GetUnit("player-ally").ApplyDamage(10);

            SkillResult result = simulation.TryUseSkill("player-healer", "player-ally");

            Assert.NotNull(result);
            Assert.Single(result.Effects);
            Assert.True(result.Effects[0].IsHealing);
            Assert.Equal(StatusEffectType.Inspired, result.Effects[0].AppliedStatus);
            Assert.True(simulation.Context.GetUnit("player-ally").HasStatus(StatusEffectType.Inspired));
            Assert.Equal(28, simulation.Context.GetUnit("player-ally").CurrentHp);
        }

        [Fact]
        public void ActiveSkill_GuardOrder_CanTargetFullHealthAlly_AndAppliesGuarded()
        {
            UnitDefinitionData commander = CreateDefinition("player-commander", "Commander", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.GuardOrder);
            UnitDefinitionData ally = CreateDefinition("player-ally", "Ally", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None);

            StageDefinitionData stage = new StageDefinitionData(
                "Guard Order Stage",
                "stage.guard_order_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(commander, new GridPosition(0, 0)),
                    new UnitSpawnData(ally, new GridPosition(2, 0)),
                    new UnitSpawnData(enemy, new GridPosition(6, 6)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            Assert.Contains(simulation.GetSkillTargets("player-commander"), unit => unit.Id == "player-ally");

            SkillResult result = simulation.TryUseSkill("player-commander", "player-ally");

            Assert.NotNull(result);
            Assert.Single(result.Effects);
            Assert.Equal(StatusEffectType.Guarded, result.Effects[0].AppliedStatus);
            Assert.True(simulation.Context.GetUnit("player-ally").HasStatus(StatusEffectType.Guarded));
            Assert.Equal(30, simulation.Context.GetUnit("player-ally").CurrentHp);
        }

        [Fact]
        public void ActiveSkill_KingsBanner_HealsPrimaryAndInspiresAdjacentAllies()
        {
            UnitDefinitionData liuBei = CreateDefinition("player-liu-bei", "Liu Bei", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.KingsBanner);
            UnitDefinitionData primary = CreateDefinition("player-primary", "Primary", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30);
            UnitDefinitionData adjacent = CreateDefinition("player-adjacent", "Adjacent", UnitFaction.Player, UnitRole.Ranger, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None);

            StageDefinitionData stage = new StageDefinitionData(
                "King's Banner Stage",
                "stage.kings_banner_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(liuBei, new GridPosition(0, 0)),
                    new UnitSpawnData(primary, new GridPosition(1, 0)),
                    new UnitSpawnData(adjacent, new GridPosition(1, 1)),
                    new UnitSpawnData(enemy, new GridPosition(6, 6)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.Context.GetUnit("player-primary").ApplyDamage(10);

            SkillResult result = simulation.TryUseSkill("player-liu-bei", "player-primary");

            Assert.NotNull(result);
            Assert.True(simulation.Context.GetUnit("player-primary").CurrentHp > 20);
            Assert.True(simulation.Context.GetUnit("player-primary").HasStatus(StatusEffectType.Guarded));
            Assert.True(simulation.Context.GetUnit("player-primary").HasStatus(StatusEffectType.Inspired));
            Assert.True(simulation.Context.GetUnit("player-adjacent").HasStatus(StatusEffectType.Inspired));
        }

        [Fact]
        public void ActiveSkill_FeatherFormation_GuardsFormationAndHealsLowestHpAlly()
        {
            UnitDefinitionData zhuge = CreateDefinition("player-zhuge", "Zhuge Liang", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.FeatherFormation);
            UnitDefinitionData primary = CreateDefinition("player-primary", "Primary", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30);
            UnitDefinitionData adjacent = CreateDefinition("player-adjacent", "Adjacent", UnitFaction.Player, UnitRole.Ranger, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24);
            UnitDefinitionData distant = CreateDefinition("player-distant", "Distant", UnitFaction.Player, UnitRole.Scout, PassiveSkillType.None, ActiveSkillType.None, maxHp: 26);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None);

            StageDefinitionData stage = new StageDefinitionData(
                "Feather Formation Stage",
                "stage.feather_formation_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(zhuge, new GridPosition(0, 0)),
                    new UnitSpawnData(primary, new GridPosition(1, 0)),
                    new UnitSpawnData(adjacent, new GridPosition(1, 1)),
                    new UnitSpawnData(distant, new GridPosition(4, 4)),
                    new UnitSpawnData(enemy, new GridPosition(8, 8)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.Context.GetUnit("player-distant").ApplyDamage(12);

            SkillResult result = simulation.TryUseSkill("player-zhuge", "player-primary");

            Assert.NotNull(result);
            Assert.True(simulation.Context.GetUnit("player-primary").HasStatus(StatusEffectType.Guarded));
            Assert.True(simulation.Context.GetUnit("player-primary").HasStatus(StatusEffectType.Inspired));
            Assert.True(simulation.Context.GetUnit("player-adjacent").HasStatus(StatusEffectType.Guarded));
            Assert.True(simulation.Context.GetUnit("player-distant").CurrentHp > 14);
            Assert.True(simulation.Context.GetUnit("player-distant").HasStatus(StatusEffectType.Guarded));
        }

        [Fact]
        public void ActiveSkill_CanBeUsedWithoutMoving()
        {
            UnitDefinitionData healer = CreateDefinition("player-healer", "Healer", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.RoyalAid);
            UnitDefinitionData ally = CreateDefinition("player-ally", "Ally", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30);

            StageDefinitionData stage = new StageDefinitionData(
                "Hold Skill Stage",
                "stage.hold_skill_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(healer, new GridPosition(0, 0)),
                    new UnitSpawnData(ally, new GridPosition(1, 0)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.Context.GetUnit("player-ally").ApplyDamage(6);

            SkillResult result = simulation.TryUseSkill("player-healer", "player-ally");

            Assert.NotNull(result);
            Assert.False(simulation.Context.GetUnit("player-healer").HasMovedThisTurn);
            Assert.Equal(30, simulation.Context.GetUnit("player-ally").CurrentHp);
        }

        [Fact]
        public void Preview_RoyalAidEstimate_MatchesResolvedHeal()
        {
            UnitDefinitionData healer = CreateDefinition("player-healer", "Healer", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.RoyalAid);
            UnitDefinitionData ally = CreateDefinition("player-ally", "Ally", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30);

            StageDefinitionData stage = new StageDefinitionData(
                "Heal Preview Stage",
                "stage.heal_preview",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(healer, new GridPosition(0, 0)),
                    new UnitSpawnData(ally, new GridPosition(1, 0)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.Context.GetUnit("player-ally").ApplyDamage(10);

            int estimatedHeal = BattlePreviewCalculator.EstimateHealing(simulation.Context.GetUnit("player-ally"), ActiveSkillRules.GetRoyalAidAmount());
            SkillResult result = simulation.TryUseSkill("player-healer", "player-ally");

            Assert.NotNull(result);
            Assert.Equal(estimatedHeal, result.Effects[0].Amount);
        }

        [Fact]
        public void ActiveSkill_Volley_HitsAdjacentEnemies()
        {
            UnitDefinitionData archer = CreateDefinition("player-archer", "Archer", UnitFaction.Player, UnitRole.Ranger, PassiveSkillType.None, ActiveSkillType.Volley, attack: 10);
            UnitDefinitionData enemyOne = CreateDefinition("enemy-1", "Bandit A", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 2);
            UnitDefinitionData enemyTwo = CreateDefinition("enemy-2", "Bandit B", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 2);

            StageDefinitionData stage = new StageDefinitionData(
                "Volley Stage",
                "stage.volley_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(archer, new GridPosition(0, 0)),
                    new UnitSpawnData(enemyOne, new GridPosition(2, 0)),
                    new UnitSpawnData(enemyTwo, new GridPosition(2, 1)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            SkillResult result = simulation.TryUseSkill("player-archer", "enemy-1");

            Assert.NotNull(result);
            Assert.Equal(2, result.Effects.Count);
            Assert.Equal(9, result.Effects[0].Amount);
            Assert.Equal(StatusEffectType.ShatteredArmor, result.Effects[0].AppliedStatus);
            Assert.True(simulation.Context.GetUnit("enemy-1").HasStatus(StatusEffectType.ShatteredArmor));
            Assert.Equal(11, simulation.Context.GetUnit("enemy-1").CurrentHp);
            Assert.Equal(11, simulation.Context.GetUnit("enemy-2").CurrentHp);
        }

        [Fact]
        public void Preview_VolleyEstimate_MatchesResolvedDamage()
        {
            UnitDefinitionData archer = CreateDefinition("player-archer", "Archer", UnitFaction.Player, UnitRole.Ranger, PassiveSkillType.None, ActiveSkillType.Volley, attack: 10);
            UnitDefinitionData enemyOne = CreateDefinition("enemy-1", "Bandit A", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 2);
            UnitDefinitionData enemyTwo = CreateDefinition("enemy-2", "Bandit B", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 2);

            StageDefinitionData stage = new StageDefinitionData(
                "Volley Preview Stage",
                "stage.volley_preview",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(archer, new GridPosition(0, 0)),
                    new UnitSpawnData(enemyOne, new GridPosition(2, 0)),
                    new UnitSpawnData(enemyTwo, new GridPosition(2, 1)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            UnitRuntimeState caster = simulation.Context.GetUnit("player-archer");
            UnitRuntimeState primaryTarget = simulation.Context.GetUnit("enemy-1");
            IReadOnlyList<UnitRuntimeState> previewTargets = BattlePreviewCalculator.GetVolleyTargets(simulation.Context, primaryTarget);
            List<int> previewDamage = previewTargets
                .Select(target => BattlePreviewCalculator.EstimateAttackDamage(
                    simulation.Context,
                    caster,
                    caster.Position,
                    target,
                    ActiveSkillRules.GetVolleyBonus()))
                .ToList();

            SkillResult result = simulation.TryUseSkill("player-archer", "enemy-1");

            Assert.NotNull(result);
            Assert.Equal(previewDamage, result.Effects.Select(effect => effect.Amount).ToList());
        }

        [Fact]
        public void ActiveSkill_PinningShot_AppliesRootedToSurvivingTarget()
        {
            UnitDefinitionData archer = CreateDefinition("player-archer", "Archer", UnitFaction.Player, UnitRole.Ranger, PassiveSkillType.None, ActiveSkillType.PinningShot, attack: 10, attackRange: 2);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 3);

            StageDefinitionData stage = new StageDefinitionData(
                "Pinning Shot Stage",
                "stage.pinning_shot_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(archer, new GridPosition(0, 0)),
                    new UnitSpawnData(enemy, new GridPosition(3, 0)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            SkillResult result = simulation.TryUseSkill("player-archer", "enemy-1");

            Assert.NotNull(result);
            Assert.Single(result.Effects);
            Assert.Equal(StatusEffectType.Rooted, result.Effects[0].AppliedStatus);
            Assert.True(simulation.Context.GetUnit("enemy-1").HasStatus(StatusEffectType.Rooted));
            Assert.True(simulation.Context.GetUnit("enemy-1").CurrentHp > 0);
        }

        [Fact]
        public void ActiveSkill_GreenDragonSlash_HitsEnemyBehindPrimary()
        {
            UnitDefinitionData striker = CreateDefinition("player-striker", "Striker", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.GreenDragonSlash, attack: 10);
            UnitDefinitionData enemyOne = CreateDefinition("enemy-1", "Bandit A", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 1);
            UnitDefinitionData enemyTwo = CreateDefinition("enemy-2", "Bandit B", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 1);

            StageDefinitionData stage = new StageDefinitionData(
                "Dragon Slash Stage",
                "stage.dragon_slash_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(striker, new GridPosition(0, 0)),
                    new UnitSpawnData(enemyOne, new GridPosition(1, 0)),
                    new UnitSpawnData(enemyTwo, new GridPosition(2, 0)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            SkillResult result = simulation.TryUseSkill("player-striker", "enemy-1");

            Assert.NotNull(result);
            Assert.Equal(2, result.Effects.Count);
            Assert.All(result.Effects, effect => Assert.Equal(11, effect.Amount));
            Assert.Equal(9, simulation.Context.GetUnit("enemy-1").CurrentHp);
            Assert.Equal(9, simulation.Context.GetUnit("enemy-2").CurrentHp);
        }

        [Fact]
        public void Preview_PowerStrikeEstimate_MatchesResolvedDamage()
        {
            UnitDefinitionData striker = CreateDefinition("player-striker", "Striker", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.PowerStrike, attack: 10);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 3);

            StageDefinitionData stage = new StageDefinitionData(
                "Power Strike Preview",
                "stage.power_strike_preview",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(striker, new GridPosition(0, 0)),
                    new UnitSpawnData(enemy, new GridPosition(1, 0)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            UnitRuntimeState caster = simulation.Context.GetUnit("player-striker");
            UnitRuntimeState target = simulation.Context.GetUnit("enemy-1");
            int estimatedDamage = BattlePreviewCalculator.EstimateAttackDamage(
                simulation.Context,
                caster,
                caster.Position,
                target,
                ActiveSkillRules.GetPowerStrikeBonus());

            SkillResult result = simulation.TryUseSkill("player-striker", "enemy-1");

            Assert.NotNull(result);
            Assert.Equal(estimatedDamage, result.Effects[0].Amount);
        }

        [Fact]
        public void ActiveSkill_DragonPierce_GuardsCasterAndLowestAdjacentAlly()
        {
            UnitDefinitionData zhaoYun = CreateDefinition("player-zhao-yun", "Zhao Yun", UnitFaction.Player, UnitRole.Scout, PassiveSkillType.RapidMarch, ActiveSkillType.DragonPierce, attack: 10);
            UnitDefinitionData ally = CreateDefinition("player-ally", "Ally", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24, defense: 3);

            StageDefinitionData stage = new StageDefinitionData(
                "Dragon Pierce Stage",
                "stage.dragon_pierce_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(zhaoYun, new GridPosition(1, 1)),
                    new UnitSpawnData(ally, new GridPosition(1, 2)),
                    new UnitSpawnData(enemy, new GridPosition(2, 1)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.Context.GetUnit("player-ally").ApplyDamage(12);

            SkillResult result = simulation.TryUseSkill("player-zhao-yun", "enemy-1");

            Assert.NotNull(result);
            Assert.Equal(3, result.Effects.Count);
            Assert.Equal("enemy-1", result.Effects[0].UnitId);
            Assert.True(result.Effects[0].Amount >= 10);
            Assert.True(simulation.Context.GetUnit("player-zhao-yun").HasStatus(StatusEffectType.Guarded));
            Assert.True(simulation.Context.GetUnit("player-ally").HasStatus(StatusEffectType.Guarded));
        }

        [Fact]
        public void ActiveSkill_WhiteHorseRescue_HitsAtRangeAndGuardsNearestAlly()
        {
            UnitDefinitionData zhaoYun = CreateDefinition("player-zhao-yun", "Zhao Yun", UnitFaction.Player, UnitRole.Scout, PassiveSkillType.GaleStride, ActiveSkillType.WhiteHorseRescue, attack: 11);
            UnitDefinitionData ally = CreateDefinition("player-ally", "Ally", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 28, defense: 6);

            StageDefinitionData stage = new StageDefinitionData(
                "White Horse Rescue Stage",
                "stage.white_horse_rescue_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(zhaoYun, new GridPosition(1, 1)),
                    new UnitSpawnData(ally, new GridPosition(1, 2)),
                    new UnitSpawnData(enemy, new GridPosition(3, 1)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.Context.GetUnit("player-ally").ApplyDamage(8);

            SkillResult result = simulation.TryUseSkill("player-zhao-yun", "enemy-1");

            Assert.NotNull(result);
            Assert.Equal(3, result.Effects.Count);
            Assert.True(result.Effects[0].Amount >= 8);
            Assert.True(simulation.Context.GetUnit("player-zhao-yun").HasStatus(StatusEffectType.Guarded));
            Assert.True(simulation.Context.GetUnit("player-ally").HasStatus(StatusEffectType.Guarded));
        }

        [Fact]
        public void ActiveSkill_WesternStampede_HitsLineAndAppliesIntimidated()
        {
            UnitDefinitionData maChao = CreateDefinition("player-ma-chao", "Ma Chao", UnitFaction.Player, UnitRole.Raider, PassiveSkillType.Vanguard, ActiveSkillType.WesternStampede, attack: 10);
            UnitDefinitionData enemyOne = CreateDefinition("enemy-1", "Bandit A", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24, defense: 2);
            UnitDefinitionData enemyTwo = CreateDefinition("enemy-2", "Bandit B", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24, defense: 2);

            StageDefinitionData stage = new StageDefinitionData(
                "Western Stampede Stage",
                "stage.western_stampede_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(maChao, new GridPosition(1, 1)),
                    new UnitSpawnData(enemyOne, new GridPosition(2, 1)),
                    new UnitSpawnData(enemyTwo, new GridPosition(3, 1)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            SkillResult result = simulation.TryUseSkill("player-ma-chao", "enemy-1");

            Assert.NotNull(result);
            Assert.Equal(2, result.Effects.Count);
            Assert.True(simulation.Context.GetUnit("enemy-1").HasStatus(StatusEffectType.Intimidated));
            Assert.True(simulation.Context.GetUnit("enemy-2").HasStatus(StatusEffectType.Intimidated));
            Assert.True(result.Effects.All(effect => effect.Amount >= 9));
        }

        [Fact]
        public void ActiveSkill_CrimsonCrescent_ShattersPrimaryAndBleedsSecondary()
        {
            UnitDefinitionData guanYu = CreateDefinition("player-guan-yu", "Guan Yu", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.ArmorBreak, ActiveSkillType.CrimsonCrescent, attack: 12);
            UnitDefinitionData enemyOne = CreateDefinition("enemy-1", "Bandit A", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 28, defense: 4);
            UnitDefinitionData enemyTwo = CreateDefinition("enemy-2", "Bandit B", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 28, defense: 4);

            StageDefinitionData stage = new StageDefinitionData(
                "Crimson Crescent Stage",
                "stage.crimson_crescent_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(guanYu, new GridPosition(1, 1)),
                    new UnitSpawnData(enemyOne, new GridPosition(2, 1)),
                    new UnitSpawnData(enemyTwo, new GridPosition(3, 1)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            SkillResult result = simulation.TryUseSkill("player-guan-yu", "enemy-1");

            Assert.NotNull(result);
            Assert.Equal(2, result.Effects.Count);
            Assert.True(simulation.Context.GetUnit("enemy-1").HasStatus(StatusEffectType.ShatteredArmor));
            Assert.True(simulation.Context.GetUnit("enemy-2").HasStatus(StatusEffectType.Bleeding));
        }

        [Fact]
        public void ActiveSkill_StormbreakCharge_HitsLineAndBleedsPrimary()
        {
            UnitDefinitionData maChao = CreateDefinition("player-ma-chao", "Ma Chao", UnitFaction.Player, UnitRole.Raider, PassiveSkillType.ThunderVanguard, ActiveSkillType.StormbreakCharge, attack: 11);
            UnitDefinitionData enemyOne = CreateDefinition("enemy-1", "Bandit A", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 28, defense: 3);
            UnitDefinitionData enemyTwo = CreateDefinition("enemy-2", "Bandit B", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 28, defense: 3);

            StageDefinitionData stage = new StageDefinitionData(
                "Stormbreak Charge Stage",
                "stage.stormbreak_charge_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(maChao, new GridPosition(1, 1)),
                    new UnitSpawnData(enemyOne, new GridPosition(2, 1)),
                    new UnitSpawnData(enemyTwo, new GridPosition(3, 1)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            SkillResult result = simulation.TryUseSkill("player-ma-chao", "enemy-1");

            Assert.NotNull(result);
            Assert.Equal(2, result.Effects.Count);
            Assert.True(simulation.Context.GetUnit("enemy-1").HasStatus(StatusEffectType.Intimidated));
            Assert.True(simulation.Context.GetUnit("enemy-2").HasStatus(StatusEffectType.Intimidated));
            Assert.True(simulation.Context.GetUnit("enemy-1").HasStatus(StatusEffectType.Bleeding));
        }

        [Fact]
        public void ActiveSkill_StonewallChallenge_TauntsNearbyEnemiesAndGuardsCaster()
        {
            UnitDefinitionData zhangFei = CreateDefinition("player-zhang-fei", "Zhang Fei", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.Fortress, ActiveSkillType.StonewallChallenge, attack: 10);
            UnitDefinitionData enemyOne = CreateDefinition("enemy-1", "Bandit A", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30, defense: 5);
            UnitDefinitionData enemyTwo = CreateDefinition("enemy-2", "Bandit B", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30, defense: 5);

            StageDefinitionData stage = new StageDefinitionData(
                "Stonewall Challenge Stage",
                "stage.stonewall_challenge_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(zhangFei, new GridPosition(2, 2)),
                    new UnitSpawnData(enemyOne, new GridPosition(2, 3)),
                    new UnitSpawnData(enemyTwo, new GridPosition(3, 2)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            SkillResult result = simulation.TryUseSkill("player-zhang-fei", "enemy-1");

            Assert.NotNull(result);
            Assert.True(simulation.Context.GetUnit("enemy-1").HasStatus(StatusEffectType.Taunted));
            Assert.True(simulation.Context.GetUnit("enemy-2").HasStatus(StatusEffectType.Taunted));
            Assert.True(simulation.Context.GetUnit("enemy-1").HasStatus(StatusEffectType.Intimidated));
            Assert.True(simulation.Context.GetUnit("player-zhang-fei").HasStatus(StatusEffectType.Guarded));
        }

        [Fact]
        public void ActiveSkill_DustDevilSweep_BleedsNearbyEnemiesAndInspiresCasterOnMultiHit()
        {
            UnitDefinitionData maChao = CreateDefinition("player-ma-chao", "Ma Chao", UnitFaction.Player, UnitRole.Raider, PassiveSkillType.GaleStride, ActiveSkillType.DustDevilSweep, attack: 10);
            UnitDefinitionData enemyOne = CreateDefinition("enemy-1", "Bandit A", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30, defense: 4);
            UnitDefinitionData enemyTwo = CreateDefinition("enemy-2", "Bandit B", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30, defense: 4);

            StageDefinitionData stage = new StageDefinitionData(
                "Dust Devil Sweep Stage",
                "stage.dust_devil_sweep_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(maChao, new GridPosition(2, 2)),
                    new UnitSpawnData(enemyOne, new GridPosition(2, 3)),
                    new UnitSpawnData(enemyTwo, new GridPosition(3, 2)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            SkillResult result = simulation.TryUseSkill("player-ma-chao", "enemy-1");

            Assert.NotNull(result);
            Assert.True(simulation.Context.GetUnit("enemy-1").HasStatus(StatusEffectType.Bleeding));
            Assert.True(simulation.Context.GetUnit("enemy-2").HasStatus(StatusEffectType.Bleeding));
            Assert.True(simulation.Context.GetUnit("player-ma-chao").HasStatus(StatusEffectType.Inspired));
        }

        [Fact]
        public void ActiveSkill_WarCry_AppliesIntimidatedUntilTargetTurnEnds()
        {
            UnitDefinitionData intimidator = CreateDefinition("player-intimidator", "Intimidator", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.WarCry);
            UnitDefinitionData ally = CreateDefinition("player-ally", "Ally", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.None);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, attack: 10);

            StageDefinitionData stage = new StageDefinitionData(
                "War Cry Stage",
                "stage.war_cry_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(intimidator, new GridPosition(1, 1)),
                    new UnitSpawnData(ally, new GridPosition(0, 1)),
                    new UnitSpawnData(enemy, new GridPosition(2, 1)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            SkillResult result = simulation.TryUseSkill("player-intimidator", "enemy-1");

            Assert.NotNull(result);
            Assert.True(simulation.Context.GetUnit("enemy-1").HasStatus(StatusEffectType.Intimidated));
            Assert.Equal(-2, StatusEffectRules.GetAttackModifier(simulation.Context.GetUnit("enemy-1")));

            simulation.Wait("player-ally");
            simulation.EndCurrentTurn();
            Assert.True(simulation.Context.GetUnit("enemy-1").HasStatus(StatusEffectType.Intimidated));

            simulation.Wait("enemy-1");
            simulation.EndCurrentTurn();
            Assert.False(simulation.Context.GetUnit("enemy-1").HasStatus(StatusEffectType.Intimidated));
        }

        [Fact]
        public void PassiveSkill_Vanguard_OnlyAddsDamageAfterMoving()
        {
            UnitDefinitionData vanguard = CreateDefinition("player-vanguard", "Vanguard", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.Vanguard, ActiveSkillType.None, attack: 10);
            UnitDefinitionData enemyAdjacent = CreateDefinition("enemy-adjacent", "Adjacent", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 5);
            UnitDefinitionData enemyFar = CreateDefinition("enemy-far", "Far", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 5);

            BattleSimulation adjacentSimulation = new BattleSimulation(
                new StageDefinitionData(
                    "Vanguard Adjacent",
                    "stage.vanguard_adjacent",
                    10,
                    10,
                    new List<UnitSpawnData>
                    {
                        new UnitSpawnData(vanguard, new GridPosition(0, 0)),
                        new UnitSpawnData(enemyAdjacent, new GridPosition(1, 0)),
                    },
                    new List<GridPosition>()));

            CombatResult adjacentResult = adjacentSimulation.TryAttack("player-vanguard", "enemy-adjacent");

            BattleSimulation movedSimulation = new BattleSimulation(
                new StageDefinitionData(
                    "Vanguard Moved",
                    "stage.vanguard_moved",
                    10,
                    10,
                    new List<UnitSpawnData>
                    {
                        new UnitSpawnData(vanguard, new GridPosition(0, 0)),
                        new UnitSpawnData(enemyFar, new GridPosition(2, 0)),
                    },
                    new List<GridPosition>()));

            bool moved = movedSimulation.TryMoveUnit("player-vanguard", new GridPosition(1, 0));
            CombatResult movedResult = movedSimulation.TryAttack("player-vanguard", "enemy-far");

            Assert.True(moved);
            Assert.NotNull(adjacentResult);
            Assert.NotNull(movedResult);
            Assert.Equal(adjacentResult.Damage + 2, movedResult.Damage);
        }

        [Fact]
        public void ActiveSkill_InspiredStatus_BoostsAttackThisTurn()
        {
            UnitDefinitionData healer = CreateDefinition("player-healer", "Healer", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.RoyalAid);
            UnitDefinitionData ally = CreateDefinition("player-ally", "Ally", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, attack: 10);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 5);

            StageDefinitionData stage = new StageDefinitionData(
                "Inspired Stage",
                "stage.inspired_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(healer, new GridPosition(0, 0)),
                    new UnitSpawnData(ally, new GridPosition(1, 0)),
                    new UnitSpawnData(enemy, new GridPosition(2, 0)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.Context.GetUnit("player-ally").ApplyDamage(6);

            SkillResult skillResult = simulation.TryUseSkill("player-healer", "player-ally");
            CombatResult attackResult = simulation.TryAttack("player-ally", "enemy-1");

            Assert.NotNull(skillResult);
            Assert.NotNull(attackResult);
            Assert.Equal(8, attackResult.Damage);
        }

        [Fact]
        public void ActiveSkill_ManaOnlyUsage_AllowsReuseOnLaterTurnsWithoutCooldown()
        {
            UnitDefinitionData striker = CreateDefinition("player-striker", "Striker", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.PowerStrike, attack: 10);
            UnitDefinitionData ally = CreateDefinition("player-ally", "Ally", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.None);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 3);

            StageDefinitionData stage = new StageDefinitionData(
                "Mana Stage",
                "stage.mana_stage",
                10,
                10,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(striker, new GridPosition(0, 0)),
                    new UnitSpawnData(ally, new GridPosition(0, 1)),
                    new UnitSpawnData(enemy, new GridPosition(1, 0)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            SkillResult firstCast = simulation.TryUseSkill("player-striker", "enemy-1");
            Assert.NotNull(firstCast);
            Assert.Equal(0, simulation.Context.GetUnit("player-striker").CurrentSkillCooldown);
            Assert.Null(simulation.TryUseSkill("player-striker", "enemy-1"));

            simulation.Wait("player-ally");
            simulation.EndCurrentTurn();
            simulation.EndCurrentTurn();

            SkillResult secondCast = simulation.TryUseSkill("player-striker", "enemy-1");
            Assert.NotNull(secondCast);
            Assert.Equal(0, simulation.Context.GetUnit("player-striker").CurrentSkillCooldown);
        }

        [Fact]
        public void EnemyAi_CanUseSkillsAcrossMultipleTurnsWithoutCooldownLock()
        {
            UnitDefinitionData player = CreateDefinition("player-1", "Hero", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, maxHp: 40, defense: 4);
            UnitDefinitionData enemy = CreateDefinition("enemy-strategist", "Strategist", UnitFaction.Enemy, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.FireStratagem, attack: 10, maxMana: 20);

            StageDefinitionData stage = new StageDefinitionData(
                "Enemy Mana Stage",
                "stage.enemy_mana_stage",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(player, new GridPosition(2, 2)),
                    new UnitSpawnData(enemy, new GridPosition(0, 2)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            simulation.EndCurrentTurn();
            UnitActionResult firstEnemyAction = simulation.ResolveEnemyAction("enemy-strategist");

            Assert.NotNull(firstEnemyAction);
            Assert.NotNull(firstEnemyAction.SkillResult);
            Assert.Equal(0, simulation.Context.GetUnit("enemy-strategist").CurrentSkillCooldown);

            simulation.Wait("player-1");
            simulation.EndCurrentTurn();
            simulation.EndCurrentTurn();

            UnitActionResult secondEnemyAction = simulation.ResolveEnemyAction("enemy-strategist");

            Assert.NotNull(secondEnemyAction);
            Assert.NotNull(secondEnemyAction.SkillResult);
            Assert.Equal(0, simulation.Context.GetUnit("enemy-strategist").CurrentSkillCooldown);
        }

        [Fact]
        public void Scenario_ReinforcementsSpawnOnceAndCancelPrematureVictory()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateGuangzong();
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);
            DefeatUnit(simulation.Context, "enemy-yellow_turban_raider");
            DefeatUnit(simulation.Context, "enemy-armored_zealot");
            DefeatUnit(simulation.Context, "enemy-zhang-bao");
            DefeatUnit(simulation.Context, "enemy-yellow_turban_archer");
            simulation.Context.EvaluateBattleOutcome();

            ScenarioEvaluationResult result = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);
            ScenarioEvaluationResult secondResult = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.Equal(3, result.SpawnedUnitIds.Count);
            Assert.False(simulation.Context.BattleEnded);
            Assert.NotNull(simulation.Context.GetUnit("enemy-zhang-liang"));
            Assert.Empty(secondResult.SpawnedUnitIds);
        }

        [Fact]
        public void Scenario_ObjectiveUpdatesAndBossKillsTriggerVictory()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateGuangzong();
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);
            Assert.Equal("objective.guangzong.opening", director.CurrentObjective.PrimaryObjectiveKey);

            DefeatUnit(simulation.Context, "enemy-yellow_turban_raider");
            DefeatUnit(simulation.Context, "enemy-armored_zealot");
            director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);
            Assert.Equal("objective.guangzong.final", director.CurrentObjective.PrimaryObjectiveKey);

            DefeatUnit(simulation.Context, "enemy-zhang-bao");
            DefeatUnit(simulation.Context, "enemy-zhang-liang");
            director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.True(simulation.Context.BattleEnded);
            Assert.Equal(TurnSide.Player, simulation.Context.WinningSide);
        }

        [Fact]
        public void Scenario_LiuBeiDeathTriggersImmediateDefeat()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateGuangzong();
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);
            DefeatUnit(simulation.Context, "player-liu-bei");
            director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.True(simulation.Context.BattleEnded);
            Assert.Equal(TurnSide.Enemy, simulation.Context.WinningSide);
        }

        [Fact]
        public void Changban_HoldObjectiveGrantsVictoryOnRoundFiveWhenLiuBeiLives()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateChangbanRearguard();
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);

            simulation.EndCurrentTurn();
            director.Evaluate(ScenarioCheckpoint.EnemyTurnStart, simulation.Context);
            simulation.EndCurrentTurn();

            simulation.EndCurrentTurn();
            director.Evaluate(ScenarioCheckpoint.EnemyTurnStart, simulation.Context);
            simulation.EndCurrentTurn();

            simulation.EndCurrentTurn();
            director.Evaluate(ScenarioCheckpoint.EnemyTurnStart, simulation.Context);
            simulation.EndCurrentTurn();

            simulation.EndCurrentTurn();
            director.Evaluate(ScenarioCheckpoint.EnemyTurnStart, simulation.Context);
            simulation.EndCurrentTurn();

            director.Evaluate(ScenarioCheckpoint.PlayerTurnStart, simulation.Context);

            Assert.True(simulation.Context.BattleEnded);
            Assert.Equal(TurnSide.Player, simulation.Context.WinningSide);
        }

        [Fact]
        public void Changban_LiuBeiDeathBeforeRoundFiveTriggersDefeat()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateChangbanRearguard();
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);
            DefeatUnit(simulation.Context, "player-liu-bei");
            director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.True(simulation.Context.BattleEnded);
            Assert.Equal(TurnSide.Enemy, simulation.Context.WinningSide);
        }

        [Fact]
        public void Dingjun_BossPhaseDoesNotStartUntilBothForwardCommandersFall()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateDingjunMountain();
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);
            DefeatUnit(simulation.Context, "enemy-wei_vanguard_captain");
            director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.False(director.HasFlag(BattleScenarioCatalog.DingjunBossArrivedFlag));
            Assert.Null(simulation.Context.GetUnit("enemy-xiahou-yuan"));

            DefeatUnit(simulation.Context, "enemy-wei_archer_captain");
            director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.True(director.HasFlag(BattleScenarioCatalog.DingjunBossArrivedFlag));
            Assert.NotNull(simulation.Context.GetUnit("enemy-xiahou-yuan"));
            Assert.Equal("objective.dingjun.final", director.CurrentObjective.PrimaryObjectiveKey);
        }

        [Fact]
        public void Bowangpo_FireTrapMutatesBattlefieldOnceAndUpdatesObjective()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateBowangpo();
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);
            DefeatUnit(simulation.Context, "enemy-bowang-vanguard");
            DefeatUnit(simulation.Context, "enemy-bowang-archer");
            DefeatUnit(simulation.Context, "enemy-bowang-rider");
            DefeatUnit(simulation.Context, "enemy-bowang-shield");
            simulation.Context.EvaluateBattleOutcome();

            ScenarioEvaluationResult result = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);
            ScenarioEvaluationResult secondResult = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.True(result.BattlefieldChanged);
            Assert.False(secondResult.BattlefieldChanged);
            Assert.Equal("objective.bowangpo.final", director.CurrentObjective.PrimaryObjectiveKey);
            Assert.Equal(TerrainType.Hazard, simulation.Context.GetTerrainAt(new GridPosition(7, 4)));
            Assert.True(simulation.Context.GetCell(new GridPosition(10, 1)).IsBlocked);
            Assert.NotNull(simulation.Context.GetUnit("enemy-xiahou-dun"));
        }

        [Fact]
        public void Jiangxia_BridgeCutMutatesBlockedCellsAndSpawnsBoss()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateJiangxiaFerry();
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);
            DefeatUnit(simulation.Context, "enemy-jiangxia-outer-warden-a");
            DefeatUnit(simulation.Context, "enemy-jiangxia-outer-warden-b");

            ScenarioEvaluationResult result = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);
            ScenarioEvaluationResult secondResult = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.True(result.BattlefieldChanged);
            Assert.False(secondResult.BattlefieldChanged);
            Assert.True(director.HasFlag(BattleScenarioCatalog.JiangxiaBridgesCutFlag));
            Assert.Equal("objective.jiangxia.final", director.CurrentObjective.PrimaryObjectiveKey);
            Assert.Equal(TerrainType.Hazard, simulation.Context.GetTerrainAt(new GridPosition(7, 2)));
            Assert.True(simulation.Context.GetCell(new GridPosition(7, 2)).IsBlocked);
            Assert.True(simulation.Context.GetCell(new GridPosition(10, 11)).IsBlocked);
            Assert.NotNull(simulation.Context.GetUnit("enemy-jiangxia-ferry-captain"));
        }

        [Fact]
        public void Baishui_BridgeCutMutatesLowerCrossingAndSpawnsPursuers()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.BaishuiScenarioId);
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);
            DefeatUnit(simulation.Context, "enemy-baishui-bridge-captain");
            DefeatUnit(simulation.Context, "enemy-baishui-bow-chief");

            ScenarioEvaluationResult result = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);
            ScenarioEvaluationResult secondResult = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.True(result.BattlefieldChanged);
            Assert.False(secondResult.BattlefieldChanged);
            Assert.True(director.HasFlag(BattleScenarioCatalog.BaishuiBridgeCutFlag));
            Assert.Equal("objective.baishui.final", director.CurrentObjective.PrimaryObjectiveKey);
            Assert.True(simulation.Context.GetCell(new GridPosition(5, 6)).IsBlocked);
            Assert.Equal(TerrainType.Hazard, simulation.Context.GetTerrainAt(new GridPosition(5, 6)));
            Assert.NotNull(simulation.Context.GetUnit("enemy-baishui-commandant"));
            Assert.NotNull(simulation.Context.GetUnit("enemy-baishui-rider-a"));
        }

        [Fact]
        public void Jiameng_BossPhaseStartsOnlyAfterGateLineBreaks()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateJiamengPass();
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);
            DefeatUnit(simulation.Context, "enemy-jiameng-gatewarden");
            director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.False(director.HasFlag(BattleScenarioCatalog.JiamengBossArrivedFlag));
            Assert.Null(simulation.Context.GetUnit("enemy-jiameng-commandant"));

            DefeatUnit(simulation.Context, "enemy-jiameng-bow-captain");
            director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.True(director.HasFlag(BattleScenarioCatalog.JiamengBossArrivedFlag));
            Assert.NotNull(simulation.Context.GetUnit("enemy-jiameng-commandant"));
            Assert.Equal("objective.jiameng.final", director.CurrentObjective.PrimaryObjectiveKey);
        }

        [Fact]
        public void Mianzhu_BreachOpensGateAndIgnitesCenterLane()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.MianzhuScenarioId);
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);
            DefeatUnit(simulation.Context, "enemy-mianzhu-gate-captain");
            DefeatUnit(simulation.Context, "enemy-mianzhu-oil-bow-chief");

            ScenarioEvaluationResult result = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);
            ScenarioEvaluationResult secondResult = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.True(result.BattlefieldChanged);
            Assert.False(secondResult.BattlefieldChanged);
            Assert.True(director.HasFlag(BattleScenarioCatalog.MianzhuBreachFlag));
            Assert.Equal("objective.mianzhu.final", director.CurrentObjective.PrimaryObjectiveKey);
            Assert.False(simulation.Context.GetCell(new GridPosition(6, 5)).IsBlocked);
            Assert.Equal(TerrainType.Plain, simulation.Context.GetTerrainAt(new GridPosition(6, 5)));
            Assert.Equal(TerrainType.Hazard, simulation.Context.GetTerrainAt(new GridPosition(6, 6)));
            Assert.NotNull(simulation.Context.GetUnit("enemy-mianzhu-commandant"));
        }

        [Fact]
        public void Luocheng_BreachOpensGateAndIgnitesInnerStreet()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateLuochengSiege();
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);
            DefeatUnit(simulation.Context, "enemy-luocheng-gate-captain");
            DefeatUnit(simulation.Context, "enemy-luocheng-wall-bow");

            ScenarioEvaluationResult result = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);
            ScenarioEvaluationResult secondResult = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.True(result.BattlefieldChanged);
            Assert.False(secondResult.BattlefieldChanged);
            Assert.True(director.HasFlag(BattleScenarioCatalog.LuochengGateBreachedFlag));
            Assert.Equal("objective.luocheng.final", director.CurrentObjective.PrimaryObjectiveKey);
            Assert.False(simulation.Context.GetCell(new GridPosition(8, 8)).IsBlocked);
            Assert.Equal(TerrainType.Plain, simulation.Context.GetTerrainAt(new GridPosition(8, 8)));
            Assert.Equal(TerrainType.Hazard, simulation.Context.GetTerrainAt(new GridPosition(8, 10)));
            Assert.NotNull(simulation.Context.GetUnit("enemy-luocheng-commandant"));
        }

        [Fact]
        public void Yangping_RockslideSealsCenterAndOpensRightFlank()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateYangpingPass();
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);
            DefeatUnit(simulation.Context, "enemy-yangping-left-sentry");
            DefeatUnit(simulation.Context, "enemy-yangping-right-sentry");

            ScenarioEvaluationResult result = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);
            ScenarioEvaluationResult secondResult = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.True(result.BattlefieldChanged);
            Assert.False(secondResult.BattlefieldChanged);
            Assert.True(director.HasFlag(BattleScenarioCatalog.YangpingRockslideFlag));
            Assert.Equal("objective.yangping.final", director.CurrentObjective.PrimaryObjectiveKey);
            Assert.True(simulation.Context.GetCell(new GridPosition(5, 5)).IsBlocked);
            Assert.False(simulation.Context.GetCell(new GridPosition(9, 8)).IsBlocked);
            Assert.Equal(TerrainType.Fort, simulation.Context.GetTerrainAt(new GridPosition(9, 8)));
            Assert.NotNull(simulation.Context.GetUnit("enemy-yangping-commandant"));
        }

        [Fact]
        public void Tiandang_SecuringBothBeaconsPreventsAlarmAndSpawnsBoss()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.TiandangScenarioId);
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);
            DefeatUnit(simulation.Context, "enemy-tiandang-left-warden");
            DefeatUnit(simulation.Context, "enemy-tiandang-right-warden");

            ScenarioEvaluationResult result = director.Evaluate(ScenarioCheckpoint.ActionResolved, simulation.Context);

            Assert.False(result.BattlefieldChanged);
            Assert.True(director.HasFlag(BattleScenarioCatalog.TiandangSignalsSecuredFlag));
            Assert.False(director.HasFlag(BattleScenarioCatalog.TiandangAlarmFlag));
            Assert.Equal("objective.tiandang.final", director.CurrentObjective.PrimaryObjectiveKey);
            Assert.NotNull(simulation.Context.GetUnit("enemy-tiandang-commandant"));
            Assert.Null(simulation.Context.GetUnit("enemy-tiandang-flank-rider-a"));
        }

        [Fact]
        public void Tiandang_AlarmOpensFlankAndSpawnsReinforcements()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.TiandangScenarioId);
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);

            while (simulation.Context.RoundNumber < 4)
            {
                simulation.EndCurrentTurn();
                director.Evaluate(ScenarioCheckpoint.EnemyTurnStart, simulation.Context);
                simulation.EndCurrentTurn();
            }

            simulation.EndCurrentTurn();
            ScenarioEvaluationResult result = director.Evaluate(ScenarioCheckpoint.EnemyTurnStart, simulation.Context);

            Assert.True(result.BattlefieldChanged);
            Assert.True(director.HasFlag(BattleScenarioCatalog.TiandangAlarmFlag));
            Assert.False(director.HasFlag(BattleScenarioCatalog.TiandangSignalsSecuredFlag));
            Assert.Equal("objective.tiandang.final", director.CurrentObjective.PrimaryObjectiveKey);
            Assert.False(simulation.Context.GetCell(new GridPosition(9, 7)).IsBlocked);
            Assert.False(simulation.Context.GetCell(new GridPosition(9, 8)).IsBlocked);
            Assert.NotNull(simulation.Context.GetUnit("enemy-tiandang-commandant"));
            Assert.NotNull(simulation.Context.GetUnit("enemy-tiandang-flank-rider-a"));
        }

        [Fact]
        public void Hanshui_CounterattackStartsOnRoundFourWhenLiuBeiLives()
        {
            BattleScenarioData scenario = BattleScenarioCatalog.CreateHanshui();
            BattleSimulation simulation = new BattleSimulation(scenario.Stage);
            ScenarioDirector director = new ScenarioDirector(scenario);

            director.Evaluate(ScenarioCheckpoint.BattleStart, simulation.Context);

            while (simulation.Context.RoundNumber < 4)
            {
                simulation.EndCurrentTurn();
                simulation.EndCurrentTurn();
            }

            director.Evaluate(ScenarioCheckpoint.PlayerTurnStart, simulation.Context);

            Assert.True(director.HasFlag(BattleScenarioCatalog.HanshuiCounterattackFlag));
            Assert.NotNull(simulation.Context.GetUnit("enemy-hanshui-commander"));
            Assert.Equal("objective.hanshui.final", director.CurrentObjective.PrimaryObjectiveKey);
        }

        [Fact]
        public void ScenarioCatalog_UsesExpectedAdaptiveBoardSizes()
        {
            Assert.Equal((16, 14), (BattleScenarioCatalog.CreateGuangzong().Stage.Width, BattleScenarioCatalog.CreateGuangzong().Stage.Height));
            Assert.Equal((12, 10), (BattleScenarioCatalog.CreateBowangpo().Stage.Width, BattleScenarioCatalog.CreateBowangpo().Stage.Height));
            Assert.Equal((12, 10), (BattleScenarioCatalog.CreateChangbanRearguard().Stage.Width, BattleScenarioCatalog.CreateChangbanRearguard().Stage.Height));
            Assert.Equal((18, 14), (BattleScenarioCatalog.CreateJiangxiaFerry().Stage.Width, BattleScenarioCatalog.CreateJiangxiaFerry().Stage.Height));
            Assert.Equal((10, 14), (BattleScenarioCatalog.CreateJiamengPass().Stage.Width, BattleScenarioCatalog.CreateJiamengPass().Stage.Height));
            Assert.Equal((12, 10), (BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.BaishuiScenarioId).Stage.Width, BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.BaishuiScenarioId).Stage.Height));
            Assert.Equal((14, 10), (BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.MianzhuScenarioId).Stage.Width, BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.MianzhuScenarioId).Stage.Height));
            Assert.Equal((18, 16), (BattleScenarioCatalog.CreateLuochengSiege().Stage.Width, BattleScenarioCatalog.CreateLuochengSiege().Stage.Height));
            Assert.Equal((12, 12), (BattleScenarioCatalog.CreateYangpingPass().Stage.Width, BattleScenarioCatalog.CreateYangpingPass().Stage.Height));
            Assert.Equal((12, 12), (BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.TiandangScenarioId).Stage.Width, BattleScenarioCatalog.CreateScenario(BattleScenarioCatalog.TiandangScenarioId).Stage.Height));
            Assert.Equal((14, 10), (BattleScenarioCatalog.CreateHanshui().Stage.Width, BattleScenarioCatalog.CreateHanshui().Stage.Height));
            Assert.Equal((12, 12), (BattleScenarioCatalog.CreateDingjunMountain().Stage.Width, BattleScenarioCatalog.CreateDingjunMountain().Stage.Height));
        }

        [Fact]
        public void RandomStage_SameSeed_ProducesSameLayout()
        {
            IReadOnlyList<UnitDefinitionData> units = CreateRandomStageUnits();

            StageDefinitionData first = RandomStageGenerator.Create("Random", "stage.random", 10, 10, units, 12345);
            StageDefinitionData second = RandomStageGenerator.Create("Random", "stage.random", 10, 10, units, 12345);

            Assert.True(first.IsRandomMap);
            Assert.Equal(12345, first.MapSeed);
            Assert.Equal(first.BlockedCells, second.BlockedCells);
            Assert.Equal(
                first.UnitSpawns.Select(spawn => spawn.StartPosition).ToList(),
                second.UnitSpawns.Select(spawn => spawn.StartPosition).ToList());
        }

        [Fact]
        public void RandomStage_GeneratedMap_KeepsFrontlinesReachable()
        {
            IReadOnlyList<UnitDefinitionData> units = CreateRandomStageUnits();
            StageDefinitionData stage = RandomStageGenerator.Create("Random", "stage.random", 10, 10, units, 67890);

            HashSet<GridPosition> blocked = stage.BlockedCells.ToHashSet();
            Assert.DoesNotContain(stage.UnitSpawns, spawn => blocked.Contains(spawn.StartPosition));

            List<GridPosition> players = stage.UnitSpawns
                .Where(spawn => spawn.Definition.Faction == UnitFaction.Player)
                .Select(spawn => spawn.StartPosition)
                .ToList();
            List<GridPosition> enemies = stage.UnitSpawns
                .Where(spawn => spawn.Definition.Faction == UnitFaction.Enemy)
                .Select(spawn => spawn.StartPosition)
                .ToList();

            int shortestPath = int.MaxValue;
            foreach (GridPosition player in players)
            {
                foreach (GridPosition enemy in enemies)
                {
                    int pathLength = FindPathDistance(stage, player, enemy);
                    Assert.True(pathLength > 0);
                    shortestPath = System.Math.Min(shortestPath, pathLength);
                }
            }

            Assert.True(shortestPath >= 4);
        }

        [Fact]
        public void ActiveSkill_FireStratagem_HitsAdjacentEnemies_AndHighlightsArea()
        {
            UnitDefinitionData strategist = CreateDefinition(
                "player-zhuge",
                "Zhuge Liang",
                UnitFaction.Player,
                UnitRole.Commander,
                PassiveSkillType.CommandAura,
                ActiveSkillType.FireStratagem,
                attack: 10,
                maxMana: 24,
                progressionResolved: true);
            UnitDefinitionData primary = CreateDefinition("enemy-primary", "Primary", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24, defense: 2, progressionResolved: true);
            UnitDefinitionData adjacent = CreateDefinition("enemy-adjacent", "Adjacent", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24, defense: 2, progressionResolved: true);

            StageDefinitionData stage = new StageDefinitionData(
                "Fire Stratagem",
                "stage.fire_stratagem",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(strategist, new GridPosition(1, 1)),
                    new UnitSpawnData(primary, new GridPosition(4, 1)),
                    new UnitSpawnData(adjacent, new GridPosition(4, 2)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            IReadOnlyList<GridPosition> affectedPositions = simulation.GetSkillAffectedPositions("player-zhuge", "enemy-primary");
            SkillResult result = simulation.TryUseSkill("player-zhuge", "enemy-primary");

            Assert.Contains(new GridPosition(4, 1), affectedPositions);
            Assert.Contains(new GridPosition(4, 2), affectedPositions);
            Assert.Contains(new GridPosition(5, 1), affectedPositions);
            Assert.NotNull(result);
            Assert.Equal(2, result.Effects.Count);

            SkillEffectResult primaryEffect = result.Effects.Single(effect => effect.UnitId == "enemy-primary");
            SkillEffectResult adjacentEffect = result.Effects.Single(effect => effect.UnitId == "enemy-adjacent");

            Assert.Equal(StatusEffectType.Intimidated, primaryEffect.AppliedStatus);
            Assert.Empty(adjacentEffect.AppliedStatuses);
            Assert.True(simulation.Context.GetUnit("enemy-primary").HasStatus(StatusEffectType.Intimidated));
            Assert.False(simulation.Context.GetUnit("enemy-adjacent").HasStatus(StatusEffectType.Intimidated));
        }

        [Fact]
        public void SkillMastery_FireStratagem_ImprovesDamageAndSpreadsIntimidated()
        {
            BattleSimulation veteranSimulation = new BattleSimulation(
                new StageDefinitionData(
                    "Fire Stratagem Veteran",
                    "stage.fire_stratagem_veteran",
                    8,
                    8,
                    new List<UnitSpawnData>
                    {
                        new UnitSpawnData(CreateDefinition("player-zhuge", "Zhuge Liang", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.FireStratagem, attack: 10, startingLevel: 10, maxMana: 24, progressionResolved: true), new GridPosition(1, 1)),
                        new UnitSpawnData(CreateDefinition("enemy-primary", "Primary", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24, defense: 2, progressionResolved: true), new GridPosition(4, 1)),
                        new UnitSpawnData(CreateDefinition("enemy-adjacent", "Adjacent", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24, defense: 2, progressionResolved: true), new GridPosition(4, 2)),
                    },
                    new List<GridPosition>()));
            BattleSimulation apprenticeSimulation = new BattleSimulation(
                new StageDefinitionData(
                    "Fire Stratagem Apprentice",
                    "stage.fire_stratagem_apprentice",
                    8,
                    8,
                    new List<UnitSpawnData>
                    {
                        new UnitSpawnData(CreateDefinition("player-zhuge", "Zhuge Liang", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.FireStratagem, attack: 10, startingLevel: 9, maxMana: 24, progressionResolved: true), new GridPosition(1, 1)),
                        new UnitSpawnData(CreateDefinition("enemy-primary", "Primary", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24, defense: 2, progressionResolved: true), new GridPosition(4, 1)),
                        new UnitSpawnData(CreateDefinition("enemy-adjacent", "Adjacent", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24, defense: 2, progressionResolved: true), new GridPosition(4, 2)),
                    },
                    new List<GridPosition>()));

            SkillResult veteranResult = veteranSimulation.TryUseSkill("player-zhuge", "enemy-primary");
            SkillResult apprenticeResult = apprenticeSimulation.TryUseSkill("player-zhuge", "enemy-primary");

            Assert.NotNull(veteranResult);
            Assert.NotNull(apprenticeResult);

            SkillEffectResult veteranPrimary = veteranResult.Effects.Single(effect => effect.UnitId == "enemy-primary");
            SkillEffectResult apprenticePrimary = apprenticeResult.Effects.Single(effect => effect.UnitId == "enemy-primary");
            SkillEffectResult veteranAdjacent = veteranResult.Effects.Single(effect => effect.UnitId == "enemy-adjacent");
            SkillEffectResult apprenticeAdjacent = apprenticeResult.Effects.Single(effect => effect.UnitId == "enemy-adjacent");

            Assert.Equal(apprenticePrimary.Amount + 2, veteranPrimary.Amount);
            Assert.Empty(apprenticeAdjacent.AppliedStatuses);
            Assert.Contains(veteranAdjacent.AppliedStatuses, status => status.Type == StatusEffectType.Intimidated && status.WasApplied);
        }

        [Fact]
        public void ActiveSkill_EightTrigramInferno_AppliesDualStatuses_WithMasteryDuration()
        {
            UnitDefinitionData strategist = CreateDefinition(
                "player-zhuge",
                "Zhuge Liang",
                UnitFaction.Player,
                UnitRole.Commander,
                PassiveSkillType.BenevolentCommand,
                ActiveSkillType.EightTrigramInferno,
                attack: 11,
                maxMana: 24,
                startingLevel: 10,
                classId: "sleeping_dragon",
                growthProfileId: "sleeping_dragon",
                progressionResolved: true);
            UnitDefinitionData primary = CreateDefinition("enemy-primary", "Primary", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 28, defense: 2, progressionResolved: true);
            UnitDefinitionData adjacent = CreateDefinition("enemy-adjacent", "Adjacent", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 28, defense: 2, progressionResolved: true);

            StageDefinitionData stage = new StageDefinitionData(
                "Eight Trigram Inferno",
                "stage.eight_trigram_inferno",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(strategist, new GridPosition(1, 1)),
                    new UnitSpawnData(primary, new GridPosition(4, 1)),
                    new UnitSpawnData(adjacent, new GridPosition(4, 2)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            SkillResult result = simulation.TryUseSkill("player-zhuge", "enemy-primary");

            Assert.NotNull(result);
            Assert.Equal(2, result.Effects.Count);
            Assert.All(result.Effects, effect =>
            {
                Assert.Contains(effect.AppliedStatuses, status => status.Type == StatusEffectType.Intimidated && status.WasApplied);
                Assert.Contains(effect.AppliedStatuses, status => status.Type == StatusEffectType.ShatteredArmor && status.WasApplied);
            });
            Assert.Equal(2, simulation.Context.GetUnit("enemy-primary").StatusEffects.Single(status => status.Type == StatusEffectType.ShatteredArmor).RemainingOwnTurnEnds);
            Assert.Equal(2, simulation.Context.GetUnit("enemy-adjacent").StatusEffects.Single(status => status.Type == StatusEffectType.ShatteredArmor).RemainingOwnTurnEnds);
        }

        [Fact]
        public void SkillMastery_RoyalAid_HealsMore_AndReportsBothSupportStatuses()
        {
            BattleSimulation veteranSimulation = new BattleSimulation(
                new StageDefinitionData(
                    "Royal Aid Veteran",
                    "stage.royal_aid_veteran",
                    8,
                    8,
                    new List<UnitSpawnData>
                    {
                        new UnitSpawnData(CreateDefinition("player-healer", "Healer", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.RoyalAid, startingLevel: 10, progressionResolved: true), new GridPosition(1, 1)),
                        new UnitSpawnData(CreateDefinition("player-ally", "Ally", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30, progressionResolved: true), new GridPosition(2, 1)),
                    },
                    new List<GridPosition>()));
            BattleSimulation apprenticeSimulation = new BattleSimulation(
                new StageDefinitionData(
                    "Royal Aid Apprentice",
                    "stage.royal_aid_apprentice",
                    8,
                    8,
                    new List<UnitSpawnData>
                    {
                        new UnitSpawnData(CreateDefinition("player-healer", "Healer", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.RoyalAid, startingLevel: 9, progressionResolved: true), new GridPosition(1, 1)),
                        new UnitSpawnData(CreateDefinition("player-ally", "Ally", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30, progressionResolved: true), new GridPosition(2, 1)),
                    },
                    new List<GridPosition>()));

            veteranSimulation.Context.GetUnit("player-ally").ApplyDamage(15);
            apprenticeSimulation.Context.GetUnit("player-ally").ApplyDamage(15);

            SkillResult veteranResult = veteranSimulation.TryUseSkill("player-healer", "player-ally");
            SkillResult apprenticeResult = apprenticeSimulation.TryUseSkill("player-healer", "player-ally");

            Assert.NotNull(veteranResult);
            Assert.NotNull(apprenticeResult);
            Assert.Equal(apprenticeResult.Effects[0].Amount + 2, veteranResult.Effects[0].Amount);
            Assert.Contains(veteranResult.Effects[0].AppliedStatuses, status => status.Type == StatusEffectType.Inspired && status.WasApplied);
            Assert.Contains(veteranResult.Effects[0].AppliedStatuses, status => status.Type == StatusEffectType.Guarded && status.WasApplied);
        }

        [Fact]
        public void SkillMastery_GuardOrder_PrimaryGetsInspired_WhileAdjacentKeepsGuarded()
        {
            UnitDefinitionData commander = CreateDefinition("player-commander", "Commander", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.GuardOrder, startingLevel: 10, progressionResolved: true);
            UnitDefinitionData primary = CreateDefinition("player-primary", "Primary", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30, progressionResolved: true);
            UnitDefinitionData adjacent = CreateDefinition("player-adjacent", "Adjacent", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30, progressionResolved: true);

            StageDefinitionData stage = new StageDefinitionData(
                "Guard Order Mastery",
                "stage.guard_order_mastery",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(commander, new GridPosition(1, 1)),
                    new UnitSpawnData(primary, new GridPosition(3, 1)),
                    new UnitSpawnData(adjacent, new GridPosition(3, 2)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.Context.GetUnit("player-primary").ApplyDamage(10);

            SkillResult result = simulation.TryUseSkill("player-commander", "player-primary");

            Assert.NotNull(result);
            SkillEffectResult primaryEffect = result.Effects.Single(effect => effect.UnitId == "player-primary");
            SkillEffectResult adjacentEffect = result.Effects.Single(effect => effect.UnitId == "player-adjacent");

            Assert.Equal(6, primaryEffect.Amount);
            Assert.Contains(primaryEffect.AppliedStatuses, status => status.Type == StatusEffectType.Guarded && status.WasApplied);
            Assert.Contains(primaryEffect.AppliedStatuses, status => status.Type == StatusEffectType.Inspired && status.WasApplied);
            Assert.Equal(0, adjacentEffect.Amount);
            Assert.Contains(adjacentEffect.AppliedStatuses, status => status.Type == StatusEffectType.Guarded && status.WasApplied);
            Assert.DoesNotContain(adjacentEffect.AppliedStatuses, status => status.Type == StatusEffectType.Inspired);
        }

        [Fact]
        public void SkillMastery_PowerStrike_UpgradesDamage_AndAppliesMultipleStatuses()
        {
            BattleSimulation veteranSimulation = new BattleSimulation(
                new StageDefinitionData(
                    "Power Strike Veteran",
                    "stage.power_strike_veteran",
                    8,
                    8,
                    new List<UnitSpawnData>
                    {
                        new UnitSpawnData(CreateDefinition("player-striker", "Striker", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.PowerStrike, attack: 10, startingLevel: 10, progressionResolved: true), new GridPosition(1, 1)),
                        new UnitSpawnData(CreateDefinition("enemy-target", "Target", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30, defense: 3, progressionResolved: true), new GridPosition(2, 1)),
                    },
                    new List<GridPosition>()));
            BattleSimulation apprenticeSimulation = new BattleSimulation(
                new StageDefinitionData(
                    "Power Strike Apprentice",
                    "stage.power_strike_apprentice",
                    8,
                    8,
                    new List<UnitSpawnData>
                    {
                        new UnitSpawnData(CreateDefinition("player-striker", "Striker", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.PowerStrike, attack: 10, startingLevel: 9, progressionResolved: true), new GridPosition(1, 1)),
                        new UnitSpawnData(CreateDefinition("enemy-target", "Target", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 30, defense: 3, progressionResolved: true), new GridPosition(2, 1)),
                    },
                    new List<GridPosition>()));

            SkillResult veteranResult = veteranSimulation.TryUseSkill("player-striker", "enemy-target");
            SkillResult apprenticeResult = apprenticeSimulation.TryUseSkill("player-striker", "enemy-target");

            Assert.NotNull(veteranResult);
            Assert.NotNull(apprenticeResult);
            Assert.Equal(apprenticeResult.Effects[0].Amount + 2, veteranResult.Effects[0].Amount);
            Assert.Contains(veteranResult.Effects[0].AppliedStatuses, status => status.Type == StatusEffectType.ShatteredArmor && status.WasApplied);
            Assert.Contains(veteranResult.Effects[0].AppliedStatuses, status => status.Type == StatusEffectType.Bleeding && status.WasApplied);
            Assert.Equal(2, veteranSimulation.Context.GetUnit("enemy-target").StatusEffects.Single(status => status.Type == StatusEffectType.ShatteredArmor).RemainingOwnTurnEnds);
        }

        [Fact]
        public void SkillMastery_GreenDragonSlash_AppliesShatteredArmorToAllSurvivors()
        {
            UnitDefinitionData striker = CreateDefinition(
                "player-striker",
                "Striker",
                UnitFaction.Player,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.GreenDragonSlash,
                attack: 10,
                startingLevel: 10,
                progressionResolved: true);
            UnitDefinitionData enemyOne = CreateDefinition("enemy-1", "Bandit A", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24, defense: 1, progressionResolved: true);
            UnitDefinitionData enemyTwo = CreateDefinition("enemy-2", "Bandit B", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24, defense: 1, progressionResolved: true);

            StageDefinitionData stage = new StageDefinitionData(
                "Green Dragon Mastery",
                "stage.green_dragon_mastery",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(striker, new GridPosition(1, 1)),
                    new UnitSpawnData(enemyOne, new GridPosition(2, 1)),
                    new UnitSpawnData(enemyTwo, new GridPosition(3, 1)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);

            SkillResult result = simulation.TryUseSkill("player-striker", "enemy-1");

            Assert.NotNull(result);
            Assert.All(result.Effects, effect => Assert.Contains(effect.AppliedStatuses, status => status.Type == StatusEffectType.ShatteredArmor && status.WasApplied));
            Assert.True(simulation.Context.GetUnit("enemy-1").HasStatus(StatusEffectType.ShatteredArmor));
            Assert.True(simulation.Context.GetUnit("enemy-2").HasStatus(StatusEffectType.ShatteredArmor));
        }

        [Fact]
        public void SkillMastery_PinningShot_AndWarCry_UpgradeControlDuration()
        {
            BattleSimulation pinningSimulation = new BattleSimulation(
                new StageDefinitionData(
                    "Pinning Mastery",
                    "stage.pinning_mastery",
                    8,
                    8,
                    new List<UnitSpawnData>
                    {
                        new UnitSpawnData(CreateDefinition("player-archer", "Archer", UnitFaction.Player, UnitRole.Ranger, PassiveSkillType.None, ActiveSkillType.PinningShot, attack: 10, attackRange: 2, startingLevel: 10, progressionResolved: true), new GridPosition(1, 1)),
                        new UnitSpawnData(CreateDefinition("enemy-target", "Target", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 24, defense: 3, progressionResolved: true), new GridPosition(4, 1)),
                    },
                    new List<GridPosition>()));
            BattleSimulation warCrySimulation = new BattleSimulation(
                new StageDefinitionData(
                    "War Cry Mastery",
                    "stage.war_cry_mastery",
                    8,
                    8,
                    new List<UnitSpawnData>
                    {
                        new UnitSpawnData(CreateDefinition("player-intimidator", "Intimidator", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.WarCry, startingLevel: 10, progressionResolved: true), new GridPosition(2, 2)),
                        new UnitSpawnData(CreateDefinition("enemy-target", "Target", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, progressionResolved: true), new GridPosition(3, 2)),
                    },
                    new List<GridPosition>()));

            SkillResult pinningResult = pinningSimulation.TryUseSkill("player-archer", "enemy-target");
            SkillResult warCryResult = warCrySimulation.TryUseSkill("player-intimidator", "enemy-target");

            Assert.NotNull(pinningResult);
            Assert.NotNull(warCryResult);
            Assert.Equal(2, pinningSimulation.Context.GetUnit("enemy-target").StatusEffects.Single(status => status.Type == StatusEffectType.Rooted).RemainingOwnTurnEnds);
            Assert.Equal(2, warCrySimulation.Context.GetUnit("enemy-target").StatusEffects.Single(status => status.Type == StatusEffectType.Intimidated).RemainingOwnTurnEnds);
        }

        [Fact]
        public void EnemyAi_UsesFireStratagemTargetThatHitsMoreUnits()
        {
            UnitDefinitionData isolatedTarget = CreateDefinition(
                "player-isolated",
                "Isolated",
                UnitFaction.Player,
                UnitRole.Ranger,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 12,
                defense: 0,
                progressionResolved: true);
            UnitDefinitionData clusteredTarget = CreateDefinition(
                "player-clustered-a",
                "Cluster A",
                UnitFaction.Player,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 12,
                defense: 0,
                progressionResolved: true);
            UnitDefinitionData clusteredNeighbor = CreateDefinition(
                "player-clustered-b",
                "Cluster B",
                UnitFaction.Player,
                UnitRole.Guardian,
                PassiveSkillType.None,
                ActiveSkillType.None,
                maxHp: 12,
                defense: 0,
                progressionResolved: true);
            UnitDefinitionData enemy = CreateDefinition(
                "enemy-strategist",
                "Enemy Strategist",
                UnitFaction.Enemy,
                UnitRole.Commander,
                PassiveSkillType.CommandAura,
                ActiveSkillType.FireStratagem,
                attack: 10,
                attackRange: 1,
                progressionResolved: true);

            StageDefinitionData stage = new StageDefinitionData(
                "Fire Stratagem Value",
                "stage.fire_stratagem_value",
                8,
                8,
                new List<UnitSpawnData>
                {
                    new UnitSpawnData(isolatedTarget, new GridPosition(2, 1)),
                    new UnitSpawnData(clusteredTarget, new GridPosition(2, 3)),
                    new UnitSpawnData(clusteredNeighbor, new GridPosition(2, 4)),
                    new UnitSpawnData(enemy, new GridPosition(0, 2)),
                },
                new List<GridPosition>());

            BattleSimulation simulation = new BattleSimulation(stage);
            simulation.EndCurrentTurn();

            AiDecision decision = simulation.BuildEnemyDecision("enemy-strategist");

            Assert.Equal(AiActionType.Skill, decision.ActionType);
            Assert.Contains(decision.TargetUnitId, new[] { "player-clustered-a", "player-clustered-b" });
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

        private static IReadOnlyList<UnitDefinitionData> CreateRandomStageUnits()
        {
            return new List<UnitDefinitionData>
            {
                CreateDefinition("player-1", "Hero", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.RoyalAid),
                CreateDefinition("player-2", "Knight", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.PowerStrike),
                CreateDefinition("player-3", "Archer", UnitFaction.Player, UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, attackRange: 2),
                CreateDefinition("enemy-1", "Bandit A", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.PowerStrike),
                CreateDefinition("enemy-2", "Bandit B", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None),
                CreateDefinition("enemy-3", "Bandit C", UnitFaction.Enemy, UnitRole.Scout, PassiveSkillType.ShieldWall, ActiveSkillType.Volley),
            };
        }

        private static int FindPathDistance(StageDefinitionData stage, GridPosition start, GridPosition target)
        {
            HashSet<GridPosition> blocked = stage.BlockedCells.ToHashSet();
            Queue<GridPosition> frontier = new Queue<GridPosition>();
            Dictionary<GridPosition, int> distances = new Dictionary<GridPosition, int>
            {
                [start] = 0,
            };
            frontier.Enqueue(start);

            while (frontier.Count > 0)
            {
                GridPosition current = frontier.Dequeue();
                if (current == target)
                {
                    return distances[current];
                }

                foreach (GridPosition neighbor in current.GetOrthogonalNeighbors())
                {
                    if (neighbor.X < 0 || neighbor.X >= stage.Width || neighbor.Y < 0 || neighbor.Y >= stage.Height)
                    {
                        continue;
                    }

                    if (blocked.Contains(neighbor) || distances.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    distances[neighbor] = distances[current] + 1;
                    frontier.Enqueue(neighbor);
                }
            }

            return -1;
        }

        private static void DefeatUnit(BattleContext context, string unitId)
        {
            UnitRuntimeState unit = context.GetUnit(unitId);
            unit.ApplyDamage(unit.CurrentHp);
            context.RemoveUnit(unitId);
        }
    }
}
