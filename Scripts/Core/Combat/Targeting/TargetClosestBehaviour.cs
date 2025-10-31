using UnityEngine;

namespace Kuantech.Core
{
    [CreateAssetMenu(fileName = "TargetClosestBehaviour", menuName = "Kuantech/Combat/Targeting Behaviour/Target Closest Behaviour")]
    public class TargetClosestBehaviour : TargetPriorityBehaviour
    {
        public float MaxPriority = 1000f;
        public override float GetTargetPriority(Actor a, Actor self)
        {
            return GetTargetPriorityByDistance(a, self, MaxPriority);
        }

        public static float GetTargetPriorityByDistance(Actor a, Actor self, float maxPriority = 1000f)
        {
            float distToSelf = (self.transform.position - a.transform.position).sqrMagnitude;
            SurroundManager tm = a.GetModule<SurroundManager>();
            if(tm != null && tm.SlotAllocator != null)
            {
                TargetSlot bestSlot = tm.SlotAllocator.GetBestSlot(a);
                if (bestSlot != null)
                {
                    distToSelf = (self.transform.position - bestSlot.GetWorldPoint().GetTargetPosition()).sqrMagnitude;
                    distToSelf = Mathf.Max(distToSelf, 0.0001f); // Prevent division by zero
                }
            }
            return Mathf.Min(1 / distToSelf, maxPriority); // Closer actors have higher priority
        }
    }
}