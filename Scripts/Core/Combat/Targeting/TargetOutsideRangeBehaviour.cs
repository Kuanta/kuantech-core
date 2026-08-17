using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// Prefers the closest enemy that is at least <see cref="MinDistance"/> away, so a blast/AoE weapon
    /// doesn't waste itself on whatever happens to be standing in melee range — falls back to the plain
    /// closest enemy only when nothing qualifies (everything is crowded in close).
    ///
    /// Scored as two tiers rather than one continuous distance score: any qualifying (far enough) enemy
    /// always outranks any non-qualifying (too close) one, and within each tier the nearer one wins — a
    /// pure distance score would instead prefer a far-away qualifying enemy over a nearby one, throwing the
    /// grenade needlessly far.
    /// </summary>
    [CreateAssetMenu(fileName = "TargetOutsideRangeBehaviour", menuName = "Kuantech/Combat/Targeting Behaviour/Target Outside Range Behaviour")]
    public class TargetOutsideRangeBehaviour : TargetPriorityBehaviour
    {
        [Tooltip("Enemies at least this far away are preferred over anything closer.")]
        public float MinDistance = 3f;

        // Closeness is capped below TierBonus so it can never let a non-qualifying (too-close) enemy
        // outscore a qualifying one, no matter how close it is.
        private const float TierBonus = 1000f;
        private const float MaxCloseness = 999f;

        public override float GetTargetPriority(Actor a, Actor self)
        {
            float distSq = (a.GetActorLocation() - self.GetActorLocation()).sqrMagnitude;
            float minDistSq = MinDistance * MinDistance;

            float closeness = Mathf.Min(1f / Mathf.Max(distSq, 0.0001f), MaxCloseness);
            float tierBonus = distSq >= minDistSq ? TierBonus : 0f;
            return tierBonus + closeness;
        }
    }
}
