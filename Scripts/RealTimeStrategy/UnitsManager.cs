using System.Collections.Generic;
using Kuantech.Core;

namespace Kuantech.RealTimeStrategy
{
    /// <summary>
    /// A level module to handle all units in the level.
    /// </summary>
    public class UnitsManager : LevelModule
    {
        private Dictionary<int, HashSet<Actor>> _actorsByFaction;
        
        //todo(rts): Factions management here. Something like factions lookup table
        
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
            if (spawned == null) return null;
            RegisterActor(spawned);
            return spawned;
        }

        public void RegisterActor(Actor actor)
        {
            if (actor == null) return;
            actor.OnDespawnedEvent += OnActorDespawned;
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

            _actorsByFaction[factionId].Remove(actor);
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
            _actorsByFaction.Clear(); //Despawning actors isn't untis managers responsabilitiy
        }

        #region Event Handlers
        public void OnActorDespawned(Actor actor)
        {
            if (actor == null) return;
            RemoveActor(actor);
        }
        #endregion
     
    }
}