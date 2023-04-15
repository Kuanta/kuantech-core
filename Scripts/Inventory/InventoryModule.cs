using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Data;
using Kuantech.Inventory.Items;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Inventory
{
    public class InventoryModule : Module
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
            equipment = GetComponent<Equipment>();
            equipment.Initialize();
            _initialized = true;
        }

        public int GetItemCountByType(Enums.ItemType itemType)
        {
            int count = 0;
            foreach (var item in items)
            {
                if (item == null) continue;
                if (item.Type == itemType) count++;
            }
            return count;
        }
        
        /// <summary>
        /// Returns an item by id. Intended for stackable items.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public Item GetItemById(int itemId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].data.id == itemId) return items[i];
            }

            return null;
        }

        /// <summary>
        /// Adds an item to the inventory of the player
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(Item item, int amount=1, bool equip=false, bool updateActorData = true)
        {
            amount = Mathf.Max(1, amount);
            if (item.stackable)
            {
                Item stackable = GetItemById(item.data.id);
                if (stackable != null)
                {
                    stackable.amount += amount;
                    return;
                }
            }
            else
            {
                amount = 1;
            }
            int availableId = GetAvailableSlotId();
            if (availableId < 0) return;
            items[availableId] = item;
            item.StateData.InventoryId = availableId;
            item.StateData.IsNew = true;
            if (equip)
            {
                EquipItem(item, item.slotType);
            }
            //Add the item data
            item.Owner = Actor;
        }
        
          /// <summary>
          /// Adds an item that already has a state data. Should only be called at the moment all the items are loaded from memory and
          /// current inventory is empty. Important because inventory Ids should be consistent between sessions.
          /// </summary>
          /// <param name="item"></param>
          /// <param name="stateData"></param>
          /// <param name="amount"></param>
          /// <param name="equip"></param>
        public void AddItem(Item item, ItemStateData stateData, int amount = 1, bool equip = false)
        {
            //Expand size if necessary
            if (stateData.InventoryId > items.Capacity)
            {
                while(items.Count < stateData.InventoryId + 1) items.Add(null);
            }

            items[stateData.InventoryId] = item;
            if (stateData.Equipped)
            {
                EquipItem(item, item.slotType);
            }

            item.Owner = Actor;
        }
          
        /// <summary>
        /// Equips an item
        /// </summary>
        /// <param name="item"></param>
        public void EquipItem(Item item, Enums.EquipmentSlotType slotType, bool updateActorData = true)
        {
            if (equipment != null)
            {
                if (!ContainsItem(item))
                {
                    AddItem(item, 1, false, updateActorData);
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
            if (item.Owner != Actor) return;
            RemoveItem(item.StateData.InventoryId);
        }
        public void RemoveItem(int InventoryId, int amount=1, bool updateActorData = true)
        {
            Item itemToRemove = items[InventoryId];
            if (itemToRemove == null) return;
            if (itemToRemove.stackable)
            {
                int newAmount = itemToRemove.amount - amount;
                if (newAmount > 0)
                {
                    return;
                }
            }
            if (equipment.slotTable.ContainsKey(itemToRemove.slotType) && equipment.slotTable[itemToRemove.slotType].item == itemToRemove)
            {
                equipment.UnequipItem(equipment.slotTable[itemToRemove.slotType].item);
            }
            items[InventoryId] = null;
        }
        
        /// <summary>
        /// Returns the index of an available slot in the inventory
        /// </summary>
        /// <returns></returns>
        public int GetAvailableSlotId()
        {
            for (int i = items.Count; i < items.Capacity; i++)
            {
                items.Add(null);
            }
            for (int i = 0; i < items.Capacity; i++)
            {
                if (items[i] == null)
                {
                    return i;
                }
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