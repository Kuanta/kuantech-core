using System;
using System.Collections.Generic;
using Kuantech.Core;
using Sirenix.OdinInspector;
using UnityEngine;



#if NETWORKING_FISHNET
using FishNet.Object;
#endif

namespace Kuantech.Inventory
{
    public class InventoryModule : ActorModule
    {
        public float MaxEncumbrance = 10f; //Can be a stat value in future
        public Equipment Equipment;
        [SerializeReference] public List<Item> items;
        public int Size = 30;
        private bool _initialized = false;

        //Events
        public EventHandler<Item> ItemEquipEvent;
        public EventHandler<Item> ItemUnequipEvent;
        
        public override void Initialize()
        {
            if (_initialized) return;
            items = new List<Item>(Size);
            for (int i = 0; i < Size; i++)
            {
                items.Add(null);
            }
            if(Equipment == null) Equipment = GetComponent<Equipment>();
            Equipment.Initialize(this);
            _initialized = true;
        }
        
        public Item GetItemAtInventoryId(int inventoryId)
        {
            if (inventoryId < 0 || inventoryId >= items.Count) return null;
            return items[inventoryId];
        }

        /// <summary>
        /// Returns an item by id. Intended for stackable items.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public Item GetItemById(string itemId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].GetId() == itemId) return items[i];
            }

            return null;
        }
        
        public bool ContainsItem(Item item)
        {
            if (items.Contains(item))
            {
                return true;
            }
            return false;
        }

        #region Add Item
        /// <summary>
        /// Returns the index of an available slot in the inventory
        /// </summary>
        /// <returns></returns>
        public int GetAvailableSlotId()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) return i;
            }
            return -1; // No available slot
        }

        public bool AddItemById(string itemId, int amount = 1)
        {
            ItemData itemData = ItemsManager.GetItemData(itemId);
            if (itemData == null) return false;
            return AddItem(itemData, amount);
        }

        /// <summary>
        /// Adds an item to the inventory of the player
        /// </summary>
        /// <param name="item"></param>
        public bool AddItem(ItemData itemData, int amount = 1)
        {
            if(IsServerInitialized)
            {
                bool result =  ExecuteAddItem(itemData, amount);
                if(!result) return false;
                //Send client rpc
                return true;
            }
            ServerAddItem_Rpc(itemData.Id, amount);
            return true;
        }

        // /// <summary>
        // /// Adds an item that already has a state data. Should only be called at the moment all the items are loaded from memory and
        // /// current inventory is empty. Important because inventory Ids should be consistent between sessions.
        // /// </summary>
        // /// <param name="item"></param>
        // /// <param name="stateData"></param>
        // /// <param name="amount"></param>
        // /// <param name="equip"></param>
        // public bool AddItem(Item item, int amount = 1)
        // {
        //     bool result = ExecuteAddItem(item, amount);
        //     if(!result) return false;
        //     return true;
        // }

        private bool ExecuteAddItem(ItemData itemData, int amount)
        {
            Item item = Item.GetItemFromData(itemData);
            return ExecuteAddItem(item, amount);
        }

        private bool ExecuteAddItem(Item item, int amount)
        {
            amount = Mathf.Max(1, amount);
            if (item.Data.stackable)
            {
                Item stackable = GetItemById(item.GetId());
                if (stackable != null)
                {
                    stackable.Amount += amount;
                    return true;
                }
            }

            int availableId = GetAvailableSlotId();
            if (availableId < 0) return false;
            items[availableId] = item;
            item.StateData.InventoryId = availableId;
            //Add the item data
            item.ParentInvetory = this;
            return true;
        }

        #endregion

        #region Remove Item
        public void RemoveItem(Item item)
        {
            if (item == null) return;
            if (item.ParentInvetory.Actor != Actor) return;
            RemoveItem(item.StateData.InventoryId);
        }
        
        public void RemoveItem(int InventoryId, int amount=1)
        {
            if(IsServerInitialized)
            {
                ExecuteRemoveItem(InventoryId, amount);
                ObserversRemoveItem_Rpc(InventoryId, amount);
            }
            else
            {
                //Send server rpc
                ServerRemoveItem_Rpc(InventoryId, amount);
            }
        }
        

        private void ExecuteRemoveItem(int InventoryId, int amount = 1)
        {
            Item itemToRemove = items[InventoryId];
            if (itemToRemove == null) return;
            if (itemToRemove.Data.stackable)
            {
                int newAmount = itemToRemove.Amount - amount;
                if (newAmount > 0)
                {
                    itemToRemove.Amount = newAmount;
                    return;
                }
            }
            if (Equipment.slotTable.ContainsKey(itemToRemove.CurrentSlot) && Equipment.slotTable[itemToRemove.CurrentSlot].item == itemToRemove)
            {
                Equipment.UnequipItem(itemToRemove);
            }
            items[InventoryId] = null;
        }
        
        #endregion
        
        #region Equip Item
        public void EquipItem(Item item, EquipmentSlotType slotType=null)
        {
            if(item == null) return;
            string slotId = slotType != null ? slotType.Id : "";
            if(IsServerInitialized)
            {
                ExecuteEquipItem(item, slotType);
                //Send client rpc
                ObserversEquipItem_Rpc(item.StateData.InventoryId, slotId);
            }
            else
            {
                ServerEquipItem_Rpc(item.StateData.InventoryId, slotId);
            }
        }

        private bool ExecuteEquipItem(Item item, EquipmentSlotType slotType = null)
        {
            if(item == null) return false;
            if(!item.CanEquip(slotType)) return false;
            
            //If this item isn't in this inventory, add it
            if (!ContainsItem(item))
            {
                AddItem(item.Data, 1);
            }
            item.Equip(slotType); //Leave equipment handling to item
            ItemEquipEvent?.Invoke(this, item); //Call event
            return true;
        }
        #endregion

        #region Unequip Item

        /// <summary>
        /// Unequips an item
        /// </summary>
        /// <param name="item"></param>
        public void UnequipItem(Item item)
        {
            if(IsServerInitialized)
            {
                ExecuteUnequipItem(item);
                ObserversUnequipItem_Rpc(item.StateData.InventoryId);
            }
            else
            {
                ServerUnequipItem_Rpc(item.StateData.InventoryId);
            }
        }

        private bool ExecuteUnequipItem(Item item)
        {
            if(!item.CanUnequip()) return false;
            item.Unequip(); //Leav item unequip to item
            ItemUnequipEvent?.Invoke(this, item); //Call event
            return true;
        }
        #endregion


        [Button("Clear inventory")]
        public void ClearInventory()
        {
            for (int i = 0; i < items.Count; ++i)
            {
                RemoveItem(items[i]);
            }
            items.Clear();
        }

        #region Networking
        [ServerRpc]
        private void ServerAddItem_Rpc(string itemId, int amount)
        {
            AddItemById(itemId, amount);
        }

        [ServerRpc]
        private void ServerRemoveItem_Rpc(int inventoryId, int amount)
        {
            RemoveItem(inventoryId, amount);
        }

        /// <summary>
        /// Tries to 
        /// </summary>
        /// <param name="inventoryId"></param>
        [ServerRpc]
        private void ServerEquipItem_Rpc(int inventoryId, string slotId)
        {
            Item item = GetItemAtInventoryId(inventoryId);
            if (item == null) return;   
            EquipmentSlotType equipmentSlotType = Equipment.GetEquipmentSlotType(slotId);
            if (equipmentSlotType == null) return;
            EquipItem(item, equipmentSlotType);
        }

        [ServerRpc]
        private void ServerUnequipItem_Rpc(int inventoryId)
        {
            Item item = GetItemAtInventoryId(inventoryId);
            if (item == null) return;
            UnequipItem(item);
        }

        [ObserversRpc]
        private void ObserversAddItem_Rpc(string itemId, int amount)
        {
            if(IsServerInitialized) return;
            Item item = GetItemById(itemId);
            if(item == null) return;
            ExecuteAddItem(item, amount);
        }

        [ObserversRpc]
        private void ObserversRemoveItem_Rpc(int inventoryId, int amount)
        {
            if(IsServerInitialized) return;
            ExecuteRemoveItem(inventoryId, amount);
        }

        /// <summary
        [ObserversRpc]
        private void ObserversEquipItem_Rpc(int inventoryId, string slotId)
        {
            if(IsServerInitialized) return;
            EquipmentSlotType slotType = Equipment != null? Equipment.GetEquipmentSlotType(slotId) : null;
            Item item = GetItemAtInventoryId(inventoryId);
            if(item == null) return;
            item.Equip(slotType);
        }

        [ObserversRpc]
        private void ObserversUnequipItem_Rpc(int inventoryId)
        {
            if (IsServerInitialized) return;
            Item item = GetItemAtInventoryId(inventoryId);
            if (item == null) return;
            item.Unequip();
        }

        #endregion
    }
}