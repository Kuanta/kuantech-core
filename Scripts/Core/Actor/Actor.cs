using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Utils;
using Unity.VisualScripting;
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
        [Header("Identifier")] 
        public string Id;
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
        
        protected bool Initialized;
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
        
        //Lifecycle events
        public UnityAction<ActorState> OnActorStateChanged;
        public UnityAction<Actor> OnSpawnedEvent;
        public UnityAction<Actor> OnDeathEvent;
        public UnityAction<Actor> OnDespawnedEvent;
        public UnityAction<HitInfo> OnHitEvent;
        public UnityAction<int> OnRankSetEvent;

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
            
            //Motions vector handler
            MotionVectorsHandler = new MotionVectorsHandler(this, ActorUpVector, ActorForwardVector);
            
            //Modules
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
                if(!module.ModuleId.IsNullOrEmpty()) ModulesById[module.ModuleId] = module;
            }
            
            //Initialize modules after getting them all so that they can require each other in their initialize methods
            foreach(var module in ActorModulesList)
            {
                module.Initialize();
            }

            VisualHandler = GetModule<ActorVisualHandler>();

            if(actorSerializableData != null)
            {
                //Load the data to the state
                LoadActorState(actorSerializableData);
            }else {
                SetDefaultStateValues();
            }

            OnModulesInitialized?.Invoke(this, EventArgs.Empty);
            Initialized = true;
            
            PostInitialize();
            
            ChangeActorState(ActorState.Inactive);

            //Call reset method
            Reset();
        }

        public void Spawn()
        {
            if (!Initialized)
            {
                Initialize();
            }
            else
            {
                Reset();
            }
            ChangeActorState(ActorState.Spawned);
        }

        public virtual void PostInitialize()
        {
            foreach (var module in ActorModulesList)
            {
                module.OnModulesInitialized();
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

        public virtual void Reset()
        {
            foreach (var module in ActorModulesList)
            {
                module.Reset();
            }
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
            OnDeathEvent = null;
            OnDespawnedEvent = null;
        }
        
        /// <summary>
        /// Despawns the actor by sending it to the pool
        /// </summary>
        public virtual void Despawn(float delay=0f)
        {
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
            }
            _despawnCoroutine = _DespawnRoutine(delay);
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(_despawnCoroutine);
        }
        
        private IEnumerator _despawnCoroutine;
        private IEnumerator _DespawnRoutine(float delay)
        {
            Cleanup();
            yield return new WaitForSeconds(delay);
            ChangeActorState(ActorState.Despawned);
            OnDespawnedEvent?.Invoke(this);
            if (VisualHandler != null)
            {
                VisualHandler.ClearCurrentVisual();
            }
            PoolManager.PoolObject(gameObject);
            _despawnCoroutine = null;
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
        /// Changes the actor state
        /// </summary>
        /// <param name="state"></param>
        public void ChangeActorState(ActorState state)
        {
            ActorState oldState = CurrentActorState;
            CurrentActorState = state;
            foreach (var module in ActorModulesList)
            {
                module.OnActorStateChanged(oldState, state);
            }
            OnActorStateChanged?.Invoke(state);
        }
        
        /// <summary>
        /// Kills the actor state, sets its state to dead
        /// </summary>
        public void KillActor()
        {
            ChangeActorState(ActorState.Dead);
            OnDeathEvent?.Invoke(this);
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
        /// Returns the first instance of a given mopdule type. Should only be used when searched for an explicit type and made sure that there is only a single instance of that component
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetModule<T>() where T : ActorModule
        {
            foreach (var pair in Modules)
            {
                if (pair.Value.Count > 0 && pair.Value[0] is T)
                {
                    return pair.Value[0] as T;
                }
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
            List<T> result = new List<T>();
            foreach (var pair in Modules)
            {
                if (pair.Value.Count > 0 && pair.Value[0] is T)
                {
                    result.AddRange(pair.Value);
                }
            }
            return result;
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
            LoadModuleState(actorSerializableData.ModuleStates);
        }
        public virtual void LoadModuleState(Dictionary<string, ActorModuleSerializableData> moduleStates)
        {
            foreach(var pair in moduleStates)
            {
                if (pair.Key.IsNullOrEmpty()) 
                {
                    continue;
                }
                if (!ModulesById.ContainsKey(pair.Key))
                {
                    Debug.LogError("Id is missing:" + pair.Key);
                    continue;
                }
                ActorModule module = ModulesById[pair.Key];
                module.LoadState(pair.Value);
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
                if(pair.Value.ModuleId.IsNullOrEmpty()) continue;
                actorSerializableData.ModuleStates[pair.Value.ModuleId] = pair.Value.CreateModuleState();
            }
            return actorSerializableData;
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
            if (hitPointSlot != null)
            {
                hitPoint.Target = hitPointSlot;
                hitPoint.Radius = ActorRadius;
            }
            return hitPoint;
        }

        public void OnHit(HitInfo hitInfo)
        {
            MovementModule mm = GetModule<MovementModule>();
            if (mm != null)
            {
                mm.Knockback(hitInfo.HitDirection, 
                    hitInfo.KnockbackForce, 
                    hitInfo.KnockbackDuration);
            }
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

        public Vector3 GetActorDirection()
        {
            RigidbodyMovementModule mm = GetModule<RigidbodyMovementModule>();
            if (mm == null) return transform.forward;
            return mm.GetForwardVector();
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
    }
}