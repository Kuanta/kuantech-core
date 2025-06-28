using System.Collections.Generic;
using System.Linq;
using Kuantech.Core.HyperCasual;
using Kuantech.HyperCasual.UI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    /// <summary>
    /// A UI element that displays price tags for an item
    /// </summary>
    public class PricePanel : MonoBehaviour
    {
        [SerializeField] private List<PriceTag> PriceTags;
        [SerializeField] private PriceTag PriceTagPrefab;

        /// <summary>
        /// Sets all the price tags
        /// </summary>
        /// <param name="buyableInfo"></param>
        /// <param name="rank"></param>
        /// <param name="startRank"></param>
        public void SetPriceInfo(BuyableInfo buyableInfo, int rank, int startRank)
        {
            if (buyableInfo == null)
            {
                if (PriceTags.IsNullOrEmpty()) return;
                foreach (var tag in PriceTags)
                {
                    tag.gameObject.SetActive(false);
                }

                return;
            }
            
            //Do we have enough price tags?
            int neededTagCount = !PriceTags.IsNullOrEmpty() ? buyableInfo.PricesInfo.Count - PriceTags.Count :  buyableInfo.PricesInfo.Count;
            if (neededTagCount > 0 && PriceTagPrefab == null)
            {
                Debug.LogWarning("PriceTagPrefab is null. Cannot create new price tags.");
            }
            else
            {
                for (int i = 0; i < neededTagCount; ++i)
                {
                    PriceTag newTag = Instantiate(PriceTagPrefab, PriceTagPrefab.transform.parent);
                    newTag.gameObject.SetActive(true);
                    PriceTags.Add(newTag);
                }
            }

            for (int i = 0; i < PriceTags.Count; ++i)
            {
                PriceTags[i].gameObject.SetActive(i < buyableInfo.PricesInfo.Count);
            }
            for (int i = 0; i < buyableInfo.PricesInfo.Count; ++i)
            {
                PriceInfo priceInfo = buyableInfo.PricesInfo[i];
                if(!PriceTags.IsValidIndex(i)) continue;
                PriceTag tag = PriceTags[i];
                tag.SetCurrency(priceInfo.CurrencyAsset);
                tag.SetPriceText(priceInfo.GetPrice(rank, startRank));

            }
        }
    }
}