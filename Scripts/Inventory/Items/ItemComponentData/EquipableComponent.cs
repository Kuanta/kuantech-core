using System.Collections.Generic;

namespace Kuantech.Inventory
{
    public class EquipableComponent : ItemComponent
    {
        private readonly List<EquipmentSlotType> _suitableSlots;
        private readonly List<EquipmentSlotType> _occupiedSlots;

        public EquipableComponent(EquipableComponentData data)
        {
            _suitableSlots = data.SuitableSlots;
            _occupiedSlots = data.OccupiedSlots;
        }

        public override int CanEquipItem(Item item, EquipmentSlotType slotType)
        {
            if (_suitableSlots == null || _suitableSlots.Count == 0) return -1;
            if (slotType == null) return 1;
            return _suitableSlots.Contains(slotType) ? 1 : -1;
        }

        public override void OnItemEquipped(Item item, EquipmentSlotType slotType)
        {
            if (slotType == null)
            {
                if (_suitableSlots == null || _suitableSlots.Count == 0) return;
                slotType = _suitableSlots[0];
            }
            item.SetEquippedSlot(slotType);
        }

        public override void OnItemUnequipped(Item item) { }

        public override void OnItemAdded(Item item) { }
        public override void OnItemRemoved(Item item) { }
        public override void OnItemUsed(Item item) { }
    }
}
