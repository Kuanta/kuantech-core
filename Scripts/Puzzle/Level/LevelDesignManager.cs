using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Data;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;

namespace Kuantech.Puzzle
{
    public class LevelDesignManager : SubManager
    {
        public SheetReader SheetReader;
        public string LevelDesignSheetRange;
        public string ClassName;
        private JObject _sheetData;

        public List<LevelDesignAsset> DesignAssets;

        public bool UseSheetReader = true;
        [NonSerialized] public bool SheetRead;
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            SheetRead = false;
            if (!UseSheetReader) return;
            _sheetData = null;
            if (SheetReader == null) return;
            SheetReader.OnSheetRead += (readData =>
            {
                SheetRead = true;
                _sheetData = readData;
            });
            await SheetReader.GetSheetData(LevelDesignSheetRange);
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
            var classType = Type.GetType(context.ClassName);
            if (classType == null || !typeof(LevelDesignData).IsAssignableFrom(classType)) return null;
            LevelDesignData levelDesignData = (LevelDesignData)Activator.CreateInstance(classType);

            if(context._sheetData != null && context.SheetRead)
            {
                if (levelDesignData.CreateFromSheetData(context._sheetData, levelIndex))
                {
                    return levelDesignData;
                }
            }
            
            //Try to get from assets array
            LevelDesignAsset designAsset = context.GetLevelDesignAsset(levelIndex);
            if (designAsset != null)
            {
                levelDesignData.CreateFromDesignAsset(designAsset);
                return levelDesignData;
            }

            return null;
        }

        private LevelDesignAsset GetLevelDesignAsset(int levelIndex)
        {
            if (DesignAssets == null) return null;
            if (levelIndex >= DesignAssets.Count) return DesignAssets[DesignAssets.Count - 1];
            return DesignAssets[levelIndex];
        }
        
        [Button("Update Design Assets")]
        public async void UpdateDesignAssets()
        {
            SheetReader.OnSheetRead += (readData =>
            {
                UpdateForEditor(readData);
            });
            await SheetReader.GetSheetData(LevelDesignSheetRange);
        }

        private void UpdateForEditor(JObject sheetData)
        {
#if UNITY_EDITOR
            if (sheetData == null) return;
            for (int i = 0; i < DesignAssets.Count; ++i)
            {
                DesignAssets[i].UpdateFromSheetData((JArray)sheetData["values"], i);
            }
#endif
        }
    }
}