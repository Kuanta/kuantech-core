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
        public int CurrencyId;
        public int Price;
    }
    public class StoreManager : HCSubManager
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
            if (!_buyables.ContainsKey(id)) return false;
            BuyableInfo info = _buyables[id];
            float availableCurrency = ((HCGameManager)ParentManager).GetCurrency(info.CurrencyId).Amount;
            if (availableCurrency < info.Price) return false;
            ((HCGameManager)ParentManager).RemoveCurrency(info.CurrencyId, info.Price);
            return true;
        }

        public int GetPrice(string id)
        {
            if (!_buyables.ContainsKey(id)) return -1;
            return _buyables[id].Price;
        }
        
    }
}