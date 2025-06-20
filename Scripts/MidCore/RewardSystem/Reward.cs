using System;
using Kuantech.Core;

namespace Kuantech.Midcore
{
    [Serializable]
    public class Reward
    {
        
        /// <summary>
        /// Earns the reward
        /// </summary>
        public virtual void EarnReward()
        {
            throw new NotImplementedException();
        }

        public virtual MetadataAsset GetMetadataAsset()
        {
            throw new NotImplementedException();
        }

        public virtual int GetAmount()
        {
            throw new NotImplementedException();
        }
    }
}