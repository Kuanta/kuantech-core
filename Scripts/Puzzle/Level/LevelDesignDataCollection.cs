using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class LevelDesignDataCollection : MonoBehaviour
    {
        public int RepeatLastLevels = 0;
        public virtual LevelDesignData GetLevelDesignData(int levelIndex)
        {
            throw new NotImplementedException();
        }

        public virtual void UpdateFromSheetData(JObject sheetData)
        {
            
        }
    }
}