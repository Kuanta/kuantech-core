using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class PickupableResource : MonoBehaviour {
        [SerializeField] private ResourceData ResourceData;
        [SerializeField] private  ResourceVisual Visual;
        [SerializeField] private Collider Collider;
        public bool Available = true;
        public bool DestroyOnPickup = true;

        public void Initialize()
        {
            if(Visual == null)
            {
                Visual = ResourceData.GetResourceVisual();
                Visual.transform.SetParent(transform);
                Visual.transform.localPosition = Vector3.zero;
                Visual.transform.localRotation = Quaternion.identity;
            }
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

        private void HandlePickup(ArcadeIdleCharacter character)
        {
            if(character.CharacterInventory == null) return;
            
            //Clear the parent of the visual just in case since the pickup can be destroyed
            if(Visual != null)
            {
                Visual.ResourceId = ResourceData.ResourceId;
                Visual.transform.SetParent(null, true);
            }

            //Check inventory space
            if(!character.CharacterInventory.CanAcceptResource(ResourceData)) return;

            //Send the resource flying
            character.CharacterInventory.AddResource(ResourceData, Visual, Visual != null ? true : false);

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