using System;
using Kuantech.Core;

namespace Kuantech.Midcore
{
    [Serializable]
    public class ExperienceReward : Reward
    {
        public ProgressableDataAsset ExperienceAsset;
        public float ExperienceAmount;
        
        public override void EarnReward()
        {
            ProgressionManager.AddRankValue(ExperienceAsset, ExperienceAmount);
        }
        
        public override MetadataAsset GetMetadataAsset()
        {
            return ExperienceAsset;
        }

        public override int GetAmount()
        {
            return (int) ExperienceAmount;
        }
    }
}