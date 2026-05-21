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
        private Inventory _inventory;

        public void Populate(Inventory inventory)
        {
            if (_inventory != null)
                UnsubscribeEvents();

            _inventory = inventory;

            if (_inventory != null)
                SubscribeEvents();

            Redraw();
        }

        public void Redraw()
        {
            foreach (var slot in _slots)
                if (slot != null) Destroy(slot.gameObject);
            _slots.Clear();

            if (_inventory == null) return;

            foreach (var item in _inventory.GetAllItems())
            {
                if (!ShouldDisplayItem(item)) continue;
                var slot = CreateSlot();
                slot.SetInventory(_inventory);
                slot.SetItem(item);
                _slots.Add(slot);
            }
        }

        public void Clear()
        {
            if (_inventory != null)
                UnsubscribeEvents();
            _inventory = null;
            Redraw();
        }

        protected virtual InventoryItemSlot CreateSlot() => Instantiate(SlotPrefab, Container);

        protected virtual bool ShouldDisplayItem(Item item)
        {
            if (item.IsEquipped()) return false;
            if (FilterTags == null || FilterTags.Count == 0) return true;
            return FilterTags.Contains(item.Data.Tag);
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents(); //Just in case
            _inventory.OnItemAdded += OnItemChanged;
            _inventory.OnItemRemoved += OnItemChanged;
            _inventory.OnItemEquipped += OnItemEquipped;
            _inventory.OnItemUnequipped += OnItemChanged;
        }

        private void UnsubscribeEvents()
        {
            _inventory.OnItemAdded -= OnItemChanged;
            _inventory.OnItemRemoved -= OnItemChanged;
            _inventory.OnItemEquipped -= OnItemEquipped;
            _inventory.OnItemUnequipped -= OnItemChanged;
        }

        private void OnItemChanged(Item item) => Redraw();
        private void OnItemEquipped(Item item, EquipmentSlotType slot) => Redraw();
    }
}
