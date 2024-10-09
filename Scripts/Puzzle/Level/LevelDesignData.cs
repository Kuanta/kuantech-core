using System;
using Newtonsoft.Json.Linq;

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
}