using System;

namespace Kuantech.Inventory
{
    [Serializable]
    public abstract class ItemComponentData
    {
        public abstract ItemComponent CreateInstance();
    }
}
