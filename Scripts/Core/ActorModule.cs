using System;
using UnityEngine;

namespace Kuantech.Core
{
    public abstract class ActorModule : MonoBehaviour {
        [NonSerialized] public Actor Actor;
        [NonSerialized] public bool Initialized;
        public string ModuleId;
        public bool IsSaveable;
        public virtual void Initialize()
        {
            CreateModuleState();
            CurrentState.ModuleId = ModuleId;
        }
        public virtual void OnModulesInitialized(){}
        public virtual void Reset(){}
        public ActorModuleState CurrentState;

        #region State
        /// <summary>
        /// Dirties the state of parent actor
        /// </summary>
        public virtual void DirtyState()
        {
            if(Actor == null) return;
            CurrentState.Dirtied = true;
            Actor.DirtyState();
        }

        /// <summary>
        /// Sets the default values for the module's state
        /// </summary>
        public virtual void SetDefaultStateValues()
        {

        }
        public virtual ActorModuleState InstantiateState()
        {
            return new ActorModuleState();
        }
        public virtual void CreateModuleState()
        {
            CurrentState = InstantiateState();
            CurrentState.ModuleId = ModuleId;
        }
        /// <summary>
        /// Loads the state for this module
        /// </summary>
        /// <param name="state"></param>
        public virtual void LoadState(string encodedState)
        {
            CreateModuleState();
            CurrentState.DecodeState(encodedState);
        }
        #endregion
    }
}