using System;
using Kuantech.Core.HyperCasual;
using Kuantech.Core.Store;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    /// <summary>
    /// A simple price tag for progressable items.Doesn't support multiple currency type. For that, see PricePanel
    /// </summary>
    public class UpgradePriceTag : MonoBehaviour
    {
    
        [SerializeField] private Image CurrencyIcon;
        [SerializeField] private TMP_Text PriceText;
        [NonSerialized] public ProgressableDataAsset ProgressableDataAsset;
        
        /// <summary>
        /// Sets the price for next upgrade of the ProgressableDataAsset.
        /// </summary>
        /// <param name="dataAsset"></param>
        public void SetProgressible(ProgressableDataAsset dataAsset)
        {
            ProgressableDataAsset = dataAsset;
            
            int currentRank = ProgressionManager.GetCurrentRank(dataAsset);
            BuyableInfo buyableInfo = ProgressableDataAsset.GetBuyableInfo();
            if (buyableInfo.PricesInfo.IsNullOrEmpty())
            {
                if (CurrencyIcon != null) CurrencyIcon.gameObject.SetActive(false);
                if (PriceText != null) PriceText.gameObject.SetActive(false);
                return;
            }
            if (CurrencyIcon != null) CurrencyIcon.gameObject.SetActive(true);
            if (PriceText != null) PriceText.gameObject.SetActive(true);
            CurrencyAsset currencyAsset = buyableInfo.PricesInfo[0].CurrencyAsset;
           
            int price = buyableInfo.GetPrice(currencyAsset.GetId(), currentRank + 1, currentRank);
            if (CurrencyIcon != null)
            {
                CurrencyIcon.sprite = currencyAsset.GetIcon();
            }

            if (PriceText != null)
            {
                PriceText.text = price.Stringfy();
            }
        }
    }
}