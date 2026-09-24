using Kuantech.Core;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    public abstract class SpawnSelectorMiddleware : ScriptableObject
    {
        /// <summary>
        /// A middleware to select what enemy to spawn
        /// </summary>
        /// <param name="waveHandler"></param>
        /// <returns></returns>
        public abstract ActorBlueprint GetActorToSpawn(HordeWaveHandler waveHandler);

    }
}