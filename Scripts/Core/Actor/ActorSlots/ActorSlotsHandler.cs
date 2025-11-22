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