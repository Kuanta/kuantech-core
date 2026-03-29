using System;
using System.Collections.Generic;
using Kuantech.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using Kuantech.Utils;


#if NETWORKING_FISHNET
using FishNet.Connection;
using FishNet.Object;
#endif

namespace Kuantech.Inventory
{
    /// <summary>
    /// Compact item state sent over RPCs. ScriptableObject-free.
    /// </summary>
    [Serializable]
    public struct SerializableItemState
    {
        public string ItemDataId;
        public int InventoryId;
        public int Amount;
        public int ItemLevel;
        public bool Equipped;
        public string EquippedSlotId;
    }

    public class InventoryModule : ActorModule
    {
        public float MaxEncumbrance = 10f; //Can be a stat value in future
        public Equipment Equipment;
        [SerializeReference] public Item[] items;
        public int Size = 30;
        private bool _initialized = false;

        // Request/response callback registry for async AddItem
        private int _nextRequestId = 0;
        private Dictionary<int, Action<Item>> _pendingAddCallbacks = new();

        //Events
        public EventHandler<Item> ItemEquipEvent;
        public EventHandler<Item> ItemUnequipEvent;
        
        public override void Initialize()
        {
            if (_initialized) return;
            //Initialize items arrat
            items = new Item[Size];
            for (int i = 0; i < Size; i++)
            {
                items[0] = null;
            }
            if(Equipment == null) 
            {
                Equipment = GetComponent<Equipment>();
            }
            if(Equipment != null)Equipment.Initialize(this);
            _initialized = true;
        }
        
        public Item GetItemAtInventoryId(int inventoryId)
        {
            if (inventoryId < 0 || inventoryId >= items.Length) return null;
            return items[inventoryId];
        }

        /// <summary>
        /// Returns an item by id. Intended for stackable items.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public Item GetItemById(string itemId)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].GetId() == itemId) return items[i];
            }

            return null;
        }
        
        /// <summary>
        /// Checks whether inventory has the item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool ContainsItemReference(Item item)
        {
            //todo: Should we check by id=
            for(int i = 0; i < items.Length; i++)
            {
                if(items[i] == item) return true;
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
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) return i;
            }
            return -1; // No available slot
        }

        public void ExtendInventory(int newSize)
        {
            if(newSize <= items.Length) return;
            Item[] newItems = new Item[newSize];
            Array.Copy(items, 0, newItems, 0, items.Length);
            items = newItems;
        }
        public bool AddItemById(string itemId, int amount = 1, Action<Item> onAdded = null)
        {
            ItemData itemData = ItemsManager.GetItemData(itemId);
            if (itemData == null) return false;
            return AddItem(itemData, amount, onAdded);
        }

        /// <summary>
        /// Adds an item. On server/single-player: executes immediately, onAdded fires synchronously.
        /// On client: sends ServerRpc and fires onAdded when server confirms via TargetRpc.
        /// </summary>
        public bool AddItem(ItemData itemData, int amount = 1, Action<Item> onAdded = null)
        {
            if (IsServerInitialized)
            {
                Item item = ExecuteAddItem(itemData, amount);
                if (item == null) return false;
                SerializableItemState state = BuildItemState(item);
                if (IsSpawned) ObserversOnItemAdded_Rpc(state);
                onAdded?.Invoke(item);
                return true;
            }
            // Client: register callback and send request to server
            int requestId = _nextRequestId++;
            if (onAdded != null) _pendingAddCallbacks[requestId] = onAdded;
            ServerAddItem_Rpc(itemData.Id, amount, requestId);
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

        // Returns the item that was added (or the existing stack), null on failure.
        private Item ExecuteAddItem(ItemData itemData, int amount, int inventoryId=-1)
        {
            Item item = Item.GetItemFromData(itemData);
            return ExecuteAddItem(item, amount, inventoryId);
        }

        private Item ExecuteAddItem(Item item, int amount, int inventoryId=-1)
        {
            amount = Mathf.Max(1, amount);
            if (item.Data.stackable)
            {
                Item stackable = GetItemById(item.GetId());
                if (stackable != null)
                {
                    stackable.Amount += amount;
                    return stackable;
                }
            }

            int availableId = -1;
            if(inventoryId < 0)
            {
                availableId = GetAvailableSlotId();
            }
            else
            {
                if(!(items.Length <= inventoryId))
                {
                    ExtendInventory(inventoryId + 1);
                    availableId = inventoryId;
                }
            }
            if (availableId < 0) return null;
            items[availableId] = item;
            item.StateData.InventoryId = availableId;
            item.ParentInvetory = this;
            item.OnAdded();
            return item;
        }

        private SerializableItemState BuildItemState(Item item)
        {
            return new SerializableItemState
            {
                ItemDataId    = item.Data.Id,
                InventoryId   = item.StateData.InventoryId,
                Amount        = item.Amount,
                ItemLevel     = item.StateData.ItemLevel,
                Equipped      = item.StateData.Equipped,
                EquippedSlotId = item.CurrentSlot != null ? item.CurrentSlot.Id : "",
            };
        }

        // Reconstructs an item from serialized state on client. Returns null if ItemData not found.
        private Item ReconstructItem(SerializableItemState state)
        {
            ItemData itemData = ItemsManager.GetItemData(state.ItemDataId);
            if (itemData == null)
            {
                Debug.LogWarning($"[InventoryModule] ReconstructItem: ItemData '{state.ItemDataId}' not found.");
                return null;
            }
            Item item = Item.GetItemFromData(itemData);
            item.Amount = state.Amount;
            item.StateData.InventoryId = state.InventoryId;
            item.StateData.ItemLevel   = state.ItemLevel;
            item.StateData.Equipped    = state.Equipped;
            item.ParentInvetory = this;

            // Expand list if this slot index is beyond current size (save/load case)
            ExtendInventory(state.InventoryId + 1);
            items[state.InventoryId] = item;
            return item;
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
            if (!ContainsItemReference(item))
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

        // Client → Server: add item request with a requestId so we can fire the callback.
        [ServerRpc]
        private void ServerAddItem_Rpc(string itemId, int amount, int requestId)
        {
            ItemData itemData = ItemsManager.GetItemData(itemId);
            if (itemData == null) return;
            Item item = ExecuteAddItem(itemData, amount);
            if (item == null) return;
            SerializableItemState state = BuildItemState(item);
            // Tell the requesting owner which slot their item landed in.
            TargetOnItemAdded_Rpc(Owner, state, requestId);
            // Tell all other observers about the new item.
            ObserversOnItemAdded_Rpc(state);
        }

        // Server → owner: confirm add and carry back the resolved state.
        [TargetRpc]
        private void TargetOnItemAdded_Rpc(NetworkConnection conn, SerializableItemState state, int requestId)
        {
            if (IsServerInitialized) return;
            Item item = ReconstructItem(state);
            if (item != null && _pendingAddCallbacks.TryGetValue(requestId, out Action<Item> cb))
            {
                _pendingAddCallbacks.Remove(requestId);
                cb.Invoke(item);
            }
        }

        // Server → all observers (ExcludeOwner: owner already handled via TargetRpc above).
        [ObserversRpc(ExcludeOwner = true)]
        private void ObserversOnItemAdded_Rpc(SerializableItemState state)
        {
            if (IsServerInitialized) return;
            ReconstructItem(state);
        }

        [ServerRpc]
        private void ServerRemoveItem_Rpc(int inventoryId, int amount)
        {
            RemoveItem(inventoryId, amount);
        }

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
        private void ObserversAddAndEquipItem_Rpc(string itemId, int inventoryId, int amount, string slotId)
        {
            if (IsServerInitialized) return;
            ExecuteAddItem(itemId, amount);
            Item item = GetItemAtInventoryId(inventoryId);
        }
        [ObserversRpc]
        private void ObserversRemoveItem_Rpc(int inventoryId, int amount)
        {
            if (IsServerInitialized) return;
            ExecuteRemoveItem(inventoryId, amount);
        }

        [ObserversRpc]
        private void ObserversEquipItem_Rpc(int inventoryId, string slotId)
        {
            if (IsServerInitialized) return;
            EquipmentSlotType slotType = Equipment != null ? Equipment.GetEquipmentSlotType(slotId) : null;
            Item item = GetItemAtInventoryId(inventoryId);
            if (item == null) return;
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