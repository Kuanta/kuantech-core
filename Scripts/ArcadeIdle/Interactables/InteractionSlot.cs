using System;
using System.Collections.Generic;
using Kuantech.AI.Utils;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class InteractionSlot : MonoBehaviour 
    {
        public bool Disabled = false;
        [KTTag("CharacterTag")]
        public List<int> AcceptedCharacterTags;
        public WorldZone InteractionZone;
        public ActorQueue ActorQueue;
        public float InteractionDistanceThreshold = 0.2f; //If character is far away, don't interact

        [Tooltip("Maximum amount of actors to hold. Set to -1 for unlimited actors")]
        public int MaxActorCount = -1;
        [Tooltip("An actor can't interact more than this value")]
        public float MaxInteractionTime = -1f;
        [Tooltip("A slight delay to the end interaction")]
        public float InteractionEndDelay = 0f;
        [Tooltip("If an interaction slot shouldn't be interacted with the player, set this to false")]
        public bool CanBeInteractedWithNonEmptyInventory = true;
        [NonSerialized] public VenueInteractable ParentInteractable;
        [NonSerialized] public HashSet<ArcadeIdleCharacter> OccupyingActors = new HashSet<ArcadeIdleCharacter>();

        public Action<ArcadeIdleCharacter> OnCharacterAssignedAction;
        public Action<ArcadeIdleCharacter> OnCharacterStartInteractionAction;
        public Action<ArcadeIdleCharacter> OnCharacterEndInteractionAction;

        [Header("Animation Parameters")]
        public int InteractionAnimationIndex = -1;

        /// <summary>
        /// Checks if the slot is occupied, meaning that it is at full capacity.
        /// </summary>
        /// <returns></returns>
        public bool IsOccupied()
        {
            if(MaxActorCount < 0) return false;
            return OccupyingActors.Count >= MaxActorCount;
        }

        /// <summary>
        /// Checks if there is any actor interacting with this slot
        /// </summary>
        /// <returns></returns>
        public bool HasInteractor()
        {
            return OccupyingActors.Count > 0;
        }

        /// <summary>
        /// Checkfs if this slot is available for a given character tag
        /// </summary>
        /// <param name="characterTag"></param>
        /// <returns></returns>
        public bool IsSlotAvailableForCharacter(ArcadeIdleCharacter character)
        {
            if(character.CharacterInventory != null && character.CharacterInventory.GetCarriedResourcesCount() > 0 && !CanBeInteractedWithNonEmptyInventory) 
            {
                //Character has non empty inventory, which is not ok for this inventory
                return false;
            }
            return AcceptedCharacterTags == null || AcceptedCharacterTags.Count == 0  ||
            (AcceptedCharacterTags != null && AcceptedCharacterTags.Contains(character.CharacterTag));
        }

        /// <summary>
        /// Checks if the slot is available for the given character tag
        /// </summary>
        /// <param name="characterTag"></param>
        /// <returns></returns>
        public bool IsSlotAvailableForCharacterTag(int characterTag)
        {
            return AcceptedCharacterTags == null || AcceptedCharacterTags.Count == 0 ||
            (AcceptedCharacterTags != null && AcceptedCharacterTags.Contains(characterTag));
        }

        /// <summary>
        /// Checks if a character is assigned to this slot
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public bool IsCharacterInTheSlot(ArcadeIdleCharacter character)
        {
            if(OccupyingActors != null && OccupyingActors.Contains(character)) return true;
            return false;
        }
        public void AddCharacter(ArcadeIdleCharacter character)
        {
            OccupyingActors.Add(character);
            character.OnAssignedToSlot(this);
            OnCharacterAssignedAction?.Invoke(character);
        }

        public void RemoveCharacater(ArcadeIdleCharacter character)
        {
            OccupyingActors.Remove(character);
            OnCharacterEndInteractionAction?.Invoke(character);
        }
   
        public void OnCharacterStartInteraction(ArcadeIdleCharacter character)
        {
            OnCharacterStartInteractionAction?.Invoke(character);
        }

        public WorldPoint GetTargetPoint()
        {
            if(InteractionZone != null)
            {
                return InteractionZone.SampleWorldPoint();
            }
            return new WorldPoint()
            {
                Position = transform.position,
                Rotation = transform.rotation,
            };
        }
    }
}