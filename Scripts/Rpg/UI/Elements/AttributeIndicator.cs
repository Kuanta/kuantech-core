using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Rpg.UI
{
    public class AttributeIndicator : MonoBehaviour {
        [SerializeField] private AttributeAsset Attribute;
        [SerializeField] private TMP_Text AttributeName;
        [SerializeField] private TMP_Text AttributeValue;
  
        public void Initialize()
        {
            SetAttribute(Attribute);
        }

        public void SetAttribute(AttributeAsset attribute)
        {
            if(attribute == null) return;
            Attribute = attribute;
            if(AttributeName != null) AttributeName.text = Attribute.GetName();
        }

        public void UpdateValue(StatsModule statsModule)
        {
            float value = statsModule.GetAttributeValue(Attribute);
            if(AttributeValue != null) AttributeValue.text = value.Stringfy(true);
        }
    }
}