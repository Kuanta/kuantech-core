using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class MetadataIndicator : MonoBehaviour
    {
        [SerializeField] private Image Icon;
        [SerializeField] private TMP_Text NameText;
        [SerializeField] private TMP_Text DescriptionText;
        [SerializeField] private Image MetadataColorImage;
        public virtual void SetMetadata(MetadataAsset asset)
        {
            if(Icon != null) Icon.sprite = asset.GetIcon();
            if(NameText != null) NameText.text = asset.GetName();
            if(DescriptionText != null) DescriptionText.text = asset.GetDescription();
            if(MetadataColorImage != null) MetadataColorImage.color = asset.GetColor();
        }
    }
}