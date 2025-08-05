using Kuantech.Core;

namespace Kuantech.Midcore
{
    public class PlayerExperienceReward : Reward
    {
        public int ExperienceAmount;
        
        public override void EarnReward()
        {
            ProgressionManager.GetContext<ProgressionManager>()?.AddExperience(ExperienceAmount);
        }

        public override MetadataAsset GetMetadataAsset()
        {
            return ProgressionManager.GetPlayerLevelDataAsset();
        }
        
        public override int GetAmount()
        {
            return ExperienceAmount;
        }
    }
}