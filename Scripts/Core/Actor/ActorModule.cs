using System;
using UnityEngine;

namespace Kuantech.Core
{
    public abstract class ActorModule : MonoBehaviour {
        [NonSerialized] public Actor Actor;
        [NonSerialized] public bool Initialized;
        public string ModuleId;
        [NonSerialized] public bool Dirtied = false;
        public virtual void Initialize()
        {
            if(Initialized) return;
            Initialized = true;
            CreateModuleState();
        }

        public virtual void OnModulesInitialized(){}
        public virtual void Reset(){}

        public virtual void Cleanup(){}

        #region State
        /// <summary>
        /// Dirties the state of parent actor
        /// </summary>
        public virtual void DirtyState()
        {
            if(Actor == null) return;
            Dirtied = true;
            Actor.DirtyState();
        }

        protected virtual ActorModuleState InstantiateState()
        {
            return new ActorModuleState();
        }
        public virtual ActorModuleState CreateModuleState()
        {
            ActorModuleState actorState = InstantiateState();
            actorState.ModuleId = ModuleId;
            return actorState;
        }
        /// <summary>
        /// Loads the state for this module
        /// </summary>
        /// <param name="state"></param>
        public virtual void LoadState(ActorModuleState state)
        {

        }
        #endregion
    }
}