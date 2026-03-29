using System;

namespace Kuantech.Inventory
{
    [Serializable]
    public class ArmorData : ItemData
    {
        public float armorValue = 0f;
        public float scalingFactor = 1;
    }
}