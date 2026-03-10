using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class MoveSystem
    {
        private readonly RangeCalculator rangeCalculator;

        public MoveSystem(RangeCalculator rangeCalculator)
        {
            this.rangeCalculator = rangeCalculator;
        }

        public bool TryMove(BattleContext context, UnitRuntimeState unit, GridPosition destination)
        {
            if (unit == null || !unit.IsAlive || unit.HasActed || unit.HasMovedThisTurn)
            {
                return false;
            }

            if (!context.IsInside(destination))
            {
                return false;
            }

            bool isReachable = rangeCalculator.GetMoveRange(context, unit).Contains(destination);
            if (!isReachable)
            {
                return false;
            }

            if (destination != unit.Position && context.IsOccupied(destination))
            {
                return false;
            }

            context.MoveUnit(unit.Id, destination);
            return true;
        }
    }
}
