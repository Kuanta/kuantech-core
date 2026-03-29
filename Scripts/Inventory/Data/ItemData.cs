using System;
using System.Collections.Generic;
using Kuantech.Core.Data;
using UnityEngine;

namespace Kuantech.Inventory
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
        public float weight;
        public float value;
        public bool stackable = false;
        
        [SerializeReference]
        public List<ItemComponent> Components;

        //Visuals
        public string ItemTemplateId;
        
        // Icon
        public string IconId;
    }
}