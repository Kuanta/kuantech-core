using System.Collections.Generic;
using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.Inventory.UI
{
    public class EquipmentPanel : UIElement
    {
        [SerializeField] private List<EquipmentItemSlot> Slots;

        private Dictionary<EquipmentSlotType, EquipmentItemSlot> _slots;

        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();

            _slots = new Dictionary<EquipmentSlotType, EquipmentItemSlot>();
            if (Slots == null) return;
            foreach (var slot in Slots)
            {
                if (slot.SlotType == null) continue;
                slot.SetEquipmentSlotType(slot.SlotType);
                _slots[slot.SlotType] = slot;
            }
        }

        public void Populate(Inventory inventory, Equipment equipment)
        {
            foreach (var slot in Slots)
                slot.Setup(inventory, equipment);
            SetFromEquipment(equipment);
        }

        public void SetFromEquipment(Equipment equipment)
        {
            foreach (var slot in _slots.Values)
                slot.ClearSlot();

            if (equipment?.slotTable == null) return;

            foreach (var kvp in equipment.slotTable)
            {
                if (kvp.Value.item == null) continue;
                if (_slots.TryGetValue(kvp.Key, out var uiSlot))
                    uiSlot.SetItem(kvp.Value.item);
            }
        }

        public EquipmentItemSlot GetSlot(EquipmentSlotType slotType)
        {
            if (_slots == null) return null;
            _slots.TryGetValue(slotType, out var slot);
            return slot;
        }
    }
}
