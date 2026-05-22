using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Inventory
{
    public class EquipableComponent : ItemComponent
    {
        private readonly List<EquipmentSlotType> _suitableSlots;
        private readonly List<EquipmentSlotType> _occupiedSlots;

        private bool _isEquipped;
        private EquipmentSlotType _equippedSlot;
        private string _equippedSlotId;

        [Serializable]
        private class EquipState
        {
            public bool IsEquipped;
            public string EquippedSlotId;
        }

        public EquipableComponent(EquipableComponentData data)
        {
            _suitableSlots = data.SuitableSlots;
            _occupiedSlots = data.OccupiedSlots;
        }

        // ── State ─────────────────────────────────────────────────────────────

        public bool IsEquipped() => _isEquipped;

        public EquipmentSlotType GetEquippedSlot() => _equippedSlot;

        public string GetEquippedSlotId()
        {
            if (_equippedSlot != null) return _equippedSlot.Id;
            return _equippedSlotId ?? "";
        }

        public void SetEquippedSlot(EquipmentSlotType slotType)
        {
            _equippedSlot = slotType;
            _equippedSlotId = slotType != null ? slotType.Id : "";
        }

        // Called when loading old saves that stored equipped state on the item directly
        public void InitFromLegacyState(bool isEquipped, string equippedSlotId)
        {
            _isEquipped = isEquipped;
            _equippedSlotId = equippedSlotId ?? "";
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public EquipmentSlotType GetSuitableSlot()
        {
            if (_suitableSlots.IsNullOrEmpty()) return null;
            return _suitableSlots[0];
        }

        public IReadOnlyList<EquipmentSlotType> GetSuitableSlots() => _suitableSlots;
        public IReadOnlyList<EquipmentSlotType> GetOccupiedSlots() => _occupiedSlots;

        // ── ItemComponent overrides ───────────────────────────────────────────

        public override int CanEquipItem(Item item, EquipmentSlotType slotType)
        {
            if (_suitableSlots == null || _suitableSlots.Count == 0) return -1;
            if (slotType == null) return 1;
            return _suitableSlots.Contains(slotType) ? 1 : -1;
        }

        public override void OnItemEquipped(Item item, EquipmentSlotType slotType)
        {
            if (slotType == null)
            {
                if (_suitableSlots == null || _suitableSlots.Count == 0) return;
                slotType = _suitableSlots[0];
            }
            _isEquipped = true;
            _equippedSlot = slotType;
            _equippedSlotId = slotType != null ? slotType.Id : "";
        }

        public override void OnItemUnequipped(Item item)
        {
            _isEquipped = false;
            _equippedSlot = null;
            _equippedSlotId = "";
        }

        public override void OnItemAdded(Item item) { }
        public override void OnItemRemoved(Item item) { }
        public override void OnItemUsed(Item item) { }

        // ── Serialization ─────────────────────────────────────────────────────

        public override string SerializeState()
        {
            return JsonUtility.ToJson(new EquipState
            {
                IsEquipped = _isEquipped,
                EquippedSlotId = GetEquippedSlotId()
            });
        }

        public override void DeserializeState(string data)
        {
            var state = JsonUtility.FromJson<EquipState>(data);
            _isEquipped = state.IsEquipped;
            _equippedSlotId = state.EquippedSlotId ?? "";
            // _equippedSlot resolved later by Inventory.RestoreEquipmentState → item.SetEquippedSlot
        }
    }
}
