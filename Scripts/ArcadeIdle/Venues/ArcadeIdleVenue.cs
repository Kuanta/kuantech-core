using System;
using System.Collections.Generic;
using Kuantech.AI.Utils;
using Kuantech.Core;
using Kuantech.HyperCasual;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class ArcadeIdleVenue : MonoBehaviour
    {
        public string VenueId;
        [Header("Npc")]
        public List<VenueNpcSpawner> NpcSpawners;
        private HashSet<ArcadeIdleNpc> _spawnedNpcs = new HashSet<ArcadeIdleNpc>();
        public UpgradeData MaxNpcUpgrade;
        public LeveledValueInt MaxNpc;
        [SerializeField] private TimedEventInvoker _spawnEvent;
        [NonSerialized] public VenueState CurrentState;

        [Header("Zones")]
        public List<VenueZone> Zones;

        [Header("Warehouse")]
        [SerializeField] private ResourceInventory PackagesInventory;

        [Header("Exits")]
        public List<WorldZone> Exits = new List<WorldZone>();

        [Header("Workers")]
        public WorldZone WorkerSpawnZone;

        private HashSet<ArcadeIdleNpc> _activeWorkers = new HashSet<ArcadeIdleNpc>();
        private Dictionary<ResourceData, HashSet<ResourceDispenser>> _resourceToDispensers;
        private Dictionary<ResourceData, HashSet<ResourceSinker>> _resourceToSinkers;
        public void SetDefaultStateValues()
        {
            CurrentState = new VenueState();
            CurrentState.VenueActorStates = new Dictionary<string, ActorState>();
            CurrentState.WorkerStates = new List<CharacterState>();
            CurrentState.ZoneStates = new Dictionary<string, bool>();
        }

        public virtual void Initialize()
        {
            LoadState();

            //Initialize Zones
            foreach(var zone in Zones)
            {
                bool unlocked = IsZoneUnlocked(zone.ZoneId) || zone.UnlockedByDefault;
                zone.Unlocked = unlocked;
                zone.Initialize(this);
                zone.Toggle(unlocked);
            }

            SetDispensersByResourceMap();
            SetSinkersByResourceMap();

            //Load player state

            //Spawn workers after getting the zones. Since zones need venue state, we are handling this here
            if (CurrentState.WorkerStates == null) return;
            foreach (var workerState in CurrentState.WorkerStates)
            {
                Vector3 position = new Vector3(workerState.PosX, 0, workerState.PosZ);
                Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, 0));
                HireWorker(ArcadeIdleManager.GetRandomNpcByTag(workerState.WorkerTag),
                workerState.ActorState, position, rotation);
            }

            _spawnEvent = new TimedEventInvoker();
            _spawnEvent.EventToFire = SpawnNpc;
        }

        private void Update()
        {
            if (_spawnedNpcs.Count >= GetMaxNpcCount() || !CanSpawnNpc()) return;
            _spawnEvent.Update();
        }

        #region Npc
        public int GetMaxNpcCount()
        {
            int level = MaxNpcUpgrade != null ? UpgradeManager.GetCurrentUpgradeLevel(MaxNpcUpgrade) : 0;
            return MaxNpc.GetValue(level);
        }
        protected virtual bool CanSpawnNpc()
        {
            return true;
        }
        private void SpawnNpc()
        {
            if (!CanSpawnNpc() || _spawnedNpcs != null && _spawnedNpcs.Count >= GetMaxNpcCount()) return;
            VenueNpcSpawner spawner = NpcSpawners.GetRandomElement();
            ArcadeIdleNpc npc = spawner.SpawnNpc(this);
            npc.CurrentVenue = this;
            _spawnedNpcs.Add(npc);
            npc.DespawnEvent = OnNpcDespawn;
        }

        private void OnNpcDespawn(object sender, EventArgs args)
        {
            ArcadeIdleNpc npc = sender as ArcadeIdleNpc;
            _spawnedNpcs.Remove(npc);
        }
        
        /// <summary>
        /// Tries to assign an npc to a random interactable of given type.
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool AssignToRandomVenueInteractable(ArcadeIdleNpc npc, List<int> interactableTags)
        {
            List<VenueActor> venueActors = GetAvailableVenueActors(interactableTags);
            return AssignToRandomInteractable(npc, interactableTags, venueActors);
        }

        /// <summary>
        /// Assigns an npc to a random interactable in the given interactables list
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="interactableTags">Filter interactables by tags</param>
        /// <param name="venueActors"></param>
        /// <returns></returns>
        public static bool AssignToRandomInteractable(ArcadeIdleNpc npc, List<int> interactableTags, List<VenueActor> venueActors)
        {
            List<VenueInteractable> _availableInteractables = new List<VenueInteractable>();
            List<VenueInteractable> _queueableInteractables = new List<VenueInteractable>();
            foreach (var venueActor in venueActors)
            {
                VenueInteractable interactable = venueActor.GetComponent<VenueInteractable>();
                if (interactable == null || venueActor.IsLocked()) continue;

                if (interactable.HasAvailableSlots(npc))
                {
                    _availableInteractables.Add(interactable);
                }
                else if (interactable.HasAvailableQueue(npc))
                {
                    _availableInteractables.Add(interactable);
                }
            }

            _availableInteractables.Shuffle();
            _queueableInteractables.Shuffle();
            for (int i = 0; i < _availableInteractables.Count; ++i)
            {
                if (_availableInteractables[i].AddInteractor(npc)) return true;
            }
            for (int i = 0; i < _queueableInteractables.Count; ++i)
            {
                if (_queueableInteractables[i].AddInteractor(npc)) return true;
            }
            return false;
        }
        public WorldZone GetRandomExit()
        {
            if (Exits == null || Exits.Count == 0) return null;
            return Exits.GetRandomElement();
        }

        public List<VenueZone> GetZonesByTag(List<int> tags)
        {
            List<VenueZone> zones = new List<VenueZone>();
            foreach(var zone in Zones)
            {
                if(tags.Contains(zone.ZoneTag))
                {
                    zones.Add(zone);
                }
            }
            return zones;
        }

        /// <summary>
        /// Returns the nearest available zone to the character
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        public VenueZone GetNearestZone(ArcadeIdleCharacter character, List<int> zoneTags = null)
        {
            List<VenueZone> sortedZones = GetZonesByTag(zoneTags);
            sortedZones.Sort((a,b)=>{
                float distA = Vector3.SqrMagnitude(a.transform.position - character.transform.position);
                float distB = Vector3.SqrMagnitude(b.transform.position - character.transform.position);
                return distA.CompareTo(distB);
            });
            List<VenueZone> slottableZones = new List<VenueZone>();
            List<VenueZone> queuebleZones = new List<VenueZone>();

            foreach (var zone in sortedZones)
            {
                if(zone.CanAcceptCharacter(character))
                {
                    slottableZones.Add(zone);
                }else if(zone.CanQueueCharacter(character))
                {
                    queuebleZones.Add(zone);
                }
            }

            if(slottableZones.Count > 0) return slottableZones[0];
            if(queuebleZones.Count > 0) return queuebleZones[0];
            return null;
        }
        #endregion

        #region Unlockables
        public void UnlockUnlockable(IUnlockable unlockable)
        {
            //Activate gameobject
            (unlockable as MonoBehaviour).gameObject.SetActive(true);
            unlockable.Unlock();
            unlockable.Toggle(true);
        }

        public bool IsZoneUnlocked(string zoneId)
        {
            if(CurrentState.ZoneStates == null || !CurrentState.ZoneStates.ContainsKey(zoneId)) return false;
            return CurrentState.ZoneStates[zoneId];
        }
        #endregion

        #region State
        public void LoadState()
        {
            ArcadeIdleState arcadeIdleState = GameStateManager.GetModuleStatic<ArcadeIdleState>();
            if (arcadeIdleState == null) {
                SetDefaultStateValues();
                return;
            }
            CurrentState = arcadeIdleState.GetVenueState(VenueId);
            if(CurrentState == null)
            {
                SetDefaultStateValues();
                return;
            }

        }

        public void DirtyActorState(VenueActor actor)
        {
            if(actor.Id.IsNullOrEmpty()) return;
            ArcadeIdleState arcadeIdleState = GameStateManager.GetModuleStatic<ArcadeIdleState>();
            if(arcadeIdleState == null) return;
            CurrentState.Dirtied = true;
            CurrentState.VenueActorStates[actor.Id] = actor.GetActorState();
            arcadeIdleState.UpdateVenueState(this);
        }

        public void DirtyZonestate(VenueZone zone)
        {
            CurrentState.ZoneStates = GetZoneStates();
            CurrentState.Dirtied = true;
            ArcadeIdleState arcadeIdleState = GameStateManager.GetModuleStatic<ArcadeIdleState>();
            arcadeIdleState.UpdateVenueState(this);
        }

        /// <summary>
        /// Gets all states  of workerStates
        /// </summary>
        /// <returns></returns>
        public List<CharacterState> GetWorkerStates()
        {
            List<CharacterState> workerStates = new List<CharacterState>();
            foreach(var worker in _activeWorkers)
            {
                workerStates.Add(worker.GetCharacterState());
            }
            return workerStates;
        }

        public Dictionary<string, bool> GetZoneStates()
        {
            Dictionary<string, bool> zoneStates = new Dictionary<string, bool>();
            foreach(var zone in Zones)
            {
                zoneStates[zone.ZoneId] = zone.Unlocked;
            }
            return zoneStates;
        }
        #endregion
    
        #region Actor Getter
        /// <summary>
        /// Returns a list of available venues
        /// </summary>
        /// <returns></returns>
        public List<VenueActor> GetAvailableVenueActors(List<int> tags = null)
        {
            List<VenueActor> actors = new List<VenueActor>();
            foreach(var zone in Zones)
            {
                if(!zone.Unlocked) continue;
                foreach(var actor in zone.VenueActors)
                {
                    if(actor.IsLocked()) continue;
                    if(tags != null && !tags.Contains(actor.VenueTag)) continue;
                    actors.Add(actor);
                }
            }
            return actors;
        }


        /// <summary>
        /// Gets a list of resource generators that produces the given resource
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public List<ResourceGenerator> GetResourceGeneratorsByProduct(ResourceData data)
        {
            List<ResourceGenerator> generators = new List<ResourceGenerator>();
            List<VenueActor> venueActors = GetAvailableVenueActors();
            foreach(var actor in venueActors)
            {
                ResourceGenerator generator = actor as ResourceGenerator;
                if (generator == null || generator.IsLocked() || generator.GetCurrentRecipe().ResourceToGenerate != data) continue;
                generators.Add(generator);
            }
            return generators;
        }

        /// <summary>
        /// Gets a list of resource generators that requires the given resource
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public List<ResourceGenerator> GetResourceGeneratorsByInput(ResourceData data)
        {
            List<ResourceGenerator> generators = new List<ResourceGenerator>();
            List<VenueActor> venueActors = GetAvailableVenueActors();
            foreach (var actor in venueActors)
            {
                ResourceGenerator generator = actor as ResourceGenerator;
                if (generator == null || !generator.RequiresResource(data)) continue;
                generators.Add(generator);
            }
            return generators;
        }

        /// <summary>
        /// Returns available generators by their tags
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public List<ResourceGenerator> GetResourceGeneratorsByTag(List<int> tags)
        {
            List<ResourceGenerator> generators = new List<ResourceGenerator>();
            List<VenueActor> venueActors = GetAvailableVenueActors(tags);
            foreach(var actor in venueActors)
            {
                ResourceGenerator generator = actor as ResourceGenerator;
                if(generator == null) continue;
                generators.Add(generator);
            }
            return generators;
        }

        public List<VenueActor> GetVenueActorsByTag(int tag)
        {
            List<VenueActor> venueActorsWithTag = new List<VenueActor>();
            List<VenueActor> venueActors = GetAvailableVenueActors();
            foreach (var actor in venueActors)
            {
                if(actor.VenueTag != tag) continue;
                venueActorsWithTag.Add(actor);
            }
            return venueActorsWithTag;
        }
 
        public List<T> GetVenueActors<T>(List<int> tags, Func<T, bool> filter=null, Func<T,T, int> comparer=null) where T : VenueActor
        {
            List<T> actors = new List<T>();
            foreach (var zone in Zones)
            {
                if (!zone.Unlocked) continue;
                foreach (var actor in zone.VenueActors)
                {
                    if (actor.IsLocked()) continue;
                    if (tags != null && tags.Count > 0 && !tags.Contains(actor.VenueTag)) continue;
                    T casted = actor as T;
                    if(casted == null) continue;
                    //Apply filter
                    if(filter != null && !filter(casted)) continue;
                    actors.Add(casted);
                }
            }
            if(comparer != null)
            {
                actors.Sort(new Comparison<T>(comparer));
            }
            return actors;
        }

        /// <summary>
        /// Searches for interactables in the venue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tags"></param>
        /// <param name="filter"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public List<T> GetVenueInteractables<T>(List<int> tags, List<int> zoneTags, Func<T, bool> filter = null, Func<T, T, int> comparer = null) where T : VenueInteractable
        {
            List<T> interactables = new List<T>();
            foreach (var zone in Zones)
            {
                //Filter by zone tags
                if (!zone.Unlocked || zoneTags != null && zoneTags.Count > 0 && !zoneTags.Contains(zone.ZoneTag)) continue;
                foreach (var actor in zone.VenueActors)
                {
                    //Filter by actor tags
                    if (actor.IsLocked() || tags != null && tags.Count > 0 && !tags.Contains(actor.VenueTag)) continue;
                    T interactable = actor.GetModule<T>();
                    if (interactable == null || interactable.Disabled) continue;
                    //Apply filter
                    if (filter != null && !filter(interactable)) continue;
                    interactables.Add(interactable);
                }
            }

            //First, check the priority levels
            interactables.Sort((a, b) =>
            {
                //Higher interactable priority value means front of the list
                int priorityComparison = b.InteractablePriority.CompareTo(a.InteractablePriority);
                if (priorityComparison != 0)
                {
                    return priorityComparison;
                }
                else if (comparer != null)
                {
                    return comparer(a, b);
                }
                return 0;
            });
            return interactables;
        }
        /// <summary>
        /// Returns a mapping from a resource to resource dispensers that dispense that resource.
        /// </summary>
        /// <returns></returns>
        private Dictionary<ResourceData, HashSet<ResourceDispenser>> SetDispensersByResourceMap(List<int> tags = null)
        {
            List<VenueActor> actors = GetAvailableVenueActors();
            Dictionary<ResourceData, HashSet<ResourceDispenser>> map = new Dictionary<ResourceData, HashSet<ResourceDispenser>>();
            foreach(var actor in actors)
            {
                ResourceDispenser dispenser = actor.GetModule<ResourceDispenser>();
                if(dispenser == null) continue;
                foreach(var resource in dispenser.DispensedResources)
                {
                    if (!map.ContainsKey(resource))
                    {
                        map[resource] = new HashSet<ResourceDispenser>();
                    }
                    map[resource].Add(dispenser);

                }
            }
            _resourceToDispensers = map;
            return map;
        }

        private Dictionary<ResourceData, HashSet<ResourceSinker>> SetSinkersByResourceMap(List<int> tags = null)
        {
            List<VenueActor> actors = GetAvailableVenueActors();
            Dictionary<ResourceData, HashSet<ResourceSinker>> map = new Dictionary<ResourceData, HashSet<ResourceSinker>>();
            foreach (var actor in actors)
            {
                ResourceSinker sinker = actor.GetModule<ResourceSinker>();
                if (sinker == null) continue;
                foreach (var resource in sinker.AcceptedResources)
                {
                    if(resource == null)
                    {
                        Debug.LogError($"{sinker.gameObject} has null arg in sinker");
                        continue;
                    }
                    if (!map.ContainsKey(resource))
                    {
                        map[resource] = new HashSet<ResourceSinker>();
                    }
                    map[resource].Add(sinker);

                }
            }
            _resourceToSinkers = map;
            return map;
        }

        /// <summary>
        /// Returns dispensers that dispenser given resource
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public HashSet<ResourceDispenser> GetDispensersByResource(ResourceData resource)
        {
            if(_resourceToDispensers == null || !_resourceToDispensers.ContainsKey(resource)) return null;
            return _resourceToDispensers[resource];
        }

        /// <summary>
        /// Returns sinkers that accept given resource
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public HashSet<ResourceSinker> GetSinkersByResource(ResourceData resource)
        {
            if(_resourceToSinkers == null || !_resourceToSinkers.ContainsKey(resource)) return null;
            return _resourceToSinkers[resource];
        }
        #endregion

        #region WorkerManagement
        public void HireWorker(ArcadeIdleNpc workerPrefab)
        {
            ArcadeIdleNpc worker = Instantiate(workerPrefab);
            WorldPoint spawnPoint = WorkerSpawnZone.SampleWorldPoint();
            spawnPoint.Position.y = 0.01f;
            worker.transform.SetParent(transform);
            worker.transform.SetPositionAndRotation(spawnPoint.Position, spawnPoint.Rotation);
            WorldPoint point = new WorldPoint()
            {
                Position = spawnPoint.Position,
                Rotation = spawnPoint.Rotation,
            };
            worker.Spawn(this, point);
            _activeWorkers.Add(worker);
        }

        /// <summary>
        /// Hires worker at a given position and rotation
        /// </summary>
        /// <param name="workerPrefab"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public void HireWorker(ArcadeIdleNpc workerPrefab, ActorState actorState, Vector3 position, Quaternion rotation)
        {
            ArcadeIdleNpc worker = Instantiate(workerPrefab);
            worker.transform.SetParent(transform);
            worker.transform.position = position;
            worker.transform.rotation = rotation;
            WorldPoint point = new WorldPoint()
            {
                Position = position,
                Rotation = rotation,
            };
            worker.Spawn(this, point, actorState);
            _activeWorkers.Add(worker);
        }
        #endregion

        //todo: This code should be moved elsewhere. Maybe a child class
        #region Warehouse
        public void OnOrderArrived(Dictionary<ResourceData, int> order)
        {
            if(PackagesInventory == null) return;
            foreach(var pair in order)
            {
                for(int i=0;i<pair.Value;++i)
                {
                    PackagesInventory.AddResource(pair.Key, null, false);
                }
            }
        }
        #endregion
    }
}