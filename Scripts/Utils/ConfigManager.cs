using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Data;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kuantech.Utils
{
    public enum ConfigType
    {
        Integer = 0,
        Float = 1,
        String = 2,
    }

    [Serializable]
    public class ConfigEntry
    {
        public string Key;
        public ConfigType ConfigType;
        public int IntegerValue;
        public float FloatValue;
        public string StringValue;
    }
    
    public class ConfigManager : SubManager
    {
        [Header("Configs")] 
        public List<ConfigEntry> ConfigEntries;
        public Dictionary<string, ConfigEntry> Configs;
        
        [Header("Config From Sheet")] 
        public SheetReader SheetReader;
        public string ConfigSheetRange;
        
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            Configs = new Dictionary<string, ConfigEntry>();
            foreach (var entry in ConfigEntries)
            {
                Configs[entry.Key] = entry;
            }
            await ReadConfigsFromSheet();
        }
        private async UniTask ReadConfigsFromSheet()
        {
            await SheetReader.GetSheetData(ConfigSheetRange);
        }
        private void OnConfigSheetRead(JObject sheetData)
        {
            JArray array = (JArray) sheetData["values"];
            for (int i = 0; i < array.Count - 1; ++i)
            {
                JToken row = array[i];
                string configKey = row[0].ToString();
                string value = row[1].ToString();
                if (Configs.ContainsKey(configKey))
                {
                    ConfigEntry configEntry = Configs[configKey];
                    //Update value
                    switch (configEntry.ConfigType)
                    {
                        case ConfigType.Float:
                            if (float.TryParse(value,NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                            {
                                configEntry.FloatValue = floatValue;
                            }
                            break;
                        case ConfigType.Integer:
                            if (Int32.TryParse(value,out int intValue))
                            {
                                configEntry.IntegerValue = intValue;
                            }
                            break;
                        case ConfigType.String:
                            configEntry.StringValue = value;
                            break;
                    }
                }
            }
        }
        
        public static ConfigEntry GetConfig(string key)
        {
            var context = GetContext<ConfigManager>();
            if (!context.Configs.ContainsKey(key)) return null;
            return context.Configs[key];
        }

        public static float GetFloatConfig(string key)
        {
            return GetConfig(key).FloatValue;
        }

        public static int GetIntConfig(string key)
        {
            return GetConfig(key).IntegerValue;
        }

        public static string GetStringConfig(string key)
        {
            return GetConfig(key).StringValue;
        }
    }
}