using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class ResourceSinker : VenueInteractable
    {
        [Header("Properties")] 
        public ResourceInventory TargetInventory;
        public List<ResourceData> AcceptedResources;
        [SerializeField] private ArcadeIdleTriggerZone TriggerZone;
        public float SinkRate = 0.1f;
        private float _lastSinkTime = 0.0f;

        protected override void Update()
        {
            if (!Initialized) return;
            base.Update();
            if (TriggerZone != null)
            {
                foreach (var actor in TriggerZone.EnteredActors)
                {
                    if(actor == null || !actor.CanInteractWith(this)) continue;
                    HandleActor(actor);
                }
            }
        }
        protected override void HandleActor(ArcadeIdleCharacter character)
        {
            base.HandleActor(character);
            //Check if sinker is full
            if(TargetInventory.IsFull())
            {
                //Since sinker can't accept any more resource, simply fail
                if(character.AssignedInteractable == this) character.EndInteraction();
                return;
            }
            ResourceInventory actorInventory = character.GetModule<ResourceInventory>();
            if (Time.time - _lastSinkTime < SinkRate || actorInventory == null) return;
            ResourceData resourceToSend = null;
            foreach (var accepted in AcceptedResources)
            {
                if (actorInventory.CanGiveResource(accepted))
                {
                    resourceToSend = accepted;
                    break;
                }
            }
            
            //Actor probably can't send anymore
            if (resourceToSend == null) 
            {
                if (character.AssignedInteractable == this) character.EndInteraction();
                return;
            }
            bool result = ArcadeIdleActor.TransferResource(actorInventory,
                TargetInventory,
                resourceToSend,
                true);
            if (result) _lastSinkTime = Time.time;
        }

        public bool CanSinkResource(ResourceData resourceData)
        {
            return TargetInventory.CanAcceptResource(resourceData);
        }

        public override bool CanBeInteractedWith(ArcadeIdleCharacter character)
        {
            if(TargetInventory == null || TargetInventory.IsFull()) return false;
            return base.CanBeInteractedWith(character);
        }
    }
}