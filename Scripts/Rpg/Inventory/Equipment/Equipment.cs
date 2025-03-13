using System;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Rpg.Inventory
{
    [Serializable]
    public class EquipmentSlot
    {
        [FormerlySerializedAs("Type")] public EquipmentSlotType SlotType;
        [SerializeReference] public Item item = null;
    }

    public class Equipment :MonoBehaviour
    {
        public InventoryModule ParentInventory;
        public List<EquipmentSlot> slotTypes;
        public Dictionary<EquipmentSlotType, EquipmentSlot> slotTable;
        
        public float Encumbrance = 0f;
        
        //Events
        public EventHandler<Item>  ItemEquippedEvent;
        public EventHandler<Item> ItemUnequippedEvent;

        public void Initialize(InventoryModule parentInventory)
        {
            ParentInventory = parentInventory;
            if (slotTable != null) return;
            slotTable = new Dictionary<EquipmentSlotType, EquipmentSlot>();
            foreach (EquipmentSlot slot in slotTypes)
            {
                slot.item = null;
                slotTable.Add(slot.SlotType, slot);
            }
        }
        public Item GetEquipedItem(EquipmentSlotType slot)
        {
            if (!slotTable.ContainsKey(slot)) return null;

            return slotTable[slot].item;
        }

        private Weapon GetWeapon(EquipmentSlotType slotType)
        {
            Item weapon = GetEquipedItem(slotType);
            if (weapon is Weapon weapon1) return weapon1;
            return null;
        }
        // Equips an item for the proper slot
        public void EquipItem(Item item, EquipmentSlotType slotType)
        {
            if (item == null || !item.CanBeEquippedToSlot(slotType)) return;
            EquipmentSlotType itemSlotType = slotType;
            if (item is Weapon itemAsWeapon)
            {
                itemSlotType = slotType;
                if (itemAsWeapon.WeaponData.SlotSize > 1)
                {
                    //itemSlotType = EquipmentSlotType.MainHand;
                    Debug.LogWarning("handle multiple slots");
                }
            }
 
            
            if (!slotTable.ContainsKey(itemSlotType)) return;
            Encumbrance += item.Data.weight; //Add to encumberance
            item.StateData.Equipped = true;
            item.CurrentSlot = itemSlotType; //For weapons. While saving, we need to know where we have equipped it
            
            Item existingItem = slotTable[itemSlotType].item;
            if (existingItem != null && existingItem != item)
            {
                ItemUnequippedEvent?.Invoke(this, existingItem);
            }
            
            slotTable[itemSlotType].item = item;
            
            //Get Actor Visual
            ActorVisual visual = GetActorVisual();
            
            //If actor visual isn't null, slot the visual
            if (visual != null)
            {
                visual.SlotItem(itemSlotType, item);
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
            Encumbrance -= item.Data.weight;
            Encumbrance = Mathf.Max(Encumbrance, 0f);
            ActorVisual actorVisual = GetActorVisual();
            if (actorVisual != null) actorVisual.ClearSlot(item.CurrentSlot);

            
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