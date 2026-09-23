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
        [Header("Aim")]
        [Tooltip("Resamples the direction this actor is deliberately facing on a fixed cadence, instead " +
                 "of only when something sets it. Off by default -- this is shared Core and it costs a " +
                 "write per actor per interval. Turn it on for server-driven AI that faces a MOVING " +
                 "target and whose rotation is computed per-peer by AimHandler (i.e. NetworkTransform is " +
                 "not syncing rotation).")]
        [SerializeField] private bool SyncAimDirection;

        [Tooltip("Seconds between aim samples; 0 samples every frame. A sample is only written when the " +
                 "direction actually moved, so a short interval costs nothing for an actor standing " +
                 "still facing something that is also standing still.")]
        [SerializeField] private float AimSyncInterval = 0.05f;

        [Tooltip("Degrees the aim direction has to move before a sample is written.")]
        [SerializeField] private float AimSyncAngleThreshold = 2f;

#if NETWORKING_NGO
        private readonly NetworkVariable<Vector3> _syncedMovement = new NetworkVariable<Vector3>();
        private readonly NetworkVariable<Vector3> _syncedTargetVector = new NetworkVariable<Vector3>();
        private readonly NetworkVariable<float> _syncedSpeedMultiplier = new NetworkVariable<float>();
#else
        private readonly OfflineNetworkVariable<Vector3> _syncedMovement = new OfflineNetworkVariable<Vector3>();
        private readonly OfflineNetworkVariable<Vector3> _syncedTargetVector = new OfflineNetworkVariable<Vector3>();
        private readonly OfflineNetworkVariable<float> _syncedSpeedMultiplier = new OfflineNetworkVariable<float>();
#endif

        private Vector3 _lastSyncedAim;
        private float _lastAimSyncTime;

        public override void Initialize()
        {
            base.Initialize();
            Actor.MotionVectorsHandler.OnMovementVectorChanged += NotifyMovementVectorChanged;
            Actor.MotionVectorsHandler.OnTargetVectorChanged += NotifyTargetVectorChanged;
            Actor.MotionVectorsHandler.OnMovementMultiplierChanged += NotifySpeedMultiplierChanged;
            Actor.MotionVectorsHandler.OnTargetChanged += OnTargetObjectChanged;
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
        /// Who an actor is facing cannot be replicated as a reference: TargetedObject is a plain Transform
        /// and is under no obligation to be a NetworkObject -- a scene prop, a bone, a hit point on a rig
        /// somebody else owns all qualify. So the DIRECTION is what travels.
        ///
        /// And it has to be resampled rather than captured once when the target was picked, because both
        /// actors keep moving relative to each other. A sample taken at target-change time is correct for
        /// exactly one frame, and worse than sending nothing at all: GetTargetVector ranks TargetVector
        /// ABOVE MovementVector, so the stale sample would outrank the live walk direction remote peers
        /// were already facing correctly.
        /// </summary>
        public override void ModuleUpdate(float deltaTime)
        {
            base.ModuleUpdate(deltaTime);
            if (!SyncAimDirection) return;

            if (!IsServer)
            {
                ClearLocallySetTarget();
                return;
            }
#if NETWORKING_NGO
            if (!IsSpawned) return;
#endif
            if (Time.time - _lastAimSyncTime < AimSyncInterval) return;
            _lastAimSyncTime = Time.time;

            Vector3 aim = Actor.MotionVectorsHandler.GetAimDirection();
            if (!HasAimMovedEnough(aim)) return;

            _lastSyncedAim = aim;
            _syncedTargetVector.Value = aim;
        }

        /// <summary>
        /// A target change is used as a hint, not as the message itself: it is exactly when the aim
        /// direction jumps, so give up the rest of the current interval and resample on the next tick.
        /// The direction still travels as a direction -- this only decides when it is sampled.
        /// </summary>
        private void OnTargetObjectChanged(Transform _) => _lastAimSyncTime = float.NegativeInfinity;

        /// <summary>
        /// On a peer that takes this actor's facing off the wire, TargetedObject is not allowed to hold
        /// anything: the replicated direction is the whole truth there.
        ///
        /// Needed because CombatModule points TargetedObject at whoever is being swung at on EVERY peer --
        /// ExecuteAttack runs everywhere, not only on the server -- and never clears it, while
        /// GetTargetVector ranks TargetedObject ABOVE TargetVector. Without this a remote enemy keeps
        /// staring at the last player it attacked and ignores every direction the server sends. It is the
        /// original "it still faces the corpse" bug surviving on clients alone, because on the server the
        /// AI overwrites TargetedObject with the live target every frame and on a client nothing does.
        ///
        /// Done as a per-frame check rather than a clear inside the receive callback on purpose: whether a
        /// synced value arrives at all depends on the sample threshold, so hanging the fix off that would
        /// leave the bug alive in exactly the case where the old and new targets lie in nearly the same
        /// direction.
        /// </summary>
        private void ClearLocallySetTarget()
        {
            if (IsOwner) return;
            MotionVectorsHandler handler = Actor.MotionVectorsHandler;
            if (handler.TargetedObject != null) handler.TargetedObject = null;
        }

        /// <summary>
        /// What makes a short interval affordable: a zombie standing over a stationary player resamples
        /// constantly and writes nothing.
        /// </summary>
        private bool HasAimMovedEnough(Vector3 aim)
        {
            bool wasAiming = _lastSyncedAim.sqrMagnitude > float.Epsilon;
            bool isAiming = aim.sqrMagnitude > float.Epsilon;

            // Dropping to zero gets through whatever the threshold says. Zero means "nothing is aiming me
            // any more", and a peer that never hears it keeps facing the last direction forever instead of
            // falling back to its movement vector.
            if (!wasAiming || !isAiming) return wasAiming != isAiming;

            return Vector3.Angle(_lastSyncedAim, aim) >= AimSyncAngleThreshold;
        }

        public override void ResetModule()
        {
            base.ResetModule();
            _lastSyncedAim = Vector3.zero;
            _lastAimSyncTime = float.NegativeInfinity;

            // MotionVectorsHandler.Reset() clears its own vectors by writing the fields directly, so none
            // of it reaches this syncer. Without clearing here, an actor coming back out of the pool would
            // still be replicating the aim direction of its previous life.
            if (!IsServer) return;
#if NETWORKING_NGO
            if (!IsSpawned) return;
#endif
            _syncedTargetVector.Value = Vector3.zero;
        }

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
