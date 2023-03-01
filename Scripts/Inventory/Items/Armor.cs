using Kuantech.Data;

namespace Kuantech.Inventory.Items
{
    public class Armor : Item
    {
        public float ArmorRating = 1;
        public float ScalingFactor = 1;
        public Armor(ArmorData data) : base(data)
        {
            ArmorRating = data.armorValue;
            Type = Enums.ItemType.Armor;
            ScalingFactor = data.scalingFactor;
        }
        public float GetArmorRating()
        {
            return ArmorRating * (1 + StateData.ItemLevel*ScalingFactor);
        }
    }
}