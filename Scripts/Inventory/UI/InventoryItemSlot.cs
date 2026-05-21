using System;
using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.Inventory.UI
{
    public class InventoryItemSlot : UIDragSlot
    {
        public static event Action<InventoryItemSlot> OnSlotTapped;
        public GameObject NullItemContents;
        public GameObject SetItemContents;
        public Item Item { get; private set; }

        protected Inventory _inventory;

        public void SetInventory(Inventory inventory) => _inventory = inventory;

        public virtual void SetItem(Item item)
        {
            Item = item;
            if (IconImage != null)
                IconImage.sprite = item != null ? ItemsManager.GetItemAsset(item.GetId())?.GetIcon() : null;

            if(NullItemContents != null)
            {
                NullItemContents.SetActive(item == null);
            }

            if(SetItemContents != null)
            {
                SetItemContents.SetActive(item != null);
            }
        }

        public virtual void ClearSlot()
        {
            SetItem(null);
        }

        protected override bool CanDrag() => Item != null;

        protected override void OnTapped()
        {
            if (Item != null)
                OnSlotTapped?.Invoke(this);
        }

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
