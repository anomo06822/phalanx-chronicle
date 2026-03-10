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
                    ApplyRoyalAid(affectedUnits.First(), effects);
                    break;
                case ActiveSkillType.PowerStrike:
                    ApplyPowerStrike(context, caster, affectedUnits.First(), effects);
                    break;
                case ActiveSkillType.Volley:
                    ApplyVolley(context, caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.GreenDragonSlash:
                    ApplyGreenDragonSlash(context, caster, affectedUnits, effects);
                    break;
                case ActiveSkillType.WarCry:
                    ApplyWarCry(context, caster, affectedUnits, effects);
                    break;
                default:
                    return null;
            }

            caster.SetSkillCooldown(ActiveSkillRules.GetCooldown(caster.ActiveSkill));
            caster.MarkActed();
            context.EvaluateBattleOutcome();
            return new SkillResult(caster.Id, caster.ActiveSkill, primaryTarget.Id, effects);
        }

        public IReadOnlyList<UnitRuntimeState> GetSkillAffectedUnits(BattleContext context, UnitRuntimeState caster, UnitRuntimeState primaryTarget)
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
                case ActiveSkillType.PowerStrike:
                    return new List<UnitRuntimeState> { primaryTarget };
                case ActiveSkillType.Volley:
                    return BattlePreviewCalculator.GetVolleyTargets(context, primaryTarget);
                case ActiveSkillType.GreenDragonSlash:
                    return BattlePreviewCalculator.GetGreenDragonSlashTargets(context, caster.Position, primaryTarget);
                case ActiveSkillType.WarCry:
                    return context.GetUnits(caster.Faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player)
                        .Where(unit => caster.Position.ManhattanDistance(unit.Position) <= ActiveSkillRules.GetRange(caster))
                        .OrderBy(unit => caster.Position.ManhattanDistance(unit.Position))
                        .ThenBy(unit => unit.Id)
                        .ToList();
                default:
                    return new List<UnitRuntimeState>();
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
                    return candidate.Faction == caster.Faction && candidate.CurrentHp < candidate.MaxHp;
                case ActiveSkillType.PowerStrike:
                case ActiveSkillType.Volley:
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.WarCry:
                    return candidate.Faction != caster.Faction;
                default:
                    return false;
            }
        }

        private static void ApplyRoyalAid(UnitRuntimeState target, ICollection<SkillEffectResult> effects)
        {
            int healedAmount = target.ApplyHealing(BattlePreviewCalculator.EstimateHealing(target, ActiveSkillRules.GetRoyalAidAmount()));
            target.AddOrRefreshStatus(StatusEffectType.Inspired, ActiveSkillRules.GetInspiredDuration());
            effects.Add(new SkillEffectResult(target.Id, healedAmount, target.CurrentHp, false, true, StatusEffectType.Inspired));
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
                ActiveSkillRules.GetPowerStrikeBonus());

            primaryTarget.ApplyDamage(damage);
            if (!primaryTarget.IsAlive)
            {
                context.RemoveUnit(primaryTarget.Id);
            }
            else
            {
                primaryTarget.AddOrRefreshStatus(StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration());
            }

            effects.Add(new SkillEffectResult(
                primaryTarget.Id,
                damage,
                primaryTarget.CurrentHp,
                !primaryTarget.IsAlive,
                false,
                primaryTarget.IsAlive ? StatusEffectType.ShatteredArmor : StatusEffectType.None));
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
                    ActiveSkillRules.GetVolleyBonus());

                target.ApplyDamage(damage);
                if (!target.IsAlive)
                {
                    context.RemoveUnit(target.Id);
                }
                else
                {
                    target.AddOrRefreshStatus(StatusEffectType.ShatteredArmor, ActiveSkillRules.GetShatteredArmorDuration());
                }

                effects.Add(new SkillEffectResult(
                    target.Id,
                    damage,
                    target.CurrentHp,
                    !target.IsAlive,
                    false,
                    target.IsAlive ? StatusEffectType.ShatteredArmor : StatusEffectType.None));
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
                    ActiveSkillRules.GetGreenDragonSlashBonus());

                target.ApplyDamage(damage);
                if (!target.IsAlive)
                {
                    context.RemoveUnit(target.Id);
                }

                effects.Add(new SkillEffectResult(
                    target.Id,
                    damage,
                    target.CurrentHp,
                    !target.IsAlive,
                    false,
                    StatusEffectType.None));
            }
        }

        private static void ApplyWarCry(
            BattleContext context,
            UnitRuntimeState caster,
            IReadOnlyList<UnitRuntimeState> affectedUnits,
            ICollection<SkillEffectResult> effects)
        {
            foreach (UnitRuntimeState target in affectedUnits)
            {
                target.AddOrRefreshStatus(StatusEffectType.Intimidated, ActiveSkillRules.GetIntimidatedDuration());
                effects.Add(new SkillEffectResult(
                    target.Id,
                    0,
                    target.CurrentHp,
                    false,
                    false,
                    StatusEffectType.Intimidated));
            }
        }
    }
}
