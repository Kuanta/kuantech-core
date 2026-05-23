using Kuantech.Inventory;

namespace Kuantech.TotemHero
{
    public class LootComponentData : ItemComponentData
    {
        public int Rarity;
        public int Level;
        public override ItemComponent CreateInstance()
        {
           return new LootComponent(Rarity, Level);
        }
    }

    public class LootComponent : ItemComponent
    {
        public int Rarity;
        public int Level;

        public LootComponent(int rarity, int level)
        {
            Rarity = rarity;
            Level = level;
        }

        public override void OnItemAdded(Item item)
        {
        }

        public override void OnItemEquipped(Item item, EquipmentSlotType slotType)
        {
        }

        public override void OnItemRemoved(Item item)
        {
        }

        public override void OnItemUnequipped(Item item)
        {
        }

        public override void OnItemUsed(Item item)
        {
        }
    }
}