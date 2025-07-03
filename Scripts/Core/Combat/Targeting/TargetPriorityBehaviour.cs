using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// Behaviour that defines how to compare actors for targeting purposes.
    /// </summary>
    public abstract class TargetPriorityBehaviour : ScriptableObject
    {
        /// <summary>
        /// Compares two actors. Return:
        /// -1 if a < b (a is prior)
        /// 0  if a == b
        /// 1  if a > b (b is prior)
        /// </summary>
        public int Compare(Actor a, Actor b, Actor self)
        {
            float scoreA = GetTargetPriority(a, self);
            float scoreB = GetTargetPriority(b, self);
            if (scoreA > scoreB) return -1;
            if (scoreA < scoreB) return 1;
            return 0;
        }

        public abstract float GetTargetPriority(Actor a, Actor self);

    }
}