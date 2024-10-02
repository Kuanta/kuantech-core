using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [Serializable]
    public class LevelDesignData
    {
        public virtual bool CreateFromDesignAsset(LevelDesignAsset levelDesignAsset)
        {
            throw new NotImplementedException();
        }

        public virtual bool CreateFromSheetData(JObject sheetData, int levelIndex)
        {
            throw new NotImplementedException();
        }
    }
    public class LevelDesignAsset : ScriptableObject
    {
        public virtual void UpdateFromSheetData(JArray sheetdata, int index)
        {
            throw new NotImplementedException();
        }
    }
}