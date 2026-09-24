using Kuantech.Core;
using UnityEngine;

namespace Kuantech.CastleDefenders
{
    /// <summary>
    /// A middleware to apply custom difficulty to enemies
    /// </summary>
    public abstract class EnemySpawnMiddleware : ScriptableObject
    {
        public abstract void ApplyDifficultyToEnemy(Actor actor);
    }
}