using System;
using System.Collections.Generic;

namespace Kuantech.Rpg.Inventory
{
        
    [Serializable]
    public class ItemData
    {
        // Common mandotary
        public string Id;
        public string Name;
        public float weight;
        public float value;
        public bool stackable = false;
        public List<EquipmentSlotType> SuitableSlots; //Which slot can this be equipped to
        public List<EquipmentSlotType> OccupiedSlots; //Which slots does this occupy?
        public ItemType ItemType;
        public int minPowerLevel;
        public int maxPowerLevel;
        
        //Visuals
        public string ItemTemplateId;
        public ItemVisual ItemVisualPrefab;
        
        // Icon
        public int iconId;
        public string description = "";
    }
}