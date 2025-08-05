using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Utils;
using Unity.VisualScripting;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class RewardsPanel : MonoBehaviour
    {
        [SerializeField] private RewardIndicator RewardIndicatorPrefab;
        [SerializeField] private RectTransform IndicatorsParent;
        [SerializeField] private float Delay = 0.15f;

        private List<Reward> _rewards;
        private IEnumerator _currentRoutine = null;
        
        public void SetRewards(List<Reward> rewards)
        {
            _rewards = rewards;
        }

        private void OnEnable()
        {
            ShowRewards(_rewards);
        }

        public void ShowRewards(List<Reward> rewards)
        {
            //Clear existing ones
            IndicatorsParent.DestroyAllChildren();
            
            if (rewards == null || rewards.Count == 0)
            {
                return;
            }
            if(_currentRoutine != null)
            {
                StopCoroutine(_currentRoutine);
            }

            _currentRoutine = ShowRewardsCoroutine(_rewards);
            StartCoroutine(_currentRoutine);
        }

        private IEnumerator ShowRewardsCoroutine(List<Reward> rewards)
        {
            yield return new WaitForNextFrameUnit();
            foreach (var reward in rewards)
            {
                if (reward == null) continue;

                RewardIndicator indicator = Instantiate(RewardIndicatorPrefab, transform);
                indicator.transform.SetParent(IndicatorsParent);
                indicator.SetReward(reward);
                yield return new WaitForSeconds(Delay);
            }
            _currentRoutine = null;
        }
    }
}