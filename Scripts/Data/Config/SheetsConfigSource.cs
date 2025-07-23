
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
        
        private void OnConfigSheetRead(JObject sheetData)
        {
            if (sheetData == null) return;
            JArray array = (JArray) sheetData["values"];
            JArray headerRow = array[0] as JArray;
            JArray valuesRow = array[1] as JArray;
            
            for (int i = 0; i < headerRow.Count; ++i)
            {
                string configKey = headerRow[i].ToString();
                string value = valuesRow[i].ToString();
                
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