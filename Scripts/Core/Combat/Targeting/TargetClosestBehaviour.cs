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
        
        /// <summary>
        /// Gets the target priority based on distance. Closer actors have higher priority.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="self"></param>
        /// <param name="maxPriority"></param>
        /// <param name="invert">If true, further ones will be prior</param>
        /// <returns></returns>
        public static float GetTargetPriorityByDistance(Actor a, Actor self, float maxPriority = 1000f, bool invert = false)
        {
            float distToSelf = (self.transform.position - a.transform.position).sqrMagnitude;
            SurroundManager tm = a.GetModule<SurroundManager>();
            if(tm != null && tm.SlotAllocator != null)
            {
                TargetSlot bestSlot = tm.SlotAllocator.GetBestSlot(a, TargetDetectionSlotType.ByDistance);
                if (bestSlot != null)
                {
                    distToSelf = (self.transform.position - bestSlot.GetWorldPoint().GetTargetPosition()).sqrMagnitude;
                    distToSelf = Mathf.Max(distToSelf, 0.0001f); // Prevent division by zero
                }
            }

            if (!invert)
            {
                return Mathf.Min(1 / distToSelf, maxPriority); // Closer actors have higher priority
            }
            return Mathf.Min(distToSelf, maxPriority); 
        }
    }
}