using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core
{
    public class Actor : MonoBehaviour
    {
        protected Dictionary<Type, ActorModule> Modules = new Dictionary<Type, ActorModule>();
        protected bool Initialized;

        //Events
        public EventHandler OnModulesInitialized;

        public virtual void Initialize()
        {
            if (Initialized) return;
            ActorModule[] modules = GetComponentsInChildren<ActorModule>();
            foreach (ActorModule module in modules)
            {
                Modules[module.GetType()] = module;
                module.Actor = this;
            }

            //Initialize modules after getting them all so that they can require each other in their initialize methods
            foreach(var module in Modules.Values)
            {
                module.Initialize();
            }

            OnModulesInitialized?.Invoke(this, EventArgs.Empty);
            Reset();
            Initialized = true;
            PostModulesInitialized();
        }

        protected virtual void PostModulesInitialized()
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

    }
}