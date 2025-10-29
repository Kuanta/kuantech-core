using System;
using System.Collections.Generic;

namespace Kuantech.TowerDefense
{
    [Serializable]
    public class EnemyMeta
    {
        public int SpawnableIndex;
        public int Cost = 10;
        public float ConcurrencyWeight = 1f;
        public List<EnemyTagAsset> Tags = new List<EnemyTagAsset>();
    }
}