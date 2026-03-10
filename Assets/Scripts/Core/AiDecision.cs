namespace PhalanxChronicle.Core
{
    public sealed class AiDecision
    {
        public AiDecision(GridPosition destination, AiActionType actionType, string targetUnitId)
        {
            Destination = destination;
            ActionType = actionType;
            TargetUnitId = targetUnitId;
        }

        public GridPosition Destination { get; }

        public AiActionType ActionType { get; }

        public string TargetUnitId { get; }

        public bool HasTarget => !string.IsNullOrEmpty(TargetUnitId);
    }
}
