using System;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public class ActorData
    {
            
    }
    
    public abstract class ActorModule : MonoBehaviour {
        [NonSerialized] public Actor Actor;
        [NonSerialized] public bool Initialized;
        public string ModuleId;
        [NonSerialized] public bool Dirtied = false;

        public virtual void SetActorData(ActorData actorData)
        {
            
        }
        
        public virtual void Initialize()
        {
            if(Initialized) return;
            Initialized = true;
            CreateModuleState();
        }

        public virtual void OnModulesInitialized()
        {
            
        }

        public virtual void ModuleUpdate()
        {
            
        }

        public virtual void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            
        }

        public virtual void OnActorRankSet(int rank)
        {
            
        }
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

        protected virtual ActorModuleSerializableData InstantiateState()
        {
            return new ActorModuleSerializableData();
        }
        public virtual ActorModuleSerializableData CreateModuleState()
        {
            ActorModuleSerializableData actorSerializableData = InstantiateState();
            actorSerializableData.ModuleId = ModuleId;
            return actorSerializableData;
        }
        /// <summary>
        /// Loads the state for this module
        /// </summary>
        /// <param name="serializableData"></param>
        public virtual void LoadState(ActorModuleSerializableData serializableData)
        {

        }

        public virtual void SetDefaultValues()
        {

        }
        #endregion

    }
}