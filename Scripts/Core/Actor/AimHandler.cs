using Kuantech.Core.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    public class AimHandler : ActorModule
    {
        [SerializeField] private Rigidbody Rigidbody;
        [SerializeField] private bool PrioritizeMovementForTargetVector;
        [SerializeField] private float rotateSpeedDegPerSec = 720f;
        [SerializeField] private Transform Anchor;
        [Tooltip("For multiplayer, if rotation is synced turn this off")]
        [SerializeField] private bool RotateOnClient = true;
        [Tooltip("Snaps straight to the target rotation instead of RotateTowards-ing at rotateSpeedDegPerSec. " +
                 "First-person needs this: the camera isn't a child of the actor the way it would be in Unreal, " +
                 "so the body has to match the camera's yaw exactly, every frame, with zero catch-up lag — " +
                 "PlayerModule toggles this when switching view modes.")]
        public bool InstantRotation = false;
        private Vector3 _targetAimVector;
        Quaternion _targetRot = Quaternion.identity;

        /// <summary>
        /// Scales rotateSpeedDegPerSec for as long as something holds it down. 1 = untouched.
        ///
        /// This is the difference between an attack that can be dodged and one that cannot: an attacker
        /// that keeps turning at full speed through its own windup simply follows whoever it is swinging
        /// at, so there is no angle to step out of and no moment of commitment to read.
        ///
        /// A plain value rather than a lock stack, because the one thing that sets it (an attack, through
        /// CombatModule) owns the actor's whole attack window anyway, and the value is cleared both when
        /// the attack ends and when the actor is reset out of the pool.
        /// </summary>
        public float RotationSpeedMultiplier { get; private set; } = 1f;

        public void SetRotationSpeedMultiplier(float multiplier) => RotationSpeedMultiplier = Mathf.Max(0f, multiplier);

        public void ResetRotationSpeedMultiplier() => RotationSpeedMultiplier = 1f;

        public LockKey RotationLockKey;

        private LockModule _lockModule;

        // public void LockRotation(object locker) => RotationLock.Lock(locker);
        // public void UnlockRotation(object locker) => RotationLock.Unlock(locker);

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _lockModule = Actor.GetModule<LockModule>();    
        }

        public override void ModuleLateUpdate(float deltaTime)
        {
            if (!Actor.IsAlive()) return;
            if(!RotateOnClient && !Actor.IsServer) return;
            if (IsRotationLocked()) return;
            _targetAimVector = Actor.MotionVectorsHandler.GetTargetVector(PrioritizeMovementForTargetVector);
            Transform t = Actor.transform;
            if (_targetAimVector.sqrMagnitude < 1e-8f)
                return;
            
            _targetRot = DirectionToRotation(transform, _targetAimVector);

            // A held-down multiplier also suspends InstantRotation -- snapping is exactly what whoever
            // slowed the rotation is trying to prevent, and first person only ever asks for the snap while
            // nothing is slowing it anyway.
            bool instant = InstantRotation && RotationSpeedMultiplier >= 1f;
            float rotateStep = rotateSpeedDegPerSec * RotationSpeedMultiplier * deltaTime;

            if(Rigidbody == null || Rigidbody.isKinematic)
            {
                t.rotation = instant
                    ? _targetRot
                    : Quaternion.RotateTowards(t.rotation, _targetRot, rotateStep);
            }
            else
            {
                var next = instant
                    ? _targetRot
                    : Quaternion.RotateTowards(Rigidbody.rotation, _targetRot, rotateStep);
                Rigidbody.MoveRotation(next);
            }
        }
        
        private Quaternion DirectionToRotation(Transform anchor, Vector3 direction)
        {
            Vector3 axis = Actor.ActorUpVector;
            
            Vector3 projected = Vector3.ProjectOnPlane(direction, axis);
            if (projected.sqrMagnitude < 1e-8f)
            {
                projected = Vector3.ProjectOnPlane(anchor.forward, axis);
                if (projected.sqrMagnitude < 1e-8f)
                {
                    projected = Vector3.Cross(axis, anchor.right);
                }
            }

            projected.Normalize();
            return Quaternion.LookRotation(projected, axis);
        }
        
        //Rotates immediately
        public void SetDirection(Vector3 direction)
        {
            Quaternion rot = DirectionToRotation(transform, direction);
            _targetAimVector = direction;

            transform.rotation = rot;
            if(Rigidbody != null)
            {
                Rigidbody.rotation = rot;
            }
        }

        public override void ResetModule()
        {
            base.ResetModule();

            // Safety net for the pool: an actor despawned mid-attack never runs the end of that attack on
            // every peer, and would otherwise come back up still turning at a fraction of its speed.
            ResetRotationSpeedMultiplier();
        }

        #region Locks
        public bool IsRotationLocked()
        {
            if(_lockModule == null || RotationLockKey == null) return false;
            return _lockModule.IsLocked(RotationLockKey);
        }

        public void LockRotation(object locker)
        {
            if (_lockModule == null)     { Debug.LogWarning($"[AimHandler] {Actor.name}: LockModule is null");      return; }
            if (RotationLockKey == null) { Debug.LogWarning($"[AimHandler] {Actor.name}: RotationLockKey is null"); return; }
            _lockModule.Lock(RotationLockKey, locker);
        }

        public void UnlockRotation(object locker)
        {
            if (_lockModule == null) return;
            _lockModule.Unlock(RotationLockKey, locker);
        }
        #endregion
    }
}