using System;
using System.Collections.Generic;
using Kuantech.Data;
using Kuantech.Inventory.Items;
using UnityEngine;

namespace Kuantech.UI
{
    [Serializable]
    public struct EquipmentSlotPair
    {
        public Enums.EquipmentSlotType SlotType;
        public EquipmentFrame Frame;
    }
    public class EquipmentsPanel : UIMenu
    {
        [SerializeField] private List<EquipmentSlotPair> FramesList = new List<EquipmentSlotPair>();
        private Dictionary<Enums.EquipmentSlotType, EquipmentFrame> Frames;
            
        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (Frames != null) return;
            Frames = new Dictionary<Enums.EquipmentSlotType, EquipmentFrame>();
            foreach (var pair in FramesList)
            {
                Frames[pair.SlotType] = pair.Frame;
            }
        }
        public void EquipItem(Item item, Enums.EquipmentSlotType slotType)
        {
            if(Frames == null) Initialize();
            Frames[slotType].EquipItem(item);
        }

        public void UnequipItem(Enums.EquipmentSlotType slotType)
        {
            if(Frames == null) Initialize();
            if (Frames[slotType] == null) return;
            Frames[slotType].UnequipItem();
        }
    }
}