using System.Collections.Generic;
using Kuantech.Core.FX;
using Kuantech.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual.UI
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
            Level currentLevel = ((HCGameManager) HCGameManager.Instance).CurrentLevel;
            foreach (var indicator in CurrencyIndicators)
            {
                int earnedCurrency = currentLevel.GetEarnedCurrency((int) indicator.CurrencyId).Amount;
                indicator.gameObject.SetActive(earnedCurrency > 0);
                indicator.SetAmount(earnedCurrency);
            }
        }
        
        private void OnCompleteLevelButton()
        {
            ((HCGameManager)GameManager.Instance).CompleteLevel();
        }
    }
}