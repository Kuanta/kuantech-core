using System;
using Kuantech.Core.UI;
using Kuantech.Core.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Inventory.UI
{
    public class InventoryItemSlot : UIDragSlot
    {
        public static event Action<InventoryItemSlot> OnSlotTapped;
        public GameObject NullItemContents;
        public GameObject SetItemContents;
        [SerializeField] private Image RarityColorImage;
        [SerializeField] private ColorPalette RarityColors;
        public Item Item { get; private set; }

        protected Inventory _inventory;

        public void SetInventory(Inventory inventory) => _inventory = inventory;

        public virtual void SetItem(Item item)
        {
            Item = item;
            if (IconImage != null)
                IconImage.sprite = item != null ? ItemsLibrary.GetItemData(item.GetId())?.GetIcon() : null;

            if(NullItemContents != null)
            {
                NullItemContents.SetActive(item == null);
            }

            if(SetItemContents != null)
            {
                SetItemContents.SetActive(item != null);
            }

            SetRarityColor(item);
        }

        // Colors the rarity swatch from the item's rarity (via IItemRarityProvider). Hidden for empty slots
        // and for items with no rarity, so Core stays free of any game-specific rarity component.
        private void SetRarityColor(Item item)
        {
            if (RarityColorImage == null) return;

            int rarity = item != null ? item.GetRarity() : -1;
            if (RarityColors == null || rarity < 0)
            {
                RarityColorImage.enabled = false;
                return;
            }

            RarityColorImage.enabled = true;
            RarityColorImage.color = RarityColors.GetColor(rarity);
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
