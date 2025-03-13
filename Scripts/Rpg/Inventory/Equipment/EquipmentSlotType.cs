using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Rpg.Inventory
{
    [CreateAssetMenu(fileName = "EquipmentSlotType", menuName = "Kuantech/Rpg/EquipmentSlotType")]
    public class EquipmentSlotType: ScriptableObject
    {
        public string SlotName;
        public List<EquipmentSlotType> OverlappingSlot; //For two handed weapons
    }
}