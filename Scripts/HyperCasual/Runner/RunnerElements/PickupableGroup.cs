using Kuantech.Core.HyperCasual.Runner;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class PickupableGroup : MonoBehaviour, IChunkElement
    {
        [Tooltip("If the group uses a common collider and the closest pickup is selected to impact point")]
        [SerializeField] private bool ByDistance;
        private Pickupable[] _childPickupables;

        public void OnChunkGenerated(RunnerChunk chunk)
        {
            _childPickupables = GetComponentsInChildren<Pickupable>();
            if(!ByDistance)
            {
                foreach (var pickup in _childPickupables)
                {
                    pickup.PickedEvent += OnPickupablesPicked;
                }
            }
        }

        public void OnChunkRestart()
        {
        }

        public void OnClearChunk()
        {
        }

        public void OnPlayerEnteredChunk()
        {
        }

        public void OnPlayerExitedChunk()
        {
        }

        private void OnTriggerEnter(Collider other)
        {
            if(!ByDistance || _childPickupables == null || _childPickupables.Length == 0) return;
            //Check the closest pickupable
            
            float minDistance = float.MaxValue;
            int indexOfMin = 0;
            for(int i=0;i<_childPickupables.Length;++i)
            {
                float distance = Vector3.Distance(other.transform.position, _childPickupables[i].transform.position);
                if(distance <  minDistance)
                {
                    minDistance = distance;
                    indexOfMin = i;
                }
            }

            _childPickupables[indexOfMin].Pickup(other);
            foreach(var pickup in _childPickupables)
            {
                pickup.Disable();
            }
            
        }

        /// <summary>
        /// A pickup has been picked, disable the others
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="picked"></param>
        private void OnPickupablesPicked(object sender, Pickupable picked)
        {
            foreach (var pickup in _childPickupables)
            {
                pickup.Disable();
            }
        }
    }
}