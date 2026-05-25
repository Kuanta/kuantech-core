using System;
using System.Collections.Generic;
using Kuantech.Inventory;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public class ActorVisualPartsHandler
    {
        private Dictionary<VisualSlotType, List<ActorVisualPart>> _partsBySlot;

        /// <summary>
        /// Auto-discovers all ActorVisualPart children (including inactive) and builds the slot map.
        /// Defaults start visible; non-default equipment pieces start hidden.
        /// </summary>
        public void Initialize(MonoBehaviour root)
        {
            _partsBySlot = new Dictionary<VisualSlotType, List<ActorVisualPart>>();
            var parts = root.GetComponentsInChildren<ActorVisualPart>(includeInactive: true);
            foreach (var part in parts)
            {
                if (part.VisualSlot == null) continue;
                if (!_partsBySlot.TryGetValue(part.VisualSlot, out var list))
                    _partsBySlot[part.VisualSlot] = list = new List<ActorVisualPart>();
                list.Add(part);
            }
            foreach (var list in _partsBySlot.Values)
                foreach (var part in list)
                    part.Toggle(part.IsDefault);
        }

        /// <summary>
        /// Called when an item visual is assigned to a slot.
        /// Shows the equipped in-place part, hides all others in that slot, then masks declared slots.
        /// </summary>
        public void OnSlotEquipped(VisualSlotType slot, ItemVisual equippedVisual)
        {
            if (slot == null) return;
            ActorVisualPart equippedPart = equippedVisual as ActorVisualPart;

            if (_partsBySlot.TryGetValue(slot, out var parts))
                foreach (var p in parts)
                    p.Toggle(p == equippedPart);

            if (equippedVisual == null || equippedVisual.SlotsToMask == null) return;
            foreach (var masked in equippedVisual.SlotsToMask)
                SetDefaultsVisible(masked, false);
        }

        /// <summary>
        /// Called when an item visual is removed from a slot.
        /// Restores masked slots and shows defaults for the vacated slot.
        /// </summary>
        public void OnSlotUnequipped(VisualSlotType slot, ItemVisual unequippedVisual)
        {
            if (unequippedVisual != null && unequippedVisual.SlotsToMask != null)
                foreach (var masked in unequippedVisual.SlotsToMask)
                    SetDefaultsVisible(masked, true);

            if (slot != null) SetDefaultsVisible(slot, true);
        }

        private void SetDefaultsVisible(VisualSlotType slot, bool visible)
        {
            if (!_partsBySlot.TryGetValue(slot, out var parts)) return;
            foreach (var p in parts)
                if (p.IsDefault) p.Toggle(visible);
        }
    }
}
