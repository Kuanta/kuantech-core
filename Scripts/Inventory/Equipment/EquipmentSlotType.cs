using UnityEngine;

namespace Kuantech.Inventory
{
    [CreateAssetMenu(fileName = "EquipmentSlotType", menuName = "Kuantech/Rpg/EquipmentSlotType")]
    public class EquipmentSlotType : ScriptableObject
    {
        [Tooltip("Slot id")]
        public string Id;

        [Tooltip("Socket transform name on the actor")]
        public string SlotName;
        public Sprite DefaultIcon;
    }
}