using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class SkillSystem
    {
        public IReadOnlyList<GridPosition> GetSkillRange(BattleContext context, UnitRuntimeState caster)
        {
            return GetSkillTargets(context, caster)
                .Select(unit => unit.Position)
                .Distinct()
                .OrderBy(position => position.Y)
                .ThenBy(position => position.X)
                .ToList();
        }

        public IReadOnlyList<UnitRuntimeState> GetSkillTargets(BattleContext context, UnitRuntimeState caster)
        {
            return GetSkillTargets(context, caster, caster.Position);
        }

        public IReadOnlyList<UnitRuntimeState> GetSkillTargets(BattleContext context, UnitRuntimeState caster, GridPosition origin)
        {
            if (context == null ||
                caster == null ||
                caster.HasActed ||
                !caster.CanUseSkill ||
                !caster.HasEnoughMana(ActiveSkillRules.GetManaCost(caster)))
            {
                return new List<UnitRuntimeState>();
            }

            int range = ActiveSkillRules.GetRange(caster);
            IEnumerable<UnitRuntimeState> units = ActiveSkillRules.IsSupportSkill(caster.ActiveSkill)
                ? context.GetUnits(caster.Faction)
                : context.GetUnits(caster.Faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player);

            return units
                .Where(unit => IsValidPrimaryTarget(caster, unit))
                .Where(unit => origin.ManhattanDistance(unit.Position) > 0 || ActiveSkillRules.IsSupportSkill(caster.ActiveSkill))
                .Where(unit => origin.ManhattanDistance(unit.Position) <= range)
                .OrderBy(unit => origin.ManhattanDistance(unit.Position))
                .ThenBy(unit => unit.Id)
                .ToList();
        }

        public SkillResult TryUseSkill(BattleContext context, UnitRuntimeState caster, UnitRuntimeState primaryTarget)
        {
            if (context == null || caster == null || primaryTarget == null || caster.HasActed || !caster.CanUseSkill)
            {
                return null;
            }

            int manaCost = ActiveSkillRules.GetManaCost(caster);
            if (!caster.SpendMana(manaCost))
            {
                return null;
            }

            bool targetIsValid = GetSkillTargets(context, caster).Any(unit => unit.Id == primaryTarget.Id);
            if (!targetIsValid)
            {
                caster.RestoreMana(manaCost);
                return null;
            }

            List<SkillEffectResult> effects = new List<SkillEffectResult>();
            IReadOnlyList<UnitRuntimeState> affectedUnits = GetSkillAffectedUnits(context, caster, primaryTarget);
            if (affectedUnits == null || affectedUnits.Count == 0)
            {
                caster.RestoreMana(manaCost);
                return null;
            }

            switch (caster.ActiveSkill)
            {
                case ActiveSkillType.RoyalAid:
                    ApplyRoyalAid(caster, affectedUnits[0], effects);
                    break;
                case ActiveSkillType.ImperialAid:
                    ApplyImperialAid(caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.GuardOrder:
                    ApplyGuardOrder(caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.KingsBanner:
                    ApplyKingsBanner(caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.PowerStrike:
                    ApplyPowerStrike(context, caster, affectedUnits[0], effects);
                    break;
                case ActiveSkillType.DragonPierce:
                    ApplyDragonPierce(context, caster, affectedUnits[0], effects);
                    break;
                case ActiveSkillType.WhiteHorseRescue:
                    ApplyWhiteHorseRescue(context, caster, affectedUnits[0], effects);
                    break;
                case ActiveSkillType.Volley:
                    ApplyVolley(context, caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.GreenDragonSlash:
                    ApplyGreenDragonSlash(context, caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.AzureDragonSlash:
                    ApplyAzureDragonSlash(context, caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.CrimsonCrescent:
                    ApplyCrimsonCrescent(context, caster, affectedUnits, primaryTarget, effects);
                    break;
                case ActiveSkillType.WarCry:
                    ApplyWarCry(caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.LionWarCry:
                    ApplyLionWarCry(caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.StonewallChallenge:
                    ApplyStonewallChallenge(context, caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.SkyVolley:
                    ApplySkyVolley(context, caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.PinningShot:
                    ApplyPinningShot(context, caster, affectedUnits[0], effects);
                    break;
                case ActiveSkillType.WesternStampede:
                    ApplyWesternStampede(context, caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.StormbreakCharge:
                    ApplyStormbreakCharge(context, caster, affectedUnits, primaryTarget, effects);
                    break;
                case ActiveSkillType.DustDevilSweep:
                    ApplyDustDevilSweep(context, caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.FireStratagem:
                    ApplyFireStratagem(context, caster, affectedUnits, primaryTarget, effects);
                    break;
                case ActiveSkillType.EightTrigramInferno:
                    ApplyEightTrigramInferno(context, caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.FeatherFormation:
                    ApplyFeatherFormation(caster, affectedUnits, effects);
                    break;
                default:
                    caster.RestoreMana(manaCost);
                    return null;
            }

            int totalExperience = 0;
            foreach (SkillEffectResult effect in effects)
            {
                UnitRuntimeState affectedUnit = context.GetUnit(effect.UnitId);
                if (affectedUnit == null)
                {
                    continue;
                }

                if (effect.IsHealing)
                {
                    totalExperience += ExperienceSystem.CalculateHealingReward(
                        caster,
                        affectedUnit,
                        effect.Amount,
                        caster.RegisterContribution("heal:" + affectedUnit.Id));
                }
                else if (effect.Amount > 0)
                {
                    totalExperience += ExperienceSystem.CalculateDamageReward(
                        caster,
                        affectedUnit,
                        effect.Amount,
                        caster.RegisterContribution("damage:" + affectedUnit.Id));

                    if (effect.UnitDied)
                    {
                        totalExperience += ExperienceSystem.CalculateKillBonus(caster, affectedUnit);
                    }
                }

                foreach (SkillStatusApplication statusApplication in effect.AppliedStatuses.Where(status => status.WasApplied))
                {
                    if (affectedUnit.Id == caster.Id)
                    {
                        continue;
                    }

                    totalExperience += ExperienceSystem.CalculateStatusReward(
                        caster,
                        affectedUnit,
                        statusApplication.Type,
                        caster.RegisterContribution("status:" + affectedUnit.Id + ":" + statusApplication.Type));
                }
            }

            int levelsGained = caster.AddExperience(totalExperience);
            caster.SetSkillCooldown(ActiveSkillRules.GetCooldown(caster.ActiveSkill));
            caster.MarkActed();
            context.EvaluateBattleOutcome();
            return new SkillResult(caster.Id, caster.ActiveSkill, primaryTarget.Id, effects, totalExperience, levelsGained);
        }

        public IReadOnlyList<UnitRuntimeState> GetSkillAffectedUnits(BattleContext context, UnitRuntimeState caster, UnitRuntimeState primaryTarget)
        {
            return GetSkillAffectedUnits(context, caster, caster != null ? caster.Position : new GridPosition(0, 0), primaryTarget);
        }

        public IReadOnlyList<UnitRuntimeState> GetSkillAffectedUnits(BattleContext context, UnitRuntimeState caster, GridPosition origin, UnitRuntimeState primaryTarget)
        {
            if (context == null || caster == null || primaryTarget == null || !caster.IsAlive || !primaryTarget.IsAlive)
            {
                return new List<UnitRuntimeState>();
            }

            if (!IsValidPrimaryTarget(caster, primaryTarget))
            {
                return new List<UnitRuntimeState>();
            }

            switch (caster.ActiveSkill)
            {
                case ActiveSkillType.RoyalAid:
                    return new List<UnitRuntimeState> { primaryTarget };
                case ActiveSkillType.ImperialAid:
                case ActiveSkillType.GuardOrder:
                case ActiveSkillType.KingsBanner:
                    return context.GetUnits(caster.Faction)
                        .Where(unit => unit.Id == primaryTarget.Id ||
                                       (unit.Id != primaryTarget.Id && unit.Position.ManhattanDistance(primaryTarget.Position) == 1))
                        .OrderBy(unit => unit.Id == primaryTarget.Id ? 0 : 1)
                        .ThenBy(unit => unit.Id)
                        .ToList();
                case ActiveSkillType.FeatherFormation:
                {
                    List<UnitRuntimeState> formationUnits = context.GetUnits(caster.Faction)
                        .Where(unit => unit.Id == primaryTarget.Id ||
                                       (unit.Id != primaryTarget.Id && unit.Position.ManhattanDistance(primaryTarget.Position) == 1))
                        .OrderBy(unit => unit.Id == primaryTarget.Id ? 0 : 1)
                        .ThenBy(unit => unit.Id)
                        .ToList();
                    UnitRuntimeState lowestHpAlly = context.GetUnits(caster.Faction)
                        .OrderBy(unit => unit.CurrentHp)
                        .ThenBy(unit => unit.Id)
                        .FirstOrDefault();
                    if (lowestHpAlly != null && formationUnits.All(unit => unit.Id != lowestHpAlly.Id))
                    {
                        formationUnits.Add(lowestHpAlly);
                    }

                    return formationUnits;
                }
                case ActiveSkillType.PowerStrike:
                case ActiveSkillType.PinningShot:
                case ActiveSkillType.DragonPierce:
                case ActiveSkillType.WhiteHorseRescue:
                    return new List<UnitRuntimeState> { primaryTarget };
                case ActiveSkillType.Volley:
                case ActiveSkillType.SkyVolley:
                case ActiveSkillType.FireStratagem:
                case ActiveSkillType.EightTrigramInferno:
                    return BattlePreviewCalculator.GetVolleyTargets(context, primaryTarget);
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.AzureDragonSlash:
                case ActiveSkillType.WesternStampede:
                case ActiveSkillType.CrimsonCrescent:
                case ActiveSkillType.StormbreakCharge:
                    return BattlePreviewCalculator.GetGreenDragonSlashTargets(context, origin, primaryTarget);
                case ActiveSkillType.WarCry:
                case ActiveSkillType.LionWarCry:
                case ActiveSkillType.StonewallChallenge:
                case ActiveSkillType.DustDevilSweep:
                    return context.GetUnits(caster.Faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player)
                        .Where(unit => origin.ManhattanDistance(unit.Position) <= ActiveSkillRules.GetRange(caster))
                        .OrderBy(unit => origin.ManhattanDistance(unit.Position))
                        .ThenBy(unit => unit.Id)
                        .ToList();
                default:
                    return new List<UnitRuntimeState>();
            }
        }

        public IReadOnlyList<GridPosition> GetSkillAffectedPositions(BattleContext context, UnitRuntimeState caster, UnitRuntimeState primaryTarget)
        {
            return GetSkillAffectedPositions(context, caster, caster != null ? caster.Position : new GridPosition(0, 0), primaryTarget);
        }

        public IReadOnlyList<GridPosition> GetSkillAffectedPositions(BattleContext context, UnitRuntimeState caster, GridPosition origin, UnitRuntimeState primaryTarget)
        {
            if (context == null || caster == null || primaryTarget == null)
            {
                return new List<GridPosition>();
            }

            switch (caster.ActiveSkill)
            {
                case ActiveSkillType.Volley:
                case ActiveSkillType.SkyVolley:
                case ActiveSkillType.FireStratagem:
                case ActiveSkillType.EightTrigramInferno:
                    return BattlePreviewCalculator.GetVolleyAreaPositions(primaryTarget.Position);
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.AzureDragonSlash:
                case ActiveSkillType.WesternStampede:
                    return BattlePreviewCalculator.GetGreenDragonSlashAreaPositions(origin, primaryTarget.Position);
                default:
                    return GetSkillAffectedUnits(context, caster, origin, primaryTarget)
                        .Select(unit => unit.Position)
                        .Distinct()
                        .OrderBy(position => position.Y)
                        .ThenBy(position => position.X)
                        .ToList();
            }
        }

        private static bool IsValidPrimaryTarget(UnitRuntimeState caster, UnitRuntimeState candidate)
        {
            if (candidate == null || !candidate.IsAlive)
            {
                return false;
            }

            switch (caster.ActiveSkill)
            {
                case ActiveSkillType.RoyalAid:
                case ActiveSkillType.ImperialAid:
                    return candidate.Faction == caster.Faction && candidate.CurrentHp < candidate.MaxHp;
                case ActiveSkillType.GuardOrder:
                case ActiveSkillType.KingsBanner:
                case ActiveSkillType.FeatherFormation:
                    return candidate.Faction == caster.Faction;
                case ActiveSkillType.PowerStrike:
                case ActiveSkillType.DragonPierce:
                case ActiveSkillType.WhiteHorseRescue:
                case ActiveSkillType.PinningShot:
                case ActiveSkillType.Volley:
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.CrimsonCrescent:
                case ActiveSkillType.WarCry:
                case ActiveSkillType.AzureDragonSlash:
                case ActiveSkillType.LionWarCry:
                case ActiveSkillType.StonewallChallenge:
                case ActiveSkillType.SkyVolley:
                case ActiveSkillType.WesternStampede:
                case ActiveSkillType.StormbreakCharge:
                case ActiveSkillType.DustDevilSweep:
                case ActiveSkillType.FireStratagem:
                case ActiveSkillType.EightTrigramInferno:
                    return candidate.Faction != caster.Faction;
                default:
                    return false;
            }
        }

        private static void ApplyRoyalAid(UnitRuntimeState caster, UnitRuntimeState target, ICollection<SkillEffectResult> effects)
        {
            int healedAmount = target.ApplyHealing(BattlePreviewCalculator.EstimateHealing(target, ActiveSkillRules.GetRoyalAidAmount(caster)));
            effects.Add(CreateEffect(
                target,
                healedAmount,
                false,
                true,
                ApplyStatuses(
                    caster,
                    target,
                    (StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration()),
                    (StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()))));
        }

        private static void ApplyImperialAid(UnitRuntimeState caster, IReadOnlyList<UnitRuntimeState> affectedUnits, ICollection<SkillEffectResult> effects)
        {
            if (affectedUnits == null || affectedUnits.Count == 0)
            {
                return;
            }

            ApplyRoyalAid(caster, affectedUnits[0], effects);
            for (int index = 1; index < affectedUnits.Count; index++)
            {
                UnitRuntimeState target = affectedUnits[index];
                int healedAmount = target.ApplyHealing(BattlePreviewCalculator.EstimateHealing(target, ActiveSkillRules.GetImperialAidSplashAmount(caster)));
                List<(StatusEffectType Type, int Duration)> statuses = new List<(StatusEffectType Type, int Duration)>
                {
                    (StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration()),
                };
                if (ActiveSkillRules.IsMastered(caster))
                {
                    statuses.Add((StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()));
                }

                effects.Add(CreateEffect(target, healedAmount, false, true, ApplyStatuses(caster, target, statuses.ToArray())));
            }
        }

        private static void ApplyGuardOrder(UnitRuntimeState caster, IReadOnlyList<UnitRuntimeState> affectedUnits, ICollection<SkillEffectResult> effects)
        {
            if (affectedUnits == null || affectedUnits.Count == 0)
            {
                return;
            }

            for (int index = 0; index < affectedUnits.Count; index++)
            {
                UnitRuntimeState target = affectedUnits[index];
                int healedAmount = index == 0
                    ? target.ApplyHealing(BattlePreviewCalculator.EstimateHealing(target, ActiveSkillRules.GetGuardOrderHealAmount(caster)))
                    : 0;
                List<(StatusEffectType Type, int Duration)> statuses = new List<(StatusEffectType Type, int Duration)>
                {
                    (StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()),
                };
                if (index == 0 && ActiveSkillRules.IsMastered(caster))
                {
                    statuses.Add((StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration()));
                }

                effects.Add(CreateEffect(target, healedAmount, false, healedAmount > 0, ApplyStatuses(caster, target, statuses.ToArray())));
            }
        }

        private static void ApplyKingsBanner(UnitRuntimeState caster, IReadOnlyList<UnitRuntimeState> affectedUnits, ICollection<SkillEffectResult> effects)
        {
            if (affectedUnits == null || affectedUnits.Count == 0)
            {
                return;
            }

            for (int index = 0; index < affectedUnits.Count; index++)
            {
                UnitRuntimeState target = affectedUnits[index];
                int healedAmount = index == 0
                    ? target.ApplyHealing(BattlePreviewCalculator.EstimateHealing(target, ActiveSkillRules.GetKingsBannerHealAmount(caster)))
                    : 0;
                List<(StatusEffectType Type, int Duration)> statuses = new List<(StatusEffectType Type, int Duration)>
                {
                    (StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration()),
                };
                if (index == 0 || ActiveSkillRules.IsMastered(caster))
                {
                    statuses.Add((StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()));
                }

                effects.Add(CreateEffect(target, healedAmount, false, healedAmount > 0, ApplyStatuses(caster, target, statuses.ToArray())));
            }
        }

        private static void ApplyPowerStrike(
            BattleContext context,
            UnitRuntimeState caster,
            UnitRuntimeState primaryTarget,
            ICollection<SkillEffectResult> effects)
        {
            int damage = BattlePreviewCalculator.EstimateAttackDamage(
                context,
                caster,
                caster.Position,
                primaryTarget,
                ActiveSkillRules.GetPowerStrikeBonus(caster));

            primaryTarget.ApplyDamage(damage);
            IReadOnlyList<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
            if (!primaryTarget.IsAlive)
            {
                context.RemoveUnit(primaryTarget.Id);
            }
            else
            {
                statuses = ApplyStatuses(
                    caster,
                    primaryTarget,
                    (StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.PowerStrike, caster)),
                    (StatusEffectType.Bleeding, 1));
            }

            effects.Add(CreateEffect(primaryTarget, damage, !primaryTarget.IsAlive, false, statuses));
        }

        private static void ApplyDragonPierce(
            BattleContext context,
            UnitRuntimeState caster,
            UnitRuntimeState primaryTarget,
            ICollection<SkillEffectResult> effects)
        {
            int damage = BattlePreviewCalculator.EstimateAttackDamage(
                context,
                caster,
                caster.Position,
                primaryTarget,
                ActiveSkillRules.GetDragonPierceBonus(caster),
                ActiveSkillRules.GetDragonPierceIgnoredDefense(caster));

            primaryTarget.ApplyDamage(damage);
            if (!primaryTarget.IsAlive)
            {
                context.RemoveUnit(primaryTarget.Id);
            }

            effects.Add(CreateEffect(primaryTarget, damage, !primaryTarget.IsAlive, false, new List<SkillStatusApplication>()));
            effects.Add(CreateEffect(
                caster,
                0,
                false,
                false,
                ApplyStatuses(caster, caster, (StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()))));

            UnitRuntimeState ally = context.GetUnits(caster.Faction)
                .Where(unit => unit.Id != caster.Id && unit.Position.ManhattanDistance(caster.Position) == 1)
                .OrderBy(unit => unit.CurrentHp)
                .ThenBy(unit => unit.Id)
                .FirstOrDefault();
            if (ally != null)
            {
                effects.Add(CreateEffect(
                    ally,
                    0,
                    false,
                    false,
                    ApplyStatuses(caster, ally, (StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()))));
            }
        }

        private static void ApplyWhiteHorseRescue(
            BattleContext context,
            UnitRuntimeState caster,
            UnitRuntimeState primaryTarget,
            ICollection<SkillEffectResult> effects)
        {
            int damage = BattlePreviewCalculator.EstimateAttackDamage(
                context,
                caster,
                caster.Position,
                primaryTarget,
                ActiveSkillRules.GetWhiteHorseRescueBonus(caster),
                ActiveSkillRules.GetWhiteHorseRescueIgnoredDefense(caster));

            primaryTarget.ApplyDamage(damage);
            if (!primaryTarget.IsAlive)
            {
                context.RemoveUnit(primaryTarget.Id);
            }

            effects.Add(CreateEffect(primaryTarget, damage, !primaryTarget.IsAlive, false, new List<SkillStatusApplication>()));

            List<(StatusEffectType Type, int Duration)> casterStatuses = new List<(StatusEffectType Type, int Duration)>
            {
                (StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()),
            };

            UnitRuntimeState ally = context.GetUnits(caster.Faction)
                .Where(unit => unit.Id != caster.Id && unit.Position.ManhattanDistance(caster.Position) == 1)
                .OrderBy(unit => unit.CurrentHp)
                .ThenBy(unit => unit.Id)
                .FirstOrDefault();
            if (ally == null && ActiveSkillRules.IsMastered(caster))
            {
                casterStatuses.Add((StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration()));
            }

            effects.Add(CreateEffect(caster, 0, false, false, ApplyStatuses(caster, caster, casterStatuses.ToArray())));
            if (ally != null)
            {
                effects.Add(CreateEffect(
                    ally,
                    0,
                    false,
                    false,
                    ApplyStatuses(caster, ally, (StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()))));
            }
        }

        private static void ApplyVolley(
            BattleContext context,
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            ICollection<SkillEffectResult> effects)
        {
            foreach (UnitRuntimeState target in affectedUnits)
            {
                int damage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    caster,
                    caster.Position,
                    target,
                    ActiveSkillRules.GetVolleyBonus(caster));

                target.ApplyDamage(damage);
                IReadOnlyList<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
                if (!target.IsAlive)
                {
                    context.RemoveUnit(target.Id);
                }
                else
                {
                    statuses = ApplyStatuses(
                        caster,
                        target,
                        (StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.Volley, caster)));
                }

                effects.Add(CreateEffect(target, damage, !target.IsAlive, false, statuses));
            }
        }

        private static void ApplySkyVolley(
            BattleContext context,
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            ICollection<SkillEffectResult> effects)
        {
            foreach (UnitRuntimeState target in affectedUnits)
            {
                int damage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    caster,
                    caster.Position,
                    target,
                    ActiveSkillRules.GetSkyVolleyBonus(caster));

                target.ApplyDamage(damage);
                IReadOnlyList<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
                if (!target.IsAlive)
                {
                    context.RemoveUnit(target.Id);
                }
                else
                {
                    statuses = ApplyStatuses(
                        caster,
                        target,
                        (StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.SkyVolley, caster)));
                }

                effects.Add(CreateEffect(target, damage, !target.IsAlive, false, statuses));
            }
        }

        private static void ApplyPinningShot(
            BattleContext context,
            UnitRuntimeState caster,
            UnitRuntimeState primaryTarget,
            ICollection<SkillEffectResult> effects)
        {
            int damage = BattlePreviewCalculator.EstimateAttackDamage(
                context,
                caster,
                caster.Position,
                primaryTarget,
                ActiveSkillRules.GetPinningShotBonus(caster));

            primaryTarget.ApplyDamage(damage);
            IReadOnlyList<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
            if (!primaryTarget.IsAlive)
            {
                context.RemoveUnit(primaryTarget.Id);
            }
            else
            {
                statuses = ApplyStatuses(caster, primaryTarget, (StatusEffectType.Rooted, ActiveSkillRules.GetRootedDuration(caster)));
            }

            effects.Add(CreateEffect(primaryTarget, damage, !primaryTarget.IsAlive, false, statuses));
        }

        private static void ApplyWesternStampede(
            BattleContext context,
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            ICollection<SkillEffectResult> effects)
        {
            foreach (UnitRuntimeState target in affectedUnits)
            {
                int damage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    caster,
                    caster.Position,
                    target,
                    ActiveSkillRules.GetWesternStampedeBonus(caster));

                target.ApplyDamage(damage);
                IReadOnlyList<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
                if (!target.IsAlive)
                {
                    context.RemoveUnit(target.Id);
                }
                else
                {
                    statuses = ApplyStatuses(
                        caster,
                        target,
                        (StatusEffectType.Intimidated, ActiveSkillRules.GetIntimidatedDuration(ActiveSkillType.WesternStampede, caster)));
                }

                effects.Add(CreateEffect(target, damage, !target.IsAlive, false, statuses));
            }
        }

        private static void ApplyGreenDragonSlash(
            BattleContext context,
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            ICollection<SkillEffectResult> effects)
        {
            foreach (UnitRuntimeState target in affectedUnits)
            {
                int damage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    caster,
                    caster.Position,
                    target,
                    ActiveSkillRules.GetGreenDragonSlashBonus(caster));

                target.ApplyDamage(damage);
                IReadOnlyList<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
                if (!target.IsAlive)
                {
                    context.RemoveUnit(target.Id);
                }
                else if (ActiveSkillRules.IsMastered(caster))
                {
                    statuses = ApplyStatuses(
                        caster,
                        target,
                        (StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.GreenDragonSlash, caster)));
                }

                effects.Add(CreateEffect(target, damage, !target.IsAlive, false, statuses));
            }
        }

        private static void ApplyAzureDragonSlash(
            BattleContext context,
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            ICollection<SkillEffectResult> effects)
        {
            foreach (UnitRuntimeState target in affectedUnits)
            {
                int damage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    caster,
                    caster.Position,
                    target,
                    ActiveSkillRules.GetAzureDragonSlashBonus(caster));

                target.ApplyDamage(damage);
                IReadOnlyList<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
                if (!target.IsAlive)
                {
                    context.RemoveUnit(target.Id);
                }
                else
                {
                    statuses = ApplyStatuses(
                        caster,
                        target,
                        (StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.AzureDragonSlash, caster)));
                }

                effects.Add(CreateEffect(target, damage, !target.IsAlive, false, statuses));
            }
        }

        private static void ApplyCrimsonCrescent(
            BattleContext context,
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            UnitRuntimeState primaryTarget,
            ICollection<SkillEffectResult> effects)
        {
            foreach (UnitRuntimeState target in affectedUnits)
            {
                int damage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    caster,
                    caster.Position,
                    target,
                    ActiveSkillRules.GetCrimsonCrescentBonus(caster));

                target.ApplyDamage(damage);
                IReadOnlyList<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
                if (!target.IsAlive)
                {
                    context.RemoveUnit(target.Id);
                }
                else if (target.Id == primaryTarget.Id)
                {
                    statuses = ApplyStatuses(
                        caster,
                        target,
                        (StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.CrimsonCrescent, caster)));
                }
                else
                {
                    List<(StatusEffectType Type, int Duration)> secondaryStatuses = new List<(StatusEffectType Type, int Duration)>
                    {
                        (StatusEffectType.Bleeding, ActiveSkillRules.GetBleedingDuration(ActiveSkillType.CrimsonCrescent, caster)),
                    };
                    if (ActiveSkillRules.IsMastered(caster))
                    {
                        secondaryStatuses.Add((StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.CrimsonCrescent, caster)));
                    }

                    statuses = ApplyStatuses(caster, target, secondaryStatuses.ToArray());
                }

                effects.Add(CreateEffect(target, damage, !target.IsAlive, false, statuses));
            }
        }

        private static void ApplyWarCry(
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            ICollection<SkillEffectResult> effects)
        {
            foreach (UnitRuntimeState target in affectedUnits)
            {
                effects.Add(CreateEffect(
                    target,
                    0,
                    false,
                    false,
                    ApplyStatuses(caster, target, (StatusEffectType.Intimidated, ActiveSkillRules.GetIntimidatedDuration(ActiveSkillType.WarCry, caster)))));
            }
        }

        private static void ApplyLionWarCry(
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            ICollection<SkillEffectResult> effects)
        {
            ApplyWarCry(caster, affectedUnits, effects);

            List<(StatusEffectType Type, int Duration)> casterStatuses = new List<(StatusEffectType Type, int Duration)>
            {
                (StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration()),
            };
            if (ActiveSkillRules.IsMastered(caster))
            {
                casterStatuses.Add((StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()));
            }

            effects.Add(CreateEffect(caster, 0, false, false, ApplyStatuses(caster, caster, casterStatuses.ToArray())));
        }

        private static void ApplyStonewallChallenge(
            BattleContext context,
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            ICollection<SkillEffectResult> effects)
        {
            foreach (UnitRuntimeState target in affectedUnits)
            {
                int damage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    caster,
                    caster.Position,
                    target,
                    ActiveSkillRules.GetStonewallChallengeBonus(caster));
                target.ApplyDamage(damage);
                IReadOnlyList<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
                if (!target.IsAlive)
                {
                    context.RemoveUnit(target.Id);
                }
                else
                {
                    statuses = ApplyStatuses(
                        caster,
                        target,
                        (StatusEffectType.Intimidated, ActiveSkillRules.GetIntimidatedDuration(ActiveSkillType.StonewallChallenge, caster)),
                        (StatusEffectType.Taunted, ActiveSkillRules.GetTauntedDuration(caster)));
                }

                effects.Add(CreateEffect(target, damage, !target.IsAlive, false, statuses));
            }

            effects.Add(CreateEffect(
                caster,
                0,
                false,
                false,
                ApplyStatuses(caster, caster, (StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()))));
        }

        private static void ApplyFireStratagem(
            BattleContext context,
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            UnitRuntimeState primaryTarget,
            ICollection<SkillEffectResult> effects)
        {
            foreach (UnitRuntimeState target in affectedUnits)
            {
                int damage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    caster,
                    caster.Position,
                    target,
                    ActiveSkillRules.GetFireStratagemBonus(caster));

                target.ApplyDamage(damage);
                IReadOnlyList<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
                if (!target.IsAlive)
                {
                    context.RemoveUnit(target.Id);
                }
                else if (target.Id == primaryTarget.Id || ActiveSkillRules.IsMastered(caster))
                {
                    statuses = ApplyStatuses(
                        caster,
                        target,
                        (StatusEffectType.Intimidated, ActiveSkillRules.GetIntimidatedDuration(ActiveSkillType.FireStratagem, caster)));
                }

                effects.Add(CreateEffect(target, damage, !target.IsAlive, false, statuses));
            }
        }

        private static void ApplyEightTrigramInferno(
            BattleContext context,
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            ICollection<SkillEffectResult> effects)
        {
            foreach (UnitRuntimeState target in affectedUnits)
            {
                int damage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    caster,
                    caster.Position,
                    target,
                    ActiveSkillRules.GetEightTrigramInfernoBonus(caster));

                target.ApplyDamage(damage);
                IReadOnlyList<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
                if (!target.IsAlive)
                {
                    context.RemoveUnit(target.Id);
                }
                else
                {
                    statuses = ApplyStatuses(
                        caster,
                        target,
                        (StatusEffectType.Intimidated, ActiveSkillRules.GetIntimidatedDuration(ActiveSkillType.EightTrigramInferno, caster)),
                        (StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration(ActiveSkillType.EightTrigramInferno, caster)));
                }

                effects.Add(CreateEffect(target, damage, !target.IsAlive, false, statuses));
            }
        }

        private static void ApplyFeatherFormation(
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            ICollection<SkillEffectResult> effects)
        {
            if (affectedUnits == null || affectedUnits.Count == 0)
            {
                return;
            }

            UnitRuntimeState healTarget = affectedUnits
                .OrderBy(unit => unit.CurrentHp)
                .ThenBy(unit => unit.Id)
                .FirstOrDefault();

            for (int index = 0; index < affectedUnits.Count; index++)
            {
                UnitRuntimeState target = affectedUnits[index];
                int healedAmount = healTarget != null && target.Id == healTarget.Id
                    ? target.ApplyHealing(BattlePreviewCalculator.EstimateHealing(target, ActiveSkillRules.GetFeatherFormationHealAmount(caster)))
                    : 0;
                List<(StatusEffectType Type, int Duration)> statuses = new List<(StatusEffectType Type, int Duration)>
                {
                    (StatusEffectType.Guarded, ActiveSkillRules.GetGuardedDuration()),
                };
                if (index == 0 || ActiveSkillRules.IsMastered(caster))
                {
                    statuses.Add((StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration()));
                }

                effects.Add(CreateEffect(target, healedAmount, false, healedAmount > 0, ApplyStatuses(caster, target, statuses.ToArray())));
            }
        }

        private static void ApplyStormbreakCharge(
            BattleContext context,
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            UnitRuntimeState primaryTarget,
            ICollection<SkillEffectResult> effects)
        {
            foreach (UnitRuntimeState target in affectedUnits)
            {
                int damage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    caster,
                    caster.Position,
                    target,
                    ActiveSkillRules.GetStormbreakChargeBonus(caster));
                target.ApplyDamage(damage);
                IReadOnlyList<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
                if (!target.IsAlive)
                {
                    context.RemoveUnit(target.Id);
                }
                else
                {
                    List<(StatusEffectType Type, int Duration)> appliedStatuses = new List<(StatusEffectType Type, int Duration)>
                    {
                        (StatusEffectType.Intimidated, ActiveSkillRules.GetIntimidatedDuration(ActiveSkillType.StormbreakCharge, caster)),
                    };
                    if (target.Id == primaryTarget.Id || ActiveSkillRules.IsMastered(caster))
                    {
                        appliedStatuses.Add((StatusEffectType.Bleeding, ActiveSkillRules.GetBleedingDuration(ActiveSkillType.StormbreakCharge, caster)));
                    }

                    statuses = ApplyStatuses(caster, target, appliedStatuses.ToArray());
                }

                effects.Add(CreateEffect(target, damage, !target.IsAlive, false, statuses));
            }
        }

        private static void ApplyDustDevilSweep(
            BattleContext context,
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            ICollection<SkillEffectResult> effects)
        {
            int hits = 0;
            foreach (UnitRuntimeState target in affectedUnits)
            {
                int damage = BattlePreviewCalculator.EstimateAttackDamage(
                    context,
                    caster,
                    caster.Position,
                    target,
                    ActiveSkillRules.GetDustDevilSweepBonus(caster));
                target.ApplyDamage(damage);
                IReadOnlyList<SkillStatusApplication> statuses = new List<SkillStatusApplication>();
                if (!target.IsAlive)
                {
                    context.RemoveUnit(target.Id);
                }
                else
                {
                    hits++;
                    statuses = ApplyStatuses(
                        caster,
                        target,
                        (StatusEffectType.Bleeding, ActiveSkillRules.GetBleedingDuration(ActiveSkillType.DustDevilSweep, caster)));
                }

                effects.Add(CreateEffect(target, damage, !target.IsAlive, false, statuses));
            }

            if (hits >= 2)
            {
                effects.Add(CreateEffect(
                    caster,
                    0,
                    false,
                    false,
                    ApplyStatuses(caster, caster, (StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration()))));
            }
        }

        private static SkillEffectResult CreateEffect(
            UnitRuntimeState target,
            int amount,
            bool unitDied,
            bool isHealing,
            IReadOnlyList<SkillStatusApplication> statuses)
        {
            return new SkillEffectResult(
                target.Id,
                amount,
                target.CurrentHp,
                unitDied,
                isHealing,
                statuses);
        }

        private static IReadOnlyList<SkillStatusApplication> ApplyStatuses(
            UnitRuntimeState target,
            params (StatusEffectType Type, int Duration)[] statuses)
        {
            return ApplyStatuses(null, target, statuses);
        }

        private static IReadOnlyList<SkillStatusApplication> ApplyStatuses(
            UnitRuntimeState source,
            UnitRuntimeState target,
            params (StatusEffectType Type, int Duration)[] statuses)
        {
            List<SkillStatusApplication> appliedStatuses = new List<SkillStatusApplication>();
            if (target == null || statuses == null)
            {
                return appliedStatuses;
            }

            foreach ((StatusEffectType type, int duration) in statuses)
            {
                if (type == StatusEffectType.None || duration <= 0)
                {
                    continue;
                }

                int adjustedDuration = duration + EquipmentEffectRules.GetStatusDurationBonus(source);
                bool wasApplied = target.AddOrRefreshStatus(type, adjustedDuration);
                appliedStatuses.Add(new SkillStatusApplication(type, adjustedDuration, wasApplied));
            }

            return appliedStatuses;
        }
    }
}
