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
    }
}