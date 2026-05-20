using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.Inventory.UI
{
    public class InventoryItemSlot : UIDragSlot
    {
        public Item Item { get; private set; }

        private Inventory _inventory;

        public void SetInventory(Inventory inventory) => _inventory = inventory;

        public virtual void SetItem(Item item)
        {
            Item = item;
            if (IconImage != null)
                IconImage.sprite = item != null ? ItemsManager.GetItemAsset(item.GetId())?.GetIcon() : null;
        }

        public virtual void ClearSlot()
        {
            SetItem(null);
        }

        protected override bool CanDrag() => Item != null;

        public override bool CanAcceptDrop(UIDragSlot source)
        {
            return source is InventoryItemSlot other && ShouldAcceptItem(other.Item);
        }

        public override void OnDropReceived(UIDragSlot source)
        {
            if (source is not InventoryItemSlot other) return;
            Item incoming = other.Item;
            Item outgoing = Item;

            if (_inventory != null && incoming != null && outgoing != null)
                _inventory.SwapItems(incoming.GetInventoryId(), outgoing.GetInventoryId());

            SetItem(incoming);
            other.SetItem(outgoing);
        }

        public override void OnDragCancelled() { }

        protected virtual bool ShouldAcceptItem(Item item) => true;
    }
}
