using Kuantech.Data;
using UnityEngine;

namespace Kuantech.Rpg.Inventory
{

    public class Armor : Item
    {
        public ArmorData ArmorData;
        public Armor(ArmorData data) : base(data)
        {
            ArmorData = data;
        }
        public float GetArmorRating()
        {
            return ArmorData.armorValue * (1 + StateData.ItemLevel*ArmorData.scalingFactor);
        }
    }
}