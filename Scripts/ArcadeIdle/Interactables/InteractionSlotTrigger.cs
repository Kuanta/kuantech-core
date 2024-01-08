using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class InteractionSlotTrigger : ArcadeIdleTriggerZone
    {
        [SerializeField] private InteractionSlot Slot;
        [Tooltip("If actor should start interacting as soon as it triggers the collider")]

        protected override void OnActorEnter(ArcadeIdleCharacter character)
        {
            base.OnActorEnter(character);
            if(!character.CanInteractWith(Slot.ParentInteractable) || !Slot.IsSlotAvailableForCharacter(character) || Slot.Disabled) return; 
            if(character.AssignedInteractable != Slot.ParentInteractable && character.AssignedInteractable != null)
            {
                character.EndInteraction();
                Debug.LogWarning("Ending the previous interaction of the player");
            }

            AssignActorToSlot();
        }

        private bool ActorIsSuitableForInteraction(ArcadeIdleCharacter character)
        {
            if(character == null) return false;
            return character.CanInteractWith(Slot.ParentInteractable) && Slot.IsSlotAvailableForCharacter(character);
        }
        protected override void OnActorLeave(ArcadeIdleCharacter character)
        {
            base.OnActorLeave(character);
            if(character.AssignedInteractable == Slot.ParentInteractable)
            {
                character.EndInteraction();
            }
        }

        private void Update()
        {
            if(Slot.Disabled || CurrentActor == null || (CurrentActor.AssignedInteractable != null && CurrentActor.AssignedInteractable != Slot.ParentInteractable)) return;
            if(CurrentActor != null && CurrentActor.AssignedInteractable == null && !Slot.Disabled && ActorIsSuitableForInteraction(CurrentActor))
            {
                //This is like OnTriggerStay. Check every frame if the slot is available
                AssignActorToSlot();
            }
        }

        private void AssignActorToSlot()
        {
            //Try to attach the actor to the slot
            
            bool result =  Slot.ParentInteractable.AssignActorToSlot(CurrentActor, Slot);
            if(result)
            {
                CurrentActor.OnReachedToSlot();
            }
        }
    }
}