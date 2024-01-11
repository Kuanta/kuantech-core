using UnityEngine;

namespace Kuantech.Core.Combat
{
    public abstract class SkillAction : ScriptableObject
    {
        /// <summary>
        /// The main act method. It does the bidding of its master
        /// </summary>
        public abstract void Act(CombatModule cm);
    }
}