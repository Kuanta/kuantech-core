using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// A handler to keep track of slots on actor
    /// </summary>
    public class ActorSlotsHandler : ActorModule
    {
        [Serializable]
        public struct ActorSlotEntry
        {
            public string SlotName;
            public Transform Slot;
        }
        
        public List<ActorSlotEntry> ActorSlots = new List<ActorSlotEntry>();
        private Dictionary<string, Transform> _slots;

        public override void Initialize()
        {
            base.Initialize();
            if (ActorSlots.IsNullOrEmpty()) return;
            _slots = new Dictionary<string, Transform>();
            foreach (var entry in ActorSlots)
            {
                _slots[entry.SlotName] = entry.Slot;
            }
        }

        public Transform GetSlot(string slotName)
        {
            if (!_slots.ContainsKey(slotName)) return null;
            return _slots[slotName];
        }
    }
}