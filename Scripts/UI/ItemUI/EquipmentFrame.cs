using System;
using Kuantech.Core;
using Kuantech.Data;
using Kuantech.Inventory.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.UI
{
    public class EquipmentFrame  : MonoBehaviour
    {
        public Image ItemIcon;
        public TMP_Text SlotText;
        public Item AssignedItem = null;
        public Button FrameButton;

        private void Awake()
        {
            if (FrameButton != null)
            {
                FrameButton.onClick.AddListener(UnequipItem);
            }
        }

        public void EquipItem(Item item)
        {
            ItemIcon.sprite = Librarian.Instance.GetIconFromItemId(item.Id);
            AssignedItem = item;
        }

        public void UnequipItem()
        {
            if (AssignedItem == null) return;
            AssignedItem.Unequip();
            AssignedItem = null;
        }
        
        public void SetSlot(Enums.EquipmentSlotType slotType)
        {
            switch (slotType)
            {
                case Enums.EquipmentSlotType.Head:
                    SlotText.text = "Head";
                    break;
                case Enums.EquipmentSlotType.None:
                    SlotText.text = "";
                    break;
                case Enums.EquipmentSlotType.MainHand:
                    SlotText.text = "Weapon";
                    break;
                case Enums.EquipmentSlotType.OffHand:
                    SlotText.text = "Off Weapon";
                    break;
                case Enums.EquipmentSlotType.Chest:
                    SlotText.text = "Chest";
                    break;
                case Enums.EquipmentSlotType.Legs:
                    SlotText.text = "Legs";
                    break;
                case Enums.EquipmentSlotType.Feet:
                    SlotText.text = "Feet";
                    break;
                case Enums.EquipmentSlotType.Arms:
                    SlotText.text = "Arms";
                    break;
                case Enums.EquipmentSlotType.Shoulders:
                    SlotText.text = "Shoulders";
                    break;
                case Enums.EquipmentSlotType.Back:
                    SlotText.text = "Back";
                    break;
                case Enums.EquipmentSlotType.Ring:
                    SlotText.text = "Ring";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slotType), slotType, null);
            }
        }
    }
}