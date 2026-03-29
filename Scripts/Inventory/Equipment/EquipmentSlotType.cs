using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Inventory
{
    [CreateAssetMenu(fileName = "EquipmentSlotType", menuName = "Kuantech/Rpg/EquipmentSlotType")]
    public class EquipmentSlotType: ScriptableObject
    {
        [Tooltip("Slot id")]
        public string Id;

        [Tooltip("Slot name in actor slot handler")]
        public string SlotName;
        public List<EquipmentSlotType> OverlappingSlot; //For two handed weapons
    }
}