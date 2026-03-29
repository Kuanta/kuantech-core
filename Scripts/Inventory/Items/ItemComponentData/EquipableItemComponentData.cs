using System;
using System.Collections.Generic;

namespace Kuantech.Inventory
{
    [Serializable]
    public class EquipableItemComponentData : ItemComponent
    {
        public List<EquipmentSlotType> SuitableSlots; //Which slot can this be equipped to
        public List<EquipmentSlotType> OccupiedSlots; //Which slots does this occupy?

        public override void OnItemAdded(Item item)
        {
            
        }

        public override bool CanEquipItem(Item item, EquipmentSlotType slotType)
        {
            if (SuitableSlots == null || SuitableSlots.Count == 0) return false;
            if (slotType == null) return true; // will default to SuitableSlots[0]
            return SuitableSlots.Contains(slotType);
        }

        public override void OnItemEquipped(Item item, EquipmentSlotType slotType)
        {
            if(item.ParentInvetory == null) return;
            Equipment equipment = item.ParentInvetory.Equipment;
            if(equipment == null) return;
            if(slotType == null)
            {
                if (SuitableSlots == null || SuitableSlots.Count == 0) return;
                slotType = SuitableSlots[0];
            }
            equipment.EquipItem(item, slotType);
        }

        public override void OnItemRemoved(Item item)
        {
        }

        public override void OnItemUnequipped(Item item)
        {
            //Handle equipment
            if (item.ParentInvetory == null) return;
            Equipment equipment = item.ParentInvetory.Equipment;
            if (equipment == null) return;
            equipment.UnequipItem(item);
        }

        public override void OnItemUsed(Item item)
        {
        }
    }
}