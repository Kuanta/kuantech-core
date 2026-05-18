using System.Collections.Generic;
using Kuantech.Core.UI;
using Kuantech.Inventory.UI;
using UnityEngine;

namespace Kuantech.Inventory
{
    public abstract class GridInventoryPanel : UIElement
    {
        [SerializeField] protected Transform Container;
        [SerializeField] protected int SlotCount = 32;

        private readonly List<InventoryItemSlot> _slots = new();

        public void Populate(Inventory inventory)
        {
            Clear();

            for (int i = 0; i < SlotCount; i++)
                _slots.Add(CreateSlot());

            if (inventory == null) return;

            int slotIdx = 0;
            foreach (var item in inventory.GetAllItems())
            {
                if (slotIdx >= SlotCount) break;
                if (!ShouldDisplayItem(item)) continue;
                _slots[slotIdx].SetItem(item);
                slotIdx++;
            }
        }

        public void Clear()
        {
            foreach (var slot in _slots)
                if (slot != null) Destroy(slot.gameObject);
            _slots.Clear();
        }

        protected abstract InventoryItemSlot CreateSlot();

        protected virtual bool ShouldDisplayItem(Item item) => true;
    }
}
