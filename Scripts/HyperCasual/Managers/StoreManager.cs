using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    [Serializable]
    public struct BuyableInfo
    {
        public string Id;
        public List<string> CurrencyIds;
        public List<int> Prices;

        public int GetPrice(string CurrencyId)
        {
            if(CurrencyIds == null){
                return 0;
            }
            for(int i=0;i<CurrencyIds.Count;++i)
            {
                if(CurrencyIds[i] == CurrencyId)
                {
                    return Prices[i];
                }
            }
            return 0;
        }
    }
    public class StoreManager : SubManager
    {
        private Dictionary<string, BuyableInfo> _buyables;
        [SerializeField] private StoreListing StoreListing;

        public override async UniTask Initialize(GameManager hcGameManager)
        {
            await base.Initialize(hcGameManager);
            
            //todo: We may want to read prices from xml in the future
            if (StoreListing == null || StoreListing.Buyables == null) return;
            _buyables = new Dictionary<string, BuyableInfo>();
            foreach (var buyable in StoreListing.Buyables)
            {
                _buyables[buyable.Id] = buyable;
            }
        }
        
        public virtual bool BuyItem(string id)
        {
            if (!_buyables.ContainsKey(id)) return true; //If doesn't have entry, it means its free
            BuyableInfo info = _buyables[id];
            GameStateManager gsm = GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager;
            bool hasCurrency = true;
            for(int i=0;i< info.CurrencyIds.Count;++i)
            {
                string currencyId = info.CurrencyIds[i];
                float availableCurrency = 0;
                Debug.LogError("FI HERE!");
                if (availableCurrency < info.Prices[i])
                {
                    hasCurrency = false;
                    break;
                }
            }
            if(!hasCurrency) return false;

            for (int i = 0; i < info.CurrencyIds.Count; ++i)
            {
                string currencyId = info.CurrencyIds[i];
               // gsm.RemoveCurrency(currencyId, info.Prices[i]);
            }
            return true;
        }

        public BuyableInfo GetBuyableInfo(string buyableId)
        {
            if(!_buyables.ContainsKey(buyableId))
            {
                return new BuyableInfo{
                    
                };
            }
            return _buyables[buyableId]; 
        }
        
        /// <summary>
        /// Checks if there are enough currency.
        /// </summary>
        /// <param name="currencyData"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static bool HasCurrency(CurrencyData currencyData, int amount)
        {
            GameStateManager gsm = GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager;
            if (gsm == null) return false;
            int heldAmount = 0;
            Debug.LogError("FIX HERE");
            if (amount <= heldAmount) return true;
            return false;
        }
        
        /// <summary>
        /// Adds currency of given type and amount
        /// </summary>
        /// <param name="currencyData"></param>
        /// <param name="amount"></param>
        public static void AddCurrency(CurrencyData currencyData, int amount)
        {
            GameStateManager gsm = GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager;
            if (gsm == null) return;
            //gsm.AddCurrency(currencyData.CurrencyId, amount);
        }
        
        /// <summary>
        /// Removes the given amount from given currency.
        /// </summary>
        /// <param name="currencyData"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static bool RemoveCurrency(CurrencyData currencyData, int amount)
        {
            GameStateManager gsm = GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager;
            if (gsm == null) return false;
            int heldAmount = 0; //todo: Fix here
            if (heldAmount >= amount)
            {
                //gsm.RemoveCurrency(currencyData.CurrencyId, amount);
                return true;
            }
            return false;
        }
    }
}