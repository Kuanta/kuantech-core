using System;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class LevelDesignDataCollection : MonoBehaviour
    {
        public virtual async UniTask Initialize()
        {
            
        }
        
        public virtual LevelDesignData GetLevelDesignData(int levelIndex)
        {
            throw new NotImplementedException();
        }

        public virtual void UpdateFromSheetData(JObject sheetData)
        {
            
        }
    }
}