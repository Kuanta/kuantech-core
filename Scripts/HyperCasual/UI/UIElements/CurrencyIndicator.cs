using TMPro;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class CurrencyIndicator : MonoBehaviour
    {
        [SerializeField] private TMP_Text CurrencyAmount;

        public void SetAmount(int amount)
        {
            CurrencyAmount.text = amount.ToString();
        }
    }
}