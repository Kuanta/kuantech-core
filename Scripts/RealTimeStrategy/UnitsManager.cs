using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.RealTimeStrategy
{
    [Serializable]
    public struct MaxUnitPerFactionEntry
    {
        public int FactionId;
        public int MaxUnit;
    }
    
    /// <summary>
    /// A level module to handle all units in the level.
    /// </summary>
    public class UnitsManager : LevelModule
    {
        [Header("Unit Counts")] 
        public List<MaxUnitPerFactionEntry> MaxUnitsPerFaction;
        private Dictionary<int, HashSet<Actor>> _actorsByFaction;
        private Dictionary<int, int> _maxUnitsPerFaction;
        private Dictionary<int, float> _maxUnitsFactorPerFaction;
        public HashSet<Actor> SpawnedActors = new HashSet<Actor>();
        public HashSet<Actor> DeadActors = new HashSet<Actor>();
        

        //todo(rts): Factions management here. Something like factions lookup table

        public UnityAction<Actor> OnActorRemoved;
        
        public override void Initialize()
        {
            _actorsByFaction = new Dictionary<int, HashSet<Actor>>();
            _maxUnitsFactorPerFaction = new Dictionary<int, float>();
            if (!MaxUnitsPerFaction.IsNullOrEmpty())
            {
                foreach (var entry in MaxUnitsPerFaction)
                {
                    SetMaxUnitPerFaction(entry.FactionId, entry.MaxUnit);
                }
            }
        }
        
        /// <summary>
        /// Spawns an actor
        /// </summary>
        /// <param name="actorBlueprint"></param>
        /// <returns></returns>
        public Actor SpawnActor(ActorBlueprint actorBlueprint)
        {
            if (!CanSpawnActor(actorBlueprint)) return null;
            Actor spawned = actorBlueprint.CreateActor();
            if (spawned == null) return null;
            RegisterActor(spawned, true);
            return spawned;
        }

        public int GetSpawnedActorCount()
        {
            return SpawnedActors.Count;
        }

        /// <summary>
        /// Registers an actor.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="spawned">For actors spawned during the game. Actors like towers, hearts should be marked as false</param>
        public void RegisterActor(Actor actor, bool spawned)
        {
            if (actor == null) return;
            if (AddActor(actor))
            {
                actor.OnDeathEvent -= OnActorDeath;
                actor.OnDeathEvent += OnActorDeath;
                actor.OnDespawnedEvent -= OnActorDespawn;
                actor.OnDespawnedEvent += OnActorDespawn;
                if (spawned)
                {
                    SpawnedActors.Add(actor);
                }
            }
        }
        
        /// <summary>
        /// Adds an actor
        /// </summary>
        /// <param name="actor"></param>
        private bool AddActor(Actor actor)
        {
            int factionId = actor.GetFactionId();
            if(_actorsByFaction == null)
                _actorsByFaction = new Dictionary<int, HashSet<Actor>>();
            if (!_actorsByFaction.ContainsKey(factionId))
            {
                _actorsByFaction[factionId] = new HashSet<Actor>();
            }

            if (_actorsByFaction[factionId].Contains(actor))
            {
                return false;
            }
            _actorsByFaction[factionId].Add(actor);
            return true;
        }

       
        
        /// <summary>
        /// Removes an actor
        /// </summary>
        /// <param name="actor"></param>
        public void RemoveActor(Actor actor)
        {
            int factionId = actor.GetFactionId();
            if (_actorsByFaction == null || !_actorsByFaction.ContainsKey(factionId))
                return;
            if(SpawnedActors != null && SpawnedActors.Contains(actor))
                SpawnedActors.Remove(actor);
            UnregisterActor(actor);
            OnActorRemoved?.Invoke(actor);
        }
        
        
        private void UnregisterActor(Actor actor)
        {
            int factionId = actor.GetFactionId();
            if (_actorsByFaction.ContainsKey(factionId) && _actorsByFaction[factionId].Contains(actor))
            {
                _actorsByFaction[factionId].Remove(actor);
            }
        }

        public HashSet<Actor> GetAllActors()
        {
            return SpawnedActors;
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
        /// Returns actors by faction Ids
        /// </summary>
        /// <param name="factionIds"></param>
        /// <returns></returns>
        public HashSet<Actor> GetActorsByFactions(List<int> factionIds)
        {
            HashSet<Actor> actors = new HashSet<Actor>();
            foreach (var factionId in factionIds)
            {
                if (_actorsByFaction == null || !_actorsByFaction.ContainsKey(factionId))
                    continue;
                actors.UnionWith(_actorsByFaction[factionId]);
            }

            return actors;
        }
        
        // /// <summary>
        // /// Gets all enemy actors
        // /// </summary>
        // /// <param name="factionId"></param>
        // /// <returns></returns>
        // public HashSet<Actor> GetEnemyActors(HashSet<int> enemyFactionIds)
        // {
        //     //For now, get all actors that are not in the faction
        //     HashSet<Actor> enemyActors = new HashSet<Actor>();
        //     foreach (var kvp in _actorsByFaction)
        //     {
        //         if (kvp.Key != factionId)
        //         {
        //             enemyActors.UnionWith(kvp.Value);
        //         }
        //     }
        //     return enemyActors;
        // }
        
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
                    actor.Despawn(0.0f);
                    UnregisterActor(actor);
                }
            }
            SpawnedActors.Clear();
            
            //To clear all dead actors that are not despawned yet
            foreach (var actor in DeadActors)
            {
                if (actor != null)
                {
                    actor.Despawn(0.0f);
                }
            }
            DeadActors.Clear();
        }

        public void ClearSpawnedActorsByFaction(int faction)
        {
            HashSet<Actor> originalActorsSet = GetActorsByFaction(faction);
            if (originalActorsSet.IsNullOrEmpty()) return;
            HashSet<Actor> actors = new HashSet<Actor>(originalActorsSet);
            if(actors.IsNullOrEmpty()) return; 
            foreach (var actor in actors)
            {
                actor.Despawn();
                UnregisterActor(actor);
            }
        }
        
        #region Pop limit
        
        public bool CanSpawnActor(ActorBlueprint actorBlueprint)
        {
            if (actorBlueprint == null) return false;
            int maxUnitCount = GetMaxActorCountByFaction(actorBlueprint.FactionId);
            if (GetSpawnedActorCountByFaction(actorBlueprint.FactionId) >= maxUnitCount && maxUnitCount >= 0) return false;
            int actorPerFaction = GetSpawnedActorCountByFaction(actorBlueprint.FactionId);
            int maxActorPerFaction = GetMaxActorCountByFaction(actorBlueprint.FactionId);
            if (maxActorPerFaction >= 0 && actorPerFaction >= maxActorPerFaction)
            {
                return false;
            }
            return true;
        }

        public int GetMaxActorCountByFaction(int faction)
        {
            if (!_maxUnitsPerFaction.IsNullOrEmpty() && _maxUnitsPerFaction.ContainsKey(faction))
            {
                float factor = GetMaxUnitFactorPerFaction(faction);
                return Mathf.FloorToInt(_maxUnitsPerFaction[faction] * factor);
            }

            return -1; //Limitless
        }
        
        public int GetSpawnedActorCountByFaction(int faction)
        {
            if (_actorsByFaction == null || !_actorsByFaction.ContainsKey(faction))
                return 0;
            return _actorsByFaction[faction].Count;    
        }
        
        public void SetMaxUnitPerFaction(int faction, int actorCount)
        {
            if (_maxUnitsPerFaction == null) _maxUnitsPerFaction = new Dictionary<int, int>();
            _maxUnitsPerFaction[faction] = actorCount;
        }

        public void SetMaxUnitFactorPerFaction(int faction, float factor)
        {
            if (_maxUnitsFactorPerFaction == null) _maxUnitsFactorPerFaction = new Dictionary<int, float>();
            _maxUnitsFactorPerFaction[faction] = factor;
        }

        public float GetMaxUnitFactorPerFaction(int faction)
        {
            if (_maxUnitsFactorPerFaction.ContainsKey(faction))
            {
                return _maxUnitsFactorPerFaction[faction];
            }

            return 1;
        }
        #endregion

        #region Event Handlers
        
        public override void OnLevelClear()
        {
            base.OnLevelClear();
            ClearSpawnedActors();
        }

        public override void OnReset()
        {
            base.OnReset();
            ClearSpawnedActors();
        }
        public void OnActorDeath(Actor.KillFeedData killFeedData)
        {
            if (killFeedData.DeadActor == null) return;
            RemoveActor(killFeedData.DeadActor);
            DeadActors.Add(killFeedData.DeadActor);
        }

        public void OnActorDespawn(Actor actor)
        {
            if (actor == null) return;
            if (DeadActors != null && DeadActors.Contains(actor))
            {
                DeadActors.Remove(actor);
            }
        }
        public override void OnLevelStateChange(LevelStateChangeData levelStateChangeData)
        {
            base.OnLevelStateChange(levelStateChangeData);
            foreach (var actors in _actorsByFaction)
            {
                foreach (var actor in actors.Value)
                {
                    actor.ChangeActorState(ActorState.Inactive);
                }
            }
        }
        #endregion
     
    }
}