using System.Linq;
using Kuantech.Utils;
using Unity.Netcode;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// The NGO half of an Actor. Actor itself knows only authority and local control; this component answers
    /// those questions from NGO's roles, carries the actor's lifecycle in and out of NGO's spawn callbacks,
    /// and turns the actor's outbound events into RPCs. Sits on the same NetworkObject as the Actor.
    /// </summary>
    [RequireComponent(typeof(Actor))]
    public class NetworkedActor : NetworkBehaviour, IActorNetwork, IActorNetBinding
    {
        private Actor _actor;
        private bool _isLocalPlayer;

        private Actor Actor => _actor != null ? _actor : (_actor = GetComponent<Actor>());

        #region IActorNetwork

        // Explicit: NetworkBehaviour already has a HasAuthority with NGO's own meaning, and the actor wants
        // the plain "am I the server" answer the old IsServer checks gave.
        bool IActorNetwork.HasAuthority => IsServer;
        bool IActorNetwork.IsLocallyControlled => IsOwner;
        ulong IActorNetwork.OwnerId => OwnerClientId;
        // The host is a client too; only a dedicated server has no screen.
        bool IActorNetwork.HasPresentation => IsClient;

        bool IActorNetwork.TryDespawn()
        {
            if (!IsSpawned) return false;
            // NetworkObject.Despawn() notifies all clients; cleanup happens in OnNetworkDespawn.
            NetworkObject.Despawn();
            return true;
        }

        #endregion

        #region IActorNetBinding

        void IActorNetBinding.Bind(Actor actor)
        {
            _actor = actor;
            actor.SetNetwork(this);

            // Unsubscribe first so a repeated Bind (a recycled actor) never doubles the RPCs.
            actor.OnStateChangeBroadcast -= BroadcastActorState;
            actor.OnStateChangeBroadcast += BroadcastActorState;
            actor.OnControllerWarpRequested -= RequestControllerWarp;
            actor.OnControllerWarpRequested += RequestControllerWarp;
            actor.OnStatePushRequested -= PushStateTo;
            actor.OnStatePushRequested += PushStateTo;
        }

        #endregion

        #region Lifecycle

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Actor.Initialize();
            Actor.Spawn();
            if (IsOwner) OnStartLocalPlayer();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (_isLocalPlayer) OnStopLocalPlayer();
            Actor.HandleExternalDespawn();
        }

        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
            if (!_isLocalPlayer) OnStartLocalPlayer();
        }

        public override void OnLostOwnership()
        {
            base.OnLostOwnership();
            if (_isLocalPlayer) OnStopLocalPlayer();
        }

        private void OnStartLocalPlayer()
        {
            _isLocalPlayer = true;
            Actor.StartLocalPlayer();
        }

        private void OnStopLocalPlayer()
        {
            _isLocalPlayer = false;
            Actor.StopLocalPlayer();
        }

        #endregion

        #region Actor State

        private void BroadcastActorState(ActorState state)
        {
            if (IsSpawned) ObserversChangeActorState_Rpc(state);
        }

        [Rpc(SendTo.NotServer)]
        private void ObserversChangeActorState_Rpc(ActorState state)
        {
            Actor.ExecuteChangeActorState(state);
        }

        #endregion

        #region Warp

        private void RequestControllerWarp(Vector3 position, Quaternion rotation)
        {
            if (IsSpawned) WarpToPoint_Rpc(position, rotation);
        }

        [Rpc(SendTo.Owner)]
        private void WarpToPoint_Rpc(Vector3 position, Quaternion rotation)
        {
            // WarpToPoint rather than a bare transform write: RigidbodyMovementModule listens for
            // OnActorWarped and moves the body itself, which a rigidbody needs in order not to fight it.
            Actor.WarpToPoint(position, rotation);
        }

        #endregion

        #region State Sync

        /// <summary>
        /// Pushes a full state snapshot to one client. NGO has no per-object "new observer" callback like
        /// FishNet's OnSpawnServer — KtNetworkManager calls Actor.PushStateTo for every live actor when a
        /// client connects.
        /// </summary>
        private void PushStateTo(ulong clientId)
        {
            if (!IsSpawned) return;
            ActorSerializableData state = Actor.GetActorState();
            int moduleCount = state.ModuleStates?.Count ?? 0;
            Debug.Log($"[Actor] PushStateTo → {name} syncing {moduleCount} module(s) to client {clientId}. Modules: [{string.Join(", ", state.ModuleStates?.Keys ?? Enumerable.Empty<string>())}]");
            byte[] bytes = SaveUtility.SerializePoco(state);
            TargetSyncActorState_Rpc(bytes, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void TargetSyncActorState_Rpc(byte[] bytes, RpcParams rpcParams = default)
        {
            ActorSerializableData state = SaveUtility.DeserializePoco<ActorSerializableData>(bytes);
            int moduleCount = state?.ModuleStates?.Count ?? 0;
            Debug.Log($"[Actor] TargetSyncActorState_Rpc received on {name} — {moduleCount} module(s): [{string.Join(", ", state?.ModuleStates?.Keys ?? Enumerable.Empty<string>())}]");
            Actor.LoadSyncedState(state);
        }

        #endregion

        #region Setup

        /// <summary>
        /// Server calls this after spawn to set the visual on all peers (including host).
        /// </summary>
        [Rpc(SendTo.Everyone)]
        public void SetVisualRpc(string visualId)
        {
            Actor.ApplyVisual(visualId);
        }

        /// <summary>
        /// Server calls this right after spawning a blank Actor as a NetworkObject, to configure it as a
        /// specific ActorBlueprint on every peer (host included) -- the networked counterpart to
        /// ActorBlueprint.CreateActor(), which instead pools+instantiates a whole new local actor. This is
        /// what lets a spawner (NetworkedWaveHandler, ...) hand out real ActorBlueprint variety -- different
        /// visuals, components, balance data -- through one shared network prefab instead of one prefab per
        /// enemy type.
        /// </summary>
        [Rpc(SendTo.Everyone)]
        public void SetActorBlueprintRpc(string blueprintId)
        {
            ActorBlueprint blueprint = ActorDataManager.GetActorBlueprint(blueprintId);
            if (blueprint == null)
            {
                Debug.LogWarning($"[NetworkedActor] SetActorBlueprintRpc: no blueprint found for id '{blueprintId}'.");
                return;
            }
            blueprint.ApplyToActor(Actor);
        }

        #endregion
    }
}
