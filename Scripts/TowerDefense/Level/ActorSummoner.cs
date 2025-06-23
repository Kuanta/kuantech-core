using Kuantech.Core;
using Kuantech.Midcore;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class ActorSummoner : LevelElement
    {
        public int ActorFactionId = 0;
        public TowerDefensePath PathToSpawn;
        public Transform SpawnPoint; // The point where the actor will be spawned
        
        public float Cooldown = 1f;
        public bool ToggledOn = true; // Whether the summoner is active or not
        //Runtime
        private float _lastSpawnedTime;

        public virtual TowerDefensePath GetPathToSpawn()
        {
            return PathToSpawn;
        }

        /// <summary>
        /// Spawns the actor
        /// </summary>
        public virtual Actor SpawnActor(ActorBlueprint actorTemplate)
        {
            if (actorTemplate == null) return null;
            Actor createdActor = actorTemplate.CreateActor();
            createdActor.Spawn();
            createdActor.FactionId = ActorFactionId;
            createdActor.transform.position = SpawnPoint.position;
            TowerDefenseActorModule tdm = createdActor.GetModule<TowerDefenseActorModule>();
            if (tdm == null)
            {
                Debug.LogWarning("Tower defense actor module is null for actor spawned from spawner");
                createdActor.Despawn();
                return null;
            }

            TowerDefensePath path = GetPathToSpawn();
            if (path == null)
            {
                Debug.LogWarning("Couldn't get tower defense path");
                createdActor.Despawn();
                return null;
            }
            path.SetActorOnPath(createdActor);
            _lastSpawnedTime = Time.time;
            
            //Add to spawned actors
            ParentLevel.AddSpawnable(createdActor);
            return createdActor;
        }

    }
}