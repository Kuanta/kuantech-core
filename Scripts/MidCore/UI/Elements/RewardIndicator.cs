using Kuantech.Core;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    public class RewardIndicator : MonoBehaviour
    {
        [SerializeField] private Image ColorTintImage;
        [SerializeField] private Image Icon;
        [SerializeField] private TMP_Text AmountText;

        public void SetReward(Reward reward)
        {
            MetadataAsset metadataAsset = reward.GetMetadataAsset();

            if (Icon != null)
            {
                Icon.sprite = metadataAsset.GetIcon();
            }

            if (ColorTintImage != null)
            {
                ColorTintImage.color = metadataAsset.MainColor;
            }

            int amount = reward.GetAmount();
            if (AmountText != null)
            {
                AmountText.text = amount.Stringfy();
                AmountText.gameObject.SetActive(amount > 1);
            }
        }
    }
}