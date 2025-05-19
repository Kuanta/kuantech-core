using Kuantech.Core.HyperCasual;
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
        
        /// <summary>
        /// Sets the price text by gettign it from the store manager
        /// </summary>
        public virtual void SetPriceText()
        {
            BuyableInfo info = StoreManager.GetContext<StoreManager>().GetBuyableInfo(BuyableId);
            PriceText.text = info.GetPrice(CurrencyId).Stringfy();
        }

        /// <summary>
        /// Sets the price text directly to the given argument
        /// </summary>
        /// <param name="price"></param>
        public void SetPriceText(int price)
        {
            PriceText.text = price.Stringfy();
        }
        /// <summary>
        /// Sets the currency 
        /// </summary>
        /// <param name="currencyId"></param>
        public void SetCurrency(string currencyId)
        {
            //Set currency
            CurrencyId = currencyId;
            
            //Set icon
            UIResourcesManager uiResMan = UIResourcesManager.GetContext<UIResourcesManager>();
            if(uiResMan != null && CurrencyIcon != null)
            {
                Sprite icon = UIResourcesManager.GetCurrencyIcon(CurrencyId);
                if(icon != null) CurrencyIcon.sprite = icon;
            }

        }
    }
}