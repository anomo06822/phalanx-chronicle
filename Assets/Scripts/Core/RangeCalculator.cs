using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class RangeCalculator
    {
        public IReadOnlyList<GridPosition> GetMoveRange(BattleContext context, UnitRuntimeState unit)
        {
            if (StatusEffectRules.IsMovementBlocked(unit))
            {
                return new List<GridPosition> { unit.Position };
            }

            int moveRange = PassiveSkillRules.GetMoveRange(unit) + EquipmentEffectRules.GetMoveBonus(context, unit);
            Dictionary<GridPosition, int> distances = new Dictionary<GridPosition, int>();
            Queue<GridPosition> frontier = new Queue<GridPosition>();
            frontier.Enqueue(unit.Position);
            distances[unit.Position] = 0;

            while (frontier.Count > 0)
            {
                GridPosition current = frontier.Dequeue();
                int minimumNextDistance = distances[current] + 1;
                if (minimumNextDistance > moveRange)
                {
                    continue;
                }

                foreach (GridPosition neighbor in current.GetOrthogonalNeighbors())
                {
                    if (!context.IsInside(neighbor))
                    {
                        continue;
                    }

                    if (!context.IsWalkable(neighbor))
                    {
                        continue;
                    }

                    if (neighbor != unit.Position && context.IsOccupied(neighbor))
                    {
                        continue;
                    }

                    int nextDistance = distances[current] + TerrainRules.GetMoveCost(unit, context.GetTerrainAt(neighbor));
                    if (nextDistance > moveRange)
                    {
                        continue;
                    }

                    if (distances.TryGetValue(neighbor, out int knownDistance) && knownDistance <= nextDistance)
                    {
                        continue;
                    }

                    distances[neighbor] = nextDistance;
                    frontier.Enqueue(neighbor);
                }
            }

            return distances.Keys
                .OrderBy(position => position.Y)
                .ThenBy(position => position.X)
                .ToList();
        }

        public IReadOnlyList<GridPosition> GetMoveDestinations(BattleContext context, UnitRuntimeState unit)
        {
            return GetMoveRange(context, unit)
                .Where(position => position != unit.Position)
                .OrderBy(position => position.Y)
                .ThenBy(position => position.X)
                .ToList();
        }

        public IReadOnlyList<GridPosition> GetAttackRange(BattleContext context, GridPosition origin, int attackRange)
        {
            List<GridPosition> positions = new List<GridPosition>();
            for (int y = 0; y < context.Height; y++)
            {
                for (int x = 0; x < context.Width; x++)
                {
                    GridPosition candidate = new GridPosition(x, y);
                    int distance = origin.ManhattanDistance(candidate);
                    if (distance > 0 && distance <= attackRange)
                    {
                        positions.Add(candidate);
                    }
                }
            }

            return positions;
        }

        public IReadOnlyList<GridPosition> GetProjectedAttackRange(BattleContext context, UnitRuntimeState attacker)
        {
            int attackRange = PassiveSkillRules.GetAttackRange(attacker);
            HashSet<GridPosition> positions = new HashSet<GridPosition>();
            foreach (GridPosition movePosition in GetMoveRange(context, attacker))
            {
                foreach (GridPosition attackPosition in GetAttackRange(context, movePosition, attackRange))
                {
                    positions.Add(attackPosition);
                }
            }

            return positions
                .OrderBy(position => position.Y)
                .ThenBy(position => position.X)
                .ToList();
        }

        public IReadOnlyList<UnitRuntimeState> GetAttackableTargets(BattleContext context, UnitRuntimeState attacker)
        {
            return GetAttackableTargets(context, attacker, attacker.Position);
        }

        public IReadOnlyList<UnitRuntimeState> GetAttackableTargets(BattleContext context, UnitRuntimeState attacker, GridPosition origin)
        {
            UnitFaction targetFaction = attacker.Faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player;
            int attackRange = PassiveSkillRules.GetAttackRange(attacker);
            HashSet<GridPosition> attackCells = new HashSet<GridPosition>(GetAttackRange(context, origin, attackRange));

            return context.GetUnits(targetFaction)
                .Where(unit => attackCells.Contains(unit.Position))
                .OrderBy(unit => origin.ManhattanDistance(unit.Position))
                .ThenBy(unit => unit.Id)
                .ToList();
        }

        public bool TryBuildMovePath(
            BattleContext context,
            UnitRuntimeState unit,
            GridPosition destination,
            out IReadOnlyList<GridPosition> path,
            out int moveCost)
        {
            path = new List<GridPosition>();
            moveCost = 0;

            if (context == null || unit == null || !unit.IsAlive || unit.HasActed)
            {
                return false;
            }

            if (!context.IsInside(destination))
            {
                return false;
            }

            if (StatusEffectRules.IsMovementBlocked(unit))
            {
                if (destination != unit.Position)
                {
                    return false;
                }

                path = new List<GridPosition> { unit.Position };
                return true;
            }

            int maxMove = PassiveSkillRules.GetMoveRange(unit) + EquipmentEffectRules.GetMoveBonus(context, unit);
            Dictionary<GridPosition, int> costs = new Dictionary<GridPosition, int>
            {
                [unit.Position] = 0,
            };
            Dictionary<GridPosition, GridPosition> previous = new Dictionary<GridPosition, GridPosition>();
            List<GridPosition> frontier = new List<GridPosition> { unit.Position };

            while (frontier.Count > 0)
            {
                int bestIndex = 0;
                for (int index = 1; index < frontier.Count; index++)
                {
                    if (costs[frontier[index]] < costs[frontier[bestIndex]])
                    {
                        bestIndex = index;
                    }
                }

                GridPosition current = frontier[bestIndex];
                frontier.RemoveAt(bestIndex);
                int currentCost = costs[current];
                if (current == destination)
                {
                    break;
                }

                foreach (GridPosition neighbor in current.GetOrthogonalNeighbors())
                {
                    if (!context.IsInside(neighbor))
                    {
                        continue;
                    }

                    if (!context.IsWalkable(neighbor))
                    {
                        continue;
                    }

                    if (neighbor != destination && neighbor != unit.Position && context.IsOccupied(neighbor))
                    {
                        continue;
                    }

                    int nextCost = currentCost + TerrainRules.GetMoveCost(unit, context.GetTerrainAt(neighbor));
                    if (nextCost > maxMove)
                    {
                        continue;
                    }

                    if (costs.TryGetValue(neighbor, out int knownCost) && knownCost <= nextCost)
                    {
                        continue;
                    }

                    costs[neighbor] = nextCost;
                    previous[neighbor] = current;
                    if (!frontier.Contains(neighbor))
                    {
                        frontier.Add(neighbor);
                    }
                }
            }

            if (!costs.TryGetValue(destination, out moveCost))
            {
                return false;
            }

            List<GridPosition> orderedPath = new List<GridPosition>();
            GridPosition cursor = destination;
            orderedPath.Add(cursor);
            while (cursor != unit.Position)
            {
                if (!previous.TryGetValue(cursor, out GridPosition prior))
                {
                    return false;
                }

                cursor = prior;
                orderedPath.Add(cursor);
            }

            orderedPath.Reverse();
            path = orderedPath;
            return true;
        }
    }
}
