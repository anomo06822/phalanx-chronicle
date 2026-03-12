namespace PhalanxChronicle.Battle.Effects
{
    public sealed class BattlePresentationProfile
    {
        public BattlePresentationProfile(
            float barkDelay,
            float interEffectDelay,
            float postSkillHold,
            float postCombatHold,
            float postActionHold)
        {
            BarkDelay = barkDelay;
            InterEffectDelay = interEffectDelay;
            PostSkillHold = postSkillHold;
            PostCombatHold = postCombatHold;
            PostActionHold = postActionHold;
        }

        public float BarkDelay { get; }

        public float InterEffectDelay { get; }

        public float PostSkillHold { get; }

        public float PostCombatHold { get; }

        public float PostActionHold { get; }

        public static BattlePresentationProfile PlayerReadable { get; } =
            new BattlePresentationProfile(0.05f, 0.01f, 0.06f, 0.08f, 0.16f);

        public static BattlePresentationProfile EnemyFastResolve { get; } =
            new BattlePresentationProfile(0.02f, 0f, 0.03f, 0.05f, 0.12f);
    }
}
