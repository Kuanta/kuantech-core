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
        
        [SerializeReference]
        public List<Reward> FailedRewards;
        
        public override void OnLevelStateChange(LevelStateChangeData changeData)
        {
            if (changeData.NewState == LevelState.Completed)
            {
                EarnRewards();
                SetRewardsUI(GetRewardsPanelFromCompletePanel(), Rewards);
            }else if (changeData.NewState == LevelState.Failed)
            {
                EarnFailedRewards();
                SetRewardsUI(GetRewardsPanelFromFailedPanel(), FailedRewards);
            }
        }

        private void EarnRewards()
        {
            foreach (var reward in Rewards)
            {
                reward.EarnReward();
            }
        }

        private void EarnFailedRewards()
        {
            foreach (var reward in FailedRewards)
            {
                reward.EarnReward();
            }
        }
        
        /// <summary>
        /// Sets the rewards
        /// </summary>
        /// <param name="rewardsPanel"></param>
        private void SetRewardsUI(RewardsPanel rewardsPanel, List<Reward> rewards)
        {
            if (rewardsPanel == null) return;
            rewardsPanel.SetRewards(rewards);
        }

        private RewardsPanel GetRewardsPanelFromCompletePanel()
        {
            Level parentLevel = ParentLevel;
            if (parentLevel.LevelUI == null) return null;
            CompletePanel completePanel = parentLevel.LevelUI.GetUIElementByType<CompletePanel>();
            if (completePanel == null) return null;
            return completePanel.RewardsPanel;
        }

        private RewardsPanel GetRewardsPanelFromFailedPanel()
        {
            Level parentLevel = ParentLevel;
            if (parentLevel.LevelUI == null) return null; 
            LevelFailPanel failPanel = parentLevel.LevelUI.GetUIElementByType<LevelFailPanel>();
            if (failPanel == null) return null;
            return failPanel.RewardsPanel;
        }
    }
}