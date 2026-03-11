using System;
using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public static class RandomStageGenerator
    {
        public static StageDefinitionData Create(
            string stageName,
            string stageNameKey,
            int width,
            int height,
            IReadOnlyList<UnitDefinitionData> units,
            int seed)
        {
            int actualSeed = seed == 0 ? Environment.TickCount : seed;
            Random random = new Random(actualSeed);

            for (int attempt = 0; attempt < 64; attempt++)
            {
                List<GridPosition> blockedCells = BuildBlockedCells(width, height, random);
                List<UnitSpawnData> spawns = BuildSpawns(width, height, units, random, blockedCells);
                List<TerrainTileData> terrainTiles = BuildTerrainTiles(width, height, random, blockedCells, spawns);
                if (IsPlayableLayout(width, height, blockedCells, spawns))
                {
                    return new StageDefinitionData(stageName, stageNameKey, width, height, spawns, blockedCells, terrainTiles, true, actualSeed);
                }
            }

            List<GridPosition> fallbackBlockedCells = new List<GridPosition>();
            List<UnitSpawnData> fallbackSpawns = BuildSpawns(width, height, units, random, fallbackBlockedCells);
            List<TerrainTileData> fallbackTerrainTiles = BuildTerrainTiles(width, height, random, fallbackBlockedCells, fallbackSpawns);
            return new StageDefinitionData(stageName, stageNameKey, width, height, fallbackSpawns, fallbackBlockedCells, fallbackTerrainTiles, true, actualSeed);
        }

        private static List<GridPosition> BuildBlockedCells(int width, int height, Random random)
        {
            HashSet<GridPosition> blocked = new HashSet<GridPosition>();
            int pairCount = Math.Max(2, height / 4) + random.Next(0, 2);
            int laneDepth = GetSpawnLaneDepth(width);
            int leftBound = Math.Max(laneDepth + 1, 2);
            int rightBound = Math.Max(leftBound + 1, width / 2);

            for (int index = 0; index < pairCount; index++)
            {
                int x = random.Next(leftBound, rightBound);
                int y = random.Next(1, height - 1);
                GridPosition left = new GridPosition(x, y);
                GridPosition right = new GridPosition(width - 1 - x, y);

                if (IsReservedLane(width, left) || IsReservedLane(width, right))
                {
                    continue;
                }

                blocked.Add(left);
                blocked.Add(right);
            }

            return blocked
                .OrderBy(position => position.Y)
                .ThenBy(position => position.X)
                .ToList();
        }

        private static List<UnitSpawnData> BuildSpawns(
            int width,
            int height,
            IReadOnlyList<UnitDefinitionData> units,
            Random random,
            IReadOnlyCollection<GridPosition> blockedCells)
        {
            List<UnitDefinitionData> players = units.Where(unit => unit.Faction == UnitFaction.Player).ToList();
            List<UnitDefinitionData> enemies = units.Where(unit => unit.Faction == UnitFaction.Enemy).ToList();

            List<GridPosition> playerPositions = BuildCandidatePositions(
                width,
                height,
                blockedCells,
                random,
                players.Count,
                leftSide: true);

            List<GridPosition> enemyPositions = BuildCandidatePositions(
                width,
                height,
                blockedCells,
                random,
                enemies.Count,
                leftSide: false);

            List<UnitSpawnData> result = new List<UnitSpawnData>();
            for (int index = 0; index < players.Count; index++)
            {
                result.Add(new UnitSpawnData(players[index], playerPositions[index]));
            }

            for (int index = 0; index < enemies.Count; index++)
            {
                result.Add(new UnitSpawnData(enemies[index], enemyPositions[index]));
            }

            return result;
        }

        private static List<GridPosition> BuildCandidatePositions(
            int width,
            int height,
            IReadOnlyCollection<GridPosition> blockedCells,
            Random random,
            int requiredCount,
            bool leftSide)
        {
            int laneDepth = GetSpawnLaneDepth(width);
            IEnumerable<GridPosition> candidates = Enumerable.Range(1, Math.Max(1, height - 2))
                .SelectMany(y => Enumerable.Range(1, laneDepth)
                    .Select(offset => new GridPosition(leftSide ? offset : width - 1 - offset, y)));

            List<GridPosition> available = candidates
                .Where(position => !blockedCells.Contains(position))
                .Distinct()
                .OrderBy(_ => random.Next())
                .ToList();

            if (requiredCount > 0 && available.Count < requiredCount)
            {
                throw new InvalidOperationException("Not enough candidate positions for generated stage.");
            }

            return requiredCount > 0 ? available.Take(requiredCount).ToList() : available;
        }

        private static List<TerrainTileData> BuildTerrainTiles(
            int width,
            int height,
            Random random,
            IReadOnlyCollection<GridPosition> blockedCells,
            IReadOnlyCollection<UnitSpawnData> spawns)
        {
            HashSet<GridPosition> reserved = new HashSet<GridPosition>(blockedCells);
            foreach (UnitSpawnData spawn in spawns)
            {
                reserved.Add(spawn.StartPosition);
            }

            List<TerrainTileData> tiles = new List<TerrainTileData>();
            TryAddTerrainPair(width, height / 2, TerrainType.Fort, reserved, tiles, 2);
            TryAddTerrainPair(width, 2, TerrainType.Forest, reserved, tiles, 3);
            TryAddTerrainPair(width, Math.Max(2, height - 3), TerrainType.Forest, reserved, tiles, 3);
            TryAddTerrainPair(width, Math.Max(3, height / 2 - 1), TerrainType.Hazard, reserved, tiles, Math.Max(3, width / 2 - 1));

            for (int index = 0; index < 2; index++)
            {
                int x = random.Next(GetSpawnLaneDepth(width) + 1, Math.Max(GetSpawnLaneDepth(width) + 2, width / 2));
                int y = random.Next(1, height - 1);
                TryAddTerrainPair(width, y, TerrainType.Forest, reserved, tiles, x);
            }

            return tiles
                .OrderBy(tile => tile.Position.Y)
                .ThenBy(tile => tile.Position.X)
                .ToList();
        }

        private static void TryAddTerrainPair(
            int width,
            int y,
            TerrainType terrainType,
            ISet<GridPosition> reserved,
            ICollection<TerrainTileData> tiles,
            int x)
        {
            GridPosition left = new GridPosition(x, y);
            GridPosition right = new GridPosition(width - 1 - x, y);
            if (reserved.Contains(left) || reserved.Contains(right))
            {
                return;
            }

            reserved.Add(left);
            reserved.Add(right);
            tiles.Add(new TerrainTileData(left, terrainType));
            tiles.Add(new TerrainTileData(right, terrainType));
        }

        private static bool IsReservedLane(int width, GridPosition position)
        {
            int laneDepth = GetSpawnLaneDepth(width);
            return position.X <= laneDepth || position.X >= width - 1 - laneDepth;
        }

        private static int GetSpawnLaneDepth(int width)
        {
            return Math.Max(2, Math.Min(3, width / 4));
        }

        private static bool IsPlayableLayout(
            int width,
            int height,
            IReadOnlyCollection<GridPosition> blockedCells,
            IReadOnlyList<UnitSpawnData> spawns)
        {
            HashSet<GridPosition> blockedLookup = new HashSet<GridPosition>(blockedCells);
            List<GridPosition> occupied = spawns.Select(spawn => spawn.StartPosition).ToList();
            if (occupied.Any(position => blockedLookup.Contains(position)))
            {
                return false;
            }

            if (occupied.Distinct().Count() != occupied.Count)
            {
                return false;
            }

            List<GridPosition> players = spawns
                .Where(spawn => spawn.Definition.Faction == UnitFaction.Player)
                .Select(spawn => spawn.StartPosition)
                .ToList();
            List<GridPosition> enemies = spawns
                .Where(spawn => spawn.Definition.Faction == UnitFaction.Enemy)
                .Select(spawn => spawn.StartPosition)
                .ToList();
            if (players.Count == 0 || enemies.Count == 0)
            {
                return false;
            }

            int minimumEngagementDistance = Math.Max(4, width / 2);
            int shortestFrontlinePath = int.MaxValue;
            foreach (GridPosition player in players)
            {
                foreach (GridPosition enemy in enemies)
                {
                    int distance = FindPathDistance(width, height, blockedLookup, player, enemy);
                    if (distance < 0)
                    {
                        return false;
                    }

                    shortestFrontlinePath = Math.Min(shortestFrontlinePath, distance);
                }
            }

            return shortestFrontlinePath >= minimumEngagementDistance;
        }

        private static int FindPathDistance(
            int width,
            int height,
            ISet<GridPosition> blockedCells,
            GridPosition start,
            GridPosition target)
        {
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
                    if (neighbor.X < 0 || neighbor.X >= width || neighbor.Y < 0 || neighbor.Y >= height)
                    {
                        continue;
                    }

                    if (blockedCells.Contains(neighbor) || distances.ContainsKey(neighbor))
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
