using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Utils
{
    public class ConfigManager : SubManager
    {
        private Dictionary<string, ConfigSource> _configIdToSource;
        
        [SerializeReference]
        public List<ConfigSource> ConfigSources;
        
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);

            List<UniTask> tasks = new List<UniTask>();
            if (ConfigSources != null)
            {
                foreach(var configSource in ConfigSources)
                {
                    if (configSource == null) continue;
                    tasks.Add(configSource.Initialize(this));
                }
                await tasks;
            
                //Register configs
                foreach(var configSource in ConfigSources)
                {
                    if (configSource == null) continue;
                    foreach (var key in configSource.ConfigDataDictionary.Keys)
                    {
                        if (_configIdToSource.ContainsKey(key))
                        {
                            Debug.LogWarning("Duplicate key found in config sources: " + key);
                            continue;
                        }

                        _configIdToSource[key] = configSource;
                    }
                }
            }
            
        }


        /// <summary>
        /// Configs are stored in a dictionary with their keys. This gets the corresponding config source
        /// </summary>
        /// <param name="configKey"></param>
        /// <returns></returns>
        private ConfigSource GetConfigSourceFromConfigKey(string configKey)
        {
            if (_configIdToSource.IsNullOrEmpty()) return null;
            return _configIdToSource[configKey];
        } 

        public static float GetFloatConfig(string key, float defaultValue = 0.0f)
        {
            var ctx = GetContext<ConfigManager>();
            ConfigSource configSource = ctx.GetConfigSourceFromConfigKey(key);
            if (configSource == null) return defaultValue;
            return configSource.GetFloatConfig(key, defaultValue);
        }
        

        public static int GetIntConfig(string key, int defaultValue = 0)
        {
            var ctx = GetContext<ConfigManager>();
            ConfigSource configSource = ctx.GetConfigSourceFromConfigKey(key);
            if (configSource == null) return defaultValue;
            return configSource.GetIntConfig(key, defaultValue);
        }

        public static string GetStringConfig(string key, string defaultValue="")
        {
            var ctx = GetContext<ConfigManager>();
            ConfigSource configSource = ctx.GetConfigSourceFromConfigKey(key);
            if (configSource == null) return defaultValue;
            return configSource.GetStringConfig(key, defaultValue);
        }
        
        public static bool GetBoolConfig(string key, bool defaultValue=false)
        {
            var ctx = GetContext<ConfigManager>();
            ConfigSource configSource = ctx.GetConfigSourceFromConfigKey(key);
            if (configSource == null) return defaultValue;
            return configSource.GetBoolConfig(key, defaultValue);
        }
       
    }
}