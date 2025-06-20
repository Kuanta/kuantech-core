using System;
using Kuantech.Core;
using Kuantech.Core.Store;

namespace Kuantech.Midcore
{
    [Serializable]
    public class CurrencyReward : Reward
    {
        public CurrencyAsset CurrencyAsset;
        public int CurrencyAmount;
        public override void EarnReward()
        {
            CurrencyManager.AddCurrency(CurrencyAsset, CurrencyAmount);
        }

        public override MetadataAsset GetMetadataAsset()
        {
            return CurrencyAsset;
        }

        public override int GetAmount()
        {
            return CurrencyAmount;
        }
    }
}