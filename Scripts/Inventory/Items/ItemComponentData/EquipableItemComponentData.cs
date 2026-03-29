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

        public override void OnItemEquipped(Item item, EquipmentSlotType slotType)
        {
            //Handle equipment
            if(item.ParentInvetory == null) return;
            Equipment equipment = item.ParentInvetory.Equipment;
            if(equipment == null) return;
            if(slotType == null)
            {
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