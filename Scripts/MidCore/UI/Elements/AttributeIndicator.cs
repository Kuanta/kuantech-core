using Kuantech.Rpg;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    public class AttributeIndicator : MonoBehaviour
    {
        public AttributeAsset AttributeAsset;
        
        [SerializeField] private Image Icon;
        [SerializeField] private TMP_Text ValueText;
        [SerializeField] private TMP_Text BonusValueText;
        [Tooltip("If set to true, bonus will be shown (value from levels) will be shown here")]
        [SerializeField] private bool ShowBonusSeperately = false;

        public void SetAttribute(AttributeDefinition attributeDefinition, int level)
        {
            Icon.sprite = attributeDefinition.AttributeAsset.Icon;
            float baseValue = attributeDefinition.BaseValue;
            float bonusValue = attributeDefinition.ValuePerLevel * level;
            ValueText.text = ShowBonusSeperately ? baseValue.Stringfy() : (baseValue + bonusValue).Stringfy();
            if (BonusValueText != null)
            {
                BonusValueText.text = bonusValue.Stringfy();
            }
        }
    }
}