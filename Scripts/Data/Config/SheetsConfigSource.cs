
using Cysharp.Threading.Tasks;
using Kuantech.Core.Database;
using Kuantech.Data;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Utils
{
    /// <summary>
    /// A google sheets config source
    /// </summary>
    public class SheetsConfigSource : ConfigSource
    {
        [Header("Config From Sheet")] 
        public SheetReader SheetReader;
        public string ConfigSheetRange;

        public override async UniTask Initialize(ConfigManager configManager)
        {
            await base.Initialize(configManager);
            await ReadConfigsFromSheet();
        }
        
        [Button("Update Design Assets")]
        private async UniTask ReadConfigsFromSheet()
        {
            if (SheetReader == null) return;
            JObject data = await SheetReader.GetSheetDataAsync(ConfigSheetRange);
            OnConfigSheetRead(data);
        }
        
        /// <summary>
        /// On sheets, configs are set in two columns. First row is header row, first column is config key, second column is config value
        /// </summary>
        /// <param name="sheetData"></param>
        private void OnConfigSheetRead(JObject sheetData)
        {
            if (sheetData == null) return;
            JArray array = (JArray) sheetData["values"];
            
            //Config Count
            int configCount = array.Count - 1;
            if (configCount <= 0) return;
            for (int i = 0; i < configCount; ++i)
            {
                JArray configRow = array[i + 1] as JArray;
                if(configRow == null || configRow.Count < 2) continue;
                string configKey = configRow[0].ToString();
                string value = configRow[1].ToString();
                //Update ConfigEntries list
                for(int j=0;j<ConfigEntries.Count; ++j)
                {
                    if (ConfigEntries[j].Key == configKey)
                    {
                        ConfigEntry entry = ConfigEntries[j];
                        entry.Value.ParseString(value);
                        ConfigEntries[j] = entry;
                        break;
                    }
                }
            }
        }

    }
}