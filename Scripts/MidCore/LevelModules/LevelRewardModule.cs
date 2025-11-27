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
        protected List<Reward> Rewards;
        
        [SerializeReference]
        protected List<Reward> FailedRewards;
        
        public override void OnLevelStateChange(LevelStateChangeData changeData)
        {
            if (changeData.NewState == LevelState.Completed)
            {
                EarnRewards();
                SetRewardsUI(GetRewardsPanelFromCompletePanel(), GetRewards());
            }else if (changeData.NewState == LevelState.Failed)
            {
                EarnFailedRewards();
                SetRewardsUI(GetRewardsPanelFromFailedPanel(), GetFailedRewards());
            }
        }

        protected virtual void EarnRewards()
        {
            foreach (var reward in GetRewards())
            {
                reward.EarnReward();
            }
        }

        protected virtual void EarnFailedRewards()
        {
            foreach (var reward in GetFailedRewards())
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

        public virtual List<Reward> GetFailedRewards()
        {
            return FailedRewards;
        }

        public virtual List<Reward> GetRewards()
        {
            return Rewards;
        }

        public void SetRewards(List<Reward> rewards)
        {
            Rewards = rewards;
        }
        
        
        public void SetFailedRewards(List<Reward> rewards)
        {
            FailedRewards = rewards;
        }
        
        protected virtual RewardsPanel GetRewardsPanelFromCompletePanel()
        {
            Level parentLevel = ParentLevel;
            if (parentLevel.LevelUI == null) return null;
            CompletePanel completePanel = parentLevel.LevelUI.GetUIElementByType<CompletePanel>();
            if (completePanel == null) return null;
            return completePanel.RewardsPanel;
        }

        protected virtual RewardsPanel GetRewardsPanelFromFailedPanel()
        {
            Level parentLevel = ParentLevel;
            if (parentLevel.LevelUI == null) return null; 
            LevelFailPanel failPanel = parentLevel.LevelUI.GetUIElementByType<LevelFailPanel>();
            if (failPanel == null) return null;
            return failPanel.RewardsPanel;
        }
    }
}