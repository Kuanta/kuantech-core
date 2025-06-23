using System;
using Cysharp.Threading.Tasks;
using Kuantech.Core.Database;

namespace Kuantech.Utils
{
 
    [Serializable]
    public class ConfigDataDictionary : SerializableDictionary<string, KtDataEntry>
    {
        
    }
    public class ConfigSource
    {
        public ConfigDataDictionary ConfigDataDictionary;
        
        public virtual async UniTask Initialize(ConfigManager configManager)
        {
        }

        public virtual KtDataEntry GetConfig(string key)
        {
            if (ConfigDataDictionary != null && ConfigDataDictionary.ContainsKey(key))
            {
                return ConfigDataDictionary[key];
            }

            return null;
        }

        public virtual bool GetValue<T>(string key, out T value)
        {
            KtDataEntry dataEntry = GetConfig(key);
            value = default;
            if (dataEntry == null) return false;
            value = dataEntry.Get<T>();
            return true;
        }
        
        public int GetIntConfig(string key, int defaultValue = 0)
        {

            if (GetValue(key, out int value))
            {
                return value;
            }

            return defaultValue;
        }

        public float GetFloatConfig(string key, float defaultValue = 0f)
        {
            if (GetValue(key, out float value))
            {
                return value;
            }

            return defaultValue;
        }

        public string GetStringConfig(string key, string defaultValue = "")
        {
            if(GetValue(key, out string value))
            {
                return value;
            }

            return defaultValue;
        }
        
        public bool GetBoolConfig(string key, bool defaultValue = false)
        {
            if(GetValue(key, out bool value))
            {
                return value;
            }

            return defaultValue;
        }
        
        public async virtual UniTask LoadConfig()
        {
        }
        
        #if UNITY_EDITOR
        public async virtual UniTask LoadConfigInEditor()
        {
            
        }
        #endif
    }
}