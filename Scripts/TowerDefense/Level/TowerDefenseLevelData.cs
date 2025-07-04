using System;
using System.Collections.Generic;
using Kuantech.Core.Utils;
using UnityEngine.Serialization;

namespace Kuantech.TowerDefense
{
    [Serializable]
    public struct WaveEntry
    {
        public int SpawnableIndex; //Index of the spawnable
        public int SpawnerIndex; //Which spawner to spawn from
    }

    [Serializable]
    public class WaveData
    {
        public int GeneratedEnemyCount;
        public WeightedProbabilityArray<int> EnemyProbabilities;
        public List<WaveEntry> WaveEntries; //Predefined
        public float WaveSpawnDelay; //Delay between each spawn in the wave

        public int GetEnemyCount()
        {
            return GeneratedEnemyCount + WaveEntries.Count;
        }
    }
    
    [Serializable]
    public class TowerDefenseLevelData
    {
        public float TowerHealth;
        public int StartingGold;
        public List<WaveData> WaveData;
    }
}