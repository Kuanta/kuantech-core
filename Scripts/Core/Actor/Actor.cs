using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

#if NETWORKING_FISHNET
    using FishNet.Connection;
    using FishNet.Object;
#endif

namespace Kuantech.Core
{
    public enum ActorState
    {
        Inactive, //Initialized but not ready for action
        Spawned,
        Dead,
        Despawned,
    }

#if NETWORKING_FISHNET

    public class Actor : NetworkBehaviour, IHittable, ISpawnable
#else
    public class Actor : MonoBehaviour, IHittable, ISpawnable
#endif
    {

        #if !NETWORKING_FISHNET
        public bool IsOwner => true;
        public bool IsServer => true;
        public bool IsClient => true;
        #endif

        [Serializable]
        public struct KillFeedData
        {
            public GameObject Killer;
            public Actor DeadActor;
        }
 
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

            foreach(var module in ActorModulesList)
            {
                module.Initialize();
            }

            VisualHandler = GetModule<ActorVisualHandler>();

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
            ChangeActorState(ActorState.Spawned);
        }

        public virtual void PostInitialize()
        {
            foreach (var module in ActorModulesList)
            {
                module.OnModulesInitialized();
            }
        }
        
        protected virtual void FixedUpdate()
        {
            if (!Initialized) return;
            foreach (var module in ActorModulesList)
            {
                module.ModuleFixedUpdate();
            }
        }
        
        protected virtual void Update()
        {
            if (!Initialized) return;
            foreach (var module in ActorModulesList)
            {
                module.ModuleUpdate();
            }
        }
        
        protected virtual void LateUpdate()
        {
            if (!Initialized) return;
            foreach (var module in ActorModulesList)
            {
                module.ModuleLateUpdate();
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
        }
        
        /// <summary>
        /// Despawns the actor. On server: tells all clients via FishNet. Standalone: returns to pool directly.
        /// </summary>
        public virtual void Despawn(float delay=0f)
        {
            if (_despawnCoroutine != null)
                StopCoroutine(_despawnCoroutine);

            _despawnCoroutine = _DespawnRoutine(delay);
            StartCoroutine(_despawnCoroutine);
        }

        private IEnumerator _despawnCoroutine;
        private IEnumerator _DespawnRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
#if NETWORKING_FISHNET
            if (IsClientInitialized && !IsServerInitialized)
            {
                // Pure client: FishNet will call OnStopClient when server despawns — do nothing here
                _despawnCoroutine = null;
                yield break;
            }
            if (IsServerInitialized && IsSpawned)
            {
                // Server: FishNet notifies all clients; cleanup happens in OnStopServer/OnStopClient
                NetworkObject.Despawn();
                _despawnCoroutine = null;
                yield break;
            }
#endif
            ExecuteLocalDespawn();
            _despawnCoroutine = null;
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

        // Called from FishNet callbacks — cleanup only, no pooling (FishNet owns the lifecycle)
        private void ExecuteNetworkDespawn()
        {
            // Notify despawn before Cleanup wipes the event delegates (see ExecuteLocalDespawn).
            ExecuteChangeActorState(ActorState.Despawned);
            Cleanup();
            if (VisualHandler != null) VisualHandler.ClearCurrentVisual();
        }

#if NETWORKING_FISHNET
        public override void OnStopServer()
        {
            base.OnStopServer();
            ExecuteNetworkDespawn();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            if (_isLocalPlayer) OnStopLocalPlayer();
            if (!IsServerInitialized) ExecuteNetworkDespawn();
        }
#endif
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
        /// Changes the actor state
        /// </summary>
        /// <param name="state"></param>
        public void ChangeActorState(ActorState state)
        {
#if NETWORKING_FISHNET
            if (!IsServerInitialized) return;
            ActorState oldState = CurrentActorState;
            ExecuteChangeActorState(state);
            if (IsSpawned) ObserversChangeActorState_Rpc(oldState, state);
#else
            ExecuteChangeActorState(state);
#endif
        }

#if NETWORKING_FISHNET
        [ObserversRpc]
        private void ObserversChangeActorState_Rpc(ActorState oldState, ActorState state)
        {
            if (IsServerInitialized) return;
            ExecuteChangeActorState(state);
        }
#endif

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

        private void ApplyVisual(string visualId)
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
            if (actorSlotsHandler == null) return hitPoint;
            
            Transform hitPointSlot =  actorSlotsHandler.GetSlot("HitPoint");
            if (hitPointSlot != null) //Hit point is not null, return it instead

            {
                hitPoint.Target = hitPointSlot;
                hitPoint.Radius = ActorRadius;
            }
            return hitPoint;
        }

        public void OnHit(HitInfo hitInfo)
        {
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

        #region Networking

#if NETWORKING_FISHNET
        private bool _isLocalPlayer;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            Initialize();
            Spawn();
            TryHandleLocalPlayerChange(null);
        }

        public override void OnSpawnServer(NetworkConnection connection)
        {
            base.OnSpawnServer(connection);
            ActorSerializableData state = GetActorState();
            int moduleCount = state.ModuleStates?.Count ?? 0;
            Debug.Log($"[Actor] OnSpawnServer → {name} syncing {moduleCount} module(s) to client {connection.ClientId}. Modules: [{string.Join(", ", state.ModuleStates?.Keys ?? Enumerable.Empty<string>())}]");
            byte[] bytes = SaveUtility.SerializePoco(state);
            TargetSyncActorState_Rpc(connection, bytes);
        }

        [TargetRpc]
        private void TargetSyncActorState_Rpc(NetworkConnection conn, byte[] bytes)
        {
            ActorSerializableData state = SaveUtility.DeserializePoco<ActorSerializableData>(bytes);
            int moduleCount = state?.ModuleStates?.Count ?? 0;
            Debug.Log($"[Actor] TargetSyncActorState_Rpc received on {name} — {moduleCount} module(s): [{string.Join(", ", state?.ModuleStates?.Keys ?? Enumerable.Empty<string>())}]");
            LoadActorState(state);
            foreach (var module in ActorModulesList)
                module.OnNetworkSynced();
        }

        /// <summary>
        /// Server calls this after spawn to set the visual on all peers (including host).
        /// </summary>
        [ObserversRpc(RunLocally = true)]
        public void SetVisualRpc(string visualId)
        {
            ApplyVisual(visualId);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            TryHandleLocalPlayerChange(null);
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            TryHandleLocalPlayerChange(prevOwner);
        }

        private void TryHandleLocalPlayerChange(NetworkConnection prevOwner)
        {
            if (!IsClientInitialized || NetworkManager == null) return;
            if (!Owner.IsValid)
            {
                if (_isLocalPlayer) OnStopLocalPlayer();
                return;
            }
            bool nowLocalPlayer = Owner == NetworkManager.ClientManager.Connection;
            if (nowLocalPlayer == _isLocalPlayer) return;
            _isLocalPlayer = nowLocalPlayer;
            if (nowLocalPlayer) OnStartLocalPlayer();
            else OnStopLocalPlayer();
        }

        private void OnStartLocalPlayer() => StartLocalPlayer();

        private void OnStopLocalPlayer()
        {
            _isLocalPlayer = false;
            StopLocalPlayer();
        }
#endif

        public bool IsOnlyClient()
        {
#if NETWORKING_FISHNET
            return IsClientOnlyInitialized;
#else
            return false;
#endif
        }

        public bool IsLocalPlayer()
        {
#if NETWORKING_FISHNET
            return IsOwner;
#else
            return true;
#endif
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