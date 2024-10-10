using System;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Data;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace Kuantech.Puzzle
{
    public class LevelDesignManager : SubManager
    {
        [Header("Remote Data")]
        public SheetReader SheetReader;
        public string LevelDesignSheetRange;
        public string ClassName;
        private JObject _sheetData;

        public LevelDesignDataCollection LevelDesignsCollection;
        //public List<LevelDesignAsset> DesignAssets;

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
                if (LevelDesignsCollection != null)
                {
                    LevelDesignsCollection.UpdateFromSheetData(_sheetData);
                }
            });
            await SheetReader.GetSheetData(LevelDesignSheetRange);
        }

        public async void UpdateDataFromRemote()
        {
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

            if (context.LevelDesignsCollection != null)
            {
                //Try to get from assets array
                return context.LevelDesignsCollection.GetLevelDesignData(levelIndex);
            }
            
            var classType = Type.GetType(context.ClassName);
            if (classType == null || !typeof(LevelDesignData).IsAssignableFrom(classType)) return null;
            LevelDesignData levelDesignData = (LevelDesignData)Activator.CreateInstance(classType);
            levelDesignData.CreateFromSheetData(context._sheetData, levelIndex);
            return levelDesignData;
        }

        private LevelDesignData GetLevelDesignAsset(int levelIndex)
        {
            return LevelDesignsCollection.GetLevelDesignData(levelIndex);
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
            LevelDesignsCollection.UpdateFromSheetData(sheetData);
#endif
        }
    }
}