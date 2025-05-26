using Kuantech.Core.Combat;

namespace Kuantech.Core
{
    public class TargetClosestBehaviour : TargetDetectionModule
    {
        public int Compare(Actor a, Actor b, Actor self)
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