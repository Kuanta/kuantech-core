using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine.Events;

namespace Kuantech.RealTimeStrategy
{
    /// <summary>
    /// A level module to handle all units in the level.
    /// </summary>
    public class UnitsManager : LevelModule
    {
        private Dictionary<int, HashSet<Actor>> _actorsByFaction;
        public HashSet<Actor> SpawnedActors = new HashSet<Actor>();

        //todo(rts): Factions management here. Something like factions lookup table

        public UnityAction OnActorRemoved;
        
        public override void Initialize()
        {
            _actorsByFaction = new Dictionary<int, HashSet<Actor>>();
        }
        
        /// <summary>
        /// Spawns an actor
        /// </summary>
        /// <param name="actorBlueprint"></param>
        /// <returns></returns>
        public Actor SpawnActor(ActorBlueprint actorBlueprint)
        {
            Actor spawned = actorBlueprint.CreateActor();
            SpawnedActors.Add(spawned);
            if (spawned == null) return null;
            return spawned;
        }

        public void RegisterActor(Actor actor)
        {
            if (actor == null) return;
            actor.OnDeathEvent += OnActorDeath;
            AddActor(actor);
        }
        
        /// <summary>
        /// Adds an actor
        /// </summary>
        /// <param name="actor"></param>
        public void AddActor(Actor actor)
        {
            int factionId = actor.FactionId;
            if(_actorsByFaction == null)
                _actorsByFaction = new Dictionary<int, HashSet<Actor>>();
            if (!_actorsByFaction.ContainsKey(factionId))
            {
                _actorsByFaction[factionId] = new HashSet<Actor>();
            }

            _actorsByFaction[factionId].Add(actor);
        }
        
        /// <summary>
        /// Removes an actor
        /// </summary>
        /// <param name="actor"></param>
        public void RemoveActor(Actor actor)
        {
            int factionId = actor.FactionId;
            if (_actorsByFaction == null || !_actorsByFaction.ContainsKey(factionId))
                return;
            if(SpawnedActors != null && SpawnedActors.Contains(actor))
                SpawnedActors.Remove(actor);
            _actorsByFaction[factionId].Remove(actor);
            OnActorRemoved?.Invoke();
        }
        
        /// <summary>
        /// Gets all actors by faction ID.
        /// </summary>
        /// <param name="factionId"></param>
        /// <returns></returns>
        public HashSet<Actor> GetActorsByFaction(int factionId)
        {
            if (_actorsByFaction == null || !_actorsByFaction.ContainsKey(factionId))
                return null;
            return _actorsByFaction[factionId];
        }
        
        /// <summary>
        /// Gets all enemy actors
        /// </summary>
        /// <param name="factionId"></param>
        /// <returns></returns>
        public HashSet<Actor> GetEnemyActors(int factionId)
        {
            //For now, get all actors that are not in the faction
            HashSet<Actor> enemyActors = new HashSet<Actor>();
            foreach (var kvp in _actorsByFaction)
            {
                if (kvp.Key != factionId)
                {
                    enemyActors.UnionWith(kvp.Value);
                }
            }
            return enemyActors;
        }
        
        public override void OnLevelClear()
        {
            base.OnLevelClear();
            ClearSpawnedActors();
            _actorsByFaction.Clear(); //Despawning actors isn't untis managers responsabilitiy
        }
        
        /// <summary>
        /// Clears all spawned actors. Spawned ac
        /// </summary>
        public void ClearSpawnedActors()
        {
            if (_actorsByFaction == null) return;
            foreach (var actor in SpawnedActors)
            {
                if (actor != null)
                {
                    //Play a vfx here?
                    actor.Despawn(0.0f);
                }
            }
            SpawnedActors.Clear();
            _actorsByFaction.Clear(); //Clear this also    
        }
        
        #region Event Handlers
        public void OnActorDeath(Actor actor)
        {
            if (actor == null) return;
            RemoveActor(actor);
        }
        #endregion
     
    }
}