using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Core.HyperCasual;
using Kuantech.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.HyperCasual.UI
{
    public class LevelCompletePanel : UIMenu
    {
        [SerializeField] private Button CompleteLevelButton;
        [SerializeField] protected Effect ShowEffect;
        [SerializeField] private List<CurrencyIndicator> CurrencyIndicators;

        public override void Show()
        {
            base.Show();
            if(ShowEffect != null) ShowEffect.Play();
            SetEarnings();
        }

        public void Initialize()
        {
            CompleteLevelButton.onClick.AddListener(OnCompleteLevelButton);
        }

        protected virtual void SetEarnings()
        {
            if (CurrencyIndicators == null) return;
            Level currentLevel = LevelManager.GetCurrentLevel();
            // foreach (var indicator in CurrencyIndicators)
            // {
            //     int earnedCurrency = currentLevel.GetEarnedCurrency(indicator.GetCurrencyId()).Amount;
            //     indicator.gameObject.SetActive(earnedCurrency > 0);
            //     indicator.SetAmount(earnedCurrency);
            // }
        }
        
        private void OnCompleteLevelButton()
        {
            LevelManager.GetContext<LevelManager>().CompleteLevel();
        }
    }
}