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

        public bool Refresh(int duration)
        {
            if (duration > RemainingOwnTurnEnds)
            {
                RemainingOwnTurnEnds = duration;
                return true;
            }

            return false;
        }

        public void AdvanceOwnTurnEnd()
        {
            RemainingOwnTurnEnds--;
        }
    }
}
