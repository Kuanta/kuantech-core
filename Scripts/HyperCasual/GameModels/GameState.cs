using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    [Serializable]
    public abstract class StateModule : ScriptableObject
    {
        public abstract string ModuleID {get;}
        public virtual void Load(string savedData)
        {
            var loadedObject = JsonConvert.DeserializeObject(savedData, this.GetType());
            foreach (var property in this.GetType().GetFields())
            {
                var loadedValue = property.GetValue(loadedObject);
                property.SetValue(this, loadedValue);
            }
        }

        public virtual string Save()
        {
            return JsonConvert.SerializeObject(this);
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

        // #region Level
        // public virtual void SetLevelIndex(int levelIndex)
        // {
        //     GameStateModel.LevelIndex = levelIndex;
        //     Dirtied = true;
        //     //PlayerPrefs.SetInt("LevelIndex", levelIndex);
        // }

        // public virtual int GetLevelIndex()
        // {
        //     return GameStateModel.LevelIndex;
        // }
        // #endregion
        
        // #region Currencies
        // public virtual void AddCurrency(int currencyId, int amount)
        // {
        //     if (!GameStateModel.Currencies.ContainsKey(currencyId))
        //     {
        //         GameStateModel.Currencies[currencyId] = new Currency
        //         {
        //             CurrencyId = currencyId,
        //             Amount = amount,
        //         };
        //     }
        //     else
        //     {
        //         GameStateModel.Currencies[currencyId] = GameStateModel.Currencies[currencyId].AddAmount(amount);
        //     }

        //     Dirtied = true;
        // }

        // public virtual void RemoveCurrency(int currencyId, int amount)
        // {
        //     AddCurrency(currencyId, -Mathf.Abs(amount));
        // }

        // public virtual Currency GetCurrency(int currencyId)
        // {
        //     if (!GameStateModel.Currencies.ContainsKey(currencyId)) return new Currency
        //     {
        //         CurrencyId = currencyId,
        //         Amount = 0,
        //     };
        //     return GameStateModel.Currencies[currencyId];
        // }
        // public virtual int GetCurrencyAmount(int currencyId)
        // {
        //     GameStateModel.Currencies ??= new Dictionary<int, Currency>();
        //     return !GameStateModel.Currencies.ContainsKey(currencyId) ? 0 : GameStateModel.Currencies[currencyId].Amount;
        // }

        // public virtual void SetCurrency(int currencyId, int amount)
        // {
        //     if (!GameStateModel.Currencies.ContainsKey(currencyId))
        //     {
        //         GameStateModel.Currencies[currencyId] = new Currency
        //         {
        //             CurrencyId = currencyId,
        //             Amount = amount,
        //         };
        //     }
        //     else
        //     {
        //         GameStateModel.Currencies[currencyId] = GameStateModel.Currencies[currencyId].SetAmount(amount);
        //     }

        //     Dirtied = true;
        // }
        //#endregion
    }
}