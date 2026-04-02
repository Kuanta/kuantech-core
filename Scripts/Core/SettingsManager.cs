using System;
using System.Collections.Generic;
using Kuantech.Core.Database;
using UnityEngine;

namespace Kuantech.Core
{
    public class SettingsManager : SubManager
    {
        [Serializable]
        public class SettingEntry : ISaveable
        {
            public struct SettingEntrySerializedData
            {
                public byte[] ValueData;
                public string ValueTypeName;
            }
            [SaveableField] public string Key;
            [NonSaveableField] [SerializeReference] public KtDataType Value;
            public byte[] Serialize()
            {
                var data = new SettingEntrySerializedData
                {
                    ValueData = Value.Serialize(),
                    ValueTypeName = Value.GetType().AssemblyQualifiedName,
                };
                return SaveUtility.SerializePoco(data);
            }

            public void Deserialize(byte[] data)
            {
                var deserializedData = SaveUtility.DeserializePoco<SettingEntrySerializedData>(data);
                var type = Type.GetType(deserializedData.ValueTypeName);
                if (type == null)
                {
                    Debug.LogError($"Failed to get type {deserializedData.ValueTypeName} for setting entry {Key}");
                    return;
                }
                Value = (KtDataType)Activator.CreateInstance(type);
                Value.Deserialize(deserializedData.ValueData);
            }
        }
        public List<SettingEntry> SettingEntries;
        [SaveableField] private Dictionary<string, SettingEntry> _settingEntries;

        
        public override void SetDefaultState()
        {
            _settingEntries = new Dictionary<string, SettingEntry>();
            foreach (var entry in SettingEntries)
            {
                _settingEntries[entry.Key] = entry;
            }
        }
        
        public static float GetFloatSetting(string key, float defaultValue = 0.0f)
        {
            var ctx = GetContext<SettingsManager>();
            if (ctx == null || ctx._settingEntries == null || !ctx._settingEntries.ContainsKey(key)) return defaultValue;
            if (ctx._settingEntries[key].Value == null) return defaultValue;
            return ctx._settingEntries[key].Value.Get<float>();
        }
        
        public static void SetFloatSetting(string key, float value)
        {
            var ctx = GetContext<SettingsManager>();
            if (ctx == null) return;
            ctx._settingEntries ??= new Dictionary<string, SettingEntry>();
            ctx._settingEntries[key] = new SettingEntry
            {
                Key = key,
                Value = new KtFloat(){Value = value},
            };
            ctx.SaveState();
        }

        public static int GetIntSetting(string key, int defaultValue = 0)
        {
            var ctx = GetContext<SettingsManager>();
            if (ctx == null || ctx._settingEntries == null || !ctx._settingEntries.ContainsKey(key)) return defaultValue;
            if (ctx._settingEntries[key].Value == null) return defaultValue;
            return ctx._settingEntries[key].Value.Get<int>();
        }

        public static void SetIntValue(string key, int value)
        {
            var ctx = GetContext<SettingsManager>();
            if (ctx == null) return;
            ctx._settingEntries ??= new Dictionary<string, SettingEntry>();
            ctx._settingEntries[key] = new SettingEntry
            {
                Key = key,
                Value = new KtInt(){Value = value},
            };
            ctx.SaveState();
        }
        
        public static string GetStringSetting(string key, string defaultValue="")
        {
            var ctx = GetContext<SettingsManager>();
            if (ctx == null || ctx._settingEntries == null || !ctx._settingEntries.ContainsKey(key)) return defaultValue;
            if (ctx._settingEntries[key].Value == null) return defaultValue;
            return ctx._settingEntries[key].Value.Get<string>();
        }

        public static string SetStringSetting(string key, string value)
        {
            var ctx = GetContext<SettingsManager>();
            if (ctx == null) return default;
            ctx._settingEntries ??= new Dictionary<string, SettingEntry>();
            ctx._settingEntries[key] = new SettingEntry
            {
                Key = key,
                Value = new KtString(){Value = value},
            };
            ctx.SaveState();
            return value;
        }
        
        public static bool GetBoolSetting(string key, bool defaultValue=false)
        {
            var ctx = GetContext<SettingsManager>();
            if (ctx == null || ctx._settingEntries == null || !ctx._settingEntries.ContainsKey(key)) return defaultValue;
            if (ctx._settingEntries[key].Value == null) return defaultValue;
            return ctx._settingEntries[key].Value.Get<bool>();

        }
        
        public static void SetBoolSetting(string key, bool value)
        {
            var ctx = GetContext<SettingsManager>();
            if (ctx == null) return;
            ctx._settingEntries ??= new Dictionary<string, SettingEntry>();
            ctx._settingEntries[key] = new SettingEntry
            {
                Key = key,
                Value = new KtBool(){Value = value},
            };
            ctx.SaveState();
        }
    }
}