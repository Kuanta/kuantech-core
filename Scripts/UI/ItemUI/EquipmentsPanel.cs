using System.Collections.Generic;
using Kuantech.Data;
using Kuantech.Inventory.Items;
using UnityEngine;

namespace Kuantech.UI
{
    public struct EquipmentSlotPair
    {
        public Enums.EquipmentSlotType SlotType;
        public EquipmentFrame Frame;
    }
    public class EquipmentsPanel : MonoBehaviour
    {
        [SerializeField] private List<EquipmentSlotPair> FramesList = new List<EquipmentSlotPair>();
        private Dictionary<Enums.EquipmentSlotType, EquipmentFrame> Frames = new Dictionary<Enums.EquipmentSlotType, EquipmentFrame>();
            
        private void Awake()
        {
            foreach (var pair in FramesList)
            {
                Frames[pair.SlotType] = pair.Frame;
            }
        }

        public void EquipItem(Item item, Enums.EquipmentSlotType slotType)
        {
            Frames[slotType].EquipItem(item);
        }

        public void UnequipItem(Item item)
        {
            foreach (var pair in Frames)
            {
                if (pair.Value.AssignedItem == item)
                {
                    pair.Value.UnequipItem();
                    return;
                }
            }
        }

        public void UnequipItem(Enums.EquipmentSlotType slotType)
        {
            Frames[slotType].UnequipItem();
        }
    }
}