using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class LevelDesignDataCollection : MonoBehaviour
    {
        [SerializeReference]
        public List<LevelDesignData> LevelDesigns;
        public int RepeatLastLevels = 10;
        public virtual async UniTask Initialize()
        {
            
        }

        public virtual async UniTask UpdateData()
        {
            
        }
        public virtual LevelDesignData GetLevelDesignData(int levelIndex)
        {
            if (LevelDesigns.IsNullOrEmpty()) return null;
            if(RepeatLastLevels > 0)
            {
                RepeatLastLevels = Mathf.Min(RepeatLastLevels, LevelDesigns.Count);
                int diff = levelIndex - LevelDesigns.Count;
                if (diff >= 0)
                {
                    int modulus = (diff) % RepeatLastLevels;
                    levelIndex = LevelDesigns.Count - RepeatLastLevels + modulus;
                }
                
            }else
            {
                levelIndex = Mathf.Min(levelIndex,LevelDesigns.Count - 1);
            }
            levelIndex = Mathf.Min(levelIndex, LevelDesigns.Count - 1);
            return LevelDesigns[levelIndex];
        }

        public virtual void UpdateFromSheetData(JObject sheetData, Type classType)
        {
            LevelDesigns = new List<LevelDesignData>();
            JArray array = (JArray) sheetData["values"];
            for (int i = 0; i < array.Count-1; ++i)
            {
                LevelDesignData data = CreateDesignDataInstance(classType);
                data.CreateFromSheetData(sheetData, i);
                LevelDesigns.Add(data);
            }
        }

        public LevelDesignData CreateDesignDataInstance(Type classType)
        {
            if (classType == null || !typeof(LevelDesignData).IsAssignableFrom(classType)) return null;
            LevelDesignData levelDesignData = (LevelDesignData)Activator.CreateInstance(classType);
            return levelDesignData;
        }
    }
}