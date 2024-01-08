using Kuantech.HyperCasual;
using Kuantech.HyperCasual.UI;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.ArcadeIdle.UI
{
    public class UpgradeButton : MonoBehaviour {
        [SerializeField] private Button Button;
        [SerializeField] private UpgradeData UpgradeData;
        [SerializeField] private TMP_Text LevelText;
        [SerializeField] private TMP_Text TitleText;
        [SerializeField] private Image UpgradeIcon;
        [SerializeField] private CurrencyIndicator CurrencyIndicator;

        private void Start()
        {
            Button.onClick.AddListener(OnBuyButtonClicked);
            UpgradeManager um = UpgradeManager.GetContext<UpgradeManager>();
            if(um == null)
            {
                Debug.LogError("Upgrade manager is null but you are trying to add an upgrade button");
                return;
            }
            if(TitleText != null) TitleText.text = UpgradeData.UpgradeName;
            if(UpgradeIcon != null && UpgradeData.UpgradeIcon)
            {
                UpgradeIcon.sprite = UpgradeData.UpgradeIcon;
            }
            UpdateLevel(UpgradeManager.GetCurrentUpgradeLevel(UpgradeData.UpgradeId));
            CurrencyIndicator.SetCurrency(UpgradeData.CurrencyData);
            CurrencyIndicator.SetAmount(UpgradeData.GetUpgradePrice());
        }

        private void OnBuyButtonClicked()
        {
            UpgradeManager um = UpgradeManager.GetContext<UpgradeManager>();
            bool success = um.BuyUpgrade(UpgradeData.UpgradeId);
            if(!success) return;
            UpdateLevel(UpgradeManager.GetCurrentUpgradeLevel(UpgradeData.UpgradeId));
        }

        /// <summary>
        /// Updates the level in the ui
        /// </summary>
        /// <param name="level"></param>
        private void UpdateLevel(int level)
        {
            LevelText.text = level.Stringfy();
        }
    }
}