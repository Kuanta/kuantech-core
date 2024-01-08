using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public abstract class InteractableComponent : MonoBehaviour {
        
        public VenueInteractable ParentInteractable;

        public virtual void Initialize(VenueInteractable parentInteractable)
        {
            ParentInteractable = parentInteractable;
        }

        public virtual void UpdateComponent()
        {

        }
        
        /// <summary>
        /// Called once a character is assigned to a slot
        /// </summary>
        /// <param name="character"></param>
        public virtual void OnCharacterAssigned(ArcadeIdleCharacter character, InteractionSlot slot)
        {

        }

        public virtual void OnInteractionStart(ArcadeIdleCharacter character)
        {

        }

        public virtual void OnInteractionEnd(ArcadeIdleCharacter character)
        {

        }

    }
}