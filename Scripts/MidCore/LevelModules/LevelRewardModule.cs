using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.UI;
using Kuantech.Midcore.UI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    public class LevelRewardModule : LevelModule
    {
        [SubclassSelector]
        [SerializeReference]
        public List<Reward> Rewards;
        
        public override void OnLevelStateChange(LevelStateChangeData changeData)
        {
            if (changeData.NewState == LevelState.Completed)
            {
                EarnRewards();
                SetRewardsUI();
            }
        }

        private void EarnRewards()
        {
            foreach (var reward in Rewards)
            {
                reward.EarnReward();
            }
        }

        private void SetRewardsUI()
        {
            Level parentLevel = ParentLevel;
            if (parentLevel.LevelUI == null) return;
            CompletePanel completePanel = parentLevel.LevelUI.GetUIElementByType<CompletePanel>() as CompletePanel;
            if (completePanel == null) return;
            RewardsPanel rewardsPanel = completePanel.RewardsPanel;
            if (rewardsPanel == null) return;
            rewardsPanel.ShowRewards(Rewards);
        }
    }
}