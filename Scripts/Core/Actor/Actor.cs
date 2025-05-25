using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core.Combat;
using Kuantech.Utils;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public enum ActorState
    {
        Alive,
        Dead,
        Despawned,
    }
    
    public class Actor : MonoBehaviour, IHittable
    {
        [Header("Identifier")]
        public string Id;
        public int FactionId = 0; //Since faction Id is used frequently, it is stated in Actor class
        
        [Header("Components")]
        public ActorVisualHandler VisualHandler;
        
        [Header("Modules")]
        protected List<ActorModule> ActorModulesList;
        protected Dictionary<Type, List<ActorModule>> Modules = new Dictionary<Type, List<ActorModule>>();
        public Dictionary<string, ActorModule> ModulesById = new Dictionary<string, ActorModule>();

        protected bool Initialized;
        [Tooltip("If set to true, actor will initialize itself on start")]
        public bool InitializeOnStart;

        //Runtime
        public ActorState CurrentActorState = ActorState.Alive;
        [NonSerialized] public bool Dirtied = false;

        //Events
        public EventHandler OnModulesInitialized;
        
        //Lifecycle events
        public UnityAction<ActorState> OnActorStateChanged;
        public UnityAction<Actor> OnSpawnedEvent;
        public UnityAction<Actor> OnDeathEvent;
        public UnityAction<Actor> OnDespawnedEvent;

        public UnityAction<HitInfo> OnHitEvent;

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
            if(actorSerializableData != null)
            {
                //Load the data to the state
                LoadActorState(actorSerializableData);
            }else {
                SetDefaultStateValues();
            }

            OnModulesInitialized?.Invoke(this, EventArgs.Empty);
            Initialized = true;

            //Call reset method
            Reset();
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
        }
        
        /// <summary>
        /// Despawns the actor by sending it to the pool
        /// </summary>
        public virtual void Despawn()
        {
            Cleanup();
            ChangeActorState(ActorState.Despawned);
            OnDespawnedEvent?.Invoke(this);
            VisualHandler.ClearCurrentVisual();
            PoolManager.PoolObject(gameObject);
        }
        #endregion

        #region Actor States
        
        /// <summary>
        /// Changes the actor state
        /// </summary>
        /// <param name="state"></param>
        public void ChangeActorState(ActorState state)
        {
            OnActorStateChanged?.Invoke(state);
            CurrentActorState = state;
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
            return CurrentActorState == ActorState.Alive;
        }

        #endregion
        
        #region Modules

        /// <summary>
        /// Return module by type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ActorModule GetModule(Type type)
        {
            if(Modules.ContainsKey(type) && Modules[type].Count > 0)
            {
                return Modules[type][0];
            }
            return null;
        }
        
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

        public void OnHit(HitInfo hitInfo)
        {
            OnHitEvent?.Invoke(hitInfo);
        }
        public void OnHit(GameObject attacker, DamageInfo damageInfo)
        {
            OnHitEvent?.Invoke(new HitInfo()
            {
                Hitter = attacker,
                DamageInfo = damageInfo,
            });
        }
        #endregion
        
        #region Utitilities

        public Vector3 GetActorDirection()
        {
            MovementModule mm = GetModule<MovementModule>();
            if (mm == null) return transform.forward;
            return mm.GetForwardVector();
        }
        #endregion
    }
}