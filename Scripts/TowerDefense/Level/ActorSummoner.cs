using Kuantech.Core;
using Kuantech.Midcore;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class ActorSummoner : LevelElement
    {
        public ActorTemplateAsset ActorTemplateAsset; //TEMPORARY
        public TowerDefensePath PathToSpawn;
        public Transform SpawnPoint; // The point where the actor will be spawned
        
        public float Cooldown = 1f;
        public bool ToggledOn = true; // Whether the summoner is active or not
        //Runtime
        private float _lastSpawnedTime;
        
        public virtual ActorTemplateAsset GetNextActorTemplate()
        {
            // This method should be overridden in derived classes to provide the next actor template.
            return ActorTemplateAsset;
        }

        public virtual TowerDefensePath GetPathToSpawn()
        {
            return PathToSpawn;
        }
        
        
        private void Update()
        {
            SpawnActor();
        }
        
        /// <summary>
        /// Spawns the actor
        /// </summary>
        public virtual void SpawnActor()
        {
            if (!CanSpawnActor()) return;
            
            ActorTemplateAsset actorTemplate = GetNextActorTemplate();
            if (actorTemplate == null) return;
            Actor createdActor = actorTemplate.CreateActor();
            createdActor.transform.position = SpawnPoint.position;
            TowerDefenseActorModule tdm = createdActor.GetModule<TowerDefenseActorModule>();
            if (tdm == null)
            {
                Debug.LogWarning("Tower defense actor module is null for actor spawned from spawner");
                createdActor.Despawn();
                return;
            }

            TowerDefensePath path = GetPathToSpawn();
            if (path == null)
            {
                Debug.LogWarning("Couldn't get tower defense path");
                createdActor.Despawn();
                return;
            }
            path.SetActorOnPath(createdActor);
            _lastSpawnedTime = Time.time;
        }
        
        /// <summary>
        /// Checks whether the actor can be spawned based on cooldown and other conditions.
        /// </summary>
        /// <returns></returns>
        public virtual bool CanSpawnActor()
        {
            // Check if enough time has passed since the last spawn    
            if (!ToggledOn || ParentLevel == null || ParentLevel.CurrentState != LevelState.Playing) return false;
            if(Time.time - _lastSpawnedTime < Cooldown)
            {
                return false;
            }

            return true;
        }
    }
}