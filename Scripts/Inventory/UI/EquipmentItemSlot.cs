using Kuantech.Core.UI;
using TMPro;
using UnityEngine;

namespace Kuantech.Inventory.UI
{
    public class EquipmentItemSlot : InventoryItemSlot
    {
        public EquipmentSlotType SlotType;
        [SerializeField] public TMP_Text SlotNameText;

        private Inventory _inventory;
        private Equipment _equipment;

        public void Setup(Inventory inventory, Equipment equipment)
        {
            _inventory = inventory;
            _equipment = equipment;
        }

        protected override bool ShouldAcceptItem(Item item)
        {
            if (item == null) return true;
            return item.CanEquip(SlotType);
        }

        public override bool CanAcceptDrop(UIDragSlot source)
        {
            if (source is not InventoryItemSlot other) return false;
            return ShouldAcceptItem(other.Item);
        }

        public override void OnDropReceived(UIDragSlot source)
        {
            if (_inventory == null || source is not InventoryItemSlot other) return;

            Item incoming = other.Item;
            Item outgoing = Item;

            if (incoming != null)
            {
                _inventory.EquipItem(incoming, SlotType);
                _equipment?.EquipItem(incoming, SlotType);
            }

            if (outgoing != null)
            {
                _inventory.UnequipItem(outgoing);
                _equipment?.UnequipItem(outgoing);
            }

            SetItem(incoming);
            other.SetItem(outgoing);
        }

        public void SetEquipmentSlotType(EquipmentSlotType slotType)
        {
            SlotType = slotType;
            if(SlotNameText != null)
            {
                SlotNameText.text = slotType.SlotName;
            }
        }
    }
}
