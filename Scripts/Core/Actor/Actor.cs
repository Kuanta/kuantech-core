using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kuantech.Utils;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public enum ActorState
    {
        Inactive, //Initialized but not ready for action
        Spawned,
        Dead,
        Despawned,
    }

    public class Actor : MonoBehaviour, IHittable, ISpawnable
    {
        [Serializable]
        public struct KillFeedData
        {
            public GameObject Killer;
            public Actor DeadActor;
        }
 
        [Header("Update")]
        /// <summary>
        /// Minimum seconds between managed updates. 0 means every frame — the right value for the player and
        /// anything input touches. A crowd actor can afford far less, and that is the only thing that scales
        /// with enemy count: the frame is not one expensive module, it is a hundred actors each costing a
        /// little. Gameplay code is free to write this at runtime (an enemy raising its own rate as it closes
        /// on the player), which is why the policy lives here and not in ActorManager.
        ///
        /// Skipping does not freeze anyone: the delta handed to modules is measured from the last actual
        /// update, so an actor ticking at 10 Hz still moves, regenerates and cools down at the correct rate.
        /// </summary>
        [Tooltip("Minimum seconds between managed updates. 0 = every frame.")]
        public float UpdateInterval;

        [Tooltip("With an IUpdateRateProvider module present, UpdateInterval is driven between these two. " +
                 "Leave Max at 0 to keep UpdateInterval fixed.")]
        public float MinUpdateInterval;
        public float MaxUpdateInterval;

        private IUpdateRateProvider _updateRateProvider;

        [Header("Identifier")]
        public string Id;
        public string ActorVisualId;
        public int ActorRank = 0;
        
        [Header("Positioning Variables")]
        public Transform ActorAnchor;

        public float ActorRadius = 0.5f;

        [Header("Factions")]
       // public int FactionId = 0; //Since faction Id is used frequently, it is stated in Actor class
        public FactionHandler FactionHandler;
        
        [Header("Modules")]
        protected List<ActorModule> ActorModulesList;
        protected Dictionary<Type, List<ActorModule>> Modules = new Dictionary<Type, List<ActorModule>>();
        public Dictionary<string, ActorModule> ModulesById = new Dictionary<string, ActorModule>();
        
        [Header("Default Slots")]
        public string HitPointSlotName;

        //Common module references
        public ActorVisualHandler VisualHandler;
        
        [SerializeField] protected bool Initialized;
        [Tooltip("If set to true, actor will initialize itself on start")]
        public bool InitializeOnStart;
        
        [Header("Motion Vectors")]
        public Vector3 ActorUpVector = Vector3.up;
        public Vector3 ActorForwardVector = Vector3.forward;
        public MotionVectorsHandler MotionVectorsHandler;
        
        //Runtime
        public ActorState CurrentActorState = ActorState.Spawned;
        [NonSerialized] public bool Dirtied = false;
        [NonSerialized] public ActorBlueprint ActorBlueprint; //May be needed in some cases. If created by a template this should be non null

        //Events
        public EventHandler OnModulesInitialized;
        public EventHandler<float> OnActorRadiusSet;
        public EventHandler<WorldPoint> OnActorWarped;
        
        //Lifecycle events
        public UnityAction<ActorState> OnActorStateChanged;
        public UnityAction<Actor> OnSpawnedEvent;
        public UnityAction<Actor> OnDeathEvent;
        public UnityAction<Actor> OnDespawnedEvent;
        public UnityAction<HitInfo> OnHitEvent;
        public UnityAction<int> OnRankSetEvent;
        public UnityAction<Actor> OnStateLoaded;
        /// <summary>
        /// Fired from KillActor, the one place the killer is actually known -- ChangeActorState's Dead
        /// transition also replicates to observers, but only the ActorState enum crosses that RPC, so a
        /// killer looked up from there would always be null on remote peers. Server-only in practice, since
        /// KillActor is only ever called from HealthcareModule.OnHit's server-gated death branch.
        /// </summary>
        public UnityAction<KillFeedData> OnKillFeedEvent;

        //Outbound events for whatever carries this actor to other peers. Nobody listens offline.
        //Deliberately not wiped by Cleanup: the binding subscribes once in Initialize and the actor outlives a despawn when pooled.
        public UnityAction<ActorState> OnStateChangeBroadcast;
        public UnityAction<Vector3, Quaternion> OnControllerWarpRequested;
        public UnityAction<ulong> OnStatePushRequested;

        #region Lifecycle
        private void Start()
        {
            if(InitializeOnStart)
            {
                Initialize(null);
            }
        }

        public virtual void Initialize(ActorSerializableData actorSerializableData = null)
        {
            if (Initialized) return;

            //Motion vector handler
            MotionVectorsHandler = new MotionVectorsHandler(this, ActorForwardVector, ActorUpVector);
            CurrentActorState = ActorState.Inactive;
            
            //Modules — discover all first so modules can reference each other during Initialize()
            ActorModulesList = GetComponentsInChildren<ActorModule>().ToList();
            ModulesById = new Dictionary<string, ActorModule>();
            foreach (ActorModule module in ActorModulesList)
            {
                if(!Modules.ContainsKey(module.GetType()))
                {
                    Modules[module.GetType()] = new List<ActorModule>();
                }
                Modules[module.GetType()].Add(module);
                module.Actor = this;
                if(!string.IsNullOrEmpty(module.ModuleId)) ModulesById[module.ModuleId] = module;
            }

            // Bindings go before module initialization so a module's first event already reaches its listeners.
            foreach (IActorNetBinding binding in GetComponentsInChildren<IActorNetBinding>())
            {
                binding.Bind(this);
            }

            foreach(var module in ActorModulesList)
            {
                module.Initialize();
            }

            VisualHandler = GetModule<ActorVisualHandler>();

            // First module with an opinion on how often this actor needs updating wins; the rest stay silent.
            foreach (ActorModule module in ActorModulesList)
            {
                if (module is not IUpdateRateProvider provider) continue;
                _updateRateProvider = provider;
                break;
            }

            BuildPhaseLists();

            if(actorSerializableData != null)
            {
                LoadActorState(actorSerializableData);
            }
            else
            {
                SetDefaultStateValues();
            }

            Initialized = true;

            //Notify modules first, then external subscribers
            PostInitialize();
            OnModulesInitialized?.Invoke(this, EventArgs.Empty);
        }

        public void Spawn()
        {
            if (!Initialized)
            {
                Initialize();
            }
            ResetActor();
            // Not ChangeActorState(): that gates on HasAuthority and broadcasts to observers, which races
            // the network spawn on peers that haven't fully replicated this object yet (the message can arrive
            // before the object exists there) — leaving CurrentActorState stuck at Inactive forever on that
            // peer, which makes ActorManager.IsUpdatable() permanently skip it (no ModuleUpdate ever again).
            // Every peer's own Spawn() activating its own local instance directly is safe: this only decides
            // "is my local copy ready to tick," not shared game truth, so it needs no server authority.
            ExecuteChangeActorState(ActorState.Spawned);

            // Start the clock here, not at zero. The delta handed to modules is measured from this stamp, so
            // a fresh (or recycled) actor would otherwise take its whole lifetime as its first frame's delta.
            // The random backdate spreads a batch of enemies spawned on the same frame across the interval —
            // without it they would all come due on the same frames forever, which moves the spike rather
            // than removing it.
            _lastUpdateTime = Time.time - UnityEngine.Random.Range(0f, UpdateInterval);

            //Register to ActorManager
            ActorManager.RegisterActor(this);
        }

        public virtual void PostInitialize()
        {
            foreach (var module in ActorModulesList)
            {
                module.OnModulesInitialized();
            }
        }
        
        /// <summary>
        /// Profiler markers per module type, created once and shared by every actor.
        ///
        /// ActorManager calls the Managed*Update methods directly rather than through Unity messages, which
        /// is the point — but it means Unity gives them no markers, and every module's cost collapses into
        /// ActorManager's self time. A profile can then say the actors are expensive without ever saying
        /// which module is. These markers compile out entirely when profiling is off.
        /// </summary>
        // Markers resolved once per actor into arrays that line up with ActorModulesList, rather than looked
        // up per call. The lookup used to be a dictionary keyed by type and phase, which ran thousands of
        // times a frame — and because it sat outside the marker it landed in the caller's self time, hiding
        // the very breakdown the markers exist to produce.
        // Modules split by the phases they actually implement, plus the markers that line up with them.
        // Most modules override none of the three: ActorVisualHandler, ActorSlotsHandler, EffectsModule and
        // a dozen others were being called ~330 times a frame each only to run an empty base method. Working
        // out who implements what once, at initialisation, removes both those calls and any per-frame test
        // for them — the loops simply never see a module that has nothing to do.
        private ActorModule[] _updateModules, _fixedUpdateModules, _lateUpdateModules;
        private ProfilerMarker[] _updateMarkers, _fixedUpdateMarkers, _lateUpdateMarkers;

        // Reflection is per module TYPE, not per instance: the first goblin pays for it and the other 109,
        // plus every one recycled from the pool afterwards, read the cached answer.
        private static readonly Dictionary<Type, bool[]> _implementedPhases = new Dictionary<Type, bool[]>();

        private static readonly string[] PhaseNames = { "ModuleUpdate", "ModuleFixedUpdate", "ModuleLateUpdate" };

        private static bool[] GetImplementedPhases(Type moduleType)
        {
            if (_implementedPhases.TryGetValue(moduleType, out bool[] phases)) return phases;

            phases = new bool[PhaseNames.Length];
            for (int i = 0; i < PhaseNames.Length; i++)
            {
                MethodInfo method = moduleType.GetMethod(PhaseNames[i],
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                phases[i] = method != null && method.DeclaringType != typeof(ActorModule);
            }
            _implementedPhases[moduleType] = phases;
            return phases;
        }

        private void BuildPhaseLists()
        {
            var update = new List<ActorModule>();
            var fixedUpdate = new List<ActorModule>();
            var lateUpdate = new List<ActorModule>();

            foreach (ActorModule module in ActorModulesList)
            {
                if (module == null) continue;
                bool[] phases = GetImplementedPhases(module.GetType());
                if (phases[0]) update.Add(module);
                if (phases[1]) fixedUpdate.Add(module);
                if (phases[2]) lateUpdate.Add(module);
            }

            _updateModules = update.ToArray();
            _fixedUpdateModules = fixedUpdate.ToArray();
            _lateUpdateModules = lateUpdate.ToArray();

            _updateMarkers = BuildMarkers(_updateModules, PhaseNames[0]);
            _fixedUpdateMarkers = BuildMarkers(_fixedUpdateModules, PhaseNames[1]);
            _lateUpdateMarkers = BuildMarkers(_lateUpdateModules, PhaseNames[2]);
        }

        private static ProfilerMarker[] BuildMarkers(ActorModule[] modules, string phase)
        {
            var markers = new ProfilerMarker[modules.Length];
            for (int i = 0; i < markers.Length; i++)
                markers[i] = new ProfilerMarker($"{modules[i].GetType().Name}.{phase}");
            return markers;
        }

        public virtual void ManagedFixedUpdate()
        {
            if (!Initialized || _fixedUpdateModules == null) return;
            for (int i = 0; i < _fixedUpdateModules.Length; i++)
            {
#if ENABLE_PROFILER
                using (_fixedUpdateMarkers[i].Auto())
#endif
                _fixedUpdateModules[i].ModuleFixedUpdate();
            }
        }

        private float _lastUpdateTime;
        private float _deltaTime;

        /// <summary>
        /// Whether enough time has passed for this actor's managed update. ActorManager asks; the actor
        /// answers from its own <see cref="UpdateInterval"/>, so the scheduling policy stays with the thing
        /// it describes and the manager never needs to know what kind of actor this is.
        /// </summary>
        public bool ShouldUpdate()
        {
            if (UpdateInterval <= 0f) return true;
            return Time.time - _lastUpdateTime >= UpdateInterval;
        }

        public virtual void ManagedUpdate()
        {
            _deltaTime = Time.time - _lastUpdateTime;
            _lastUpdateTime = Time.time;
            if (!Initialized || _updateModules == null) return;

            // Re-decide the rate for the next period, here rather than in ShouldUpdate: this runs only on the
            // frames the actor is actually updated, so a distant enemy ticking at 5 Hz is asked five times a
            // second instead of sixty. The cost is up to one period of staleness, which is the same order as
            // the interval it is choosing.
            if (MaxUpdateInterval > 0f && _updateRateProvider != null)
            {
                UpdateInterval = Mathf.Lerp(MinUpdateInterval, MaxUpdateInterval,
                    Mathf.Clamp01(_updateRateProvider.GetUpdateIntervalFactor()));
            }

            for (int i = 0; i < _updateModules.Length; i++)
            {
#if ENABLE_PROFILER
                using (_updateMarkers[i].Auto())
#endif
                _updateModules[i].ModuleUpdate(_deltaTime);
            }
        }

        public virtual void ManagedLateUpdate()
        {
            if (!Initialized || _lateUpdateModules == null) return;
            for (int i = 0; i < _lateUpdateModules.Length; i++)
            {
#if ENABLE_PROFILER
                using (_lateUpdateMarkers[i].Auto())
#endif
                _lateUpdateModules[i].ModuleLateUpdate(_deltaTime);
            }
        }

        public virtual void ResetActor()
        {
            foreach (var module in ActorModulesList)
            {
                module.ResetModule();
            }

            MotionVectorsHandler.Reset();
        }

        public virtual void Cleanup()
        {
            // Symmetric with Spawn's RegisterActor. Cleanup is the single despawn chokepoint (both the local
            // and network paths call it), so the actor leaves the managed-update loop exactly once here.
            ActorManager.UnregisterActor(this);

            foreach (var module in ActorModulesList)
            {
                module.Cleanup();
            }

            //Cleanup events
            OnDeathEvent = null;
            OnActorStateChanged = null;
            OnSpawnedEvent = null;
            OnDespawnedEvent = null;
            OnHitEvent = null;
            OnKillFeedEvent = null;
        }
        
        /// <summary>
        /// Despawns the actor. Only the authority can: when the network manages this actor's lifetime it
        /// despawns it everywhere, otherwise the actor despawns itself locally.
        /// </summary>
        public virtual void Despawn(float delay=0f)
        {
            if (_despawnCoroutine != null)
                StopCoroutine(_despawnCoroutine);

            if(delay <= 0f)
            {
                _Despawn();
            }
            else
            {
                _despawnCoroutine = _DespawnRoutine(delay);
                StartCoroutine(_despawnCoroutine);
            }
        }

        private IEnumerator _despawnCoroutine;
        private IEnumerator _DespawnRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            _Despawn();
            _despawnCoroutine = null;
        }

        private void _Despawn()
        {
            if (!HasAuthority)
            {
                // Not ours to despawn: the authority does it and the network layer calls HandleExternalDespawn.
                _despawnCoroutine = null;
                return;
            }
            if (Net.TryDespawn())
            {
                // The network layer despawns every peer's copy and calls HandleExternalDespawn on each.
                _despawnCoroutine = null;
                return;
            }
            ExecuteLocalDespawn();
        }

        private void ExecuteLocalDespawn()
        {
            // Fire the Despawned state change (and OnDespawnedEvent) BEFORE Cleanup nulls the delegates,
            // otherwise external subscribers never get notified of the despawn.
            ExecuteChangeActorState(ActorState.Despawned);
            Cleanup();
            if (VisualHandler != null) VisualHandler.ClearCurrentVisual();
            Destroy(gameObject);
            //PoolManager.PoolObject(gameObject);
        }

        /// <summary>
        /// Called by whoever owns this actor's lifetime once it has already despawned it -- cleanup only, no
        /// destroy or pooling, since that owner decides what happens to the object itself.
        /// </summary>
        public void HandleExternalDespawn()
        {
            // Notify despawn before Cleanup wipes the event delegates (see ExecuteLocalDespawn).
            ExecuteChangeActorState(ActorState.Despawned);
            Cleanup();
            if (VisualHandler != null) VisualHandler.ClearCurrentVisual();
        }
        #endregion

        #region ActorRank

        public void SetActorRank(int rank)
        {
            ActorRank = rank;
            OnRankSetEvent?.Invoke(rank);
            foreach (var module in ActorModulesList)
            {
                module.OnActorRankSet(rank);
            }
        }

        public int GetActorRank()
        {
            return ActorRank;
        }

        public void IncreaseActorRank()
        {
            SetActorRank(GetActorRank()+1);
        }
        #endregion

        #region Actor States
        
        /// <summary>
        /// Changes the actor state. Authority only; every other peer is told through
        /// <see cref="OnStateChangeBroadcast"/> and applies it with <see cref="ExecuteChangeActorState"/>.
        /// </summary>
        /// <param name="state"></param>
        public void ChangeActorState(ActorState state)
        {
            if (!HasAuthority) return;
            ExecuteChangeActorState(state);
            OnStateChangeBroadcast?.Invoke(state);
        }

        public void ExecuteChangeActorState(ActorState state)
        {
            ActorState oldState = CurrentActorState;
            CurrentActorState = state;
            foreach (var module in ActorModulesList)
            {
                module.OnActorStateChanged(oldState, state);
            }
            OnActorStateChanged?.Invoke(state);

            switch(state)
            {
                case ActorState.Spawned:
                    OnSpawnedEvent?.Invoke(this);
                    break;
                case ActorState.Dead:
                    OnDeathEvent?.Invoke(this);
                    break;
                case ActorState.Despawned:
                    OnDespawnedEvent?.Invoke(this);
                    break;
            }
        }
        
        /// <summary>
        /// Kills the actor, sets its state to dead
        /// </summary>
        public void KillActor(GameObject killer = null)
        {
            OnKillFeedEvent?.Invoke(new KillFeedData { Killer = killer, DeadActor = this });
            ChangeActorState(ActorState.Dead);
        }


        /// <summary>
        /// Checks if actor is alive
        /// </summary>
        /// <returns></returns>
        public bool IsAlive()
        {
            return CurrentActorState == ActorState.Spawned;
        }

        #endregion
        
        #region Modules
        
        /// <summary>
        /// Returns the first instance of a given module type. Should only be used when searched for an explicit type and made sure that there is only a single instance of that component
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetModule<T>() where T : ActorModule
        {
            if (Modules.TryGetValue(typeof(T), out var moduleList) && moduleList.Count > 0)
            {
                return moduleList[0] as T;
            }
            return null;
        }
        
        /// <summary>
        /// Returns all modules that match the given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetModules<T>() where T : ActorModule
        {
            if (Modules.TryGetValue(typeof(T), out var moduleList))
            {
                return moduleList.Cast<T>().ToList();
            }
            return new List<T>();
        }

        #endregion

        #region State
    
        /// <summary>
        /// The method that should be overriden for actors that needs their o
        /// </summary>
        /// <returns></returns>
        protected virtual ActorSerializableData InstantiateActorState()
        {
            return new ActorSerializableData()
            {
                ActorId = Id,
            };
        }

        private ActorSerializableData CreateActorState()
        {
            ActorSerializableData actorSerializableData = InstantiateActorState();
            actorSerializableData.ActorId = Id;
            actorSerializableData.ActorVisualId = ActorVisualId;
            return actorSerializableData;
        }

        public virtual void DirtyState()
        {
            Dirtied = true;
        }

        /// <summary>
        /// Sets the default values for the actor state
        /// </summary>
        public virtual void SetDefaultStateValues()
        {
            foreach (var module in ActorModulesList)
            {
                module.SetDefaultValues();
            }
        }

        /// <summary>
        /// Loads the actor state
        /// </summary>
        /// <param name="actorSerializableData"></param>
        public virtual void LoadActorState(ActorSerializableData actorSerializableData)
        {
            if(actorSerializableData == null)
            {
                SetDefaultStateValues();
                return;
            }
            if (!string.IsNullOrEmpty(actorSerializableData.ActorVisualId))
                ApplyVisual(actorSerializableData.ActorVisualId);
            LoadModuleState(actorSerializableData.ModuleStates);
            OnStateLoaded?.Invoke(this);
        }

        public virtual void LoadModuleState(Dictionary<string, ActorModuleSerializableData> moduleStates)
        {
            foreach(var pair in moduleStates)
            {
                if (string.IsNullOrEmpty(pair.Key)) continue;
                if (!ModulesById.ContainsKey(pair.Key))
                {
                    Debug.LogError($"[Actor] LoadModuleState: no module found for key '{pair.Key}'");
                    continue;
                }
                ModulesById[pair.Key].LoadState(pair.Value);
            }
        }
        /// <summary>
        /// Gets the actor stat
        /// </summary>
        /// <returns></returns>
        public virtual ActorSerializableData GetActorState()
        {
            ActorSerializableData actorSerializableData = CreateActorState();
            actorSerializableData.ModuleStates = new Dictionary<string, ActorModuleSerializableData>();
            foreach(var pair in ModulesById)
            {
                ActorModuleSerializableData serializableData = pair.Value.CreateModuleState();
                if(serializableData == null) continue;
                actorSerializableData.ModuleStates[pair.Key] = serializableData;
            }
            return actorSerializableData;
        }

        /// <summary>
        /// Applies identity and faction from spawn data immediately.
        /// If the actor is already initialized, also loads module states from ModuleDatas.
        /// Safe to call before Initialize (module datas will be skipped and must be re-applied after).
        /// </summary>
        public void ApplySpawnData(ActorSpawnData data)
        {
            Initialize();
            Id = data.ActorId;
            ActorVisualId = data.ActorVisualId;
            if (FactionHandler != null)
                FactionHandler.BelongingFaction = data.FactionId;

            if (!Initialized || data.ModuleDatas == null || data.ModuleDatas.Count == 0) return;

            var dict = new Dictionary<string, ActorModuleSerializableData>();
            foreach (var md in data.ModuleDatas)
                if (!string.IsNullOrEmpty(md.ModuleId)) dict[md.ModuleId] = md;
            if (dict.Count > 0) LoadModuleState(dict);
        }

        public void ApplyVisual(string visualId)
        {
            if (string.IsNullOrEmpty(visualId)) return;
            ActorVisualId = visualId;
            ActorVisual prefab = ActorDataManager.GetActorVisual(visualId);
            if (prefab == null)
            {
                Debug.LogWarning($"[Actor] ApplyVisual: no visual found for id '{visualId}'");
                return;
            }
            ActorVisual instance = Instantiate(prefab);
            VisualHandler?.SetActorVisual(instance);
        }

        #endregion
        
        #region IHittable
        public virtual bool CanBeHit()
        {
            return true;
        }

        public virtual WorldPoint GetHitPoint(Actor attackingActor)
        {
            WorldPoint hitPoint = new WorldPoint()
            {
                Target =  GetActorAnchor(),
                Radius = ActorRadius,
            };
            ActorSlotsHandler actorSlotsHandler = GetModule<ActorSlotsHandler>();
            if (actorSlotsHandler == null || HitPointSlotName == null || HitPointSlotName == "") return hitPoint;
            
            Transform hitPointSlot =  actorSlotsHandler.GetSlot(HitPointSlotName);
            if (hitPointSlot != null) //Hit point is not null, return it instead

            {
                hitPoint.Target = hitPointSlot;
                hitPoint.Radius = ActorRadius;
            }
            return hitPoint;
        }

        public void OnHit(HitInfo hitInfo)
        {
            // Faction relevance lives here now, not in whatever physics query found this actor —
            // CombatUtilities only knows about layers and CanBeHit(); the actor itself decides whether the
            // hitter (from HitInfo.Hitter) is actually its enemy. No hitter, or a hitter with no Actor
            // (e.g. an environmental damage source), means there's nothing to filter — the hit proceeds.
            if (hitInfo.Hitter != null && hitInfo.Hitter.TryGetComponent(out Actor hitter) && !hitter.IsEnemy(this))
                return;

            OnHitEvent?.Invoke(hitInfo);
        }
        
        public void OnHit(GameObject attacker, DamageInfo damageInfo)
        {
           
            OnHit(new HitInfo()
            {
                Hitter = attacker,
                DamageInfo = damageInfo,
            });
        }
        #endregion
        
        #region Utitilities

        public void WarpToPoint(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
            OnActorWarped?.Invoke(this, new WorldPoint()
            {
                Position = position,
                Rotation = rotation,
            });
        }

        /// <summary>
        /// Authority only. Moves whoever CONTROLS this actor to a new position, touching nothing about its
        /// state -- unlike a respawn, this is not "the player came back from something", just "the level moved".
        ///
        /// Split across two peers for the same reason DownedModule.Respawn is: a controller-authoritative
        /// transform (NetworkTransform, say) would just overwrite a position the authority wrote directly on
        /// its next update. So when the authority is not also the controller, the move is asked for through
        /// <see cref="OnControllerWarpRequested"/> and applied with <see cref="WarpToPoint"/> on the controller.
        /// </summary>
        public void WarpControllerToPoint(Vector3 position, Quaternion rotation)
        {
            if (!HasAuthority) return;
            if (IsLocallyControlled)
            {
                WarpToPoint(position, rotation);
                return;
            }
            OnControllerWarpRequested?.Invoke(position, rotation);
        }

        public Vector3 GetActorDirection()
        {
            return MotionVectorsHandler?.GetTargetVector() ?? transform.forward;
        }
        #endregion

        #region Actor Location
        /// <summary>
        /// Returns the actors location
        /// </summary>
        /// <returns></returns>
        public Vector3 GetActorLocation()
        {
            if (ActorAnchor != null)
            {
                return ActorAnchor.transform.position;
            }
            return transform.position;
        }

        public Transform GetActorAnchor()
        {
            if(ActorAnchor != null)
            {
                return ActorAnchor;
            }

            return transform;
        }

        public void SetActorAnchor(Transform anchor)
        {
            ActorAnchor = anchor;
        }

        public void SetActorRadius(float radius)
        {
            ActorRadius = radius;
            OnActorRadiusSet?.Invoke(this, radius);
        }
        
        /// <summary>
        /// Returns distance towards point
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public float GetDistanceToPosition(Vector3 point)
        {
            Vector3 actorPosition = GetActorLocation();
            Vector3 diff = (point - actorPosition);
            float diffMag = diff.magnitude;
            return Mathf.Max(0, diffMag - ActorRadius);
        }
        #endregion
        
        #region Factions
        
        /// <summary>
        /// Sets the faction Id
        /// </summary>
        /// <param name="factionId"></param>
        public void SetFactionId(int factionId)
        {
            if (FactionHandler == null)
            {
                Debug.LogWarning("Faction Handler is null");
                FactionHandler = new FactionHandler();
            }
            FactionHandler.BelongingFaction = factionId;
        }
        
        /// <summary>
        /// Returns faction Id
        /// </summary>
        /// <returns></returns>
        public int GetFactionId()
        {
            if (FactionHandler == null)
            {
                Debug.LogWarning("Faction Handler is null");
                return 0;
            }
            return FactionHandler.BelongingFaction;
        }
        public bool IsAlly(Actor otherActor)
        {
            FactionHandler.FactionType relationType = FactionHandler.GetFactionRelation(otherActor);
            return relationType == FactionHandler.FactionType.Ally || relationType == FactionHandler.FactionType.Same;
        }
        
        public bool IsEnemy(Actor otherActor)
        {
            return FactionHandler.GetFactionRelation(otherActor) == FactionHandler.FactionType.Enemy;
        }
        
        #endregion

        #region Authority

        /// <summary>The network this actor answers to. Offline until a binding registers itself.</summary>
        public IActorNetwork Net { get; private set; } = OfflineActorNetwork.Instance;

        /// <summary>This peer decides game truth for the actor.</summary>
        public bool HasAuthority => Net.HasAuthority;

        /// <summary>This peer's input drives the actor.</summary>
        public bool IsLocallyControlled => Net.IsLocallyControlled;

        /// <summary>Id of the peer that controls the actor.</summary>
        public ulong OwnerId => Net.OwnerId;

        /// <summary>This peer shows the game to a player (false on a dedicated server).</summary>
        public bool HasPresentation => Net.HasPresentation;

        /// <summary>Called by an <see cref="IActorNetBinding"/> from Bind to take over the authority questions.</summary>
        public void SetNetwork(IActorNetwork network)
        {
            Net = network ?? OfflineActorNetwork.Instance;
        }

        /// <summary>
        /// Authority only. Asks the network layer to send a full state snapshot to one peer, for a peer that
        /// joined after this actor was already live. The peer applies it with <see cref="LoadSyncedState"/>.
        /// </summary>
        public void PushStateTo(ulong targetId)
        {
            if (!HasAuthority) return;
            OnStatePushRequested?.Invoke(targetId);
        }

        /// <summary>Applies a snapshot received from the authority, then lets modules react to being in sync.</summary>
        public void LoadSyncedState(ActorSerializableData state)
        {
            LoadActorState(state);
            foreach (var module in ActorModulesList)
                module.OnNetworkSynced();
        }

        #endregion

        #region Local Player

        public bool IsLocalPlayer()
        {
            return IsLocallyControlled;
        }

        public void StartLocalPlayer()
        {
            foreach (var module in ActorModulesList)
                module.OnLocalPlayerStart();
        }

        public void StopLocalPlayer()
        {
            foreach (var module in ActorModulesList)
                module.OnLocalPlayerStop();
        }

        #endregion
    }
}