using System;
using System.Collections.Generic;
using Kuantech.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Rpg.Inventory
{
    public class InventoryModule : ActorModule
    {
        public float MaxEncumbrance = 10f; //Can be a stat value in future
        public Equipment equipment;
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
            if(equipment == null) equipment = GetComponent<Equipment>();
            equipment.Initialize(this);
            _initialized = true;
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

        /// <summary>
        /// Adds an item to the inventory of the player
        /// </summary>
        /// <param name="item"></param>
        public bool AddItem(Item item, int amount=1)
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
            else
            {
                amount = 1;
            }
            int availableId = GetAvailableSlotId();
            if (availableId < 0) return false;
            items[availableId] = item;
            item.StateData.InventoryId = availableId;
            item.StateData.IsNew = true;
            //Add the item data
            item.ParentInvetory = this;
            return true;
        }
        
          /// <summary>
          /// Adds an item that already has a state data. Should only be called at the moment all the items are loaded from memory and
          /// current inventory is empty. Important because inventory Ids should be consistent between sessions.
          /// </summary>
          /// <param name="item"></param>
          /// <param name="stateData"></param>
          /// <param name="amount"></param>
          /// <param name="equip"></param>
        public void AddItem(Item item, ItemStateData stateData, int amount = 1)
        {
            //Expand size if necessary
            if (stateData.InventoryId > items.Capacity)
            {
                while(items.Count < stateData.InventoryId + 1) items.Add(null);
            }
            items[stateData.InventoryId] = item;
            item.ParentInvetory = this;
        }
          
        /// <summary>
        /// Equips an item
        /// </summary>
        /// <param name="item"></param>
        public void EquipItem(Item item, EquipmentSlotType slotType, bool updateActorData = true)
        {
            if (equipment != null)
            {
                if (!ContainsItem(item))
                {
                    AddItem(item, 1);
                }
                equipment.EquipItem(item, slotType);
                ItemEquipEvent?.Invoke(this, item);
            }
            else
            {
                Debug.LogError("Equipment is null");
            }
        }

        /// <summary>
        /// Unequips an item
        /// </summary>
        /// <param name="item"></param>
        public void UnequipItem(Item item, bool updateActorData = true)
        {
            if (equipment == null)
            {
                Debug.LogError("Equipment is null");
                return;
            }
            equipment.UnequipItem(item);
            ItemUnequipEvent?.Invoke(this, item);
        }
        
        public bool ContainsItem(Item item)
        {
            if (items.Contains(item))
            {
                return true;
            }
            return false;
        }

        public void RemoveItem(Item item)
        {
            if (item == null) return;
            if (item.ParentInvetory.Actor != Actor) return;
            RemoveItem(item.StateData.InventoryId);
        }
        
        public void RemoveItem(int InventoryId, int amount=1, bool updateActorData = true)
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
            if (equipment.slotTable.ContainsKey(itemToRemove.CurrentSlot) && equipment.slotTable[itemToRemove.CurrentSlot].item == itemToRemove)
            {
                equipment.UnequipItem(itemToRemove);
            }
            items[InventoryId] = null;
        }
        
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

        public float GetNormalizedEncumbrance()
        {
            return Mathf.Clamp(equipment.Encumbrance / MaxEncumbrance,0f, 1f);
        }

        public float GetEncumbrance()
        {
            return equipment.Encumbrance;
        }
        
        [Button("Clear inventory")]
        public void ClearInventory()
        {
            for (int i = 0; i < items.Count; ++i)
            {
                RemoveItem(items[i]);
            }
            items.Clear();
        }
    }
}