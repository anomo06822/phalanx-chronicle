using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class UnitRuntimeState
    {
        private readonly List<StatusEffectState> statusEffects = new List<StatusEffectState>();

        public UnitRuntimeState(UnitDefinitionData definition, GridPosition startPosition)
        {
            Id = definition.Id;
            DisplayName = definition.DisplayName;
            DisplayNameKey = definition.DisplayNameKey;
            Faction = definition.Faction;
            Role = definition.Role;
            RoleNameKey = definition.RoleNameKey;
            PassiveSkill = definition.PassiveSkill;
            PassiveSkillNameKey = definition.PassiveSkillNameKey;
            PassiveSkillDescriptionKey = definition.PassiveSkillDescriptionKey;
            ActiveSkill = definition.ActiveSkill;
            ActiveSkillNameKey = definition.ActiveSkillNameKey;
            ActiveSkillDescriptionKey = definition.ActiveSkillDescriptionKey;
            MaxHp = definition.MaxHp;
            CurrentHp = definition.MaxHp;
            Attack = definition.Attack;
            Defense = definition.Defense;
            MoveRange = definition.MoveRange;
            AttackRange = definition.AttackRange;
            MaxMana = definition.MaxMana;
            CurrentMana = definition.MaxMana;
            Position = startPosition;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string DisplayNameKey { get; }

        public UnitFaction Faction { get; }

        public UnitRole Role { get; }

        public string RoleNameKey { get; }

        public PassiveSkillType PassiveSkill { get; }

        public string PassiveSkillNameKey { get; }

        public string PassiveSkillDescriptionKey { get; }

        public ActiveSkillType ActiveSkill { get; }

        public string ActiveSkillNameKey { get; }

        public string ActiveSkillDescriptionKey { get; }

        public int MaxHp { get; }

        public int CurrentHp { get; private set; }

        public int Attack { get; }

        public int Defense { get; }

        public int MoveRange { get; }

        public int AttackRange { get; }

        public int MaxMana { get; }

        public int CurrentMana { get; private set; }

        public GridPosition Position { get; private set; }

        public bool HasActed { get; private set; }

        public bool HasMovedThisTurn { get; private set; }

        public int CurrentSkillCooldown { get; private set; }

        public IReadOnlyList<StatusEffectState> StatusEffects => statusEffects;

        public bool IsAlive => CurrentHp > 0;

        public bool CanUseSkill => ActiveSkill != ActiveSkillType.None && CurrentSkillCooldown == 0;

        public void MoveTo(GridPosition position)
        {
            Position = position;
            HasMovedThisTurn = true;
        }

        public void UndoMoveTo(GridPosition position)
        {
            Position = position;
            HasMovedThisTurn = false;
        }

        public void ResetTurn()
        {
            HasActed = false;
            HasMovedThisTurn = false;
        }

        public bool HasEnoughMana(int manaAmount)
        {
            return manaAmount <= 0 || CurrentMana >= manaAmount;
        }

        public bool SpendMana(int amount)
        {
            if (!HasEnoughMana(amount))
            {
                return false;
            }

            if (amount > 0)
            {
                CurrentMana -= amount;
            }

            return true;
        }

        public void RestoreMana(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentMana += amount;
            if (CurrentMana > MaxMana)
            {
                CurrentMana = MaxMana;
            }
        }

        public void MarkActed()
        {
            HasActed = true;
        }

        public void SetSkillCooldown(int cooldown)
        {
            CurrentSkillCooldown = cooldown > 0 ? cooldown + 1 : 0;
        }

        public void ApplyDamage(int damage)
        {
            CurrentHp -= damage;
            if (CurrentHp < 0)
            {
                CurrentHp = 0;
            }
        }

        public int ApplyHealing(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return 0;
            }

            int before = CurrentHp;
            CurrentHp += amount;
            if (CurrentHp > MaxHp)
            {
                CurrentHp = MaxHp;
            }

            return CurrentHp - before;
        }

        public void AddOrRefreshStatus(StatusEffectType type, int duration)
        {
            if (type == StatusEffectType.None || duration <= 0 || !IsAlive)
            {
                return;
            }

            StatusEffectState existing = statusEffects.FirstOrDefault(effect => effect.Type == type);
            if (existing != null)
            {
                existing.Refresh(duration);
                return;
            }

            statusEffects.Add(new StatusEffectState(type, duration));
        }

        public bool HasStatus(StatusEffectType type)
        {
            return statusEffects.Any(effect => effect.Type == type && effect.RemainingOwnTurnEnds > 0);
        }

        public void AdvanceOwnTurnEnd()
        {
            if (CurrentSkillCooldown > 0)
            {
                CurrentSkillCooldown--;
            }

            for (int index = statusEffects.Count - 1; index >= 0; index--)
            {
                statusEffects[index].AdvanceOwnTurnEnd();
                if (statusEffects[index].RemainingOwnTurnEnds <= 0)
                {
                    statusEffects.RemoveAt(index);
                }
            }
        }
    }
}
