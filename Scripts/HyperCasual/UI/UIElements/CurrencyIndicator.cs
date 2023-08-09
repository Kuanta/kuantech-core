using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class CurrencyIndicator : MonoBehaviour
    {
        public int CurrencyId;
        [SerializeField] private TMP_Text CurrencyAmount;

        public virtual void SetAmount(int amount)
        {
            CurrencyAmount.text = amount.Stringfy();
        }
    }
}