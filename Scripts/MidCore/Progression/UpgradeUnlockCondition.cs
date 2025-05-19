using System;
using UnityEngine;

namespace Kuantech.Midcore
{
    [Serializable]
    public class UpgradeUnlockCondition
    {
        public UpgradeDataAsset dependingUpgradeType;
        public int DependingProgressionRank;
        public int RequiredPlayerLevel = 0;
        
        /// <summary>
        /// Checks if this progression can be unlocked
        /// </summary>
        /// <returns></returns>
        public bool IsConditionMet()
        {
            int playerLevel = ProgressionManager.GetPlayerLevel().CurrentLevel;
            RequiredPlayerLevel = Mathf.Max(1, RequiredPlayerLevel); //Can't require 0 or less level
            if (RequiredPlayerLevel > playerLevel) return false;
            if (dependingUpgradeType == null ) return true;
            UpgradeData upgradeData = ProgressionManager.GetUpgradeData(dependingUpgradeType);
            if (upgradeData == null)
            {
                return false;
            }
            int currentRank = ProgressionManager.GetUpgradeData(dependingUpgradeType).CurrentRank;
            if (currentRank < DependingProgressionRank) return false;
            return true;
        }
    }
}