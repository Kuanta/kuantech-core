using FishNet.Object;
using FishNet.Object.Synchronizing;
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
        private readonly SyncVar<Vector3> _syncedMovement = new();
        private readonly SyncVar<Vector3> _syncedTargetVector = new();
        private readonly SyncVar<float> _syncedSpeedMultiplier = new();

        public override void Initialize()
        {
            base.Initialize();
            Actor.MotionVectorsHandler.OnMovementVectorChanged += NotifyMovementVectorChanged;
            Actor.MotionVectorsHandler.OnTargetVectorChanged += NotifyTargetVectorChanged;
            Actor.MotionVectorsHandler.OnMovementMultiplierChanged += NotifySpeedMultiplierChanged;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _syncedMovement.OnChange += OnMovementChanged;
            _syncedTargetVector.OnChange += OnTargetVectorChanged;
            _syncedSpeedMultiplier.OnChange += OnSpeedMultiplierChanged;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _syncedMovement.OnChange -= OnMovementChanged;
            _syncedTargetVector.OnChange -= OnTargetVectorChanged;
            _syncedSpeedMultiplier.OnChange -= OnSpeedMultiplierChanged;
        }

        /// <summary>
        /// Called by MotionVectorsHandler when MovementVector is set.
        /// </summary>
        public void NotifyMovementVectorChanged(Vector3 movement)
        {
            if (IsServerInitialized)
                _syncedMovement.Value = movement;
            else if (IsOwner)
                ServerRpc_SetMovement(movement);
        }

        /// <summary>
        /// Called by MotionVectorsHandler when TargetVector is set.
        /// </summary>
        public void NotifyTargetVectorChanged(Vector3 targetVector)
        {
            if (IsServerInitialized)
                _syncedTargetVector.Value = targetVector;
            else if (IsOwner)
                ServerRpc_SetTargetVector(targetVector);
        }

        public void NotifySpeedMultiplierChanged(float speedMultiplier)
        {
            if (IsServerInitialized)
                _syncedSpeedMultiplier.Value = speedMultiplier;
            else if (IsOwner)
                ServerRpc_SetSpeedMultiplier(speedMultiplier);
        }

        [ServerRpc]
        private void ServerRpc_SetMovement(Vector3 movement)
        {
            _syncedMovement.Value = movement;
            Actor.MotionVectorsHandler.MovementVector = movement;
        }

        [ServerRpc]
        private void ServerRpc_SetTargetVector(Vector3 targetVector)
        {
            _syncedTargetVector.Value = targetVector;
            Actor.MotionVectorsHandler.TargetVector = targetVector;
        }

        [ServerRpc]
        private void ServerRpc_SetSpeedMultiplier(float speedMultiplier)
        {
            _syncedSpeedMultiplier.Value = speedMultiplier;
            Actor.MotionVectorsHandler.MovementMultiplier = speedMultiplier;
        }

        private void OnMovementChanged(Vector3 _, Vector3 next, bool asServer)
        {
            if (!asServer && !IsOwner)
                Actor.MotionVectorsHandler.MovementVector = next;
        }

        private void OnTargetVectorChanged(Vector3 _, Vector3 next, bool asServer)
        {
            if (!asServer && !IsOwner)
                Actor.MotionVectorsHandler.TargetVector = next;
        }

        private void OnSpeedMultiplierChanged(float _, float next, bool asServer)
        {
            if(!asServer && !IsOwner)
            {
                Actor.MotionVectorsHandler.MovementMultiplier = next;
            }
        }
    }
}
