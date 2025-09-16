using System;
using System.Collections.Generic;

namespace Kuantech.TowerDefense
{
    [Serializable]
    public struct WaveEntry
    {
        public int SpawnableIndex; //Index of the spawnable
        public int SpawnerIndex; //Which spawner to spawn from
        public int Amount;
    }

    [Serializable]
    public struct EnemyProbabilityData
    {
        public List<int> Values;
        public List<float> Weights;
    }
    
    [Serializable]
    public class WaveData
    {
        public int EnemyFactionId = 1; //Faction ID of the enemies in this wave
        public int WaveActorsLevel = 0;
        public List<WaveEntry> WaveEntries; //Predefined
        public int MaxEnemyCount = -1; //If greater than -1, limits the number of enemies in the wave      
        public int GeneratedEnemyCount;
        public EnemyProbabilityData EnemyProbabilities;
        public float WaveSpawnDelay; //Delay between each spawn in the wave
        
        public int GetEnemyCount()
        {
            int total = GeneratedEnemyCount;
            foreach (var waveEntry in WaveEntries)
            {
                total += waveEntry.Amount;
            }
            return total;
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