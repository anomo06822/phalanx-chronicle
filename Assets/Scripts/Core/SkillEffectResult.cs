using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class SkillEffectResult
    {
        public SkillEffectResult(
            string unitId,
            int amount,
            int remainingHp,
            bool unitDied,
            bool isHealing,
            IReadOnlyList<SkillStatusApplication> appliedStatuses = null)
        {
            UnitId = unitId;
            Amount = amount;
            RemainingHp = remainingHp;
            UnitDied = unitDied;
            IsHealing = isHealing;
            AppliedStatuses = appliedStatuses != null
                ? appliedStatuses
                    .Where(status => status != null && status.Type != StatusEffectType.None)
                    .ToList()
                : new List<SkillStatusApplication>();
        }

        public string UnitId { get; }

        public int Amount { get; }

        public int RemainingHp { get; }

        public bool UnitDied { get; }

        public bool IsHealing { get; }

        public IReadOnlyList<SkillStatusApplication> AppliedStatuses { get; }

        public StatusEffectType AppliedStatus => AppliedStatuses.FirstOrDefault(status => status.WasApplied)?.Type ?? StatusEffectType.None;
    }
}
