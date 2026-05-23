using System;
using Kuantech.Core;

namespace Kuantech.Inventory
{
    public abstract class ItemComponent
    {
        [NonSerialized] public Item ParentItem;
        public virtual void Initialize(Item item)
        {
            ParentItem = item;
        }
        public virtual void OnItemInitialized(){}
        public abstract void OnItemAdded(Item item);
        public abstract void OnItemRemoved(Item item);
        public abstract void OnItemUsed(Item item);
        public abstract void OnItemEquipped(Item item, EquipmentSlotType slotType);
        public abstract void OnItemUnequipped(Item item);
        public virtual void OnAttachedToActor(Actor actor) { }
        public virtual void OnDetachedFromActor(Actor actor) { }


        public virtual int CanEquipItem(Item item, EquipmentSlotType slotType) => 0;
        public virtual bool CanUnequipItem(Item item) => true;

        public virtual string SerializeState() => null;
        public virtual void DeserializeState(string data) { }

        public Actor GetOwner()
        {
            if(ParentItem == null) return null;
            if(ParentItem.ParentInventory == null) return null;
            return ParentItem.ParentInventory.Owner;
        }
    }
}
