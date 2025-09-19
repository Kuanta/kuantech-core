using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Data;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    
    [Serializable]
    public class TowerDefenseLevelsCollection
    {
        public List<TowerDefenseLevelData> TowerDefenseLevels;
        
        public virtual TowerDefenseLevelData GetLevelData(int worldNumber, int levelNumber)
        {
            foreach (var level in TowerDefenseLevels)
            {
                if (level.WorldIndex == worldNumber && level.LevelIndex == levelNumber)
                {
                    return level;
                }
            }
            return null;
        }
    }
    public class TowerDefenseLevelDataManager : SubManager
    {
        [SerializeField] private WaveGeneratorConfig WaveGeneratorConfig;
        
        public static TowerDefenseLevelData GetLevelData<T>(int worldNumber, int levelNumber) where T : TowerDefenseLevelsCollection
        {
            TowerDefenseLevelsCollection levelsData = JsonDataManager.GetData<T>();
            if (levelsData == null)
            {
                return null;
            }
            TowerDefenseLevelData levelData = levelsData.GetLevelData(worldNumber, levelNumber);
            if (levelData == null)
            {
                //Try to get default
                levelData = levelsData.GetLevelData(-1, -1);
            }

            return levelData;
        }

        public static WaveGeneratorConfig GetWaveGeneratorConfig()
        {
            var ctx = TowerDefenseLevelDataManager.GetContext<TowerDefenseLevelDataManager>();
            return ctx.WaveGeneratorConfig;
        }
    }
}