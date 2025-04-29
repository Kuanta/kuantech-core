using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Data;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
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

        public string ConfigClassName;
        
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            Configs = new Dictionary<string, ConfigEntry>();
            foreach (var entry in ConfigEntries)
            {
                Configs[entry.Key] = entry;
            }
            await ReadConfigsFromSheet();
            UpdateConfigClassFields();
        }

        private void UpdateConfigClassFields()
        {
            Type staticConfigClassType = Type.GetType(ConfigClassName);
            if (staticConfigClassType == null) return;
            FieldInfo[] fields = staticConfigClassType.GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (var field in fields)
            {
                if (Configs.TryGetValue(field.Name, out var configEntry))
                {
                    try
                    {
                        if (field.FieldType == typeof(int))
                        {
                            field.SetValue(null, configEntry.IntegerValue);
                        }
                        else if (field.FieldType == typeof(float))
                        {
                            field.SetValue(null, configEntry.FloatValue);
                        }
                        else if (field.FieldType == typeof(string))
                        {
                            field.SetValue(null, configEntry.StringValue);
                        }
                        else
                        {
                            Debug.LogWarning($"Unsupported field type for config: {field.Name} ({field.FieldType})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to set config field {field.Name}: {ex}");
                    }
                }
            }
        }
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

        [Button("DetectConfigs")]
        public void DetectConfigEntries()
        {
            DetectConfigEntries(Type.GetType(ConfigClassName));
        }
        
        public void DetectConfigEntries(Type staticConfigClassType)
        {
            if(staticConfigClassType == null) return;
            ConfigEntries = new List<ConfigEntry>();
            Configs = new Dictionary<string, ConfigEntry>();

            // Static, public field'ları alıyoruz
            FieldInfo[] fields = staticConfigClassType.GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (var field in fields)
            {
                var entry = new ConfigEntry
                {
                    Key = field.Name
                };

                object value = field.GetValue(null); // Static olduğu için instance yok, o yüzden null veriyoruz.

                if (value is int intValue)
                {
                    entry.ConfigType = ConfigType.Integer;
                    entry.IntegerValue = intValue;
                }
                else if (value is float floatValue)
                {
                    entry.ConfigType = ConfigType.Float;
                    entry.FloatValue = floatValue;
                }
                else if (value is string stringValue)
                {
                    entry.ConfigType = ConfigType.String;
                    entry.StringValue = stringValue;
                }
                else
                {
                    Debug.LogWarning($"Unsupported config field type: {field.Name} ({field.FieldType})");
                    continue; // Desteklemediğimiz bir türse, eklemiyoruz
                }

                ConfigEntries.Add(entry);
                Configs[entry.Key] = entry;
            }
        }
    }
}