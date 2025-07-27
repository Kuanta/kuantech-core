using System;
using Kuantech.Core.UI;
using TMPro;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class UpgradeButtonPanel : UIElement
    {
        [SerializeField] private TMP_Text UpgradeName;
        [SerializeField] private TMP_Text UpgradeDescription;

        [SerializeField] private UpgradeButton UpgradeButton;
        
        [NonSerialized] public UpgradeTreeButton ParentTreeButton;

        public void Initialize(UpgradeTreeButton parentButton)
        {
            ParentTreeButton = parentButton;
            UpgradeButton.OnUpgradePurchased += parentButton.OnRankPurchased;
        }
        
        public void SetProgressable(ProgressableDataAsset progressableDataAsset, int rank)
        {
            if (UpgradeName != null)
            {
                if (progressableDataAsset is TraitUpgradeProgressable traitUpgradeProgressable)
                {
                    UpgradeName.text = traitUpgradeProgressable.GetName(rank);
                }
                else
                {
                    UpgradeName.text = progressableDataAsset.GetName();
                }
            }
            if (UpgradeDescription != null) UpgradeDescription.text = progressableDataAsset.GetDescription();
            UpgradeButton.SetProgressable(progressableDataAsset, rank);
        }

        public void SetPurchasedState()
        {
            if (UpgradeButton != null)
            {
                UpgradeButton.gameObject.SetActive(false);
            }
        }
        
        public void SetPurchasableState()
        {
            if (UpgradeButton != null)
            {
                UpgradeButton.gameObject.SetActive(true);
            }
        }
    }
}