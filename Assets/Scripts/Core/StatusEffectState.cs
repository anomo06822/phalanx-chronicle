namespace PhalanxChronicle.Core
{
    public sealed class StatusEffectState
    {
        public StatusEffectState(StatusEffectType type, int remainingOwnTurnEnds)
        {
            Type = type;
            RemainingOwnTurnEnds = remainingOwnTurnEnds;
        }

        public StatusEffectType Type { get; }

        public int RemainingOwnTurnEnds { get; private set; }

        public void Refresh(int duration)
        {
            if (duration > RemainingOwnTurnEnds)
            {
                RemainingOwnTurnEnds = duration;
            }
        }

        public void AdvanceOwnTurnEnd()
        {
            RemainingOwnTurnEnds--;
        }
    }
}
