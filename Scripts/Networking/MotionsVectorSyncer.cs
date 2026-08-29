#if NETWORKING_NGO
using Unity.Netcode;
#endif
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// Syncs MotionVectorsHandler vectors across the network.
    /// Add this component to networked actors. In single-player, leave it out.
    /// MotionVectorsHandler notifies this syncer whenever a vector is set,
    /// so all existing callers (SpellBook, NavMesh, AI, etc.) work without changes.
    /// </summary>
    public class MotionVectorSyncer : ActorModule
    {
#if NETWORKING_NGO
        private readonly NetworkVariable<Vector3> _syncedMovement = new NetworkVariable<Vector3>();
        private readonly NetworkVariable<Vector3> _syncedTargetVector = new NetworkVariable<Vector3>();
        private readonly NetworkVariable<float> _syncedSpeedMultiplier = new NetworkVariable<float>();
#else
        private readonly OfflineNetworkVariable<Vector3> _syncedMovement = new OfflineNetworkVariable<Vector3>();
        private readonly OfflineNetworkVariable<Vector3> _syncedTargetVector = new OfflineNetworkVariable<Vector3>();
        private readonly OfflineNetworkVariable<float> _syncedSpeedMultiplier = new OfflineNetworkVariable<float>();
#endif

        public override void Initialize()
        {
            base.Initialize();
            Actor.MotionVectorsHandler.OnMovementVectorChanged += NotifyMovementVectorChanged;
            Actor.MotionVectorsHandler.OnTargetVectorChanged += NotifyTargetVectorChanged;
            Actor.MotionVectorsHandler.OnMovementMultiplierChanged += NotifySpeedMultiplierChanged;
#if !NETWORKING_NGO
            _syncedMovement.OnValueChanged += OnMovementChanged;
            _syncedTargetVector.OnValueChanged += OnTargetVectorChanged;
            _syncedSpeedMultiplier.OnValueChanged += OnSpeedMultiplierChanged;
#endif
        }

#if NETWORKING_NGO
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _syncedMovement.OnValueChanged += OnMovementChanged;
            _syncedTargetVector.OnValueChanged += OnTargetVectorChanged;
            _syncedSpeedMultiplier.OnValueChanged += OnSpeedMultiplierChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _syncedMovement.OnValueChanged -= OnMovementChanged;
            _syncedTargetVector.OnValueChanged -= OnTargetVectorChanged;
            _syncedSpeedMultiplier.OnValueChanged -= OnSpeedMultiplierChanged;
        }
#endif

        /// <summary>
        /// Called by MotionVectorsHandler when MovementVector is set.
        /// </summary>
        public void NotifyMovementVectorChanged(Vector3 movement)
        {
            if (IsServer)
                _syncedMovement.Value = movement;
            else if (IsOwner)
                ServerRpc_SetMovement_Rpc(movement);
        }

        /// <summary>
        /// Called by MotionVectorsHandler when TargetVector is set.
        /// </summary>
        public void NotifyTargetVectorChanged(Vector3 targetVector)
        {
            if (IsServer)
                _syncedTargetVector.Value = targetVector;
            else if (IsOwner)
                ServerRpc_SetTargetVector_Rpc(targetVector);
        }

        public void NotifySpeedMultiplierChanged(float speedMultiplier)
        {
            if (IsServer)
                _syncedSpeedMultiplier.Value = speedMultiplier;
            else if (IsOwner)
                ServerRpc_SetSpeedMultiplier_Rpc(speedMultiplier);
        }

#if NETWORKING_NGO
        [Rpc(SendTo.Server)]
#endif
        private void ServerRpc_SetMovement_Rpc(Vector3 movement)
        {
            _syncedMovement.Value = movement;
            Actor.MotionVectorsHandler.MovementVector = movement;
        }

#if NETWORKING_NGO
        [Rpc(SendTo.Server)]
#endif
        private void ServerRpc_SetTargetVector_Rpc(Vector3 targetVector)
        {
            _syncedTargetVector.Value = targetVector;
            Actor.MotionVectorsHandler.TargetVector = targetVector;
        }

#if NETWORKING_NGO
        [Rpc(SendTo.Server)]
#endif
        private void ServerRpc_SetSpeedMultiplier_Rpc(float speedMultiplier)
        {
            _syncedSpeedMultiplier.Value = speedMultiplier;
            Actor.MotionVectorsHandler.MovementMultiplier = speedMultiplier;
        }

        private void OnMovementChanged(Vector3 _, Vector3 next)
        {
            if (!IsServer && !IsOwner)
                Actor.MotionVectorsHandler.MovementVector = next;
        }

        private void OnTargetVectorChanged(Vector3 _, Vector3 next)
        {
            if (!IsServer && !IsOwner)
                Actor.MotionVectorsHandler.TargetVector = next;
        }

        private void OnSpeedMultiplierChanged(float _, float next)
        {
            if (!IsServer && !IsOwner)
            {
                Actor.MotionVectorsHandler.MovementMultiplier = next;
            }
        }
    }
}
