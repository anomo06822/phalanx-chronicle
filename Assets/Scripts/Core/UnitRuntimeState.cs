using System;
using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class UnitRuntimeState
    {
        private readonly List<StatusEffectState> statusEffects = new List<StatusEffectState>();
        private readonly Dictionary<string, int> contributionCounts = new Dictionary<string, int>();
        private readonly List<string> unlockedSkillIds = new List<string>();
        private readonly List<string> traitSlots = new List<string>();

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
            ClassId = definition.ClassId;
            GrowthProfileId = definition.GrowthProfileId;
            AiProfile = definition.AiProfile;
            EquipmentLoadout = definition.EquipmentLoadout ?? EquipmentLoadout.Empty;
            BondState = definition.BondState != null
                ? new BondState(definition.BondState.SupportLevel, definition.BondState.SharedBattles)
                : new BondState();
            MaxHp = definition.MaxHp;
            Attack = definition.Attack;
            Defense = definition.Defense;
            MoveRange = definition.MoveRange;
            AttackRange = definition.AttackRange;
            MaxMana = definition.MaxMana;
            Level = Math.Min(ExperienceSystem.MaxLevel, definition.StartingLevel < 1 ? 1 : definition.StartingLevel);
            CurrentExp = definition.StartingExp < 0 ? 0 : definition.StartingExp;
            if (!definition.ProgressionResolved)
            {
                NormalizeStartingProgression();
            }
            else if (Level >= ExperienceSystem.MaxLevel)
            {
                CurrentExp = Math.Min(CurrentExp, ExperienceSystem.GetRequiredExpForLevel(Level) - 1);
            }

            BattleStartLevel = Level;
            CurrentHp = MaxHp;
            CurrentMana = MaxMana;
            Position = startPosition;
            RefreshProgressionState();
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

        public int MaxHp { get; private set; }

        public int CurrentHp { get; private set; }

        public int Attack { get; private set; }

        public int Defense { get; private set; }

        public int MoveRange { get; private set; }

        public int AttackRange { get; private set; }

        public int MaxMana { get; private set; }

        public int CurrentMana { get; private set; }

        public string ClassId { get; }

        public string GrowthProfileId { get; }

        public AiProfileType AiProfile { get; }

        public EquipmentLoadout EquipmentLoadout { get; }

        public BondState BondState { get; }

        public int Level { get; private set; }

        public int BattleStartLevel { get; }

        public int CurrentExp { get; private set; }

        public int NextLevelExp => ExperienceSystem.GetRequiredExpForLevel(Level);

        public int BattleExpEarned { get; private set; }

        public int BattleLevelUpsGained { get; private set; }

        public bool AdvancedSkillUnlocked { get; private set; }

        public bool PromotionReady { get; private set; }

        public bool SignaturePassiveUnlocked { get; private set; }

        public IReadOnlyList<string> UnlockedSkillIds => unlockedSkillIds;

        public IReadOnlyList<string> TraitSlots => traitSlots;

        public GridPosition Position { get; private set; }

        public bool HasActed { get; private set; }

        public bool HasMovedThisTurn { get; private set; }

        public int CurrentSkillCooldown { get; private set; }

        public IReadOnlyList<StatusEffectState> StatusEffects => statusEffects;

        public bool IsAlive => CurrentHp > 0;

        public bool CanUseSkill => ActiveSkill != ActiveSkillType.None;

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
            contributionCounts.Clear();
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

        public int AddExperience(int amount)
        {
            return AddExperienceInternal(amount, true);
        }

        public int AddBonusExperience(int amount)
        {
            return AddExperienceInternal(amount, false);
        }

        public int RegisterContribution(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return 0;
            }

            int repeatCount = contributionCounts.TryGetValue(key, out int currentCount) ? currentCount : 0;
            contributionCounts[key] = repeatCount + 1;
            return repeatCount;
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

        public bool AddOrRefreshStatus(StatusEffectType type, int duration)
        {
            if (type == StatusEffectType.None || duration <= 0 || !IsAlive)
            {
                return false;
            }

            StatusEffectState existing = statusEffects.FirstOrDefault(effect => effect.Type == type);
            if (existing != null)
            {
                return existing.Refresh(duration);
            }

            statusEffects.Add(new StatusEffectState(type, duration));
            return true;
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

        private void NormalizeStartingProgression()
        {
            for (int level = 2; level <= Level; level++)
            {
                ApplyGrowthForLevel(level, false);
            }

            while (Level < ExperienceSystem.MaxLevel)
            {
                int requiredExp = ExperienceSystem.GetRequiredExpForLevel(Level);
                if (CurrentExp < requiredExp)
                {
                    break;
                }

                CurrentExp -= requiredExp;
                Level++;
                ApplyGrowthForLevel(Level, false);
            }
        }

        private void ApplyGrowthForLevel(int level, bool restoreResources)
        {
            GrowthProfileDefinition growthProfile = GrowthProfileCatalog.Get(GrowthProfileId);
            int hpGain = growthProfile.HpPerLevel;
            int attackGain = growthProfile.GetAttackGain(level);
            int defenseGain = growthProfile.GetDefenseGain(level);
            int manaGain = growthProfile.GetManaGain(level);

            MaxHp += hpGain;
            Attack += attackGain;
            Defense += defenseGain;
            MaxMana += manaGain;

            if (restoreResources)
            {
                CurrentHp = Math.Min(MaxHp, CurrentHp + hpGain);
                CurrentMana = Math.Min(MaxMana, CurrentMana + manaGain);
            }
        }

        private void RefreshProgressionState()
        {
            GrowthProfileDefinition growthProfile = GrowthProfileCatalog.Get(GrowthProfileId);
            AdvancedSkillUnlocked = Level >= growthProfile.AdvancedSkillUnlockLevel;
            PromotionReady = Level >= growthProfile.PromotionUnlockLevel;
            SignaturePassiveUnlocked = Level >= growthProfile.SignaturePassiveUnlockLevel;

            unlockedSkillIds.Clear();
            unlockedSkillIds.Add(ActiveSkill.ToString());
            if (AdvancedSkillUnlocked)
            {
                unlockedSkillIds.Add(ActiveSkill + ".mastery");
            }

            int traitSlotCount = growthProfile.GetTraitSlotCount(Level);
            while (traitSlots.Count < traitSlotCount)
            {
                traitSlots.Add(string.Empty);
            }

            if (traitSlots.Count > traitSlotCount)
            {
                traitSlots.RemoveRange(traitSlotCount, traitSlots.Count - traitSlotCount);
            }
        }

        private int AddExperienceInternal(int amount, bool trackBattleExp)
        {
            if (amount <= 0)
            {
                return 0;
            }

            if (trackBattleExp)
            {
                BattleExpEarned += amount;
            }

            if (Level >= ExperienceSystem.MaxLevel)
            {
                return 0;
            }

            CurrentExp += amount;
            int levelsGained = 0;
            while (Level < ExperienceSystem.MaxLevel)
            {
                int requiredExp = ExperienceSystem.GetRequiredExpForLevel(Level);
                if (CurrentExp < requiredExp)
                {
                    break;
                }

                CurrentExp -= requiredExp;
                Level++;
                levelsGained++;
                BattleLevelUpsGained++;
                ApplyGrowthForLevel(Level, true);
            }

            if (Level >= ExperienceSystem.MaxLevel)
            {
                CurrentExp = Math.Min(CurrentExp, ExperienceSystem.GetRequiredExpForLevel(Level) - 1);
            }

            RefreshProgressionState();
            return levelsGained;
        }
    }
}
