namespace PhalanxChronicle.Core
{
    public sealed class SkillEffectResult
    {
        public SkillEffectResult(string unitId, int amount, int remainingHp, bool unitDied, bool isHealing, StatusEffectType appliedStatus)
        {
            UnitId = unitId;
            Amount = amount;
            RemainingHp = remainingHp;
            UnitDied = unitDied;
            IsHealing = isHealing;
            AppliedStatus = appliedStatus;
        }

        public string UnitId { get; }

        public int Amount { get; }

        public int RemainingHp { get; }

        public bool UnitDied { get; }

        public bool IsHealing { get; }

        public StatusEffectType AppliedStatus { get; }
    }
}
