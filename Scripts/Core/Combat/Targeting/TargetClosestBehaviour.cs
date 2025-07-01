using UnityEngine;

namespace Kuantech.Core
{
    [CreateAssetMenu(fileName = "TargetClosestBehaviour", menuName = "Kuantech/Combat/Targeting Behaviour/Target Closest Behaviour")]
    public class TargetClosestBehaviour : TargetPriorityBehaviour
    {
        public override int Compare(Actor a, Actor b, Actor self)
        {
            float distToA = (self.transform.position - a.transform.position).sqrMagnitude;
            float distToB = (self.transform.position - b.transform.position).sqrMagnitude;
            
            if (distToA < distToB)
            {
                return -1; // a is closer
            }
            else if (distToA > distToB)
            {
                return 1; // b is closer
            }

            return 0;
        }
    }
}