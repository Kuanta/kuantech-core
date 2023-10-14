using Kuantech.Core.HyperCasual.Runner;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class PickupableGroup : MonoBehaviour, IChunkElement
    {
        private Pickupable[] _childPickupables;
        public void OnChunkGenerated(RunnerChunk chunk)
        {
            _childPickupables = GetComponentsInChildren<Pickupable>();
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
            if(_childPickupables == null || _childPickupables.Length == 0) return;
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
    }
}