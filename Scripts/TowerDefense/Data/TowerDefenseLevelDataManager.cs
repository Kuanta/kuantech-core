using Kuantech.ConveyorDefense;
using Kuantech.Core;
using Kuantech.Core.Data;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class TowerDefenseLevelDataManager : SubManager
    {
        [SerializeField] private WaveGeneratorConfig WaveGeneratorConfig;

        public static ConveyorDefenseLevelData GetLevelData(int worldNumber, int levelNumber)
        {
            ConveyorDefenseLevels levelsData = JsonDataManager.GetData<ConveyorDefenseLevels>();
            if (levelsData == null)
            {
                return null;
            }
            ConveyorDefenseLevelData levelData = levelsData.GetLevelData(worldNumber, levelNumber);
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