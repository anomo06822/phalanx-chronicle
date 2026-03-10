using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class BattleContext
    {
        private readonly Dictionary<GridPosition, GridCell> cells;
        private readonly Dictionary<string, UnitRuntimeState> unitsById;

        public BattleContext(StageDefinitionData stage)
        {
            StageName = stage.StageName;
            StageNameKey = stage.StageNameKey;
            Width = stage.Width;
            Height = stage.Height;
            IsRandomMap = stage.IsRandomMap;
            MapSeed = stage.MapSeed;
            cells = CreateCells(stage.Width, stage.Height);
            unitsById = new Dictionary<string, UnitRuntimeState>();

            foreach (GridPosition blockedCell in stage.BlockedCells)
            {
                if (cells.TryGetValue(blockedCell, out GridCell cell))
                {
                    cell.SetBlocked(true);
                }
            }

            foreach (UnitSpawnData spawn in stage.UnitSpawns)
            {
                UnitRuntimeState unit = new UnitRuntimeState(spawn.Definition, spawn.StartPosition);
                unitsById.Add(unit.Id, unit);
                cells[spawn.StartPosition].SetOccupant(unit.Id);
            }
        }

        public string StageName { get; }

        public string StageNameKey { get; }

        public int Width { get; }

        public int Height { get; }

        public bool IsRandomMap { get; }

        public int MapSeed { get; }

        public TurnSide CurrentTurnSide { get; private set; } = TurnSide.Player;

        public bool BattleEnded { get; private set; }

        public TurnSide WinningSide { get; private set; }

        public IReadOnlyCollection<UnitRuntimeState> Units => unitsById.Values;

        public IReadOnlyList<GridPosition> BlockedCells => cells.Values
            .Where(cell => cell.IsBlocked)
            .Select(cell => cell.Position)
            .OrderBy(position => position.Y)
            .ThenBy(position => position.X)
            .ToList();

        public bool IsInside(GridPosition position)
        {
            return position.X >= 0 &&
                   position.X < Width &&
                   position.Y >= 0 &&
                   position.Y < Height;
        }

        public GridCell GetCell(GridPosition position)
        {
            return cells[position];
        }

        public UnitRuntimeState GetUnit(string unitId)
        {
            return unitsById.TryGetValue(unitId, out UnitRuntimeState unit) ? unit : null;
        }

        public UnitRuntimeState GetUnitAt(GridPosition position)
        {
            if (!cells.TryGetValue(position, out GridCell cell) || !cell.IsOccupied)
            {
                return null;
            }

            return GetUnit(cell.OccupantUnitId);
        }

        public IReadOnlyList<UnitRuntimeState> GetUnits(UnitFaction faction, bool aliveOnly = true)
        {
            IEnumerable<UnitRuntimeState> query = unitsById.Values.Where(unit => unit.Faction == faction);
            if (aliveOnly)
            {
                query = query.Where(unit => unit.IsAlive);
            }

            return query
                .OrderBy(unit => unit.Position.Y)
                .ThenBy(unit => unit.Position.X)
                .ThenBy(unit => unit.Id)
                .ToList();
        }

        public bool IsOccupied(GridPosition position)
        {
            return IsInside(position) && cells[position].IsOccupied;
        }

        public bool IsWalkable(GridPosition position)
        {
            return IsInside(position) && cells[position].IsWalkable;
        }

        public void MoveUnit(string unitId, GridPosition destination)
        {
            UnitRuntimeState unit = GetUnit(unitId);
            GridCell sourceCell = cells[unit.Position];
            GridCell destinationCell = cells[destination];
            sourceCell.ClearOccupant();
            destinationCell.SetOccupant(unit.Id);
            unit.MoveTo(destination);
        }

        public void RemoveUnit(string unitId)
        {
            UnitRuntimeState unit = GetUnit(unitId);
            if (unit == null)
            {
                return;
            }

            GridCell cell = cells[unit.Position];
            if (cell.OccupantUnitId == unitId)
            {
                cell.ClearOccupant();
            }
        }

        public void SetCurrentTurn(TurnSide turnSide)
        {
            CurrentTurnSide = turnSide;
        }

        public void EvaluateBattleOutcome()
        {
            bool anyPlayersAlive = GetUnits(UnitFaction.Player).Count > 0;
            bool anyEnemiesAlive = GetUnits(UnitFaction.Enemy).Count > 0;

            if (!anyEnemiesAlive)
            {
                BattleEnded = true;
                WinningSide = TurnSide.Player;
            }
            else if (!anyPlayersAlive)
            {
                BattleEnded = true;
                WinningSide = TurnSide.Enemy;
            }
        }

        private static Dictionary<GridPosition, GridCell> CreateCells(int width, int height)
        {
            Dictionary<GridPosition, GridCell> result = new Dictionary<GridPosition, GridCell>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    result[position] = new GridCell(position);
                }
            }

            return result;
        }
    }
}
