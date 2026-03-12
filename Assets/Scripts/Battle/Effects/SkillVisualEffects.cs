using System.Collections;
using PhalanxChronicle.Battle.Units;
using PhalanxChronicle.Core;
using PhalanxChronicle.Presentation;
using UnityEngine;

namespace PhalanxChronicle.Battle.Effects
{
    public static class SkillVisualEffects
    {
        public static bool ShouldAnimateLunge(ActiveSkillType skillType)
        {
            return skillType == ActiveSkillType.PowerStrike ||
                   skillType == ActiveSkillType.DragonPierce ||
                   skillType == ActiveSkillType.WesternStampede ||
                   skillType == ActiveSkillType.GreenDragonSlash ||
                   skillType == ActiveSkillType.AzureDragonSlash;
        }

        public static IEnumerator PlayCasterEffect(ActiveSkillType skillType, Unit casterView, Unit primaryTargetView, UnitRuntimeState casterState = null)
        {
            if (casterView == null)
            {
                yield break;
            }

            Color accent = RoleLoadoutCatalog.GetSkillAccent(skillType);
            bool mastered = ActiveSkillRules.IsMastered(casterState);
            switch (skillType)
            {
                case ActiveSkillType.RoyalAid:
                case ActiveSkillType.ImperialAid:
                case ActiveSkillType.GuardOrder:
                    yield return PlayPulse(
                        "RoyalAidCast",
                        casterView.GetAnchorPosition(0.42f),
                        accent,
                        new Vector3(0.35f, 0.35f, 1f),
                        new Vector3(mastered ? 1.2f : 1.05f, mastered ? 1.2f : 1.05f, 1f),
                        mastered ? 0.2f : 0.16f,
                        RuntimeSpriteLibrary.RingSprite,
                        0f,
                        24f);
                    if (mastered)
                    {
                        yield return PlayPulse(
                            "RoyalAidCastMastery",
                            casterView.GetAnchorPosition(0.42f),
                            new Color(1f, 0.96f, 0.7f, 0.92f),
                            new Vector3(0.28f, 0.28f, 1f),
                            new Vector3(1.32f, 1.32f, 1f),
                            0.18f,
                            RuntimeSpriteLibrary.RingSprite,
                            0.06f,
                            38f);
                    }

                    if (primaryTargetView != null)
                    {
                        yield return PlayBeam(casterView.GetAnchorPosition(0.42f), primaryTargetView.GetAnchorPosition(0.62f), accent, 0.09f, 0.12f);
                    }

                    break;
                case ActiveSkillType.PowerStrike:
                case ActiveSkillType.DragonPierce:
                case ActiveSkillType.WesternStampede:
                    yield return PlaySparkBurst(casterView.GetAnchorPosition(0.34f), accent, 0.12f, mastered ? 0.88f : 0.72f);
                    if (mastered)
                    {
                        yield return PlayPulse("PowerStrikeMastery", casterView.GetAnchorPosition(0.32f), accent, new Vector3(0.22f, 0.22f, 1f), new Vector3(0.88f, 0.88f, 1f), 0.12f, RuntimeSpriteLibrary.RingSprite, 0f, 20f);
                    }

                    break;
                case ActiveSkillType.Volley:
                case ActiveSkillType.SkyVolley:
                case ActiveSkillType.PinningShot:
                    yield return PlayPulse(
                        "VolleyCast",
                        casterView.GetAnchorPosition(0.5f),
                        accent,
                        new Vector3(0.28f, 0.28f, 1f),
                        new Vector3(mastered ? 1.08f : 0.92f, mastered ? 1.08f : 0.92f, 1f),
                        mastered ? 0.15f : 0.12f,
                        RuntimeSpriteLibrary.RingSprite,
                        0f,
                        -16f);
                    if (mastered)
                    {
                        yield return PlaySparkBurst(casterView.GetAnchorPosition(0.56f), new Color(1f, 0.94f, 0.68f, 1f), 0.12f, 0.46f);
                    }

                    break;
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.AzureDragonSlash:
                    yield return PlaySparkBurst(casterView.GetAnchorPosition(0.34f), accent, 0.12f, mastered ? 0.86f : 0.7f);
                    if (mastered)
                    {
                        yield return PlayPulse("SlashMastery", casterView.GetAnchorPosition(0.38f), accent, new Vector3(0.24f, 0.24f, 1f), new Vector3(0.94f, 0.94f, 1f), 0.12f, RuntimeSpriteLibrary.RingSprite, 0f, 18f);
                    }

                    if (primaryTargetView != null)
                    {
                        yield return PlayBeam(casterView.GetAnchorPosition(0.48f), primaryTargetView.GetAnchorPosition(0.36f), accent, 0.11f, 0.14f);
                    }

                    break;
                case ActiveSkillType.WarCry:
                case ActiveSkillType.LionWarCry:
                    yield return PlayPulse(
                        "WarCryCast",
                        casterView.GetAnchorPosition(0.2f),
                        accent,
                        new Vector3(0.42f, 0.42f, 1f),
                        new Vector3(mastered ? 1.84f : 1.6f, mastered ? 1.84f : 1.6f, 1f),
                        mastered ? 0.22f : 0.18f,
                        RuntimeSpriteLibrary.RingSprite,
                        0f,
                        24f);
                    yield return PlaySparkBurst(casterView.GetAnchorPosition(0.82f), accent, 0.12f, mastered ? 0.72f : 0.58f);
                    if (mastered)
                    {
                        yield return PlayPulse("WarCryMastery", casterView.GetAnchorPosition(0.2f), new Color(1f, 0.92f, 0.64f, 0.92f), new Vector3(0.3f, 0.3f, 1f), new Vector3(1.98f, 1.98f, 1f), 0.2f, RuntimeSpriteLibrary.RingSprite, 0f, 32f);
                    }

                    break;
                case ActiveSkillType.FireStratagem:
                case ActiveSkillType.EightTrigramInferno:
                    yield return PlayArcaneSigil(casterView.GetAnchorPosition(0.18f), accent, mastered ? 1.22f : 1f, mastered);
                    if (primaryTargetView != null)
                    {
                        yield return PlayBeam(casterView.GetAnchorPosition(0.44f), primaryTargetView.GetAnchorPosition(0.52f), accent, mastered ? 0.12f : 0.09f, mastered ? 0.14f : 0.11f);
                    }

                    break;
            }
        }

        public static IEnumerator PlayTargetEffect(ActiveSkillType skillType, Unit targetView, bool isPrimaryTarget, UnitRuntimeState casterState = null)
        {
            if (targetView == null)
            {
                yield break;
            }

            Color accent = RoleLoadoutCatalog.GetSkillAccent(skillType);
            bool mastered = ActiveSkillRules.IsMastered(casterState);
            switch (skillType)
            {
                case ActiveSkillType.RoyalAid:
                case ActiveSkillType.ImperialAid:
                case ActiveSkillType.GuardOrder:
                    yield return PlayPulse(
                        "RoyalAidTarget",
                        targetView.GetAnchorPosition(0.5f),
                        accent,
                        new Vector3(0.34f, 0.34f, 1f),
                        new Vector3(mastered ? 1.36f : 1.18f, mastered ? 1.36f : 1.18f, 1f),
                        mastered ? 0.2f : 0.18f,
                        RuntimeSpriteLibrary.RingSprite,
                        0.08f,
                        18f);
                    yield return PlaySparkBurst(targetView.GetAnchorPosition(0.88f), new Color(1f, 0.98f, 0.72f, 1f), 0.14f, mastered ? 0.94f : 0.78f);
                    break;
                case ActiveSkillType.PowerStrike:
                case ActiveSkillType.DragonPierce:
                case ActiveSkillType.WesternStampede:
                    yield return PlaySparkBurst(targetView.GetAnchorPosition(0.2f), accent, 0.12f, isPrimaryTarget ? (mastered ? 1.18f : 1.02f) : (mastered ? 0.92f : 0.78f));
                    yield return PlayPulse(
                        "PowerStrikeRing",
                        targetView.GetAnchorPosition(0.18f),
                        new Color(1f, 0.86f, 0.52f, 0.95f),
                        new Vector3(0.22f, 0.22f, 1f),
                        new Vector3(mastered ? 1.08f : 0.92f, mastered ? 1.08f : 0.92f, 1f),
                        mastered ? 0.15f : 0.12f,
                        RuntimeSpriteLibrary.RingSprite,
                        0f,
                        22f);
                    break;
                case ActiveSkillType.Volley:
                case ActiveSkillType.SkyVolley:
                case ActiveSkillType.PinningShot:
                    yield return PlayArrowStrike(targetView.GetAnchorPosition(0.18f), accent, isPrimaryTarget ? (mastered ? 1.08f : 0.96f) : (mastered ? 0.88f : 0.76f));
                    break;
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.AzureDragonSlash:
                    yield return PlaySlashArc(targetView.GetAnchorPosition(0.24f), accent, isPrimaryTarget ? (mastered ? 1.22f : 1.08f) : (mastered ? 0.94f : 0.8f));
                    yield return PlaySparkBurst(targetView.GetAnchorPosition(0.24f), new Color(0.8f, 1f, 0.85f, 1f), 0.12f, isPrimaryTarget ? (mastered ? 0.86f : 0.7f) : (mastered ? 0.66f : 0.54f));
                    break;
                case ActiveSkillType.WarCry:
                case ActiveSkillType.LionWarCry:
                    yield return PlayPulse(
                        "WarCryTarget",
                        targetView.GetAnchorPosition(0.4f),
                        accent,
                        new Vector3(0.32f, 0.32f, 1f),
                        new Vector3(mastered ? 1.16f : 1f, mastered ? 1.16f : 1f, 1f),
                        mastered ? 0.18f : 0.15f,
                        RuntimeSpriteLibrary.RingSprite,
                        0.05f,
                        -18f);
                    yield return PlaySparkBurst(targetView.GetAnchorPosition(0.7f), new Color(1f, 0.75f, 0.56f, 0.95f), 0.12f, mastered ? 0.7f : 0.56f);
                    break;
                case ActiveSkillType.FireStratagem:
                case ActiveSkillType.EightTrigramInferno:
                    yield return PlayMeteorStrike(targetView.GetAnchorPosition(0.3f), accent, isPrimaryTarget ? 1f : 0.82f, mastered);
                    break;
            }
        }

        private static IEnumerator PlayArcaneSigil(Vector3 worldPosition, Color color, float scale, bool mastered)
        {
            yield return PlayPulse(
                "ArcaneSigil",
                worldPosition,
                color,
                new Vector3(0.28f, 0.28f, 1f),
                new Vector3(1.08f * scale, 1.08f * scale, 1f),
                mastered ? 0.2f : 0.16f,
                RuntimeSpriteLibrary.RingSprite,
                0.05f,
                mastered ? 42f : 28f);
            yield return PlayPulse(
                "ArcaneSigilEcho",
                worldPosition,
                new Color(color.r, Mathf.Min(1f, color.g + 0.12f), Mathf.Min(1f, color.b + 0.18f), mastered ? 0.9f : 0.76f),
                new Vector3(0.18f, 0.18f, 1f),
                new Vector3((mastered ? 1.28f : 1.08f) * scale, (mastered ? 1.28f : 1.08f) * scale, 1f),
                mastered ? 0.18f : 0.14f,
                RuntimeSpriteLibrary.RingSprite,
                0.02f,
                mastered ? -48f : -30f);
            yield return PlaySparkBurst(worldPosition + new Vector3(0f, 0.4f, 0f), new Color(1f, 0.82f, 0.48f, 0.95f), 0.14f, mastered ? 0.72f : 0.56f);
        }

        private static IEnumerator PlayMeteorStrike(Vector3 worldPosition, Color color, float scale, bool mastered)
        {
            yield return PlayPulse(
                "InfernoGround",
                worldPosition,
                color,
                new Vector3(0.26f, 0.26f, 1f),
                new Vector3((mastered ? 1.24f : 1.04f) * scale, (mastered ? 1.24f : 1.04f) * scale, 1f),
                mastered ? 0.18f : 0.14f,
                RuntimeSpriteLibrary.RingSprite,
                0f,
                mastered ? 36f : 20f);
            yield return PlayPulse(
                "InfernoRing",
                worldPosition + new Vector3(0f, 0.06f, 0f),
                new Color(1f, 0.74f, 0.32f, mastered ? 0.9f : 0.72f),
                new Vector3(0.2f, 0.2f, 1f),
                new Vector3((mastered ? 1.48f : 1.24f) * scale, (mastered ? 1.48f : 1.24f) * scale, 1f),
                mastered ? 0.16f : 0.12f,
                RuntimeSpriteLibrary.RingSprite,
                0.02f,
                mastered ? -44f : -26f);
            yield return PlaySparkBurst(worldPosition + new Vector3(0f, 0.55f, 0f), new Color(1f, 0.8f, 0.46f, 0.96f), 0.14f, (mastered ? 0.98f : 0.78f) * scale);
            if (mastered)
            {
                yield return PlaySparkBurst(worldPosition + new Vector3(0.08f, 0.8f, 0f), new Color(1f, 0.96f, 0.64f, 0.9f), 0.12f, 0.42f * scale);
            }
        }

        private static IEnumerator PlayPulse(
            string effectName,
            Vector3 worldPosition,
            Color color,
            Vector3 startScale,
            Vector3 endScale,
            float duration,
            Sprite sprite,
            float driftY,
            float endRotation)
        {
            GameObject effectObject = CreateEffectObject(effectName, sprite, worldPosition, color, 57);
            SpriteRenderer renderer = effectObject.GetComponent<SpriteRenderer>();
            Vector3 startPosition = effectObject.transform.position;
            Quaternion startRotation = effectObject.transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, endRotation);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                effectObject.transform.position = startPosition + Vector3.up * (driftY * eased);
                effectObject.transform.localScale = Vector3.Lerp(startScale, endScale, eased);
                effectObject.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);
                renderer.color = Color.Lerp(color, new Color(color.r, color.g, color.b, 0f), progress);
                yield return null;
            }

            Object.Destroy(effectObject);
        }

        private static IEnumerator PlaySparkBurst(Vector3 worldPosition, Color color, float duration, float scale)
        {
            GameObject effectObject = CreateEffectObject("SparkBurst", RuntimeSpriteLibrary.SparkSprite, worldPosition, color, 58);
            SpriteRenderer renderer = effectObject.GetComponent<SpriteRenderer>();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.Sin(progress * Mathf.PI);
                effectObject.transform.localScale = Vector3.one * Mathf.Lerp(scale * 0.35f, scale, eased);
                effectObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 65f, progress));
                renderer.color = Color.Lerp(color, new Color(color.r, color.g, color.b, 0f), progress);
                yield return null;
            }

            Object.Destroy(effectObject);
        }

        private static IEnumerator PlayArrowStrike(Vector3 worldPosition, Color color, float scale)
        {
            GameObject effectObject = CreateEffectObject("ArrowStrike", RuntimeSpriteLibrary.ArrowSprite, worldPosition + new Vector3(0f, 1.3f, 0f), color, 58);
            SpriteRenderer renderer = effectObject.GetComponent<SpriteRenderer>();
            Vector3 startPosition = effectObject.transform.position;
            Vector3 endPosition = worldPosition;
            float duration = 0.16f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                effectObject.transform.position = Vector3.Lerp(startPosition, endPosition, eased);
                effectObject.transform.localScale = new Vector3(scale, scale, 1f);
                renderer.color = Color.Lerp(color, new Color(color.r, color.g, color.b, 0f), Mathf.Max(0f, progress - 0.45f) / 0.55f);
                yield return null;
            }

            Object.Destroy(effectObject);
            yield return PlaySparkBurst(worldPosition, new Color(1f, 0.96f, 0.76f, 0.92f), 0.1f, scale * 0.7f);
            yield return PlayPulse("ArrowImpactRing", worldPosition, new Color(color.r, color.g, color.b, 0.82f), new Vector3(0.18f, 0.18f, 1f), new Vector3(scale * 0.88f, scale * 0.88f, 1f), 0.08f, RuntimeSpriteLibrary.RingSprite, 0f, 18f);
        }

        private static IEnumerator PlaySlashArc(Vector3 worldPosition, Color color, float scale)
        {
            GameObject effectObject = CreateEffectObject("SlashArc", RuntimeSpriteLibrary.SlashSprite, worldPosition, color, 58);
            SpriteRenderer renderer = effectObject.GetComponent<SpriteRenderer>();
            float duration = 0.14f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                effectObject.transform.localScale = Vector3.one * Mathf.Lerp(scale * 0.45f, scale, progress);
                effectObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-34f, 14f, progress));
                renderer.color = Color.Lerp(color, new Color(color.r, color.g, color.b, 0f), progress);
                yield return null;
            }

            Object.Destroy(effectObject);
        }

        private static IEnumerator PlayBeam(Vector3 start, Vector3 end, Color color, float width, float duration)
        {
            GameObject effectObject = CreateEffectObject("SkillBeam", RuntimeSpriteLibrary.WhiteSprite, Vector3.Lerp(start, end, 0.5f), color, 56);
            SpriteRenderer renderer = effectObject.GetComponent<SpriteRenderer>();
            Vector3 delta = end - start;
            float length = Mathf.Max(0.01f, delta.magnitude);
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            effectObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float alpha = progress < 0.35f ? progress / 0.35f : 1f - ((progress - 0.35f) / 0.65f);
                effectObject.transform.localScale = new Vector3(length, Mathf.Lerp(width * 0.7f, width, progress), 1f);
                renderer.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha) * 0.8f);
                yield return null;
            }

            Object.Destroy(effectObject);
        }

        private static GameObject CreateEffectObject(string effectName, Sprite sprite, Vector3 worldPosition, Color color, int sortingOrder)
        {
            GameObject effectObject = new GameObject(effectName);
            effectObject.transform.position = new Vector3(worldPosition.x, worldPosition.y, -0.74f);

            SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return effectObject;
        }
    }
}
