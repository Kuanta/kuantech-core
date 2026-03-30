using System;
using System.Collections.Generic;
using Kuantech.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using Kuantech.Networking;


#if NETWORKING_FISHNET
using FishNet.Connection;
using FishNet.Object;
#endif

namespace Kuantech.Inventory
{
    [Serializable]
    public class InventoryState : ActorModuleSerializableData
    {
        public List<SerializableItemState> ItemStates;
    }

    public class InventoryModule : ActorModule
    {
        public float MaxEncumbrance = 10f; //Can be a stat value in future
        public Equipment Equipment;
        [SerializeReference] public Item[] items;
        public int Size = 30;
        private bool _initialized = false;

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

        public EquipmentSlotType GetEquipmentSlotTypeFromId(string slotTypeId)
        {
            if(Equipment == null) return null;
            return Equipment.GetEquipmentSlotType(slotTypeId);
        }

        #region Add Item

        public virtual bool CanAddItem(ItemData itemData)
        {
            int availableInventoryId = GetAvailableSlotId();
            if(availableInventoryId < 0) return false;
            return true; //For encumbrance or inventory limits checks
        }

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
        public bool AddItemById(string itemId, int amount = 1)
        {
            ItemData itemData = ItemsManager.GetItemData(itemId);
            if (itemData == null) return false;
            return AddItem(itemData, amount);
        }

        /// <summary>
        /// Adds an item. On server/single-player: executes immediately, onAdded fires synchronously.
        /// On client: sends ServerRpc and fires onAdded when server confirms via TargetRpc.
        /// </summary>
        public bool AddItem(ItemData itemData, int amount = 1)
        {
            if (IsServerInitialized)
            {
                Item item = ExecuteAddItem(itemData, amount);
                if (item == null) return false;
                if (IsSpawned)
                {
                    ObserversOnItemAdded_Rpc(itemData.Id, amount, item.GetInventoryId());
                } 
                return true;
            }
            // Client: register callback and send request to server
            ServerAddItem_Rpc(itemData.Id, amount);
            return true;
        }

        private Item ExecuteAddItem(string itemId, int amount, int inventoryId=-1)
        {
            ItemData itemData = ItemsManager.GetItemData(itemId);
            if (itemData == null) return null;
            return ExecuteAddItem(itemData, amount, inventoryId);
        }

        // Returns the item that was added (or the existing stack), null on failure.
        private Item ExecuteAddItem(ItemData itemData, int amount, int inventoryId=-1)
        {
            if(!CanAddItem(itemData)) return null;
            Item item = Item.GetItemFromData(itemData);
            amount = Mathf.Max(1, amount);
            if (item.Data.stackable)
            {
                Item stackable = GetItemById(item.GetId());
                if (stackable != null)
                {
                    stackable.AddAmount(amount);
                    return stackable;
                }
            }

            int availableId = -1;
            if (inventoryId < 0)
            {
                availableId = GetAvailableSlotId();
            }
            else
            {
                if (!(items.Length <= inventoryId))
                {
                    ExtendInventory(inventoryId + 1);
                    availableId = inventoryId;
                }
            }
            if (availableId < 0) return null;
            items[availableId] = item;
            item.SetInventoryId(availableId);
            item.ParentInvetory = this;
            item.OnAdded();
            return item;
        }

        #endregion

        #region Remove Item
        public void RemoveItem(Item item)
        {
            if (item == null) return;
            if (item.ParentInvetory.Actor != Actor) return;
            RemoveItem(item.GetInventoryId());
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
                int newAmount = itemToRemove.GetAmount() - amount;
                if (newAmount > 0)
                {
                    itemToRemove.SetAmount(newAmount);
                    return;
                }
            }
            if (Equipment.slotTable.ContainsKey(itemToRemove.GetEquippedSlot()) && Equipment.slotTable[itemToRemove.GetEquippedSlot()].item == itemToRemove)
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
                ObserversEquipItem_Rpc(item.GetInventoryId(), slotId);
            }
            else
            {
                ServerEquipItem_Rpc(item.GetInventoryId(), slotId);
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

        public bool AddAndEquipItem(string itemId, EquipmentSlotType slotType=null, int amount=1)
        {
            string slotId = slotType != null ? slotType.Id : "";
            if (IsServerInitialized)
            {
                Item addedItem = ExecuteAddItem(itemId, amount);
                if(addedItem == null) return false;
                bool equipped = ExecuteEquipItem(addedItem, slotType);
                if(equipped)
                {
                    ObserversAddAndEquipItem_Rpc(itemId, addedItem.GetInventoryId(), amount, slotId);
                }
                else
                {
                    ObserversOnItemAdded_Rpc(itemId, amount, addedItem.GetInventoryId());
                }
                return true;
            }
            else
            {
                ServerAddAndEquipItem_Rpc(itemId, amount, slotId);
            }
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
                ObserversUnequipItem_Rpc(item.GetInventoryId());
            }
            else
            {
                ServerUnequipItem_Rpc(item.GetInventoryId());
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
            if (Equipment != null) Equipment.UnequipAll();
            for (int i = 0; i < items.Length; ++i)
            {
                items[i]?.OnRemoved();
                items[i] = null;
            }
        }

        #region State

        protected override ActorModuleSerializableData InstantiateState()
        {
            var itemStates = new List<SerializableItemState>();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                    itemStates.Add(items[i].BuildState());
            }
            return new InventoryState { ItemStates = itemStates };
        }

        public override void LoadState(ActorModuleSerializableData serializableData)
        {
            if (serializableData is not InventoryState inventoryState) return;
            if (inventoryState.ItemStates == null) return;
            foreach (var state in inventoryState.ItemStates)
            {
                Item existing = GetItemAtInventoryId(state.InventoryId);
                if (existing != null)
                {
                    // Item already exists server-side (host case) — equip data is already set,
                    // but visual may not have been spawned yet. Handled in OnNetworkSynced.
                    continue;
                }
                Item item = Item.FromState(state, this);
                if (item == null) continue;
                if (state.Equipped)
                {
                    EquipmentSlotType slot = Equipment != null ? Equipment.GetEquipmentSlotType(state.EquippedSlotId) : null;
                    ExecuteEquipItem(item, slot);
                }
            }
        }

        // Called on client after state sync — ensures equipped item visuals are spawned.
        // Covers the host/listen-server case where items exist server-side but visual was never spawned client-side.
        public override void OnNetworkSynced()
        {
            if (!KtNetworkManager.IsClient()) return;
            if (Equipment == null) return;
            foreach (var slot in Equipment.slotTable.Values)
            {
                Item item = slot.item;
                if (item == null || item.ItemVisual != null) continue;
                ActorVisual visual = Equipment.GetActorVisual();
                if (visual == null) continue;
                item.ItemVisual = visual.SlotItem(slot.SlotType, item);
            }
        }

        #endregion

        #region Networking
#if NETWORKING_FISHNET
        // Client → Server: add item request with a requestId so we can fire the callback.
        [ServerRpc]
        private void ServerAddItem_Rpc(string itemId, int amount)
        {
            ItemData itemData = ItemsManager.GetItemData(itemId);
            if (itemData == null) return;
            AddItem(itemData,amount);
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
        private void ServerAddAndEquipItem_Rpc(string itemId, int amount, string slotId)
        {
            AddAndEquipItem(itemId, Equipment.GetEquipmentSlotType(slotId), amount);
        }

        [ServerRpc]
        private void ServerUnequipItem_Rpc(int inventoryId)
        {
            Item item = GetItemAtInventoryId(inventoryId);
            if (item == null) return;
            UnequipItem(item);
        }

        // Server → all observers (ExcludeOwner: owner already handled via TargetRpc above).
        [ObserversRpc]
        private void ObserversOnItemAdded_Rpc(string itemId, int amount, int inventoryId)
        {
            if (IsServerInitialized) return;
            ExecuteAddItem(itemId, amount, inventoryId);
        }

        [ObserversRpc]
        private void ObserversAddAndEquipItem_Rpc(string itemId, int inventoryId, int amount, string slotId)
        {
            if(IsServerInitialized) return;
            Item addedItem = ExecuteAddItem(itemId, amount, inventoryId);
            if (addedItem == null) return;
            ExecuteEquipItem(addedItem, Equipment.GetEquipmentSlotType(slotId));
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
#else
        private void ServerAddItem_Rpc(string itemId, int amount) { }
        private void ServerRemoveItem_Rpc(int inventoryId, int amount) { }
        private void ServerEquipItem_Rpc(int inventoryId, string slotId) { }
        private void ServerAddAndEquipItem_Rpc(string itemId, int amount, string slotId) { }
        private void ServerUnequipItem_Rpc(int inventoryId) { }
        private void ObserversOnItemAdded_Rpc(string itemId, int amount, int inventoryId) { }
        private void ObserversAddAndEquipItem_Rpc(string itemId, int inventoryId, int amount, string slotId) { }
        private void ObserversRemoveItem_Rpc(int inventoryId, int amount) { }
        private void ObserversEquipItem_Rpc(int inventoryId, string slotId) { }
        private void ObserversUnequipItem_Rpc(int inventoryId) { }
#endif
        #endregion
    }
}