using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core
{
    public class Actor : MonoBehaviour
    {
        protected Dictionary<Type, ActorModule> Modules = new Dictionary<Type, ActorModule>();
        private bool _initialized;

        //Events
        public EventHandler OnModulesInitialized;

        public virtual void Initialize()
        {
            if (_initialized) return;
            ActorModule[] modules = GetComponents<ActorModule>();
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
            _initialized = true;
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
            if(Modules.ContainsKey(typeof(T))) return Modules[typeof(T)] as T;
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