using Kuantech.Core.HyperCasual;
using Kuantech.Core.Store;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.HyperCasual.UI
{
    public class PriceTag : MonoBehaviour
    {
        [SerializeField] private string BuyableId;
        [SerializeField] protected string CurrencyId;
        [SerializeField] private TMP_Text PriceText;
        [SerializeField] private Image CurrencyIcon;
        [Tooltip("If price is 0, this text will be shown as long as its not empty")]
        [SerializeField] private string FreeText;        
        
        /// <summary>
        /// Sets the price text by gettign it from the store manager
        /// </summary>
        public virtual void SetPrice()
        {
            if (BuyableId.IsNullOrEmpty()) return;
            BuyableInfo info = StoreManager.GetContext<StoreManager>().GetBuyableInfo(BuyableId);
            SetPrice(info.GetPrice(CurrencyId));
        }

        /// <summary>
        /// Sets the price text directly to the given argument
        /// </summary>
        /// <param name="price"></param>
        public void SetPrice(int price)
        {
            if (price <= 0 && !FreeText.IsNullOrEmpty())
            {
                PriceText.text = FreeText;
            }
            else
            {
                PriceText.text = price.Stringfy();
            }
        }
        
        /// <summary>
        /// Sets the currency 
        /// </summary>
        /// <param name="currencyId"></param>
        public void SetCurrency(CurrencyAsset currencyAsset)
        {
            //Set currency
            CurrencyId = currencyAsset.GetId();
            if (CurrencyIcon != null)
            {
                CurrencyIcon.sprite = currencyAsset.Icon;
            }
        }
    }
}