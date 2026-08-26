using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// Picks a random enemy within Range instead of the nearest one -- any in-range candidate always
    /// outranks any out-of-range one (same two-tier trick as TargetOutsideRangeBehaviour), and among
    /// in-range candidates a fresh random roll on every query picks the winner. Not provably uniform (two
    /// candidates queried multiple times across a scan's pairwise comparisons can each win or lose more than
    /// once), but that precision doesn't matter for a targeting feel -- it only needs to not always be the
    /// same enemy, which this delivers.
    /// </summary>
    [CreateAssetMenu(fileName = "TargetRandomInRangeBehaviour", menuName = "Kuantech/Combat/Targeting Behaviour/Target Random In Range Behaviour")]
    public class TargetRandomInRangeBehaviour : TargetPriorityBehaviour
    {
        [Tooltip("Only enemies within this distance are eligible to be picked at all.")]
        public float Range = 8f;

        private const float TierBonus = 1000f;

        public override float GetTargetPriority(Actor a, Actor self)
        {
            float distSq = (a.GetActorLocation() - self.GetActorLocation()).sqrMagnitude;
            bool inRange = distSq <= Range * Range;
            return (inRange ? TierBonus : 0f) + Random.value;
        }
    }
}
