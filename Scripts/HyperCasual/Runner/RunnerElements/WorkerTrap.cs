using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class WorkerTrap : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out CrowdElement worker))
            {
                worker.Despawn();
            }
        }
    }
}