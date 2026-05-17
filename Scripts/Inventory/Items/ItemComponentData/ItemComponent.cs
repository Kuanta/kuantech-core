using System;

namespace Kuantech.Inventory
{
    public abstract class ItemComponent
    {
        public abstract void OnItemAdded(Item item);
        public abstract void OnItemRemoved(Item item);
        public abstract void OnItemUsed(Item item);
        public abstract void OnItemEquipped(Item item, EquipmentSlotType slotType);
        public abstract void OnItemUnequipped(Item item);

        public virtual int CanEquipItem(Item item, EquipmentSlotType slotType) => 0;
        public virtual bool CanUnequipItem(Item item) => true;

        public virtual string SerializeState() => null;
        public virtual void DeserializeState(string data) { }
    }
}
