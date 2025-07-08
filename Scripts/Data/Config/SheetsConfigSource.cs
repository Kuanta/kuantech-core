
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
            CreateDictionary();
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
            JArray array = (JArray) sheetData["values"];
            for (int i = 0; i < array.Count; ++i)
            {
                JToken headerRow = array[0];
                JToken row = array[1];
                string configKey = headerRow[i].ToString();
                string value = row[i].ToString();
                
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