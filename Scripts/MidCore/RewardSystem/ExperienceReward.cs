using System;
using Kuantech.Core;

namespace Kuantech.Midcore
{
    [Serializable]
    public class ExperienceReward : Reward
    {
        public int ExperienceAmount;
        
        public override void EarnReward()
        {
            ProgressionManager pm = ProgressionManager.GetContext<ProgressionManager>();
            pm.AddExperience(ExperienceAmount);
        }
        
        public override int GetAmount()
        {
            return (int) ExperienceAmount;
        }
    }
}