using Kuantech.Inventory;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class PickupableResource : ResourceVisual {
        public ResourceDataReference ResourceDataReference;
        [SerializeField] private Collider Collider;
        public bool Available = true;
        public bool DestroyOnPickup = true;
        
        public override void Spawn(ItemData parentData)
        {
            base.Spawn(parentData);
            ResourceDataReference.SetId(parentData.Id);
            Available = true;
            Collider.enabled = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if(!Available) return;
            if(other.gameObject.TryGetComponent(out ArcadeIdleCharacter character))
            {
                HandlePickup(character);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!Available) return;
            if (other.gameObject.TryGetComponent(out ArcadeIdleCharacter character))
            {
                HandlePickup(character);
            }
        }

        private void HandlePickup(ArcadeIdleCharacter character)
        {
            if(IsMoving) return;
            if(character.CharacterInventory == null) return;

            //Check inventory space
            ResourceData data = ItemData as ResourceData;
            if (!character.CharacterInventory.CanAcceptResource(data)) return;

            //Send the resource flying
            character.CharacterInventory.AddResource(ResourceDataReference.GetResourceData(), this, true);

            Toggle(false);
            if(DestroyOnPickup)
            {
                Destroy(gameObject);
            }
        }

        public void Toggle(bool toggle)
        {
            Collider.enabled = toggle;
            Available = toggle;
        }
    }
}