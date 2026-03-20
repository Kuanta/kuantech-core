using System;
using Kuantech.Rpg;
using UnityEngine;
#if NETWORKING_FISHNET
using FishNet.Object;
#endif

namespace Kuantech.Core
{
    [Serializable]
    public class ActorData
    {

    }

#if NETWORKING_FISHNET
    public abstract class ActorModule : NetworkBehaviour
#else
    public abstract class ActorModule : MonoBehaviour
#endif
    {
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

        public virtual void ModuleFixedUpdate()
        {
            
        }
        public virtual void ModuleUpdate()
        {
            
        }

        public virtual void ModuleLateUpdate()
        {
            
        }
        public virtual void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            
        }

        public virtual void OnActorRankSet(int rank)
        {
            
        }
        public virtual void ResetModule(){}

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

        #region Networking

        // Called by KtActorNetworkBehaviour when this actor becomes the local player
        public virtual void OnLocalPlayerStart() { }
        public virtual void OnLocalPlayerStop() { }

#if !NETWORKING_FISHNET
        // Stub callbacks — no-op in offline builds.
        // When NETWORKING_FISHNET is defined, NetworkBehaviour provides these.
        public virtual void OnStartNetwork() { }
        public virtual void OnStopNetwork() { }
        public virtual void OnStartServer() { }
        public virtual void OnStopServer() { }
        public virtual void OnStartClient() { }
        public virtual void OnStopClient() { }

        public bool IsOwner => true;
        public bool IsServer => true;
        public bool IsClient => true;
        public bool IsServerStarted => true;
        public bool IsServerInitialized => true;
        public bool IsClientStarted => true;
        public bool IsClientOnlyInitialized => false;
        // False in offline: guards ObserversRpc/ClientRpc calls that shouldn't run locally
        public bool IsSpawned => false;
#endif
        #endregion
    }
}