using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Networking;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Inventory
{
    [Serializable]
    public class EquipmentSlot
    {
        public EquipmentSlotType SlotType;
        [SerializeReference] public Item item = null;
    }

    public class Equipment :MonoBehaviour
    {
        public InventoryModule ParentInventory;
        public List<EquipmentSlot> slotTypes;
        public Dictionary<EquipmentSlotType, EquipmentSlot> slotTable;
        private Dictionary<string, EquipmentSlotType> _slotTypesById;
        
        //Events
        public EventHandler<Item>  ItemEquippedEvent;
        public EventHandler<Item> ItemUnequippedEvent;

        public void Initialize(InventoryModule parentInventory)
        {
            ParentInventory = parentInventory;
            if (slotTable != null) return;
            slotTable = new Dictionary<EquipmentSlotType, EquipmentSlot>();
            _slotTypesById = new Dictionary<string, EquipmentSlotType>();
            foreach (EquipmentSlot slot in slotTypes)
            {
                slot.item = null;
                slotTable.Add(slot.SlotType, slot);
                _slotTypesById[slot.SlotType.Id] = slot.SlotType;
            }
        }

        /// <summary>
        /// Gets equipment slot type by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public EquipmentSlotType GetEquipmentSlotType(string id)
        {
            if(_slotTypesById == null) return null;
            if(_slotTypesById.ContainsKey(id)) return _slotTypesById[id];
            return null;
        }

        public Item GetEquipedItem(EquipmentSlotType slot)
        {
            if (!slotTable.ContainsKey(slot)) return null;

            return slotTable[slot].item;
        }

        // Equips an item for the proper slot
        public void EquipItem(Item item, EquipmentSlotType slotType)
        {
            if (item == null) return;
            EquipmentSlotType itemSlotType = slotType;
            
            if (!slotTable.ContainsKey(itemSlotType)) return;
            item.StateData.Equipped = true;
            item.CurrentSlot = itemSlotType; //For weapons. While saving, we need to know where we have equipped it
            
            Item existingItem = slotTable[itemSlotType].item;
            if (existingItem != null && existingItem != item)
            {
                ItemUnequippedEvent?.Invoke(this, existingItem);
            }
            
            slotTable[itemSlotType].item = item;
            
            // Spawn visual only on clients (or single-player). Dedicated server has no visuals.
            if (KtNetworkManager.IsClient())
            {
                ActorVisual visual = GetActorVisual();
                if (visual != null)
                    item.ItemVisual = visual.SlotItem(itemSlotType, item);
            }
 

            //UI handler
            try
            {
                //todo: UI handle for equipment
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
        
        public void UnequipItem(Item item)
        {
            if (item == null) return;
            if (!slotTable.ContainsKey(item.CurrentSlot)) return;
            EquipmentSlot slot = slotTable[item.CurrentSlot];
            if (slot.item == item)
            {
                slot.item = null;
            }
            item.StateData.Equipped = false;

            if (item.ItemVisual != null)
            {
                PoolManager.PoolObject(item.ItemVisual.gameObject);
                item.ItemVisual = null;
            }
            
            //UI handler
            try
            {
                //todo: UI handle for equipment
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
        
        private ActorVisual GetActorVisual()
        {
            if (ParentInventory == null)
            {
                Debug.LogError("Parent inventory for equipment is null");
                return null;
            }

            return ParentInventory.Actor.VisualHandler.GetActorVisual();
        }
    }
}