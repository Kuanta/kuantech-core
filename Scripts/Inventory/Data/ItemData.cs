using System;
using Kuantech.Core.Data;

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
        public bool stackable;
        public string ItemTemplateId;
        public string IconId;
    }
}
