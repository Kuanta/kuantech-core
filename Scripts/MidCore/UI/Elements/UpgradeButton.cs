using System;
using Kuantech.Core.FX;
using Kuantech.Core.UI;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Midcore.UI
{
    public class UpgradeButton : MonoBehaviour, KtButton.IUIButtonAction
    {
        [SerializeField] private UpgradePriceTag PriceTag;
        [NonSerialized] public ProgressableDataAsset ProgressableToUpgrade;
        public UnityAction OnUpgradePurchased;
        [KTTag("AudioTag")] public int PurchasedSoundTag;
        [KTTag("AudioTag")] public int CantPurchaseSoundTag;
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
                AudioLibrary.PlaySoundByTag(PurchasedSoundTag);
            }
            else
            {
                AudioLibrary.PlaySoundByTag(CantPurchaseSoundTag);
            }
        }
    }
}