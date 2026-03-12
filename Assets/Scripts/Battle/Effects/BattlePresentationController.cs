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

                float interEffectDelay = actionSequencer.GetInterEffectDelay(actingSide, skillResult);
                if (interEffectDelay > 0f)
                {
                    yield return new WaitForSeconds(interEffectDelay);
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
