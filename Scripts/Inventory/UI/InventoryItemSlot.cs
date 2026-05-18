using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.Inventory.UI
{
    public class InventoryItemSlot : UIDragSlot
    {
        public Item Item { get; private set; }

        public virtual void SetItem(Item item)
        {
            Item = item;
            if (IconImage != null && item != null)
                IconImage.sprite = ItemsManager.GetItemAsset(item.GetId())?.GetIcon();
        }

        public virtual void ClearSlot()
        {
            Item = null;
            if (IconImage != null) IconImage.sprite = null;
        }

        protected override bool CanDrag() => Item != null;
    }
}
