using System.Linq;

namespace PhalanxChronicle.Core
{
    public static class SupportRules
    {
        public static int GetAttackBonus(BattleContext context, UnitRuntimeState unit, GridPosition origin)
        {
            return GetAdjacentAllies(context, unit, origin)
                .Select(ally => UnitClassCatalog.Get(ally.ClassId))
                .Sum(definition => definition.SupportAttackBonus);
        }

        public static int GetDefenseBonus(BattleContext context, UnitRuntimeState unit)
        {
            return GetAdjacentAllies(context, unit, unit.Position)
                .Select(ally => UnitClassCatalog.Get(ally.ClassId))
                .Sum(definition => definition.SupportDefenseBonus);
        }

        public static int GetManaRecovery(BattleContext context, UnitRuntimeState unit)
        {
            return GetAdjacentAllies(context, unit, unit.Position)
                .Select(ally => UnitClassCatalog.Get(ally.ClassId))
                .Sum(definition => definition.SupportManaRecovery);
        }

        private static IQueryable<UnitRuntimeState> GetAdjacentAllies(BattleContext context, UnitRuntimeState unit, GridPosition origin)
        {
            return context.GetUnits(unit.Faction)
                .Where(ally => ally.Id != unit.Id && ally.Position.ManhattanDistance(origin) == 1)
                .AsQueryable();
        }
    }
}
