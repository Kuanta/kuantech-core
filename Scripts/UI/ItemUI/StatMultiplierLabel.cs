using Kuantech.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.UI
{
    public class StatMultiplierLabel : MonoBehaviour
    {
        [SerializeField] private Image StatIcon;
        [SerializeField] private TMP_Text MultiplierText;

        public void SetMultiplier(StatTypes statType, float multiplier)
        {
            SetMultiplier(statType);
            SetMultiplier(multiplier);
        }

        public void SetMultiplier(StatTypes statType)
        {
            StatIcon.sprite = UIManager.Instance.IconLibrary.GetStatIcon(statType);
        }
        
        public void SetMultiplier(float multiplier)
        {
            MultiplierText.text = multiplier.ToString("F");
        }
    }
}