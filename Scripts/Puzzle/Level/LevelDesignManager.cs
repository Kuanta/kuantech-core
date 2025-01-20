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
        [Header("Sheer Remote Data")]
        public SheetReader SheetReader;
        public string LevelDesignSheetRange;
        public string ClassName;
        private JObject _sheetData;

        public LevelDesignDataCollection LevelDesignsCollection;
        
        public bool UseSheetReader = true;
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            if (!UseSheetReader)
            {
                if (LevelDesignsCollection == null) return;
                await LevelDesignsCollection.Initialize();
                return;
            }
            
            //Rest is for backward compability
            _sheetData = null;
            if (SheetReader == null) return;
            SheetReader.OnSheetRead += (readData =>
            {
                _sheetData = readData;
                if (LevelDesignsCollection != null)
                {
                    LevelDesignsCollection.UpdateFromSheetData(_sheetData, Type.GetType(ClassName));
                }
            });
            await SheetReader.GetSheetData(LevelDesignSheetRange);
        }

        public async void UpdateDataFromRemote()
        {
            await SheetReader.GetSheetData(LevelDesignSheetRange);
        }

        public async void UpdateLevelDataCollection()
        {
            if (LevelDesignsCollection == null) return;
            await LevelDesignsCollection.UpdateData();
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
            LevelDesignsCollection.UpdateFromSheetData(sheetData, Type.GetType(ClassName));
#endif
        }
    }
}