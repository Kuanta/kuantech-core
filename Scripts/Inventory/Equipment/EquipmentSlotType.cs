using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Inventory
{
    [CreateAssetMenu(fileName = "EquipmentSlotType", menuName = "Kuantech/Rpg/EquipmentSlotType")]
    public class EquipmentSlotType : MetadataAsset
    {
        [Tooltip("Socket transform name on the actor")]
        public string SlotName;
    }
}