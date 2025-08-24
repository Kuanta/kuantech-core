using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    [CreateAssetMenu(fileName = "World Data Asset", menuName = "Kuantech/World Data Asset")]
    public class WorldDataAsset : MetadataAsset
    {
        public List<Level> Levels;

        public Level GetLevelPrefab(int levelIndex)
        {
            if (Levels.IsNullOrEmpty()) return null;
            return Levels[levelIndex % Levels.Count];
        }
        
        /// <summary>
        /// Returns the corresponding power level
        /// </summary>
        /// <param name="worldIndex"></param>
        /// <param name="levelIndex"></param>
        /// <returns></returns>
        public int GetPowerLevel(int worldIndex, int levelIndex)
        {
            int rankUpPerWorld = ConfigManager.GetIntConfig("RankUpPerWorld");
            if (rankUpPerWorld == 0) return worldIndex;
            int powerLevel = Mathf.FloorToInt((float)worldIndex / rankUpPerWorld);
            return powerLevel;
        }
    }
}