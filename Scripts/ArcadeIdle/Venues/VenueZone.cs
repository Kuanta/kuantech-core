using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class VenueZone : MonoBehaviour, IUnlockable {
        [KTTag("ZoneTags")]
        public int ZoneTag;
        public string ZoneId;
        [SerializeField] private List<GameObject> Blockers;
        [NonSerialized] public List<VenueActor> VenueActors;
        [NonSerialized] public ArcadeIdleVenue ParentVenue;
        public bool UnlockedByDefault;
        [NonSerialized] public bool Unlocked;
        public void Initialize(ArcadeIdleVenue venue)
        {
            ParentVenue = venue;
            VenueActors = GetComponentsInChildren<VenueActor>().ToList();
            foreach (var actor in VenueActors)
            {
                actor.ParentZone = this;
                if (actor.Id.IsNullOrEmpty())
                {
                    actor.Initialize(); //Interactable still desires to be initialized
                    continue;
                }
                
                ActorState actorState = null;
                if(venue.CurrentState != null && venue.CurrentState.VenueActorStates.ContainsKey(actor.Id))
                {   
                    actorState = venue.CurrentState.VenueActorStates[actor.Id];
                }

                actor.Initialize(actorState);
            }

            foreach (var interactable in VenueActors)
            {
                interactable.PostInitialize();
            }
        }

        public bool AssignToRandomZoneInteractable(ArcadeIdleNpc npc, List<int> interactableTags)
        {
            return ArcadeIdleVenue.AssignToRandomInteractable(npc, interactableTags, VenueActors);
        }

        /// <summary>
        /// Checks if the zone has available interaction slots for the character. Queues don't count.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public bool CanAcceptCharacter(ArcadeIdleCharacter character)
        {
            foreach(var venueActor in VenueActors)
            {
                VenueInteractable interactable = venueActor.GetComponent<VenueInteractable>();
                if (interactable == null || venueActor.IsLocked()) continue;
                if(interactable.HasAvailableSlots(character)) return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the zone has available slots or queues for the character.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public bool CanQueueCharacter(ArcadeIdleCharacter character)
        {
            foreach (var venueActor in VenueActors)
            {
                VenueInteractable interactable = venueActor.GetComponent<VenueInteractable>();
                if (interactable == null || venueActor.IsLocked()) continue;
                if (interactable.HasAvailableSlots(character) || interactable.HasAvailableQueue(character)) return true;
            }
            return false;
        }

        public bool IsLocked()
        {
            return !UnlockedByDefault && !Unlocked;
        }
        public void Unlock()
        {
            Unlocked = true;
            ParentVenue.DirtyZonestate(this);
        }
        public void Toggle(bool toggle)
        {
            gameObject.SetActive(toggle);
            if(Blockers != null) 
            {
                foreach(var blocker in Blockers)
                {
                    blocker.SetActive(!toggle);
                }
            }
        }
    }
}