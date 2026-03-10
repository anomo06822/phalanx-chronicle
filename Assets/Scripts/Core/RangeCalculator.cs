using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class RangeCalculator
    {
        public IReadOnlyList<GridPosition> GetMoveRange(BattleContext context, UnitRuntimeState unit)
        {
            int moveRange = PassiveSkillRules.GetMoveRange(unit);
            Dictionary<GridPosition, int> distances = new Dictionary<GridPosition, int>();
            Queue<GridPosition> frontier = new Queue<GridPosition>();
            frontier.Enqueue(unit.Position);
            distances[unit.Position] = 0;

            while (frontier.Count > 0)
            {
                GridPosition current = frontier.Dequeue();
                int nextDistance = distances[current] + 1;
                if (nextDistance > moveRange)
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

                    if (distances.ContainsKey(neighbor))
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
    }
}
