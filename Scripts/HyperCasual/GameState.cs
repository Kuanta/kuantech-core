using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class GameState
    {
        public int LevelIndex;
        private Dictionary<int, Currency> _currencies;

        public GameState(List<int> currencyIds)
        {
            if (currencyIds.IsNullOrEmpty())
            {
                currencyIds = new List<int> {0};
            }
            _currencies = new Dictionary<int, Currency>();
            foreach (int id in currencyIds)
            {
                _currencies[id] = new Currency
                {
                    CurrencyId = id,
                    Amount = 0,
                };
            }    
        }
        
        public virtual void LoadData()
        {
            LoadCurrencies();
        }

        public virtual void SaveData()
        {
            SaveCurrencies();
        }
        
        #region Level
        public virtual void SetLevelIndex(int levelIndex)
        {
            PlayerPrefs.SetInt("LevelIndex", levelIndex);
        }

        public virtual int GetLevelIndex()
        {
            return PlayerPrefs.GetInt("LevelIndex", 0);
        }
        #endregion
        
        #region Currencies

        protected virtual void LoadCurrencies()
        {
            foreach (int currencyId in _currencies.Keys)
            {
                int amount = ReadCurrency(currencyId);
                _currencies[currencyId].SetAmount(amount);
            }
        }

        protected virtual void SaveCurrencies()
        {
            // foreach (int currencyId in _currencies.Keys)
            // {
            //     int amount = _currencies[currencyId].Amount;
            //     WriteCurrency(currencyId);
            // } 
        }
        
        /// <summary>
        /// Reads the amount of currency from saved data
        /// </summary>
        /// <param name="currencyId"></param>
        /// <returns></returns>
        protected virtual int ReadCurrency(int currencyId)
        {
            return PlayerPrefs.GetInt(currencyId.ToString(), 0);
        }
        
        /// <summary>
        /// Writes the currency to desired format.
        /// </summary>
        /// <param name="currencyId"></param>
        /// <param name="amount"></param>
        protected virtual void WriteCurrency(int currencyId)
        {
            PlayerPrefs.SetInt(currencyId.ToString(), _currencies[currencyId].Amount);
        }
        
        public virtual void AddCurrency(int currencyId, int amount)
        {
            if (!_currencies.ContainsKey(currencyId))
            {
                _currencies[currencyId] = new Currency
                {
                    CurrencyId = currencyId,
                    Amount = amount,
                };
            }
            else
            {
                _currencies[currencyId] = _currencies[currencyId].AddAmount(amount);
            }
            WriteCurrency(currencyId);
        }

        public virtual void RemoveCurrency(int currencyId, int amount)
        {
            AddCurrency(currencyId, -amount);
        }

        public virtual Currency GetCurrency(int currencyId)
        {
            if (!_currencies.ContainsKey(currencyId)) return new Currency
            {
                CurrencyId = currencyId,
                Amount = 0,
            };
            return _currencies[currencyId];
        }
        public virtual int GetCurrencyAmount(int currencyId)
        {
            if (!_currencies.ContainsKey(currencyId)) return 0;
            return _currencies[currencyId].Amount;
        }

        public virtual void SetCurrency(int currencyId, int amount)
        {
            if (!_currencies.ContainsKey(currencyId))
            {
                _currencies[currencyId] = new Currency
                {
                    CurrencyId = currencyId,
                    Amount = amount,
                };
            }
            else
            {
                _currencies[currencyId].SetAmount(amount);

            }
            WriteCurrency(currencyId);
        }
        #endregion
    }
}