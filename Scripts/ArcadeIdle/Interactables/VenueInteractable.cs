using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class VenueInteractable : ActorModule
    {
        public int InteractablePriority = 0;
        [SerializeField] private List<InteractionSlot> InteractionPoints;
        public bool Disabled = false;

        private List<InteractableComponent> _interactableComponents;

        public override void Initialize()
        {
            base.Initialize();

            _interactableComponents = GetComponents<InteractableComponent>().ToList();
            if(_interactableComponents == null) return;
            
            //Initialize or reset slot
            foreach(var interactionPoint in InteractionPoints)
            {
                interactionPoint.OccupyingActors = new HashSet<ArcadeIdleCharacter>();
                interactionPoint.ParentInteractable = this;
            }

            foreach(var comp in _interactableComponents)
            {
                comp.Initialize(this);
            }
        }

        protected virtual void Update()
        {
            if(!Initialized) return;
            HandleActors();
            HandleComponents();
            HandleInteractionSlots();
        }

        public VenueActor GetParentActor()
        {
            return Actor as VenueActor;
        }

        public VenueZone GetParentZone()
        {
            VenueActor parentActor = GetParentActor();
            if(parentActor == null) return null;
            return GetParentActor().ParentZone;
        }

        #region Interaction
        /// <summary>
        /// Checks the queues and assigns actors to slots if there are actors in queues
        /// </summary>
        private void HandleInteractionSlots()
        {
            foreach(var slot in InteractionPoints)
            {
                if(slot.IsOccupied() || slot.ActorQueue == null || slot.Disabled) continue;
                ArcadeIdleCharacter character = slot.ActorQueue.DequeueActor();
                if(character == null) continue;
                AssignActorToSlot(character,slot);
            }
        }
        private int _interactingChacaters = 0;
        public virtual void HandleActors()
        {
            _interactingChacaters = 0;
            foreach(var slot in InteractionPoints)
            {
                HashSet<ArcadeIdleCharacter> slotCharacters = new HashSet<ArcadeIdleCharacter>(slot.OccupyingActors); //Copy a list so that EndInteraction doesn't modify looped list
                foreach(ArcadeIdleCharacter attachedCharacter in slotCharacters)
                {
                    if (!attachedCharacter.StartedInteracting) continue; //This character is still on its way
                    if (attachedCharacter.GetInteractionElapsedTime() > slot.MaxInteractionTime && slot.MaxInteractionTime > 0)
                    {
                        attachedCharacter.EndInteraction();
                        continue;
                    }

                    HandleActor(attachedCharacter);
                    //Check if character is interacting and not on its way towards the slot
                    if (attachedCharacter.StartedInteracting) { }
                    _interactingChacaters++;
                }

            }
        }

        protected virtual void HandleActor(ArcadeIdleCharacter character)
        {

        }

        public virtual void HandleComponents()
        {
            foreach (var comp in _interactableComponents)
            {
                comp.UpdateComponent();
            }
        }
        public virtual bool Interact(ArcadeIdleCharacter actor)
        {
            return true;
        }

        /// <summary>
        /// Returns the number of interacting characters.
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfInteractingCharacters()
        {
            return _interactingChacaters;
        }
        #endregion

        #region Assignment

        /// <summary>
        /// Checks if the interactable is available for the given character tag.
        /// </summary>
        /// <returns></returns>
        public virtual bool CanBeInteractedWith(ArcadeIdleCharacter character)
        {
            if (!character.CanInteractWith(this) || InteractionPoints == null || InteractionPoints.Count == 0)
            {
                return false;
            }

            return HasAvailableSlots(character) || HasAvailableQueue(character);
        }

        /// <summary>
        /// Checks if the interactable has available slot for the character type
        /// </summary>
        /// <param name="characterTag"></param>
        /// <returns></returns>
        public bool HasAvailableSlots(ArcadeIdleCharacter character)
        {
            foreach (var slot in InteractionPoints)
            {
                if (!slot.IsSlotAvailableForCharacter(character) || slot.IsOccupied() || slot.Disabled)
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if interactable is suitable for given character tag
        /// </summary>
        /// <param name="characterTag"></param>
        /// <returns></returns>
        public bool HasAvailableSlots(int characterTag)
        {
            foreach (var slot in InteractionPoints)
            {
                if (!slot.IsSlotAvailableForCharacterTag(characterTag) || slot.IsOccupied() || slot.Disabled)
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the interactable has queue slot for the character type
        /// </summary>
        /// <param name="characterTag"></param>
        /// <returns></returns>
        public bool HasAvailableQueue(ArcadeIdleCharacter character)
        {
            foreach (var slot in InteractionPoints)
            {
                if (!slot.IsSlotAvailableForCharacter(character) || slot.ActorQueue == null || !slot.ActorQueue.IsAvailable())
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        public bool HasAvailableQueue(int characterTag)
        {
            foreach (var slot in InteractionPoints)
            {
                if (!slot.IsSlotAvailableForCharacterTag(characterTag) || slot.ActorQueue == null || !slot.ActorQueue.IsAvailable())
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        public int GetActorCountInQueue(ArcadeIdleCharacter character)
        {
            int sum = 0;
            foreach (var slot in InteractionPoints)
            {
                if (!slot.IsSlotAvailableForCharacter(character) || slot.ActorQueue == null || !slot.ActorQueue.IsAvailable())
                {
                    continue;
                }
                sum += slot.ActorQueue.GetActorInQueueCount();
            }
            return sum;
        }
        /// <summary>
        /// Adds the actor to a slot, or a queue if no slot is available (and a queue is available).
        /// </summary>
        /// <param name="character"></param>
        /// <returns>Returns true if actor is added to a slot or a queue.</returns>
        public bool AddInteractor(ArcadeIdleNpc character)
        {
            if(character.AssignedInteractable == this) return true; //Character already assigned here
            if(!CanBeInteractedWith(character)) return false;
            if(character.AssignedInteractable != this && character.AssignedInteractable != null)
            {
                //End interaction with previous interactable
                character.EndInteraction(true);
            }
            ActorQueue fallbackQueue;
            InteractionSlot slot = GetAvailableSlot(character, out fallbackQueue);
            if(slot != null)
            {

                AssignActorToSlot(character, slot);
                return true;
            }
            if(fallbackQueue == null) return false;
            QueueActor(character, fallbackQueue);
            return true;
        }

        /// <summary>
        /// Gets an available slot for a given character tag.
        /// </summary>
        /// <param name="characterTag">Character tag</param>
        /// <param name="fallbackQueue">Fallback queue to queue the actor</param>
        /// <returns></returns>
        public InteractionSlot GetAvailableSlot(ArcadeIdleCharacter character, out ActorQueue fallbackQueue)
        {
            fallbackQueue = null;
            if (InteractionPoints == null || InteractionPoints.Count == 0)
            {
                return null;
            }
            foreach (var slot in InteractionPoints)
            {
                if(!slot.IsSlotAvailableForCharacter(character) || slot.IsOccupied() || slot.Disabled) {
                    //Is queue available?
                    if(fallbackQueue == null && slot.ActorQueue != null && slot.ActorQueue.IsAvailable())
                    {
                        fallbackQueue = slot.ActorQueue;
                    }
                    continue;
                }
                return slot;
            }
            return null;
        }

        
        /// <summary>
        /// Assigns actor to a given slot
        /// </summary>
        /// <param name="character"></param>
        /// <param name="slot"></param>
        public bool AssignActorToSlot(ArcadeIdleCharacter character, InteractionSlot slot)
        {
            if(slot.IsOccupied() || slot.Disabled || !character.CanInteractWith(this)) return false;
            character.AssignedSlot = slot;
            character.AssignedInteractable = this;
            slot.AddCharacter(character);
            character.SetInteractable(this);

            foreach (var comp in _interactableComponents)
            {
                comp.OnCharacterAssigned(character, slot);
            }
 
            return true;
        }

        /// <summary>
        /// Queues an actor. Since queue only makes sense for npcs, it accepts an npc
        /// </summary>
        /// <param name="actor"></param>
        public void QueueActor(ArcadeIdleNpc actor, ActorQueue queue)
        {
            actor.AssignedQueue = queue;
            actor.SetInteractable(this);
            queue.QueueActor(actor);
        }

        /// <summary>
        /// Removes an actor from the queue or an interaction slot
        /// </summary>
        /// <param name="character"></param>
        public void RemoveActor(ArcadeIdleCharacter character)
        {
            if (character.AssignedInteractable != this)
            {
                Debug.LogWarning("Trying to remove an actor from an interactable it doesn't belong");
                return;
            }

            if (character.AssignedQueue != null)
            {
                character.AssignedQueue.RemoveFromQueue(character as ArcadeIdleNpc);
                character.AssignedQueue = null;
            }

            if (character.AssignedSlot == null)
            {
                Debug.LogWarning("Trying to remove an actor from a slot while its assigned slot is null");
            }

            foreach (var comp in _interactableComponents)
            {
                comp.OnInteractionEnd(character);
            }

            var slot = InteractionPoints.FirstOrDefault(slot => slot.OccupyingActors.Contains(character));
            if (slot == null) return;
            slot.RemoveCharacater(character);
            character.AssignedSlot = null;
            character.AssignedInteractable = null;
            OnActorRemoved(character);
        }

        /// <summary>
        /// Called when a character reaches the slot
        /// </summary>
        /// <param name="character"></param>
        public virtual void OnActorReachedInteractable(ArcadeIdleCharacter character)
        {
            character.AssignedSlot.OnCharacterStartInteraction(character);
            foreach (var comp in _interactableComponents)
            {
                comp.OnInteractionStart(character);
            }
        }

        public virtual void OnActorRemoved(ArcadeIdleCharacter character)
        {

        }
        #endregion
    }
}