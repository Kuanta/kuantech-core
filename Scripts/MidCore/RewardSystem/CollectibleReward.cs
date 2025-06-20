using System;
using Kuantech.Core;

namespace Kuantech.Midcore
{
    [Serializable]
    public class CollectibleReward : Reward
    {
        public ProgressableDataAsset CollectibleData;
        public override void EarnReward()
        {
            base.EarnReward();
            if (ProgressionManager.IsProgressibleUnlocked(CollectibleData)) return;
            ProgressionManager.SetRank(CollectibleData, 0);
        }
        
        public override int GetAmount()
        {
            return 1; // Collectible rewards are typically singular
        }
        
        public override MetadataAsset GetMetadataAsset()
        {
            return CollectibleData;
        }
    }
}