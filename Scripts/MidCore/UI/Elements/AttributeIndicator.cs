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
        [SerializeField] private TMP_Text AttributeName;
        [SerializeField] private TMP_Text ValueText;
        [SerializeField] private TMP_Text BonusValueText;
        [Tooltip("If set to true, bonus will be shown (value from levels) will be shown here")]
        [SerializeField] private bool ShowBonusSeperately = false;

        public void SetAttribute(AttributeAsset attributeAsset)
        {
           if(Icon != null) Icon.sprite = attributeAsset.GetIcon();
           if(AttributeName != null) AttributeName.text = attributeAsset.GetName();
        }

        public void SetValue(float value)
        {
            ValueText.text = value.Stringfy();
        }
    }
}