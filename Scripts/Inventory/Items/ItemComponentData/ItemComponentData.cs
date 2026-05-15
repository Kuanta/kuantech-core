using System;

namespace Kuantech.Inventory
{
    [Serializable]
    public abstract class ItemComponent
    {
        #region Event Handlers
        public abstract void OnItemAdded(Item item);
        public abstract void OnItemRemoved(Item item);
        public abstract void OnItemUsed(Item item);
        public abstract void OnItemEquipped(Item item, EquipmentSlotType slotType);
        public abstract void OnItemUnequipped(Item item);

        #endregion

        #region Checkers
        /// <summary>
        /// Checks if item can be equipped. -1 means false, 0 means don't care, 1 means true
        /// </summary>
        /// <param name="item"></param>
        /// <param name="slotType"></param>
        /// <returns></returns>
        public virtual int CanEquipItem(Item item, EquipmentSlotType slotType)
        {
            return 0;
        }

        public virtual bool CanUnequipItem(Item item)
        {
            return true;
        }
        #endregion

        public virtual ItemComponent Clone() => (ItemComponent)MemberwiseClone();

        public virtual string BuildComponentState() => null;
        public virtual void LoadComponentState(string data) { }
    }
}