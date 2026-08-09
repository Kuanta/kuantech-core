using System;
using Kuantech.Rpg;
using UnityEngine;
#if NETWORKING_FISHNET
using FishNet.Object;
#else
using FishNet.Connection;
#endif

namespace Kuantech.Core
{
    [Serializable]
    public class ActorData
    {

    }

    /// <summary>
    /// Implemented by a module that knows how urgently its actor needs updating — typically because it
    /// already tracks the thing that decides it, like a horde enemy's distance to the player.
    ///
    /// The module supplies a normalised factor; the actor owns the mapping to actual seconds, so the rates
    /// stay tunable per prefab in the inspector and this stays a statement about need rather than timing.
    /// The actor only asks while it is being updated anyway, so a distant, rarely-ticked actor does not pay
    /// to be asked on the frames it is skipping.
    /// </summary>
    public interface IUpdateRateProvider
    {
        /// <summary>0 = update as often as possible, 1 = update as rarely as this actor allows.</summary>
        float GetUpdateIntervalFactor();
    }

#if NETWORKING_FISHNET
    public abstract class ActorModule : NetworkBehaviour
#else
    public abstract class ActorModule : MonoBehaviour
#endif
    {
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
        public bool IsClientInitialized => true;

        // False in offline: guards ObserversRpc/ClientRpc calls that shouldn't run locally
        public bool IsSpawned => false;

        // FishNet NetworkBehaviour.Owner stub — always null offline; only reached inside
        // `if (IsSpawned)` guards which never fire when IsSpawned returns false.
        protected NetworkConnection Owner => null;
#endif
        [NonSerialized] public Actor Actor;
        [NonSerialized] public bool Initialized;
        public string ModuleId;
        [NonSerialized] public bool Dirtied = false;

        public bool IsDedicatedServer => IsServerInitialized && !IsClientInitialized;

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
        public virtual void ModuleUpdate(float deltaTime)
        {
            
        }

        public virtual void ModuleLateUpdate(float deltaTime)
        {
            
        }
        public virtual void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            if(oldState != ActorState.Spawned && newState == ActorState.Spawned)
            {
                OnActorSpawned();
            }
        }

        public virtual void OnActorSpawned()
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

        // Called on client after actor state is synced from server (late-join sync)
        public virtual void OnNetworkSynced() { }


        #endregion
    }
}