using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class RewardsPanel : MonoBehaviour
    {
        [SerializeField] private RewardIndicator RewardIndicatorPrefab;
        [SerializeField] private RectTransform IndicatorsParent;
        
        public void ShowRewards(List<Reward> rewards)
        {
            //Clear existing ones
            IndicatorsParent.DestroyAllChildren();
            
            if (rewards == null || rewards.Count == 0)
            {
                return;
            }

            foreach (var reward in rewards)
            {
                if (reward == null) continue;

                RewardIndicator indicator = Instantiate(RewardIndicatorPrefab, transform);
                indicator.transform.SetParent(IndicatorsParent);
                indicator.SetReward(reward);
                
                //Do the animation?
            }
        }
    }
}