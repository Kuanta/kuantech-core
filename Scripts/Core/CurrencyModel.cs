using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public struct Currency
    {
        public int Amount;
        public string CurrencyId;

        public Currency SetAmount(int amount)
        {
            Amount = amount;
            Amount = Mathf.Max(Amount, 0);
            return this;
        }

        public Currency AddAmount(int amount)
        {
            Amount += amount;
            Amount = Mathf.Max(Amount, 0);
            return this;
        }
    }

    [CreateAssetMenu(menuName = "Kuantech/StateModules/CurrencyModule")]
    public class CurrencyModel : StateModule
    {

        public Dictionary<string, Currency> Currencies;
        public List<Currency> DefaultCurrencies;

        public override void SetDefaultValues()
        {
            Currencies = new Dictionary<string, Currency>();
            if(DefaultCurrencies == null) return;
            foreach(var defaultCurr in DefaultCurrencies)
            {
                Currencies[defaultCurr.CurrencyId] = defaultCurr;
            }
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
            if (Currencies != null && Currencies.ContainsKey(currencyId))
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