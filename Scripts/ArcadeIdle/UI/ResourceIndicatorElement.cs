using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.ArcadeIdle.UI
{
    public class ResourceIndicatorElement : MonoBehaviour {
        [SerializeField] private Image ResourceIcon;
        [SerializeField] private TMP_Text AmountText;

        private ResourceData _resourceData;
        public void SetResource(ResourceData resourceData)
        {
            _resourceData = resourceData;
            ResourceIcon.sprite = resourceData.ResourceIcon;
        }

        public void SetAmount(int amount)
        {
            AmountText.text = amount.Stringfy();
        }   
    }
}