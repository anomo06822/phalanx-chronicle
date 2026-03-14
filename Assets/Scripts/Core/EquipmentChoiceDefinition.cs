using System;

namespace PhalanxChronicle.Core
{
    public enum EquipmentChoiceStateKind
    {
        Current = 0,
        Available = 1,
        EquippedByOther = 2,
    }

    [Serializable]
    public sealed class EquipmentChoiceDefinition
    {
        public EquipmentChoiceDefinition(
            string itemId,
            ItemCategory category,
            EquipmentChoiceStateKind stateKind,
            string equippedByUnitId = "")
        {
            ItemId = itemId ?? string.Empty;
            Category = category;
            StateKind = stateKind;
            EquippedByUnitId = equippedByUnitId ?? string.Empty;
        }

        public string ItemId { get; }

        public ItemCategory Category { get; }

        public EquipmentChoiceStateKind StateKind { get; }

        public string EquippedByUnitId { get; }
    }
}
