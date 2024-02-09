using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Kuantech.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class NonSaveableAttribute : Attribute
    {
    }


    public class NonSaveableContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            // Check if the NonSaveableAttribute is applied to the field
            if (Attribute.IsDefined(member, typeof(NonSaveableAttribute)))
            {
                property.ShouldSerialize = _ => false;
            }

            return property;
        }
    }
    
    [Serializable]
    public abstract class StateModule : ScriptableObject
    {
        public virtual string ModuleID => GetType().FullName;
        public virtual void Load(string savedData)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            var loadedObject = JsonConvert.DeserializeObject(savedData, this.GetType(), settings);
            foreach (var field in this.GetType().GetFields())
            {
                if (!Attribute.IsDefined(field, typeof(NonSaveableAttribute)))
                {
                    var loadedValue = field.GetValue(loadedObject);
                    field.SetValue(this, loadedValue);
                }
            }
        }

        public virtual string Save()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                ContractResolver = new NonSaveableContractResolver()
            };

            return JsonConvert.SerializeObject(this, Formatting.Indented, settings);
        }

        [NonSerialized] public bool Dirtied = false;

        public abstract void SetDefaultValues();
    }

    public class GameState
    {
        private Dictionary<string, StateModule> _modules = new Dictionary<string, StateModule>();
        private Dictionary<string, string> _serializedModules = new Dictionary<string, string>();

        public void RegisterModule(StateModule module)
        {
            _modules[module.ModuleID] = module;
        }

        public T GetModule<T>() where T : StateModule
        {
            string moduleID = typeof(T).ToString();
            if (_modules.TryGetValue(moduleID, out StateModule module))
            {
                return module as T;
            }
            return null;
        }

        /// <summary>
        /// Saves the module if on of them is dirtied
        /// </summary>
        /// <param name="path"></param>
        public void SaveAllModules(string path)
        {
            bool dirtied = false;
            foreach (var pair in _modules)
            {
                if(pair.Value.Dirtied)
                {
                    string serializedVal = pair.Value.Save();
                    _serializedModules[pair.Key] = pair.Value.Save();
                    dirtied = true;
                    pair.Value.Dirtied = false;
                }
            }

            if(!dirtied) return;
            string json = JsonConvert.SerializeObject(_serializedModules);
            File.WriteAllText(path, json);
        }

        public string StateFileName = "/gameState.json";
        
        public virtual async UniTask LoadData()
        {
            string jsonPath = GetSaveFilePath();
            if (!File.Exists(jsonPath))
            {
                //Set default models
                foreach (var pair in _modules)
                {
                    StateModule module = pair.Value;
                    module.SetDefaultValues();
                }
                return;
            }

            Task<string> readTask = File.ReadAllTextAsync(jsonPath);
            await readTask;

            string jsonString = readTask.Result;
            _serializedModules = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

            foreach(var pair in _modules)
            {
                StateModule module = pair.Value;
                if(_serializedModules.ContainsKey(pair.Key))
                {
                    module.Load(_serializedModules[pair.Key]);
                }else{
                    module.SetDefaultValues();
                }
            }
        }

        
        public virtual void SaveData()
        {
            // Write JSON to file.
            string inventoryPath = GetSaveFilePath();
            SaveAllModules(inventoryPath);
        }

        protected string GetSaveFilePath()
        {
            return Application.persistentDataPath + StateFileName;
        }    
    }
}