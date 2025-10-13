using UnityEngine;

namespace Kuantech.Core
{
    [CreateAssetMenu(fileName = "TargetClosestBehaviour", menuName = "Kuantech/Combat/Targeting Behaviour/Target Closest Behaviour")]
    public class TargetClosestBehaviour : TargetPriorityBehaviour
    {
        public override float GetTargetPriority(Actor a, Actor self)
        {
            float distToSelf = (self.transform.position - a.transform.position).sqrMagnitude;
            TargetManager tm = a.GetModule<TargetManager>();
            if(tm != null && tm.SlotAllocator != null)
            {
                TargetSlot bestSlot = tm.SlotAllocator.GetBestSlot(a);
                if (bestSlot != null)
                {
                    distToSelf = (self.transform.position - bestSlot.GetWorldPoint().GetTargetPosition()).sqrMagnitude;
                }
            }
            return 1 / distToSelf; // Closer actors have higher priority
        }
    }
}