using System.Collections.Generic;
using Kuantech.Core.UI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Inventory.UI
{
    public class GridInventoryPanel : UIElement
    {
        [SerializeField] protected Transform Container;
        [SerializeField] protected int SlotCount = 32;
        [SerializeField] private InventoryItemSlot SlotPrefab;
        [KTTag("ItemTag")] [SerializeField] private List<int> FilterTags;

        private readonly List<InventoryItemSlot> _slots = new();

        public void Populate(Inventory inventory)
        {
            Clear();
            if (inventory == null) return;

            foreach (var item in inventory.GetAllItems())
            {
                if (!ShouldDisplayItem(item)) continue;
                var slot = CreateSlot();
                slot.SetInventory(inventory);
                slot.SetItem(item);
                _slots.Add(slot);
            }
        }

        public void Clear()
        {
            foreach (var slot in _slots)
                if (slot != null) Destroy(slot.gameObject);
            _slots.Clear();
        }

        protected virtual InventoryItemSlot CreateSlot() => Instantiate(SlotPrefab, Container);

        protected virtual bool ShouldDisplayItem(Item item)
        {
            if (FilterTags == null || FilterTags.Count == 0) return true;
            return FilterTags.Contains(item.Data.Tag);
        }
    }
}
