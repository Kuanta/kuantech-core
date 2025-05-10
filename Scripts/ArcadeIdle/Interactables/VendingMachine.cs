using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class VendingMachine : VenueInteractable
    {
        [Header("Vending Resource")]
        [SerializeField] private ResourceDataReference VendingResource;
        [SerializeField] private ResourceInventory SourceInventory;
        
        [Header("Currency")]
        [SerializeField] private ResourceDataReference OutputResource;
        [SerializeField] private ResourceInventory OutputInventory;

        public override bool CanBeInteractedWith(ArcadeIdleCharacter character)
        {
            if(SourceInventory.GetAvailableAmount(VendingResource.GetId()) <= 0) return false; //Not enogh
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
            ArcadeIdleActor.TransferResource(SourceInventory, characterInventory, VendingResource.GetResourceData(), true);
            
            //Add money
            OutputInventory.AddResource(OutputResource.GetResourceData(), null, false);

            character.EndInteraction();
        }
    }
}