using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    [Serializable]
    public class LevelDictionary : SerializableDictionary<int, Level>{}
    public class LevelManager : SubManager
    {
        [Header("Levels List")] 
        public List<Level> LevelDictionary = new List<Level>();
        
        public virtual Level GetLevel(int levelIndex)
        {
            if (LevelDictionary.Count <= levelIndex)
            {
                levelIndex = LevelDictionary.Count - 1;
            }
            Level level = Instantiate(LevelDictionary[levelIndex].gameObject).GetComponent<Level>();
            level.transform.position = Vector3.zero;
            level.transform.rotation = Quaternion.identity;
            level.LevelIndex = levelIndex;
            level.OnLevelCreated(); //todo(optimization): This may be unefficient
            return level;
        }
    }
}