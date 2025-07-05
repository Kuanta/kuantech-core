using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
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

        private Dictionary<string, CurrencyAsset> _assetsById = new Dictionary<string, CurrencyAsset>();
        
        //Event
        public UnityAction<CurrencyData> CurrencyUpdated;
        public UnityAction<string> CurrencyUpdatedById;
        
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);

            _amountsById = new Dictionary<string, int>();
            foreach (var asset in CurrencyAssets)
            {
                _amountsById[asset.GetId()] = 0; //Default
                _assetsById[asset.GetId()] = asset;
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
            TriggerCurrencyUpdatedEvent(asset.GetId());
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
            if (currencyAsset == null) return 0;
            return GetCurrencyAmount(currencyAsset.GetId());
        }

        public static int GetCurrencyAmount(string currencyId)
        {
            var ctx = CurrencyManager.GetContext<CurrencyManager>();
            if (ctx == null) return 0;
            if (ctx._amountsById.ContainsKey(currencyId))
            {
                return ctx._amountsById[currencyId];
            }
            else
            {
                return 0;
            }
        }
        [Button("Add Currency")]
        public static void AddCurrency(CurrencyAsset currencyAsset, int amount)
        {
            if (currencyAsset == null) return;
            AddCurrency(currencyAsset.GetId(), amount);
        }

        public static void AddCurrency(string currencyId, int amount)
        {
            int currAmount = GetCurrencyAmount(currencyId);
            SetCurrency(currencyId, currAmount + amount);
        }

        [Button("Remove Currency")]
        public static void RemoveCurrency(CurrencyAsset currencyAsset, int amount)
        {
            int currAmount = GetCurrencyAmount(currencyAsset);
            RemoveCurrency(currencyAsset.GetId(), amount);
        }

        public static void RemoveCurrency(string currencyId, int amount)
        {
            var ctx = GetContext<CurrencyManager>();
            int currAmount = GetCurrencyAmount(currencyId);
            SetCurrency(currencyId,currAmount - amount);
        }
        
        public static void SetCurrency(CurrencyAsset currencyAsset, int amount)
        {
            SetCurrency(currencyAsset.GetId(), amount);
        }
        
        [Button("Set Currency")]
        public static void SetCurrency(string currencyId, int amount)
        {
            var ctx = GetContext<CurrencyManager>();
            if (ctx == null) return;
            ctx._amountsById[currencyId] = amount;
            CurrencyAsset asset = GetCurrencyAssetById(currencyId);
            if (asset == null)
            {
                Debug.LogWarning($"No currency asset with id {currencyId}");
                return;
            }
            ctx.TriggerCurrencyUpdatedEvent(GetCurrencyAssetById(currencyId));
            ctx.SaveState();
        }

        public static CurrencyAsset GetCurrencyAssetById(string id)
        {
            var ctx = GetContext<CurrencyManager>();
            if (ctx._assetsById.ContainsKey(id)) return ctx._assetsById[id];
            return null;
        }
        #endregion
    }
}