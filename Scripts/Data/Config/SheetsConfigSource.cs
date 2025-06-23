
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
        
        [Button("Update Design Assets")]
        private async UniTask ReadConfigsFromSheet()
        {
            if (SheetReader == null) return;
            SheetReader.OnSheetRead += OnConfigSheetRead;
            await SheetReader.GetSheetData(ConfigSheetRange);
        }
        
        private void OnConfigSheetRead(JObject sheetData)
        {
            JArray array = (JArray) sheetData["values"];
            for (int i = 0; i < array.Count; ++i)
            {
                JToken row = array[i];
                string configKey = row[0].ToString();
                string value = row[1].ToString();
                if (ConfigDataDictionary.ContainsKey(configKey))
                {
                    KtDataType configEntry = ConfigDataDictionary[configKey];
                    configEntry.ParseString(value);
                }
            }
        }

    }
}