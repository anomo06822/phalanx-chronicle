using System;

namespace PhalanxChronicle.Core
{
    [Serializable]
    public sealed class UnitSpawnData
    {
        public UnitSpawnData(UnitDefinitionData definition, GridPosition startPosition)
        {
            Definition = definition;
            StartPosition = startPosition;
        }

        public UnitDefinitionData Definition { get; }

        public GridPosition StartPosition { get; }
    }
}
