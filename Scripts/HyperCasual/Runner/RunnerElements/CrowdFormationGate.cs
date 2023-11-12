using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class CrowdFormationGate : Pickupable
    {
        [SerializeField] private CrowdFormationData CrowdFormationData;

        protected override void OnPickup(Collider other)
        {
            if (other.gameObject.TryGetComponent(out Crowd crowd))
            {
                crowd.SetCrowdFormation(CrowdFormationData);
                base.OnPickup(other);
            }
        }
    }

}