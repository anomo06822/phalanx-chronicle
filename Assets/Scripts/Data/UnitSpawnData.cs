using PhalanxChronicle.Core;
using UnityEngine;

namespace PhalanxChronicle.Data
{
    [System.Serializable]
    public sealed class UnitSpawnData
    {
        [SerializeField] private UnitDefinition definition;
        [SerializeField] private Vector2Int position;

        public UnitSpawnData(UnitDefinition definition, Vector2Int position)
        {
            this.definition = definition;
            this.position = position;
        }

        public PhalanxChronicle.Core.UnitSpawnData ToData()
        {
            return new PhalanxChronicle.Core.UnitSpawnData(definition.ToData(), new GridPosition(position.x, position.y));
        }
    }
}
