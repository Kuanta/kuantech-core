using System;
using System.Collections.Generic;
using Kuantech.Character;
using Kuantech.Core;
using Kuantech.Data;
using Kuantech.Inventory.Items;
using UnityEngine;

namespace Kuantech.Inventory
{
    [Serializable]
    public class EquipmentSlot
    {
        public Enums.EquipmentSlotType type;
        [SerializeReference] public Item item = null;
    }

    public class Equipment :MonoBehaviour
    {
        public Actor Actor;
        public List<EquipmentSlot> slotTypes;
        public Dictionary<Enums.EquipmentSlotType, EquipmentSlot> slotTable;

        public float Encumbrance = 0f;
        
        private void Awake()
        {
            Actor = GetComponent<Actor>();
            slotTable = new Dictionary<Enums.EquipmentSlotType, EquipmentSlot>();
            foreach (EquipmentSlot slot in slotTypes)
            {
                slot.item = null;
                slotTable.Add(slot.type, slot);
            }
        }

        public Item GetEquipedItem(Enums.EquipmentSlotType slot)
        {
            if (!slotTable.ContainsKey(slot)) return null;

            return slotTable[slot].item;
        }

        public Weapon GetMainWeapon()
        {
            return GetWeapon(Enums.EquipmentSlotType.MainHand);
        }
        
        public Weapon GetOffWeapon()
        {
            return GetWeapon(Enums.EquipmentSlotType.OffHand);
        }

        private Weapon GetWeapon(Enums.EquipmentSlotType slotType)
        {
            Item weapon = GetEquipedItem(slotType);
            if (weapon is Weapon weapon1) return weapon1;
            return null;
        }
        // Equips an item for the proper slot
        public void EquipItem(Item item, Enums.EquipmentSlotType slotType)
        {
            if (item == null || !item.equipable) return;
            Enums.EquipmentSlotType itemSlotType = item.slotType;
            if (item is Weapon)
            {
                Weapon itemAsWeapon = item as Weapon;
                itemSlotType = slotType;
                if (itemAsWeapon.SlotSize > 1)
                {
                    itemSlotType = Enums.EquipmentSlotType.MainHand;
                }

                if (itemAsWeapon.IsOffHand)
                {
                    itemSlotType = Enums.EquipmentSlotType.OffHand;
                }
            }
            else
            {
                itemSlotType = item.slotType;
            }
       
            if (!slotTable.ContainsKey(itemSlotType)) return;
            Encumbrance += item.Weight; //Add to encumberance
            item.StateData.Equipped = true;
            item.slotType = itemSlotType; //For weapons. While saving, we need to know where we have equipped it
            
            Item existingItem = slotTable[itemSlotType].item;
            if (existingItem != null && existingItem != item)
            {
                GameManager.Instance.UnequipItem(existingItem);
            }
            slotTable[itemSlotType].item = item;
            if (Actor == null) return;
            if (Actor.TryGetComponent(out CharacterBody cb))
            {
                if (item.templateData.inPlace)
                {
                    cb.SlotInplaceEquipment(item.data.templateId);
                }
                else
                {
                    GameObject modelPrefab = Librarian.Instance.GetItemPrefab(item.data.templateId);
                    if (modelPrefab == null) return;
                    cb.SlotObject(itemSlotType, modelPrefab);
                }
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
            if (!slotTable.ContainsKey(item.slotType)) return;
            EquipmentSlot slot = slotTable[item.slotType];
            if (slot.item == item)
            {
                slot.item = null;
            }
            item.StateData.Equipped = false;
            Encumbrance -= item.Weight;
            Encumbrance = Mathf.Max(Encumbrance, 0f);
            if (Actor == null ) return;
            if (item.templateData != null && Actor.TryGetComponent(out CharacterBody cb))
            {
                if (item.templateData.inPlace)
                {
                    cb.RemoveInplaceObject(item.templateData.id);
                }
                else
                {
                    cb.RemoveObject(item.slotType);
                    
                }
                cb.ToggleDefaultInplaceEquipment(item.slotType, true);
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
        
        /// <summary>
        /// Equips (visually) all equipped items
        /// </summary>
        public void SlotEquippedItems()
        {
            if (Actor == null) return;
            if (!Actor.TryGetComponent(out CharacterBody cb)) return;
            foreach (var key in slotTable.Keys)
            {
                if(slotTable[key] == null || slotTable[key].item == null) continue;
                GameObject modelPrefab = Librarian.Instance.GetItemPrefab(slotTable[key].item.modelPrefabId);
                cb.SlotObject(slotTable[key].item.slotType, modelPrefab);
            }
        }
    }
}