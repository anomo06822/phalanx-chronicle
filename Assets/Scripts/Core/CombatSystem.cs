using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class CombatSystem
    {
        private readonly RangeCalculator rangeCalculator;

        public CombatSystem(RangeCalculator rangeCalculator)
        {
            this.rangeCalculator = rangeCalculator;
        }

        public CombatResult TryAttack(BattleContext context, UnitRuntimeState attacker, UnitRuntimeState defender)
        {
            if (attacker == null || defender == null)
            {
                return null;
            }

            if (!attacker.IsAlive || !defender.IsAlive || attacker.HasActed)
            {
                return null;
            }

            if (attacker.Faction == defender.Faction)
            {
                return null;
            }

            bool canAttack = rangeCalculator
                .GetAttackableTargets(context, attacker)
                .Any(target => target.Id == defender.Id);

            if (!canAttack)
            {
                return null;
            }

            int damage = BattlePreviewCalculator.EstimateAttackDamage(context, attacker, attacker.Position, defender);

            defender.ApplyDamage(damage);
            attacker.MarkActed();

            if (!defender.IsAlive)
            {
                context.RemoveUnit(defender.Id);
            }

            int experience = ExperienceSystem.CalculateDamageReward(
                attacker,
                defender,
                damage,
                attacker.RegisterContribution("damage:" + defender.Id));
            if (!defender.IsAlive)
            {
                experience += ExperienceSystem.CalculateKillBonus(attacker, defender);
            }

            int levelsGained = attacker.AddExperience(experience);
            context.EvaluateBattleOutcome();

            return new CombatResult(
                attacker.Id,
                defender.Id,
                damage,
                defender.CurrentHp,
                !defender.IsAlive,
                experience,
                levelsGained);
        }
    }
}
