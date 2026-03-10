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
            if (context == null || caster == null || caster.HasActed || !caster.CanUseSkill)
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

            bool targetIsValid = GetSkillTargets(context, caster).Any(unit => unit.Id == primaryTarget.Id);
            if (!targetIsValid)
            {
                return null;
            }

            List<SkillEffectResult> effects = new List<SkillEffectResult>();
            switch (caster.ActiveSkill)
            {
                case ActiveSkillType.RoyalAid:
                    ApplyRoyalAid(primaryTarget, effects);
                    break;
                case ActiveSkillType.PowerStrike:
                    ApplyPowerStrike(context, caster, primaryTarget, effects);
                    break;
                case ActiveSkillType.Volley:
                    ApplyVolley(context, caster, primaryTarget, effects);
                    break;
                default:
                    return null;
            }

            caster.SetSkillCooldown(ActiveSkillRules.GetCooldown(caster.ActiveSkill));
            caster.MarkActed();
            context.EvaluateBattleOutcome();
            return new SkillResult(caster.Id, caster.ActiveSkill, primaryTarget.Id, effects);
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
            UnitRuntimeState primaryTarget,
            ICollection<SkillEffectResult> effects)
        {
            IReadOnlyList<UnitRuntimeState> affectedUnits = BattlePreviewCalculator.GetVolleyTargets(context, primaryTarget);

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
    }
}
