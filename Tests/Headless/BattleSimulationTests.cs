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
            Assert.Equal("player-clustered-a", decision.TargetUnitId);
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
            Assert.Equal(7, attackResult.Damage);
        }

        [Fact]
        public void ActiveSkill_Cooldown_BlocksReuseUntilLaterTurn()
        {
            UnitDefinitionData striker = CreateDefinition("player-striker", "Striker", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.None, ActiveSkillType.PowerStrike, attack: 10);
            UnitDefinitionData ally = CreateDefinition("player-ally", "Ally", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.None, ActiveSkillType.None);
            UnitDefinitionData enemy = CreateDefinition("enemy-1", "Bandit", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.None, ActiveSkillType.None, maxHp: 20, defense: 3);

            StageDefinitionData stage = new StageDefinitionData(
                "Cooldown Stage",
                "stage.cooldown_stage",
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
            Assert.Equal(2, simulation.Context.GetUnit("player-striker").CurrentSkillCooldown);

            simulation.Wait("player-ally");
            simulation.EndCurrentTurn();
            simulation.EndCurrentTurn();

            SkillResult blockedCast = simulation.TryUseSkill("player-striker", "enemy-1");
            Assert.Null(blockedCast);
            Assert.Equal(1, simulation.Context.GetUnit("player-striker").CurrentSkillCooldown);

            simulation.Wait("player-striker");
            simulation.Wait("player-ally");
            simulation.EndCurrentTurn();
            simulation.EndCurrentTurn();

            SkillResult secondCast = simulation.TryUseSkill("player-striker", "enemy-1");
            Assert.NotNull(secondCast);
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
            int attackRange = 1)
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
                attackRange);
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
    }
}
