using Kuantech.Data;
using UnityEngine;

namespace Kuantech.Rpg.Inventory
{

    public class Armor : Item
    {
        public float ArmorRating = 1;
        public float ScalingFactor = 1;
        public Enums.ArmorType ArmorType;
        public Armor(ArmorData data) : base(data)
        {
            ArmorRating = data.armorValue;
            Type = Enums.ItemType.Armor;
            ScalingFactor = data.scalingFactor;
            ArmorType = data.armorType;
        }
        public float GetArmorRating()
        {
            return ArmorRating * (1 + StateData.ItemLevel*ScalingFactor);
        }
    }
}