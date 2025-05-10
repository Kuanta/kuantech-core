using System;
using System.Collections.Generic;
using Kuantech.Core.Data;

namespace Kuantech.Rpg.Inventory
{
    public enum ItemRarities
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
    }
    
    [Serializable]
    public class ItemData : VaultData
    {
        public string Name;
        public string Description = "";
        public float weight;
        public float value;
        public bool stackable = false;
        public List<EquipmentSlotType> SuitableSlots; //Which slot can this be equipped to
        public List<EquipmentSlotType> OccupiedSlots; //Which slots does this occupy?
        public ItemType ItemType;
        
        //Visuals
        public string ItemTemplateId;
        
        // Icon
        public string IconId;
    }
}