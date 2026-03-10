namespace PhalanxChronicle.Core
{
    public sealed class ObjectiveState
    {
        public ObjectiveState(
            string primaryObjectiveKey,
            string primaryObjectiveFallback,
            string failureConditionKey,
            string failureConditionFallback)
        {
            PrimaryObjectiveKey = primaryObjectiveKey;
            PrimaryObjectiveFallback = primaryObjectiveFallback;
            FailureConditionKey = failureConditionKey;
            FailureConditionFallback = failureConditionFallback;
        }

        public string PrimaryObjectiveKey { get; }

        public string PrimaryObjectiveFallback { get; }

        public string FailureConditionKey { get; }

        public string FailureConditionFallback { get; }
    }
}
