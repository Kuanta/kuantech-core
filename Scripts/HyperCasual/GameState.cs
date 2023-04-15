using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class GameState
    {
        public int LevelIndex;
        public virtual void LoadData()
        {
            
        }

        public virtual void SaveData()
        {
            
        }

        public virtual void SetLevelIndex(int levelIndex)
        {
            PlayerPrefs.SetInt("LevelIndex", levelIndex);
        }

        public virtual int GetLevelIndex()
        {
            return PlayerPrefs.GetInt("LevelIndex", 0);
        }
    }
}