using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class BattleThreatProjection
    {
        public BattleThreatProjection(int threateningEnemyCount, int maxProjectedDamage, IReadOnlyList<string> threateningEnemyIds)
        {
            ThreateningEnemyCount = threateningEnemyCount;
            MaxProjectedDamage = maxProjectedDamage;
            ThreateningEnemyIds = threateningEnemyIds ?? new List<string>();
        }

        public int ThreateningEnemyCount { get; }

        public int MaxProjectedDamage { get; }

        public IReadOnlyList<string> ThreateningEnemyIds { get; }

        public bool IsExposed => ThreateningEnemyCount > 0;

        public bool IsLethalRisk(int currentHp)
        {
            return currentHp > 0 && MaxProjectedDamage >= currentHp;
        }

        public BattleThreatSummary ToSummary()
        {
            return new BattleThreatSummary(ThreateningEnemyCount, MaxProjectedDamage, ThreateningEnemyIds.ToList());
        }
    }
}
