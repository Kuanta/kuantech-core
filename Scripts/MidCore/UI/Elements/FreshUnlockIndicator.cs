using Kuantech.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    public class FreshUnlockIndicator : MonoBehaviour
    {
        [SerializeField] private TMP_Text Name;
        [SerializeField] private Image Icon;

        public void SetAsset(MetadataAsset asset)
        {
            if (Name != null)
            {
                Name.text = asset.GetName();
            }

            if (Icon != null)
            {
                Icon.sprite = asset.GetIcon();
            }
        }
    }
}