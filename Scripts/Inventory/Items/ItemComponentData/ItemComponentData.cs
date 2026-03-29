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
        /// Checks if item can be equipped
        /// </summary>
        /// <param name="item"></param>
        /// <param name="slotType"></param>
        /// <returns></returns>
        public virtual bool CanEquipItem(Item item, EquipmentSlotType slotType)
        {
            return true;
        }

        public virtual bool CanUnequipItem(Item item)
        {
            return true;
        }
        #endregion
    }
}