using UnityEngine;

namespace Kuantech.Core
{
    public abstract class TargetingBehaviour : ScriptableObject
    {
        /// <summary>
        /// Compares two actors. Return:
        /// -1 if a < b (a is prior)
        /// 0  if a == b
        /// 1  if a > b (b is prior)
        /// </summary>
        public abstract int Compare(Actor a, Actor b, Actor self);
    }
}