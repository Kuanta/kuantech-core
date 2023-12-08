using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    public class Actor : MonoBehaviour
    {
        public string Id;
        protected List<ActorModule> ActorModulesList;
        protected Dictionary<Type, ActorModule> Modules = new Dictionary<Type, ActorModule>();
        public Dictionary<string, ActorModule> ModulesById = new Dictionary<string, ActorModule>();
        protected bool Initialized;

        //Events
        public EventHandler OnModulesInitialized;
        [NonSerialized] public ActorState CurrentState; //ActorState holds informationabout actor
        [NonSerialized] public StateModule StateModel; //StateModel is a general module tha holds information about various game features

        public virtual void Initialize(string actorState = null)
        {
            if (Initialized) return;
            ActorModulesList = GetComponentsInChildren<ActorModule>().ToList();
            ModulesById = new Dictionary<string, ActorModule>();
            foreach (ActorModule module in ActorModulesList)
            {
                Modules[module.GetType()] = module;
                module.Actor = this;
                if(!module.ModuleId.IsNullOrEmpty()) ModulesById[module.ModuleId] = module;
            }

            //Initialize modules after getting them all so that they can require each other in their initialize methods
            foreach(var module in Modules.Values)
            {
                module.Initialize();
            }
            CreateActorState();
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
            foreach (var module in Modules.Values)
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
            if(Modules.ContainsKey(type))
            {
                return Modules[type];
            }
            return null;
        }
        
        public T GetModule<T>() where T : ActorModule
        {
            foreach (var module in Modules.Values)
            {
                if (module is T)
                {
                    return module as T;
                }
            }

            return null;
        }

        protected virtual void Update()
        {

        }

        public virtual void Reset()
        {
            foreach (var key in Modules.Keys)
            {
                Modules[key].Reset();
            }
        }

        #region State
        /// <summary>
        /// The method that should be overriden for actors that needs their o
        /// </summary>
        /// <returns></returns>
        public virtual ActorState InstantiateActorState()
        {
            return new ActorState(){Actor = this};
        }
        public void CreateActorState()
        {
            CurrentState = InstantiateActorState();
            CurrentState.EncodedModuleStates = new Dictionary<string, string>();
            CurrentState.Actor = this;
        }
        public virtual void DirtyState()
        {
            CurrentState.Dirtied = true; //This is the state of the actor
            if (StateModel == null) return;
            StateModel.Dirtied = true; //This is the state model this actor belongs to
        }

        /// <summary>
        /// Sets the default values for the actor state
        /// </summary>
        public virtual void SetDefaultStateValues()
        {
    
        }

        /// <summary>
        /// Loads the actor state
        /// </summary>
        /// <param name="actorState"></param>
        public virtual void LoadActorState(string encodedState)
        {
            CreateActorState();
            if(encodedState == null)
            {
                SetDefaultStateValues();
                return;
            }
            CurrentState.DecodeState(encodedState);
            foreach (var pair in CurrentState.EncodedModuleStates)
            {
                if(pair.Key.IsNullOrEmpty()) continue;
                if(!ModulesById.ContainsKey(pair.Key))
                {
                    Debug.LogError("Id is missing:"+pair.Key);
                    continue;
                }
                ActorModule module = ModulesById[pair.Key];
                module.LoadState(pair.Value);
            }
            CurrentState.Actor = this; //Needed somehow
        }

        public virtual string SaveActorState()
        {
            return CurrentState.EncodeState();
        }
        #endregion
    }
}