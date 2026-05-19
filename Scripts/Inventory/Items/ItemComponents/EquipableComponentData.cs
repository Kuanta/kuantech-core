using System;
using System.Collections.Generic;

namespace Kuantech.Inventory
{
    [Serializable]
    public class EquipableComponentData : ItemComponentData
    {
        public List<EquipmentSlotType> SuitableSlots;
        public List<EquipmentSlotType> OccupiedSlots;

        public override ItemComponent CreateInstance() => new EquipableComponent(this);
    }
}
