namespace PhalanxChronicle.Core
{
    public sealed class CombatResult
    {
        public CombatResult(string attackerUnitId, string defenderUnitId, int damage, int defenderRemainingHp, bool defenderDied)
        {
            AttackerUnitId = attackerUnitId;
            DefenderUnitId = defenderUnitId;
            Damage = damage;
            DefenderRemainingHp = defenderRemainingHp;
            DefenderDied = defenderDied;
        }

        public string AttackerUnitId { get; }

        public string DefenderUnitId { get; }

        public int Damage { get; }

        public int DefenderRemainingHp { get; }

        public bool DefenderDied { get; }
    }
}
