using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    public sealed class SkillResult
    {
        public SkillResult(string casterUnitId, ActiveSkillType skillType, string primaryTargetUnitId, IReadOnlyList<SkillEffectResult> effects)
        {
            CasterUnitId = casterUnitId;
            SkillType = skillType;
            PrimaryTargetUnitId = primaryTargetUnitId;
            Effects = effects;
        }

        public string CasterUnitId { get; }

        public ActiveSkillType SkillType { get; }

        public string PrimaryTargetUnitId { get; }

        public IReadOnlyList<SkillEffectResult> Effects { get; }

        public bool HasEffects => Effects != null && Effects.Count > 0;
    }
}
