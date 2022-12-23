using Kuantech.Data;

namespace Kuantech.Inventory.Items
{
    public class Armor : Item
    {
        public float armorRating = 1;
        public Armor(ArmorData data) : base(data)
        {
            armorRating = data.armorValue;
            Type = Enums.ItemType.Armor;
        }
        private float GetDefenseRating()
        {
            return armorRating;
        }
    }
}