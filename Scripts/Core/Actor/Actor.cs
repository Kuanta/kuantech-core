using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Utils;
using Unity.VisualScripting;
using UnityEngine;

namespace Kuantech.Core
{
    public class Actor : MonoBehaviour
    {
        public string Id;
        protected List<ActorModule> ActorModulesList;
        protected Dictionary<Type, List<ActorModule>> Modules = new Dictionary<Type, List<ActorModule>>();
        public Dictionary<string, ActorModule> ModulesById = new Dictionary<string, ActorModule>();
        protected bool Initialized;

        //Flag to notify about the dirty state
        [NonSerialized] public bool Dirtied = false;

        //Events
        public EventHandler OnModulesInitialized;

        public virtual void Initialize(ActorState actorState = null)
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
            if(actorState != null)
            {
                //Load the data to the state
                LoadActorState(actorState);
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

        protected virtual void Update()
        {

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
        #region State
    
        /// <summary>
        /// The method that should be overriden for actors that needs their o
        /// </summary>
        /// <returns></returns>
        protected virtual ActorState InstantiateActorState()
        {
            return new ActorState()
            {
                ActorId = Id,
            };
        }

        private ActorState CreateActorState()
        {
            ActorState actorState = InstantiateActorState();
            actorState.ActorId = Id;
            return actorState;
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
        /// <param name="actorState"></param>
        public virtual void LoadActorState(ActorState actorState)
        {
            if(actorState == null)
            {
                SetDefaultStateValues();
                return;
            }
            LoadModuleState(actorState.ModuleStates);
        }
        public virtual void LoadModuleState(Dictionary<string, ActorModuleState> moduleStates)
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
        public virtual ActorState GetActorState()
        {
            ActorState actorState = CreateActorState();
            actorState.ModuleStates = new Dictionary<string, ActorModuleState>();
            foreach(var pair in ModulesById)
            {
                if(pair.Value.ModuleId.IsNullOrEmpty()) continue;
                actorState.ModuleStates[pair.Value.ModuleId] = pair.Value.CreateModuleState();
            }
            return actorState;
        }
        #endregion
    }
}