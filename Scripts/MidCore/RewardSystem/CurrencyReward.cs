using System;
using Kuantech.Core;
using Kuantech.Core.Store;

namespace Kuantech.Midcore
{
    [Serializable]
    public class CurrencyReward : Reward
    {
        // Id, not a direct CurrencyAsset reference -- Rewards round-trip through JSON (see
        // ArenaData.RewardData / IJsonDeserializable), and JsonUtility can't carry a UnityEngine.Object
        // reference. Resolved via CurrencyManager.GetCurrencyAssetById wherever the actual asset is needed.
        public string CurrencyId;
        public int CurrencyAmount;
        public override void EarnReward()
        {
            CurrencyManager.AddCurrency(CurrencyId, CurrencyAmount);
        }

        public override MetadataAsset GetMetadataAsset()
        {
            return CurrencyManager.GetCurrencyAssetById(CurrencyId);
        }

        public override int GetAmount()
        {
            return CurrencyAmount;
        }
    }
}