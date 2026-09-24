using Kuantech.Core;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// The NetworkedWaveHandler counterpart to SpawnSelectorMiddleware -- kept as its own type rather than
    /// changing that one's signature, so any SpawnSelectorMiddleware asset already authored against
    /// HordeWaveHandler keeps compiling untouched.
    /// </summary>
    public abstract class NetworkedSpawnSelectorMiddleware : ScriptableObject
    {
        /// <summary>A middleware to select what enemy to spawn.</summary>
        public abstract ActorBlueprint GetActorToSpawn(NetworkedWaveHandler waveHandler);
    }
}
