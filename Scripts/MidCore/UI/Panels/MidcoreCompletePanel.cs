using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Core.UI;
using Kuantech.HyperCasual.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    public class MidcoreCompletePanel :CompletePanel
    {
        [SerializeField] private List<CurrencyIndicator> CurrencyIndicators;

        public override void Open()
        {
            base.Open();
            //Show Rewards
        }


        protected virtual void SetEarnings()
        {
            if (CurrencyIndicators == null) return;
            Level currentLevel = LevelManager.GetCurrentLevel();
            Debug.Log("Show rewards");
            // foreach (var indicator in CurrencyIndicators)
            // {
            //     int earnedCurrency = currentLevel.GetEarnedCurrency(indicator.GetCurrencyId()).Amount;
            //     indicator.gameObject.SetActive(earnedCurrency > 0);
            //     indicator.SetAmount(earnedCurrency);
            // }
        }
        
        public override void OnCompleteLevelButton()
        {
            base.OnCompleteLevelButton();
            //Go to main menu
            string menuSceneName = MidcoreGameSceneManager.GetMenuSceneName();
            GameManager.ChangeScene(menuSceneName);
        }
    }
}