using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Battle.Units;
using PhalanxChronicle.Core;
using PhalanxChronicle.Localization;
using PhalanxChronicle.Presentation;
using PhalanxChronicle.UI;
using UnityEngine;
using Text = TMPro.TextMeshProUGUI;

namespace PhalanxChronicle.Battle.Effects
{
    public sealed class BattlePresentationController
    {
        private readonly BattleActionSequencer actionSequencer;
        private readonly BattleHudModelBuilder hudModelBuilder;

        public BattlePresentationController(BattleActionSequencer actionSequencer, BattleHudModelBuilder hudModelBuilder)
        {
            this.actionSequencer = actionSequencer ?? new BattleActionSequencer();
            this.hudModelBuilder = hudModelBuilder ?? new BattleHudModelBuilder();
        }

        public IEnumerator PlaySkillSequence(
            BattleSimulation simulation,
            BattleHUD battleHUD,
            Func<string, Unit> getUnitView,
            SkillResult skillResult,
            TurnSide actingSide,
            Func<string, ActiveSkillType, string> getSkillBarkText,
            Action refreshAllVisuals)
        {
            if (simulation == null || battleHUD == null || skillResult == null || getUnitView == null)
            {
                yield break;
            }

            Unit casterView = getUnitView(skillResult.CasterUnitId);
            Unit primaryTargetView = getUnitView(skillResult.PrimaryTargetUnitId);
            UnitRuntimeState casterState = simulation.Context.GetUnit(skillResult.CasterUnitId);
            bool shouldBatchTargets = actionSequencer.ShouldBatchSkillResult(actingSide, skillResult);
            BattlePresentationProfile profile = actionSequencer.GetProfile(actingSide);
            if (actionSequencer.ShouldTakeOverRibbon(actingSide, skillResult))
            {
                battleHUD.BindForecast(hudModelBuilder.BuildSkillResultForecastModel(simulation, skillResult));
            }
            else
            {
                battleHUD.ClearForecast();
            }

            if (casterView != null && getSkillBarkText != null)
            {
                string barkText = getSkillBarkText(skillResult.CasterUnitId, skillResult.SkillType);
                if (!string.IsNullOrWhiteSpace(barkText) && actionSequencer.ShouldShowSkillBark(actingSide, skillResult))
                {
                    FloatingText.Spawn(barkText, casterView.GetAnchorPosition(1.32f), new Color(1f, 0.92f, 0.72f, 1f));
                    yield return new WaitForSeconds(profile.BarkDelay);
                }
            }

            if (casterView != null)
            {
                yield return SkillVisualEffects.PlayCasterEffect(skillResult.SkillType, casterView, primaryTargetView, casterState);
            }

            if (casterView != null && primaryTargetView != null && SkillVisualEffects.ShouldAnimateLunge(skillResult.SkillType))
            {
                yield return casterView.AnimateAttack(primaryTargetView.transform.position);
            }

            int resolvedEffects = 0;
            foreach (SkillEffectResult effect in skillResult.Effects)
            {
                Unit targetView = getUnitView(effect.UnitId);
                if (targetView == null)
                {
                    continue;
                }

                bool isPrimary = effect.UnitId == skillResult.PrimaryTargetUnitId;
                if (effect.IsHealing)
                {
                    yield return SkillVisualEffects.PlayTargetEffect(skillResult.SkillType, targetView, isPrimary, casterState);
                    FloatingText.Spawn("+" + effect.Amount, targetView.GetAnchorPosition(0.98f), new Color(0.54f, 1f, 0.62f, 1f));
                    yield return targetView.AnimatePulse(new Color(0.7f, 1f, 0.78f, 1f));
                }
                else if (effect.Amount > 0)
                {
                    yield return SkillVisualEffects.PlayTargetEffect(skillResult.SkillType, targetView, isPrimary, casterState);
                    FloatingText.Spawn("-" + effect.Amount, targetView.GetAnchorPosition(0.98f), new Color(1f, 0.89f, 0.4f, 1f));
                    yield return targetView.AnimateHit();

                    if (effect.UnitDied)
                    {
                        FloatingText.Spawn(LocalizationService.Text("ui.combat.popup_ko", "KO"), targetView.GetAnchorPosition(1.24f), new Color(1f, 0.56f, 0.42f, 1f));
                    }
                }
                else
                {
                    yield return SkillVisualEffects.PlayTargetEffect(skillResult.SkillType, targetView, isPrimary, casterState);
                    yield return targetView.AnimatePulse(new Color(0.75f, 0.72f, 1f, 1f));
                }

                if (effect.AppliedStatuses.Count > 0)
                {
                    string statusFloatingText = BuildStatusFloatingText(effect.AppliedStatuses);
                    if (!string.IsNullOrWhiteSpace(statusFloatingText))
                    {
                        FloatingText.Spawn(statusFloatingText, targetView.GetAnchorPosition(1.18f), new Color(0.76f, 0.96f, 1f, 1f));
                    }
                }

                resolvedEffects++;
                float interEffectDelay = actionSequencer.GetInterEffectDelay(actingSide, skillResult);
                if (interEffectDelay > 0f)
                {
                    yield return new WaitForSeconds(interEffectDelay);
                }

                if (shouldBatchTargets && resolvedEffects < skillResult.Effects.Count)
                {
                    continue;
                }
            }

            if (casterView != null && skillResult.CasterExpGained > 0)
            {
                FloatingText.Spawn(FormatExpGainText(skillResult.CasterExpGained), casterView.GetAnchorPosition(1.2f), new Color(0.76f, 0.98f, 0.58f, 1f));
                if (skillResult.CasterLevelsGained > 0)
                {
                    FloatingText.Spawn(LocalizationService.Text("ui.exp.level_up", "LEVEL UP"), casterView.GetAnchorPosition(1.36f), new Color(0.98f, 0.9f, 0.52f, 1f));
                }
            }

            yield return new WaitForSeconds(actionSequencer.GetPostSkillHold(actingSide, skillResult));
            battleHUD.ClearForecast();
            refreshAllVisuals?.Invoke();
        }

        public IEnumerator PlayCombatSequence(
            BattleSimulation simulation,
            BattleHUD battleHUD,
            Func<string, Unit> getUnitView,
            CombatResult combatResult,
            TurnSide actingSide,
            Action refreshAllVisuals)
        {
            if (simulation == null || battleHUD == null || combatResult == null || getUnitView == null)
            {
                yield break;
            }

            Unit attackerView = getUnitView(combatResult.AttackerUnitId);
            Unit defenderView = getUnitView(combatResult.DefenderUnitId);
            if (actionSequencer.ShouldTakeOverRibbon(actingSide, combatResult))
            {
                battleHUD.BindForecast(hudModelBuilder.BuildCombatResultForecastModel(simulation, combatResult));
            }
            else
            {
                battleHUD.ClearForecast();
            }

            if (attackerView != null && defenderView != null)
            {
                yield return attackerView.AnimateAttack(defenderView.transform.position);
                yield return PlaySlashEffect(defenderView.GetAnchorPosition(0.12f));
                FloatingText.Spawn("-" + combatResult.Damage, defenderView.GetAnchorPosition(0.98f), new Color(1f, 0.89f, 0.4f, 1f));
                yield return defenderView.AnimateHit();

                if (combatResult.DefenderDied)
                {
                    FloatingText.Spawn(LocalizationService.Text("ui.combat.popup_ko", "KO"), defenderView.GetAnchorPosition(1.24f), new Color(1f, 0.56f, 0.42f, 1f));
                }

                if (combatResult.AttackerExpGained > 0)
                {
                    FloatingText.Spawn(FormatExpGainText(combatResult.AttackerExpGained), attackerView.GetAnchorPosition(1.2f), new Color(0.76f, 0.98f, 0.58f, 1f));
                    if (combatResult.AttackerLevelsGained > 0)
                    {
                        FloatingText.Spawn(LocalizationService.Text("ui.exp.level_up", "LEVEL UP"), attackerView.GetAnchorPosition(1.36f), new Color(0.98f, 0.9f, 0.52f, 1f));
                    }
                }
            }

            yield return new WaitForSeconds(actionSequencer.GetPostCombatHold(actingSide, combatResult));
            battleHUD.ClearForecast();
            refreshAllVisuals?.Invoke();
        }

        public IEnumerator PlayDuelSequence(
            BattleSimulation simulation,
            BattleHUD battleHUD,
            Func<string, Unit> getUnitView,
            DuelResult duelResult,
            TurnSide actingSide,
            Action refreshAllVisuals)
        {
            if (simulation == null || battleHUD == null || duelResult == null || getUnitView == null)
            {
                yield break;
            }

            Unit attackerView = getUnitView(duelResult.AttackerUnitId);
            Unit defenderView = getUnitView(duelResult.DefenderUnitId);
            battleHUD.BindForecast(hudModelBuilder.BuildDuelResultForecastModel(simulation, duelResult));

            yield return PlayDuelChallengeBanner(LocalizationService.Text(duelResult.TitleKey, duelResult.TitleFallback));

            if (attackerView != null && defenderView != null)
            {
                yield return PlayDuelApproachClash(attackerView, defenderView);
                yield return PlayDuelImpactFreeze(defenderView.GetAnchorPosition(0.18f));
            }

            if (defenderView != null && duelResult.DefenderEffect != null)
            {
                if (duelResult.DefenderEffect.Amount > 0)
                {
                    FloatingText.Spawn("-" + duelResult.DefenderEffect.Amount, defenderView.GetAnchorPosition(0.98f), new Color(1f, 0.89f, 0.4f, 1f));
                }

                yield return defenderView.AnimateHit();

                if (duelResult.DefenderDefeated)
                {
                    FloatingText.Spawn(LocalizationService.Text("ui.combat.popup_ko", "KO"), defenderView.GetAnchorPosition(1.24f), new Color(1f, 0.56f, 0.42f, 1f));
                }
                else if (duelResult.DefenderEffect.AppliedStatuses.Count > 0)
                {
                    string statusFloatingText = BuildStatusFloatingText(duelResult.DefenderEffect.AppliedStatuses);
                    if (!string.IsNullOrWhiteSpace(statusFloatingText))
                    {
                        FloatingText.Spawn(statusFloatingText, defenderView.GetAnchorPosition(1.18f), new Color(0.76f, 0.96f, 1f, 1f));
                    }
                }
            }

            foreach (SkillEffectResult splashEffect in duelResult.NearbyEnemyEffects)
            {
                Unit nearbyTargetView = getUnitView(splashEffect.UnitId);
                if (nearbyTargetView == null)
                {
                    continue;
                }

                if (splashEffect.Amount > 0)
                {
                    FloatingText.Spawn("-" + splashEffect.Amount, nearbyTargetView.GetAnchorPosition(0.98f), new Color(1f, 0.89f, 0.4f, 1f));
                    yield return nearbyTargetView.AnimateHit();
                }
                else
                {
                    yield return nearbyTargetView.AnimatePulse(new Color(0.75f, 0.72f, 1f, 1f));
                }

                if (splashEffect.AppliedStatuses.Count > 0)
                {
                    string statusFloatingText = BuildStatusFloatingText(splashEffect.AppliedStatuses);
                    if (!string.IsNullOrWhiteSpace(statusFloatingText))
                    {
                        FloatingText.Spawn(statusFloatingText, nearbyTargetView.GetAnchorPosition(1.16f), new Color(0.76f, 0.96f, 1f, 1f));
                    }
                }
            }

            if (attackerView != null && duelResult.CasterStatuses.Count > 0)
            {
                string statusFloatingText = BuildStatusFloatingText(duelResult.CasterStatuses);
                if (!string.IsNullOrWhiteSpace(statusFloatingText))
                {
                    FloatingText.Spawn(statusFloatingText, attackerView.GetAnchorPosition(1.18f), new Color(0.78f, 0.96f, 0.92f, 1f));
                }
            }

            if (attackerView != null && duelResult.AttackerExpGained > 0)
            {
                FloatingText.Spawn(FormatExpGainText(duelResult.AttackerExpGained), attackerView.GetAnchorPosition(1.2f), new Color(0.76f, 0.98f, 0.58f, 1f));
                if (duelResult.AttackerLevelsGained > 0)
                {
                    FloatingText.Spawn(LocalizationService.Text("ui.exp.level_up", "LEVEL UP"), attackerView.GetAnchorPosition(1.36f), new Color(0.98f, 0.9f, 0.52f, 1f));
                }
            }

            yield return PlayDuelResultOverlay(
                duelResult.DefenderDefeated
                    ? LocalizationService.Text("ui.duel.result.breakthrough", "突破")
                    : LocalizationService.Text("ui.duel.result.clash", "交鋒"),
                duelResult.DefenderDefeated ? new Color(1f, 0.88f, 0.5f, 1f) : new Color(0.84f, 0.9f, 1f, 1f));

            yield return new WaitForSeconds(actionSequencer.GetPostDuelHold(actingSide, duelResult));
            battleHUD.ClearForecast();
            refreshAllVisuals?.Invoke();
        }

        private static IEnumerator PlayDuelChallengeBanner(string title)
        {
            GameObject banner = new GameObject("DuelBanner", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            Canvas canvas = banner.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            CanvasGroup canvasGroup = banner.GetComponent<CanvasGroup>();

            GameObject panel = new GameObject("BannerPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(banner.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(420f, 86f);
            UnityEngine.UI.Image panelImage = panel.GetComponent<UnityEngine.UI.Image>();
            panelImage.sprite = RuntimeSpriteLibrary.WhiteSprite;
            panelImage.color = new Color(0.12f, 0.08f, 0.04f, 0.92f);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(panel.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 12f);
            labelRect.offsetMax = new Vector2(-18f, -12f);
            Text label = labelObject.GetComponent<Text>();
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.font = RuntimeSpriteLibrary.GetUiTmpFont(26, FontStyle.Bold);
            label.fontSize = 26f;
            label.text = string.IsNullOrWhiteSpace(title)
                ? LocalizationService.Text("ui.duel.header", "一騎對決")
                : title;
            label.color = new Color(1f, 0.9f, 0.58f, 1f);

            float elapsed = 0f;
            const float duration = 0.42f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float alpha = progress < 0.35f ? progress / 0.35f : 1f - ((progress - 0.35f) / 0.65f);
                canvasGroup.alpha = Mathf.Clamp01(alpha);
                panelRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.04f, Mathf.Clamp01(progress));
                yield return null;
            }

            UnityEngine.Object.Destroy(banner);
        }

        private static IEnumerator PlayDuelApproachClash(Unit attackerView, Unit defenderView)
        {
            if (attackerView == null || defenderView == null)
            {
                yield break;
            }

            yield return attackerView.AnimateAttack(defenderView.transform.position);
            yield return defenderView.AnimatePulse(new Color(1f, 0.82f, 0.52f, 1f));
        }

        private static IEnumerator PlayDuelImpactFreeze(Vector3 worldPosition)
        {
            GameObject impact = new GameObject("DuelImpact", typeof(SpriteRenderer));
            impact.transform.position = new Vector3(worldPosition.x, worldPosition.y, -0.8f);
            SpriteRenderer renderer = impact.GetComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteLibrary.SlashSprite;
            renderer.color = new Color(1f, 0.94f, 0.76f, 0f);
            renderer.sortingOrder = 72;

            const float duration = 0.16f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float alpha = progress < 0.4f ? progress / 0.4f : 1f - ((progress - 0.4f) / 0.6f);
                renderer.color = new Color(1f, 0.94f, 0.76f, Mathf.Clamp01(alpha));
                impact.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.4f, progress);
                yield return null;
            }

            UnityEngine.Object.Destroy(impact);
        }

        private static IEnumerator PlayDuelResultOverlay(string text, Color color)
        {
            GameObject overlay = new GameObject("DuelResultOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            Canvas canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 610;
            CanvasGroup canvasGroup = overlay.GetComponent<CanvasGroup>();

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(overlay.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(360f, 70f);
            Text label = labelObject.GetComponent<Text>();
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.font = RuntimeSpriteLibrary.GetUiTmpFont(34, FontStyle.Bold);
            label.fontSize = 34f;
            label.text = text;
            label.color = color;

            const float duration = 0.48f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float alpha = progress < 0.3f ? progress / 0.3f : 1f - ((progress - 0.3f) / 0.7f);
                canvasGroup.alpha = Mathf.Clamp01(alpha);
                labelRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-8f, 10f, progress));
                yield return null;
            }

            UnityEngine.Object.Destroy(overlay);
        }

        private static IEnumerator PlaySlashEffect(Vector3 worldPosition)
        {
            GameObject slashObject = new GameObject("SlashEffect");
            slashObject.transform.position = new Vector3(worldPosition.x, worldPosition.y, -0.72f);
            slashObject.transform.rotation = Quaternion.Euler(0f, 0f, -18f);

            SpriteRenderer renderer = slashObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteLibrary.SlashSprite;
            renderer.sortingOrder = 55;

            float duration = 0.12f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float alpha = progress < 0.5f ? progress * 2f : (1f - progress) * 2f;
                renderer.color = new Color(1f, 0.95f, 0.78f, alpha);
                slashObject.transform.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.15f, progress);
                yield return null;
            }

            UnityEngine.Object.Destroy(slashObject);
        }

        private static string BuildStatusFloatingText(IReadOnlyList<SkillStatusApplication> statuses)
        {
            return string.Join(
                " / ",
                statuses
                    .Where(status => status.WasApplied)
                    .Select(status => GetStatusDisplayName(status.Type))
                    .Distinct());
        }

        private static string FormatExpGainText(int experience)
        {
            return LocalizationService.Format("ui.exp.gain", "EXP +{0}", experience);
        }

        private static string GetStatusDisplayName(StatusEffectType statusEffectType)
        {
            switch (statusEffectType)
            {
                case StatusEffectType.Inspired:
                    return LocalizationService.Text("status.inspired.name", "Inspired");
                case StatusEffectType.ShatteredArmor:
                    return LocalizationService.Text("status.shattered_armor.name", "Shattered Armor");
                case StatusEffectType.Intimidated:
                    return LocalizationService.Text("status.intimidated.name", "Intimidated");
                case StatusEffectType.Bleeding:
                    return LocalizationService.Text("status.bleeding.name", "Bleeding");
                case StatusEffectType.Rooted:
                    return LocalizationService.Text("status.rooted.name", "Rooted");
                case StatusEffectType.Guarded:
                    return LocalizationService.Text("status.guarded.name", "Guarded");
                case StatusEffectType.Taunted:
                    return LocalizationService.Text("status.taunted.name", "Taunted");
                default:
                    return statusEffectType.ToString();
            }
        }
    }
}
