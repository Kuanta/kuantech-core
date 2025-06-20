using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core.Store;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    [Serializable]
    public struct PriceInfo
    {
        public CurrencyAsset CurrencyAsset;
        public int BasePrice;
        public int PricePerRank;
        
        /// <summary>
        /// Returns the price needed to go to the given rank from starting rank.
        /// </summary>
        /// <param name="rank"></param>
        /// <param name="startRank"></param>
        /// <returns></returns>
        public int GetPrice(int rank=0, int startRank=-1)
        {
            if (rank < startRank) return 0;
            return GetPriceForRank(rank) - GetPriceForRank(startRank);
        }
        
        /// <summary>
        /// Returns the total price to reach the given rank
        /// </summary>
        /// <param name="rank"></param>
        /// <returns></returns>
        private int GetPriceForRank(int rank)
        {
            if (rank < 0) return 0;
            return BasePrice + PricePerRank * rank;
        }
    }
    
    [Serializable]
    public class BuyableInfo
    {
        public string Id;
        public List<PriceInfo> PricesInfo;

        /// <summary>
        /// Gets the price of the item in the given currency. The rank is used to calculate the price.
        /// </summary>
        /// <param name="currencyId"></param>
        /// <param name="rank"></param>
        /// <param name="startRank">Starting rank. -1 means not unlocked</param>
        /// <returns></returns>
        public int GetPrice(string currencyId, int rank=0, int startRank=-1)
        {
            if(PricesInfo.IsNullOrEmpty()){
                return 0;
            }
            for(int i=0;i<PricesInfo.Count;++i)
            {
                if(PricesInfo[i].CurrencyAsset.GetId() == currencyId)
                {
                    return PricesInfo[i].GetPrice(rank, startRank);
                }
            }
            return 0;
        }
    }
    
    /// <summary>
    /// Handles the pricings and currency transactions. Doesn't any data about what is sold. It only deals the money
    /// </summary>
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

        #region Queries

        public bool IsListed(string id)
        {
            if (_buyables == null) return false;
            if (_buyables.Count == 0) return false;
            if (!_buyables.ContainsKey(id)) return false; //If doesn't have entry
            return true;
        }
        
        public bool CanBeBought(BuyableInfo info, int rank=0, int startRank=-1)
        {
            bool hasCurrency = true;
            for(int i=0;i< info.PricesInfo.Count;++i)
            {
                string currencyId = info.PricesInfo[i].CurrencyAsset.GetId();
                float availableCurrency = CurrencyManager.GetCurrencyAmount(currencyId);
                if (availableCurrency < info.PricesInfo[i].GetPrice(rank, startRank))
                {
                    hasCurrency = false;
                    break;
                }
            }
            if(!hasCurrency) return false;
            return true;
        }

        #endregion
     
        public virtual bool BuyItem(string id, int rank=0, int startRank=-1)
        {
            if (!_buyables.ContainsKey(id))
            {
                return true;//Doesn't have an entry
            }
            BuyableInfo info = _buyables[id];
            return BuyItem(info, rank, startRank);
        }

        public virtual bool BuyItem(BuyableInfo info, int rank = 0, int startRank = -1)
        {
            if (info == null) return false; //No entry
            if (!CanBeBought(info, rank, startRank)) return false;
            for (int i = 0; i < info.PricesInfo.Count; ++i)
            {
                string currencyId = info.PricesInfo[i].CurrencyAsset.GetId();
                int price = info.PricesInfo[i].GetPrice(rank, startRank);
                CurrencyManager.RemoveCurrency(currencyId, price);
            }
            return true;
        }
        
        public BuyableInfo GetBuyableInfo(string buyableId)
        {
            if(!_buyables.ContainsKey(buyableId))
            {
                return null;
            }
            return _buyables[buyableId]; 
        }
        
        /// <summary>
        /// Checks if there are enough currency.
        /// </summary>
        /// <param name="currencyAsset"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static bool HasCurrency(CurrencyAsset currencyAsset, int amount)
        {
            int heldAmount = CurrencyManager.GetCurrencyAmount(currencyAsset);
            if (amount <= heldAmount) return true;
            return false;
        }
    }
}