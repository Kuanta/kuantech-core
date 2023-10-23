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
        
        public bool BuyItem(string id)
        {
            if (!_buyables.ContainsKey(id)) return true; //If doesn't have entry, it means its free
            BuyableInfo info = _buyables[id];
            GameStateManager gsm = GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager;
            bool hasCurrency = true;
            for(int i=0;i< info.CurrencyIds.Count;++i)
            {
                string currencyId = info.CurrencyIds[i];
                float availableCurrency = gsm.GetCurrency(currencyId).Amount;
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
                gsm.RemoveCurrency(currencyId, info.Prices[i]);
                Debug.LogError($"Removing {info.Prices[i]} {info.CurrencyIds[i]}");
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
    }
}