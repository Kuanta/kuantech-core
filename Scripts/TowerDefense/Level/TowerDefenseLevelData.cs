using System;
using System.Collections.Generic;
using Kuantech.Core.Utils;

namespace Kuantech.TowerDefense
{
    [Serializable]
    public struct WaveEntry
    {
        public int SpawnableIndex; //Index of the spawnable
        public int SpawnerIndex; //Which spawner to spawn from
    }

    [Serializable]
    public struct WaveData
    {
        public int EnemyCount;
        public WeightedProbabilityArray<int> EnemyProbabilities;
        public List<WaveEntry> WaveEntries; //Predefined
        public float WaveSpawnDelay; //Delay between each spawn in the wave
    }
    
    [Serializable]
    public class TowerDefenseLevelData
    {
        public float TowerHealth;
        public int StartingGold;
        public List<WaveData> WaveData;
    }
}