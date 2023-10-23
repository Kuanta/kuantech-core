using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.UI
{
    public class PriceTag : MonoBehaviour
    {
        [SerializeField] private string BuyableId;
        [SerializeField] protected string CurrencyId;
        [SerializeField] private TMP_Text PriceText;

        public virtual void SetPriceText()
        {
            BuyableInfo info = StoreManager.GetContext<StoreManager>().GetBuyableInfo(BuyableId);
            PriceText.text = info.GetPrice(CurrencyId).Stringfy();
        }
    }
}