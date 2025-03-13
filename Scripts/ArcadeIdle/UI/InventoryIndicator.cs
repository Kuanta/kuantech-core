using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.ArcadeIdle
{
    public class InventoryIndicator : MonoBehaviour {
        [SerializeField] private ResourceDataReference ResourceToShow;
        [SerializeField] private ResourceInventory SourceInventory;
        [SerializeField] private Image ResourceIcon;
        [SerializeField] private TMP_Text Text;
        [SerializeField] private bool ShowMaxValue;

        private void Initialize()
        {
            SourceInventory.OnResourceAdded += OnResourceInventoryUpdated;
            SourceInventory.OnResourceRemoved += OnResourceInventoryUpdated;
            SourceInventory.InventoryLoaded +=  UpdateIndicator;
            UpdateIndicator();
            if (ResourceIcon == null ) return;
            ResourceIcon.sprite = ResourceToShow.GetResourceIcon();
        }
        private void OnResourceInventoryUpdated((ResourceData, int) args)
        {
            UpdateIndicator();
        }
        public void UpdateIndicator()
        {
            int heldAmount = SourceInventory.GetHeldAmount(ResourceToShow.ResourceId);
            int maxAmount = SourceInventory.InventoryCapacity; //todo: This is only makes sense for inventories with single resource
            if(maxAmount <= 0 || SourceInventory.AcceptedResources.Count != 1)
            {
                ShowMaxValue = false;
            }

            Text.text = ShowMaxValue ? $"{heldAmount.Stringfy()}/{maxAmount.Stringfy()}" : $"{heldAmount.Stringfy()}";
        }
    }
}