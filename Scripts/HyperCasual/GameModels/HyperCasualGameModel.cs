using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    [CreateAssetMenu(menuName = "Kuantech/StateModules/HyperCasualModule")]
    public class HyperCasualGameModel : StateModule
    {
        // A unique identifier for this module.
        public override string ModuleID => typeof(HyperCasualGameModel).ToString();

        public int LevelIndex {get;set;}
        public Dictionary<string, Currency> Currencies { get; set; } = new Dictionary<string, Currency>();

        public override void SetDefaultValues()
        {
            LevelIndex = 0;
            Currencies = new Dictionary<string, Currency>();
        }

        public void SetLevelIndex(int levelIndex)
        {
            LevelIndex = levelIndex;
            Dirtied = true;
        }

        public int GetLevelIndex()
        {
            return LevelIndex;
        }

        public void AddCurrency(string currencyId, int amount)
        {
            Dirtied = true;
            if (!Currencies.ContainsKey(currencyId))
            {
                Currencies[currencyId] = new Currency{
                    Amount = amount,
                    CurrencyId = currencyId,
                };
                return;
            }
            Currencies[currencyId] = Currencies[currencyId].AddAmount(amount);
        }

        public void RemoveCurrency(string currencyId, int amount)
        {
            Dirtied = true;
            if (!Currencies.ContainsKey(currencyId)) return;
            AddCurrency(currencyId, -Mathf.Abs(amount));
        }

        public Currency GetCurrency(string currencyId)
        {
            if (Currencies.ContainsKey(currencyId))
            {
                return Currencies[currencyId];
            }
            return new Currency{
                Amount = 0,
                CurrencyId = currencyId,
            };
        }

        public int GetCurrencyAmount(string currencyId)
        {
            return GetCurrency(currencyId).Amount;
        }

        public void SetCurrency(string currencyId, int amount)
        {
            Currencies[currencyId] = new Currency{
                Amount = amount,
                CurrencyId = currencyId,
            };
            Dirtied = true;
        }
    }
}