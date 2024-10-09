using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class LevelDesignAsset : ScriptableObject
    {
        public virtual void UpdateFromSheetData(JArray sheetdata, int index)
        {
            throw new NotImplementedException();
        }
    }
}