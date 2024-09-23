using System;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Data;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class LevelDesignManager : SubManager
    {
        public SheetReader SheetReader;
        public string ClassName;
        private JObject _sheetData;
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            _sheetData = null;
            if (SheetReader == null) return;
            SheetReader.OnSheetRead += (readData =>
            {
                _sheetData = readData;
            });
            await SheetReader.GetSheetData();
        }
        
        /// <summary>
        /// Gets level design data
        /// </summary>
        /// <param name="levelIndex"></param>
        /// <returns></returns>
        public static LevelDesignData GetLevelDesignData(int levelIndex)
        {
            var context = LevelDesignManager.GetContext<LevelDesignManager>();
            if (context == null) return null;
            if (context._sheetData == null) return null;
            var classType = Type.GetType(context.ClassName);
            if (classType == null || !typeof(LevelDesignData).IsAssignableFrom(classType)) return null;
            
            LevelDesignData levelDesignData = (LevelDesignData)Activator.CreateInstance(classType);
            if (!levelDesignData.CreateFromSheetData(context._sheetData, levelIndex))
            {
                Debug.LogError($"Failed to read level design data for level:{levelIndex}");
                return null;
            }
            Debug.LogError($"Got level design from sheet for level:{levelIndex}");
            return levelDesignData;
        }
    }
}