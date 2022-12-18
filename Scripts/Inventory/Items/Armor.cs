
namespace Kuantech.Inventory.Items
{
    public class Armor : Item
    {
        public float armorRating = 1;
        public Armor(ArmorData data) : base(data)
        {
            armorRating = data.armorValue;
        }
        private float GetDefenseRating()
        {
            return armorRating;
        }
    }
}