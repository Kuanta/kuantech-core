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

        private ActorVisualHandler _actorVisualHandler;
        
        public override void Initialize()
        {
            base.Initialize();
            SetExistingActorSlots();
        }

        private void SetExistingActorSlots()
        {
            if(_slots == null) _slots = new Dictionary<string, Transform>();
            if(ActorSlots.IsNullOrEmpty()) return;
            foreach (var entry in ActorSlots)
            {
                _slots[entry.SlotName] = entry.Slot;
            }
            
            //Get slots
            ActorSlot[] slots = Actor.GetComponentsInChildren<ActorSlot>();
            foreach (var slot in slots)
            {
                if (_slots.ContainsKey(slot.ActorSlotName))
                {
                    Debug.LogWarning("Duplicate slot name found: " + slot.ActorSlotName + ". Overriding existing slot.");
                    continue;
                }
                _slots[slot.ActorSlotName] = slot.transform;
            }
        }
        
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _actorVisualHandler = Actor.GetModule<ActorVisualHandler>();
            if (_actorVisualHandler != null)
            {
                _actorVisualHandler.OnActorVisualSet += OnActorVisualSet;
                _actorVisualHandler.OnActorVisualRemoved += OnActorVisualRemoved;

                if (_actorVisualHandler.CurrentActorVisual != null)
                    OnActorVisualSet(_actorVisualHandler.CurrentActorVisual);
            }
        }
        

        public Transform GetSlot(string slotName)
        {
            if (!_slots.ContainsKey(slotName)) return null;
            return _slots[slotName];
        }

        /// <summary>
        /// Registers (or overwrites) a single slot at runtime -- for anything that attaches to the actor
        /// AFTER the bulk ActorSlot scans above already ran (Initialize, ActorVisual swap), most notably an
        /// equipped item's own attack point (see WeaponVisual.AttackPoint). Overwrites whatever was already
        /// registered under the same name.
        /// </summary>
        public void RegisterSlot(string slotName, Transform slot)
        {
            if (string.IsNullOrEmpty(slotName) || slot == null) return;
            if (_slots == null) _slots = new Dictionary<string, Transform>();
            _slots[slotName] = slot;
        }

        /// <summary>
        /// Removes a slot registered via RegisterSlot -- only if it's still the SAME transform, so an
        /// unequip racing a re-equip of a different item can't clobber the newer registration.
        /// </summary>
        public void UnregisterSlot(string slotName, Transform slot)
        {
            if (string.IsNullOrEmpty(slotName) || _slots == null) return;
            if (_slots.TryGetValue(slotName, out Transform current) && current == slot)
                _slots.Remove(slotName);
        }

        public void OnActorVisualSet(ActorVisual actorVisual)
        {
            if (_slots == null) _slots = new Dictionary<string, Transform>();
            //Check last slots
            if (_actorVisualHandler.CurrentActorVisual != null)
            {
                ActorSlot[] slotsFromOld = _actorVisualHandler.CurrentActorVisual.GetComponentsInChildren<ActorSlot>();
                if (!slotsFromOld.IsNullOrEmpty())
                {
                    foreach (var oldSlot in slotsFromOld)
                    {
                        if (_slots.ContainsKey(oldSlot.ActorSlotName))
                        {
                            _slots.Remove(oldSlot.ActorSlotName);
                        }
                    }
                }
            }
            ActorSlot[] slots = actorVisual.GetComponentsInChildren<ActorSlot>();
            if (slots.IsNullOrEmpty()) return;
            foreach (var slot in slots)
            {
                if(slot.ActorSlotName.IsNullOrEmpty()) continue;
                _slots[slot.ActorSlotName] = slot.transform;
            }
        }

        public void OnActorVisualRemoved(ActorVisual actorVisual)
        {
            _slots.Clear();
            SetExistingActorSlots();
        }
    }
}