using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine.Events;

namespace Kuantech.Core.Store
{
    [Serializable]
    public struct CurrencyData
    {
        public string CurrencyId;
        public int CurrencyAmount;
    }
    /// <summary>
    /// Manages the currency of the game
    /// </summary>
    public class CurrencyManager : SubManager
    {
        public List<CurrencyAsset> CurrencyAssets;
        
        [SaveableField] private Dictionary<string, int> _amountsById = new Dictionary<string, int>();
        
        //Event
        public UnityAction<CurrencyData> CurrencyUpdated;
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);

            _amountsById = new Dictionary<string, int>();
            foreach (var asset in CurrencyAssets)
            {
                _amountsById[asset.CurrencyId] = 0; //Default
            }
        }
        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            foreach (var pair in _amountsById)
            {
                TriggerCurrencyUpdatedEvent(pair.Key);
            }
        }

        private void TriggerCurrencyUpdatedEvent(CurrencyAsset asset)
        {
            TriggerCurrencyUpdatedEvent(asset.CurrencyId);
        }

        private void TriggerCurrencyUpdatedEvent(string currencyId)
        {
            if (!_amountsById.ContainsKey(currencyId))
            {
                CurrencyUpdated?.Invoke(new CurrencyData()
                {
                    CurrencyId = currencyId,
                    CurrencyAmount = 0,
                });
            }
            else
            {
                CurrencyUpdated?.Invoke(new CurrencyData()
                {
                    CurrencyId = currencyId,
                    CurrencyAmount = _amountsById[currencyId],
                });
            }
        }

        #region Queries

        public static int GetCurrencyAmount(CurrencyAsset currencyAsset)
        {
            var ctx = CurrencyManager.GetContext<CurrencyManager>();
            if (ctx == null) return 0;
            if (ctx._amountsById.ContainsKey(currencyAsset.CurrencyId))
            {
                return ctx._amountsById[currencyAsset.CurrencyId];
            }
            else
            {
                return 0;
            }
        }
        
        public static void AddCurrency(CurrencyAsset currencyAsset, int amount)
        {
            int currAmount = GetCurrencyAmount(currencyAsset);
            SetCurrency(currencyAsset, currAmount + amount);
        }
        
        [Button("Add Currency")]
        private void _AddCurrency(CurrencyAsset currencyAsset, int amount)
        {
            int currAmount = GetCurrencyAmount(currencyAsset);
            SetCurrency(currencyAsset, currAmount + amount);
        }
        
        
        public static void RemoveCurrency(CurrencyAsset currencyAsset, int amount)
        {
            int currAmount = GetCurrencyAmount(currencyAsset);
            SetCurrency(currencyAsset, currAmount - amount);
        }
        
        [Button("Remove Currency")]
        private void _RemoveCurrency(CurrencyAsset currencyAsset, int amount)
        {
            int currAmount = GetCurrencyAmount(currencyAsset);
            SetCurrency(currencyAsset, currAmount - amount);
        }
        
        public static void SetCurrency(CurrencyAsset currencyAsset, int amount)
        {
            var ctx = GetContext<CurrencyManager>();
            if (ctx == null) return;
            ctx._SetCurrency(currencyAsset, amount);
        }
        
        [Button("Set Currency")]
        private void _SetCurrency(CurrencyAsset currencyAsset, int amount)
        {
            _amountsById[currencyAsset.CurrencyId] = amount;
            TriggerCurrencyUpdatedEvent(currencyAsset);
            SaveState();
        }
        #endregion
    }
}