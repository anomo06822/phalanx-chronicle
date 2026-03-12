using System;

namespace PhalanxChronicle.Core
{
    [Serializable]
    public sealed class SkillStatusApplication
    {
        public SkillStatusApplication(StatusEffectType type, int duration, bool wasApplied)
        {
            Type = type;
            Duration = duration < 0 ? 0 : duration;
            WasApplied = wasApplied;
        }

        public StatusEffectType Type { get; }

        public int Duration { get; }

        public bool WasApplied { get; }
    }
}
