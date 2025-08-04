using Kuantech.Core;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class TowerDefenseActorSummoner : ActorSummoner
    {
        public TowerDefensePath PathToSpawn;

        public virtual TowerDefensePath GetPathToSpawn()
        {
            return PathToSpawn;
        }

        /// <summary>
        /// Spawns the actor
        /// </summary>
        public override Actor SpawnActor(ActorBlueprint actorTemplate, int order = 0)
        {
            if (actorTemplate == null) return null;
            Actor createdActor = base.SpawnActor(actorTemplate);
            createdActor.Spawn();
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
            
            //Add to spawned actors
            ParentLevel.AddSpawnable(createdActor);
            return createdActor;
        }

    }
}