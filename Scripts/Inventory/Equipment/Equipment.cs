using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Inventory
{
    [Serializable]
    public class EquipmentSlot
    {
        public EquipmentSlotType SlotType;
        [SerializeReference] public Item item = null;
    }

    [Serializable]
    public class Equipment
    {
        public List<EquipmentSlotType> SlotTypes;
        private List<EquipmentSlot> _slots;
        public Dictionary<EquipmentSlotType, EquipmentSlot> slotTable;
        private Dictionary<string, EquipmentSlotType> _slotTypesById;

        public event Action<Item, EquipmentSlotType> OnItemSlotted;
        public event Action<Item> OnItemUnslotted;

        public void Initialize()
        {
            slotTable = new Dictionary<EquipmentSlotType, EquipmentSlot>();
            _slotTypesById = new Dictionary<string, EquipmentSlotType>();
            _slots = new List<EquipmentSlot>();
            if (SlotTypes == null) return;
            foreach (var slotType in SlotTypes)
            {
                var slot = new EquipmentSlot { SlotType = slotType };
                slot.item = null;
                _slots.Add(slot);
                slotTable[slot.SlotType] = slot;
                _slotTypesById[slot.SlotType.Id] = slot.SlotType;
            }
        }

        public EquipmentSlotType GetEquipmentSlotType(string id)
        {
            if (_slotTypesById == null) return null;
            _slotTypesById.TryGetValue(id, out var slotType);
            return slotType;
        }

        public bool CanEquip(EquipmentSlotType slotType)
        {
            if (slotTable == null) return false;
            if (slotType != null) return slotTable.ContainsKey(slotType);
            return slotTable.Count > 0;
        }

        public Item GetEquippedItem(EquipmentSlotType slot)
        {
            if (slotTable == null || !slotTable.ContainsKey(slot)) return null;
            return slotTable[slot].item;
        }

        public void EquipItem(Item item, EquipmentSlotType slotType)
        {
            if (item == null || slotType == null) return;
            if (!slotTable.ContainsKey(slotType)) return;

            Item existing = slotTable[slotType].item;
            if (existing != null && existing != item)
                UnequipItem(existing);

            slotTable[slotType].item = item;
            OnItemSlotted?.Invoke(item, slotType);
        }

        public void UnequipItem(Item item)
        {
            if (item == null || slotTable == null) return;
            foreach (var slot in slotTable.Values)
            {
                if (slot.item != item) continue;
                slot.item = null;
                OnItemUnslotted?.Invoke(item);
                return;
            }
        }

        public void UnequipAll()
        {
            if (slotTable == null) return;
            foreach (var slot in slotTable.Values)
            {
                if (slot.item != null)
                    UnequipItem(slot.item);
            }
        }
    }
}
