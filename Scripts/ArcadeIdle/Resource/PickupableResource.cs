using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class PickupableResource : ResourceVisual {
        [SerializeField] private ResourceDataReference ResourceDataReference;
        [SerializeField] private Collider Collider;
        public bool Available = true;
        public bool DestroyOnPickup = true;
        
        public override void Spawn()
        {
            base.Spawn();
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

            //Clear the parent of the visual just in case since the pickup can be destroyed
            ResourceId = ResourceDataReference.ResourceId;

            //Check inventory space
            if (!character.CharacterInventory.CanAcceptResource(ResourceDataReference.GetResourceData())) return;

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