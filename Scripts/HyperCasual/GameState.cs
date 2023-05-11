using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sirenix.Utilities;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    [Serializable]
    //Base Class for common game states
    public class GameStateModel
    {
        public int LevelIndex;
        public Dictionary<int, Currency> Currencies;

        public virtual void SetDefaultValues()
        {
            LevelIndex = 0;
            Currencies = new Dictionary<int, Currency>();
        }
    }
    
    public class GameState
    {
        // public int LevelIndex;
        // private Dictionary<int, Currency> _currencies;
        public GameStateModel GameStateModel;
        public string StateFileName = "/gameState.json";

        protected bool Dirtied = false;
        public GameState(List<int> currencyIds)
        {
            Dirtied = false;
        }

        public virtual void LoadData()
        {
            string jsonPath = GetSaveFilePath();
            if (!File.Exists(jsonPath))
            {
                CreateStateModel();
                return;
            }
            string jsonString = File.ReadAllText(jsonPath);
            GameStateModel = JsonConvert.DeserializeObject<GameStateModel>(jsonString);
            GameStateModel.Currencies ??= new Dictionary<int, Currency>();
        }
        
        protected virtual void CreateStateModel()
        {
            GameStateModel = new GameStateModel();
            GameStateModel.SetDefaultValues();
        }
        
        public virtual void SaveData()
        {
            if (!Dirtied) return;
            string jsonString = JsonConvert.SerializeObject(GameStateModel);

            // Write JSON to file.
            string inventoryPath = GetSaveFilePath();
            File.WriteAllText(inventoryPath, jsonString);
            Dirtied = false;
        }

        protected string GetSaveFilePath()
        {
            return Application.persistentDataPath + StateFileName;
        }
        #region Level
        public virtual void SetLevelIndex(int levelIndex)
        {
            GameStateModel.LevelIndex = levelIndex;
            Dirtied = true;
            //PlayerPrefs.SetInt("LevelIndex", levelIndex);
        }

        public virtual int GetLevelIndex()
        {
            return GameStateModel.LevelIndex;
        }
        #endregion
        
        #region Currencies
        public virtual void AddCurrency(int currencyId, int amount)
        {
            if (!GameStateModel.Currencies.ContainsKey(currencyId))
            {
                GameStateModel.Currencies[currencyId] = new Currency
                {
                    CurrencyId = currencyId,
                    Amount = amount,
                };
            }
            else
            {
                GameStateModel.Currencies[currencyId] = GameStateModel.Currencies[currencyId].AddAmount(amount);
            }

            Dirtied = true;
        }

        public virtual void RemoveCurrency(int currencyId, int amount)
        {
            AddCurrency(currencyId, -amount);
        }

        public virtual Currency GetCurrency(int currencyId)
        {
            if (!GameStateModel.Currencies.ContainsKey(currencyId)) return new Currency
            {
                CurrencyId = currencyId,
                Amount = 0,
            };
            return GameStateModel.Currencies[currencyId];
        }
        public virtual int GetCurrencyAmount(int currencyId)
        {
            GameStateModel.Currencies ??= new Dictionary<int, Currency>();
            return !GameStateModel.Currencies.ContainsKey(currencyId) ? 0 : GameStateModel.Currencies[currencyId].Amount;
        }

        public virtual void SetCurrency(int currencyId, int amount)
        {
            if (!GameStateModel.Currencies.ContainsKey(currencyId))
            {
                GameStateModel.Currencies[currencyId] = new Currency
                {
                    CurrencyId = currencyId,
                    Amount = amount,
                };
            }
            else
            {
                GameStateModel.Currencies[currencyId].SetAmount(amount);

            }

            Dirtied = true;
        }
        #endregion
    }
}