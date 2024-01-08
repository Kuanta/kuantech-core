using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class VendingMachine : VenueInteractable
    {
        [Header("Vending Resource")]
        [SerializeField] private ResourceData VendingResource;
        [SerializeField] private ResourceInventory SourceInventory;
        [Header("Currency")]
        [SerializeField] private ResourceData OutputResource;
        [SerializeField] private ResourceInventory OutputInventory;

        public override bool CanBeInteractedWith(ArcadeIdleCharacter character)
        {
            if(SourceInventory.GetAvailableAmount(VendingResource.ResourceId) <= 0) return false; //Not enogh
            return base.CanBeInteractedWith(character);
        }

        protected override void HandleActor(ArcadeIdleCharacter character)
        {
            ResourceInventory characterInventory = character.CharacterInventory;
            if(characterInventory == null)
            {
                Debug.LogError($"{character.name} has no inventory");
                character.EndInteraction();
            }

            //Send to player
            ArcadeIdleActor.TransferResource(SourceInventory, characterInventory, VendingResource, true);
            
            //Add money
            OutputInventory.AddResource(OutputResource, null, false);

            character.EndInteraction();
        }
    }
}