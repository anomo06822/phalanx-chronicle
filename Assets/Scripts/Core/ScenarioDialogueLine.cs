namespace PhalanxChronicle.Core
{
    public sealed class ScenarioDialogueLine
    {
        public ScenarioDialogueLine(string speakerNameKey, string speakerFallback, string textKey, string textFallback)
        {
            SpeakerNameKey = speakerNameKey;
            SpeakerFallback = speakerFallback;
            TextKey = textKey;
            TextFallback = textFallback;
        }

        public string SpeakerNameKey { get; }

        public string SpeakerFallback { get; }

        public string TextKey { get; }

        public string TextFallback { get; }
    }
}
