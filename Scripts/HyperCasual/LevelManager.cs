using System;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    [Serializable]
    public class LevelDictionary : SerializableDictionary<int, Level>{}
    public class LevelManager : Singleton<LevelManager>
    {
        [Header("Levels List")] 
        public LevelDictionary LevelDictionary = new LevelDictionary();

        public virtual Level GetLevel(int levelIndex)
        {
            Level level = LevelDictionary[levelIndex];
            level.LevelIndex = levelIndex;
            return LevelDictionary[levelIndex];
        }
    }
}