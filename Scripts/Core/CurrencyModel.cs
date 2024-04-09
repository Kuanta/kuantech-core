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
    [Serializable]
    public class CurrencyModelData
    {
        public Dictionary<string, Currency> Currencies;
        public List<Currency> DefaultCurrencies;
    }
    
    [CreateAssetMenu(menuName = "Kuantech/StateModules/CurrencyModule")]
    public class CurrencyModel : StateModule
    {
        

        public CurrencyModelData Data;
        public override void SetDefaultValues()
        {
            Data = new CurrencyModelData();
            Data.Currencies = new Dictionary<string, Currency>();
            if(Data.DefaultCurrencies == null) return;
            foreach(var defaultCurr in Data.DefaultCurrencies)
            {
                Data.Currencies[defaultCurr.CurrencyId] = defaultCurr;
            }
        }

        public void AddCurrency(string currencyId, int amount)
        {
            Dirtied = true;
            if (!Data.Currencies.ContainsKey(currencyId))
            {
                Data.Currencies[currencyId] = new Currency{
                    Amount = amount,
                    CurrencyId = currencyId,
                };
                return;
            }
            Data.Currencies[currencyId] = Data.Currencies[currencyId].AddAmount(amount);
        }

        public void RemoveCurrency(string currencyId, int amount)
        {
            Dirtied = true;
            if (!Data.Currencies.ContainsKey(currencyId)) return;
            AddCurrency(currencyId, -Mathf.Abs(amount));
        }

        public Currency GetCurrency(string currencyId)
        {
            if (Data.Currencies != null && Data.Currencies.ContainsKey(currencyId))
            {
                return Data.Currencies[currencyId];
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
            Data.Currencies[currencyId] = new Currency{
                Amount = amount,
                CurrencyId = currencyId,
            };
            Dirtied = true;
        }

        public override object GetData()
        {
            return Data;
        }

        public override void SetData(object loadedData)
        {
            Data = loadedData as CurrencyModelData;
        }

        public override Type GetDataType()
        {
           return typeof(CurrencyModelData);
        }
    }
}