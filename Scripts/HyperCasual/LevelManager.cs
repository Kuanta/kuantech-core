using System;
using System.Linq;
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
            if (!LevelDictionary.ContainsKey(levelIndex))
            {
                levelIndex = (LevelDictionary.Keys.ToList())[LevelDictionary.Keys.Count -1];
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