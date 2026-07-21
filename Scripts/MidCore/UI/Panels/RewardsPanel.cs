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

            // Use the argument, not the field: the guards above check the argument, so reading the field
            // here would let a caller pass a valid list and still get the previous one (or null).
            _rewards = rewards;
            _currentRoutine = ShowRewardsCoroutine(rewards);
            StartCoroutine(_currentRoutine);
        }

        private IEnumerator ShowRewardsCoroutine(List<Reward> rewards)
        {
            yield return new WaitForNextFrameUnit();
            foreach (var reward in rewards)
            {
                if (reward == null) continue;

                // Straight into the parent: spawning elsewhere and reparenting keeps world scale/position,
                // which a layout group then fights — indicators end up mis-sized or offset.
                RewardIndicator indicator = Instantiate(RewardIndicatorPrefab, IndicatorsParent);
                indicator.SetReward(reward);
                yield return new WaitForSeconds(Delay);
            }
            _currentRoutine = null;
        }
    }
}