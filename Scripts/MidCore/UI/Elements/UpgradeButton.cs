using System;
using Kuantech.Core.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Midcore.UI
{
    [RequireComponent(typeof(KtButton))]
    public class UpgradeButton : MonoBehaviour, KtButton.IUIButtonAction
    {
        [SerializeField] private UpgradePriceTag PriceTag;
        [NonSerialized] public ProgressableDataAsset ProgressableToUpgrade;
        public UnityAction OnUpgradePurchased;
        private KtButton _ktButton;

        private void Awake()
        {
            _ktButton = GetComponent<KtButton>();
        }
        public void SetProgressable(ProgressableDataAsset progressableDataAsset)
        {
            int currentRank = ProgressionManager.GetCurrentRank(progressableDataAsset);
            SetProgressable(progressableDataAsset, currentRank);
        }
        public void SetProgressable(ProgressableDataAsset progressableDataAsset, int currentRank)
        {
            ProgressableToUpgrade = progressableDataAsset;
            
            if (PriceTag != null)
            {
                PriceTag.gameObject.SetActive(true);
                PriceTag.SetProgressible(progressableDataAsset);
            }
            else
            {
                Debug.LogWarning("PriceTag is not set for UpgradeButton.");
            }
          
        }
        public void OnClick()
        {
            if (ProgressionManager.RankUpUpgrade(ProgressableToUpgrade))
            {
                OnUpgradePurchased?.Invoke();
                if (_ktButton != null) _ktButton.TriggerPositiveEffect();
            }
            else
            {
                if (_ktButton != null) _ktButton.TriggerNegativeEffect();
            }
        }
    }
}